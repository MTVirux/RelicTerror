using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RelicTerror.Data;

namespace RelicTerror.UI;

internal sealed class AuditWindow : Window, IDisposable
{
    private static readonly Vector4 ColorOk      = new(0.3f,  0.85f, 0.5f,  1f);
    private static readonly Vector4 ColorProblem = new(0.95f, 0.35f, 0.35f, 1f);
    private static readonly Vector4 ColorDimmed  = new(0.45f, 0.45f, 0.45f, 1f);

    private readonly Func<AuditReport> _rerun;
    private AuditReport _report;

    internal AuditWindow(AuditReport report, Func<AuditReport> rerun)
        : base("RelicTerror - Data Audit")
    {
        _report = report;
        _rerun  = rerun;

        Size          = new Vector2(640, 480);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    internal void Rerun() => _report = _rerun();

    public override void Draw()
    {
        if (ImGui.Button("Re-run audit"))
            Rerun();

        ImGui.SameLine();
        ImGui.TextDisabled($"game data {_report.GameVersion}");

        ImGui.Spacing();

        if (_report.Problems == 0)
            ImGui.TextColored(ColorOk, $"OK - all {_report.Checked} tracked identifiers resolve.");
        else
            ImGui.TextColored(ColorProblem, $"{_report.Problems} issue(s) across {_report.Checked} tracked identifiers.");

        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 34f);
        ImGui.TextDisabled(
            "Relic item, achievement and quest IDs are hand-authored. Anything listed here no longer " +
            "matches the game's own sheets and needs re-checking against the current patch.");
        ImGui.PopTextWrapPos();

        ImGui.Spacing();

        foreach (var section in _report.Sections)
            DrawSection(section);
    }

    private static void DrawSection(AuditSection section)
    {
        var failed = section.Findings.Count;
        var header = failed == 0
            ? $"{section.Sheet} sheet - {section.Checked} checked, all OK"
            : $"{section.Sheet} sheet - {failed} of {section.Checked} failed";

        var flags = failed == 0 ? ImGuiTreeNodeFlags.None : ImGuiTreeNodeFlags.DefaultOpen;
        if (!ImGui.CollapsingHeader($"{header}##audit_{section.Sheet}", flags))
            return;

        ImGui.Indent();

        if (failed == 0)
            ImGui.TextColored(ColorDimmed, "Nothing to report.");
        else
            DrawFindings(section);

        ImGui.Unindent();
        ImGui.Spacing();
    }

    private static void DrawFindings(AuditSection section)
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp;
        if (!ImGui.BeginTable($"##audit_{section.Sheet}_rows", 2, flags))
            return;

        ImGui.TableSetupColumn("Where",   ImGuiTableColumnFlags.WidthStretch, 0.35f);
        ImGui.TableSetupColumn("Problem", ImGuiTableColumnFlags.WidthStretch, 0.65f);
        ImGui.TableHeadersRow();

        foreach (var finding in section.Findings)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(finding.Scope);

            ImGui.TableSetColumnIndex(1);
            ImGui.TextWrapped(finding.Message);
        }

        ImGui.EndTable();
    }

    public void Dispose() { }
}
