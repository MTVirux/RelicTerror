using System.Collections.Generic;
using RelicTerror.Data;

namespace RelicTerror.State;

public sealed record ItemTotalRow(
    uint ItemId,
    string ItemName,
    int Remaining,
    int Total,
    int Held);

public sealed record SeriesTotals(
    string SeriesId,
    string SeriesName,
    int WeaponsRemaining,
    int WeaponsTotal,
    IReadOnlyList<ItemTotalRow> Rows);

public static class ItemTotals
{
    /// <summary>
    /// Aggregates per-series material requirements. <paramref name="progress"/> only affects
    /// the Remaining column; Total is derived from the database alone so the window still
    /// renders before any cache exists (fresh character, or logged out).
    /// </summary>
    public static IReadOnlyList<SeriesTotals> Build(
        IReadOnlyList<RelicSeries> allSeries,
        IReadOnlyDictionary<(string, Job), WeaponProgress> progress,
        IReadOnlyDictionary<uint, int> itemCounts)
    {
        var result = new List<SeriesTotals>(allSeries.Count);

        foreach (var series in allSeries)
        {
            // Insertion-ordered so rows read in relic-stage order rather than alphabetically.
            var order   = new List<uint>();
            var totals  = new Dictionary<uint, int>();
            var pending = new Dictionary<uint, int>();
            var names   = new Dictionary<uint, string>();

            var weaponsRemaining = 0;

            foreach (var weapon in series.Weapons)
            {
                progress.TryGetValue((series.Id, weapon.Job), out var weaponProgress);
                if (weaponProgress is not { RelicOwned: true })
                    weaponsRemaining++;

                for (var i = 0; i < weapon.Steps.Count; i++)
                {
                    // No cached progress means nothing is proven complete, so every step counts
                    // toward Remaining.
                    var stepComplete = weaponProgress is not null
                        && i < weaponProgress.Steps.Count
                        && weaponProgress.Steps[i].IsComplete;

                    foreach (var req in weapon.Steps[i].Requirements)
                    {
                        if (!totals.ContainsKey(req.ItemId))
                        {
                            order.Add(req.ItemId);
                            totals[req.ItemId]  = 0;
                            pending[req.ItemId] = 0;
                            names[req.ItemId]   = req.ItemName;
                        }

                        totals[req.ItemId] += req.RequiredCount;
                        if (!stepComplete)
                            pending[req.ItemId] += req.RequiredCount;
                    }
                }
            }

            var rows = new List<ItemTotalRow>(order.Count);
            foreach (var itemId in order)
            {
                rows.Add(new ItemTotalRow(
                    itemId,
                    names[itemId],
                    pending[itemId],
                    totals[itemId],
                    itemCounts.TryGetValue(itemId, out var held) ? held : 0));
            }

            result.Add(new SeriesTotals(
                series.Id,
                series.Name,
                weaponsRemaining,
                series.Weapons.Count,
                rows));
        }

        return result;
    }
}
