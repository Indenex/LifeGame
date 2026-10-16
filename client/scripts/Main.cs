using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LifeGame;

// Sets up the scene and ties input, the world and the server connection together.
// Server: --server=http://host:3000 and --db=lifegame after "--" on the command line,
// or the LIFEGAME_SERVER and LIFEGAME_DB environment variables.
public partial class Main : Node3D
{
    // Cached names: a string here would build a new StringName every frame.
    static readonly StringName NWalkLeft = "walk_left";
    static readonly StringName NWalkRight = "walk_right";
    static readonly StringName NWalkBack = "walk_back";
    static readonly StringName NWalkForward = "walk_forward";
    static readonly StringName NSneak = "sneak";

    const float WalkSpeed = 4.2f;
    const float SendInterval = World.MoveTickMillis / 1000f;

    readonly Terrain terrain = new();
    readonly CameraRig cameraRig = new();
    readonly Hud hud = new();
    readonly SocialLayer social = new();
    readonly UseKey useKey = new();
    readonly Journal journal = new();
    readonly CraftingUi crafting = new();
    FireView fires = null!;
    CeramicsView clay = null!;
    readonly Atmosphere atmosphere = new();
    readonly SurvivalClient survival = new();
    readonly Avatar me = new("You");
    readonly Herd herd = new();
    readonly Dictionary<uint, Avatar> others = new();
    readonly Dictionary<ulong, Node3D> items = new();
    readonly Dictionary<uint, string> names = new();
    BuildingSystem building = null!;

    DbConnection? conn;
    Identity? myIdentity;
    uint myId;
    bool spawned;
    Vector3 velocity;
    float heading, sendTimer;
    Vector3 lastSent = new(float.NaN, 0, float.NaN);
    (int, int) subscribedChunk = (int.MinValue, int.MinValue);
    SubscriptionHandle? subscription;
    MeshInstance3D water = null!;
    readonly SubViewport view = new() { Msaa3D = Viewport.Msaa.Msaa4X, ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Fxaa };

    public override void _Ready()
    {
        SetupInput();
        SetupView();
        SetupEnvironment();
        atmosphere.Player = me;
        view.AddChild(atmosphere);
        view.AddChild(terrain);
        view.AddChild(me);
        view.AddChild(herd);
        view.AddChild(cameraRig);
        fires = new FireView(terrain.HeightAt);
        view.AddChild(fires);
        clay = new CeramicsView(view, terrain.HeightAt, me, hud.Note);
        AddChild(clay);
        AddChild(hud);
        AddChild(social);
        AddChild(journal);
        view.AddChild(journal.Notes);
        AddChild(crafting);
        crafting.Notice += hud.Note;
        building = new BuildingSystem(terrain, hud, me, cameraRig, view, () => conn);
        atmosphere.Sheltered = p => building.ShelterAt(p) != null;
        AddChild(building);
        survival.Setup(view, terrain, me, hud);
        AddChild(survival);
        // Last child, so G reaches the Use key before any lane's old binding.
        useKey.Player = () => me.Position;
        useKey.Facing = () => new Vector3(Mathf.Sin(me.Rotation.Y), 0, Mathf.Cos(me.Rotation.Y));
        useKey.Blocked = () => crafting.CapturesInput || building.Placing || Panels.Open();
        JournalPanel? journalPanel = null;
        ClaimPanel? claimPanel = null;
        Panels.Open = () => PauseMenu.IsOpen
            || (journalPanel ??= journal.GetChildren().OfType<JournalPanel>().FirstOrDefault())?.IsOpen == true
            || (claimPanel ??= social.GetChildren().OfType<ClaimPanel>().FirstOrDefault())?.IsOpen == true;
        herd.Blocked = () => crafting.CapturesInput || building.Placing || Panels.Open();
        herd.Facing = () => new Vector3(Mathf.Sin(me.Rotation.Y), 0, Mathf.Cos(me.Rotation.Y));
        AddChild(useKey);
        AddChild(new Hints());
        var keyHelp = new KeyHelp();
        AddChild(keyHelp);
        // Esc belongs to whatever panel, chat or placement is open; otherwise it opens the menu.
        var menu = new PauseMenu { Keys = keyHelp, OthersOpen = () => crafting.CapturesInput || building.Placing || social.Typing || GetViewport().GuiGetFocusOwner() != null };
        AddChild(menu);
        // First child, so Esc reaches it only after every lane panel had its turn.
        MoveChild(menu, 0);
        AddChild(new CameraDrag { Rig = cameraRig, Blocked = () => building.Placing });
        RegisterUse();
        cameraRig.Camera.MakeCurrent();
        if (CeramicsDemo.Wanted) view.AddChild(new CeramicsDemo(terrain, me));
        if (Arg("frame-stats") is { } stats) AddChild(new FrameStats(stats));
        Connect();
        ScheduleScreenshot();
    }

