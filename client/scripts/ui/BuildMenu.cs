using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The build menu with pictures (ui-20; Justin: pictures instead of tabbing through every
// placeable). Category tabs along the top and a grid of tiles: each a picture of the
// finished structure, its name and what it takes, greyed when you carry too little.
// Clicking a tile starts House building's ghost. The pictures are rendered from the real
// structure models (StructureView) in an offscreen viewport when the menu first opens,
// so a new kind gets its picture without anyone drawing one (D-206).
// Justin picked this layout (option 1). B opens it (BuildingSystem keeps B while its ghost
// is up, so B there still cancels); Esc or B closes it.
public partial class BuildMenu : CanvasLayer
{
    // What you carry of an item (Main sets it); the mock counts sticks and fiber.
    public Func<Item, uint> Carried = item => item is Item.Stick or Item.Fiber ? 12u : 0u;
    // Starts placing a kind (House building's ghost).
    public Action<int> Place = _ => { };
    // True when a kind can't be laid out where the player stands because it needs a claim
    // (Main sets it from House building's NeedsClaimHere).
    public Func<int, bool> NeedsClaim = _ => false;

    const int Thumb = 112, Columns = 4, Gap = 12;
    public bool IsOpen => panel.Visible;

    Control panel = null!;
    HFlowContainer tabs = null!;
    GridContainer grid = null!;
    string category = "";
    readonly Dictionary<int, Texture2D> pictures = new();
    // The open tab's picture frames, filled in as each picture finishes (ui-49).
    readonly Dictionary<int, PictureFrame> frames = new();
    bool rendering;

    // Each shape names its own tab (House building's StructureShape.Category).
    public static string CategoryOf(int kind) => Structures.Shape(kind).Category;

    // Tabs in this order; any category not listed comes after, so a new one still shows.
    static readonly string[] Preferred = { "Shelter", "Storage", "Crafting", "Fire", "Fences and gates", "Claims" };
    static IEnumerable<string> Order => Preferred
        .Concat(Structures.Kinds.Select(CategoryOf).Distinct().Where(c => !Preferred.Contains(c)));

    // Everything it takes, all stages together.
    public static List<(Item item, uint count)> Costs(int kind) =>
        Structures.Exists(kind)
            ? Structures.Costs(kind).SelectMany(stage => stage)
                .GroupBy(c => c.Item).Select(g => (g.Key, (uint)g.Sum(c => c.Count))).ToList()
            : new();

    public override void _Ready()
    {
        Layer = 2;
        panel = MakePanel();
        panel.Visible = false;
        AddChild(panel);
        Panels.Centre(this, Close);
        // Pictures are made a few seconds after the world loads, so the menu rarely opens
        // before they're ready (ui-49: blank boxes on first open).
        // Not headless (bots and checks): there's nothing to draw with.
        if (DisplayServer.GetName() != "headless")
            GetTree().CreateTimer(4).Timeout += () => { if (!rendering && pictures.Count == 0) RenderPictures(); };
        // --screenshot-buildmenu opens the first tab; =Walls (any tab's name) opens that one.
        foreach (var arg in OS.GetCmdlineUserArgs().Where(a => a.StartsWith("--screenshot-buildmenu")))
        {
            string wanted = arg.Split('=').ElementAtOrDefault(1) ?? "";
            category = Order.FirstOrDefault(c => c.Equals(wanted, System.StringComparison.OrdinalIgnoreCase)) ?? "";
            GetTree().CreateTimer(1.5).Timeout += Open;
        }
    }

    public void Open()
    {
        Panels.Opening(this);
        panel.Visible = true;
        if (category == "") category = Order.First(c => Kinds(c).Any());
        Refresh();
        if (!rendering && pictures.Count == 0) RenderPictures();
    }

    public void Close() => panel.Visible = false;

    // While House building's ghost is up (or something else has the keys) B is theirs.
    public Func<bool> Blocked = () => false;

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false }) return;
        if (IsOpen && (e.IsActionPressed("ui_cancel") || e.IsActionPressed("build_mode")))
            Close();
        else if (!IsOpen && InputMap.HasAction("build_mode") && e.IsActionPressed("build_mode") && !Blocked())
            Open();
        else return;
        GetViewport().SetInputAsHandled();
    }

    static IEnumerable<int> Kinds(string category) =>
        Structures.Kinds.Where(k => CategoryOf(k) == category);

    Control MakePanel()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        box.AddChild(Hud.Text("Build", 20));
        // Four card widths across, always (ui-33): tabs wrap onto a second row rather than
        // widening the panel, and a tall tab scrolls instead of running off the screen.
        float across = Columns * (Thumb + 24) + (Columns - 1) * Gap;
        tabs = new HFlowContainer { CustomMinimumSize = new Vector2(across, 0) };
        tabs.AddThemeConstantOverride("h_separation", 3);
        tabs.AddThemeConstantOverride("v_separation", 4);
        box.AddChild(tabs);
        grid = new GridContainer { Columns = Columns };
        grid.AddThemeConstantOverride("h_separation", Gap);
        grid.AddThemeConstantOverride("v_separation", Gap);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, CustomMinimumSize = new Vector2(across, 0) };
        scroll.AddChild(grid);
        grid.MinimumSizeChanged += () =>
            scroll.CustomMinimumSize = new Vector2(across, Mathf.Min(grid.GetCombinedMinimumSize().Y, GetViewport().GetVisibleRect().Size.Y * 0.6f));
        box.AddChild(scroll);
        var hint = Hud.Text("Click one to lay it out    B or Esc closes", 13);
        hint.Modulate = new Color(1, 1, 1, 0.65f);
        box.AddChild(hint);

        var outer = Ui.Window(box);
        return outer;
    }

    void Refresh()
    {
        foreach (var child in tabs.GetChildren()) { tabs.RemoveChild(child); child.QueueFree(); }
        foreach (var c in Order.Where(c => Kinds(c).Any()))
        {
            var tab = new Button { Text = c, Flat = c != category, FocusMode = Control.FocusModeEnum.None, ToggleMode = false };
            tab.AddThemeFontSizeOverride("font_size", 14);
            tab.Modulate = new Color(1, 1, 1, c == category ? 1f : 0.65f);
            string chosen = c;
            tab.Pressed += () => { category = chosen; Refresh(); };
            tabs.AddChild(tab);
        }
        foreach (var child in grid.GetChildren()) { grid.RemoveChild(child); child.QueueFree(); }
        frames.Clear();
        foreach (int kind in Kinds(category)) grid.AddChild(Tile(kind));
    }

    Control Tile(int kind)
    {
        var costs = Costs(kind);
        bool afford = costs.All(c => Carried(c.item) >= c.count);
        bool claimless = NeedsClaim(kind);
        // A Card keeps its width and grows taller to fit; the costs wrap inside it (ui-33).
        var tile = new Card(Thumb + 24, Thumb + 76);
        tile.Pressed += () => { Close(); Place(kind); };
        tile.Modulate = new Color(1, 1, 1, afford && !claimless ? 1f : WarmLook.On ? 0.7f : 0.45f);
        tile.TooltipText = claimless ? "Needs a claim: stand inside your claim to build this"
            : afford ? "" : Shortfall(costs);

        var picture = new PictureFrame(Thumb) { SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        picture.SetPicture(pictures.GetValueOrDefault(kind));
        frames[kind] = picture;
        tile.Column.AddChild(picture);
        tile.Column.AddChild(tile.Line(Structures.Shape(kind).Name, 14));
        var row = tile.Wrap();
        foreach (var (item, count) in costs)
        {
            // Icon and number kept together, so a wrap never splits them.
            var pair = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            pair.AddThemeConstantOverride("separation", 2);
            pair.AddChild(new TextureRect
            {
                Texture = ItemIcon.For(item),
                CustomMinimumSize = new Vector2(20, 20),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
            var n = Hud.Text(count.ToString(), 12);
            // Amber where you carry too little of this one.
            if (Carried(item) < count) n.Modulate = new Color(1f, 0.75f, 0.45f);
            n.MouseFilter = Control.MouseFilterEnum.Ignore;
            pair.AddChild(n);
            row.AddChild(pair);
        }
        tile.Column.AddChild(row);
        if (claimless)
        {
            var note = tile.Line("Needs a claim", 12);
            note.Modulate = new Color(1f, 0.75f, 0.45f);
            tile.Column.AddChild(note);
        }
        return tile;
    }

    // What you still need for a kind, one line per short item with where to get it (the
    // crafting cards' wording). You can lay out a footprint first and bring the rest later.
    string Shortfall(List<(Item item, uint count)> costs) =>
        "You can lay it out now and bring the rest as you build. Still to find:\n" + string.Join("\n", costs
            .Where(c => Carried(c.item) < c.count)
            .Select(c => $"{c.count - Carried(c.item)} {Items.Def(c.item).Count(c.count - Carried(c.item)).ToLowerInvariant()}. {CraftingUi.WhereFrom(c.item)}"));

    // Renders each kind, finished, in a small lit scene of its own, one per frame.
    async void RenderPictures()
    {
        rendering = true;
        var view = new SubViewport
        {
            Size = new Vector2I(Thumb * 2, Thumb * 2),
            TransparentBg = true,
            OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
            Msaa3D = Viewport.Msaa.Msaa4X,
        };
        AddChild(view);
        var camera = new Camera3D { Fov = 30f };
        view.AddChild(camera);
        // Bright enough that thin sticks (fences, gates, racks) still read on the card.
        view.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-50, -35, 0), LightEnergy = 1.35f });
        view.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.75f, 0.78f, 0.85f),
                AmbientLightEnergy = 0.8f,
                BackgroundMode = Godot.Environment.BGMode.ClearColor,
            },
        });

        // The tab you'll see first is made first (ui-59), so its cards fill in right away.
        string first = category != "" ? category : Order.FirstOrDefault(c => Kinds(c).Any()) ?? "";
        foreach (int kind in Structures.Kinds.OrderBy(k => CategoryOf(k) == first ? 0 : 1))
        {
            var model = new StructureView(kind);
            view.AddChild(model);
            model.Show(StructureModel.StageCount(kind), 1f, complete: true, animate: false);
            // Framed from the front left and a little above, sized to the footprint and height.
            var shape = Structures.Shape(kind);
            float size = Mathf.Max(shape.Round ? shape.Radius : Mathf.Max(shape.HalfWidth, shape.HalfDepth), 0.5f);
            // Fences and gates are long and thin: frame them closer, so the rails aren't hairlines.
            if (shape.Fence || shape.Gate) size = Mathf.Max(size, 1.0f);
            float distance = size * 4.2f + 1.5f;
            var target = new Vector3(0, size * 0.55f, 0);
            camera.Position = target + new Vector3(-0.55f, 0.45f, 0.8f).Normalized() * distance;
            camera.LookAt(target, Vector3.Up);

            view.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            pictures[kind] = ImageTexture.CreateFromImage(view.GetTexture().GetImage());
            if (frames.TryGetValue(kind, out var frame) && IsInstanceValid(frame)) frame.SetPicture(pictures[kind]);
            view.RemoveChild(model);
            model.QueueFree();
        }
        view.QueueFree();
        rendering = false;
    }
}

