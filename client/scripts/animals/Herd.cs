using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Every animal the server sends us. The server decides where animals go a couple
// of times a second; each animal node glides between those positions. Also
// offers shearing on the Use key (G) next to a sheep with a full fleece, and asks
// for the sneak hint the first time a flock bolts near the player. Dead animals
// (carcasses) lie where they fell until someone butchers them with G.
public partial class Herd : Node3D
{
    readonly Dictionary<ulong, Sheep> animals = new();
    readonly Dictionary<ulong, Sheep> carcasses = new();
    // Animals whose row just went away; if a carcass for them arrives this frame
    // the same node lies down, otherwise it is freed at the end of the frame.
    readonly Dictionary<ulong, Sheep> leaving = new();
    Terrain terrain = null!;
    DbConnection? conn;
    Func<Vector3> player = () => Vector3.Zero;
    // True while something else owns the mouse (crafting, placing a building).
    public Func<bool> Blocked = () => false;
    // Which way the player faces, flat and unit length.
    public Func<Vector3> Facing = () => Vector3.Forward;
    double lastStrike, lastStrikeHint = -1000;

    public override void _Ready()
    {
        // Strike (D-039): left mouse or Z. The UX lane owns the binding in
        // Controls.cs; this only fills in if that hasn't run.
        if (InputMap.HasAction("strike")) return;
        InputMap.AddAction("strike");
        InputMap.ActionAddEvent("strike", new InputEventMouseButton { ButtonIndex = MouseButton.Left });
        InputMap.ActionAddEvent("strike", new InputEventKey { PhysicalKeycode = Key.Z });
    }

    public void Bind(DbConnection connection, Terrain ground, Func<Vector3> playerPosition)
    {
        conn = connection;
        terrain = ground;
        player = playerPosition;
        conn.Db.Animal.OnInsert += (_, a) => Upsert(a, true);
        conn.Db.Animal.OnUpdate += (_, _, a) => Upsert(a, false);
        conn.Db.Animal.OnDelete += (_, a) =>
        {
            if (!animals.Remove(a.Id, out var node)) return;
            leaving[a.Id] = node;
            Callable.From(() => { if (leaving.Remove(a.Id, out var gone)) gone.QueueFree(); }).CallDeferred();
        };
        conn.Db.AnimalCarcass.OnInsert += (_, c) => AddCarcass(c);
        conn.Db.AnimalCarcass.OnDelete += (_, c) =>
        {
            if (carcasses.Remove(c.Id, out var node)) node.FadeAway();
        };
        conn.Db.Animal.OnUpdate += (_, old, a) =>
        {
            if (a.State == AnimalState.Run && old.State != AnimalState.Run && DistanceTo(a, player()) < BoltHintRange)
                Hints.Request("sneak");
        };
        conn.Db.AnimalHit.OnInsert += (_, h) =>
        {
            if (animals.TryGetValue(h.AnimalId, out var node)) node.Flinch(h.Crit, new Vector3(h.FromX, 0f, h.FromZ));
            HitPop.Show(new Vector3(h.X, terrain.HeightAt(h.X, h.Z), h.Z), h.Crit, h.Killed);
        };
        Use.Register(new UseHandler { Id = "animals.shear", Priority = 15, Find = OfferShear });
        Use.Register(new UseHandler { Id = "animals.butcher", Priority = 16, Find = OfferButcher });
        Strike.Register("animals.animal", OfferStrike);
        Strike.Register("animals.bow", OfferShot);
        var racks = new HideRacks();
        AddChild(racks);
        racks.Bind(conn, terrain);
    }

    const float BoltHintRange = 10f;

    void AddCarcass(AnimalCarcass c)
    {
        // Seen alive a moment ago: that same sheep settles down where it stood.
        bool gently = leaving.Remove(c.AnimalId, out var sheep) || animals.Remove(c.AnimalId, out sheep);
        if (!gently)
        {
            sheep = new Sheep(c.AnimalId, terrain, c.Species);
            AddChild(sheep);
        }
        sheep!.LieDown(c.X, c.Z, c.Heading, c.Fleece, c.Growth, gently);
        carcasses[c.Id] = sheep;
    }

    // The strike key runs the nearest target any lane offers (see Strike.cs); this
    // lane offers the nearest animal ahead in reach. The server checks reach and
    // weapon again; a struck sheep bolts, so a kill is a short chase.
    public override void _UnhandledInput(InputEvent e)
    {
        if (conn == null || Blocked() || !e.IsActionPressed("strike")) return;
        double now = Time.GetTicksMsec() / 1000.0;
        if (now - lastStrike < 0.5) return;
        if (Strike.Best(player(), Facing()) is not { } offer) return;
        lastStrike = now;
        offer.Strike();
    }

