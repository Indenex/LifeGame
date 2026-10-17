using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Maker's marks (ceramics-15): at the Potter's bench the Use key offers "Choose your mark", the
// lowest-priority offer, so anything else at hand comes first. UX/UI's picker panel replaces the
// body of this offer; until it lands the prompt steps to the next glyph in your current colour.
public partial class CeramicsView
{
    // Opens the mark picker. Set by UX/UI's panel; null until it exists.
    public static System.Action? OpenMarkPicker;

    // The first time you start shaping a pot with no mark chosen, the picker opens (once per
    // session), so the pot you are making can carry your mark.
    bool askedForMark;

    void AttachMarks(SpacetimeDB.Types.DbConnection c)
    {
        // A new or changed mark restyles every pot its maker made: drop the cached meshes so they rebuild.
        c.Db.PlayerMark.OnInsert += (_, _) => ClayItemLook.ClearMarked();
        c.Db.PlayerMark.OnUpdate += (_, _, _) => ClayItemLook.ClearMarked();
        c.Db.MyCraftJob.OnInsert += (_, job) =>
        {
            if (askedForMark || OpenMarkPicker == null) return;
            if (ClayRules.FormFor(job.ProcessId) is not { } form || form == ClayForm.Brick) return;
            if (c.Db.PlayerMark.PlayerId.Find(job.PlayerId) != null) return;
            askedForMark = true;
            OpenMarkPicker();
        };
    }

    UseOffer? MarkOffer(Vector3 me3)
    {
        if (conn == null) return null;
        var bench = Stations.NearestOf(conn, StationKind.PottersBench, me3);
        if (bench == null) return null;
        float d = new Vector2(bench.X - me3.X, bench.Z - me3.Z).Length();
        if (d - 0.8f > Structures.WorkReach) return null;
        return new UseOffer("Choose your mark", d, ChooseMark, Target: new Vector3(bench.X, 0, bench.Z));
    }

    int standInSlot = -1;

    void ChooseMark()
    {
        if (conn == null) return;
        if (OpenMarkPicker != null) { OpenMarkPicker(); return; }
        // Stand-in picker: the next glyph, in ochre.
        standInSlot = (standInSlot + 1) % Marks.All.Count;
        conn.Reducers.SetMark(Marks.All[standInSlot].Id, Marks.DefaultColor);
    }
}
