using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace RelicTerror.UI;

internal sealed class ConfigWindow : IDisposable
{
    private const string ResetCurrentPopupId = "RelicTerror.ResetCurrent";
    private const string ResetAllPopupId     = "RelicTerror.ResetAll";

    private static readonly Vector4 DangerColor = new(0.95f, 0.55f, 0.55f, 1f);

    private readonly Action<ResetScope> _resetFloors;
    private readonly Action _refetchAchievements;
    private readonly Func<bool> _allaganToolsConnected;
    private bool _isOpen;

    internal ConfigWindow(
        Action<ResetScope> resetFloors,
        Action refetchAchievements,
        Func<bool> allaganToolsConnected)
    {
        _resetFloors = resetFloors;
        _refetchAchievements = refetchAchievements;
        _allaganToolsConnected = allaganToolsConnected;
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

        if (ImGui.BeginTabBar("RelicTerror.ConfigTabs"))
        {
            if (ImGui.BeginTabItem("Status"))
            {
                DrawStatusTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Look & feel"))
            {
                DrawLookAndFeelTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.End();
    }

    private void DrawStatusTab()
    {
        ImGui.SetNextItemOpen(true, ImGuiCond.FirstUseEver);
        if (ImGui.CollapsingHeader("Item locations"))
            DrawInventorySourceStatus();

        if (ImGui.CollapsingHeader("Achievements"))
            DrawAchievements();

        if (ImGui.CollapsingHeader("Feedback & support"))
            DrawFeedback();

        ImGui.PushStyleColor(ImGuiCol.Text, DangerColor);
        var dangerOpen = ImGui.CollapsingHeader("Danger zone");
        ImGui.PopStyleColor();
        if (dangerOpen)
            DrawDangerZone();
    }

    private static void DrawLookAndFeelTab()
    {
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
    }

    private void DrawInventorySourceStatus()
    {
        foreach (var integration in Integrations.All(_allaganToolsConnected()))
        {
            ImGui.TextColored(integration.Color, $"{integration.Name} {integration.State}");
            ImGui.TextDisabled(integration.Detail);
        }
    }

    private void DrawAchievements()
    {
        ImGui.TextDisabled("Relic achievement completion is pulled from the server once per character\nand cached, so steps resolve without opening the in-game Achievements window.");
        if (ImGui.Button("Re-fetch now"))
            _refetchAchievements();
        ImGui.SameLine();
        ImGui.TextDisabled("(updates the cache from the server)");
    }

    private static void DrawFeedback()
    {
        ImGui.TextDisabled("Bug reports and suggestions are highly appreciated!");
        SupportLinks.DrawButtons();
    }

    private void DrawDangerZone()
    {
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
