using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using RelicTerror.Data;
using RelicTerror.GameState;
using RelicTerror.State;
using RelicTerror.UI;

namespace RelicTerror;

public enum ResetScope { Current, All }

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/rt";

    // Audit is a dev aid - kept out of Release builds.
#if DEBUG
    private const string HelpText  = "Open the RelicTerror tracker window. \"/rt config\" for settings, \"/rt refetch\" to re-pull achievements, \"/rt audit\" to re-check tracked IDs against game data.";
    private const string KnownArgs = "config, refetch, audit";
#else
    private const string HelpText  = "Open the RelicTerror tracker window. \"/rt config\" for settings, \"/rt refetch\" to re-pull achievements.";
    private const string KnownArgs = "config, refetch";
#endif

    internal static Configuration Config { get; private set; } = null!;

    // Allagan Tools allocates per item stack per owner on each pull and inventory events
    // arrive in bursts, so the snapshot is throttled.
    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromSeconds(2);

    // Presence is a single cheap bool call, so it polls independently of snapshot pulls.
    private static readonly TimeSpan AvailabilityInterval = TimeSpan.FromSeconds(1);

    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly WindowSystem       _windowSystem = new("RelicTerror");
    private readonly CharacterTracker   _characterTracker;
    private readonly ProgressReader     _progressReader;
    private readonly AchievementFetcher _achievementFetcher;
    private readonly AllaganToolsIpc    _allaganTools;
    private readonly MainWindow         _mainWindow;
    private readonly ConfigWindow       _configWindow;
    private readonly ItemTotalsWindow   _itemTotalsWindow;
    private readonly FirstRunNotice     _firstRunNotice;
#if DEBUG
    private readonly AuditWindow        _auditWindow;
#endif

    private IReadOnlyDictionary<(string, Job), WeaponProgress> _progressCache
        = new Dictionary<(string, Job), WeaponProgress>();

    // Reused by the item-totals window so it does not rescan every bag each frame.
    private IReadOnlyDictionary<uint, int> _itemCounts = new Dictionary<uint, int>();

    // Retainer loads fire a burst of per-slot events - coalesce into one rebuild next frame.
    private bool _rebuildPending;

    // ContentId may not be loaded on the Login tick, so hydration waits for the frame loop.
    private bool _achievementHydratePending;
    private bool _achievementFetchCompleted;

    private DateTime _lastSnapshotPull;
    private bool     _snapshotRefreshSkipped;

    private DateTime _lastAvailabilityCheck;
    private bool     _allaganToolsAvailable;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
        pluginInterface.Create<Services>(pluginInterface);
        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        MigrateConfig();

        _progressReader     = new ProgressReader();
        _characterTracker   = new CharacterTracker(Services.ClientState);
        _achievementFetcher = new AchievementFetcher();
        _allaganTools       = new AllaganToolsIpc();
        _achievementFetcher.ProgressUpdated += OnAchievementProgressUpdated;
        _achievementFetcher.FetchCompleted  += OnAchievementFetchCompleted;
        _mainWindow         = new MainWindow(GetProgress, GetJournalQuestStatuses, GetLocationLookup, IsWeaponResolving, () => _allaganToolsAvailable, OpenConfigUi, OpenItemTotalsUi) { IsOpen = Config.OpenOnLoad };
        _configWindow       = new ConfigWindow(ResetFloors, SeedAchievementFetch, () => _allaganToolsAvailable);
        _itemTotalsWindow   = new ItemTotalsWindow(() => _progressCache, () => _itemCounts);
        _firstRunNotice     = new FirstRunNotice();
        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_itemTotalsWindow);
#if DEBUG
        _auditWindow        = new AuditWindow(DataAudit.Run(), DataAudit.Run);
        _windowSystem.AddWindow(_auditWindow);
