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
        ["Penumbrae"] = "Penumbrae",
        ["Umbrae"]    = "Umbrae",
        ["Obscurum"]  = "Obscurum",
    };

    internal static void Run()
    {
        var sheet = Services.DataManager.GetExcelSheet<Item>();
        var problems = 0;

        foreach (var series in RelicDatabase.AllSeries)
        foreach (var weapon in series.Weapons)
        foreach (var step in weapon.Steps)
        {
            if (step.CompletionItemIds is not { Count: > 0 } ids) continue;

            foreach (var id in ids)
            {
                if (!sheet.TryGetRow(id, out var row))
                {
                    Services.Log.Warning(
                        $"[CompletionItemIdAudit] {series.Id} {weapon.Job} {step.Name}: item ID {id} does not resolve.");
                    problems++;
                    continue;
                }

                if (StageNameTokens.TryGetValue(step.Name, out var token))
                {
                    var name = row.Name.ExtractText();
                    if (name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        Services.Log.Warning(
                            $"[CompletionItemIdAudit] {series.Id} {weapon.Job} {step.Name}: " +
                            $"item ID {id} resolves to \"{name}\" which does not contain \"{token}\".");
                        problems++;
                    }
                }
            }
        }

        if (problems == 0)
            Services.Log.Information("[CompletionItemIdAudit] OK — all CompletionItemIds resolved.");
        else
            Services.Log.Warning($"[CompletionItemIdAudit] {problems} issue(s) found. See warnings above.");
    }
}
