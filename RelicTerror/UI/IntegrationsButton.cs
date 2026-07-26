using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace RelicTerror.UI;

/// <summary>
/// The chain in the title bar: which optional plugins RelicTerror is co-operating with right now.
/// Hovering says what each one is doing and what is lost without it, so the icon carries its whole
/// meaning without being clicked.
/// </summary>
internal static class IntegrationsButton
{
    internal static TitleBarButton Build(Func<bool> allaganToolsConnected) => new()
    {
        Icon        = FontAwesomeIcon.Link,
        IconOffset  = new Vector2(2, 2),
        IconColor   = Integrations.AggregateColor(Integrations.All(allaganToolsConnected())),
        // A read-only indicator, but Dalamud invokes Click unconditionally, so it cannot be null.
        Click       = _ => { },
        ShowTooltip = () => DrawTooltip(allaganToolsConnected()),
    };

    /// <summary>
    /// The icon colour is a property rather than a callback, so the owning window repaints it from
    /// PreDraw - before ImGui.Begin lays the title bar out - to keep it in step with the tooltip.
    /// </summary>
    internal static void Refresh(TitleBarButton button, bool allaganToolsConnected) =>
        button.IconColor = Integrations.AggregateColor(Integrations.All(allaganToolsConnected));

    private static void DrawTooltip(bool allaganToolsConnected)
    {
        ImGui.BeginTooltip();

        ImGui.TextUnformatted("Plugin integrations");
        ImGui.Separator();

        foreach (var integration in Integrations.All(allaganToolsConnected))
        {
            Icons.Text(FontAwesomeIcon.Circle, integration.Color);
            ImGui.SameLine();
            ImGui.TextColored(integration.Color, $"{integration.Name} - {integration.State}");

            ImGui.Indent();
            ImGui.TextDisabled(integration.Detail);
            ImGui.Unindent();
        }

        ImGui.EndTooltip();
    }
}
