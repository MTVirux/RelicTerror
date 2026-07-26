using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using RelicTerror.Data;
using RelicTerror.GameState;
using RelicTerror.State;

namespace RelicTerror.UI;

internal static class DetailPanel
{
    private static readonly Vector4 ColorComplete = new(0.3f,  0.85f, 0.5f,  1f);
    private static readonly Vector4 ColorReplica  = new(0.72f, 0.5f,  0.95f, 1f);
    private static readonly Vector4 ColorCurrent  = new(0.98f, 0.75f, 0.15f, 1f);
    private static readonly Vector4 ColorDimmed   = new(0.45f, 0.45f, 0.45f, 1f);
    private static readonly Vector4 ColorPartial  = new(0.98f, 0.75f, 0.15f, 1f);
    private static readonly Vector4 ColorMissing  = new(0.95f, 0.35f, 0.35f, 1f);

    internal static void Draw(
        (string SeriesId, Job Job) cell,
        WeaponProgress progress,
        IReadOnlyList<JournalQuestStatus> journalQuests,
        Func<uint, ProgressReader.ItemLocation?> findItemLocation)
    {
        ImGui.TextColored(ColorCurrent, $"{cell.Job} — {cell.SeriesId} Weapon");
        ImGui.Separator();

        var showQuestPanel = journalQuests.Count > 0;
        var questsExpanded = showQuestPanel && Plugin.Config.ExpandJournalQuests;

        var avail = ImGui.GetContentRegionAvail().Y;
        var topAreaHeight =
            !showQuestPanel ? 0f :
            questsExpanded  ? avail * 0.55f :
                              MathF.Max(avail - QuestActivityPanel.CollapsedHeight(), 1f);

        ImGui.BeginChild("##detailtop", new Vector2(0, topAreaHeight), false);

        var halfWidth = ImGui.GetContentRegionAvail().X * 0.48f;

        ImGui.BeginChild("##steps", new Vector2(halfWidth, 0), false);
        DrawStepList(progress, findItemLocation);
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##items", new Vector2(0, 0), false);
        DrawItemRequirements(progress, findItemLocation);
        ImGui.EndChild();

        ImGui.EndChild();

        if (!showQuestPanel)
            return;

        // Collapsed, the section is only its header, so it needs no scroll child.
        if (questsExpanded)
        {
            ImGui.BeginChild("##questactivity", new Vector2(0, 0), false);
            QuestActivityPanel.Draw(cell.SeriesId, journalQuests);
            ImGui.EndChild();
        }
        else
        {
            QuestActivityPanel.Draw(cell.SeriesId, journalQuests);
        }
    }

    private static void DrawStepList(WeaponProgress progress, Func<uint, ProgressReader.ItemLocation?> findItemLocation)
    {
        ImGui.TextDisabled("STEPS");

        var fraction = progress.TotalSteps > 0
            ? (float)progress.CompletedSteps / progress.TotalSteps
            : 0f;
        ImGui.ProgressBar(fraction, new Vector2(-1, 6), string.Empty);
        ImGui.TextColored(ColorComplete, $"{progress.CompletedSteps} / {progress.TotalSteps} completed");
        ImGui.Spacing();

        for (var i = 0; i < progress.Steps.Count; i++)
            DrawStepRow(progress, i, findItemLocation);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextDisabled("COLLECTION");
        DrawCollectionRow(
            progress.RelicOwned,
            ColorComplete,
            progress.RelicOwned ? "Relic owned" : "Relic not acquired",
            progress.RelicItemIds,
            "Final relic weapon not tracked for this series.",
            findItemLocation);
        DrawCollectionRow(
            progress.ReplicaOwned,
            ColorReplica,
            progress.ReplicaOwned ? "Replica owned" : "Replica not acquired",
            progress.ReplicaItemId is { } replicaId ? [replicaId] : [],
            "This series has no replica weapon.",
            findItemLocation);
    }

