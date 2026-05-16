using System;
using System.Collections.Generic;
using System.Linq;
using RelicTerror.Data;

namespace RelicTerror.Data.Series;

internal static class MandervilleSeries
{
    private const uint ManderiumMeteorite     = 38420;
    private const uint ComplementaryChondrite = 38940;
    private const uint AmplifyingAchondrite   = 40322;
    private const uint CosmicCrystallite      = 41032;

    // Achievement role order observed in the in-game Achievement sheet for this series.
    // Stage IDs are contiguous; per-job offset is the index in this list.
    private static readonly Job[] AchievementJobOrder =
    [
        Job.PLD, Job.WAR, Job.DRK, Job.GNB,
        Job.DRG, Job.MNK, Job.NIN, Job.SAM, Job.RPR,
        Job.BRD, Job.MCH, Job.DNC,
        Job.BLM, Job.SMN, Job.RDM,
        Job.WHM, Job.SCH, Job.AST, Job.SGE,
    ];

    // Item-id job order shared by every Manderville per-stage weapon block — verified
    // empirically against the Item sheet (e.g. Manderville Sword 38400 → PLD, Knuckles
    // 38401 → MNK, Axe 38402 → WAR). PLD weapon sits at base+0, PLD shield sits at base+19.
    private static readonly Job[] WeaponOrder =
    [
        Job.PLD, Job.MNK, Job.WAR, Job.DRG, Job.BRD, Job.NIN, Job.DRK,
        Job.MCH, Job.WHM, Job.BLM, Job.SMN, Job.SCH, Job.AST,
        Job.SAM, Job.RDM, Job.GNB, Job.DNC, Job.SGE, Job.RPR,
    ];

    private const uint MandervilleBase    = 3128; // "Hamm(er)ing It Up: Manderville <weapon>"
    private const uint AmazingBase        = 3224; // "Well-oiled: Amazing Manderville <weapon>"
    private const uint MajesticBase       = 3285; // "Reforged: Majestic Manderville <weapon>"
    private const uint MandervillousBase  = 3380; // "Perfect: Mandervillous <weapon>"

    // Per-stage item-id bases. PLD weapon = base, PLD shield = base+19, other jobs = base+offset.
    private const uint MandervilleItemBase    = 38400; // Manderville Sword
    private const uint AmazingItemBase        = 39144; // Amazing Manderville Sword
    private const uint MajesticItemBase       = 39920; // Majestic Manderville Sword
    private const uint MandervillousItemBase  = 40932; // Mandervillous Falchion

    private static uint? AchId(uint baseId, Job job)
    {
        var idx = Array.IndexOf(AchievementJobOrder, job);
        return idx < 0 ? null : (uint?)(baseId + (uint)idx);
    }

    private static IReadOnlyList<uint> StageItems(Job job, uint baseId)
    {
        var off = (uint)Array.IndexOf(WeaponOrder, job);
        return job == Job.PLD
            ? [baseId, baseId + 19]  // weapon + shield
            : [baseId + off];
    }

    private static IReadOnlyList<RelicStep> BuildSteps(Job job) =>
    [
        new("Manderville",
            AchievementId: AchId(MandervilleBase, job),
            CompletionItemIds: StageItems(job, MandervilleItemBase),
            Requirements: [ new(ManderiumMeteorite, "Manderium Meteorite", 3) ]),
        new("Amazing Manderville",
            AchievementId: AchId(AmazingBase, job),
            CompletionItemIds: StageItems(job, AmazingItemBase),
            Requirements: [ new(ComplementaryChondrite, "Complementary Chondrite", 3) ]),
        new("Majestic Manderville",
            AchievementId: AchId(MajesticBase, job),
            CompletionItemIds: StageItems(job, MajesticItemBase),
            Requirements: [ new(AmplifyingAchondrite, "Amplifying Achondrite", 3) ]),
        new("Mandervillous",
            AchievementId: AchId(MandervillousBase, job),
            CompletionItemIds: StageItems(job, MandervillousItemBase),
            Requirements: [ new(CosmicCrystallite, "Cosmic Crystallite", 3) ]),
    ];

    public static RelicSeries Build() => new(
        Id: "Manderville",
        Name: "Manderville Weapons",
        Expansion: Expansion.EW,
        Weapons: AchievementJobOrder
            .Select(job => new RelicWeapon(job, BuildSteps(job), HasReplica: false, ReplicaItemId: null))
            .ToList());
}
