using System;
using System.Collections.Generic;
using System.Linq;
using RelicTerror.Data;

namespace RelicTerror.Data.Series;

internal static class PhantomSeries
{
    private const uint Arcanite        = 47750;
    private const uint WaxingArcanite  = 46850;
    private const uint WaningArcanite  = 50058;
    private const uint EclipticArcanite = 50977;

    private static readonly Job[] AchievementJobOrder =
    [
        Job.PLD, Job.WAR, Job.DRK, Job.GNB,
        Job.DRG, Job.MNK, Job.NIN, Job.VPR, Job.SAM, Job.RPR,
        Job.BRD, Job.MCH, Job.DNC,
        Job.BLM, Job.SMN, Job.RDM, Job.PCT,
        Job.WHM, Job.SCH, Job.AST, Job.SGE,
    ];

    // Item-id job order shared by every Phantom per-stage weapon block — verified
    // against the Item sheet's ClassJobCategory for every offset (Sword 47869 → PLD,
    // Guillotine 47879 → DRK, Blade 47882 → SAM, Pendulums 47887 → SGE).
    // PLD weapon sits at base+0, PLD shield sits at base+21.
    private static readonly Job[] WeaponOrder =
    [
        Job.PLD, Job.MNK, Job.WAR, Job.DRG, Job.BRD, Job.WHM, Job.BLM,
        Job.SMN, Job.SCH, Job.NIN, Job.DRK, Job.MCH, Job.AST,
        Job.SAM, Job.RDM, Job.GNB, Job.DNC, Job.RPR, Job.SGE,
        Job.VPR, Job.PCT,
    ];

    private const uint PenumbraeBase = 3638; // "The Might Phantastick: <weapon> Penumbrae"
    private const uint UmbraeBase    = 3752; // "Phantom of the Umbra: <weapon> Umbrae"
    private const uint ObscurumBase  = 3842; // "Clare Obscurum: <weapon> Obscurum"
    private const uint OccultumBase  = 3949; // "Cut Above the Rest: <weapon> Occultum"

    // Per-stage item-id bases. PLD weapon = base, PLD shield = base+21, other jobs = base+offset.
    private const uint PenumbraeItemBase  = 47869; // Phantom Sword Penumbrae
    private const uint UmbraeItemBase     = 47006; // Phantom Sword Umbrae
    private const uint ObscurumItemBase   = 50032; // Phantom Sword Obscurum
    private const uint EclipticumItemBase = 50978; // Phantom Sword Eclipticum
    private const uint OccultumItemBase   = 51000; // Phantom Sword Occultum

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
        // No in-game achievement exists for this stage (verified 2026-08-15 against the
        // Achievement sheet — the Obscurum block ends at 3862 and nothing Phantom-related
        // appears until the Occultum block at 3949). Falls back to CompletionItemIds.
        new("Eclipticum",
            AchievementId: null,
            CompletionItemIds: StageItems(job, EclipticumItemBase),
            Requirements: [ new(EclipticArcanite, "Ecliptic Arcanite", 3) ]),
        // Gated on filling the Knowledge Crystal with martial memories — no item turn-in.
        new("Occultum",
            AchievementId: AchId(OccultumBase, job),
            CompletionItemIds: StageItems(job, OccultumItemBase),
            Requirements: []),
    ];

    // Membership and order from JournalGenre 92 ("Phantom Weapons"). All once-only —
    // additional weapons are NPC exchanges, not quest re-runs.
    private static readonly JournalQuest[] JournalQuests =
    [
        new(70855, "Arcane Artistry",             Repeatable: false),
        new(70856, "Forging the Phantasmal",      Repeatable: false), // Penumbrae
        new(70916, "Keeping the Old Ways Alive",  Repeatable: false),
        new(70917, "Aether, Aether, Everywhere",  Repeatable: false),
        new(70918, "Wrought by Hands Phantasmal", Repeatable: false), // Umbrae
        new(70990, "Timeworn Techniques",         Repeatable: false),
        new(70991, "In Pursuit of Perfection",    Repeatable: false),
        new(70992, "A Phantom Reborn",            Repeatable: false), // Obscurum
        new(71038, "Under No Illusion",           Repeatable: false),
        new(71039, "Phantoms to Fillet",          Repeatable: false),
        new(71040, "A Phantom Unveiled",          Repeatable: false), // Eclipticum
        new(71041, "Final Phantasm",              Repeatable: false), // Occultum
        new(71042, "All Too Fleeting",            Repeatable: false), // bonus glamour reward
    ];

    public static RelicSeries Build() => new(
        Id: "Phantom",
        Name: "Phantom Weapons",
        Expansion: Expansion.DT,
        Weapons: AchievementJobOrder
            .Select(job => new RelicWeapon(job, BuildSteps(job), HasReplica: false, ReplicaItemId: null))
            .ToList(),
        JournalQuests: JournalQuests);
}
