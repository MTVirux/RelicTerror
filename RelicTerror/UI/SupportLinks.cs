using Dalamud.Bindings.ImGui;
using Dalamud.Utility;

namespace RelicTerror.UI;

internal static class SupportLinks
{
    private const string IssuesUrl     = "https://github.com/MTVirux/RelicTerror/issues";
    private const string DiscordHandle = "@mtvirux";

    private static double _copiedUntil;

    internal static void DrawButtons()
    {
        if (ImGui.Button("Open GitHub Issues"))
            Util.OpenLink(IssuesUrl);
        ImGui.SameLine();
        if (ImGui.Button("Copy Discord handle"))
        {
            ImGui.SetClipboardText(DiscordHandle);
            _copiedUntil = ImGui.GetTime() + 2.0;
        }
        if (ImGui.GetTime() < _copiedUntil)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("Copied!");
        }
    }
}
