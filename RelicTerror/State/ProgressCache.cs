using System;
using System.Collections.Generic;
using System.Linq;
using RelicTerror.Data;

namespace RelicTerror.State;

public sealed record WeaponProgress(
    int CompletedSteps,
    int TotalSteps,
    IReadOnlyList<StepDetail> Steps,
    bool RelicOwned,
    bool ReplicaOwned,
    IReadOnlyList<FormOwnership> Forms);

public sealed record FormOwnership(
    int StepIndex,
    string StepName,
    IReadOnlyList<uint> ItemIds,
    bool Owned);

public sealed record StepDetail(
    RelicStep Step,
    IReadOnlyList<StepItemStatus> ItemStatuses,
    bool IsComplete,
    bool IsCurrent);

public sealed record StepItemStatus(
    StepRequirement Requirement,
    int CurrentCount);

public static class ProgressCache
{
    public static WeaponProgress ComputeWeaponProgress(
        RelicWeapon weapon,
        IReadOnlyDictionary<uint, int> itemCounts,
        Func<uint, bool> isAchievementComplete,
        IReadOnlySet<uint> storedItemIds,
        Func<uint, bool> isQuestComplete,
        WeaponProgress? floor = null)
    {
        var steps = weapon.Steps;

        // Floor scan: highest step whose identifier currently fires. Lookback marks
        // every prior step complete by transitivity.
        var floorIndex = -1;
        for (var i = 0; i < steps.Count; i++)
        {
            if (IsStepComplete(steps[i], itemCounts, isAchievementComplete, storedItemIds, isQuestComplete))
                floorIndex = i;
        }

        var completedSteps = floorIndex + 1;
        for (var i = completedSteps; i < steps.Count; i++)
        {
            if (IsStepComplete(steps[i], itemCounts, isAchievementComplete, storedItemIds, isQuestComplete))
                completedSteps++;
            else
                break;
        }

        // Regression guard: progress never goes backwards within a session. Item sources like
        // the Armoire, Glamour Dresser, and retainer bags only return data when their windows
        // are open, so a rebuild triggered while those are unloaded can produce a lower count.
        if (floor is not null && floor.CompletedSteps > completedSteps)
            completedSteps = floor.CompletedSteps;

        var stepDetails = steps
            .Select((step, idx) => new StepDetail(
                Step: step,
                ItemStatuses: step.Requirements
                    .Select(r => new StepItemStatus(r, itemCounts.TryGetValue(r.ItemId, out var c) ? c : 0))
                    .ToList(),
                IsComplete: idx < completedSteps,
                IsCurrent: idx == completedSteps))
            .ToList();

        var computedRelicOwned = steps.Count > 0
            && IsStepComplete(steps[^1], itemCounts, isAchievementComplete, storedItemIds, isQuestComplete);
        var relicOwned = computedRelicOwned || (floor?.RelicOwned ?? false);

        var computedReplicaOwned = weapon.HasReplica
            && weapon.ReplicaItemId is { } replicaId
            && (itemCounts.TryGetValue(replicaId, out var rc) && rc > 0
                || storedItemIds.Contains(replicaId));
        var replicaOwned = computedReplicaOwned || (floor?.ReplicaOwned ?? false);

        var forms = new List<FormOwnership>(steps.Count);
        for (var i = 0; i < steps.Count; i++)
        {
            var ids = steps[i].CompletionItemIds;
            if (ids is null || ids.Count == 0) continue;
            var owned = ids.Any(id => itemCounts.ContainsKey(id) || storedItemIds.Contains(id));
            forms.Add(new FormOwnership(i, steps[i].Name, ids, owned));
        }

        return new WeaponProgress(completedSteps, steps.Count, stepDetails, relicOwned, replicaOwned, forms);
    }

    // Single source of truth for step completion. Priority chain:
    //   1. CompletionQuestId — when set, authoritative. Job-specific quest
    //      completion flags are always memory-resident, so this needs no fetch.
    //   2. AchievementId — when set, authoritative. Owning the form weapon does
    //      NOT mark the step done if the achievement is incomplete.
    //   3. CompletionItemIds — fallback identifier when neither is set.
    //      Any listed ID present in tracked inventory / Armoury / Glamour Dresser
    //      / Armoire counts.
    //   4. Otherwise false. Material Requirements NEVER identify completion;
    //      they only drive the per-step progress display.
    private static bool IsStepComplete(
        RelicStep step,
        IReadOnlyDictionary<uint, int> itemCounts,
        Func<uint, bool> isAchievementComplete,
        IReadOnlySet<uint> storedItemIds,
        Func<uint, bool> isQuestComplete)
    {
        if (step.CompletionQuestId is { } questId)
            return isQuestComplete(questId);

        if (step.AchievementId is { } achId)
            return isAchievementComplete(achId);

        if (step.CompletionItemIds is { Count: > 0 } itemIds)
            return itemIds.Any(id => itemCounts.ContainsKey(id) || storedItemIds.Contains(id));

        return false;
    }
}
