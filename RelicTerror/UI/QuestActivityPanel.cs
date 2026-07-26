using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using RelicTerror.Data;

namespace RelicTerror.UI;

internal static class QuestActivityPanel
{
    private static readonly Vector4 ColorActive     = new(0.3f,  0.85f, 0.5f,  1f);
    private static readonly Vector4 ColorCompleted  = new(0.5f,  0.5f,  0.5f,  1f);
    private static readonly Vector4 ColorNotStarted = new(0.35f, 0.35f, 0.35f, 1f);

    // Space the section still occupies once collapsed: the gap above it, the
    // separator's own spacing, and the header row.
    internal static float CollapsedHeight() =>
        (ImGui.GetStyle().ItemSpacing.Y * 3f) + ImGui.GetFrameHeight();

    internal static void Draw(string seriesId, IReadOnlyList<JournalQuestStatus> journalQuests)
    {
        ImGui.Spacing();
        ImGui.Separator();

        if (!DrawHeader($"{seriesId.ToUpperInvariant()} JOURNAL QUESTS"))
            return;

        foreach (var status in journalQuests)
        {
            var (marker, color) =
                status.IsAccepted ? ("▶", ColorActive) :
                status.IsComplete ? ("✓", ColorCompleted) :
                                    ("○", ColorNotStarted);
            var tag = status.Quest.Repeatable ? "  (repeatable)" : "";
            ImGui.TextColored(color, $"  {marker} {status.Quest.DisplayName}{tag}");
        }
    }

    private static bool DrawHeader(string label)
    {
        ImGui.SetNextItemOpen(Plugin.Config.ExpandJournalQuests, ImGuiCond.Always);

        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
        var expanded = ImGui.TreeNodeEx(
            label,
            ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanAvailWidth);
        ImGui.PopStyleColor();

        if (expanded != Plugin.Config.ExpandJournalQuests)
        {
            Plugin.Config.ExpandJournalQuests = expanded;
            Plugin.Config.Save();
        }

        return expanded;
    }
}