#endif

        Services.ClientState.Login              += OnLogin;
        Services.UnlockState.Unlock             += OnUnlock;
        Services.GameInventory.InventoryChanged += OnInventoryChanged;
        Services.Framework.Update               += OnFrameworkUpdate;
        Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = HelpText,
        });

        pluginInterface.UiBuilder.Draw         += _windowSystem.Draw;
        pluginInterface.UiBuilder.Draw         += _configWindow.Draw;
        pluginInterface.UiBuilder.Draw         += _firstRunNotice.Draw;
        pluginInterface.UiBuilder.OpenMainUi   += OpenMainUi;
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;

        if (Services.ClientState.IsLoggedIn)
            RebuildCache();

        _achievementHydratePending = true;
    }

    // v3: Resistance achievement order changed - stale floors must go or the regression guard
    // pins them forever. v4: identifier-priority revamp, existing floors stay valid.
    private static void MigrateConfig()
    {
        if (Config.Version >= 4) return;

        if (Config.Version < 3)
        {
            foreach (var info in Config.Characters.Values)
            {
                var stale = info.ProgressFloors.Keys
                    .Where(k => k.StartsWith("Resistance|"))
                    .ToList();
                foreach (var key in stale)
                    info.ProgressFloors.Remove(key);
            }
        }

        Config.Version = 4;
        Config.Save();
    }

    private IReadOnlyDictionary<(string, Job), WeaponProgress> GetProgress(ulong _) => _progressCache;

    private LocationLookup GetLocationLookup() =>
        new(_progressReader.FindItemLocation, _progressReader.CoversAllStorage);

    private static Dictionary<(string, Job), uint[]>? _weaponAchievementIds;

    private static Dictionary<(string, Job), uint[]> WeaponAchievementIds
    {
        get
        {
            if (_weaponAchievementIds is not null) return _weaponAchievementIds;

            _weaponAchievementIds = [];
            foreach (var series in RelicDatabase.AllSeries)
            foreach (var weapon in series.Weapons)
            {
                var ids = weapon.Steps
                    .Where(s => s.CompletionQuestId is null && s.AchievementId is not null)
                    .Select(s => s.AchievementId!.Value)
                    .Distinct()
                    .ToArray();
                if (ids.Length > 0) _weaponAchievementIds[(series.Id, weapon.Job)] = ids;
            }
            return _weaponAchievementIds;
        }
    }

    // Quest-identified steps are excluded: their flags are memory-resident, so only pending
    // achievement round-trips can leave a cell unresolved.
    private bool IsWeaponResolving((string SeriesId, Job Job) cell)
    {
        if (!WeaponAchievementIds.TryGetValue(cell, out var ids)) return false;

        foreach (var id in ids)
            if (_achievementFetcher.IsAwaiting(id)) return true;

        return false;
    }

    private IReadOnlyList<JournalQuestStatus> GetJournalQuestStatuses(string seriesId)
    {
        var series = RelicDatabase.AllSeries.FirstOrDefault(s => s.Id == seriesId);
        if (series is null) return [];

        var statuses = new List<JournalQuestStatus>(series.JournalQuests.Count);
        foreach (var q in series.JournalQuests)
        {
            statuses.Add(new JournalQuestStatus(
                q,
                _progressReader.IsQuestAccepted(q.QuestId),
                _progressReader.IsQuestComplete(q.QuestId)));
        }
        return statuses;
    }

    private void OnLogin()
    {
        // ContentId may not be loaded on the first Login tick - a later inventory/unlock
        // event rebuilds and resaves.
        _progressCache = TryHydrateFromPersistedFloors();

        // The previous character's storage is meaningless now; force a pull past the throttle.
        _lastSnapshotPull        = default;
        _progressReader.Snapshot = null;
        RebuildCache();

        // Achievement completion is per-character.
        _achievementHydratePending = true;
    }

    private void OnAchievementProgressUpdated() => _rebuildPending = true;

    // Raised from the receive detour; defer config writes to the framework thread.
    private void OnAchievementFetchCompleted() => _achievementFetchCompleted = true;

    private void SeedAchievementFetch()
    {
        var ids = new HashSet<uint>();
        foreach (var series in RelicDatabase.AllSeries)
        foreach (var weapon in series.Weapons)
        foreach (var step in weapon.Steps)
            if (step.AchievementId is { } id)
                ids.Add(id);
        _achievementFetcher.Seed(ids);
    }

    private Dictionary<(string, Job), WeaponProgress> TryHydrateFromPersistedFloors()
    {
        var contentId = Services.PlayerState.IsLoaded ? Services.PlayerState.ContentId : 0UL;
        if (contentId == 0 || !Config.Characters.TryGetValue(contentId, out var info))
            return new Dictionary<(string, Job), WeaponProgress>();

        return State.FloorStore.Hydrate(info, RelicDatabase.AllSeries);
    }
    private void OnUnlock(Lumina.Excel.RowRef _) => RebuildCache();
    private void OnInventoryChanged(IReadOnlyCollection<InventoryEventArgs> _) => _rebuildPending = true;
    private void OnFrameworkUpdate(IFramework _)
    {
        if (_achievementHydratePending)
            TryHydrateAchievements();

        _achievementFetcher.Update();

        if (_achievementFetchCompleted)
        {
            _achievementFetchCompleted = false;
            PersistFetchedAchievements();
        }

        PollAllaganTools();

        if (_snapshotRefreshSkipped && DateTime.UtcNow - _lastSnapshotPull >= SnapshotInterval)
            _rebuildPending = true;

        if (!_rebuildPending) return;
        _rebuildPending = false;
        RebuildCache();
    }

    private void TryHydrateAchievements()
    {
        if (!Services.PlayerState.IsLoaded) return;
        var contentId = Services.PlayerState.ContentId;
        if (contentId == 0) return;

        _achievementHydratePending = false;
        Config.Characters.TryGetValue(contentId, out var info);
        _achievementFetcher.ResetForCharacter(info?.CompletedAchievements ?? []);
        if (info?.CompletedAchievements.Count > 0)
            _rebuildPending = true;

        // Fetch once per character ever; afterwards only the manual re-fetch pulls again.
        if (info is null || info.LastAchievementFetch == default)
            SeedAchievementFetch();
    }

    private void PersistFetchedAchievements()
    {
        if (!Services.PlayerState.IsLoaded) return;
        var contentId = Services.PlayerState.ContentId;
        if (contentId == 0 || !Config.Characters.TryGetValue(contentId, out var info)) return;

        info.CompletedAchievements.UnionWith(_achievementFetcher.CompletedIds);
        info.LastAchievementFetch = DateTime.UtcNow;
        Config.Save();
    }
    private void OnCommand(string _, string args)
    {
        var arg = args.Trim();
        if (arg.Length == 0)
            _mainWindow.Toggle();
        else if (arg.Equals("config", System.StringComparison.OrdinalIgnoreCase))
            _configWindow.Toggle();
        else if (arg.Equals("refetch", System.StringComparison.OrdinalIgnoreCase))
            SeedAchievementFetch();
#if DEBUG
        else if (arg.Equals("audit", System.StringComparison.OrdinalIgnoreCase))
            OpenAuditUi();
#endif
        else
            Services.Chat.Print($"RelicTerror: unknown argument \"{arg}\". Try: {KnownArgs}.");
    }
    private void OpenMainUi()       => _mainWindow.IsOpen       = true;
    private void OpenConfigUi()     => _configWindow.IsOpen     = true;
    private void OpenItemTotalsUi() => _itemTotalsWindow.IsOpen = true;

