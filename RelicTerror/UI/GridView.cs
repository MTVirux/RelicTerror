using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using RelicTerror.Data;
using RelicTerror.State;

namespace RelicTerror.UI;

internal static class GridView
{
    private static readonly (string Label, Job[] Jobs)[] RoleGroups =
    [
        ("TANK",        [Job.PLD, Job.WAR, Job.DRK, Job.GNB]),
        ("HEALER",      [Job.WHM, Job.SCH, Job.AST, Job.SGE]),
        ("MELEE DPS",   [Job.MNK, Job.DRG, Job.NIN, Job.SAM, Job.RPR, Job.VPR]),
        ("PHYS RANGED", [Job.BRD, Job.MCH, Job.DNC]),
        ("CASTER",      [Job.BLM, Job.SMN, Job.RDM, Job.PCT]),
    ];

    private static readonly Vector4 ColorComplete   = new(0.3f,  0.85f, 0.5f,  1f);
    private static readonly Vector4 ColorReplica    = new(0.72f, 0.5f,  0.95f, 1f);
    private static readonly Vector4 ColorPartial    = new(0.98f, 0.75f, 0.15f, 1f);
    private static readonly Vector4 ColorNotStarted = new(0.5f,  0.5f,  0.5f,  1f);
    private static readonly Vector4 ColorNA         = new(0.35f, 0.35f, 0.35f, 1f);

    internal static void Draw(
        IReadOnlyList<RelicSeries> allSeries,
        IReadOnlyDictionary<(string SeriesId, Job Job), WeaponProgress> weapons,
        ref (string SeriesId, Job Job)? selectedCell)
    {
        if (!ImGui.BeginTable("##grid", allSeries.Count + 1, ImGuiTableFlags.RowBg))
            return;

        var useLongNames = Plugin.Config.UseLongJobNames;
        var firstColumnWidth = useLongNames ? 110f : 50f;
        ImGui.TableSetupColumn("##job", ImGuiTableColumnFlags.WidthFixed, firstColumnWidth);
        foreach (var series in allSeries)
        {
            var header = Plugin.Config.ShowExpansionColumns ? series.Expansion.ToString() : series.Name;
            ImGui.TableSetupColumn(header, ImGuiTableColumnFlags.WidthStretch);
        }

        DrawCenteredHeaders(allSeries.Count + 1);

        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));

        foreach (var (roleLabel, jobs) in RoleGroups)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            CenteredTextDisabled(roleLabel);

            foreach (var job in jobs)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                CenteredTextUnformatted(useLongNames ? JobNames.Long(job) : job.ToString());

                for (var col = 0; col < allSeries.Count; col++)
                {
                    ImGui.TableSetColumnIndex(col + 1);
                    var series = allSeries[col];
                    var key = (series.Id, job);

                    if (!series.Weapons.Any(w => w.Job == job))
                    {
                        CenteredTextColored(ColorNA, "—");
                        continue;
                    }

                    var (icon, color) = GetCellDisplay(weapons, key);
                    var isSelected = selectedCell == key;

                    ImGui.PushFont(Icons.FixedWidthFont);
                    ImGui.PushStyleColor(ImGuiCol.Text, color);
                    if (ImGui.Selectable($"{icon.ToIconString()}##{series.Id}_{job}", isSelected,
                        ImGuiSelectableFlags.None, new Vector2(0, 0)))
                        selectedCell = key;
                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                }
            }
        }

        ImGui.PopStyleVar();
        ImGui.EndTable();
    }

    private static void DrawCenteredHeaders(int columnCount)
    {
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        for (var col = 0; col < columnCount; col++)
        {
            if (!ImGui.TableSetColumnIndex(col)) continue;
            var name = ImGui.TableGetColumnName(col) ?? string.Empty;
            ImGui.PushID(col);
            var textWidth = ImGui.CalcTextSize(name).X;
            var avail = ImGui.GetContentRegionAvail().X;
            if (textWidth > 0 && textWidth < avail)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (avail - textWidth) * 0.5f);
            ImGui.TableHeader(name);
            ImGui.PopID();
        }
    }

    private static void CenteredTextUnformatted(string text)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var width = ImGui.CalcTextSize(text).X;
        if (width < avail)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (avail - width) * 0.5f);
        ImGui.TextUnformatted(text);
    }

    private static void CenteredTextDisabled(string text)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var width = ImGui.CalcTextSize(text).X;
        if (width < avail)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (avail - width) * 0.5f);
        ImGui.TextDisabled(text);
    }

    private static void CenteredTextColored(Vector4 color, string text)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var width = ImGui.CalcTextSize(text).X;
        if (width < avail)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (avail - width) * 0.5f);
        ImGui.TextColored(color, text);
    }

    private static (FontAwesomeIcon Icon, Vector4 Color) GetCellDisplay(
        IReadOnlyDictionary<(string, Job), WeaponProgress> weapons,
        (string SeriesId, Job Job) key)
    {
        if (!weapons.TryGetValue(key, out var progress))
            return (FontAwesomeIcon.Circle, ColorNotStarted);

        if (progress.RelicOwned)
            return (FontAwesomeIcon.Check, progress.ReplicaOwned ? ColorReplica : ColorComplete);

        if (progress.CompletedSteps > 0)
            return (FontAwesomeIcon.DotCircle, ColorPartial);

        return (FontAwesomeIcon.Circle, ColorNotStarted);
    }
}
