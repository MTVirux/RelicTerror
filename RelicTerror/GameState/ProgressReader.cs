using System.Collections.Generic;
using Dalamud.Game.Inventory;
using Lumina.Excel.Sheets;
using CSMirage          = FFXIVClientStructs.FFXIV.Client.Game.MirageManager;
using CSQuestManager    = FFXIVClientStructs.FFXIV.Client.Game.QuestManager;
using CSRetainerManager = FFXIVClientStructs.FFXIV.Client.Game.RetainerManager;
using CSUIState         = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState;

namespace RelicTerror.GameState;

internal sealed class ProgressReader
{
    private static readonly GameInventoryType[] ScannedBags =
    [
        GameInventoryType.Inventory1,
        GameInventoryType.Inventory2,
        GameInventoryType.Inventory3,
        GameInventoryType.Inventory4,
        GameInventoryType.KeyItems,
        GameInventoryType.ArmoryMainHand,
        GameInventoryType.ArmoryOffHand,
        GameInventoryType.EquippedItems,
        GameInventoryType.SaddleBag1,
        GameInventoryType.SaddleBag2,
        GameInventoryType.PremiumSaddleBag1,
        GameInventoryType.PremiumSaddleBag2,
        GameInventoryType.RetainerPage1,
        GameInventoryType.RetainerPage2,
        GameInventoryType.RetainerPage3,
        GameInventoryType.RetainerPage4,
        GameInventoryType.RetainerPage5,
        GameInventoryType.RetainerPage6,
        GameInventoryType.RetainerPage7,
        GameInventoryType.RetainerEquippedItems,
    ];

    private Dictionary<uint, uint>? _itemIdToCabinetId;

    private static readonly GameInventoryType[] LookupBags =
    [
        GameInventoryType.EquippedItems,
        GameInventoryType.ArmoryMainHand,
        GameInventoryType.ArmoryOffHand,
        GameInventoryType.Inventory1,
        GameInventoryType.Inventory2,
        GameInventoryType.Inventory3,
        GameInventoryType.Inventory4,
        GameInventoryType.SaddleBag1,
        GameInventoryType.SaddleBag2,
        GameInventoryType.PremiumSaddleBag1,
        GameInventoryType.PremiumSaddleBag2,
        GameInventoryType.RetainerPage1,
        GameInventoryType.RetainerPage2,
        GameInventoryType.RetainerPage3,
        GameInventoryType.RetainerPage4,
        GameInventoryType.RetainerPage5,
        GameInventoryType.RetainerPage6,
        GameInventoryType.RetainerPage7,
        GameInventoryType.RetainerEquippedItems,
        GameInventoryType.KeyItems,
    ];

    internal Dictionary<uint, int> ReadItemCounts()
    {
        var counts = new Dictionary<uint, int>();
        foreach (var bag in ScannedBags)
        {
            foreach (var item in Services.GameInventory.GetInventoryItems(bag))
            {
                if (item.BaseItemId == 0) continue;
                counts.TryGetValue(item.BaseItemId, out var existing);
                counts[item.BaseItemId] = existing + (int)item.Quantity;
            }
        }
        return counts;
    }

    internal unsafe HashSet<uint> ReadGlamourDresserItemIds()
    {
        var stored = new HashSet<uint>();
        var mirage = CSMirage.Instance();
        if (mirage == null || !mirage->PrismBoxLoaded) return stored;

        foreach (var id in mirage->PrismBoxItemIds)
        {
            if (id == 0) continue;
            // PrismBox stores HQ items as itemId + 1_000_000; strip it for matching.
            stored.Add(id > 1_000_000 ? id - 1_000_000 : id);
        }
        return stored;
    }

    internal unsafe HashSet<uint> ReadArmoireItemIds()
    {
        var owned = new HashSet<uint>();
        var uiState = CSUIState.Instance();
        if (uiState == null || !uiState->Cabinet.IsCabinetLoaded()) return owned;

        foreach (var (itemId, cabinetId) in GetItemIdToCabinetId())
        {
            if (uiState->Cabinet.IsItemInCabinet(cabinetId))
                owned.Add(itemId);
        }
        return owned;
    }

    internal unsafe bool IsQuestAccepted(uint questId)
    {
        var qm = CSQuestManager.Instance();
        return qm != null && qm->IsQuestAccepted(questId);
    }

    // For repeatable quests this reports "completed at least once".
    internal bool IsQuestComplete(uint questId) => CSQuestManager.IsQuestComplete(questId);

    private Dictionary<uint, uint> GetItemIdToCabinetId()
    {
        if (_itemIdToCabinetId != null) return _itemIdToCabinetId;

        var map = new Dictionary<uint, uint>();
        foreach (var row in Services.DataManager.GetExcelSheet<Cabinet>())
        {
            var itemId = row.Item.RowId;
            if (itemId != 0) map[itemId] = row.RowId;
        }
        _itemIdToCabinetId = map;
        return map;
    }

    /// <summary>Where one item sits, and how much of it is there.</summary>
    internal sealed record ItemPlacement(string Label, int Quantity, StorageCategory Category);

    internal sealed record ItemLocation(string ItemName, IReadOnlyList<ItemPlacement> Placements);

    /// <summary>
    /// Allagan Tools' view of the active character's storage, refreshed by the plugin's rebuild
    /// loop. Null means it is unavailable and lookups fall back to scanning resident game memory.
    /// </summary>
    internal InventorySnapshot? Snapshot { get; set; }

