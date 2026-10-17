using LifeGame.Shared;
using SpacetimeDB;

// claims-15: the rest of what players leave lying outside a claim. Weathering.cs tracks
// structures and dropped items by last touch; clay pieces drying in the open and free-standing
// hide racks have no touch time, but any use removes them, so a row that is still there has been
// left alone. The sweep notes when it first saw each one and removes it after the same
// WeatherAfterHours + WeatherForHours as everything else (so at most an hour late). Inside a
// standing claim the clock keeps restarting.
public static partial class Module
{
    const byte SeenClay = 0, SeenHide = 1;

    // Private: when the hourly sweep first saw a clay piece or hide rack.
    [SpacetimeDB.Table(Accessor = "WeatherSeen")]
    [SpacetimeDB.Index.BTree(Accessor = "ByThing", Columns = new[] { nameof(Kind), nameof(RefId) })]
    public partial struct WeatherSeen
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        public byte Kind;
        public ulong RefId;
        public Timestamp FirstSeen;
    }

    static void SweepUntrackedThings(ReducerContext ctx, Timestamp now)
    {
        long goneAfter = (long)((Structures.WeatherAfterHours + Structures.WeatherForHours) * 3600e6);
        var live = new HashSet<(byte, ulong)>();

        // True when the thing has weathered away and should be removed.
        bool Aged(byte kind, ulong id, float x, float z)
        {
            live.Add((kind, id));
            var rows = ctx.Db.WeatherSeen.ByThing.Filter((kind, id)).ToList();
            if (rows.Count == 0)
            {
                ctx.Db.WeatherSeen.Insert(new WeatherSeen { Id = 0, Kind = kind, RefId = id, FirstSeen = now });
                return false;
            }
            if (InClaim(ctx, x, z))
            {
                ctx.Db.WeatherSeen.Id.Update(rows[0] with { FirstSeen = now });
                return false;
            }
            return now.MicrosecondsSinceUnixEpoch - rows[0].FirstSeen.MicrosecondsSinceUnixEpoch >= goneAfter;
        }

        foreach (var piece in ctx.Db.ClayPiece.Iter().ToList())
            if (Aged(SeenClay, piece.Id, piece.X, piece.Z))
                ctx.Db.ClayPiece.Id.Delete(piece.Id);

        // Hides on a drying-rack station go with the station; only the old free-standing racks are swept here.
        foreach (var rack in ctx.Db.HideRack.Iter().ToList())
            if (rack.StationId == 0 && Aged(SeenHide, rack.Id, rack.X, rack.Z))
                ctx.Db.HideRack.Id.Delete(rack.Id);

        foreach (var row in ctx.Db.WeatherSeen.Iter().ToList())
            if (!live.Contains((row.Kind, row.RefId)))
                ctx.Db.WeatherSeen.Id.Delete(row.Id);
    }
}
