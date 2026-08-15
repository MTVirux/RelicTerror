using System.Collections.Generic;

namespace RelicTerror.Data;

public enum Expansion { ARR, HW, SB, ShB, EW, DT }

public enum Job
{
    PLD, WAR, DRK, GNB,            // Tank
    WHM, SCH, AST, SGE,            // Healer
    MNK, DRG, NIN, SAM, RPR, VPR, // Melee DPS
    BRD, MCH, DNC,                 // Physical Ranged DPS
    BLM, SMN, RDM, PCT, BLU        // Caster DPS
}

public sealed record RelicSeries(
    string Id,
    string Name,
    Expansion Expansion,
    IReadOnlyList<RelicWeapon> Weapons,
    IReadOnlyList<JournalQuest> JournalQuests);

/// <param name="QuestId">Quest-sheet row id (65536 + 16-bit game quest id). QuestManager's
/// uint overloads mask to 16 bits, so either form matches at runtime.</param>
/// <param name="DisplayName">Journal name, sans the leading quest-type icon glyph.</param>
/// <param name="Repeatable">Mirrors the Quest sheet's IsRepeatable column. Repeatable quests
/// are re-accepted for each additional weapon.</param>
public sealed record JournalQuest(uint QuestId, string DisplayName, bool Repeatable);

public sealed record JournalQuestStatus(JournalQuest Quest, bool IsAccepted, bool IsComplete);

public sealed record RelicWeapon(
    Job Job,
    IReadOnlyList<RelicStep> Steps,
    bool HasReplica,
    uint? ReplicaItemId);

/// <param name="Name">Stage name, e.g. "Animus".</param>
/// <param name="CompletionQuestId">
/// Authoritative completion marker, checked before AchievementId because quest flags are
/// always memory-resident. Only usable when the quest is per-job - a shared once-per-character
/// quest cannot attribute completion to a specific weapon.
/// </param>
/// <param name="AchievementId">
/// Authoritative marker when CompletionQuestId is null - owning the form weapon is NOT
/// sufficient once an achievement is set here.
/// </param>
/// <param name="CompletionItemIds">
/// Form-weapon item IDs for this stage. Identifies the step only when AchievementId is null:
/// any listed ID in inventory, Armoury, Glamour Dresser, or Armoire marks the step and all
/// prior steps complete. Always drives the Forms tooltip and the RelicOwned derivation.
/// Null for steps that produce no new item.
/// </param>
/// <param name="Requirements">
/// Materials gathered for this step. Display-only - these NEVER identify a step as complete.
/// </param>
public sealed record RelicStep(
    string Name,
    uint? AchievementId,
    IReadOnlyList<uint>? CompletionItemIds,
    IReadOnlyList<StepRequirement> Requirements,
    uint? CompletionQuestId = null);

public sealed record StepRequirement(
    uint ItemId,
    string ItemName,
    int RequiredCount);
