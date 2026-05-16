using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using RelicTerror.Data.Series;

namespace RelicTerror.UI;

internal static class QuestActivityPanel
{
    private static readonly Vector4 ColorActive    = new(0.3f, 0.85f, 0.5f, 1f);
    private static readonly Vector4 ColorCompleted = new(0.5f, 0.5f,  0.5f, 1f);

    internal static void Draw(
        IReadOnlyList<(uint QuestId, string DisplayName)> activeJournalQuests)
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextDisabled("RESISTANCE JOURNAL QUESTS");

        var activeIds = new HashSet<uint>(activeJournalQuests.Select(q => q.QuestId));

        foreach (var (questId, displayName) in ResistanceJournalQuests.Quests)
        {
            var isActive = activeIds.Contains(questId);
            var color = isActive ? ColorActive : ColorCompleted;
            var marker = isActive ? "▶" : "○";
            ImGui.TextColored(color, $"  {marker} {displayName}");
        }
    }
}
