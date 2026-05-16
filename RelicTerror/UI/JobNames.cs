using RelicTerror.Data;

namespace RelicTerror.UI;

internal static class JobNames
{
    internal static string Long(Job job) => job switch
    {
        Job.PLD => "Paladin",
        Job.WAR => "Warrior",
        Job.DRK => "Dark Knight",
        Job.GNB => "Gunbreaker",
        Job.WHM => "White Mage",
        Job.SCH => "Scholar",
        Job.AST => "Astrologian",
        Job.SGE => "Sage",
        Job.MNK => "Monk",
        Job.DRG => "Dragoon",
        Job.NIN => "Ninja",
        Job.SAM => "Samurai",
        Job.RPR => "Reaper",
        Job.VPR => "Viper",
        Job.BRD => "Bard",
        Job.MCH => "Machinist",
        Job.DNC => "Dancer",
        Job.BLM => "Black Mage",
        Job.SMN => "Summoner",
        Job.RDM => "Red Mage",
        Job.PCT => "Pictomancer",
        Job.BLU => "Blue Mage",
        _       => job.ToString(),
    };
}
