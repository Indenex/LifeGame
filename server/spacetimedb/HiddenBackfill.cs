using SpacetimeDB;

// Hidden values (bugfix-53/54): rows that were live before a table went private or lost a column get
// their new counterpart on the next connection, so an upgraded world carries on where it was. Cheap:
// the tables are small and a row already carried over is skipped.
public static partial class Module
{
    static void BackfillHiddenValues(ReducerContext ctx)
    {
        // Devices running at upgrade time show up again.
        foreach (var job in ctx.Db.DeviceJob.Iter())
            if (ctx.Db.DeviceStatus.StructureId.Find(job.StructureId) is null)
                SaveDeviceJob(ctx, job);
        // Food on a fire at upgrade time keeps its quality; the public row stops carrying it.
        foreach (var roast in ctx.Db.Roast.Iter().ToList())
        {
            if (ctx.Db.RoastQuality.RoastId.Find(roast.Id) is not null) continue;
            ctx.Db.RoastQuality.Insert(new RoastQuality { RoastId = roast.Id, Quality = roast.Quality });
            if (roast.Quality != 0) ctx.Db.Roast.Id.Update(roast with { Quality = 0 });
        }
    }
}
