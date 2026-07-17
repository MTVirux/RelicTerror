using System;
using System.Text;
using Lumina.Excel.Sheets;

namespace RelicTerror.Data;

internal static class QuestIdAudit
{
    internal static void Run()
    {
        var sheet = Services.DataManager.GetExcelSheet<Quest>();
        var problems = 0;

        foreach (var series in RelicDatabase.AllSeries)
        {
            foreach (var q in series.JournalQuests)
            {
                if (!sheet.TryGetRow(q.QuestId, out var row))
                {
                    Services.Log.Warning(
                        $"[QuestIdAudit] {series.Id} {q.DisplayName}: quest ID {q.QuestId} does not resolve.");
                    problems++;
                    continue;
                }

                var sheetName = StripIconGlyphs(row.Name.ExtractText());
                if (!sheetName.Equals(q.DisplayName, StringComparison.Ordinal))
                {
                    Services.Log.Warning(
                        $"[QuestIdAudit] {series.Id} {q.DisplayName}: quest ID {q.QuestId} resolves to \"{sheetName}\".");
                    problems++;
                }

                if (row.IsRepeatable != q.Repeatable)
                {
                    Services.Log.Warning(
                        $"[QuestIdAudit] {series.Id} {q.DisplayName}: Repeatable={q.Repeatable} but sheet says {row.IsRepeatable}.");
                    problems++;
                }
            }

            foreach (var weapon in series.Weapons)
            foreach (var step in weapon.Steps)
            {
                if (step.CompletionQuestId is not { } questId) continue;

                if (!sheet.TryGetRow(questId, out _))
                {
                    Services.Log.Warning(
                        $"[QuestIdAudit] {series.Id} {weapon.Job} {step.Name}: completion quest ID {questId} does not resolve.");
                    problems++;
                }
            }
        }

        if (problems == 0)
            Services.Log.Information("[QuestIdAudit] OK — all quest IDs resolved.");
        else
            Services.Log.Warning($"[QuestIdAudit] {problems} issue(s) found. See warnings above.");
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
