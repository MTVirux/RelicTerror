using System.Collections.Generic;
using System.Linq;

namespace RelicTerror.Data;

// Scope locates the identifier in the relic tables (series, job, step).
internal sealed record AuditFinding(string Scope, string Message);

// Sheet is the Excel sheet the section's identifiers were resolved against.
internal sealed record AuditSection(string Sheet, int Checked, IReadOnlyList<AuditFinding> Findings);

internal sealed record AuditReport(IReadOnlyList<AuditSection> Sections, string GameVersion)
{
    internal int Checked  => Sections.Sum(s => s.Checked);
    internal int Problems => Sections.Sum(s => s.Findings.Count);
}

// Relic identifiers are hand-authored, so a patch can silently invalidate them. Each sheet
// audit resolves what it can and returns findings rather than logging them, so the same pass
// serves both the load-time log line and the on-demand audit window.
internal static class DataAudit
{
    internal static AuditReport Run()
    {
        var report = new AuditReport(
            [
                CompletionItemIdAudit.Collect(),
                AchievementIdAudit.Collect(),
                QuestIdAudit.Collect(),
            ],
            GameVersion());

        WriteLog(report);
        return report;
    }

    private static void WriteLog(AuditReport report)
    {
        foreach (var section in report.Sections)
        foreach (var finding in section.Findings)
            Services.Log.Warning($"[DataAudit/{section.Sheet}] {finding.Scope}: {finding.Message}");

        if (report.Problems == 0)
            Services.Log.Information(
                $"[DataAudit] OK - {report.Checked} identifier(s) verified against game data {report.GameVersion}.");
        else
            Services.Log.Warning(
                $"[DataAudit] {report.Problems} issue(s) across {report.Checked} identifier(s) " +
                $"on game data {report.GameVersion}. Run \"/rt audit\" for details.");
    }

    private static string GameVersion()
    {
        try
        {
            return Services.DataManager.GameData.Repositories["ffxiv"].Version;
        }
        catch
        {
            return "unknown";
        }
    }
}
