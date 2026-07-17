using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Hooking;
using CSAchievement = FFXIVClientStructs.FFXIV.Client.Game.UI.Achievement;

namespace RelicTerror.GameState;

// Achievement completion only becomes readable via Achievement.IsComplete once the player
// opens the Achievements window (State == Loaded). To fill relic step completion before that,
// we request per-achievement progress from the server (the same mechanism the achievement UI
// uses), which works as soon as the player is loaded. The server allows one outstanding
// request at a time, so requests are drained one round-trip per frame.
internal sealed unsafe class AchievementFetcher : IDisposable
{
    internal event Action? ProgressUpdated;

    private readonly Hook<CSAchievement.Delegates.ReceiveAchievementProgress> _hook;
    private readonly HashSet<uint> _pending  = [];
    private readonly HashSet<uint> _complete = [];

    internal AchievementFetcher()
    {
        _hook = Services.GameInteropProvider.HookFromAddress<CSAchievement.Delegates.ReceiveAchievementProgress>(
            CSAchievement.Addresses.ReceiveAchievementProgress.Value, ReceiveAchievementProgressDetour);
        _hook.Enable();
    }

    internal void Seed(IEnumerable<uint> achievementIds)
    {
        _pending.Clear();
        _complete.Clear();
        foreach (var id in achievementIds)
            if (id != 0) _pending.Add(id);
    }

    internal void Update()
    {
        if (_pending.Count == 0 || !Services.PlayerState.IsLoaded) return;

        var ach = CSAchievement.Instance();
        if (ach == null || ach->ProgressRequestState == CSAchievement.AchievementState.Requested) return;

        ach->RequestAchievementProgress(_pending.First());
    }

    internal bool IsComplete(uint achievementId)
    {
        var ach = CSAchievement.Instance();
        if (ach != null && ach->IsLoaded())
            return ach->IsComplete((int)achievementId);
        return _complete.Contains(achievementId);
    }

    private void ReceiveAchievementProgressDetour(CSAchievement* self, uint id, uint current, uint max)
    {
        _pending.Remove(id);
        if (max > 0 && current >= max)
            _complete.Add(id);
        ProgressUpdated?.Invoke();
        _hook.Original(self, id, current, max);
    }

    public void Dispose() => _hook.Dispose();
}
