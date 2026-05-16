using System;
using System.Collections.Generic;
using RelicTerror.Data;

namespace RelicTerror.State;

internal static class FloorStore
{
    internal static string EncodeKey(string seriesId, Job job) => $"{seriesId}|{job}";

    internal static Dictionary<(string SeriesId, Job Job), WeaponProgress> Hydrate(
        CharacterInfo character,
        IEnumerable<RelicSeries> allSeries)
    {
        var seed = new Dictionary<(string, Job), WeaponProgress>();
        foreach (var series in allSeries)
        foreach (var weapon in series.Weapons)
        {
            var key = EncodeKey(series.Id, weapon.Job);
            if (!character.ProgressFloors.TryGetValue(key, out var floor)) continue;

            seed[(series.Id, weapon.Job)] = new WeaponProgress(
                CompletedSteps: floor.CompletedSteps,
                TotalSteps:     weapon.Steps.Count,
                Steps:          [],
                RelicOwned:     floor.CompletedSteps == weapon.Steps.Count,
                ReplicaOwned:   floor.ReplicaOwned,
                Forms:          Array.Empty<FormOwnership>());
        }
        return seed;
    }

    internal static bool MergeAndDiff(
        CharacterInfo character,
        IReadOnlyDictionary<(string SeriesId, Job Job), WeaponProgress> latest)
    {
        var changed = false;
        foreach (var ((seriesId, job), progress) in latest)
        {
            var key = EncodeKey(seriesId, job);
            character.ProgressFloors.TryGetValue(key, out var existing);

            var nextSteps   = existing is null ? progress.CompletedSteps
                                               : System.Math.Max(existing.CompletedSteps, progress.CompletedSteps);
            var nextReplica = (existing?.ReplicaOwned ?? false) || progress.ReplicaOwned;

            if (existing is null
                || existing.CompletedSteps != nextSteps
                || existing.ReplicaOwned   != nextReplica)
            {
                character.ProgressFloors[key] = new RelicFloor
                {
                    CompletedSteps = nextSteps,
                    ReplicaOwned   = nextReplica,
                };
                changed = true;
            }
        }
        return changed;
    }
}
