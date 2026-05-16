using System.Collections.Generic;
using RelicTerror.Data.Series;

namespace RelicTerror.Data;

public static class RelicDatabase
{
    public static IReadOnlyList<RelicSeries> AllSeries { get; } =
    [
        ZodiacSeries.Build(),
        AnimaSeries.Build(),
        EurekaSeries.Build(),
        ResistanceSeries.Build(),
        MandervilleSeries.Build(),
        PhantomSeries.Build(),
    ];
}