    private static void DrawCollectionRow(
        bool owned,
        Vector4 ownedColor,
        string label,
        IReadOnlyList<uint> itemIds,
        string untrackedNote,
        Func<uint, ProgressReader.ItemLocation?> findItemLocation)
    {
        var color = owned ? ownedColor : ColorDimmed;

        ImGui.BeginGroup();
        DrawIconLabel(owned ? FontAwesomeIcon.Check : FontAwesomeIcon.Circle, color, label);
        ImGui.EndGroup();

        var searchId = ItemSearch.FirstId(itemIds);
        if (ItemSearch.Row(searchId))
            DrawLocationTooltip(owned, itemIds, untrackedNote, findItemLocation, searchId != 0);
    }

    private static void DrawLocationTooltip(
        bool owned,
        IReadOnlyList<uint> itemIds,
        string untrackedNote,
        Func<uint, ProgressReader.ItemLocation?> findItemLocation,
        bool searchable)
    {
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 24f);
        ImGui.TextDisabled("LOCATION");

        if (itemIds.Count == 0)
        {
            ImGui.TextUnformatted(untrackedNote);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
            return;
        }

        var located = false;
        foreach (var id in itemIds)
        {
            var loc = findItemLocation(id);
            if (loc is null) continue;

            ImGui.TextUnformatted(loc.ItemName);
            ImGui.SameLine();

            if (loc.BagLabel is { } bag)
            {
                ImGui.TextColored(ColorComplete, $"— {bag}");
                located = true;
            }
            else
            {
                ImGui.TextColored(ColorDimmed, "— not in a tracked location");
            }
        }

        if (!located)
        {
            ImGui.Spacing();
            ImGui.TextColored(ColorDimmed, owned
                ? "Counted from an achievement or an earlier session. Retainer bags only report while that retainer is summoned."
                : "Searched inventory, Armoury Chest, saddlebags, summoned retainers, Glamour Dresser, and Armoire.");
        }

        if (searchable)
        {
            ImGui.Spacing();
            ItemSearch.Hint();
        }

        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    private static void DrawItemRequirements(WeaponProgress progress, Func<uint, ProgressReader.ItemLocation?> findItemLocation)
    {
        ImGui.TextDisabled("ITEMS");

        for (var i = 0; i < progress.Steps.Count; i++)
        {
            var detail = progress.Steps[i];
            DrawStepRow(progress, i, findItemLocation);

            ImGui.Indent();

            if (detail.Step.Requirements.Count == 0)
            {
                ImGui.TextColored(ColorDimmed, "(no item requirements)");
                ImGui.Unindent();
                ImGui.Spacing();
                continue;
            }

            foreach (var status in detail.ItemStatuses)
                DrawItemStatusRow(status, detail.IsComplete);

            ImGui.Unindent();
            ImGui.Spacing();
        }
    }

    private static void DrawItemStatusRow(StepItemStatus status, bool stepComplete)
    {
        var (icon, color) = stepComplete
            ? (FontAwesomeIcon.Check, ColorDimmed)
            : status.CurrentCount >= status.Requirement.RequiredCount
                ? (FontAwesomeIcon.Check, ColorComplete)
                : status.CurrentCount > 0
                    ? (FontAwesomeIcon.DotCircle, ColorPartial)
                    : (FontAwesomeIcon.Times, ColorMissing);

        var countText = $"×{status.Requirement.RequiredCount}";
        var shortfall = !stepComplete && status.CurrentCount < status.Requirement.RequiredCount;
        if (shortfall)
            countText += $"  ({status.CurrentCount}/{status.Requirement.RequiredCount})";

        ImGui.BeginGroup();
        Icons.Text(icon, color);
        ImGui.SameLine();
        ImGui.TextColored(color, status.Requirement.ItemName);
        ImGui.SameLine();
        ImGui.TextColored(ColorDimmed, countText);
        ImGui.EndGroup();

        if (!ItemSearch.Row(status.Requirement.ItemId))
            return;

        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 24f);

        if (shortfall)
        {
            ImGui.TextUnformatted("Retainer inventories only count after you've summoned that retainer this session.");
            ImGui.Spacing();
        }