    // Hunting needs a spear or a blade; other lanes set their own rules.
    StrikeOffer? OfferStrike(Vector3 at, Vector3 facing)
    {
        if (conn == null || !(Carrying(ItemTags.Piercing) || Carrying(ItemTags.Cutting))) return null;
        Animal? nearest = null;
        float best = World.Reach + 1.5f;
        foreach (var a in conn.Db.Animal.Iter())
        {
            var to = new Vector3(a.X - at.X, 0, a.Z - at.Z);
            float d = to.Length();
            // Ahead of the player, or so close that facing doesn't matter.
            if (d <= best && (d < 1.2f || to.Dot(facing) > -0.2f * d))
                (nearest, best) = (a, d);
        }
        if (nearest == null) return null;
        ulong id = nearest.Id;
        return new StrikeOffer(best, () => conn.Reducers.StrikeAnimal(id));
    }

    // Shooting (animals-15): with a bow and an arrow, the strike key shoots the nearest
    // animal in a cone in front, within BowRange. The server decides the hit, so the
    // client only picks the target and animates the arrow.
    const float BowRange = 40f;
    static readonly float BowCone = Mathf.Cos(Mathf.DegToRad(25f));

    StrikeOffer? OfferShot(Vector3 at, Vector3 facing)
    {
        if (conn == null || !CarryingItem(Item.Bow) || !CarryingItem(Item.Arrow)) return null;
        Animal? nearest = null;
        float best = BowRange;
        foreach (var a in conn.Db.Animal.Iter())
        {
            var to = new Vector3(a.X - at.X, 0, a.Z - at.Z);
            float d = to.Length();
            if (d <= best && d > 1f && to.Dot(facing) >= BowCone * d)
                (nearest, best) = (a, d);
        }
        if (nearest == null) return null;
        ulong id = nearest.Id;
        var target = new Vector3(nearest.X, terrain.HeightAt(nearest.X, nearest.Z) + 0.8f, nearest.Z);
        return new StrikeOffer(best, () =>
        {
            conn.Reducers.ShootAnimal(id);
            ArrowFlight.Launch(this, at + new Vector3(0, 1.4f, 0), target);
        });
    }

    bool CarryingItem(Item item) =>
        conn?.Db.MyInventory.Iter().Any(i => i.Count > 0 && i.Item == (ushort)item) == true;

    public override void _Process(double delta)
    {
        // Carrying a weapon near a sheep: offer the strike hint now and then.
        double now = Time.GetTicksMsec() / 1000.0;
        if (conn == null || now - lastStrikeHint < 30) return;
        lastStrikeHint = now;
        var me = player();
        if (Carrying(ItemTags.Piercing) && conn.Db.Animal.Iter().Any(a => DistanceTo(a, me) < 6f))
            Hints.Request("strike");
    }

    bool Carrying(ItemTags tags) =>
        conn?.Db.MyInventory.Iter().Any(i => i.Count > 0 && Items.IsKnown(i.Item) && Items.Def((Item)i.Item).Tags.HasFlag(tags)) == true;

    UseOffer? OfferButcher(Vector3 at)
    {
        if (conn == null || !Carrying(ItemTags.Cutting)) return null;
        AnimalCarcass? nearest = null;
        float best = World.Reach;
        foreach (var c in conn.Db.AnimalCarcass.Iter())
        {
            float d = new Vector2(c.X - at.X, c.Z - at.Z).Length();
            if (d <= best) (nearest, best) = (c, d);
        }
        if (nearest == null) return null;
        ulong id = nearest.Id;
        return new UseOffer("Butcher", best, () => conn.Reducers.ButcherCarcass(id));
    }

    static float DistanceTo(Animal a, Vector3 at) => new Vector2(a.X - at.X, a.Z - at.Z).Length();

    // Only offered when shearing can work: a sheep in reach with its wool grown back.
    UseOffer? OfferShear(Vector3 at)
    {
        if (conn == null || !Carrying(ItemTags.Cutting)) return null;
        Animal? nearest = null;
        float best = World.Reach;
        foreach (var a in conn.Db.Animal.Iter())
        {
            float d = DistanceTo(a, at);
            if (d <= best && AnimalDefs.Get((byte)a.Species).Shearable && a.Fleece >= 80)
                (nearest, best) = (a, d);
        }
        if (nearest == null) return null;
        ulong id = nearest.Id;
        return new UseOffer("Shear", best, () => conn.Reducers.ShearSheep(id));
    }

    void Upsert(Animal a, bool inserted)
    {
        if (!animals.TryGetValue(a.Id, out var sheep))
        {
            sheep = new Sheep(a.Id, terrain, a.Species);
            animals[a.Id] = sheep;
            AddChild(sheep);
            inserted = true;
        }
        sheep.Apply(a, inserted);
    }
}