    // --screenshot=path.png [--screenshot-delay=6] saves one frame and quits. Used to
    // check the look in CI and on machines without a display.
    void ScheduleScreenshot()
    {
        if (Arg("screenshot") is not { } path) return;
        double delay = double.TryParse(Arg("screenshot-delay"), out var d) ? d : 6;
        if (Arg("screenshot-yaw") is { } yaw && int.TryParse(yaw, out var steps)) cameraRig.Turn(steps);
        // --screenshot-craft=1 opens the crafting panel on that tier.
        if (Arg("screenshot-craft") is { } craftTier && int.TryParse(craftTier, out var t))
            GetTree().CreateTimer(Math.Max(1, delay - 3)).Timeout += () => crafting.Open(t);
        // --screenshot-fire places a local campfire just in front of the player.
        if (Arg("screenshot-fire") != null)
            GetTree().CreateTimer(Math.Max(1, delay - 4)).Timeout += () =>
                fires.Preview(me.Position + new Vector3(Mathf.Sin(cameraRig.Rotation.Y), 0, Mathf.Cos(cameraRig.Rotation.Y)) * -1.6f + new Vector3(1.2f, 0, 0));
        GetTree().CreateTimer(delay).Timeout += () =>
        {
            GetViewport().GetTexture().GetImage().SavePng(path);
            GetTree().Quit();
        };
    }

