using System.Collections.Generic;
using Dalamud.Game.Inventory;
using Lumina.Excel.Sheets;
using CSAchievement  = FFXIVClientStructs.FFXIV.Client.Game.UI.Achievement;
using CSMirage       = FFXIVClientStructs.FFXIV.Client.Game.MirageManager;
using CSQuestManager = FFXIVClientStructs.FFXIV.Client.Game.QuestManager;
using CSUIState      = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState;

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

    internal unsafe bool IsAchievementComplete(uint achievementId)
    {
        var ach = CSAchievement.Instance();
        return ach != null && ach->IsComplete((int)achievementId);
    }

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

    internal sealed record ItemLocation(string ItemName, string? BagLabel);

    internal ItemLocation? FindItemLocation(uint baseItemId)
    {
        if (baseItemId == 0) return null;

        var name = ResolveItemName(baseItemId);

        foreach (var bag in LookupBags)
        {
            foreach (var item in Services.GameInventory.GetInventoryItems(bag))
            {
                if (item.BaseItemId == baseItemId)
                    return new ItemLocation(name, BagLabel(bag));
            }
        }

        if (ReadGlamourDresserItemIds().Contains(baseItemId))
            return new ItemLocation(name, "Glamour Dresser");

        if (ReadArmoireItemIds().Contains(baseItemId))
            return new ItemLocation(name, "Armoire");

        return new ItemLocation(name, null);
    }

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
            or GameInventoryType.RetainerPage7   => "Retainer Inventory",
        GameInventoryType.RetainerEquippedItems  => "Retainer (Equipped)",
        GameInventoryType.KeyItems               => "Key Items",
        _ => bag.ToString(),
    };
}
