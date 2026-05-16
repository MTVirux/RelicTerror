using System;
using System.Collections.Generic;
using System.Linq;
using RelicTerror.Data;

namespace RelicTerror.Data.Series;

internal static class ResistanceSeries
{
    // Resistance (base)
    private const uint ThavnairianScalepowder = 30273;

    // Augmented Resistance
    private const uint TorturedMemoryOfTheDying  = 31573;
    private const uint SorrowfulMemoryOfTheDying = 31574;
    private const uint HarrowingMemoryOfTheDying = 31575;

    // Recollection
    private const uint BitterMemoryOfTheDying = 31576;

    // Law's Order
    private const uint LoathsomeMemoryOfTheDying = 32956;

    // Augmented Law's Order
    private const uint HauntingMemoryOfTheDying  = 32957;
    private const uint VexatiousMemoryOfTheDying = 32958;
    private const uint TimewornArtifact          = 32959;

    // Blade's
    private const uint RawEmotion = 33767;

    // Single job order shared by every Resistance achievement stage — verified against the
    // in-game Achievement sheet (e.g. row 2580 = Soulscourge → BLM, row 2583 = Ingrimm → WHM).
    private static readonly Job[] AchievementJobOrder =
    [
        Job.PLD, Job.WAR, Job.DRK, Job.GNB,
        Job.DRG, Job.MNK, Job.NIN, Job.SAM,
        Job.BRD, Job.MCH, Job.DNC,
        Job.BLM, Job.SMN, Job.RDM,
        Job.WHM, Job.SCH, Job.AST,
    ];

    // Item-id job order shared by every Resistance per-stage weapon block — verified empirically
    // against the Item sheet (e.g. Honorbound 30228 → PLD, Samsara 30229 → MNK, Skullrender 30230 → WAR).
    // PLD weapon sits at base+0, PLD shield sits at base+17. All other jobs occupy base+offset.
    private static readonly Job[] WeaponOrder =
    [
        Job.PLD, Job.MNK, Job.WAR, Job.DRG, Job.BRD, Job.NIN, Job.DRK,
        Job.MCH, Job.WHM, Job.BLM, Job.SMN, Job.SCH, Job.AST,
        Job.SAM, Job.RDM, Job.GNB, Job.DNC,
    ];

    private const uint ResistanceBase   = 2569; // "Pièce de Résistance: <weapon>"
    private const uint RecollectionBase = 2694; // "Pièce de Résistance: <weapon> Recollection"
    private const uint LawsOrderBase    = 2768; // "Quick to Judge: Law's Order <weapon>" — fires on Augmented stage completion despite the name.
    private const uint BladesBase       = 2857; // "Fit for a Queen: Blade's <weapon>"

    // Per-stage item-id bases. PLD weapon = base, PLD shield = base+17, other jobs = base+offset.
    private const uint ResistanceItemBase         = 30228; // Honorbound
    private const uint AugmentedResistanceItemBase = 30767; // Augmented Honorbound
    private const uint RecollectionItemBase       = 30785; // Honorbound Recollection
    private const uint LawsOrderItemBase          = 32651; // Law's Order Bastard Sword
    private const uint AugmentedLawsOrderItemBase = 32669; // Augmented Law's Order Bastard Sword
    private const uint BladesItemBase             = 33462; // Blade's Honor

    private static uint? AchId(uint baseId, Job job)
    {
        var idx = Array.IndexOf(AchievementJobOrder, job);
        return idx < 0 ? null : (uint?)(baseId + (uint)idx);
    }

    private static IReadOnlyList<uint> StageItems(Job job, uint baseId)
    {
        var off = (uint)Array.IndexOf(WeaponOrder, job);
        return job == Job.PLD
            ? [baseId, baseId + 17]  // weapon + shield
            : [baseId + off];
    }

    private static IReadOnlyList<RelicStep> BuildSteps(Job job) =>
    [
        new("Resistance",
            AchievementId: AchId(ResistanceBase, job),
            CompletionItemIds: StageItems(job, ResistanceItemBase),
            Requirements: [ new(ThavnairianScalepowder, "Thavnairian Scalepowder", 4) ]),
        // No in-game achievement exists for this stage (verified 2026-05-17 against Achievement.csv).
        // Identification falls back to CompletionItemIds.
        new("Augmented Resistance",
            AchievementId: null,
            CompletionItemIds: StageItems(job, AugmentedResistanceItemBase),
            Requirements:
            [
                new(TorturedMemoryOfTheDying,  "Tortured Memory of the Dying",  20),
                new(SorrowfulMemoryOfTheDying, "Sorrowful Memory of the Dying", 20),
                new(HarrowingMemoryOfTheDying, "Harrowing Memory of the Dying", 20),
            ]),
        new("Recollection",
            AchievementId: AchId(RecollectionBase, job),
            CompletionItemIds: StageItems(job, RecollectionItemBase),
            Requirements: [ new(BitterMemoryOfTheDying, "Bitter Memory of the Dying", 6) ]),
        // No in-game achievement exists for the base Law's Order stage (verified 2026-05-17).
        // The "Quick to Judge: Law's Order <weapon>" achievement (2768) actually fires upon
        // completing the Augmented Law's Order stage — it is wired there.
        // Identification falls back to CompletionItemIds.
        new("Law's Order",
            AchievementId: null,
            CompletionItemIds: StageItems(job, LawsOrderItemBase),
            Requirements: [ new(LoathsomeMemoryOfTheDying, "Loathsome Memory of the Dying", 15) ]),
        new("Augmented Law's Order",
            AchievementId: AchId(LawsOrderBase, job),
            CompletionItemIds: StageItems(job, AugmentedLawsOrderItemBase),
            Requirements:
            [
                new(HauntingMemoryOfTheDying,  "Haunting Memory of the Dying",  18),
                new(VexatiousMemoryOfTheDying, "Vexatious Memory of the Dying", 18),
                new(TimewornArtifact,          "Timeworn Artifact",             15),
            ]),
        new("Blade's",
            AchievementId: AchId(BladesBase, job),
            CompletionItemIds: StageItems(job, BladesItemBase),
            Requirements: [ new(RawEmotion, "Raw Emotion", 15) ]),
    ];

    private static readonly (Job Job, uint ReplicaItemId)[] WeaponDefs =
    [
        (Job.PLD, 33820), (Job.WAR, 33823), (Job.DRK, 33827), (Job.GNB, 33836),
        (Job.WHM, 33829), (Job.SCH, 33832), (Job.AST, 33833),
        (Job.MNK, 33822), (Job.DRG, 33824), (Job.NIN, 33826), (Job.SAM, 33834),
        (Job.BRD, 33825), (Job.MCH, 33828), (Job.DNC, 33837),
        (Job.BLM, 33830), (Job.SMN, 33831), (Job.RDM, 33835),
    ];

    public static RelicSeries Build() => new(
        Id: "Resistance",
        Name: "Resistance Weapons",
        Expansion: Expansion.ShB,
        Weapons: WeaponDefs
            .Select(d => new RelicWeapon(d.Job, BuildSteps(d.Job), HasReplica: true, ReplicaItemId: d.ReplicaItemId))
            .ToList());
}
