#if DEBUG
using System;
using System.Collections.Generic;
using Lumina.Excel.Sheets;

namespace RelicTerror.Data;

internal static class AchievementIdAudit
{
    // For stages whose achievement name contains a recognizable token, verify
    // that the resolved Achievement row's name contains the token. Stages
    // omitted from this map are only checked for ID resolution (no name token
    // assertion).
    private static readonly Dictionary<string, string> StageNameTokens = new()
    {
        // Anima
        ["Hyperconductive"] = "Hyper Animaniac",
        ["Reconditioned"]   = "It's Alive",
        ["Sharpened"]       = "It's Smart",
        ["Complete"]        = "It's Done",
        ["Lux"]             = "It's Really Done",
        // Resistance
        ["Resistance"]   = "Pièce de Résistance",
        ["Recollection"] = "Recollection",
        ["Augmented Law's Order"] = "Law's Order",
        ["Blade's"]      = "Blade's",
        // Manderville
        ["Manderville"]          = "Hamm",
        ["Amazing Manderville"]  = "Well-oiled",
        ["Majestic Manderville"] = "Reforged",
        ["Mandervillous"]        = "Perfect",
        // Phantom
        ["Penumbrae"] = "Phantastick",
        ["Umbrae"]    = "Umbra",
        ["Obscurum"]  = "Clare Obscurum",
        ["Occultum"]  = "Cut Above the Rest",
        // Eureka
        ["Anemos"]    = "Anemos",
        ["Elemental"] = "Elemental",
        ["Pyros"]     = "Pyros",
        ["Eureka"]    = "Eureka",
    };

    internal static AuditSection Collect()
    {
        var sheet    = Services.DataManager.GetExcelSheet<Achievement>();
        var findings = new List<AuditFinding>();
        var count    = 0;

        foreach (var series in RelicDatabase.AllSeries)
        foreach (var weapon in series.Weapons)
        foreach (var step in weapon.Steps)
        {
            if (step.AchievementId is not { } achId) continue;

            count++;
            var scope = $"{series.Id} {weapon.Job} {step.Name}";

            if (!sheet.TryGetRow(achId, out var row))
            {
                findings.Add(new AuditFinding(scope, $"achievement ID {achId} does not resolve."));
                continue;
            }

            if (!StageNameTokens.TryGetValue(step.Name, out var token)) continue;

            var name = row.Name.ExtractText();
            if (name.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                findings.Add(new AuditFinding(
                    scope, $"achievement ID {achId} resolves to \"{name}\" which does not contain \"{token}\"."));
        }

        return new AuditSection("Achievement", count, findings);
    }
}
#endif
