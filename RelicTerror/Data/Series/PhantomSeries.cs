using System;
using System.Collections.Generic;
using System.Linq;
using RelicTerror.Data;

namespace RelicTerror.Data.Series;

internal static class PhantomSeries
{
    private const uint Arcanite       = 47750;
    private const uint WaxingArcanite = 46850;
    private const uint WaningArcanite = 50058;

    private static readonly Job[] AchievementJobOrder =
    [
        Job.PLD, Job.WAR, Job.DRK, Job.GNB,
        Job.DRG, Job.MNK, Job.NIN, Job.VPR, Job.SAM, Job.RPR,
        Job.BRD, Job.MCH, Job.DNC,
        Job.BLM, Job.SMN, Job.RDM, Job.PCT,
        Job.WHM, Job.SCH, Job.AST, Job.SGE,
    ];

    // Item-id job order shared by every Phantom per-stage weapon block — verified
    // empirically against the Item sheet (Sword 47869 → PLD, Knuckles 47870 → MNK,
    // Bardiche 47871 → WAR). PLD weapon sits at base+0, PLD shield sits at base+21.
    private static readonly Job[] WeaponOrder =
    [
        Job.PLD, Job.MNK, Job.WAR, Job.DRG, Job.BRD, Job.WHM, Job.BLM,
        Job.SMN, Job.SCH, Job.NIN, Job.SAM, Job.MCH, Job.AST,
        Job.DRK, Job.RDM, Job.GNB, Job.DNC, Job.RPR, Job.SGE,
        Job.VPR, Job.PCT,
    ];

    private const uint PenumbraeBase = 3638; // "The Might Phantastick: <weapon> Penumbrae"
    private const uint UmbraeBase    = 3752; // "Phantom of the Umbra: <weapon> Umbrae"
    private const uint ObscurumBase  = 3842; // "Clare Obscurum: <weapon> Obscurum"

    // Per-stage item-id bases. PLD weapon = base, PLD shield = base+21, other jobs = base+offset.
    private const uint PenumbraeItemBase = 47869; // Phantom Sword Penumbrae
    private const uint UmbraeItemBase    = 47006; // Phantom Sword Umbrae
    private const uint ObscurumItemBase  = 50032; // Phantom Sword Obscurum

    private static uint? AchId(uint baseId, Job job)
    {
        var idx = Array.IndexOf(AchievementJobOrder, job);
        return idx < 0 ? null : (uint?)(baseId + (uint)idx);
    }

    private static IReadOnlyList<uint> StageItems(Job job, uint baseId)
    {
        var off = (uint)Array.IndexOf(WeaponOrder, job);
        return job == Job.PLD
            ? [baseId, baseId + 21]  // weapon + shield
            : [baseId + off];
    }

    private static IReadOnlyList<RelicStep> BuildSteps(Job job) =>
    [
        new("Penumbrae",
            AchievementId: AchId(PenumbraeBase, job),
            CompletionItemIds: StageItems(job, PenumbraeItemBase),
            Requirements: [ new(Arcanite, "Arcanite", 3) ]),
        new("Umbrae",
            AchievementId: AchId(UmbraeBase, job),
            CompletionItemIds: StageItems(job, UmbraeItemBase),
            Requirements: [ new(WaxingArcanite, "Waxing Arcanite", 3) ]),
        new("Obscurum",
            AchievementId: AchId(ObscurumBase, job),
            CompletionItemIds: StageItems(job, ObscurumItemBase),
            Requirements: [ new(WaningArcanite, "Waning Arcanite", 3) ]),
    ];

    public static RelicSeries Build() => new(
        Id: "Phantom",
        Name: "Phantom Weapons",
        Expansion: Expansion.DT,
        Weapons: AchievementJobOrder
            .Select(job => new RelicWeapon(job, BuildSteps(job), HasReplica: false, ReplicaItemId: null))
            .ToList());
}