    // The world renders at full resolution by default. --pixel=2 or more renders
    // it smaller and scales it up with nearest filtering, for a retro look.
    void SetupView()
    {
        int pixel = int.TryParse(Arg("pixel"), out var p) ? Math.Max(1, p) : 1;
        var layer = new CanvasLayer { Layer = -1 };
        var container = new SubViewportContainer
        {
            Stretch = true,
            StretchShrink = pixel,
            TextureFilter = pixel > 1 ? CanvasItem.TextureFilterEnum.Nearest : CanvasItem.TextureFilterEnum.Linear,
        };
        container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        container.AddChild(view);
        layer.AddChild(container);
        AddChild(layer);

        // A soft vignette, like the edges of a painting.
        var vignette = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };
        vignette.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vignette.Material = new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = """
                    shader_type canvas_item;
                    void fragment() {
                        vec2 uv = (SCREEN_UV - 0.5) * vec2(1.0, 0.85);
                        float edge = smoothstep(0.4, 0.85, length(uv));
                        COLOR = vec4(0.04, 0.05, 0.06, edge * 0.45);
                    }
                    """,
            },
        };
        var vignetteLayer = new CanvasLayer { Layer = 0 };
        vignetteLayer.AddChild(vignette);
        AddChild(vignetteLayer);
    }

    void SetupEnvironment()
    {
        var sky = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color(0.30f, 0.45f, 0.62f),
            SkyHorizonColor = new Color(0.80f, 0.74f, 0.62f),
            SkyCurve = 0.12f,
            GroundBottomColor = new Color(0.24f, 0.28f, 0.22f),
            GroundHorizonColor = new Color(0.86f, 0.76f, 0.64f),
            SunAngleMax = 20f,
            SunCurve = 0.08f,
        };
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = sky },
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightSkyContribution = 0.75f,
            AmbientLightEnergy = 0.45f,
            TonemapMode = Godot.Environment.ToneMapper.Aces,
            TonemapExposure = 1.0f,
            TonemapWhite = 6f,
            SsaoEnabled = true,
            SsaoRadius = 1.6f,
            SsaoIntensity = 1.8f,
            SsaoDetail = 0.8f,
            // Light bouncing off the grass tints the shadows green.
            SsilEnabled = true,
            SsilRadius = 3f,
            SsilIntensity = 0.9f,
            GlowEnabled = true,
            GlowIntensity = 0.6f,
            GlowBloom = 0.08f,
            GlowHdrThreshold = 1.1f,
            // Distance haze that swallows the far forest.
            FogEnabled = true,
            FogLightColor = new Color(0.62f, 0.68f, 0.70f),
            FogDensity = 0.006f,
            FogSkyAffect = 0.5f,
            FogAerialPerspective = 0.5f,
            // Volumetric fog catches the sun between the trees.
            VolumetricFogEnabled = true,
            VolumetricFogDensity = 0.007f,
            VolumetricFogAlbedo = new Color(0.86f, 0.88f, 0.86f),
            VolumetricFogAnisotropy = 0.6f,
            VolumetricFogLength = 64f,
            VolumetricFogAmbientInject = 0.4f,
            VolumetricFogSkyAffect = 0.3f,
            AdjustmentEnabled = true,
            AdjustmentSaturation = 1.0f,
            AdjustmentContrast = 1.08f,
        };
        view.AddChild(new WorldEnvironment { Environment = env });

        // Late-afternoon sun: low and warm, with long shadows.
        view.AddChild(new DirectionalLight3D
        {
            LightColor = new Color(1.0f, 0.86f, 0.66f),
            LightEnergy = 2.4f,
            LightAngularDistance = 0.8f,
            LightVolumetricFogEnergy = 1.6f,
            ShadowEnabled = true,
            ShadowBlur = 1.2f,
            DirectionalShadowMaxDistance = 60f,
            Rotation = new Vector3(Mathf.DegToRad(-24f), Mathf.DegToRad(-40f), 0),
        });

        water = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(260, 260) },
            MaterialOverride = Look.WaterSurface,
            Position = new Vector3(0, Look.WaterLevel, 0),
        };
        view.AddChild(water);
    }

    static void SetupInput()
    {
        Controls.Setup();
        void Bind(string action, params Key[] keys)
        {
            if (InputMap.HasAction(action)) return;
            InputMap.AddAction(action);
            foreach (var key in keys) InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
        }
        Bind("walk_forward", Key.W, Key.Up);
        Bind("walk_back", Key.S, Key.Down);
        Bind("walk_left", Key.A, Key.Left);
        Bind("walk_right", Key.D, Key.Right);
        Bind("turn_left", Key.Q);
        Bind("turn_right", Key.E);
        Bind("dig", Key.F);
        Bind("pick_up", Key.Space);
        Bind("craft", Key.C);
        Bind("sneak", Key.Shift);
    }

    // --- Networking -------------------------------------------------------

    void Connect()
    {
        string server = Arg("server") ?? OS.GetEnvironment("LIFEGAME_SERVER");
        if (string.IsNullOrEmpty(server)) server = "http://127.0.0.1:3000";
        string db = Arg("db") ?? OS.GetEnvironment("LIFEGAME_DB");
        if (string.IsNullOrEmpty(db)) db = "lifegame";

        hud.SetStatus("Connecting...");
        var builder = DbConnection.Builder()
            .WithUri(server)
            .WithDatabaseName(db)
            .WithCompression(Compression.Brotli)
            .OnConnect(OnConnected)
            .OnConnectError(e => CallDeferred(nameof(GoOffline), e.Message))
            .OnDisconnect((_, e) => CallDeferred(nameof(GoOffline), e?.Message ?? "disconnected"));
        string? token = LoadToken();
        if (token != null) builder = builder.WithToken(token);
        conn = builder.Build();
        journal.Attach(conn, () => me.Position, terrain.HeightAt, hud.Note);

        conn.Db.Player.OnInsert += (_, p) => OnPlayer(p);
        conn.Db.Player.OnUpdate += (_, _, p) => OnPlayer(p);
        // Only online players are subscribed, so a delete means they logged off.
        conn.Db.Player.OnDelete += (_, p) =>
        {
            if (others.TryGetValue(p.PlayerId, out var a)) a.Visible = false;
        };
        conn.Db.PlayerPos.OnInsert += (_, p) => OnPlayerPos(p, true);
        conn.Db.PlayerPos.OnUpdate += (_, _, p) => OnPlayerPos(p, false);
        conn.Db.PlayerPos.OnDelete += (_, p) => RemoveAvatar(p.PlayerId);
        conn.Db.WorldItem.OnInsert += (_, item) => AddItem(item);
        conn.Db.WorldItem.OnDelete += (_, item) => RemoveItem(item.Id);
        conn.Db.TerrainEdit.OnInsert += (_, e) => terrain.SetDepth(e.CellX, e.CellZ, e.Depth);
        conn.Db.TerrainEdit.OnUpdate += (_, _, e) => terrain.SetDepth(e.CellX, e.CellZ, e.Depth);
        conn.Db.MyInventory.OnInsert += (_, _) => RefreshInventory();
        conn.Db.MyInventory.OnUpdate += (_, _, _) => RefreshInventory();
        conn.Db.MyInventory.OnDelete += (_, _) => RefreshInventory();
        herd.Bind(conn, terrain, () => me.Position);
        conn.OnUnhandledReducerError += (_, e) => ServerCheck.Failed(e.Message, hud.Note, "");
        social.Attach(conn, view, terrain, hud, id => id == myId ? me : others.GetValueOrDefault(id));
        crafting.Attach(conn);
        fires.Attach(conn);
        clay.Attach(conn);
        building.Bind(conn);
        survival.Attach(conn);
    }

    void OnConnected(DbConnection c, Identity identity, string token)
    {
        myIdentity = identity;
        SaveToken(token);
        hud.SetStatus("Connected");
        ServerCheck.Warn = hud.Warn;
        c.SubscriptionBuilder()
            .OnApplied(_ => { if (c.Db.ServerInfo.Id.Find(0) is { } info) ServerCheck.Compare(info.Build); })
            .OnError((_, e) => ServerCheck.Failed(e.Message, hud.Note))
            .Subscribe(new[] { "SELECT * FROM server_info" });
        SubscribeSelf();
        atmosphere.Connect(c);
    }

    void GoOffline(string reason)
    {
        hud.SetStatus($"Offline: {reason}. Walking alone.");
        conn = null;
        journal.Detach();
        Spawn(Vector3.Zero);
    }

    // Our own rows come first, wherever we saved, then the area around them. The
    // area around the origin would miss a player who logged off far from it (bugfix-1).
    void SubscribeSelf()
    {
        if (conn == null) return;
        conn.SubscriptionBuilder()
            .OnApplied(_ =>
            {
                var at = MyPos();
                Subscribe(World.ChunkOf(at?.X ?? 0), World.ChunkOf(at?.Z ?? 0));
            })
            .OnError((_, e) =>
            {
                ServerCheck.Failed(e.Message, hud.Note);
                Subscribe(0, 0);
            })
            .Subscribe(Subscriptions.Self);
    }

    PlayerPos? MyPos() =>
        conn != null && myIdentity is { } id && conn.Db.Player.Identity.Find(id) is { } p
            ? conn.Db.PlayerPos.PlayerId.Find(p.PlayerId) : null;

    void Subscribe(int chunkX, int chunkZ)
    {
        if (conn == null || subscribedChunk == (chunkX, chunkZ)) return;
        subscribedChunk = (chunkX, chunkZ);
        string range = Subscriptions.Range(chunkX, chunkZ, World.ViewRadiusChunks);
        var old = subscription;
        // Subscribe to the new area before dropping the old one so nothing pops out.
        // The first area is where we wake up: appear once it has loaded, all at once.
        subscription = conn.SubscriptionBuilder()
            .OnApplied(_ =>
            {
                old?.Unsubscribe();
                if (!spawned && MyPos() is { } p) Spawn(new Vector3(p.X, terrain.HeightAt(p.X, p.Z), p.Z));
            })
            .OnError((_, e) =>
            {
                ServerCheck.Failed(e.Message, hud.Note);
                // Still wake up and walk: one failed subscription must not freeze the player.
                if (!spawned) Spawn(MyPos() is { } p ? new Vector3(p.X, terrain.HeightAt(p.X, p.Z), p.Z) : Vector3.Zero);
            })
            .Subscribe(Subscriptions.Area(range).Concat(SurvivalClient.Queries(range)).Concat(BuildingSystem.Queries()).ToArray());
    }

    void OnPlayer(Player p)
    {
        names[p.PlayerId] = p.Name;
        if (p.Identity == myIdentity) myId = p.PlayerId;
        if (others.TryGetValue(p.PlayerId, out var avatar))
        {
            avatar.SetPlayerName(p.Name);
            avatar.Visible = p.Online;
        }
    }

    void OnPlayerPos(PlayerPos p, bool inserted)
    {
        var at = new Vector3(p.X, terrain.HeightAt(p.X, p.Z), p.Z);
        if (myId != 0 && p.PlayerId == myId || (myId == 0 && IsMine(p.PlayerId)))
        {
            // Before spawning we wait for the area around us (see Subscribe).
            if (!spawned) return;
            // The server clamps impossible moves; snap back if we drifted too far.
            if (new Vector2(me.Position.X - p.X, me.Position.Z - p.Z).Length() > 3f)
                me.Position = at;
            return;
        }

        if (!others.TryGetValue(p.PlayerId, out var avatar))
        {
            avatar = new Avatar(names.GetValueOrDefault(p.PlayerId, "Wanderer")) { Remote = true, Position = at };
            others[p.PlayerId] = avatar;
            view.AddChild(avatar);
        }
        avatar.Target = at;
        avatar.TargetHeading = p.Heading;
        avatar.Visible = conn?.Db.Player.PlayerId.Find(p.PlayerId) is { Online: true };
        if (inserted) avatar.Position = at;
    }

    bool IsMine(uint playerId)
    {
        if (conn == null || myIdentity == null) return false;
        if (conn.Db.Player.Identity.Find(myIdentity.Value) is not { } mine) return false;
        myId = mine.PlayerId;
        return mine.PlayerId == playerId;
    }

    void RemoveAvatar(uint playerId)
    {
        if (others.Remove(playerId, out var avatar)) avatar.QueueFree();
    }

    void RefreshInventory()
    {
        if (conn == null) return;
        // Stacks whose qualities share a grade read the same, so they show as one slot.
        var stacks = conn.Db.MyInventory.Iter()
            .OrderBy(i => i.Item).ThenByDescending(i => i.Quality)
            .GroupBy(i => (i.Item, Grade: Items.IsKnown(i.Item) && Items.Def((Item)i.Item).HasQuality ? Processes.Grade(i.Quality) : ""))
            .Select(g => { uint n = (uint)g.Sum(i => i.Count); return new InventoryEntry((Item)g.Key.Item, ItemLook.Label((Item)g.Key.Item, g.First().Quality, n), n); })
            .ToList();
        hud.SetItems(stacks);
    }

    // --- Items on the ground ---------------------------------------------

    void AddItem(WorldItem item)
    {
        if (items.ContainsKey(item.Id)) return;
        var node = new MeshInstance3D { Mesh = ItemLook.MeshFor((Item)item.Item) };
        float yaw = Terrain.Hash01((int)item.Id, 3, 21) * Mathf.Tau;
        node.Position = new Vector3(item.X, terrain.HeightAt(item.X, item.Z) + 0.04f, item.Z);
        node.Rotation = new Vector3(0, yaw, 0);
        items[item.Id] = node;
        view.AddChild(node);
    }

    void RemoveItem(ulong id)
    {
        if (items.Remove(id, out var node)) node.QueueFree();
    }

    // --- Frame loop -------------------------------------------------------

    void Spawn(Vector3 at)
    {
        spawned = true;
        me.Position = at;
        cameraRig.Focus = at;
        cameraRig.Position = at;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("turn_left")) cameraRig.Turn(1);
        if (e.IsActionPressed("turn_right")) cameraRig.Turn(-1);
        if (e is InputEventMouseButton { Pressed: true } mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp) cameraRig.Zoom(-2f);
            if (mb.ButtonIndex == MouseButton.WheelDown) cameraRig.Zoom(2f);
        }
        if (e.IsActionPressed("dig")) Dig();
        if (e.IsActionPressed("pick_up")) PickUp();
        if (e.IsActionPressed("craft")) crafting.Toggle();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        conn?.FrameTick();

        if (spawned && !PauseMenu.IsOpen && !crafting.CapturesInput) Walk(dt);
        social.Track(me.Position, myId);
        terrain.Focus(World.ChunkOf(me.Position.X), World.ChunkOf(me.Position.Z));
        water.Position = new Vector3(Mathf.Round(me.Position.X), Look.WaterLevel, Mathf.Round(me.Position.Z));
        cameraRig.Focus = me.Position + new Vector3(0, 0.9f, 0);

        if (conn != null && spawned)
        {
            Subscribe(World.ChunkOf(me.Position.X), World.ChunkOf(me.Position.Z));
            sendTimer -= dt;
            if (sendTimer <= 0f)
            {
                sendTimer = SendInterval;
                if (me.Position.DistanceTo(lastSent) > 0.01f || float.IsNaN(lastSent.X))
                {
                    conn.Reducers.Move(me.Position.X, me.Position.Z, heading);
                    lastSent = me.Position;
                }
            }
        }
    }

    void Walk(float dt)
    {
        var input = social.Typing ? Vector2.Zero : Input.GetVector(NWalkLeft, NWalkRight, NWalkBack, NWalkForward);
        var wish = (cameraRig.Right * input.X + cameraRig.Forward * input.Y) * (Input.IsActionPressed(NSneak) ? World.SneakSpeed : WalkSpeed) * survival.WalkFactor;
        // Ease in and out of walking rather than starting and stopping instantly.
        velocity = velocity.Lerp(wish, 1f - Mathf.Exp(-(wish == Vector3.Zero ? 10f : 7f) * dt));

        var pos = building.Collide(me.Position, me.Position + velocity * dt);
        // Deep water is not walkable yet, but a step that climbs is always allowed,
        // so nobody gets trapped in a flooded hole they dug.
        float next = terrain.HeightAt(pos.X, pos.Z);
        if (next < Look.WaterLevel - 0.3f && next < terrain.HeightAt(me.Position.X, me.Position.Z))
            pos = me.Position;
        // Ease up and down slopes instead of popping between heights.
        pos.Y = Mathf.Lerp(me.Position.Y, terrain.HeightAt(pos.X, pos.Z), 1f - Mathf.Exp(-14f * dt));
        me.Position = pos;

        if (velocity.LengthSquared() > 0.05f)
        {
            heading = Mathf.Atan2(velocity.X, velocity.Z);
            me.Rotation = new Vector3(0, Mathf.LerpAngle(me.Rotation.Y, heading, 1f - Mathf.Exp(-12f * dt)), 0);
        }
    }

    void Dig()
    {
        var front = me.Position + new Vector3(Mathf.Sin(me.Rotation.Y), 0, Mathf.Cos(me.Rotation.Y)) * 1.0f;
        int cx = World.CellOf(front.X), cz = World.CellOf(front.Z);
        Dust(new Vector3(cx, terrain.TileHeight(cx, cz), cz), World.IsClay(cx, cz) ? Look.Clay : Look.DugEarth);
        if (conn == null)
        {
            hud.Note("Digging needs the server");
            return;
        }
        conn.Reducers.Dig(cx, cz);
    }

    void PickUp()
    {
        if (conn == null) return;
        var nearest = conn.Db.WorldItem.Iter()
            .Select(i => (item: i, d: new Vector2(i.X - me.Position.X, i.Z - me.Position.Z).Length()))
            .Where(t => t.d <= World.Reach)
            .OrderBy(t => t.d)
            .FirstOrDefault();
        if (nearest.item == null) hud.Note("Nothing within reach");
        else conn.Reducers.PickUp(nearest.item.Id);
    }

    // G on a fire puts the cheapest fuel you carry on it.
    void RegisterUse()
    {
        Use.Register(new UseHandler
        {
            Id = "crafting.feed_fire",
            Priority = 20,
            Find = at =>
            {
                if (conn == null || fires.NearestWithin(conn, at, World.Reach) is not { } fire) return null;
                var fuel = conn.Db.MyInventory.Iter()
                    .Where(i => i.Count > 0 && Items.IsKnown(i.Item) && Items.Def((Item)i.Item).FuelSeconds > 0)
                    .OrderBy(i => Items.Def((Item)i.Item).FuelSeconds > 30 ? 0 : 1)
                    .FirstOrDefault();
                if (fuel == null) return null;
                float d = new Vector2(fire.X - at.X, fire.Z - at.Z).Length();
                return new UseOffer("Feed the fire", d,
                    () => conn.Reducers.FeedFire(fire.Id, fuel.Item), Target: new Vector3(fire.X, 0, fire.Z));
            },
        });
    }

    void Dust(Vector3 at, Color color)
    {
        var particles = new CpuParticles3D
        {
            Emitting = true,
            OneShot = true,
            Amount = 14,
            Lifetime = 0.7f,
            Explosiveness = 0.9f,
            Direction = Vector3.Up,
            Spread = 50f,
            InitialVelocityMin = 1.2f,
            InitialVelocityMax = 2.4f,
            Gravity = new Vector3(0, -7f, 0),
            ScaleAmountMin = 0.6f,
            ScaleAmountMax = 1.2f,
            Mesh = new BoxMesh { Size = Vector3.One * 0.07f, Material = Look.Solid(color) },
            Position = at + new Vector3(0, 0.05f, 0),
        };
        view.AddChild(particles);
        GetTree().CreateTimer(1.2).Timeout += particles.QueueFree;
    }

    // --- Helpers ----------------------------------------------------------

    // Accepts options with or without Godot's "--" separator, so both
    // "LifeGame --profile=2" and "LifeGame -- --profile=2" work.
    static string? Arg(string name)
    {
        foreach (var arg in OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()))
            if (arg.StartsWith($"--{name}="))
                return arg[(name.Length + 3)..];
        return null;
    }

    // One identity per install, so you keep your character between sessions.
    static string TokenPath => Arg("profile") is { } p ? $"user://token_{p}.txt" : "user://token.txt";

    static string? LoadToken()
    {
        if (!FileAccess.FileExists(TokenPath)) return null;
        using var file = FileAccess.Open(TokenPath, FileAccess.ModeFlags.Read);
        var token = file.GetAsText().Trim();
        return token.Length > 0 ? token : null;
    }

    static void SaveToken(string token)
    {
        using var file = FileAccess.Open(TokenPath, FileAccess.ModeFlags.Write);
        file.StoreString(token);
    }
}
