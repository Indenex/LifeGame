using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Clay in the world: draws every drying and firing piece the server knows about,
// and handles X, the one clay action (until the shared Use key lands). X on a pot beside a burning fire puts it in
// the fire; on a dry pot or brick picks it up; near a fire with dry pots in your
// bag sets them in; at the water's edge fills your fired pots.
public partial class CeramicsView : Node
{
    const double ConfirmSeconds = 4;

    readonly Node3D world = new();
    readonly Func<float, float, float> heightAt;
    readonly Node3D me;
    readonly Action<string> note;
    readonly Dictionary<ulong, PotView> pieces = new();
    DbConnection? conn;
    uint myId;
    ulong confirmPiece;
    double confirmUntil;

    public CeramicsView(Node parent3D, Func<float, float, float> heightAt, Node3D me, Action<string> note)
    {
        this.heightAt = heightAt;
        this.me = me;
        this.note = note;
        parent3D.AddChild(world);
    }

    public void Attach(DbConnection conn)
    {
        this.conn = conn;
        conn.Db.ClayPiece.OnInsert += (_, p) => Upsert(p);
        conn.Db.ClayPiece.OnUpdate += (_, _, p) => Upsert(p);
        conn.Db.ClayPiece.OnDelete += (_, p) =>
        {
            if (pieces.Remove(p.Id, out var view)) view.QueueFree();
        };
        conn.Db.ClayNews.OnInsert += (_, n) =>
        {
            if (n.PlayerId == MyId) note(n.Text);
        };
        conn.Db.MyCraftJob.OnDelete += (_, j) =>
        {
            if (!j.Failed && ClayRules.FormFor(j.ProcessId) is { } form)
                note(form == ClayForm.Brick
                    ? "The brick is drying. It turns pale when it is ready."
                    : "Your pot is drying. It turns pale when it is ready to fire.");
        };
    }

    uint MyId
    {
        get
        {
            if (myId == 0 && conn?.Identity is { } identity && conn.Db.Player.Identity.Find(identity) is { } p)
                myId = p.PlayerId;
            return myId;
        }
    }

    void Upsert(ClayPiece p)
    {
        if (!pieces.TryGetValue(p.Id, out var view))
        {
            view = new PotView();
            pieces[p.Id] = view;
            world.AddChild(view);
        }
        view.Position = new Vector3(p.X, 0, p.Z);
        view.Show(p.Id, (ClayForm)p.Form, ClayStage.Drying, FireKind.Pit, p.Moisture, Seconds(p.MoistureAt), p.DryRate);
        if (p.Stage == (byte)ClayStage.Firing)
            view.Firing(Seconds(p.FireStart), Seconds(p.FireFinish));
    }

    static double Seconds(SpacetimeDB.Timestamp t) => t.MicrosecondsSinceUnixEpoch / 1_000_000.0;

    public override void _Ready()
    {
        // --camera-zoom=-8 starts the camera closer, for screenshots of small things.
        var arg = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).FirstOrDefault(a => a.StartsWith("--camera-zoom="));
        if (arg != null && float.TryParse(arg[14..], out var zoom))
            foreach (var rig in GetTree().Root.FindChildren("*", nameof(CameraRig), true, false).OfType<CameraRig>())
                rig.Zoom(zoom);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.V })
        {
            Tend();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        // Only the nearest piece within reach shows its label, so a camp of pots stays readable.
        PotView? nearest = null;
        float nearestDist = World.Reach;
        foreach (var view in pieces.Values)
        {
            float d = new Vector2(view.Position.X - me.Position.X, view.Position.Z - me.Position.Z).Length();
            if (d < nearestDist) { nearest = view; nearestDist = d; }
            // Follow the ground if someone digs beside it; pots in a fire sit in the embers.
            float ground = heightAt(view.Position.X, view.Position.Z) + (view.Stage == ClayStage.Firing ? -0.03f : 0f);
            view.Position = new Vector3(view.Position.X, ground, view.Position.Z);
        }
        foreach (var view in pieces.Values) view.ShowLabel = view == nearest;
    }

    void Tend()
    {
        if (conn == null)
        {
            note("Clay needs the server");
            return;
        }
        var at = new Vector2(me.Position.X, me.Position.Z);

        // The nearest piece of drying clay within reach.
        var piece = pieces
            .Where(kv => kv.Value.Stage == ClayStage.Drying)
            .Select(kv => (id: kv.Key, view: kv.Value, d: new Vector2(kv.Value.Position.X, kv.Value.Position.Z).DistanceTo(at)))
            .Where(t => t.d <= World.Reach)
            .OrderBy(t => t.d)
            .FirstOrDefault();
        if (piece.view != null)
        {
            var row = conn.Db.ClayPiece.Id.Find(piece.id);
            bool isPot = row != null && row.Form != (byte)ClayForm.Brick;
            bool besideFire = isPot && BurningFireNear(new Vector2(piece.view.Position.X, piece.view.Position.Z), ClayRules.FireReach) != null;
            if (besideFire && ClayRules.TooWetToFire(piece.view.Moisture))
            {
                // Firing a damp pot cracks it: ask first.
                if (confirmPiece != piece.id || Time.GetTicksMsec() / 1000.0 > confirmUntil)
                {
                    confirmPiece = piece.id;
                    confirmUntil = Time.GetTicksMsec() / 1000.0 + ConfirmSeconds;
                    note($"It is still {ClayRules.Describe(piece.view.Moisture)} and will crack. Press V again to fire it anyway.");
                    return;
                }
                confirmPiece = 0;
                conn.Reducers.TendClay(piece.id, true);
                return;
            }
            conn.Reducers.TendClay(piece.id, false);
            return;
        }

        bool carryingUnfired = Carrying(i => ClayRules.FormOfUnfired(i) != null);
        if (carryingUnfired && BurningFireNear(at, World.Reach) is { } fire)
        {
            conn.Reducers.FirePots(fire.Id);
            return;
        }
        if (Carrying(i => ClayRules.FilledItem(i) != null))
        {
            conn.Reducers.FillPots();
            return;
        }
        note(carryingUnfired
            ? "Take your dry pots to a burning fire and press V to fire them."
            : "Nothing to tend here. Shape clay from the crafting panel (C).");
    }

    bool Carrying(Func<Item, bool> match) =>
        conn?.Db.MyInventory.Iter().Any(i => Items.IsKnown(i.Item) && match((Item)i.Item)) ?? false;

    Fire? BurningFireNear(Vector2 at, float reach)
    {
        if (conn == null) return null;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        return conn.Db.Fire.Iter()
            .Where(f => f.FuelUntil.MicrosecondsSinceUnixEpoch > now && new Vector2(f.X, f.Z).DistanceTo(at) <= reach)
            .OrderBy(f => new Vector2(f.X, f.Z).DistanceTo(at))
            .FirstOrDefault();
    }
}
