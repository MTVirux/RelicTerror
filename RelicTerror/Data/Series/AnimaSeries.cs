using System;
using System.Collections.Generic;
using System.Linq;
using RelicTerror.Data;

namespace RelicTerror.Data.Series;

internal static class AnimaSeries
{
    // Animated
    private const uint LuminousWindCrystal      = 13570;
    private const uint LuminousFireCrystal      = 13571;
    private const uint LuminousLightningCrystal = 13573;
    private const uint LuminousIceCrystal       = 13569;
    private const uint LuminousEarthCrystal     = 13572;
    private const uint LuminousWaterCrystal     = 13574;

    // Animus (Unidentifiable materials + crafted components)
    private const uint UnidentifiableBone  = 13582;
    private const uint UnidentifiableShell = 13584;
    private const uint UnidentifiableOre   = 13586;
    private const uint UnidentifiableSeeds = 13588;
    private const uint AdamantiteFrancesca = 13589;
    private const uint TitaniumAlloyMirror = 13591;
    private const uint DispellingArrow     = 13593;
    private const uint Kingcake            = 13595;

    // Hyperconductive
    private const uint AetherOil = 14899;

    // Reconditioned
    private const uint CrystalSand = 15841;
    private const uint Umbrite     = 15840;

    // Sharpened
    private const uint SingingCluster = 16064;

    // Complete
    private const uint Pneumite = 16933;

    // Lux
    private const uint ArchaicEnchantedInk = 16934;

    // Achievement job order shared across Animaniac (1406), Hyper Animaniac (1499),
    // and It's Really Done (1708) — per the in-game Achievement sheet.
    private static readonly Job[] AchievementJobOrder =
    [
        Job.PLD, Job.MNK, Job.WAR, Job.DRG, Job.BRD,
        Job.WHM, Job.BLM, Job.SMN, Job.SCH,
        Job.NIN, Job.MCH, Job.DRK, Job.AST,
    ];

    // Item-id job order for all per-stage Anima weapon blocks. Each stage stores
    // 13 weapons in this sequence, followed by the PLD shield at offset 13.
    private static readonly Job[] WeaponOrder =
    [
        Job.PLD, Job.MNK, Job.WAR, Job.DRG, Job.BRD, Job.NIN, Job.DRK,
        Job.MCH, Job.WHM, Job.BLM, Job.SMN, Job.SCH, Job.AST,
    ];

    private const uint AnimusBase          = 1406; // "Animaniac: <weapon>"
    private const uint HyperconductiveBase = 1499; // "Hyper Animaniac: <weapon>"
    private const uint ReconditionedBaseAch = 1605; // "It's Alive: <weapon>"
    private const uint SharpenedBaseAch    = 1667; // "It's Smart: <weapon>"
    private const uint CompleteBaseAch     = 1695; // "It's Done: <weapon>"
    private const uint LuxBase             = 1708; // "It's Really Done: <weapon>"

    // Stage item-id bases — PLD weapon sits at the base, PLD shield sits at base+13.
    private const uint AnimatedBase        = 13611;
    private const uint AnimaBase           = 13223; // Animus stage (items are unique proper nouns, e.g. "Almace", "Aettir")
    private const uint HyperItemBase       = 14870;
    private const uint ReconditionedBase   = 15223;
    private const uint SharpenedBase       = 15237;
    private const uint CompleteBase        = 15251;
    private const uint LuxItemBase         = 16050;

    private static uint? AchId(uint baseId, Job job)
    {
        var idx = Array.IndexOf(AchievementJobOrder, job);
        return idx < 0 ? null : (uint?)(baseId + (uint)idx);
    }

    private static IReadOnlyList<uint> StageItems(Job job, uint baseId)
    {
        var off = (uint)Array.IndexOf(WeaponOrder, job);
        return job == Job.PLD
            ? [baseId, baseId + 13]  // sword + shield
            : [baseId + off];
    }

