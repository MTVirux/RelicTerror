using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RelicTerror.Data;
using RelicTerror.State;

namespace RelicTerror.UI;

internal sealed class ItemTotalsWindow : Window, IDisposable
{
    private static readonly Vector4 ColorComplete = new(0.3f,  0.85f, 0.5f,  1f);
    private static readonly Vector4 ColorPartial  = new(0.98f, 0.75f, 0.15f, 1f);
    private static readonly Vector4 ColorMissing  = new(0.95f, 0.35f, 0.35f, 1f);
    private static readonly Vector4 ColorDimmed   = new(0.45f, 0.45f, 0.45f, 1f);

    private readonly Func<IReadOnlyDictionary<(string, Job), WeaponProgress>> _getProgress;
    private readonly Func<IReadOnlyDictionary<uint, int>>                     _getItemCounts;

    internal ItemTotalsWindow(
        Func<IReadOnlyDictionary<(string, Job), WeaponProgress>> getProgress,
        Func<IReadOnlyDictionary<uint, int>> getItemCounts)
        : base("RelicTerror — Item Totals")
    {
        _getProgress   = getProgress;
        _getItemCounts = getItemCounts;

        Size          = new Vector2(460, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var remainingOnly = Plugin.Config.ShowRemainingOnly;
        if (ImGui.Checkbox("Only what I still need", ref remainingOnly))
        {
            Plugin.Config.ShowRemainingOnly = remainingOnly;
            Plugin.Config.Save();
        }

        ImGui.TextDisabled(remainingOnly
            ? "(materials for steps you haven't completed yet)"
            : "(materials for every step of every weapon in the series)");

        ImGui.Spacing();

        var totals = ItemTotals.Build(RelicDatabase.AllSeries, _getProgress(), _getItemCounts());
        foreach (var series in totals)
            DrawSeries(series, remainingOnly);
    }

    private static void DrawSeries(SeriesTotals series, bool remainingOnly)
    {
        var complete = series.WeaponsRemaining == 0;
        var header = complete
            ? $"{series.SeriesName} — ALL COMPLETE"
            : $"{series.SeriesName} — {series.WeaponsRemaining}/{series.WeaponsTotal} weapons remaining";

        if (!ImGui.CollapsingHeader($"{header}##{series.SeriesId}"))
            return;

        ImGui.Indent();

        var rows = new List<ItemTotalRow>(series.Rows.Count);
        foreach (var row in series.Rows)
        {
            if (remainingOnly && row.Remaining == 0) continue;
            rows.Add(row);
        }

        if (rows.Count == 0)
        {
            ImGui.TextColored(ColorDimmed, remainingOnly
                ? "Nothing left to gather."
                : "This series has no tracked material requirements.");
            ImGui.Unindent();
            ImGui.Spacing();
            return;
        }

        DrawTable(series.SeriesId, rows, remainingOnly);

        ImGui.Unindent();
        ImGui.Spacing();
    }

    private static void DrawTable(string seriesId, IReadOnlyList<ItemTotalRow> rows, bool remainingOnly)
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp;
        if (!ImGui.BeginTable($"##totals_{seriesId}", 4, flags))
            return;

        var numberWidth = ImGui.CalcTextSize("00000").X;
        ImGui.TableSetupColumn("Item",  ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Need",  ImGuiTableColumnFlags.WidthFixed, numberWidth);
        ImGui.TableSetupColumn("Total", ImGuiTableColumnFlags.WidthFixed, numberWidth);
        ImGui.TableSetupColumn("Have",  ImGuiTableColumnFlags.WidthFixed, numberWidth);
        ImGui.TableHeadersRow();

        foreach (var row in rows)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(row.ItemName);

            ImGui.TableSetColumnIndex(1);
            ImGui.TextColored(remainingOnly ? HeldColor(row) : ColorDimmed, row.Remaining.ToString());

            ImGui.TableSetColumnIndex(2);
            ImGui.TextColored(remainingOnly ? ColorDimmed : HeldColor(row), row.Total.ToString());

            ImGui.TableSetColumnIndex(3);
            ImGui.TextColored(HeldColor(row), row.Held.ToString());
        }

        ImGui.EndTable();
    }

    private static Vector4 HeldColor(ItemTotalRow row)
    {
        var target = row.Remaining > 0 ? row.Remaining : row.Total;
        if (target == 0 || row.Held >= target) return ColorComplete;
        return row.Held > 0 ? ColorPartial : ColorMissing;
    }

    public void Dispose() { }
}