// Where a build card's picture sits (ui-49): a soft ground patch under the model, so thin
// dark sticks still stand out, and while the picture is still being made, a faint pencil
// house that breathes, instead of an empty box.
public partial class PictureFrame : Control
{
    readonly TextureRect picture;
    float time;

    public PictureFrame(int size)
    {
        CustomMinimumSize = new Vector2(size, size);
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
        picture = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        picture.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(picture);
    }

    public void SetPicture(Texture2D? texture)
    {
        picture.Texture = texture;
        SetProcess(texture == null);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var c = Size / 2;
        // A light oval of ground under where the model stands.
        var light = WarmLook.On ? WarmLook.Paper.Lightened(0.35f) : new Color(Look.Cream, 0.16f);
        DrawSetTransform(c + new Vector2(0, Size.Y * 0.22f), 0f, new Vector2(1f, 0.38f));
        DrawCircle(Vector2.Zero, Size.X * 0.42f, new Color(light, WarmLook.On ? 0.75f : 1f));
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        if (picture.Texture != null) return;
        // A little pencil house, breathing while the picture is made.
        var ink = WarmLook.On ? WarmLook.Ink : Look.Cream;
        ink = new Color(ink, 0.25f + 0.12f * Mathf.Sin(time * 3f));
        float s = Size.X / 112f;
        var o = c + new Vector2(0, 6) * s;
        var house = new[]
        {
            o + new Vector2(-22, 20) * s, o + new Vector2(-22, -6) * s, o + new Vector2(0, -26) * s,
            o + new Vector2(22, -6) * s, o + new Vector2(22, 20) * s, o + new Vector2(-22, 20) * s,
        };
        DrawPolyline(house, ink, 2.5f * s, true);
        DrawPolyline(new[] { o + new Vector2(-6, 20) * s, o + new Vector2(-6, 6) * s, o + new Vector2(6, 6) * s, o + new Vector2(6, 20) * s }, ink, 2.5f * s, true);
    }
}
