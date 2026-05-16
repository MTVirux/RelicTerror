using System;
using System.Collections.Generic;
using System.Linq;
using RelicTerror.Data;

namespace RelicTerror.Data.Series;

internal static class EurekaSeries
{
    // Anemos stage
    private const uint ProteanCrystal = 21801;
    private const uint PazuzusFeather = 21802;

    // Pagos / Elemental stage
    private const uint FrostedProteanCrystal = 23309;
    private const uint PagosCrystal          = 22976;
    private const uint LouhisIce             = 22975;

    // Pyros stage
    private const uint PyrosCrystal       = 24124;
    private const uint PenthesileaasFlame = 24123;

    // Hydatos / Eureka stage
    private const uint HydatosCrystal   = 24807;
    private const uint CrystallineScale = 24806;

    // Physeos stage
    private const uint EurekaFragment = 24808;

    // Weapon item order is consistent across every Eureka stage (Antiquated, Anemos,
    // Elemental, Pyros, Hydatos, Physeos). PLD shield sits at offset 15 within each block.
    private static readonly Job[] WeaponOrder =
    [
        Job.PLD, Job.MNK, Job.WAR, Job.DRG, Job.BRD, Job.NIN, Job.DRK, Job.MCH,
        Job.WHM, Job.BLM, Job.SMN, Job.SCH, Job.AST, Job.SAM, Job.RDM,
    ];

    // Achievement job order for Eureka stages — verified empirically against the
    // in-game Achievement sheet, 2026-05-17. Shared across Anemos (2030),
    // Elemental (2082), Pyros (2143), and Eureka/Hydatos (2212).
    private static readonly Job[] AchievementJobOrder =
    [
        Job.PLD, Job.WAR, Job.DRK, Job.DRG, Job.MNK, Job.NIN, Job.SAM, Job.BRD,
        Job.MCH, Job.BLM, Job.SMN, Job.RDM, Job.WHM, Job.SCH, Job.AST,
    ];

    private const uint AntiquatedBase = 17817;
    private const uint AnemosBase     = 21990;
    private const uint ElementalBase  = 22957;
    private const uint PyrosBase      = 24071;
    private const uint EurekaBase     = 24691;
    private const uint PhyseosBase    = 24707;

    private const uint AnemosBaseAch    = 2030; // "I've Got It: <weapon> Anemos"
    private const uint ElementalBaseAch = 2082; // "I've Got It: Elemental <type>"
    private const uint PyrosBaseAch     = 2143; // "I've Got It: Pyros <type>"
    private const uint EurekaBaseAch    = 2212; // "I've Got It: <weapon> Eureka"

    private static uint? AchId(uint baseId, Job job)
    {
        var idx = Array.IndexOf(AchievementJobOrder, job);
        return idx < 0 ? null : (uint?)(baseId + (uint)idx);
    }

    private static IReadOnlyList<uint> StageItems(Job job, uint baseId)
    {
        var off = (uint)Array.IndexOf(WeaponOrder, job);
        return job == Job.PLD
            ? [baseId, baseId + 15]  // sword + shield
            : [baseId + off];
    }

    private static IReadOnlyList<RelicStep> BuildSteps(Job job) =>
    [
        // Antiquated → starting weapon, obtained from level 70 job quest.
        // No in-game achievement exists for this stage (verified 2026-05-17 against Achievement.csv).
        // Identification falls back to CompletionItemIds.
        new("Antiquated",
            AchievementId: null,
            CompletionItemIds: StageItems(job, AntiquatedBase),
            Requirements: []),
        new("Anemos",
            AchievementId: AchId(AnemosBaseAch, job),
            CompletionItemIds: StageItems(job, AnemosBase),
            Requirements:
            [
                new(ProteanCrystal, "Protean Crystal", 1300),
                new(PazuzusFeather, "Pazuzu's Feather",    3),
            ]),
        new("Elemental",
            AchievementId: AchId(ElementalBaseAch, job),
            CompletionItemIds: StageItems(job, ElementalBase),
            Requirements:
            [
                new(FrostedProteanCrystal, "Frosted Protean Crystal", 31),
                new(PagosCrystal,          "Pagos Crystal",          500),
                new(LouhisIce,             "Louhi's Ice",              5),
            ]),
        new("Pyros",
            AchievementId: AchId(PyrosBaseAch, job),
            CompletionItemIds: StageItems(job, PyrosBase),
            Requirements:
            [
                new(PyrosCrystal,       "Pyros Crystal",       650),
                new(PenthesileaasFlame, "Penthesilea's Flame",   5),
            ]),
        new("Eureka",
            AchievementId: AchId(EurekaBaseAch, job),
            CompletionItemIds: StageItems(job, EurekaBase),
            Requirements:
            [
                new(HydatosCrystal,   "Hydatos Crystal",  350),
                new(CrystallineScale, "Crystalline Scale",  5),
            ]),
        // Physeos requires Baldesion Arsenal runs — tracked by Eureka Fragment drops.
        // No in-game achievement exists for this stage (verified 2026-05-17 against Achievement.csv).
        // Identification falls back to CompletionItemIds.
        new("Physeos",
            AchievementId: null,
            CompletionItemIds: StageItems(job, PhyseosBase),
            Requirements: [ new(EurekaFragment, "Eureka Fragment", 100) ]),
    ];

    public static RelicSeries Build() => new(
        Id: "Eureka",
        Name: "Eurekan Weapons",
        Expansion: Expansion.SB,
        Weapons: WeaponOrder
            .Select(j => new RelicWeapon(j, BuildSteps(j), HasReplica: false, ReplicaItemId: null))
            .ToList());
}
