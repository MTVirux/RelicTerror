using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace RelicTerror;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public Dictionary<ulong, CharacterInfo> Characters { get; set; } = [];
    public ulong SelectedCharacterId { get; set; }
    public (string SeriesId, Data.Job Job)? SelectedCell { get; set; }

    public bool UseLongJobNames { get; set; }
    public bool ShowExpansionColumns { get; set; } = true;
    public bool HideCharacterSelector { get; set; } = true;
    public bool OpenOnLoad { get; set; }
    public int AcknowledgedNoticeVersion { get; set; }

    public void Save() => Services.PluginInterface.SavePluginConfig(this);
}

[Serializable]
public sealed class CharacterInfo
{
    public string Name { get; set; } = string.Empty;
    public string World { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }

    public Dictionary<string, RelicFloor> ProgressFloors { get; set; } = [];

    // Achievement completion is monotonic, so persisting it lets steps resolve
    // without re-querying the server on every login.
    public HashSet<uint> CompletedAchievements { get; set; } = [];
    public DateTime LastAchievementFetch { get; set; }
}

[Serializable]
public sealed class RelicFloor
{
    public int CompletedSteps { get; set; }
    public bool ReplicaOwned { get; set; }
}