    /// <summary>
    /// Whether lookups currently cover storage the game does not keep resident - unsummoned
    /// retainers, the Free Company chest, housing. Drives the caveats shown in tooltips.
    /// </summary>
    internal bool CoversAllStorage => Snapshot is not null;

    internal ItemLocation? FindItemLocation(uint baseItemId)
    {
        if (baseItemId == 0) return null;

        var name = ResolveItemName(baseItemId);

        return new ItemLocation(name, Snapshot is { } snapshot
            ? FromSnapshot(snapshot, baseItemId)
            : FromResidentBags(baseItemId));
    }

    private static IReadOnlyList<ItemPlacement> FromSnapshot(InventorySnapshot snapshot, uint baseItemId)
    {
        var rows = snapshot.PlacementsFor(baseItemId);
        if (rows.Count == 0) return [];

        var merged = new Dictionary<string, ItemPlacement>();
        foreach (var row in rows)
        {
            var (label, category) = ContainerLabels.Describe(row.Container, row.OwnerId);
            Accumulate(merged, label, row.Quantity, category);
        }

        return Ordered(merged);
    }

    private IReadOnlyList<ItemPlacement> FromResidentBags(uint baseItemId)
    {
        var merged = new Dictionary<string, ItemPlacement>();

        foreach (var bag in LookupBags)
        {
            foreach (var item in Services.GameInventory.GetInventoryItems(bag))
            {
                if (item.BaseItemId != baseItemId) continue;
                Accumulate(merged, BagLabel(bag), (int)item.Quantity, CategoryOf(bag));
            }
        }

        // The dresser and armoire report membership, not quantity; both hold one of each item.
        if (ReadGlamourDresserItemIds().Contains(baseItemId))
            Accumulate(merged, "Glamour Dresser", 1, StorageCategory.Personal);

        if (ReadArmoireItemIds().Contains(baseItemId))
            Accumulate(merged, "Armoire", 1, StorageCategory.Personal);

        return Ordered(merged);
    }

    // Several stacks routinely share one label (four inventory pages all read as "Inventory"), so
    // they collapse into a single line carrying the total.
    private static void Accumulate(
        Dictionary<string, ItemPlacement> merged, string label, int quantity, StorageCategory category)
    {
        merged[label] = merged.TryGetValue(label, out var existing)
            ? existing with { Quantity = existing.Quantity + quantity }
            : new ItemPlacement(label, quantity, category);
    }

    private static IReadOnlyList<ItemPlacement> Ordered(Dictionary<string, ItemPlacement> merged)
    {
        var ordered = new List<ItemPlacement>(merged.Values);
        ordered.Sort(static (a, b) =>
        {
            var byCategory = a.Category.CompareTo(b.Category);
            return byCategory != 0 ? byCategory : string.CompareOrdinal(a.Label, b.Label);
        });
        return ordered;
    }

    private static StorageCategory CategoryOf(GameInventoryType bag) => bag switch
    {
        GameInventoryType.RetainerPage1
            or GameInventoryType.RetainerPage2
            or GameInventoryType.RetainerPage3
            or GameInventoryType.RetainerPage4
            or GameInventoryType.RetainerPage5
            or GameInventoryType.RetainerPage6
            or GameInventoryType.RetainerPage7
            or GameInventoryType.RetainerEquippedItems => StorageCategory.Retainer,
        _ => StorageCategory.Personal,
    };

    private static string ResolveItemName(uint itemId)
    {
        var sheet = Services.DataManager.GetExcelSheet<Item>();
        return sheet.TryGetRow(itemId, out var row)
            ? row.Name.ExtractText()
            : $"Item #{itemId}";
    }

    private static string BagLabel(GameInventoryType bag) => bag switch
    {
        GameInventoryType.EquippedItems         => "Equipped",
        GameInventoryType.ArmoryMainHand        => "Armory Chest (Main Hand)",
        GameInventoryType.ArmoryOffHand         => "Armory Chest (Off Hand)",
        GameInventoryType.Inventory1
            or GameInventoryType.Inventory2
            or GameInventoryType.Inventory3
            or GameInventoryType.Inventory4     => "Inventory",
        GameInventoryType.SaddleBag1
            or GameInventoryType.SaddleBag2     => "Saddlebag",
        GameInventoryType.PremiumSaddleBag1
            or GameInventoryType.PremiumSaddleBag2 => "Premium Saddlebag",
        GameInventoryType.RetainerPage1
            or GameInventoryType.RetainerPage2
            or GameInventoryType.RetainerPage3
            or GameInventoryType.RetainerPage4
            or GameInventoryType.RetainerPage5
            or GameInventoryType.RetainerPage6
            or GameInventoryType.RetainerPage7   => RetainerLabel(equipped: false),
        GameInventoryType.RetainerEquippedItems  => RetainerLabel(equipped: true),
        GameInventoryType.KeyItems               => "Key Items",
        _ => bag.ToString(),
    };

    // Only one retainer's containers are resident at a time, so any hit in a retainer
    // bag belongs to whichever retainer was last summoned.
    private static string RetainerLabel(bool equipped)
    {
        var name = ActiveRetainerName();
        if (name is null)
            return equipped ? "Retainer (Equipped)" : "Retainer Inventory";

        return equipped ? $"Retainer {name} (Equipped)" : $"Retainer {name}";
    }

    private static unsafe string? ActiveRetainerName()
    {
        var manager = CSRetainerManager.Instance();
        if (manager == null) return null;

        var retainer = manager->GetActiveRetainer();
        if (retainer == null) return null;

        var name = retainer->NameString;
        return string.IsNullOrEmpty(name) ? null : name;
    }
}
