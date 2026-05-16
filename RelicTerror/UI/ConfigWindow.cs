using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace RelicTerror.UI;

internal sealed class ConfigWindow : IDisposable
{
    private bool _isOpen;

    internal bool IsOpen
    {
        get => _isOpen;
        set => _isOpen = value;
    }

    internal void Toggle() => _isOpen = !_isOpen;

    internal void Draw()
    {
        if (!_isOpen) return;

        ImGui.SetNextWindowSize(new Vector2(420, 220), ImGuiCond.FirstUseEver);
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

        ImGui.End();
    }

    public void Dispose() { }
}
