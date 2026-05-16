namespace RelicTerror.Data.Series;

internal static class ResistanceJournalQuests
{
    // Journal quests tied to the Resistance Weapon progression. Each is shared across all
    // 17 weapons (one active at a time per character); we surface them only as context above
    // the per-job stage rows so the user knows what's currently in their quest log.
    public static readonly (uint QuestId, string DisplayName)[] Quests =
    [
        (69218, "Vows of Virtue, Deeds of Cruelty"),
        (67795, "Resistance Is Futile"),
        (69574, "Change of Arms"),
        (69506, "For Want of a Memory"),
        (69576, "A New Path of Resistance"),
        (67915, "Future Proof"),
    ];
}
