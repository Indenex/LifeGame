using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Clay in the world: draws every drying and firing piece and every kiln the server
// knows about, and offers the clay actions on the Use key: fire a pot beside a
// burning fire, pick up a dry pot or brick, set pots from your bag into a fire,
// load or fire a kiln, and fill fired pots at the water's edge.
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
    // The last drying lines offered; the floating labels step aside while G shows one.
    UseOffer? dryingOffer, dampFireOffer;
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
        RegisterUse();
        AttachSimmer(conn);
        AttachLamps(conn);
        AttachTroughs(conn);
        AttachMarks(conn);
        conn.Db.ClayPiece.OnInsert += (_, p) => Upsert(p);
        conn.Db.ClayPiece.OnUpdate += (_, _, p) => Upsert(p);
        conn.Db.ClayPiece.OnDelete += (_, p) =>
        {
            if (pieces.Remove(p.Id, out var view)) view.QueueFree();
        };
        // A finished kiln structure gets the fire, smoke and label; the bricks
        // themselves are Building's StructureView.
        conn.Db.Structure.OnInsert += (_, s) => KilnSite(s);
        conn.Db.Structure.OnUpdate += (_, _, s) => KilnSite(s);
        conn.Db.Structure.OnDelete += (_, s) =>
        {
            if (kilns.Remove(s.Id, out var view)) view.QueueFree();
        };
        conn.Db.Kiln.OnInsert += (_, k) => KilnState(k);
        conn.Db.Kiln.OnUpdate += (_, _, k) => KilnState(k);
        conn.Db.Kiln.OnDelete += (_, k) =>
        {
            if (kilns.TryGetValue(k.StructureId, out var view)) view.Show(0, false, 0, 0);
        };
        conn.Db.ClayNews.OnInsert += (_, n) =>
        {
            if (n.PlayerId == MyId) note(n.Text.Replace("{use}", UseKey));
        };
        conn.Db.MyCraftJob.OnDelete += (_, j) =>
        {
            if (!j.Failed && ClayRules.FormFor(j.ProcessId) is { } form)
            {
                note(form switch
                {
                    ClayForm.Brick => "The brick is drying. It turns pale when it is ready.",
                    ClayForm.Trough => "Your clay trough is drying. It is too big for a campfire: once it is pale, fire it in a kiln.",
                    ClayForm.Tile => "Your roof tile is drying. It is too fine for a campfire: once it is pale, fire it in a kiln.",
                    ClayForm.StorageJar => "Your storage jar is drying. It is too big for a campfire: once it is pale, fire it in a kiln.",
                    _ => "Your pot is drying. It turns pale when it is ready to fire.",
                });
                Tip("ceramics.dry_by_fire", "Clay dries twice as fast beside a burning fire.");
            }
        };
    }

    uint MyId
    {
        get
        {
            if (myId == 0 && Me.Key(conn) is { } identity && conn.Db.Player.Identity.Find(identity) is { } p)
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
        view.Show(p.Id, (ClayForm)p.Form, ClayStage.Drying, FireKind.Pit, p.Moisture, Seconds(p.MoistureAt), p.DryRate, false, p.Mark);
        if (p.Stage == (byte)ClayStage.Firing)
            view.Firing(Seconds(p.FireStart), Seconds(p.FireFinish));
    }

    void KilnSite(Structure s)
    {
        if (s.Kind != StructureKind.Kiln || !s.Complete || kilns.ContainsKey(s.Id)) return;
        var view = new KilnView { Position = new Vector3(s.X, heightAt(s.X, s.Z), s.Z), Rotation = new Vector3(0, s.Yaw, 0) };
        kilns[s.Id] = view;
        world.AddChild(view);
        if (conn?.Db.Kiln.StructureId.Find(s.Id) is { } k) KilnState(k);
    }

    void KilnState(Kiln k)
    {
        if (kilns.TryGetValue(k.StructureId, out var view))
            view.Show(k.Load?.Count ?? 0, k.State == 1, Seconds(k.FireStart), Seconds(k.FireFinish));
    }

    // Show a kiln without a server, for the offline demo.
    public KilnView DemoKiln(Vector3 at, float yaw, int pots, bool firing)
    {
        var view = new KilnView(withBricks: true) { Position = at, Rotation = new Vector3(0, yaw, 0) };
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

    // Clay actions on the shared Use key (G, D-025). Each offers only when G would
    // really do something, so the prompt never promises what it can't do.
    void RegisterUse()
    {
        Use.Register(new UseHandler { Id = "ceramics.kiln", Priority = 26, Find = KilnOffer });
        // Like taking meat off a fire: ahead of feeding it.
        Use.Register(new UseHandler { Id = "ceramics.simmer", Priority = 25, Find = SimmerOffer });
        // A lamp you face: feed it tallow, or pick it up once it's out. Setting one down
        // sits just under feeding a fire, so G at a fire still feeds the fire.
        Use.Register(new UseHandler { Id = "ceramics.lamp", Priority = 24, Find = LampOffer });
        Use.Register(new UseHandler { Id = "ceramics.trough", Priority = 23, Find = TroughOffer });
        Use.Register(new UseHandler { Id = "ceramics.mark", Priority = 2, Find = MarkOffer });
        Use.Register(new UseHandler { Id = "ceramics.set_lamp", Priority = 19, Find = SetLampOffer });
        Use.Register(new UseHandler { Id = "ceramics.fire_pots", Priority = 22, Find = FirePotsOffer });
        // Firing a dry pot that sits by a fire outranks feeding that fire, as setting pots in does.
        Use.Register(new UseHandler { Id = "ceramics.fire_pot", Priority = 22, Find = at => TendOffer(at, firing: true) });
        Use.Register(new UseHandler { Id = "ceramics.tend", Priority = 12, Find = at => TendOffer(at, firing: false) });
        // How long a wet piece has left is only news: drinking, filling, picking berries
        // or anything else G can really do wins over it (bugfix-19).
        Use.Register(new UseHandler { Id = "ceramics.drying", Priority = 0, Find = at => TendOffer(at, firing: false, info: true) });
        Use.Register(new UseHandler { Id = "ceramics.fill", Priority = 11, Find = FillOffer });
        Use.Register(new UseHandler { Id = "ceramics.pick_up", Priority = 10, Find = at => PickUpOffer(at, byFire: false) });
        // A pot fired in a campfire lands on its ring: taking it out outranks feeding
        // that fire, as taking a roast does, or G burns sticks instead (bugfix-17).
        Use.Register(new UseHandler { Id = "ceramics.take_from_fire", Priority = 24, Find = at => PickUpOffer(at, byFire: true) });
    }

    // The player's Use key as prompts write it (rebindable on the Controls page).
    static string UseKey => Controls.KeyFor("use") ?? "G";

    // Fresh clay is small; drying pieces this close get a faint rim light.
    const float SpotRange = 8f;

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
        // The G drying line says what the label would, and sits where it would; so
        // while it shows, the floating labels step aside.
        bool promptShowing = Use.Shown is { } shown && (ReferenceEquals(shown, dryingOffer) || ReferenceEquals(shown, dampFireOffer));
        foreach (var view in pieces.Values)
        {
            view.ShowLabel = view == nearest && !kilnFirst && !promptShowing;
            float d = new Vector2(view.Position.X - me.Position.X, view.Position.Z - me.Position.Z).Length();
            view.Highlight = view == nearest && !kilnFirst ? 2 : d < SpotRange ? 1 : 0;
        }
        foreach (var view in kilns.Values) view.ShowLabel = kilnFirst && view == kiln.view;
    }

    UseOffer? KilnOffer(Vector3 me3)
    {
        if (conn == null) return null;
        var kiln = NearestKiln(new Vector2(me3.X, me3.Z));
        if (kiln.view == null || kiln.view.IsFiring) return null;
        bool pots = Carrying(i => ClayRules.FormOfUnfired(i) != null);
        ulong id = kiln.id;
        var target = new Vector3(kiln.view.Position.X, 0, kiln.view.Position.Z);
        if (pots && kiln.view.Load < ClayRules.KilnCapacity)
            return new UseOffer("Load the kiln", kiln.d, () => conn.Reducers.UseKiln(id), Target: target);
        if (kiln.view.Load > 0 && FuelSeconds() >= ClayRules.KilnFireSeconds)
            return new UseOffer("Fire the kiln", kiln.d, () => conn.Reducers.UseKiln(id), Target: target);
        return null;
    }

    UseOffer? FirePotsOffer(Vector3 me3)
    {
        if (conn == null || !Carrying(i => ClayRules.FormOfUnfired(i) is { } f && !ClayRules.KilnOnly(f))) return null;
        var at = new Vector2(me3.X, me3.Z);
        // Only a fire that can take a pot: a full ring or too little fuel would just
        // refuse, and this offer outranks Feed the fire (bugfix-18).
        if (BurningFireNear(at, World.Reach) is not { } fire || !FireCanTakePot(fire)) return null;
        float d = new Vector2(fire.X, fire.Z).DistanceTo(at);
        return new UseOffer("Set pots in the fire", d, () => conn.Reducers.FirePots(fire.Id), Target: new Vector3(fire.X, 0, fire.Z));
    }

    // Pass a Target so G picks what you face (KEYMAP.md); without one, a fire you face
    // always won over the pot beside it (bugfix-13).
    UseOffer? TendOffer(Vector3 me3, bool firing, bool info = false)
    {
        if (conn == null) return null;
        var at = new Vector2(me3.X, me3.Z);
        var piece = pieces
            .Where(kv => kv.Value.Stage == ClayStage.Drying)
            .Select(kv => (id: kv.Key, view: kv.Value, d: new Vector2(kv.Value.Position.X, kv.Value.Position.Z).DistanceTo(at)))
            .Where(t => t.d <= World.Reach)
            .OrderBy(t => t.d)
            .FirstOrDefault();
        if (piece.view == null) return null;
        var row = conn.Db.ClayPiece.Id.Find(piece.id);
        bool isPot = row != null && row.Form != (byte)ClayForm.Brick;
        // A storage jar is fired in a kiln, never where it stands.
        bool jar = row != null && ClayRules.KilnOnly((ClayForm)row.Form);
        float moisture = piece.view.Moisture;
        bool dry = !ClayRules.TooWetToFire(moisture);
        // A dry pot counts as beside a fire only if that fire can take it, as the server
        // decides; otherwise G picks it up. That pick-up outranks feeding the fire too
        // (it goes with the firing offers), unless the fire only lacks fuel and you carry
        // some: then Feed the fire wins, and Fire the pot follows (bugfix-18).
        var fire = isPot && !jar ? BurningFireNear(new Vector2(piece.view.Position.X, piece.view.Position.Z), ClayRules.FireReach) : null;
        bool canTake = fire != null && FireCanTakePot(fire);
        bool besideFire = fire != null && (!dry || canTake);
        bool pickUpFirst = fire != null && dry && !canTake && !(FuelShort(fire) && FuelSeconds() > 0);
        if ((besideFire || pickUpFirst) != firing || (!firing && info == dry)) return null;
        ulong id = piece.id;
        var target = new Vector3(piece.view.Position.X, 0, piece.view.Position.Z);
        if (row?.MadeBy == MyId)
            Tip("ceramics.tend", $"{UseKey} by your drying clay shows how long it has left. Once it turns pale, {UseKey} picks it up, or fires it beside a fire.");
        if (dry)
            return besideFire
                ? new UseOffer("Fire the pot", piece.d, () => conn.Reducers.TendClay(id, false), Target: target)
                : new UseOffer(jar ? "Pick up the jar" : isPot ? "Pick up the pot" : "Pick up the brick", piece.d, () => conn.Reducers.TendClay(id, false), Target: target);
        // Still wet: say how long it has left. Beside a fire, pressing G twice fires it
        // anyway (it cracks); away from one, G says where it would dry faster.
        string wait = $"Drying, {ClayRules.DryWait(piece.view.SecondsUntilDry)}";
        if (besideFire)
            return dampFireOffer = new UseOffer($"{wait} ({UseKey} twice fires it wet)", piece.d, () => FireDamp(id, moisture), Target: target);
        bool warm = BurningFireNear(new Vector2(piece.view.Position.X, piece.view.Position.Z), ClayRules.FireReach) != null;
        // No Target: a thing you face that G can really use wins over this.
        return dryingOffer = new UseOffer(wait, piece.d, () => note(warm
            ? $"It is {ClayRules.Describe(moisture)}. It turns pale when it is dry enough."
            : $"It is {ClayRules.Describe(moisture)}. Beside a burning fire it would dry twice as fast."));
    }

    // First-time tips go to UX's hint corner, which shows each once per install.
    // TendOffer runs every frame, so each id is handed over once per session.
    readonly HashSet<string> tipsSent = new();
    void Tip(string id, string text)
    {
        if (tipsSent.Add(id)) Hints.Tip(id, text);
    }

    // Firing a damp pot cracks it: the first press warns, a second within a few seconds fires it.
    void FireDamp(ulong id, float moisture)
    {
        if (confirmPiece != id || Time.GetTicksMsec() / 1000.0 > confirmUntil)
        {
            confirmPiece = id;
            confirmUntil = Time.GetTicksMsec() / 1000.0 + ConfirmSeconds;
            note($"It is still {ClayRules.Describe(moisture)} and will crack. Press {UseKey} again to fire it anyway.");
            return;
        }
        confirmPiece = 0;
        conn?.Reducers.TendClay(id, true);
    }

    UseOffer? FillOffer(Vector3 me3)
    {
        if (conn == null || !Carrying(i => ClayRules.FilledItem(i) != null)) return null;
        // Water within reach, checked as the server's NearWater does: a grid point under
        // the water line. A point between grid points can dip below it on a sloped bank
        // where the server finds none (bugfix-22). Reach is trimmed a little, as for
        // Drink, so a position the server has not caught up with yet still agrees.
        float reach = World.Reach - 0.2f;
        int r = (int)MathF.Ceiling(reach);
        int cx = World.CellOf(me3.X), cz = World.CellOf(me3.Z);
        for (int gx = cx - r; gx <= cx + r; gx++)
            for (int gz = cz - r; gz <= cz + r; gz++)
                if ((gx - me3.X) * (gx - me3.X) + (gz - me3.Z) * (gz - me3.Z) <= reach * reach && heightAt(gx, gz) < World.WaterLevel)
                    return new UseOffer("Fill your pots and waterskin with water", 0.5f, () => conn.Reducers.FillPots(), Again: Use.HoldInterval);
        return null;
    }

    // Fired pots and dry bricks on the ground: G picks them up, as Space does. byFire
    // offers only goods lying within a fire's reach, and the other offer only the rest.
    UseOffer? PickUpOffer(Vector3 me3, bool byFire)
    {
        if (conn == null) return null;
        WorldItem? best = null;
        float bestD = World.Reach;
        foreach (var w in conn.Db.WorldItem.Iter())
        {
            if (!IsClayGood((Item)w.Item)) continue;
            float d = new Vector2(w.X - me3.X, w.Z - me3.Z).Length();
            if (d <= bestD) { best = w; bestD = d; }
        }
        if (best == null || ByFire(best.X, best.Z) != byFire) return null;
        ulong id = best.Id;
        string name = Items.Def((Item)best.Item).Name.ToLowerInvariant();
        return new UseOffer($"Pick up the {name}", bestD, () => conn.Reducers.PickUp(id),
            Target: new Vector3(best.X, heightAt(best.X, best.Z), best.Z));
    }

    static bool IsClayGood(Item item) => item is Item.Brick or Item.PinchPot or Item.CoilPot
        or Item.PinchPotOfWater or Item.CoilPotOfWater or Item.Potsherd or Item.StorageJar or Item.UnfiredStorageJar
        or Item.PotOfBroth or Item.PotOfStew or Item.ClayLamp or Item.Tallow
        or Item.UnfiredRoofTile or Item.RoofTile or Item.UnfiredClayTrough or Item.ClayTrough;

    float FuelSeconds() =>
        conn?.Db.MyInventory.Iter()
            .Where(i => Items.IsKnown(i.Item) && Items.Def((Item)i.Item).FuelSeconds > 0)
            .Sum(i => Items.Def((Item)i.Item).FuelSeconds * i.Count) ?? 0f;

    // Kilns are measured from their wall, not their center.
    // Plain loops: these run every frame for the Use key, and LINQ here made garbage (bugfix-8).
    (ulong id, KilnView? view, float d) NearestKiln(Vector2 at)
    {
        (ulong id, KilnView? view, float d) best = default;
        foreach (var (id, view) in kilns)
        {
            float d = Math.Max(0f, new Vector2(view.Position.X, view.Position.Z).DistanceTo(at) - 0.8f);
            if (d <= World.Reach - 0.8f && (best.view == null || d < best.d)) best = (id, view, d);
        }
        return best;
    }

    bool Carrying(Func<Item, bool> match)
    {
        if (conn == null) return false;
        foreach (var i in conn.Db.MyInventory.Iter())
            if (Items.IsKnown(i.Item) && match((Item)i.Item)) return true;
        return false;
    }

    static float FuelLeft(Fire fire) =>
        (fire.FuelUntil.MicrosecondsSinceUnixEpoch - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000) / 1_000_000f;

    bool FuelShort(Fire fire) => FuelLeft(fire) < ClayRules.PitFireSeconds;

    bool FireCanTakePot(Fire fire)
    {
        int potsIn = 0;
        foreach (var p in conn!.Db.ClayPiece.Iter())
            if (p.Stage == (byte)ClayStage.Firing && p.FireId == fire.Id) potsIn++;
        return ClayRules.FireCanTakePot(FuelLeft(fire), potsIn);
    }

    // Any fire, lit or not yet cleared away; a plain loop, as this runs every frame.
    bool ByFire(float x, float z)
    {
        foreach (var f in conn!.Db.Fire.Iter())
            if (new Vector2(f.X - x, f.Z - z).Length() <= ClayRules.FireReach) return true;
        return false;
    }

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
