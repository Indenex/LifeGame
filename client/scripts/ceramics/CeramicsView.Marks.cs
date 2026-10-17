using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Maker's marks (ceramics-15): at the Potter's bench the Use key can change your mark to the
// next of the twelve glyphs. It has the lowest priority, so anything else at hand comes first.
public partial class CeramicsView
{
    UseOffer? MarkOffer(Vector3 me3)
    {
        if (conn == null) return null;
        var bench = Stations.NearestOf(conn, StationKind.PottersBench, me3);
        if (bench == null) return null;
        float d = new Vector2(bench.X - me3.X, bench.Z - me3.Z).Length();
        if (d - 0.8f > Structures.WorkReach) return null;
        return new UseOffer("Change your mark", d, () => conn.Reducers.NextMakerMark(), Target: new Vector3(bench.X, 0, bench.Z));
    }
}
