using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// House building on the client: laying out footprints with a ghost preview,
// adding materials to nearby sites, drawing every structure at its current
// stage, keeping people out of hut walls, and saying when you are sheltered.
//
// B opens placement (Tab picks what, T turns it, click lays it out, Esc cancels).
// Hold G (the shared Use key) next to an unfinished site to add what you carry.
// Using finished buildings (doors, baskets) and weathering are in BuildingSystem.Use.cs.
public partial class BuildingSystem : Node
{
    const float WorkInterval = 0.65f;

    readonly Terrain terrain;
    readonly Hud hud;
    readonly Node3D me;
    readonly CameraRig cameraRig;
    readonly SubViewport view;
    readonly Func<DbConnection?> conn;
    // Whether you may bar doors at a point (you own or belong to the claim there); Main wires it to Social.
    public Func<float, float, bool> MayBar = (_, _) => false;
    // Whether a standing claim covers this point (claim fences stop non-members even in a jump).
    public Func<float, float, bool> ClaimStandingAt = (_, _) => false;

    readonly Node3D root = new() { Name = "Structures" };
    readonly Dictionary<ulong, StructureView> views = new();
    readonly Dictionary<ulong, Structure> rows = new();
    readonly HashSet<long> cleared = new();

    readonly Label prompt = new();
    PanelContainer promptPanel = null!;

