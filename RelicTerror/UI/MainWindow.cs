using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using RelicTerror.Data;
using RelicTerror.GameState;
using RelicTerror.State;

namespace RelicTerror.UI;

internal sealed class MainWindow : Window, IDisposable
{
    private readonly Func<ulong, IReadOnlyDictionary<(string, Job), WeaponProgress>> _getProgress;
    private readonly Func<string, IReadOnlyList<JournalQuestStatus>>                 _getJournalQuestStatuses;
    private readonly Func<LocationLookup>                                          _getLocationLookup;
    private readonly Func<(string SeriesId, Job Job), bool>                        _isResolving;
    private readonly Func<bool>                                                   _allaganToolsConnected;
    private readonly TitleBarButton                                               _integrationsButton;
    private (string SeriesId, Job Job)? _selectedCell;

    internal MainWindow(
        Func<ulong, IReadOnlyDictionary<(string, Job), WeaponProgress>> getProgress,
        Func<string, IReadOnlyList<JournalQuestStatus>> getJournalQuestStatuses,
        Func<LocationLookup> getLocationLookup,
        Func<(string SeriesId, Job Job), bool> isResolving,
        Func<bool> allaganToolsConnected,
        Action openConfig,
        Action openItemTotals)
        : base("RelicTerror")
    {
        _getProgress             = getProgress;
        _getJournalQuestStatuses = getJournalQuestStatuses;
        _getLocationLookup       = getLocationLookup;
        _isResolving             = isResolving;
        _allaganToolsConnected   = allaganToolsConnected;
        _selectedCell            = Plugin.Config.SelectedCell;

        Size          = new Vector2(720, 520);
        SizeCondition = ImGuiCond.FirstUseEver;

        _integrationsButton = IntegrationsButton.Build(allaganToolsConnected);
        TitleBarButtons.Add(_integrationsButton);

        TitleBarButtons.Add(new TitleBarButton
        {
            Icon        = FontAwesomeIcon.Cog,
            IconOffset  = new Vector2(2, 2),
            Click       = m => { if (m == ImGuiMouseButton.Left) openConfig(); },
            ShowTooltip = () => ImGui.SetTooltip("Open settings"),
        });

        TitleBarButtons.Add(new TitleBarButton
        {
            Icon        = FontAwesomeIcon.ListUl,
            IconOffset  = new Vector2(2, 2),
            Click       = m => { if (m == ImGuiMouseButton.Left) openItemTotals(); },
            ShowTooltip = () => ImGui.SetTooltip("Item totals"),
        });
    }

    public override void PreDraw() =>
        IntegrationsButton.Refresh(_integrationsButton, _allaganToolsConnected());

    public override void Draw()
    {
        if (!Plugin.Config.HideCharacterSelector)
        {
            DrawCharacterDropdown();
            ImGui.Separator();
        }

        var charId  = Plugin.Config.SelectedCharacterId;
        var weapons = _getProgress(charId);

        var gridWidth = ImGui.GetContentRegionAvail().X * 0.42f;
        ImGui.BeginChild("##gridpanel", new Vector2(gridWidth, 0), false);
        GridView.Draw(RelicDatabase.AllSeries, weapons, _isResolving, ref _selectedCell);
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##detailpanel", new Vector2(0, 0), false);
        if (_selectedCell.HasValue && weapons.TryGetValue(_selectedCell.Value, out var progress))
            DetailPanel.Draw(_selectedCell.Value, progress, _getJournalQuestStatuses(_selectedCell.Value.SeriesId), _getLocationLookup());
        else
            ImGui.TextDisabled("Select a cell to see details.");
        ImGui.EndChild();

        if (_selectedCell != Plugin.Config.SelectedCell)
        {
            Plugin.Config.SelectedCell = _selectedCell;
            Plugin.Config.Save();
        }
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
