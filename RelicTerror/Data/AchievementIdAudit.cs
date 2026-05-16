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
        // Eureka
        ["Anemos"]    = "Anemos",
        ["Elemental"] = "Elemental",
        ["Pyros"]     = "Pyros",
        ["Eureka"]    = "Eureka",
    };

    internal static void Run()
    {
        var sheet = Services.DataManager.GetExcelSheet<Achievement>();
        var problems = 0;

        foreach (var series in RelicDatabase.AllSeries)
        foreach (var weapon in series.Weapons)
        foreach (var step in weapon.Steps)
        {
            if (step.AchievementId is not { } achId) continue;

            if (!sheet.TryGetRow(achId, out var row))
            {
                Services.Log.Warning(
                    $"[AchievementIdAudit] {series.Id} {weapon.Job} {step.Name}: " +
                    $"achievement ID {achId} does not resolve.");
                problems++;
                continue;
            }

            if (StageNameTokens.TryGetValue(step.Name, out var token))
            {
                var name = row.Name.ExtractText();
                if (name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    Services.Log.Warning(
                        $"[AchievementIdAudit] {series.Id} {weapon.Job} {step.Name}: " +
                        $"achievement ID {achId} resolves to \"{name}\" which does not contain \"{token}\".");
                    problems++;
                }
            }
        }

        if (problems == 0)
            Services.Log.Information("[AchievementIdAudit] OK — all AchievementIds resolved.");
        else
            Services.Log.Warning($"[AchievementIdAudit] {problems} issue(s) found. See warnings above.");
    }
}