    private static IReadOnlyList<RelicStep> BuildSteps(Job job) =>
    [
        // No in-game achievement exists for this stage (verified 2026-05-17 against Achievement.csv).
        // Identification falls back to CompletionItemIds.
        new("Animated",
            AchievementId: null,
            CompletionItemIds: StageItems(job, AnimatedBase),
            Requirements:
            [
                new(LuminousWindCrystal,      "Luminous Wind Crystal",      1),
                new(LuminousFireCrystal,      "Luminous Fire Crystal",      1),
                new(LuminousLightningCrystal, "Luminous Lightning Crystal", 1),
                new(LuminousIceCrystal,       "Luminous Ice Crystal",       1),
                new(LuminousEarthCrystal,     "Luminous Earth Crystal",     1),
                new(LuminousWaterCrystal,     "Luminous Water Crystal",     1),
            ]),
        // Awoken requires completing 10 specific dungeons with the weapon equipped — no item tracking.
        // No in-game achievement exists for this stage (verified 2026-05-17 against Achievement.csv).
        new("Awoken",
            AchievementId: null,
            CompletionItemIds: null,
            Requirements: []),
        new("Animus",
            AchievementId: AchId(AnimusBase, job),
            CompletionItemIds: StageItems(job, AnimaBase),
            Requirements:
            [
                new(UnidentifiableBone,  "Unidentifiable Bone",    10),
                new(UnidentifiableShell, "Unidentifiable Shell",   10),
                new(UnidentifiableOre,   "Unidentifiable Ore",     10),
                new(UnidentifiableSeeds, "Unidentifiable Seeds",   10),
                new(AdamantiteFrancesca, "Adamantite Francesca",    4),
                new(TitaniumAlloyMirror, "Titanium Alloy Mirror",   4),
                new(DispellingArrow,     "Dispelling Arrow",         4),
                new(Kingcake,            "Kingcake",                 4),
            ]),
        new("Hyperconductive",
            AchievementId: AchId(HyperconductiveBase, job),
            CompletionItemIds: StageItems(job, HyperItemBase),
            Requirements: [ new(AetherOil, "Aether Oil", 5) ]),
        new("Reconditioned",
            AchievementId: AchId(ReconditionedBaseAch, job),
            CompletionItemIds: StageItems(job, ReconditionedBase),
            Requirements:
            [
                new(CrystalSand, "Crystal Sand", 60),
                new(Umbrite,     "Umbrite",       60),
            ]),
        new("Sharpened",
            AchievementId: AchId(SharpenedBaseAch, job),
            CompletionItemIds: StageItems(job, SharpenedBase),
            Requirements: [ new(SingingCluster, "Singing Cluster", 50) ]),
        new("Complete",
            AchievementId: AchId(CompleteBaseAch, job),
            CompletionItemIds: StageItems(job, CompleteBase),
            Requirements: [ new(Pneumite, "Pneumite", 15) ]),
        new("Lux",
            AchievementId: AchId(LuxBase, job),
            CompletionItemIds: StageItems(job, LuxItemBase),
            Requirements: [ new(ArchaicEnchantedInk, "Archaic Enchanted Ink", 1) ]),
    ];

    private static readonly (Job Job, uint ReplicaItemId)[] WeaponDefs =
    [
        (Job.PLD, 20998), (Job.WAR, 21000), (Job.DRK, 21004), (Job.MNK, 20999),
        (Job.DRG, 21001), (Job.NIN, 21003), (Job.BRD, 21002), (Job.MCH, 21005),
        (Job.WHM, 21006), (Job.BLM, 21007), (Job.SMN, 21008), (Job.SCH, 21009),
        (Job.AST, 21010),
    ];

    // Membership and order from JournalGenre 89 ("Anima Weapons"), chained via the Quest
    // sheet's PreviousQuest links; stage notes from each quest's required-item fields.
    // The Reconditioned Umbrite/Crystal Sand trade runs through hidden system quest 67870
    // ("Recondition the Anima"), which never appears in the journal and is omitted here.
    private static readonly JournalQuest[] JournalQuests =
    [
        new(67747, "An Unexpected Proposal",     Repeatable: false),
        new(67748, "Soul without Life",          Repeatable: true),  // Animated
        new(67749, "Toughening Up",              Repeatable: true),  // Awoken
        new(67750, "Coming into Its Own",        Repeatable: true),  // Anima
        new(67820, "Finding Your Voice",         Repeatable: true),  // Hyperconductive
        new(67864, "A Dream Fulfilled",          Repeatable: true),  // Reconditioned
        new(67915, "Future Proof",               Repeatable: true),  // Sharpened
        new(67916, "Seeking Inspiration",        Repeatable: true),  // Sharpened
        new(67917, "Cut from a Different Cloth", Repeatable: true),  // Sharpened
        new(67932, "Born Again Anima",           Repeatable: true),  // Complete
        new(67933, "Some Assembly Required",     Repeatable: true),  // Complete
        new(67934, "Body and Soul",              Repeatable: false),
        new(67939, "Words of Wisdom",            Repeatable: false),
        new(67940, "Best Friends Forever",       Repeatable: true),  // Lux
    ];

    public static RelicSeries Build() => new(
        Id: "Anima",
        Name: "Anima Weapons",
        Expansion: Expansion.HW,
        Weapons: WeaponDefs
            .Select(d => new RelicWeapon(d.Job, BuildSteps(d.Job), HasReplica: true, ReplicaItemId: d.ReplicaItemId))
            .ToList(),
        JournalQuests: JournalQuests);
}
