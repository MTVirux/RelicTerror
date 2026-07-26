using System;
using System.Collections.Generic;
using Dalamud.Plugin.Ipc;

namespace RelicTerror.GameState;

/// <summary>
/// The Allagan Tools call gates RelicTerror reads (the plugin ships under the Dalamud internal
/// name "InventoryTools"). Allagan Tools persists every container it has ever seen, so it can
/// answer for retainers, the Free Company chest and housing storerooms that the game only keeps
/// in memory while they are open.
///
/// Every entry point reports unavailability instead of throwing: RelicTerror must behave exactly
/// as it did before when Allagan Tools is absent, stopped, or on an incompatible version.
/// </summary>
internal sealed class AllaganToolsIpc
{
    private const string IsInitializedGate  = "AllaganTools.IsInitialized";
    private const string CurrentCharGate    = "AllaganTools.CurrentCharacter";
    private const string OwnedCharsGate     = "AllaganTools.GetCharactersOwnedByActive";
    private const string CharacterItemsGate = "AllaganTools.GetCharacterItems";

    private readonly ICallGateSubscriber<bool>                       _isInitialized;
    private readonly ICallGateSubscriber<ulong>                      _currentCharacter;
    private readonly ICallGateSubscriber<bool, HashSet<ulong>>       _ownedCharacters;
    private readonly ICallGateSubscriber<ulong, HashSet<ulong[]>>    _characterItems;

    // One failed gate means the rest of this pull is untrustworthy too, so the whole pull is
    // abandoned rather than half-read. ResetDegraded reopens it for the next throttled attempt.
    private bool _degraded;

    internal AllaganToolsIpc()
    {
        var pluginInterface = Services.PluginInterface;
        _isInitialized    = pluginInterface.GetIpcSubscriber<bool>(IsInitializedGate);
        _currentCharacter = pluginInterface.GetIpcSubscriber<ulong>(CurrentCharGate);
        _ownedCharacters  = pluginInterface.GetIpcSubscriber<bool, HashSet<ulong>>(OwnedCharsGate);
        _characterItems   = pluginInterface.GetIpcSubscriber<ulong, HashSet<ulong[]>>(CharacterItemsGate);
    }

    internal bool IsAvailable => Invoke(_isInitialized.InvokeFunc, false);

    /// <summary>
    /// The gate on its own, outside a pull. It ignores the degraded latch so Allagan Tools being
    /// enabled after a failed read is noticed rather than staying written off, and it stays out of
    /// the log because the caller repeats it on a timer.
    /// </summary>
    internal bool Probe()
    {
        try
        {
            return _isInitialized.InvokeFunc();
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal ulong CurrentCharacter() => Invoke(_currentCharacter.InvokeFunc, 0UL);

    /// <summary>
    /// The active character plus everything it owns - its retainers, its Free Company and its
    /// houses. Allagan Tools scopes this itself, so no alt-character data can reach us.
    /// </summary>
    internal IReadOnlySet<ulong> OwnedCharacterIds() =>
        Invoke<IReadOnlySet<ulong>>(() => _ownedCharacters.InvokeFunc(true), new HashSet<ulong>());

    /// <remarks>
    /// Upstream indexes its inventory dictionary with First(), so an id it does not know throws
    /// across the IPC boundary. Only ids from <see cref="OwnedCharacterIds"/> may be passed.
    /// </remarks>
    internal IReadOnlyCollection<ulong[]> CharacterItems(ulong characterId) =>
        Invoke<IReadOnlyCollection<ulong[]>>(() => _characterItems.InvokeFunc(characterId), []);

    internal void ResetDegraded() => _degraded = false;

    private T Invoke<T>(Func<T> call, T fallback)
    {
        if (_degraded) return fallback;

        try
        {
            return call();
        }
        catch (Exception ex)
        {
            _degraded = true;
            Services.Log.Debug(ex, "Allagan Tools IPC unavailable; using the in-memory scan instead.");
            return fallback;
        }
    }
}
