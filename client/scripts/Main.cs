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
    static readonly StringName NJump = "jump";

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
    readonly JumpMotion jump = new();
    readonly JumpView jumpView = new();
    readonly Dictionary<uint, Avatar> others = new();
    readonly GroundItems items = new();
    readonly Woodcutting woodcutting = new();
    readonly Dictionary<uint, string> names = new();
    BuildingSystem building = null!;

    DbConnection? conn;
    Identity? myIdentity;
    uint myId;
    readonly TradeWindow trade = new();
    readonly AdminPanel admin = new();
    readonly EquipmentPanel equipment = new();
    readonly StationPanel station = new();
    readonly BuildMenu buildMenu = new();
    readonly MarkPicker markPicker = new();
    readonly Placing placing = new();
    readonly LookPicker lookPicker = new();
    bool spawned;
    Vector3 velocity;
    float heading, sendTimer;
    Vector3 lastSent = new(float.NaN, 0, float.NaN);
    (int, int) subscribedChunk = (int.MinValue, int.MinValue);
    int subscribedFar = -1;
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
        cameraRig.GroundAt = terrain.HeightAt;
        fires = new FireView(terrain.HeightAt);
        view.AddChild(fires);
        view.AddChild(items);
        clay = new CeramicsView(view, terrain.HeightAt, me, hud.Note);
        AddChild(clay);
        AddChild(hud);
        AddChild(social);
        AddChild(journal);
        view.AddChild(journal.Notes);
        AddChild(crafting);
        // Setting crafted things down (ui-16): from the bag, a hotbar key or the after-craft prompt.
        placing.Terrain = terrain;
        placing.Me = me;
        placing.CameraRig = cameraRig;
        placing.View = view;
        placing.Note = hud.Note;
        placing.OverBar = at => hud.Hotbar.RowRect.HasPoint(at);
        HotbarBar.Placeable = item => Items.IsKnown((ushort)item) && Items.Def(item).Placeable;
        hud.Hotbar.PlaceRequested += placing.StartItem;
        AddChild(placing);
        // The maker's mark (ui-23): chosen from Options, shown on made things' tooltips.
        BagGrid.Makers = markPicker.Link;
        AddChild(markPicker);
        AddChild(woodcutting);
        crafting.Notice += hud.Note;
        building = new BuildingSystem(terrain, hud, me, cameraRig, view, () => conn);
        atmosphere.Sheltered = p => building.ShelterAt(p) != null;
        atmosphere.SeeThrough = building.SeeThroughOf;
        atmosphere.OtherFigures = () => others.Values;
        AddChild(building);
        // The picture build menu (ui-20): B opens it; a tile starts House building's ghost.
        // Added after building, so it sees B first; while the ghost is up, B is building's.
        buildMenu.Place = building.StartPlacing;
        buildMenu.NeedsClaim = building.NeedsClaimHere;
        buildMenu.Blocked = () => building.Placing || placing.Active || crafting.CapturesInput || Panels.Open() || survival.Mourning || social.Typing;
        AddChild(buildMenu);
        survival.Setup(view, terrain, me, hud);
        survival.AvatarOf = id => others.GetValueOrDefault(id);
        AddChild(survival);
        // Last child, so G reaches the Use key before any lane's old binding.
        useKey.Player = () => me.Position;
        useKey.Facing = () => new Vector3(Mathf.Sin(me.Rotation.Y), 0, Mathf.Cos(me.Rotation.Y));
        useKey.Blocked = () => crafting.CapturesInput || admin.IsOpen || building.Placing || placing.Active || Panels.Open() || Panels.Side() || survival.Mourning;
        // The hotbar's number keys (ui-19) rest while something else has the keys.
        hud.Hotbar.Blocked = () => crafting.CapturesInput || admin.IsOpen || building.Placing || placing.Active || Panels.Open() || Panels.Side() || survival.Mourning || social.Typing;
        hud.Hotbar.Note = hud.Note;
        // The bag in the I panel (ui-18): drags go to the hotbar, the worn slots or the ground.
        hud.Hotbar.ShowOverPanels = () => equipment.IsOpen;
        equipment.Bag.Use = hud.Hotbar.UseItem;
        equipment.Bag.TakeOff = slot => equipment.Link.TakeOff(slot);
        equipment.Bag.ClearHotbar = i => hud.Hotbar.Link.Set(i, null);
        equipment.Bag.OverPanel = at => equipment.Covers(at) || hud.Hotbar.RowRect.HasPoint(at);
        equipment.Bag.PlaceOrDrop = (item, quality) =>
        {
            if (HotbarBar.Placeable(item)) { equipment.Close(); hud.Hotbar.RequestPlace(item); }
            else equipment.Bag.Link.Drop(item, quality);
        };
        social.HideChip = () => survival.Mourning;
        // What you wear (ui-10, D-074).
        equipment.Note = hud.Note;
        AddChild(equipment);
        building.MayBar = social.HasRightsAt;
        building.ClaimStandingAt = social.ClaimStandingAt;
        building.ClaimBorderNear = social.ClaimBorderNear;
        // Trading face to face (ui-9, D-069).
        trade.Note = hud.Note;
        AddChild(trade);
        AddChild(admin);
        admin.Note = hud.Note;
        AdminCommands.Menu = admin;
        AdminCommands.IsFlying = () => flying;
        AdminCommands.SetFlying = SetFlying;
        // The panel at a crafting station (ui-13, D-098).
        station.Conn = () => conn;
        station.Crafting = crafting;
        station.Me = () => me.Position;
        AddChild(station);
        // Choosing how you look (ui-21): opens by itself at first join, and from Options.
        lookPicker.Conn = () => conn;
        lookPicker.MyId = () => myId;
        AddChild(lookPicker);
        JournalPanel? journalPanel = null;
        Panels.Open = () => PauseMenu.IsOpen
            || bugReport?.IsOpen == true
            || (journalPanel ??= journal.GetChildren().OfType<JournalPanel>().FirstOrDefault())?.IsOpen == true
            || social.PanelOpen
            || trade.IsOpen
            || equipment.IsOpen
            || station.IsOpen
            || buildMenu.IsOpen
            || markPicker.IsOpen
            || lookPicker.IsOpen;
        Panels.Side = () => crafting.PanelOpen;
        herd.Blocked = () => crafting.CapturesInput || admin.IsOpen || building.Placing || placing.Active || Panels.Open() || survival.Mourning;
        herd.Facing = () => new Vector3(Mathf.Sin(me.Rotation.Y), 0, Mathf.Cos(me.Rotation.Y));
        AddChild(useKey);
        AddChild(new Hints());
        // The crit cue over an animal (HitPop.Show, called by Animals).
        AddChild(new HitPop());
        // First-evening tips for a brand-new player (ui-14, M2).
        AddChild(new FirstEvening { Conn = () => conn, MyId = () => myId, TimeOfDay = () => atmosphere.TimeOfDay });
        var keyHelp = new KeyHelp();
        AddChild(keyHelp);
        SetUpBugReport();
        // Esc belongs to whatever panel, chat or placement is open; otherwise it opens the menu.
        var menu = new PauseMenu { Keys = keyHelp, Sound = () => atmosphere.Sound, OthersOpen = () => bugReport?.IsOpen == true || crafting.CapturesInput || admin.IsOpen || building.Placing || social.Typing || social.PanelOpen || GetViewport().GuiGetFocusOwner() != null };
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
        // --screenshot-fire places a local campfire just in front of the player; =cold a burned-out one.
        if (Arg("screenshot-fire") is { } fireArg)
            GetTree().CreateTimer(Math.Max(1, delay - 4)).Timeout += () =>
            {
                var at = me.Position + new Vector3(Mathf.Sin(cameraRig.Rotation.Y), 0, Mathf.Cos(cameraRig.Rotation.Y)) * -1.6f + new Vector3(1.2f, 0, 0);
                if (fireArg == "cold") fires.PreviewCold(at); else fires.Preview(at);
            };
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
            Mesh = new PlaneMesh { Size = new Vector2(500, 500) },
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
        Bind("dig", Key.F);
        Bind("pick_up", Key.E);
        Bind("craft", Key.C);
        Bind("sneak", Key.Ctrl);
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
        // A maker's mark carries 16 bits of the maker's id; this finds their name (ceramics-15).
        ItemLook.MakerName = id => conn.Db.Player.Iter().FirstOrDefault(p => (p.PlayerId & 0xFFFFu) == id)?.Name;
        ClayLook.MarkLookup = id =>
        {
            var m = conn.Db.PlayerMark.Iter().FirstOrDefault(r => (r.PlayerId & 0xFFFFu) == id);
            int slot = m == null ? -1 : MakerStamp.SlotOf(m.Glyph);
            if (m == null || slot < 0) return null;
            return (slot, new Color(((m.Color >> 16) & 0xFF) / 255f, ((m.Color >> 8) & 0xFF) / 255f, (m.Color & 0xFF) / 255f));
        };

        conn.Db.Player.OnInsert += (_, p) => OnPlayer(p);
        conn.Db.Player.OnUpdate += (_, _, p) => OnPlayer(p);
        conn.Db.PlayerLook.OnInsert += (_, l) => OnPlayerLook(l);
        conn.Db.PlayerLook.OnUpdate += (_, _, l) => OnPlayerLook(l);
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
        jumpView.Attach(conn, id => others.GetValueOrDefault(id), () => myId);
        conn.OnUnhandledReducerError += (_, e) => { BugLog.Error(e.Message); Use.Failed(); ServerCheck.Failed(e.Message, hud.Note, ""); };
        Use.Busy = () => crafting.Working;
        social.Attach(conn, view, terrain, hud, id => id == myId ? me : others.GetValueOrDefault(id));
        trade.Attach(new ServerTradeLink(conn, () => myId));
        admin.Attach(conn);
        equipment.Attach(new ServerEquipLink(conn));
        hud.Hotbar.Attach(new ServerHotbarLink(conn, () => myId));
        placing.Attach(new ServerPlaceLink(conn, crafting, terrain.HeightAt));
        equipment.Bag.Link = new ServerBagLink(conn);
        markPicker.Attach(new ServerMarkLink(conn, () => myId));
        BagGrid.Makers = markPicker.Link;
        buildMenu.Carried = item => conn.Db.MyInventory.Iter().Where(i => i.Item == (ushort)item).Aggregate(0u, (n, i) => n + i.Count);
        crafting.Attach(conn);
        AvatarWork.Attach(conn, me, id => others.GetValueOrDefault(id));
        fires.Attach(conn);
        woodcutting.Attach(conn, terrain, crafting);
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
        // Resubscribes when you cross a chunk or the far radius (view distance setting) changes.
        if (conn == null || (subscribedChunk == (chunkX, chunkZ) && subscribedFar == Subscriptions.FarRadius)) return;
        subscribedChunk = (chunkX, chunkZ);
        subscribedFar = Subscriptions.FarRadius;
        string range = Subscriptions.Range(chunkX, chunkZ, World.ViewRadiusChunks);
        string farRange = Subscriptions.Range(chunkX, chunkZ, subscribedFar);
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
            .Subscribe(Subscriptions.Area(range, farRange).Concat(SurvivalClient.Queries(range)).Concat(BuildingSystem.Queries()).ToArray());
    }

    void OnPlayer(Player p)
    {
        names[p.PlayerId] = p.Name;
        if (p.Identity == myIdentity)
        {
            myId = p.PlayerId;
            if (conn?.Db.PlayerLook.PlayerId.Find(p.PlayerId) is { } chosen)
                me.SetLook(HumanFigure.Appearance.From(chosen.Skin, chosen.Hair, chosen.Cut, chosen.Beard));
            else me.SetLook(p.PlayerId);
        }
        if (others.TryGetValue(p.PlayerId, out var avatar))
        {
            avatar.SetPlayerName(p.Name);
            avatar.Visible = p.Online;
        }
    }

    // A player's chosen look (model-3): applied to their avatar over the id default.
    void OnPlayerLook(PlayerLook l)
    {
        var look = HumanFigure.Appearance.From(l.Skin, l.Hair, l.Cut, l.Beard);
        if (myId != 0 && l.PlayerId == myId || (myId == 0 && IsMine(l.PlayerId))) me.SetLook(look);
        else if (others.TryGetValue(l.PlayerId, out var avatar)) avatar.SetLook(look);
    }

    void OnPlayerPos(PlayerPos p, bool inserted)
    {
        var at = new Vector3(p.X, Wading.FeetY(terrain.HeightAt(p.X, p.Z)), p.Z);
        if (myId != 0 && p.PlayerId == myId || (myId == 0 && IsMine(p.PlayerId)))
        {
            // Before spawning we wait for the area around us (see Subscribe).
            if (!spawned) return;
            // The server clamps impossible moves; snap back if we drifted too far.
            if (!flying && new Vector2(me.Position.X - p.X, me.Position.Z - p.Z).Length() > 3f)
                me.Position = at;
            return;
        }

        if (!others.TryGetValue(p.PlayerId, out var avatar))
        {
            avatar = new Avatar(names.GetValueOrDefault(p.PlayerId, "Wanderer")) { Remote = true, Position = at };
            if (conn?.Db.PlayerLook.PlayerId.Find(p.PlayerId) is { } chosen)
                avatar.SetLook(HumanFigure.Appearance.From(chosen.Skin, chosen.Hair, chosen.Cut, chosen.Beard));
            else avatar.SetLook(p.PlayerId);
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
            .GroupBy(i => (i.Item, Grade: Items.IsKnown(i.Item) && Items.Def((Item)i.Item).HasQuality ? Processes.Grade(i.Quality) : "", Worn: ItemLook.Condition(i.Item, i.Quality, i.Wear)))
            .Select(g => { uint n = (uint)g.Sum(i => i.Count); return new InventoryEntry((Item)g.Key.Item, ItemLook.Label((Item)g.Key.Item, g.First().Quality, n, g.Key.Worn), n); })
            .ToList();
        hud.SetItems(stacks);
    }

    // --- Items on the ground ---------------------------------------------

    void AddItem(WorldItem item)
    {
        if (items.Has(item.Id)) return;
        float yaw = item.Placed ? item.Yaw : Terrain.Hash01((int)item.Id, 3, 21) * Mathf.Tau;
        var at = new Vector3(item.X, terrain.HeightAt(item.X, item.Z) + 0.04f, item.Z);
        items.Add(item.Id, (Item)item.Item, new Transform3D(new Basis(Vector3.Up, yaw), at), item.Wear);
    }

    void RemoveItem(ulong id) => items.Remove(id);

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
        if (e is InputEventMouseButton { Pressed: true } mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp) cameraRig.Zoom(-2f);
            if (mb.ButtonIndex == MouseButton.WheelDown) cameraRig.Zoom(2f);
        }
        if (e.IsActionPressed("dig") && !e.IsEcho()) { digHold.Pressed(me.Position); Dig(); }
        if (e.IsActionPressed("pick_up") && !e.IsEcho()) { pickHold.Pressed(me.Position); PickUp(); }
        if (e.IsActionPressed("craft")) crafting.Toggle();
    }

    // Hold the dig key to keep digging, one cell at a time as the ground allows (HoldRepeat).
    readonly HoldRepeat digHold = new();
    // The same for picking things up off the ground with the Use key when nothing else is on offer.
    readonly HoldRepeat pickHold = new();

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        conn?.FrameTick();
        if (spawned && !PauseMenu.IsOpen && !crafting.CapturesInput && !admin.IsOpen
            && digHold.Update(Input.IsActionPressed("dig"), dt, World.DigCooldownMicros / 1_000_000f + 0.1f, false, me.Position))
        {
            digHold.Acted(me.Position);
            Dig();
        }
        if (spawned && !PauseMenu.IsOpen && !crafting.CapturesInput && !admin.IsOpen
            && pickHold.Update(Input.IsActionPressed("pick_up") && Use.Shown == null, dt, 0.5f, false, me.Position)
            && conn != null && conn.Db.WorldItem.Iter().Any(i => new Vector2(i.X - me.Position.X, i.Z - me.Position.Z).Length() <= World.Reach))
        {
            pickHold.Acted(me.Position);
            PickUp();
        }

        if (spawned && !PauseMenu.IsOpen && !crafting.CapturesInput && !admin.IsOpen) Walk(dt);
        jumpView.Process(dt, atmosphere.GroundAt);
        social.Track(me.Position, myId);
        terrain.Focus(World.ChunkOf(me.Position.X), World.ChunkOf(me.Position.Z));
        water.Position = new Vector3(Mathf.Round(me.Position.X), Look.WaterLevel, Mathf.Round(me.Position.Z));
        cameraRig.Focus = me.Position + new Vector3(0, 0.9f, 0);

        if (conn != null && spawned)
        {
            Subscribe(World.ChunkOf(me.Position.X), World.ChunkOf(me.Position.Z));
            heartbeatTimer -= dt;
            if (heartbeatTimer <= 0f) { heartbeatTimer = 20f; conn.Reducers.Heartbeat(); }
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

    float heartbeatTimer; // tells the server we're still here (see Presence.cs)

    void Walk(float dt)
    {
        var input = social.Typing ? Vector2.Zero : Input.GetVector(NWalkLeft, NWalkRight, NWalkBack, NWalkForward);
        if (flying) { Fly(dt, input); return; }
        bool sneak = Input.IsActionPressed(NSneak);
        float wading = Wading.SpeedFactor(Wading.Depth(terrain.HeightAt(me.Position.X, me.Position.Z)), SwimmingRank());
        var wish = (cameraRig.Right * input.X + cameraRig.Forward * input.Y) * survival.MoveSpeed(sneak, input != Vector2.Zero, WalkSpeed, dt) * wading;
        // Ease in and out of walking rather than starting and stopping instantly.
        velocity = velocity.Lerp(wish, 1f - Mathf.Exp(-(wish == Vector3.Zero ? 10f : 7f) * dt));

        // Jump (bugfix-25): a hop keeps its takeoff direction, and the server tells the others.
        if (!social.Typing && Input.IsActionJustPressed(NJump) && jump.CanStart(sneak) && !Wading.Floating(Wading.Depth(terrain.HeightAt(me.Position.X, me.Position.Z))) && survival.TrySpendStamina(Stamina.JumpCost))
        {
            jump.Start(velocity);
            conn?.Reducers.Jump();
            Sfx.Play(Cue.Jump);
        }
        bool landed = jump.Tick(dt);
        if (jump.Airborne) velocity = jump.Launch;

        var pos = building.Collide(me.Position, me.Position + velocity * dt, jump.AirHeight);
        pos = terrain.CollideVegetation(pos, jump.AirHeight);
        // Water is walkable (bugfix-32): feet follow the bed, or float over deep water.
        float next = terrain.HeightAt(pos.X, pos.Z);
        float feet = Wading.FeetY(next);
        // Ease up and down slopes instead of popping between heights; in the air the feet follow the arc.
        pos.Y = jump.Airborne ? feet + jump.AirHeight : Mathf.Lerp(me.Position.Y, feet, 1f - Mathf.Exp(-14f * dt));
        me.Position = pos;
        me.SetJump(jump.Airborne, jump.Rising, jump.Landing);
        if (landed) Sfx.Land(jump.Landing, atmosphere.GroundAt(pos));

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
                return new UseOffer($"Feed the fire ({FireView.TimeLeft(fire)})", d,
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

    // One identity per install, so you keep your character between sessions. It lives in the
    // Godot user folder (outside the launcher's game folder), so reinstalling the game keeps it.
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
        // A different token from the one on disk (server wiped, key changed) keeps the old one as
        // a .bak, so a character is never lost for good by an overwrite.
        if (LoadToken() is { } old && old != token)
        {
            using var bak = FileAccess.Open(TokenPath + ".bak", FileAccess.ModeFlags.Write);
            bak.StoreString(old);
        }
        using var file = FileAccess.Open(TokenPath, FileAccess.ModeFlags.Write);
        file.StoreString(token);
    }
}
