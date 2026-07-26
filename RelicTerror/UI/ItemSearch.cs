using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using CSItemFinder = FFXIVClientStructs.FFXIV.Client.UI.Misc.ItemFinderModule;

namespace RelicTerror.UI;

/// <summary>
/// Wires detail-panel rows up to the game's own Item Search (/isearch), which highlights
/// where an item sits across inventory, Armoury Chest, saddlebags, and retainers.
/// </summary>
internal static class ItemSearch
{
    private const string HintText = "Click to search for this item";

    internal static unsafe void Open(uint itemId)
    {
        if (itemId == 0) return;

        var module = CSItemFinder.Instance();
        if (module is null) return;

        module->SearchForItem(itemId);
    }

    /// <summary>
    /// Makes the item just drawn trigger an Item Search on click. A zero
    /// <paramref name="itemId"/> leaves the row inert - no cursor change, no click handling.
    /// </summary>
    /// <returns>Whether the row is hovered, so callers can draw their own tooltip.</returns>
    internal static bool Row(uint itemId)
    {
        if (!ImGui.IsItemHovered()) return false;
        if (itemId == 0) return true;

        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            Open(itemId);

        return true;
    }

    internal static void Hint()
    {
        ImGui.TextDisabled(HintText);
    }

    internal static void HintTooltip()
    {
        ImGui.BeginTooltip();
        Hint();
        ImGui.EndTooltip();
    }

    internal static uint FirstId(IReadOnlyList<uint>? itemIds) =>
        itemIds is { Count: > 0 } ids ? ids[0] : 0;
}
