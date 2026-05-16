using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace RelicTerror.UI;

internal static class Icons
{
    internal static ImFontPtr Font            => Services.PluginInterface.UiBuilder.FontIcon;
    internal static ImFontPtr FixedWidthFont  => Services.PluginInterface.UiBuilder.FontIconFixedWidth;

    internal static void Text(FontAwesomeIcon icon, Vector4 color)
    {
        ImGui.PushFont(Font);
        ImGui.TextColored(color, icon.ToIconString());
        ImGui.PopFont();
    }
}
