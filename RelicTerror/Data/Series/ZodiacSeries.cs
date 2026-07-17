using System;
using System.Collections.Generic;
using System.Linq;
using RelicTerror.Data;

namespace RelicTerror.Data.Series;

internal static class ZodiacSeries
{
    // Zenith
    private const uint ThavnairianMist = 6268;

    // Atma (one per astrological sign, dropped from FATEs)
    private const uint AtmaOfTheMaiden      = 7851;
    private const uint AtmaOfTheScorpion    = 7852;
    private const uint AtmaOfTheWaterBearer = 7853;
    private const uint AtmaOfTheGoat        = 7854;
    private const uint AtmaOfTheBull        = 7855;
    private const uint AtmaOfTheRam         = 7856;
    private const uint AtmaOfTheTwins       = 7857;
    private const uint AtmaOfTheLion        = 7858;
    private const uint AtmaOfTheFish        = 7859;
    private const uint AtmaOfTheArcher      = 7860;
    private const uint AtmaOfTheScales      = 7861;
    private const uint AtmaOfTheCrab        = 7862;

    // Novus
    private const uint SuperiorEnchantedInk = 7885;
    private const uint Alexandrite          = 7883;

    // Zodiac Braves — purchased items (1× each, 100,000 gil each)
    private const uint BronzeLakeCrystal = 9538;
    private const uint AllaganResin      = 9542;
    private const uint FuriteSand        = 9543;
    private const uint BrassKettle       = 9544;

    // Zodiac Braves — required per sub-quest (×4 sub-quests = 4 total)
    private const uint BombardCore       = 9539;
    private const uint SacredSpringWater = 9540;

    // Zodiac Braves — crafted HQ items (1× each)
    private const uint PerfectFirewood  = 9511;
    private const uint FurnaceRing      = 9510;
    private const uint PerfectPestle    = 9513;
    private const uint PerfectMortar    = 9512;
    private const uint PerfectVellum    = 9514;
    private const uint PerfectPounce    = 9515;
    private const uint PerfectCloth     = 9517;
    private const uint TailorMadeEelPie = 9516;

    // Sword-line job order — offset within each per-stage contiguous block.
    // Holy Shield (PLD shield) sits at +9 from sword PLD in every block; NIN is non-contiguous.
    private static readonly Job[] SwordOrder =
    [
        Job.PLD, Job.MNK, Job.WAR, Job.DRG, Job.BRD,
        Job.WHM, Job.BLM, Job.SMN, Job.SCH,
    ];

    private const uint ZenithBase = 6257;
    private const uint AtmaBase   = 7824;
    private const uint AnimusBase = 7834;
    private const uint NovusBase  = 7863;
    private const uint NexusBase  = 8649;
    private const uint BravesBase = 9491;
    private const uint ZetaBase   = 10054;

    // Yoshimitsu (NIN) was added late in ARR, so its IDs live outside the sword block.
    private const uint NinZenith = 9250;
    private const uint NinAtma   = 9251;
    private const uint NinAnimus = 9252;
    private const uint NinNovus  = 9253;
    private const uint NinNexus  = 9254;
    private const uint NinBraves = 9501; // Sasuke's Blades
    private const uint NinZeta   = 10064; // Sasuke's Blades Zeta

    private static IReadOnlyList<uint> StageItems(Job job, uint baseId, uint ninId)
    {
        if (job == Job.NIN) return [ninId];
        var off = (uint)Array.IndexOf(SwordOrder, job);
        return job == Job.PLD
            ? [baseId, baseId + 9]  // Curtana + Holy Shield (or rename pair at Braves/Zeta).
            : [baseId + off];
    }

    // Per-job "A Relic Reborn (<weapon>)" quests — completed once per job, so quest
    // completion is an exact per-job marker for the base stage. Verified 2026-07-17
    // against the Quest sheet.
    private static readonly Dictionary<Job, uint> RelicRebornQuests = new()
    {
        [Job.WAR] = 66655, // Bravura
        [Job.PLD] = 66656, // Curtana
        [Job.MNK] = 66657, // Sphairai
        [Job.DRG] = 66658, // Gae Bolg
        [Job.BLM] = 66659, // Stardust Rod
        [Job.WHM] = 66660, // Thyrus
        [Job.BRD] = 66661, // Artemis Bow
        [Job.SMN] = 66662, // The Veil of Wiyu
        [Job.SCH] = 66663, // Omnilex
        [Job.NIN] = 67115, // Yoshimitsu
    };

