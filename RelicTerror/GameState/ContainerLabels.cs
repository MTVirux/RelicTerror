using CSRetainerManager = FFXIVClientStructs.FFXIV.Client.Game.RetainerManager;

namespace RelicTerror.GameState;

/// <summary>
/// Ordering bucket for a placement, so tooltips list what is on the character before what needs
/// a trip to fetch.
/// </summary>
internal enum StorageCategory
{
    Personal    = 0,
    Retainer    = 1,
    FreeCompany = 2,
    Housing     = 3,
}

/// <summary>
/// Container ids exactly as Allagan Tools reports them (its InventoryType enum, row index 20).
/// Mirrored here rather than referenced so RelicTerror takes no assembly dependency on Allagan
/// Tools - an id this table does not recognise still renders, just with a numeric label.
/// </summary>
internal static class ContainerLabels
{
    // Another player's gear seen through the Examine window. Never the local character's, so it
    // must not reach counts even though it can appear in a tracked inventory.
    private const uint ExamineContainer = 2009;

    internal const uint ArmoireContainer       = 2500;
    internal const uint GlamourChestContainer  = 2501;

    internal static bool IsCountable(uint container) => container != ExamineContainer;

    internal static (string Label, StorageCategory Category) Describe(uint container, ulong ownerId) =>
        container switch
        {
            <= 3                     => ("Inventory",                    StorageCategory.Personal),
            1000 or 1001             => ("Equipped",                     StorageCategory.Personal),
            2000                     => ("Currency",                     StorageCategory.Personal),
            2001                     => ("Crystals",                     StorageCategory.Personal),
            2003                     => ("Mail",                         StorageCategory.Personal),
            2004                     => ("Key Items",                    StorageCategory.Personal),
            2005                     => ("Hand-in",                      StorageCategory.Personal),
            2007                     => ("Damaged Gear",                 StorageCategory.Personal),
            ArmoireContainer         => ("Armoire",                      StorageCategory.Personal),
            GlamourChestContainer    => ("Glamour Dresser",              StorageCategory.Personal),
            2502                     => ("Free Company Credits",         StorageCategory.FreeCompany),
            3200                     => ("Armoury Chest (Off Hand)",     StorageCategory.Personal),
            3201                     => ("Armoury Chest (Head)",         StorageCategory.Personal),
            3202                     => ("Armoury Chest (Body)",         StorageCategory.Personal),
            3203                     => ("Armoury Chest (Hands)",        StorageCategory.Personal),
            3204                     => ("Armoury Chest (Waist)",        StorageCategory.Personal),
            3205                     => ("Armoury Chest (Legs)",         StorageCategory.Personal),
            3206                     => ("Armoury Chest (Feet)",         StorageCategory.Personal),
            3207                     => ("Armoury Chest (Earrings)",     StorageCategory.Personal),
            3208                     => ("Armoury Chest (Necklace)",     StorageCategory.Personal),
            3209                     => ("Armoury Chest (Bracelets)",    StorageCategory.Personal),
            3300                     => ("Armoury Chest (Rings)",        StorageCategory.Personal),
            3400                     => ("Armoury Chest (Soul Crystal)", StorageCategory.Personal),
            3500                     => ("Armoury Chest (Main Hand)",    StorageCategory.Personal),
            4000 or 4001             => ("Saddlebag",                    StorageCategory.Personal),
            4100 or 4101             => ("Premium Saddlebag",            StorageCategory.Personal),
            >= 10000 and <= 10006    => (RetainerLabel(ownerId, null),        StorageCategory.Retainer),
            11000                    => (RetainerLabel(ownerId, "equipped"),  StorageCategory.Retainer),
            12000                    => (RetainerLabel(ownerId, "gil"),       StorageCategory.Retainer),
            12001                    => (RetainerLabel(ownerId, "crystals"),  StorageCategory.Retainer),
            12002                    => (RetainerLabel(ownerId, "on sale"),   StorageCategory.Retainer),
            >= 20000 and <= 20010    => ("Free Company Chest",           StorageCategory.FreeCompany),
            22000 or 22001           => ("Free Company Chest",           StorageCategory.FreeCompany),
            25000 or 25002           => ("Housing (fixtures)",           StorageCategory.Housing),
            25001 or 25200
                or (>= 25003 and <= 25014) => ("Housing (placed)",       StorageCategory.Housing),
            27000 or 27200
                or (>= 27001 and <= 27011) => ("Housing Storeroom",      StorageCategory.Housing),
            _                        => ($"Container {container}",       StorageCategory.Personal),
        };

    private static string RetainerLabel(ulong retainerId, string? suffix)
    {
        var name = ResolveRetainerName(retainerId) ?? "Retainer";
        return suffix is null ? name : $"{name} ({suffix})";
    }

    // The retainer roster is resident once the list has loaded this session, which does not
    // require summoning any of them. An id that is not on it falls back to a generic label.
    private static unsafe string? ResolveRetainerName(ulong retainerId)
    {
        if (retainerId == 0) return null;

        var manager = CSRetainerManager.Instance();
        if (manager == null) return null;

        foreach (ref var retainer in manager->Retainers)
        {
            if (retainer.RetainerId != retainerId) continue;

            var name = retainer.NameString;
            return string.IsNullOrEmpty(name) ? null : name;
        }

        return null;
    }
}