#if DEBUG
    // Re-run so the report reflects game data as it stands now.
    private void OpenAuditUi()
    {
        _auditWindow.Rerun();
        _auditWindow.IsOpen = true;
    }
#endif

    // Allagan Tools can load or unload mid-session, so presence is polled; a change either
    // direction invalidates the counts.
    private void PollAllaganTools()
    {
        var now = DateTime.UtcNow;
        if (now - _lastAvailabilityCheck < AvailabilityInterval) return;
        _lastAvailabilityCheck = now;

        var available = _allaganTools.Probe();
        if (available == _allaganToolsAvailable) return;

        _allaganToolsAvailable = available;

        // Drop its view of storage now so the fallback never serves counts from a gone plugin.
        if (!available) _progressReader.Snapshot = null;

        _rebuildPending = true;
    }

    // When Allagan Tools answers it is the sole count source - merging with the resident scan
    // would double-count, and it reads the same memory anyway.
    private void RefreshInventorySnapshot()
    {
        var now = DateTime.UtcNow;
        if (now - _lastSnapshotPull < SnapshotInterval)
        {
            // The triggering change has not reached the snapshot yet - rebuild once the throttle
            // expires, or it is lost until some unrelated later event.
            _snapshotRefreshSkipped = true;
            return;
        }

        _lastSnapshotPull       = now;
        _snapshotRefreshSkipped = false;

        _allaganTools.ResetDegraded();
        _progressReader.Snapshot = InventorySnapshot.TryBuild(_allaganTools);
    }

    private void RebuildCache()
    {
        RefreshInventorySnapshot();

        var snapshot      = _progressReader.Snapshot;
        var itemCounts    = snapshot?.Counts ?? _progressReader.ReadItemCounts();
        var dresserItems  = _progressReader.ReadGlamourDresserItemIds();
        var armoireItems  = _progressReader.ReadArmoireItemIds();
        var storedItemIds = new HashSet<uint>(dresserItems);
        storedItemIds.UnionWith(armoireItems);
        if (snapshot is not null)
            storedItemIds.UnionWith(snapshot.StoredItemIds);

        var newCache = new Dictionary<(string, Job), WeaponProgress>();

        foreach (var series in RelicDatabase.AllSeries)
        foreach (var weapon in series.Weapons)
        {
            var key = (series.Id, weapon.Job);
            _progressCache.TryGetValue(key, out var floor);

            newCache[key] =
                ProgressCache.ComputeWeaponProgress(
                    weapon,
                    itemCounts,
                    _achievementFetcher.IsComplete,
                    storedItemIds,
                    _progressReader.IsQuestComplete,
                    floor);
        }

        _progressCache = newCache;
        _itemCounts    = itemCounts;
        PersistFloors(newCache);
    }

    private void ResetFloors(ResetScope scope)
    {
        if (scope == ResetScope.All)
        {
            foreach (var info in Config.Characters.Values)
                info.ProgressFloors.Clear();
        }
        else
        {
            if (!Services.PlayerState.IsLoaded) return;
            var contentId = Services.PlayerState.ContentId;
            if (contentId == 0 || !Config.Characters.TryGetValue(contentId, out var info)) return;
            info.ProgressFloors.Clear();
        }

        Config.Save();
        _progressCache = new Dictionary<(string, Job), WeaponProgress>();
        RebuildCache();
    }

    private void PersistFloors(IReadOnlyDictionary<(string, Job), WeaponProgress> latest)
    {
        if (!Services.PlayerState.IsLoaded) return;
        var contentId = Services.PlayerState.ContentId;
        if (contentId == 0 || !Config.Characters.TryGetValue(contentId, out var info)) return;

        if (State.FloorStore.MergeAndDiff(info, latest))
            Config.Save();
    }

    public void Dispose()
    {
        Services.CommandManager.RemoveHandler(CommandName);
        Services.ClientState.Login              -= OnLogin;
        Services.UnlockState.Unlock             -= OnUnlock;
        Services.GameInventory.InventoryChanged -= OnInventoryChanged;
        Services.Framework.Update               -= OnFrameworkUpdate;
        _pluginInterface.UiBuilder.Draw         -= _windowSystem.Draw;
        _pluginInterface.UiBuilder.Draw         -= _configWindow.Draw;
        _pluginInterface.UiBuilder.Draw         -= _firstRunNotice.Draw;
        _windowSystem.RemoveAllWindows();
        _pluginInterface.UiBuilder.OpenMainUi   -= OpenMainUi;
        _pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        _achievementFetcher.ProgressUpdated -= OnAchievementProgressUpdated;
        _achievementFetcher.FetchCompleted  -= OnAchievementFetchCompleted;
        _achievementFetcher.Dispose();
        _mainWindow.Dispose();
        _configWindow.Dispose();
        _itemTotalsWindow.Dispose();
#if DEBUG
        _auditWindow.Dispose();
#endif
        _characterTracker.Dispose();
    }
}
