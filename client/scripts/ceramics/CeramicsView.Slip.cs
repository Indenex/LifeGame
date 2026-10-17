using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Slip decoration (ceramics-14): at the Potter's bench, with a little clean clay in the bag, the Use
// key offers "Decorate with slip" on a damp pot or jar of yours standing beside you. It opens the
// slip window (SlipPicker); Apply sends SlipPiece, and the server checks everything again.
public partial class CeramicsView
{
    SlipPicker? slipPicker;

    void AttachSlip()
    {
        if (slipPicker != null) return;
        slipPicker = new SlipPicker();
        AddChild(slipPicker);
    }

    UseOffer? SlipOffer(Vector3 me3)
    {
        if (conn == null || slipPicker == null) return null;
        var bench = Stations.NearestOf(conn, StationKind.PottersBench, me3);
        if (bench == null) return null;
        if (new Vector2(bench.X - me3.X, bench.Z - me3.Z).Length() - 0.8f > Structures.WorkReach) return null;
        if (!Carrying(i => i == Item.CleanClay)) return null;
        var at = new Vector2(me3.X, me3.Z);
        var found = pieces
            .Where(kv => kv.Value.Stage == ClayStage.Drying && kv.Value.Moisture >= ClayRules.LeatherHard)
            .Select(kv => (id: kv.Key, view: kv.Value, d: new Vector2(kv.Value.Position.X, kv.Value.Position.Z).DistanceTo(at)))
            .Where(t => t.d <= World.Reach)
            .OrderBy(t => t.d)
            .Select(t => (t.id, t.view, t.d, row: conn.Db.ClayPiece.Id.Find(t.id)))
            .FirstOrDefault(t => t.row != null && t.row.MadeBy == MyId && Slip.CanDecorate((ClayForm)t.row.Form));
        if (found.row == null) return null;
        ulong id = found.id;
        var row = found.row;
        return new UseOffer("Decorate with slip", found.d,
            () => slipPicker.Open((ClayForm)row.Form, row.Mark, (rgb, bands) => conn.Reducers.SlipPiece(id, rgb, (byte)bands)),
            Target: new Vector3(found.view.Position.X, 0, found.view.Position.Z));
    }
}
