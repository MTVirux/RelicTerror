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

    internal static void Draw(string seriesId, IReadOnlyList<JournalQuestStatus> journalQuests)
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextDisabled($"{seriesId.ToUpperInvariant()} JOURNAL QUESTS");

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
}
