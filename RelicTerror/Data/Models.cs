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
    IReadOnlyList<RelicWeapon> Weapons);

public sealed record RelicWeapon(
    Job Job,
    IReadOnlyList<RelicStep> Steps,
    bool HasReplica,
    uint? ReplicaItemId);

/// <param name="Name">Human-readable step name shown in the detail panel (e.g., "Animus", "Hyperconductive").</param>
/// <param name="AchievementId">
/// Primary identifier. When set, this achievement's completion is the
/// AUTHORITATIVE marker for the step — owning the form weapon is NOT
/// sufficient if an achievement is defined here.
/// </param>
/// <param name="CompletionItemIds">
/// Form-weapon item IDs for this stage. Two roles:
/// (1) Fallback step identifier when <see cref="AchievementId"/> is null —
/// presence of ANY listed ID in inventory, Armoury, Glamour Dresser, or
/// Armoire marks the step (and all prior steps via lookback) complete.
/// (2) Drives the Forms tooltip in the detail panel and the
/// <see cref="State.WeaponProgress.RelicOwned"/> derivation regardless of
/// whether an achievement is set.
/// Leave null for steps that produce no new item (e.g., Anima "Awoken").
/// </param>
/// <param name="Requirements">
/// Materials the player gathers for this step. DISPLAY-ONLY — used for the
/// per-step progress counts in the UI. These NEVER identify the step as
/// complete; identification uses <see cref="AchievementId"/> and then
/// <see cref="CompletionItemIds"/> only.
/// </param>
public sealed record RelicStep(
    string Name,
    uint? AchievementId,
    IReadOnlyList<uint>? CompletionItemIds,
    IReadOnlyList<StepRequirement> Requirements);

public sealed record StepRequirement(
    uint ItemId,
    string ItemName,
    int RequiredCount);
