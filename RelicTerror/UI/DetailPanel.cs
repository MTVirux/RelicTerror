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
    private static readonly Vector4 ColorCurrent  = new(0.98f, 0.75f, 0.15f, 1f);
    private static readonly Vector4 ColorDimmed   = new(0.45f, 0.45f, 0.45f, 1f);
    private static readonly Vector4 ColorPartial  = new(0.98f, 0.75f, 0.15f, 1f);
    private static readonly Vector4 ColorMissing  = new(0.95f, 0.35f, 0.35f, 1f);

    internal static void Draw(
        (string SeriesId, Job Job) cell,
        WeaponProgress progress,
        IReadOnlyList<(uint QuestId, string DisplayName)> activeJournalQuests,
        Func<uint, ProgressReader.ItemLocation?> findItemLocation)
    {
        ImGui.TextColored(ColorCurrent, $"{cell.Job} — {cell.SeriesId} Weapon");
        ImGui.Separator();

        var showQuestPanel = cell.SeriesId == "Resistance";
        var topAreaHeight = showQuestPanel
            ? ImGui.GetContentRegionAvail().Y * 0.55f
            : 0f;

        var topAreaSize = showQuestPanel ? new Vector2(0, topAreaHeight) : new Vector2(0, 0);
        ImGui.BeginChild("##detailtop", topAreaSize, false);

        var halfWidth = ImGui.GetContentRegionAvail().X * 0.48f;

        ImGui.BeginChild("##steps", new Vector2(halfWidth, 0), false);
        DrawStepList(progress, findItemLocation);
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##items", new Vector2(0, 0), false);
        DrawItemRequirements(progress, findItemLocation);
        ImGui.EndChild();

        ImGui.EndChild();

        if (showQuestPanel)
        {
            ImGui.BeginChild("##questactivity", new Vector2(0, 0), false);
            QuestActivityPanel.Draw(activeJournalQuests);
            ImGui.EndChild();
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
        DrawIconLabel(
            progress.RelicOwned ? FontAwesomeIcon.Check : FontAwesomeIcon.Circle,
            progress.RelicOwned ? ColorComplete : ColorDimmed,
            progress.RelicOwned ? "Relic owned" : "Relic not acquired");
        DrawIconLabel(
            progress.ReplicaOwned ? FontAwesomeIcon.Check : FontAwesomeIcon.Circle,
            progress.ReplicaOwned ? ColorComplete : ColorDimmed,
            progress.ReplicaOwned ? "Replica owned" : "Replica not acquired");
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

        Icons.Text(icon, color);
        ImGui.SameLine();
        ImGui.TextColored(color, status.Requirement.ItemName);
        ImGui.SameLine();

        var countText = $"×{status.Requirement.RequiredCount}";
        if (!stepComplete && status.CurrentCount < status.Requirement.RequiredCount)
            countText += $"  ({status.CurrentCount}/{status.Requirement.RequiredCount})";

        ImGui.TextColored(ColorDimmed, countText);

        if (!stepComplete && status.CurrentCount < status.Requirement.RequiredCount && ImGui.IsItemHovered())
            ImGui.SetTooltip("Retainer inventories only count after you've summoned that retainer this session.");
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

        if (detail.IsCurrent && ImGui.IsItemHovered())
            DrawFormsTooltip(progress, stepIndex, findItemLocation);
    }

    private static void DrawFormsTooltip(
        WeaponProgress progress,
        int currentStepIndex,
        Func<uint, ProgressReader.ItemLocation?> findItemLocation)
    {
        ImGui.BeginTooltip();
        ImGui.TextDisabled("FORMS");

        if (progress.Forms.Count == 0)
        {
            ImGui.TextUnformatted("Weapon forms not tracked for this series.");
            ImGui.EndTooltip();
            return;
        }

        foreach (var form in progress.Forms)
        {
            var isCurrent = form.StepIndex == currentStepIndex;
            var (icon, color) = (form.Owned, isCurrent) switch
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
                var labels = new List<string>(form.ItemIds.Count);
                foreach (var id in form.ItemIds)
                {
                    var loc = findItemLocation(id);
                    if (loc?.BagLabel is { } bag) labels.Add(bag);
                }
                var locText = labels.Count > 0 ? string.Join(", ", labels) : "tracked";
                ImGui.TextColored(ColorDimmed, $"— {locText}");
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

        ImGui.EndTooltip();
    }
}
