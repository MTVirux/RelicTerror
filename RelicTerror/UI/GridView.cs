using System;
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

    private static readonly int BodyRowCount = RoleGroups.Length + RoleGroups.Sum(g => g.Jobs.Length);

    private static readonly Vector4 ColorComplete   = new(0.3f,  0.85f, 0.5f,  1f);
    private static readonly Vector4 ColorReplica    = new(0.72f, 0.5f,  0.95f, 1f);
    private static readonly Vector4 ColorPartial    = new(0.98f, 0.75f, 0.15f, 1f);
    private static readonly Vector4 ColorNotStarted = new(0.5f,  0.5f,  0.5f,  1f);
    private static readonly Vector4 ColorNA         = new(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Vector4 ColorLoading    = new(0.4f,  0.7f,  0.95f, 1f);

    private static readonly FontAwesomeIcon[] LoadingFrames =
    [
        FontAwesomeIcon.HourglassStart,
        FontAwesomeIcon.HourglassHalf,
        FontAwesomeIcon.HourglassEnd,
    ];

    internal static void Draw(
        IReadOnlyList<RelicSeries> allSeries,
        IReadOnlyDictionary<(string SeriesId, Job Job), WeaponProgress> weapons,
        Func<(string SeriesId, Job Job), bool> isResolving,
        ref (string SeriesId, Job Job)? selectedCell)
    {
        // Rows stretch to divide up whatever vertical space the window leaves, so the grid always
        // ends flush with the bottom instead of trailing empty space.
        var cellPaddingY = ImGui.GetStyle().CellPadding.Y;
        var headerHeight = ImGui.GetTextLineHeight() + (cellPaddingY * 2f);
        var available    = ImGui.GetContentRegionAvail().Y;
        var exactRow     = MathF.Max(headerHeight, (available - headerHeight) / BodyRowCount);
        var rowIndex     = 0;

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
            var cellHeight = BeginRow(ref rowIndex, exactRow, cellPaddingY);
            ImGui.TableSetColumnIndex(0);
            CenteredTextDisabled(roleLabel, cellHeight);

            foreach (var job in jobs)
            {
                cellHeight = BeginRow(ref rowIndex, exactRow, cellPaddingY);
                ImGui.TableSetColumnIndex(0);
                CenteredTextUnformatted(useLongNames ? JobNames.Long(job) : job.ToString(), cellHeight);

                for (var col = 0; col < allSeries.Count; col++)
                {
                    ImGui.TableSetColumnIndex(col + 1);
                    var series = allSeries[col];
                    var key = (series.Id, job);

                    if (!series.Weapons.Any(w => w.Job == job))
                    {
                        CenteredTextColored(ColorNA, "—", cellHeight);
                        continue;
                    }

                    var (icon, color, loading) = GetCellDisplay(weapons, key, isResolving);
                    var isSelected = selectedCell == key;

                    ImGui.PushFont(Icons.FixedWidthFont);
                    ImGui.PushStyleColor(ImGuiCol.Text, color);
                    if (ImGui.Selectable($"{icon.ToIconString()}##{series.Id}_{job}", isSelected,
                        ImGuiSelectableFlags.None, new Vector2(0, cellHeight)))
                        selectedCell = key;
                    ImGui.PopStyleColor();
                    ImGui.PopFont();

                    if (loading && ImGui.IsItemHovered())
                        ImGui.SetTooltip("Loading status - waiting on achievement data from the server.");
                }
            }
        }

        ImGui.PopStyleVar();
        ImGui.EndTable();
    }

    // Row bounds come from rounding running totals rather than each row's own height, so the
    // leftover fraction is spread over the grid: consecutive rows differ by at most a pixel and
    // the bottom edge follows the window continuously instead of in row-sized steps.
    private static float BeginRow(ref int rowIndex, float exactRow, float cellPaddingY)
    {
        var top    = MathF.Floor(rowIndex * exactRow);
        var bottom = MathF.Floor(++rowIndex * exactRow);
        var height = bottom - top;
        ImGui.TableNextRow(ImGuiTableRowFlags.None, height);
        return height - (cellPaddingY * 2f);
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

    private static void CenteredTextUnformatted(string text, float cellHeight)
    {
        CenterCursor(text, cellHeight);
        ImGui.TextUnformatted(text);
    }

    private static void CenteredTextDisabled(string text, float cellHeight)
    {
        CenterCursor(text, cellHeight);
        ImGui.TextDisabled(text);
    }

    private static void CenteredTextColored(Vector4 color, string text, float cellHeight)
    {
        CenterCursor(text, cellHeight);
        ImGui.TextColored(color, text);
    }

    private static void CenterCursor(string text, float cellHeight)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var size  = ImGui.CalcTextSize(text);
        if (size.X < avail)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((avail - size.X) * 0.5f));
        if (size.Y < cellHeight)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((cellHeight - size.Y) * 0.5f));
    }

    // Known progress always wins over the loading state - a partially complete weapon shows
    // its real standing rather than regressing to a spinner while later steps resolve.
    private static (FontAwesomeIcon Icon, Vector4 Color, bool Loading) GetCellDisplay(
        IReadOnlyDictionary<(string, Job), WeaponProgress> weapons,
        (string SeriesId, Job Job) key,
        Func<(string SeriesId, Job Job), bool> isResolving)
    {
        if (weapons.TryGetValue(key, out var progress))
        {
            if (progress.RelicOwned)
                return (FontAwesomeIcon.Check, progress.ReplicaOwned ? ColorReplica : ColorComplete, false);

            if (progress.CompletedSteps > 0)
                return (FontAwesomeIcon.DotCircle, ColorPartial, false);
        }

        return isResolving(key)
            ? (LoadingFrames[(int)(ImGui.GetTime() * 3d) % LoadingFrames.Length], ColorLoading, true)
            : (FontAwesomeIcon.Circle, ColorNotStarted, false);
    }
}
