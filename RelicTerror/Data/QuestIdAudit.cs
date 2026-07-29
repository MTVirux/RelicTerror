using System;
using System.Collections.Generic;
using System.Text;
using Lumina.Excel.Sheets;

namespace RelicTerror.Data;

internal static class QuestIdAudit
{
    internal static AuditSection Collect()
    {
        var sheet    = Services.DataManager.GetExcelSheet<Quest>();
        var findings = new List<AuditFinding>();
        var count    = 0;

        foreach (var series in RelicDatabase.AllSeries)
        {
            foreach (var q in series.JournalQuests)
            {
                count++;
                var scope = $"{series.Id} {q.DisplayName}";

                if (!sheet.TryGetRow(q.QuestId, out var row))
                {
                    findings.Add(new AuditFinding(scope, $"quest ID {q.QuestId} does not resolve."));
                    continue;
                }

                var sheetName = StripIconGlyphs(row.Name.ExtractText());
                if (!sheetName.Equals(q.DisplayName, StringComparison.Ordinal))
                    findings.Add(new AuditFinding(
                        scope, $"quest ID {q.QuestId} resolves to \"{sheetName}\"."));

                if (row.IsRepeatable != q.Repeatable)
                    findings.Add(new AuditFinding(
                        scope, $"Repeatable={q.Repeatable} but sheet says {row.IsRepeatable}."));
            }

            foreach (var weapon in series.Weapons)
            foreach (var step in weapon.Steps)
            {
                if (step.CompletionQuestId is not { } questId) continue;

                count++;
                if (!sheet.TryGetRow(questId, out _))
                    findings.Add(new AuditFinding(
                        $"{series.Id} {weapon.Job} {step.Name}",
                        $"completion quest ID {questId} does not resolve."));
            }
        }

        return new AuditSection("Quest", count, findings);
    }

    // Repeatable quests carry a leading private-use-area icon glyph (U+E000-U+F8FF)
    // in the sheet name; strip it before comparing.
    private static string StripIconGlyphs(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (c < 0xE000 || c > 0xF8FF)
                sb.Append(c);
        }
        return sb.ToString().Trim();
    }
}
