using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace RelicTerror.UI;

internal sealed class ConfigWindow : IDisposable
{
    private const string ResetCurrentPopupId = "RelicTerror.ResetCurrent";
    private const string ResetAllPopupId     = "RelicTerror.ResetAll";

    private readonly Action<ResetScope> _resetFloors;
    private readonly Action _refetchAchievements;
    private bool _isOpen;

    internal ConfigWindow(Action<ResetScope> resetFloors, Action refetchAchievements)
    {
        _resetFloors = resetFloors;
        _refetchAchievements = refetchAchievements;
    }

    internal bool IsOpen
    {
        get => _isOpen;
        set => _isOpen = value;
    }

    internal void Toggle() => _isOpen = !_isOpen;

    internal void Draw()
    {
        if (!_isOpen) return;

        ImGui.SetNextWindowSize(new Vector2(420, 320), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("RelicTerror — Configuration", ref _isOpen))
        {
            ImGui.End();
            return;
        }

        var useLong = Plugin.Config.UseLongJobNames;
        if (ImGui.Checkbox("Use long class names", ref useLong))
        {
            Plugin.Config.UseLongJobNames = useLong;
            Plugin.Config.Save();
        }
        ImGui.TextDisabled(useLong ? "(e.g. Paladin, Dragoon)" : "(e.g. PLD, DRG)");

        ImGui.Spacing();

        var showExpansion = Plugin.Config.ShowExpansionColumns;
        if (ImGui.Checkbox("Show expansion instead of relic name in headers", ref showExpansion))
        {
            Plugin.Config.ShowExpansionColumns = showExpansion;
            Plugin.Config.Save();
        }
        ImGui.TextDisabled(showExpansion ? "(e.g. ARR, HW, SB)" : "(e.g. Zodiac Weapons, Anima Weapons)");

        ImGui.Spacing();

        var hideSelector = Plugin.Config.HideCharacterSelector;
        if (ImGui.Checkbox("Hide character selector", ref hideSelector))
        {
            Plugin.Config.HideCharacterSelector = hideSelector;
            Plugin.Config.Save();
        }

        ImGui.Spacing();

        var openOnLoad = Plugin.Config.OpenOnLoad;
        if (ImGui.Checkbox("Open window on plugin load", ref openOnLoad))
        {
            Plugin.Config.OpenOnLoad = openOnLoad;
            Plugin.Config.Save();
        }

        ImGui.Spacing();

        var autoFetch = Plugin.Config.AutoFetchAchievements;
        if (ImGui.Checkbox("Auto-fetch achievements on startup", ref autoFetch))
        {
            Plugin.Config.AutoFetchAchievements = autoFetch;
            Plugin.Config.Save();
        }
        ImGui.TextDisabled("Pulls relic achievement completion from the server so steps resolve\nwithout opening the in-game Achievements window.");
        if (ImGui.Button("Re-fetch now"))
            _refetchAchievements();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted("Feedback & support");
        ImGui.TextDisabled("Bug reports and suggestions are highly appreciated!");
        SupportLinks.DrawButtons();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawDangerZone();

        ImGui.End();
    }

    private void DrawDangerZone()
    {
        ImGui.TextColored(new Vector4(0.95f, 0.55f, 0.55f, 1f), "Danger zone");
        ImGui.TextDisabled("Clears persisted relic progress. The table will rebuild from current in-game state.");
        ImGui.Spacing();

        var currentChar  = TryGetCurrentCharacter(out var currentName);
        var hasAnyChars  = Plugin.Config.Characters.Count > 0;

        if (!currentChar)
            ImGui.BeginDisabled();
        if (ImGui.Button("Reset current character"))
            ImGui.OpenPopup(ResetCurrentPopupId);
        if (!currentChar)
        {
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("No logged-in character with stored progress.");
        }

        ImGui.SameLine();

        if (!hasAnyChars)
            ImGui.BeginDisabled();
        if (ImGui.Button("Reset all characters"))
            ImGui.OpenPopup(ResetAllPopupId);
        if (!hasAnyChars)
        {
            ImGui.EndDisabled();
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip("No characters with stored progress.");
        }

        DrawConfirmModal(
            ResetCurrentPopupId,
            $"This will clear persisted relic progress for {currentName}.\nThe table will rebuild from current in-game state.",
            () => _resetFloors(ResetScope.Current));

        DrawConfirmModal(
            ResetAllPopupId,
            $"This will clear persisted relic progress for all {Plugin.Config.Characters.Count} characters.\nThe table will rebuild from current in-game state.",
            () => _resetFloors(ResetScope.All));
    }

    private static void DrawConfirmModal(string popupId, string message, Action onConfirm)
    {
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        var open = true;
        if (!ImGui.BeginPopupModal(popupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextUnformatted(message);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Yes, reset", new Vector2(120, 0)))
        {
            onConfirm();
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120, 0)))
            ImGui.CloseCurrentPopup();

        ImGui.EndPopup();
    }

    private static bool TryGetCurrentCharacter(out string displayName)
    {
        displayName = "(no character)";
        if (!Services.PlayerState.IsLoaded) return false;
        var contentId = Services.PlayerState.ContentId;
        if (contentId == 0 || !Plugin.Config.Characters.TryGetValue(contentId, out var info))
            return false;
        displayName = string.IsNullOrEmpty(info.World) ? info.Name : $"{info.Name} — {info.World}";
        return true;
    }

    public void Dispose() { }
}
