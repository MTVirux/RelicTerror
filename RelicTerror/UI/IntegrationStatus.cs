using System.Collections.Generic;
using System.Numerics;

namespace RelicTerror.UI;

/// <summary>
/// One optional plugin RelicTerror co-operates with but never requires. The wording lives here
/// rather than at each draw site so the title bar tooltip and the settings window cannot drift
/// apart as the integration's behaviour changes.
/// </summary>
internal readonly record struct IntegrationStatus(string Name, bool Connected, string State, string Detail)
{
    internal Vector4 Color => Connected ? Integrations.ConnectedColor : Integrations.AbsentColor;
}

/// <summary>
/// The full roster of optional plugin integrations, in one place so a new one is added here and
/// appears in every indicator at once.
/// </summary>
internal static class Integrations
{
    internal static readonly Vector4 ConnectedColor = new(0.3f,  0.85f, 0.5f,  1f);
    internal static readonly Vector4 PartialColor   = new(0.98f, 0.75f, 0.15f, 1f);
    internal static readonly Vector4 AbsentColor    = new(0.45f, 0.45f, 0.45f, 1f);
    internal static readonly Vector4 AllGoodColor   = new(1f,    1f,    1f,    1f);

    internal static IReadOnlyList<IntegrationStatus> All(bool allaganToolsConnected) =>
        [AllaganTools(allaganToolsConnected)];

    private static IntegrationStatus AllaganTools(bool connected) => new(
        "Allagan Tools",
        connected,
        connected ? "connected" : "not detected",
        connected
            ? "Locations and item counts cover your retainers, Free Company chest and\nhousing storerooms without summoning or opening them."
            : "Falling back to inventories the game keeps loaded. Retainer bags only report\nwhile that retainer is summoned. Install Allagan Tools to search them all.");

    /// <summary>
    /// The indicator's tint. White when every integration is live: there is nothing to flag, so it
    /// reads as an ordinary title bar button. Colour is spent only on what needs attention, which
    /// is why this differs from the per-integration text colour.
    /// </summary>
    internal static Vector4 AggregateColor(IReadOnlyList<IntegrationStatus> statuses)
    {
        var connected = 0;
        foreach (var status in statuses)
            if (status.Connected) connected++;

        if (connected == 0) return AbsentColor;
        return connected == statuses.Count ? AllGoodColor : PartialColor;
    }
}