        ItemSearch.Hint();

        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    private static (FontAwesomeIcon Icon, Vector4 Color) StepDisplay(StepDetail detail)
    {
        if (detail.IsComplete) return (FontAwesomeIcon.Check, ColorComplete);
        if (detail.IsCurrent)  return (FontAwesomeIcon.Play,  ColorCurrent);
        return (FontAwesomeIcon.Circle, ColorDimmed);
    }

    private static void DrawIconLabel(FontAwesomeIcon icon, Vector4 color, string label)
    {
        Icons.Text(icon, color);
        ImGui.SameLine();
        ImGui.TextColored(color, label);
    }

    private static void DrawStepRow(
        WeaponProgress progress,
        int stepIndex,
        Func<uint, ProgressReader.ItemLocation?> findItemLocation)
    {
        var detail = progress.Steps[stepIndex];
        var (icon, color) = StepDisplay(detail);

        ImGui.BeginGroup();
        Icons.Text(icon, color);
        ImGui.SameLine();
        ImGui.TextColored(color, detail.Step.Name);
        ImGui.EndGroup();

        // Steps that produce no new weapon (e.g. Anima "Awoken") have nothing to search for.
        var searchId = ItemSearch.FirstId(detail.Step.CompletionItemIds);
        if (!ItemSearch.Row(searchId))
            return;

        if (detail.IsCurrent)
            DrawFormsTooltip(progress, stepIndex, findItemLocation, searchId != 0);
        else if (searchId != 0)
            ItemSearch.HintTooltip();
    }

    private static void DrawFormsTooltip(
        WeaponProgress progress,
        int currentStepIndex,
        Func<uint, ProgressReader.ItemLocation?> findItemLocation,
        bool searchable)
    {
        ImGui.BeginTooltip();
        ImGui.TextDisabled("FORMS");

        if (progress.Forms.Count == 0)
        {
            ImGui.TextUnformatted("Weapon forms not tracked for this series.");
            if (searchable)
            {
                ImGui.Spacing();
                ItemSearch.Hint();
            }
            ImGui.EndTooltip();
            return;
        }

        // A form pinned to a real bag proves every earlier form was built and upgraded away,
        // so those steps read as completed even though their items no longer exist.
        var confirmedIndex = -1;
        var locations = new string?[progress.Forms.Count];
        for (var i = 0; i < progress.Forms.Count; i++)
        {
            var form = progress.Forms[i];
            if (!form.Owned) continue;

            var labels = new List<string>(form.ItemIds.Count);
            foreach (var id in form.ItemIds)
            {
                var loc = findItemLocation(id);
                if (loc?.BagLabel is { } bag) labels.Add(bag);
            }

            if (labels.Count == 0) continue;

            locations[i] = string.Join(", ", labels);
            if (form.StepIndex > confirmedIndex) confirmedIndex = form.StepIndex;
        }

        for (var i = 0; i < progress.Forms.Count; i++)
        {
            var form = progress.Forms[i];
            var isCurrent = form.StepIndex == currentStepIndex;
            var upgradedAway = !form.Owned && form.StepIndex < confirmedIndex;

            var (icon, color) = (form.Owned || upgradedAway, isCurrent) switch
            {
                (true,  _)     => (FontAwesomeIcon.Check,  ColorComplete),
                (false, true)  => (FontAwesomeIcon.Play,   ColorCurrent),
                (false, false) => (FontAwesomeIcon.Circle, ColorDimmed),
            };

            Icons.Text(icon, color);
            ImGui.SameLine();
            ImGui.TextColored(color, form.StepName);
            ImGui.SameLine();

            if (form.Owned)
            {
                ImGui.TextColored(ColorDimmed, $"— {locations[i] ?? "tracked"}");
            }
            else if (upgradedAway)
            {
                ImGui.TextColored(ColorDimmed, "— upgraded");
            }
            else if (isCurrent)
            {
                ImGui.TextColored(ColorDimmed, "(current)");
            }
            else
            {
                ImGui.TextColored(ColorDimmed, "— not yet acquired");
            }
        }

        if (searchable)
        {
            ImGui.Spacing();
            ItemSearch.Hint();
        }

        ImGui.EndTooltip();
    }
}