    bool placing;
    public bool Placing => placing;
    int placeKind;
    float ghostYawTarget, ghostYaw;
    Vector3 ghostPos;
    bool ghostPlaced;
    StructureView? ghost;
    string? placeProblem;
    readonly StandardMaterial3D ghostMaterial = new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        // Drawn over trees and trunks, so the outline never hides behind one (the trees
        // inside it are cleared once it is laid out anyway).
        NoDepthTest = true,
        RenderPriority = 1,
        AlbedoColor = new Color(0.85f, 1f, 0.8f, 0.35f),
    };


    public BuildingSystem(Terrain terrain, Hud hud, Node3D me, CameraRig cameraRig, SubViewport view, Func<DbConnection?> conn)
    {
        this.terrain = terrain;
        this.hud = hud;
        this.me = me;
        this.cameraRig = cameraRig;
        this.view = view;
        this.conn = conn;
        terrain.Cleared = (x, z) => cleared.Contains(World.CellKey(x, z));
    }

    public override void _Ready()
    {
        view.AddChild(root);
        BindKey("build_mode", Key.B);
        BindKey("build_next", Key.Tab);
        BindKey("build_rotate", Key.T);
        BindKey("build_cancel", Key.Escape);

        var layer = new CanvasLayer { Layer = 1 };
        AddChild(layer);
        var ui = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        ui.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(ui);

        Hud.Style(prompt, 16, 1f);
        prompt.HorizontalAlignment = HorizontalAlignment.Center;
        promptPanel = Hud.Panel(prompt, Vector2.Zero);
        promptPanel.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        promptPanel.GrowHorizontal = Control.GrowDirection.Both;
        promptPanel.GrowVertical = Control.GrowDirection.Begin;
        // Above the inventory and the survival prompt.
        promptPanel.Position = new Vector2(0, -160);
        promptPanel.Visible = false;
        ui.AddChild(promptPanel);

        // Adding materials is a hold-G Use action (KEYMAP.md); it runs again every WorkInterval while held.
        Use.Register(new UseHandler
        {
            Id = "building.add",
            Priority = 30,
            Find = at =>
            {
                if (conn() is not { } c || NearestSite() is not { } site) return null;
                var carried = c.Db.MyInventory.Iter().Where(i => i.Count > 0).Select(i => i.Item).ToHashSet();
                if (!site.Needs.Any(n => n.Have < n.Need && carried.Contains(n.Item))) return null;
                float d = new Vector2(site.X - at.X, site.Z - at.Z).Length();
                return new UseOffer("Add materials", d, () => conn()?.Reducers.Build(site.Id), Repeat: WorkInterval, Target: new Vector3(site.X, at.Y, site.Z));
            },
        });

        var args = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).ToList();
        if (args.Contains("--building-showcase")) Showcase();
        if (args.Contains("--building-closeup")) cameraRig.Zoom(-8f);
    }

    // --building-showcase lays out local, finished and half-built structures in
    // front of the starting camera, for checking the look without a server.
    void Showcase()
    {
        var f = cameraRig.Forward;
        var r = cameraRig.Right;
        float yaw = Mathf.Atan2(-f.X, -f.Z);
        ulong id = ulong.MaxValue;
        void Put(StructureKind kind, float forward, float right, int stage, float fraction, float turn = 0f)
        {
            var at = f * forward + r * right;
            int stages = Structures.Shape((int)kind).Stages.Length;
            var needs = stage >= stages ? new List<MaterialNeed>() : new List<MaterialNeed> { new((ushort)Item.Stick, (uint)(fraction * 10), 10) };
            Upsert(new Structure(id--, kind, at.X, at.Z, yaw + turn, World.ChunkOf(at.X), World.ChunkOf(at.Z), default,
                (byte)Math.Min(stage, stages), needs, stage >= stages, 1, default), fresh: false);
        }
        // --building-mat: a brick house with only its first courses up, so the sleeping mat by the hearth shows.
        if (OS.GetCmdlineUserArgs().Contains("--building-mat"))
        {
            var at = f * 7.5f;
            Put(StructureKind.BrickHouse, 7.5f, 0f, 1, 0.3f, 0.5f);
            views[id + 1].HideUnbuilt();
            var (mx, mz) = Structures.ToWorld(at.X, at.Z, yaw + 0.5f, 1.2f, -0.3f);
            ShowMat(1, mx, mz, yaw + 0.5f);
            return;
        }
        // --building-placing: laying out a lean-to, to check the ghost and its cost hint.
        if (OS.GetCmdlineUserArgs().Contains("--building-placing"))
        {
            placing = true;
            ghostYawTarget = ghostYaw = yaw;
            MakeGhost();
            return;
        }
        // --building-leanto: finished lean-tos seen from the ridge side and from behind.
        if (OS.GetCmdlineUserArgs().Contains("--building-leanto"))
        {
            Put(StructureKind.LeanTo, 6f, -2.4f, 4, 1f, 0f);
            Put(StructureKind.LeanTo, 6f, 2.4f, 4, 1f, Mathf.Pi);
            return;
        }
        // --building-house: a finished brick house and one going up, without the rest.
        if (OS.GetCmdlineUserArgs().Contains("--building-house"))
        {
            Put(StructureKind.BrickHouse, 7f, -3.2f, 4, 1f, 0.25f);
            Put(StructureKind.BrickHouse, 8.5f, 4.2f, 1, 0.6f, -0.3f);
            return;
        }
        // --building-door: just a finished hut with its door barred, close up.
        if (OS.GetCmdlineUserArgs().Contains("--building-door"))
        {
            Put(StructureKind.ClayHut, 5.5f, 0f, 4, 1f, 0f);
            SetDoor(new StructureDoor(ulong.MaxValue, true, true, default), animate: false);
            return;
        }
        Put(StructureKind.ClayHut, 8f, -3.2f, 4, 1f, 0.3f);
        Put(StructureKind.LeanTo, 6f, 3.4f, 4, 1f, -2.3f);
        Put(StructureKind.ClayHut, 14f, 5.5f, 2, 0.5f, -0.5f);
        Put(StructureKind.LeanTo, 13.5f, -1.5f, 3, 0.4f, 0.7f);
        Put(StructureKind.LeanTo, 4f, -4.5f, 0, 0.5f, 2.6f);
        Put(StructureKind.Basket, 4.5f, -1.2f, 1, 1f);
        Put(StructureKind.Basket, 5.2f, -0.4f, 0, 0.5f, 1f);
        SetDoor(new StructureDoor(ulong.MaxValue, true, true, default), animate: false);
    }

    public void Bind(DbConnection c)
    {
        c.Db.Structure.OnInsert += (_, s) => Upsert(s, fresh: s.Stage == 0 && s.Builders == 0 && s.Needs.All(n => n.Have == 0));
        c.Db.Structure.OnUpdate += (_, _, s) => Upsert(s, fresh: false);
        c.Db.Structure.OnDelete += (_, s) => Remove(s);
        BindUse(c);
    }

    // --- Rows to views ----------------------------------------------------

    void Upsert(Structure s, bool fresh)
    {
        bool existed = rows.TryGetValue(s.Id, out var before);
        rows[s.Id] = s;
        if (!views.TryGetValue(s.Id, out var v))
        {
            v = new StructureView((int)s.Kind)
            {
                StructureId = s.Id,
                Position = new Vector3(s.X, terrain.HeightAt(s.X, s.Z), s.Z),
                Rotation = new Vector3(0, s.Yaw, 0),
            };
            views[s.Id] = v;
            root.AddChild(v);
            SetCleared(s, true);
            ApplyExtras(s, v);
        }

        int appeared = v.Show(s.Stage, Fraction(s), s.Complete, animate: existed || fresh);
        // A finished claim marker is drawn by client/scripts/social, which weathers it.
        v.Visible = !(s.Complete && Structures.Shape((int)s.Kind).Model == "ClaimMarker");
        if (appeared > 0 && (existed || fresh))
            Puff(v.Position + Vector3.Up * 0.6f, s.Stage >= 3 || Structures.Shape((int)s.Kind).Hut && s.Stage == 2 ? Look.LeafWarm : Look.DugEarth);

        if (existed && !before!.Complete && s.Complete && me.Position.DistanceTo(v.Position) < 30f)
            hud.Note($"The {ShapeOf(s).Name.ToLowerInvariant()} is finished");
    }

    void Remove(Structure s)
    {
        rows.Remove(s.Id);
        SetCleared(s, false);
        if (views.Remove(s.Id, out var v)) v.QueueFree();
    }

    static float Fraction(Structure s)
    {
        uint need = 0, have = 0;
        foreach (var n in s.Needs)
        {
            need += n.Need;
            have += Math.Min(n.Have, n.Need);
        }
        return need == 0 ? 1f : have / (float)need;
    }

    static StructureShape ShapeOf(Structure s) => Structures.Shape((int)s.Kind);

    // Trees and grass make way for the footprint, and the floor is trodden bare.
    void SetCleared(Structure s, bool on)
    {
        int r = 3;
        int cx = World.CellOf(s.X), cz = World.CellOf(s.Z);
        for (int x = cx - r; x <= cx + r; x++)
            for (int z = cz - r; z <= cz + r; z++)
            {
                if (!Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, x, z, 0.4f + ShapeOf(s).ClearExtra)) continue;
                long key = World.CellKey(x, z);
                if (on) cleared.Add(key);
                else if (!rows.Values.Any(o => Structures.Contains((int)o.Kind, o.X, o.Z, o.Yaw, x, z, 0.4f + ShapeOf(o).ClearExtra))) cleared.Remove(key);
                terrain.Refresh(x, z);
            }
    }

    // --- Shelter and walls ------------------------------------------------

    // The finished structure sheltering this spot, if any. Matches the server's ShelterAt.
    public Structure? ShelterAt(Vector3 p) =>
        rows.Values.FirstOrDefault(s => s.Complete && ShapeOf(s).RainCover > 0f && Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, p.X, p.Z, -0.2f));

    // Keeps a step from passing through a hut wall anywhere but the door.
    // airHeight is how high the walker's feet are in a jump (bugfix-25): fences and gates lower than that
    // can be hopped, unless they stand in a claim the walker doesn't belong to. Walls always block.
    public Vector3 Collide(Vector3 from, Vector3 to, float airHeight = 0f)
    {
        foreach (var s in rows.Values)
        {
            var shape = ShapeOf(s);
            if (shape.Fence)
            {
                if (s.Complete && !(shape.Gate && !DoorShut(s.Id))
                    && Jumping.FenceStops(airHeight, shape.Gate, ClaimStandingAt(s.X, s.Z), MayBar(s.X, s.Z)))
                    to = FenceLine(s, from, to);
                continue;
            }
            if (shape.Solid && shape.Round && (s.Complete || s.Stage >= 1))
            {
                float dx = to.X - s.X, dz = to.Z - s.Z, d = Mathf.Sqrt(dx * dx + dz * dz), keep = shape.Radius + 0.3f;
                if (d < keep && d > 0.01f) to = new Vector3(s.X + dx / d * keep, to.Y, s.Z + dz / d * keep);
                continue;
            }
            if (shape.Room && (s.Complete || s.Stage >= 2))
            {
                to = HouseWalls(s, from, to);
                continue;
            }
            if (shape.Model == "LeanTo")
            {
                to = LeanToWalls(s, shape, from, to);
                continue;
            }
            if (!shape.Hut || (!s.Complete && s.Stage < 1)) continue;
            float wall = Structures.HutWallRadiusFor((int)s.Kind);
            const float half = 0.32f, door = 0.42f;
            var (lx, lz) = Structures.ToLocal(s.X, s.Z, s.Yaw, to.X, to.Z);
            float r = Mathf.Sqrt(lx * lx + lz * lz);
            if (Mathf.Abs(r - wall) > half || r < 0.01f) continue;
            if (Mathf.Abs(Mathf.Atan2(lx, lz)) < door && !DoorShut(s.Id)) continue;
            var (fx, fz) = Structures.ToLocal(s.X, s.Z, s.Yaw, from.X, from.Z);
            bool inside = fx * fx + fz * fz < wall * wall;
            float target = inside ? wall - half : wall + half;
            var (wx, wz) = Structures.ToWorld(s.X, s.Z, s.Yaw, lx / r * target, lz / r * target);
            to = new Vector3(wx, to.Y, wz);
        }
        return to;
    }

    // A fence or shut gate: keeps you on the side of its line you came from.
    static Vector3 FenceLine(Structure s, Vector3 from, Vector3 to)
    {
        const float half = 0.3f;
        var (lx, lz) = Structures.ToLocal(s.X, s.Z, s.Yaw, to.X, to.Z);
        if (Mathf.Abs(lx) > Structures.FenceHalfLength + half * 0.5f || Mathf.Abs(lz) >= half) return to;
        var (_, fz) = Structures.ToLocal(s.X, s.Z, s.Yaw, from.X, from.Z);
        // Landing from a jump on top of the line: step out the side you were heading to.
        float side = Mathf.Abs(fz) < half ? lz : fz;
        var (x, z) = Structures.ToWorld(s.X, s.Z, s.Yaw, lx, side >= 0f ? half : -half);
        return new Vector3(x, to.Y, z);
    }

    // A lean-to's two sides and back; the front is open (bugfix-23).
    static Vector3 LeanToWalls(Structure s, StructureShape shape, Vector3 from, Vector3 to)
    {
        const float half = 0.25f;
        float wx = shape.HalfWidth, wz = shape.HalfDepth;
        var (lx, lz) = Structures.ToLocal(s.X, s.Z, s.Yaw, to.X, to.Z);
        var (fx, fz) = Structures.ToLocal(s.X, s.Z, s.Yaw, from.X, from.Z);
        if (lz > -wz - half && lz < wz && Mathf.Abs(Mathf.Abs(lx) - wx) < half)
            lx = Mathf.Sign(lx) * (Mathf.Abs(fx) < wx ? wx - half : wx + half);
        if (Mathf.Abs(lx) < wx + half && Mathf.Abs(lz + wz) < half)
            lz = fz > -wz ? -wz + half : -wz - half;
        var (x, z) = Structures.ToWorld(s.X, s.Z, s.Yaw, lx, lz);
        return new Vector3(x, to.Y, z);
    }

    // A brick house's four straight walls, open at the doorway unless the door is shut.
    Vector3 HouseWalls(Structure s, Vector3 from, Vector3 to)
    {
        const float half = 0.3f;
        float wx = Structures.HouseWallXFor((int)s.Kind), wz = Structures.HouseWallZFor((int)s.Kind), door = Structures.HouseDoorHalfWidthFor((int)s.Kind);
        var (lx, lz) = Structures.ToLocal(s.X, s.Z, s.Yaw, to.X, to.Z);
        var (fx, fz) = Structures.ToLocal(s.X, s.Z, s.Yaw, from.X, from.Z);
        // Side walls at x = ±wx.
        if (Mathf.Abs(lz) < wz + half && Mathf.Abs(Mathf.Abs(lx) - wx) < half)
            lx = Mathf.Sign(lx) * (Mathf.Abs(fx) < wx ? wx - half : wx + half);
        // Back and front walls at z = ±wz; the front has the doorway.
        bool doorway = lz > 0 && Mathf.Abs(lx) < door && !DoorShut(s.Id);
        if (!doorway && Mathf.Abs(lx) < wx + half && Mathf.Abs(Mathf.Abs(lz) - wz) < half)
            lz = Mathf.Sign(lz) * (Mathf.Abs(fz) < wz ? wz - half : wz + half);
        var (x, z) = Structures.ToWorld(s.X, s.Z, s.Yaw, lx, lz);
        return new Vector3(x, to.Y, z);
    }

    // --- Input ------------------------------------------------------------

    // Clicks are read before the GUI sees them, since the world view fills the screen.
    public override void _Input(InputEvent e)
    {
        if (!placing || e is not InputEventMouseButton { Pressed: true } click) return;
        if (click.ButtonIndex == MouseButton.Left) Place();
        else if (click.ButtonIndex == MouseButton.Right) StopPlacing();
        else return;
        GetViewport().SetInputAsHandled();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!placing && UseInput(e))
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        if (e.IsActionPressed("build_mode"))
        {
            if (placing) StopPlacing();
            else StartPlacing();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (!placing) return;

        if (e.IsActionPressed("build_cancel"))
        {
            StopPlacing();
            GetViewport().SetInputAsHandled();
        }
        else if (e.IsActionPressed("build_next"))
        {
            placeKind = Structures.Kinds[(System.Array.IndexOf(Structures.Kinds, placeKind) + 1) % Structures.Kinds.Length];
            MakeGhost();
            GetViewport().SetInputAsHandled();
        }
        else if (e.IsActionPressed("build_rotate"))
        {
            ghostYawTarget += Mathf.Pi / 4f * (Input.IsKeyPressed(Key.Shift) ? -1 : 1);
            GetViewport().SetInputAsHandled();
        }
        else if (e is InputEventKey { Pressed: true, Keycode: Key.Enter or Key.KpEnter })
        {
            Place();
            GetViewport().SetInputAsHandled();
        }
    }

    // Starts placing a particular kind (the picture build menu, ui-20); B still starts with the last one.
    public void StartPlacing(int kind)
    {
        if (!Structures.Exists(kind)) return;
        placeKind = kind;
        StartPlacing();
    }

    void StartPlacing()
    {
        if (conn() == null)
        {
            hud.Note("Building needs the server");
            return;
        }
        placing = true;
        ghostPlaced = false;
        // Face the open side toward the camera, so you look into what you build.
        ghostYawTarget = ghostYaw = Mathf.Snapped(Mathf.Atan2(-cameraRig.Forward.X, -cameraRig.Forward.Z), Mathf.Pi / 4f);
        MakeGhost();
    }

    void StopPlacing()
    {
        placing = false;
        ghost?.QueueFree();
        ghost = null;
    }

    void MakeGhost()
    {
        ghost?.QueueFree();
        ghost = new StructureView(placeKind, ghostMaterial);
        ghost.Show(StructureModel.StageCount(placeKind), 1f, complete: true, animate: false);
        root.AddChild(ghost);
        ghostPlaced = false;
    }

    void Place()
    {
        if (conn() is not { } c) return;
        if (placeProblem != null)
        {
            hud.Note(placeProblem);
            return;
        }
        c.Reducers.PlaceStructure((StructureKind)placeKind, ghostPos.X, ghostPos.Z, ghostYawTarget);
        Sfx.Play(Cue.Place);
        // Shift keeps the ghost up for another of the same kind (fences, walls, houses in a row).
        if (Input.IsKeyPressed(Key.Shift)) MakeGhost();
        else StopPlacing();
    }

    // --- Frame loop -------------------------------------------------------

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (placing) UpdateGhost(dt);
        UpdateUse();
        UpdateWeathering(dt);
        UpdateSeeThrough(dt);
        UpdatePrompt();
    }

    // Finished shelters cut away while the player is inside or behind them (D-168): roof gone
    // from inside, camera-side walls faded, far walls solid.
    void UpdateSeeThrough(float dt)
    {
        var camera = me.GetViewport()?.GetCamera3D();
        foreach (var s in rows.Values)
        {
            if (!views.TryGetValue(s.Id, out var v)) continue;
            var shape = ShapeOf(s);
            bool inside = false, hides = false;
            if (s.Complete && shape.RainCover > 0f && camera != null
                && new Vector2(s.X - me.Position.X, s.Z - me.Position.Z).LengthSquared() < 25f * 25f)
            {
                inside = Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, me.Position.X, me.Position.Z, -0.2f);
                hides = inside || Look.HidesPlayer(Footprint(s, shape, v.Position.Y), camera.GlobalPosition, me.Position);
            }
            if (hides || v.Fading) v.UpdateSeeThrough(inside, hides, camera?.GlobalPosition ?? Vector3.Zero, dt);
        }
    }

    // How far the structure is cut away for the player right now, 0 solid to 1 fully clear
    // (0 for a structure with no view). Doorway glows and the like fade with it.
    public float SeeThroughOf(ulong structureId) => views.TryGetValue(structureId, out var v) ? v.SeeThroughAmount : 0f;

    // The world-space box a shelter fills, roof included, for the see-through test.
    static Aabb Footprint(Structure s, StructureShape shape, float ground)
    {
        float height = 3.7f * shape.Scale, eave = 0.5f * shape.Scale; // up to the ridge cap, and out past the eaves (homes are 1.25x, building-14)
        float hx = (shape.Round ? shape.Radius : shape.HalfWidth) + eave, hz = (shape.Round ? shape.Radius : shape.HalfDepth) + eave;
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var (lx, lz) in new[] { (-hx, -hz), (hx, -hz), (-hx, hz), (hx, hz) })
        {
            var (wx, wz) = Structures.ToWorld(s.X, s.Z, s.Yaw, lx, lz);
            minX = Mathf.Min(minX, wx); maxX = Mathf.Max(maxX, wx);
            minZ = Mathf.Min(minZ, wz); maxZ = Mathf.Max(maxZ, wz);
        }
        return new Aabb(new Vector3(minX, ground - 0.5f, minZ), new Vector3(maxX - minX, height + 0.5f, maxZ - minZ));
    }

    void UpdateGhost(float dt)
    {
        if (ghost == null) return;
        var target = PointUnderMouse() ?? me.Position + cameraRig.Forward * 3.5f;
        var offset = target - me.Position;
        offset.Y = 0;
        if (offset.Length() > Structures.PlaceRange - 0.3f)
            target = me.Position + offset.Normalized() * (Structures.PlaceRange - 0.3f);
        target.Y = terrain.HeightAt(target.X, target.Z);

        if (Structures.IsFence(placeKind)) target = SnapFence(target);

        // The ghost glides after the cursor rather than jumping cell to cell.
        float ease = 1f - Mathf.Exp(-14f * dt);
        ghostPos = ghostPlaced ? ghostPos.Lerp(target, ease) : target;
        ghostPlaced = true;
        ghostYaw = Mathf.LerpAngle(ghostYaw, ghostYawTarget, 1f - Mathf.Exp(-12f * dt));
        ghost.Position = new Vector3(ghostPos.X, terrain.HeightAt(ghostPos.X, ghostPos.Z), ghostPos.Z);
        ghost.Rotation = new Vector3(0, ghostYaw, 0);

        placeProblem = PlaceProblem(ghostPos, ghostYawTarget);
        var tint = placeProblem == null ? new Color(0.85f, 1f, 0.8f, 0.38f) : new Color(1f, 0.6f, 0.5f, 0.3f);
        ghostMaterial.AlbedoColor = ghostMaterial.AlbedoColor.Lerp(tint, 1f - Mathf.Exp(-10f * dt));
    }

    // A fence laid near the free end of another fence or gate joins on to it, along
    // the ghost's own heading (T turns it, so runs can bend at corners).
    // Claims implements this in its own file (claims-12) to lay fences along a claim's
    // border: set done, with the spot and heading, and end-to-end snapping is skipped.
    partial void SnapFenceHook(Vector3 target, ref Vector3 snapped, ref float yaw, ref bool done);

    Vector3 SnapFence(Vector3 target)
    {
        Vector3 claimed = target;
        bool done = false;
        SnapFenceHook(target, ref claimed, ref ghostYawTarget, ref done);
        if (done) return claimed;
        const float reach = 0.9f;
        Vector3? best = null;
        float bestD = reach;
        var axis = new Vector3(Mathf.Cos(ghostYawTarget), 0f, -Mathf.Sin(ghostYawTarget)) * Structures.FenceHalfLength;
        foreach (var s in rows.Values)
        {
            if (!Structures.IsFence((int)s.Kind)) continue;
            var (a, b) = Structures.FenceEnds(s.X, s.Z, s.Yaw);
            foreach (var (ex, ez) in new[] { a, b })
            {
                var end = new Vector3(ex, target.Y, ez);
                // The new fence's nearer end sits on this end, its middle toward the cursor.
                var center = (target - end).Dot(axis) >= 0f ? end + axis : end - axis;
                float d = new Vector2(center.X - target.X, center.Z - target.Z).Length();
                if (d < bestD)
                {
                    bestD = d;
                    best = center;
                }
            }
        }
        if (best is not { } snapped) return target;
        snapped.Y = terrain.HeightAt(snapped.X, snapped.Z);
        return snapped;
    }

    string? PlaceProblem(Vector3 at, float yaw)
    {
        var mine = MyIdentity();
        if (mine != null && rows.Values.Count(s => s.Owner == mine && !s.Complete) >= Structures.MaxOpenSites)
            return "Finish one of your building sites first";
        foreach (var s in rows.Values)
            if (Structures.TooClose(placeKind, at.X, at.Z, (int)s.Kind, s.X, s.Z))
                return "Too close to another building";
        // A hearth goes inside a finished hut or house, a little way in from the walls (as the server checks).
        if (Structures.IsIndoor(placeKind) && !rows.Values.Any(s => s.Complete && Structures.IsHome((int)s.Kind)
                && Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, at.X, at.Z, -Structures.HearthWallGap)))
            return "A hearth goes inside a finished hut or house";
        // Only basic survival buildings stand outside a claim (building-13); the server checks the same.
        if (!Structures.IsWild(placeKind) && !MayBar(at.X, at.Z))
            return $"A {Structures.Shape(placeKind).Name.ToLowerInvariant()} has to stand inside your claim";
        return Structures.GroundProblem(placeKind, at.X, at.Z, yaw, terrain.HeightAt);
    }

    // True when this kind can't be laid out where the player stands because it needs a claim
    // the player belongs to (the build menu greys it with "Needs a claim").
    public bool NeedsClaimHere(int kind) => !Structures.IsWild(kind) && !MayBar(me.Position.X, me.Position.Z);

    // Where the mouse points on the ground, found by stepping along the camera ray.
    Vector3? PointUnderMouse()
    {
        var camera = cameraRig.Camera;
        var screen = GetViewport().GetVisibleRect().Size;
        var mouse = GetViewport().GetMousePosition() * new Vector2(view.Size.X / screen.X, view.Size.Y / screen.Y);
        var origin = camera.ProjectRayOrigin(mouse);
        var dir = camera.ProjectRayNormal(mouse);
        float prev = 0f;
        for (float t = 0.5f; t < 120f; t += 0.5f)
        {
            var p = origin + dir * t;
            if (p.Y > terrain.HeightAt(p.X, p.Z)) { prev = t; continue; }
            // Narrow down between the last point above ground and this one below.
            float lo = prev, hi = t;
            for (int i = 0; i < 8; i++)
            {
                float mid = (lo + hi) / 2;
                var m = origin + dir * mid;
                if (m.Y > terrain.HeightAt(m.X, m.Z)) lo = mid; else hi = mid;
            }
            return origin + dir * hi;
        }
        return null;
    }

    // A plain loop: this runs every frame for the Use key and the prompt (bugfix-8).
    Structure? NearestSite()
    {
        Structure? best = null;
        float bestD = float.MaxValue;
        foreach (var s in rows.Values)
        {
            if (s.Complete || !Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, me.Position.X, me.Position.Z, Structures.WorkReach)) continue;
            float d = new Vector2(s.X - me.Position.X, s.Z - me.Position.Z).LengthSquared();
            if (d < bestD) { best = s; bestD = d; }
        }
        return best;
    }

    void UpdatePrompt()
    {
        string? text = null;
        if (placing)
        {
            var shape = Structures.Shape(placeKind);
            string name = shape.Name.ToLowerInvariant();
            text = $"Lay out {("aeiou".Contains(name[0]) ? "an" : "a")} {name}    Tab change    T turn    click to place    Shift keeps placing    Esc cancel";
            if (placeProblem != null) text += $"\n{placeProblem}";
            else text += $"\nNeeds {StageList(placeKind)}";
        }
        else if (NearestSite() is { } site)
        {
            var shape = ShapeOf(site);
            string stage = shape.Stages[Math.Min(site.Stage, shape.Stages.Length - 1)];
            var have = string.Join("   ", site.Needs.Select(n => $"{ItemName(n.Item, n.Need)} {n.Have}/{n.Need}"));
            text = $"{shape.Name}: {stage} ({site.Stage + 1} of {shape.Stages.Length})    {have}";
            // Out of what it still needs: say what to fetch, since G has nothing to add.
            var missing = site.Needs.Where(n => n.Have < n.Need).ToList();
            if (conn() is { } c && missing.Count > 0 && missing.All(n => Carried(c, (Item)n.Item) == 0))
                text += "\nNeeds " + string.Join(" and ", missing.Select(n => $"{n.Need - n.Have} more {ItemName(n.Item, n.Need - n.Have)}"));
        }
        else if (UsePrompt() is { } use)
            text = use;
        else if (ShelterAt(me.Position) is { } home)
            text = $"Sheltered in the {ShapeOf(home).Name.ToLowerInvariant()}";

        promptPanel.Visible = text != null;
        if (text != null) prompt.Text = text;
    }

    // --- Helpers ----------------------------------------------------------

    // What each stage takes, in order: "4 sticks, then 3 plant fiber, then ...".
    static string StageList(int kind) =>
        string.Join(", then ", Structures.Costs(kind).Select(stage =>
            string.Join(" and ", stage.Select(c => $"{c.Count} {ItemName((ushort)c.Item, c.Count)}"))));

    static string ItemName(ushort item, uint count) =>
        Items.IsKnown(item) ? Items.Def((Item)item).Count(count).ToLowerInvariant() : "something";

    SpacetimeDB.Identity? MyIdentity() => conn()?.Identity;

    void Puff(Vector3 at, Color color)
    {
        var particles = new CpuParticles3D
        {
            Emitting = true,
            OneShot = true,
            Amount = 18,
            Lifetime = 0.9f,
            Explosiveness = 0.85f,
            Direction = Vector3.Up,
            Spread = 70f,
            InitialVelocityMin = 0.8f,
            InitialVelocityMax = 1.8f,
            Gravity = new Vector3(0, -3f, 0),
            ScaleAmountMin = 0.5f,
            ScaleAmountMax = 1.1f,
            Mesh = new BoxMesh { Size = Vector3.One * 0.06f, Material = Look.Solid(color) },
            Position = at,
        };
        view.AddChild(particles);
        GetTree().CreateTimer(1.4).Timeout += particles.QueueFree;
    }

    static void BindKey(string action, Key key)
    {
        if (InputMap.HasAction(action)) return;
        InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }
}
