using System;
using System.Collections.Generic;
using Lumina.Excel.Sheets;

namespace RelicTerror.Data;

internal static class AchievementLookup
{
    private static Dictionary<string, uint>? _byName;

    public static uint? ByName(string name)
    {
        var map = _byName ??= BuildNameMap();
        return map.TryGetValue(name, out var id) ? id : null;
    }

    private static Dictionary<string, uint> BuildNameMap()
    {
        var map = new Dictionary<string, uint>(StringComparer.Ordinal);
        foreach (var row in Services.DataManager.GetExcelSheet<Achievement>())
        {
            if (row.RowId == 0) continue;
            var name = row.Name.ExtractText();
            if (!string.IsNullOrEmpty(name))
                map[name] = row.RowId;
        }
        return map;
    }
}