    private static IReadOnlyList<RelicStep> BuildSteps(Job job) =>
    [
        new("Relic",
            AchievementId: null,
            CompletionItemIds: null,
            Requirements: [],
            CompletionQuestId: RelicRebornQuests[job]),
        // No in-game achievement exists for this stage (verified 2026-05-18 against Achievement.csv).
        // Identification falls back to CompletionItemIds.
        new("Zenith",
            AchievementId: null,
            CompletionItemIds: StageItems(job, ZenithBase, NinZenith),
            Requirements: [ new(ThavnairianMist, "Thavnairian Mist", 3) ]),
        // No in-game achievement exists for this stage (verified 2026-05-18 against Achievement.csv).
        // Identification falls back to CompletionItemIds.
        new("Atma",
            AchievementId: null,
            CompletionItemIds: StageItems(job, AtmaBase, NinAtma),
            Requirements:
            [
                new(AtmaOfTheMaiden,      "Atma of the Maiden",       1),
                new(AtmaOfTheScorpion,    "Atma of the Scorpion",     1),
                new(AtmaOfTheWaterBearer, "Atma of the Water-bearer", 1),
                new(AtmaOfTheGoat,        "Atma of the Goat",         1),
                new(AtmaOfTheBull,        "Atma of the Bull",         1),
                new(AtmaOfTheRam,         "Atma of the Ram",          1),
                new(AtmaOfTheTwins,       "Atma of the Twins",        1),
                new(AtmaOfTheLion,        "Atma of the Lion",         1),
                new(AtmaOfTheFish,        "Atma of the Fish",         1),
                new(AtmaOfTheArcher,      "Atma of the Archer",       1),
                new(AtmaOfTheScales,      "Atma of the Scales",       1),
                new(AtmaOfTheCrab,        "Atma of the Crab",         1),
            ]),
        // Animus requires completing job-specific Trials of the Braves books;
        // those are quest key items without standard inventory IDs.
        // Only a generic single-weapon achievement exists ("Taking It to the Stars", id 925);
        // no per-job achievement exists, so identification falls back to CompletionItemIds.
        new("Animus",
            AchievementId: null,
            CompletionItemIds: StageItems(job, AnimusBase, NinAnimus),
            Requirements: []),
        // Only a generic single-weapon achievement exists ("A Star Is Born", id 926);
        // no per-job achievement exists, so identification falls back to CompletionItemIds.
        new("Novus",
            AchievementId: null,
            CompletionItemIds: StageItems(job, NovusBase, NinNovus),
            Requirements:
            [
                new(SuperiorEnchantedInk, "Superior Enchanted Ink", 3),
                new(Alexandrite,          "Alexandrite",            75),
            ]),
        // Nexus is light-farming; no items to track.
        // Only a generic single-weapon achievement exists ("Inspire the Nexus", id 1028);
        // no per-job achievement exists, so identification falls back to CompletionItemIds.
        new("Nexus",
            AchievementId: null,
            CompletionItemIds: StageItems(job, NexusBase, NinNexus),
            Requirements: []),
        // Only a generic single-weapon achievement exists ("Lethal Weapon", id 1054);
        // no per-job achievement exists, so identification falls back to CompletionItemIds.
        new("Braves",
            AchievementId: null,
            CompletionItemIds: StageItems(job, BravesBase, NinBraves),
            Requirements:
            [
                new(BombardCore,       "Bombard Core",        4),
                new(SacredSpringWater, "Sacred Spring Water", 4),
                new(BronzeLakeCrystal, "Bronze Lake Crystal", 1),
                new(AllaganResin,      "Allagan Resin",       1),
                new(FuriteSand,        "Furite Sand",         1),
                new(BrassKettle,       "Brass Kettle",        1),
                new(PerfectFirewood,   "Perfect Firewood",    1),
                new(FurnaceRing,       "Furnace Ring",        1),
                new(PerfectPestle,     "Perfect Pestle",      1),
                new(PerfectMortar,     "Perfect Mortar",      1),
                new(PerfectVellum,     "Perfect Vellum",      1),
                new(PerfectPounce,     "Perfect Pounce",      1),
                new(PerfectCloth,      "Perfect Cloth",       1),
                new(TailorMadeEelPie,  "Tailor-made Eel Pie", 1),
            ]),
        // Zeta requires 12 Mahatma key items obtained from Remon;
        // these are quest key items without standard inventory IDs.
        // Only a generic single-weapon achievement exists ("The Letter Z", id 1081);
        // no per-job achievement exists, so identification falls back to CompletionItemIds.
        new("Zeta",
            AchievementId: null,
            CompletionItemIds: StageItems(job, ZetaBase, NinZeta),
            Requirements: []),
    ];

    private static readonly (Job Job, uint ReplicaItemId)[] WeaponDefs =
    [
        (Job.PLD, 12123), (Job.WAR, 12141), (Job.MNK, 12132), (Job.DRG, 12150),
        (Job.BRD, 12159), (Job.NIN, 12168), (Job.WHM, 12177), (Job.BLM, 12186),
        (Job.SMN, 12195), (Job.SCH, 12204),
    ];

    // Membership and order from JournalGenre 88 ("Zodiac Weapons") plus the unlock quest
    // 66241, chained via the Quest sheet's PreviousQuest links. The base-stage per-job
    // "A Relic Reborn" quests live in RelicRebornQuests instead. A duplicate row 67823
    // also carries the name "The Vital Title"; 66097 is the one in the chain.
    private static readonly JournalQuest[] JournalQuests =
    [
        new(66241, "The Weaponsmith of Legend",   Repeatable: false),
        new(66971, "Up in Arms",                  Repeatable: false), // Atma
        new(66972, "Trials of the Braves",        Repeatable: false), // Animus
        new(66998, "Celestial Radiance",          Repeatable: false), // Novus
        new(67000, "Star Light, Star Bright",     Repeatable: false), // Nexus
        new(65742, "Mmmmmm, Soulglazed Relics",   Repeatable: false), // Braves
        new(65892, "Wherefore Art Thou, Zodiac",  Repeatable: false), // Braves
        new(65893, "A Ponze of Flesh",            Repeatable: true),  // Braves book
        new(65894, "Labor of Love",               Repeatable: true),  // Braves book
        new(65895, "Method in His Malice",        Repeatable: true),  // Braves book
        new(65896, "A Treasured Mother",          Repeatable: true),  // Braves book
        new(65897, "His Dark Materia",            Repeatable: false), // Zeta
        new(66096, "Rise and Shine",              Repeatable: false), // Zeta
        new(66097, "The Vital Title",             Repeatable: false), // Zeta
    ];

    public static RelicSeries Build() => new(
        Id: "Zodiac",
        Name: "Zodiac Weapons",
        Expansion: Expansion.ARR,
        Weapons: WeaponDefs
            .Select(d => new RelicWeapon(d.Job, BuildSteps(d.Job), HasReplica: true, ReplicaItemId: d.ReplicaItemId))
            .ToList(),
        JournalQuests: JournalQuests);
}
