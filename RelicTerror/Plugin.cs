using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using RelicTerror.Data;
using RelicTerror.Data.Series;
using RelicTerror.GameState;
using RelicTerror.State;
using RelicTerror.UI;

namespace RelicTerror;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/rt";

    internal static Configuration Config { get; private set; } = null!;

    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly CharacterTracker _characterTracker;
    private readonly ProgressReader   _progressReader;
    private readonly MainWindow       _mainWindow;
    private readonly ConfigWindow     _configWindow;

    private IReadOnlyDictionary<(string, Job), WeaponProgress> _progressCache
        = new Dictionary<(string, Job), WeaponProgress>();

    // Retainer inventories only return data while a retainer is summoned, and the load
    // fires a burst of per-slot events. Coalesce them into one rebuild on the next frame.
    private bool _rebuildPending;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
        pluginInterface.Create<Services>(pluginInterface);
        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        MigrateConfig();

        _progressReader   = new ProgressReader();
        _characterTracker = new CharacterTracker(Services.ClientState);
        _mainWindow       = new MainWindow(GetProgress, GetActiveJournalQuests, _progressReader.FindItemLocation) { IsOpen = Config.OpenOnLoad };
        _configWindow     = new ConfigWindow();

        Services.ClientState.Login              += OnLogin;
        Services.UnlockState.Unlock             += OnUnlock;
        Services.GameInventory.InventoryChanged += OnInventoryChanged;
        Services.Framework.Update               += OnFrameworkUpdate;
        Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the RelicTerror tracker window. Use \"/rt config\" to open settings.",
        });

        pluginInterface.UiBuilder.Draw         += _mainWindow.Draw;
        pluginInterface.UiBuilder.Draw         += _configWindow.Draw;
        pluginInterface.UiBuilder.OpenMainUi   += OpenMainUi;
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;

        if (Services.ClientState.IsLoggedIn)
            RebuildCache();

        CompletionItemIdAudit.Run();
        AchievementIdAudit.Run();
    }

    // v3: ResistanceSeries achievement order was corrected — drop any persisted floors
    // since the regression guard would otherwise keep stale values pinned.
    // v4: Relic-tracking semantics revamp — strict identifier priority replaces the
    // old any-of rule. No data migration required; existing CompletedSteps floors
    // remain valid high-water marks under the new logic.
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

    private IReadOnlyList<(uint QuestId, string DisplayName)> GetActiveJournalQuests()
    {
        var active = new List<(uint, string)>();
        foreach (var q in ResistanceJournalQuests.Quests)
        {
            if (_progressReader.IsQuestAccepted(q.QuestId))
                active.Add((q.QuestId, q.DisplayName));
        }
        return active;
    }

    private void OnLogin()
    {
        // Different character: drop the previous floor and seed from the new character's
        // persisted high-water mark (if any). ContentId may not be loaded yet on the very
        // first Login tick — a later inventory/unlock event will rebuild and resave.
        _progressCache = TryHydrateFromPersistedFloors();
        RebuildCache();
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
        if (!_rebuildPending) return;
        _rebuildPending = false;
        RebuildCache();
    }
    private void OnCommand(string _, string args)
    {
        if (args.Trim().Equals("config", System.StringComparison.OrdinalIgnoreCase))
            _configWindow.Toggle();
        else
            _mainWindow.Toggle();
    }
    private void OpenMainUi()   => _mainWindow.IsOpen   = true;
    private void OpenConfigUi() => _configWindow.IsOpen = true;

    private void RebuildCache()
    {
        var itemCounts    = _progressReader.ReadItemCounts();
        var dresserItems  = _progressReader.ReadGlamourDresserItemIds();
        var armoireItems  = _progressReader.ReadArmoireItemIds();
        var storedItemIds = new HashSet<uint>(dresserItems);
        storedItemIds.UnionWith(armoireItems);

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
                    _progressReader.IsAchievementComplete,
                    storedItemIds,
                    floor);
        }

        _progressCache = newCache;
        PersistFloors(newCache);
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
        _pluginInterface.UiBuilder.Draw         -= _mainWindow.Draw;
        _pluginInterface.UiBuilder.Draw         -= _configWindow.Draw;
        _pluginInterface.UiBuilder.OpenMainUi   -= OpenMainUi;
        _pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        _mainWindow.Dispose();
        _configWindow.Dispose();
        _characterTracker.Dispose();
    }
}
