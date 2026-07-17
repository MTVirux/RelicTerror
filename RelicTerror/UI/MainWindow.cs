using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using RelicTerror.Data;
using RelicTerror.GameState;
using RelicTerror.State;

namespace RelicTerror.UI;

internal sealed class MainWindow : IDisposable
{
    private readonly Func<ulong, IReadOnlyDictionary<(string, Job), WeaponProgress>> _getProgress;
    private readonly Func<string, IReadOnlyList<JournalQuestStatus>>                 _getJournalQuestStatuses;
    private readonly Func<uint, ProgressReader.ItemLocation?>                       _findItemLocation;
    private bool _isOpen;
    private (string SeriesId, Job Job)? _selectedCell;

    internal MainWindow(
        Func<ulong, IReadOnlyDictionary<(string, Job), WeaponProgress>> getProgress,
        Func<string, IReadOnlyList<JournalQuestStatus>> getJournalQuestStatuses,
        Func<uint, ProgressReader.ItemLocation?> findItemLocation)
    {
        _getProgress             = getProgress;
        _getJournalQuestStatuses = getJournalQuestStatuses;
        _findItemLocation        = findItemLocation;
        _selectedCell            = Plugin.Config.SelectedCell;
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

        ImGui.SetNextWindowSize(new Vector2(720, 520), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("RelicTerror", ref _isOpen))
        {
            ImGui.End();
            return;
        }

        if (!Plugin.Config.HideCharacterSelector)
        {
            DrawCharacterDropdown();
            ImGui.Separator();
        }

        var charId  = Plugin.Config.SelectedCharacterId;
        var weapons = _getProgress(charId);

        var gridWidth = ImGui.GetContentRegionAvail().X * 0.42f;
        ImGui.BeginChild("##gridpanel", new Vector2(gridWidth, 0), false);
        GridView.Draw(RelicDatabase.AllSeries, weapons, ref _selectedCell);
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##detailpanel", new Vector2(0, 0), false);
        if (_selectedCell.HasValue && weapons.TryGetValue(_selectedCell.Value, out var progress))
            DetailPanel.Draw(_selectedCell.Value, progress, _getJournalQuestStatuses(_selectedCell.Value.SeriesId), _findItemLocation);
        else
            ImGui.TextDisabled("Select a cell to see details.");
        ImGui.EndChild();

        if (_selectedCell != Plugin.Config.SelectedCell)
        {
            Plugin.Config.SelectedCell = _selectedCell;
            Plugin.Config.Save();
        }

        ImGui.End();
    }

    private static void DrawCharacterDropdown()
    {
        var chars   = Plugin.Config.Characters;
        var selId   = Plugin.Config.SelectedCharacterId;
        var selName = chars.TryGetValue(selId, out var sel) ? $"{sel.Name} — {sel.World}" : "No character";

        ImGui.SetNextItemWidth(260f);
        if (ImGui.BeginCombo("##charselect", selName))
        {
            foreach (var (id, info) in chars.OrderByDescending(c => c.Value.LastSeen))
            {
                var isSelected = id == selId;
                if (ImGui.Selectable($"{info.Name} — {info.World}##{id}", isSelected))
                {
                    Plugin.Config.SelectedCharacterId = id;
                    Plugin.Config.Save();
                }
                if (isSelected) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
    }

    public void Dispose() { }
}
