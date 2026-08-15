#if DEBUG
using System;
using System.Collections.Generic;
using Lumina.Excel.Sheets;

namespace RelicTerror.Data;

internal static class CompletionItemIdAudit
{
    // Map each step name we recognize to a substring expected in the item's
    // game name. Stages whose item names do NOT contain the stage name (e.g.,
    // Zodiac "Zenith" weapons aren't named "Zenith") are omitted — the audit
    // only verifies the ID resolves to a real Item row in that case.
    private static readonly Dictionary<string, string> StageNameTokens = new()
    {
        ["Hyperconductive"] = "Hyperconductive",
        ["Sharpened"]       = "Sharpened",
        ["Lux"]             = "Lux",
        // Resistance
        ["Recollection"]    = "Recollection",
        ["Law's Order"]     = "Law's Order",
        ["Augmented Law's Order"] = "Augmented Law's Order",
        ["Blade's"]         = "Blade's",
        // Manderville
        ["Manderville"]       = "Manderville",
        ["Amazing Manderville"] = "Amazing Manderville",
        ["Majestic Manderville"] = "Majestic Manderville",
        ["Mandervillous"]     = "Mandervillous",
        // Phantom
        ["Penumbrae"]  = "Penumbrae",
        ["Umbrae"]     = "Umbrae",
        ["Obscurum"]   = "Obscurum",
        ["Eclipticum"] = "Eclipticum",
        ["Occultum"]   = "Occultum",
    };

    internal static AuditSection Collect()
    {
        var sheet    = Services.DataManager.GetExcelSheet<Item>();
        var findings = new List<AuditFinding>();
        var count    = 0;

        foreach (var series in RelicDatabase.AllSeries)
        foreach (var weapon in series.Weapons)
        foreach (var step in weapon.Steps)
        {
            if (step.CompletionItemIds is not { Count: > 0 } ids) continue;

            foreach (var id in ids)
            {
                count++;
                var scope = $"{series.Id} {weapon.Job} {step.Name}";

                if (!sheet.TryGetRow(id, out var row))
                {
                    findings.Add(new AuditFinding(scope, $"item ID {id} does not resolve."));
                    continue;
                }

                if (!StageNameTokens.TryGetValue(step.Name, out var token)) continue;

                var name = row.Name.ExtractText();
                if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                    findings.Add(new AuditFinding(
                        scope, $"item ID {id} resolves to \"{name}\" which does not contain \"{token}\"."));
            }
        }

        return new AuditSection("Item", count, findings);
    }
}
#endif
