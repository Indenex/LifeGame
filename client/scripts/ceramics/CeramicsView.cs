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
    readonly Dictionary<ulong, KilnView> kilns = new();
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
        conn.Db.Kiln.OnInsert += (_, k) => UpsertKiln(k);
        conn.Db.Kiln.OnUpdate += (_, _, k) => UpsertKiln(k);
        conn.Db.Kiln.OnDelete += (_, k) =>
        {
            if (kilns.Remove(k.Id, out var view)) view.QueueFree();
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

    void UpsertKiln(Kiln k)
    {
        if (!kilns.TryGetValue(k.Id, out var view))
        {
            view = new KilnView();
            kilns[k.Id] = view;
            world.AddChild(view);
        }
        view.Position = new Vector3(k.X, heightAt(k.X, k.Z), k.Z);
        view.Rotation = new Vector3(0, k.Yaw, 0);
        view.Show(k.Load?.Count ?? 0, k.State == 1, Seconds(k.FireStart), Seconds(k.FireFinish));
    }

    // Show a kiln without a server, for the offline demo.
    public KilnView DemoKiln(Vector3 at, float yaw, int pots, bool firing)
    {
        var view = new KilnView { Position = at, Rotation = new Vector3(0, yaw, 0) };
        world.AddChild(view);
        view.Show(pots, firing, PotView.Now - 20, PotView.Now + 160);
        return view;
    }

    static double Seconds(SpacetimeDB.Timestamp t) => t.MicrosecondsSinceUnixEpoch / 1_000_000.0;

    public override void _Ready()
    {
        // --camera-zoom=-8 starts the camera closer, for screenshots of small things.
        var arg = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).FirstOrDefault(a => a.StartsWith("--camera-zoom="));
        if (arg != null && float.TryParse(arg[14..], out var zoom))
            foreach (var rig in GetTree().Root.FindChildren("*", "", true, false).OfType<CameraRig>())
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
        foreach (var view in kilns.Values)
            view.Position = new Vector3(view.Position.X, heightAt(view.Position.X, view.Position.Z), view.Position.Z);
        // A kiln's label shows when it is the closest thing to tend.
        var kiln = NearestKiln(new Vector2(me.Position.X, me.Position.Z));
        bool kilnFirst = kiln.view != null && kiln.d < nearestDist;
        foreach (var view in pieces.Values) view.ShowLabel = view == nearest && !kilnFirst;
        foreach (var view in kilns.Values) view.ShowLabel = kilnFirst && view == kiln.view;
    }

    void Tend()
    {
        if (conn == null)
        {
            note("Clay needs the server");
            return;
        }
        var at = new Vector2(me.Position.X, me.Position.Z);

        // A kiln within reach comes first unless a piece of clay is closer.
        var kiln = NearestKiln(at);
        // The nearest piece of drying clay within reach.

        var piece = pieces
            .Where(kv => kv.Value.Stage == ClayStage.Drying)
            .Select(kv => (id: kv.Key, view: kv.Value, d: new Vector2(kv.Value.Position.X, kv.Value.Position.Z).DistanceTo(at)))
            .Where(t => t.d <= World.Reach)
            .OrderBy(t => t.d)
            .FirstOrDefault();
        if (kiln.view != null && (piece.view == null || kiln.d < piece.d))
        {
            if (kiln.view.IsFiring)
                note("The kiln is firing. The pots come out when it cools.");
            else if (kiln.view.Load == 0 && !Carrying(i => ClayRules.FormOfUnfired(i) != null))
                note("The kiln is empty. Bring dry pots: pick up a bone-dry pot with V.");
            else
                conn.Reducers.UseKiln(kiln.id);
            return;
        }
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

    // Kilns are measured from their wall, not their center.
    (ulong id, KilnView? view, float d) NearestKiln(Vector2 at) =>
        kilns
            .Select(kv => (id: kv.Key, view: (KilnView?)kv.Value, d: Math.Max(0f, new Vector2(kv.Value.Position.X, kv.Value.Position.Z).DistanceTo(at) - 0.8f)))
            .Where(t => t.d <= World.Reach - 0.8f)
            .OrderBy(t => t.d)
            .FirstOrDefault();

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
