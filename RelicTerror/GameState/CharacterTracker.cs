using System;
using Dalamud.Plugin.Services;

namespace RelicTerror.GameState;

internal sealed class CharacterTracker : IDisposable
{
    private readonly IClientState _clientState;

    internal CharacterTracker(IClientState clientState)
    {
        _clientState = clientState;
        _clientState.Login  += OnLogin;
        _clientState.Logout += OnLogout;

        if (_clientState.IsLoggedIn)
            UpsertCurrentCharacter();
    }

    private void OnLogin()               => UpsertCurrentCharacter();
    private void OnLogout(int _, int __) { }

    private void UpsertCurrentCharacter()
    {
        var playerState = Services.PlayerState;
        if (!playerState.IsLoaded) return;

        var contentId = playerState.ContentId;
        if (contentId == 0) return;

        if (!Plugin.Config.Characters.TryGetValue(contentId, out var info))
        {
            info = new CharacterInfo();
            Plugin.Config.Characters[contentId] = info;
        }

        info.Name     = playerState.CharacterName;
        info.World    = playerState.HomeWorld.Value.Name.ExtractText();
        info.LastSeen = DateTime.UtcNow;

        if (Plugin.Config.SelectedCharacterId == 0)
            Plugin.Config.SelectedCharacterId = contentId;

        Plugin.Config.Save();
    }

    public void Dispose()
    {
        _clientState.Login  -= OnLogin;
        _clientState.Logout -= OnLogout;
    }
}
