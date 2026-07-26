using System.Collections.Generic;

namespace RelicTerror.GameState;

/// <summary>One item stack as Allagan Tools reports it, before its container is given a label.</summary>
internal readonly record struct PlacementRow(int Quantity, ulong OwnerId, uint Container);

/// <summary>
/// An indexed read of every container Allagan Tools tracks for the active character - its own
/// bags, its retainers, its Free Company and its houses. Built once per refresh and reused, since
/// each pull allocates one array per item stack per owner.
/// </summary>
internal sealed class InventorySnapshot
{
    // Upstream's ToNumeric writes 25 fixed fields before a variable-length gear-set tail.
    private const int MinimumRowLength = 25;
    private const int ItemIdIndex      = 2;
    private const int QuantityIndex    = 3;
    private const int ContainerIndex   = 20;
    private const int OwnerIdIndex     = 23;

    // Some inventory sources report high-quality entries offset by a million. Requirements are
    // keyed on the base id, matching how ReadItemCounts uses BaseItemId.
    private const uint HqItemIdOffset = 1_000_000;

    private readonly Dictionary<uint, int>                _counts;
    private readonly Dictionary<uint, List<PlacementRow>> _placements;
    private readonly HashSet<uint>                        _storedItemIds;

    private InventorySnapshot(
        Dictionary<uint, int> counts,
        Dictionary<uint, List<PlacementRow>> placements,
        HashSet<uint> storedItemIds)
    {
        _counts        = counts;
        _placements    = placements;
        _storedItemIds = storedItemIds;
    }

    internal IReadOnlyDictionary<uint, int> Counts => _counts;

    /// <summary>Items sitting in the Armoire or Glamour Dresser, for the ownership set.</summary>
    internal IReadOnlySet<uint> StoredItemIds => _storedItemIds;

    internal IReadOnlyList<PlacementRow> PlacementsFor(uint itemId) =>
        _placements.TryGetValue(itemId, out var rows) ? rows : [];

    /// <summary>
    /// Reads the active character's whole storage through Allagan Tools. Returns null when it is
    /// absent, still warming up, or reports nothing for the active character - the caller then
    /// falls back to the in-memory scan rather than trusting a result that would zero every count.
    /// </summary>
    internal static InventorySnapshot? TryBuild(AllaganToolsIpc ipc)
    {
        if (!ipc.IsAvailable) return null;

        var activeId = ipc.CurrentCharacter();
        if (activeId == 0) return null;

        var ownedIds = ipc.OwnedCharacterIds();
        if (!ownedIds.Contains(activeId)) return null;

        // An empty own-inventory means the cache has not populated yet.
        var activeRows = ipc.CharacterItems(activeId);
        if (activeRows.Count == 0) return null;

        var counts     = new Dictionary<uint, int>();
        var placements = new Dictionary<uint, List<PlacementRow>>();
        var stored     = new HashSet<uint>();

        Ingest(activeRows, counts, placements, stored);

        foreach (var ownerId in ownedIds)
        {
            if (ownerId == activeId) continue;
            Ingest(ipc.CharacterItems(ownerId), counts, placements, stored);
        }

        return new InventorySnapshot(counts, placements, stored);
    }

    private static void Ingest(
        IReadOnlyCollection<ulong[]> rows,
        Dictionary<uint, int> counts,
        Dictionary<uint, List<PlacementRow>> placements,
        HashSet<uint> stored)
    {
        foreach (var row in rows)
        {
            if (row.Length < MinimumRowLength) continue;

            var itemId = (uint)row[ItemIdIndex];
            if (itemId == 0) continue;
            if (itemId > HqItemIdOffset) itemId -= HqItemIdOffset;

            var container = (uint)row[ContainerIndex];
            if (!ContainerLabels.IsCountable(container)) continue;

            // Dresser and armoire entries prove ownership regardless of the quantity they report.
            if (container is ContainerLabels.ArmoireContainer or ContainerLabels.GlamourChestContainer)
                stored.Add(itemId);

            var quantity = (int)row[QuantityIndex];
            if (quantity <= 0) continue;

            counts.TryGetValue(itemId, out var running);
            counts[itemId] = running + quantity;

            if (!placements.TryGetValue(itemId, out var list))
                placements[itemId] = list = [];
            list.Add(new PlacementRow(quantity, row[OwnerIdIndex], container));
        }
    }
}
