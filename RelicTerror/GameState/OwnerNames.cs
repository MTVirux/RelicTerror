using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CSRetainerManager = FFXIVClientStructs.FFXIV.Client.Game.RetainerManager;

namespace RelicTerror.GameState;

/// <summary>
/// Puts a name to an inventory owner id.
///
/// The game's retainer roster is ten fixed slots that read as zero until it has loaded this
/// session, which <c>RetainerManager.IsReady</c> reports. Allagan Tools' inventory cache
/// persists across sessions, so it lists retainer-held items from the moment you log in - long
/// before the game knows any names. Reading only the roster therefore misses on exactly the
/// lookups this feature exists to answer.
///
/// Allagan Tools keeps its own persisted name map, so that is the durable source. The live
/// roster is still preferred when it is ready, so a retainer renamed this session is correct
/// before Allagan Tools next writes its config.
/// </summary>
internal static class OwnerNames
{
    private const string AllaganToolsConfig = "InventoryTools.json";

    // Only ever re-read when the file's timestamp moves; this is just the floor on how often
    // that timestamp is checked, since lookups happen per placement per frame.
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private static Dictionary<ulong, string> _saved = [];
    private static DateTime _lastCheck;
    private static DateTime _lastWrite;
    private static bool     _checkedOnce;

    internal static string? Retainer(ulong ownerId)
    {
        if (ownerId == 0) return null;
        return FromRoster(ownerId) ?? FromSavedCharacters(ownerId);
    }

    private static unsafe string? FromRoster(ulong ownerId)
    {
        var manager = CSRetainerManager.Instance();
        if (manager == null || !manager->IsReady) return null;

        foreach (ref var retainer in manager->Retainers)
        {
            if (retainer.RetainerId != ownerId) continue;

            var name = retainer.NameString;
            return string.IsNullOrEmpty(name) ? null : name;
        }

        return null;
    }

    private static string? FromSavedCharacters(ulong ownerId)
    {
        EnsureLoaded();
        return _saved.TryGetValue(ownerId, out var name) ? name : null;
    }

    private static void EnsureLoaded()
    {
        var now = DateTime.UtcNow;
        if (_checkedOnce && now - _lastCheck < CheckInterval) return;

        _lastCheck   = now;
        _checkedOnce = true;

        try
        {
            var path = ConfigPath();
            if (path is null || !File.Exists(path)) return;

            var written = File.GetLastWriteTimeUtc(path);
            if (written == _lastWrite) return;
            _lastWrite = written;

            _saved = ParseSavedCharacters(path);
        }
        catch (Exception ex)
        {
            Services.Log.Debug(ex, "Could not read Allagan Tools' saved character names.");
        }
    }

    // Allagan Tools' config sits beside our own in the launcher's pluginConfigs directory;
    // deriving the path from ours keeps this working for portable and non-XIVLauncher installs.
    private static string? ConfigPath()
    {
        var directory = Services.PluginInterface.ConfigFile.Directory?.FullName;
        return directory is null ? null : Path.Combine(directory, AllaganToolsConfig);
    }

    private static Dictionary<ulong, string> ParseSavedCharacters(string path)
    {
        var names = new Dictionary<ulong, string>();

        // Shared read: Allagan Tools may be writing this file while we look at it.
        using var stream   = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty("SavedCharacters", out var saved)
            || saved.ValueKind != JsonValueKind.Object)
            return names;

        foreach (var entry in saved.EnumerateObject())
        {
            if (!ulong.TryParse(entry.Name, out var ownerId) || ownerId == 0) continue;
            if (entry.Value.ValueKind != JsonValueKind.Object) continue;
            if (!entry.Value.TryGetProperty("Name", out var nameProperty)) continue;

            var name = nameProperty.GetString();
            if (!string.IsNullOrEmpty(name)) names[ownerId] = name;
        }

        return names;
    }
}
