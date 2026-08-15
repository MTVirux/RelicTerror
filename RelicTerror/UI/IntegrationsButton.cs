using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace RelicTerror.UI;

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
    /// IconColor is a property, not a callback - the owning window has to repaint it from PreDraw,
    /// before ImGui.Begin lays the title bar out.
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
