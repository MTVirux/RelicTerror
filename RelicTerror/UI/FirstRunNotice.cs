using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Utility;

namespace RelicTerror.UI;

internal sealed class FirstRunNotice
{
    // Bump to re-show the notice to users who acknowledged an older version.
    private const int NoticeVersion = 1;

    private const string PopupId       = "RelicTerror - Heads up!###RelicTerror.FirstRunNotice";
    private const string IssuesUrl     = "https://github.com/MTVirux/RelicTerror/issues";
    private const string DiscordHandle = "@mtvirux";

    private double _copiedUntil;

    internal void Draw()
    {
        if (Plugin.Config.AcknowledgedNoticeVersion >= NoticeVersion) return;

        if (!ImGui.IsPopupOpen(PopupId))
            ImGui.OpenPopup(PopupId);

        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        var open = true;
        if (ImGui.BeginPopupModal(PopupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.TextColored(new Vector4(1f, 0.75f, 0.35f, 1f), "This plugin is highly experimental!");
            ImGui.Spacing();
            ImGui.TextUnformatted(
                "Not all of RelicTerror has been thoroughly tested yet, so bug\n" +
                "reports and suggestions are highly appreciated!\n" +
                "\n" +
                "Feel free to reach out to @mtvirux on Discord for assistance,\n" +
                "or submit a GitHub issue.");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

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

            ImGui.Spacing();
            if (ImGui.Button("Got it", new Vector2(120, 0)))
            {
                Acknowledge();
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        // Titlebar X: BeginPopupModal returns false with open flipped to false,
        // and the popup is already closed - just persist the acknowledgement.
        if (!open)
            Acknowledge();
    }

    private static void Acknowledge()
    {
        Plugin.Config.AcknowledgedNoticeVersion = NoticeVersion;
        Plugin.Config.Save();
    }
}
