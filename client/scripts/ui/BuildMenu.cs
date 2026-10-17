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

    const int Thumb = 112;
    public bool IsOpen => panel.Visible;

    Control panel = null!;
    HBoxContainer tabs = null!;
    GridContainer grid = null!;
    string category = "";
    readonly Dictionary<int, Texture2D> pictures = new();
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
        if (OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--screenshot-buildmenu")))
            GetTree().CreateTimer(1.5).Timeout += Open;
    }

    public void Open()
    {
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
        tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 6);
        box.AddChild(tabs);
        grid = new GridContainer { Columns = 4 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        box.AddChild(grid);
        var hint = Hud.Text("Click one to lay it out    B or Esc closes", 13);
        hint.Modulate = new Color(1, 1, 1, 0.65f);
        box.AddChild(hint);

        var outer = Hud.Panel(box, Vector2.Zero);
        var style = (StyleBoxFlat)outer.GetThemeStylebox("panel");
        style.BgColor = new Color(0.10f, 0.11f, 0.09f, 0.82f);
        style.ContentMarginTop = style.ContentMarginBottom = 14;
        style.ContentMarginLeft = style.ContentMarginRight = 18;
        outer.SetAnchorsPreset(Control.LayoutPreset.Center);
        outer.GrowHorizontal = Control.GrowDirection.Both;
        outer.GrowVertical = Control.GrowDirection.Both;
        return outer;
    }

    void Refresh()
    {
        foreach (var child in tabs.GetChildren()) { tabs.RemoveChild(child); child.QueueFree(); }
        foreach (var c in Order.Where(c => Kinds(c).Any()))
        {
            var tab = new Button { Text = c, Flat = c != category, FocusMode = Control.FocusModeEnum.None, ToggleMode = false };
            tab.AddThemeFontSizeOverride("font_size", 15);
            tab.Modulate = new Color(1, 1, 1, c == category ? 1f : 0.65f);
            string chosen = c;
            tab.Pressed += () => { category = chosen; Refresh(); };
            tabs.AddChild(tab);
        }
        foreach (var child in grid.GetChildren()) { grid.RemoveChild(child); child.QueueFree(); }
        foreach (int kind in Kinds(category)) grid.AddChild(Tile(kind));
    }

    Control Tile(int kind)
    {
        var costs = Costs(kind);
        bool afford = costs.All(c => Carried(c.item) >= c.count);
        bool claimless = NeedsClaim(kind);
        var tile = new Button { FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(Thumb + 24, Thumb + 76 + (claimless ? 18 : 0)) };
        var style = new StyleBoxFlat { BgColor = new Color(0.09f, 0.10f, 0.07f, 0.6f), BorderColor = new Color(Look.Cream, 0.22f) };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(10);
        var hover = (StyleBoxFlat)style.Duplicate();
        hover.BorderColor = new Color(Look.Cream, 0.6f);
        tile.AddThemeStyleboxOverride("normal", style);
        tile.AddThemeStyleboxOverride("hover", hover);
        tile.AddThemeStyleboxOverride("pressed", hover);
        tile.Pressed += () => { Close(); Place(kind); };
        tile.Modulate = new Color(1, 1, 1, afford && !claimless ? 1f : 0.45f);
        tile.TooltipText = claimless ? "Needs a claim: stand inside your claim to build this"
            : afford ? "" : "You don't carry enough yet";

        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        column.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        column.OffsetLeft = column.OffsetTop = 8;
        column.OffsetRight = column.OffsetBottom = -8;
        column.AddThemeConstantOverride("separation", 4);
        tile.AddChild(column);
        var picture = new TextureRect
        {
            Texture = pictures.GetValueOrDefault(kind),
            CustomMinimumSize = new Vector2(Thumb, Thumb),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        column.AddChild(picture);
        var name = Hud.Text(Structures.Shape(kind).Name, 14);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.MouseFilter = Control.MouseFilterEnum.Ignore;
        column.AddChild(name);
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 6);
        foreach (var (item, count) in costs)
        {
            var icon = new TextureRect
            {
                Texture = ItemIcon.For(item),
                CustomMinimumSize = new Vector2(20, 20),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                TooltipText = Items.All.TryGetValue(item, out var def) ? def.Count(count) : "",
            };
            row.AddChild(icon);
            var n = Hud.Text(count.ToString(), 12);
            // Amber where you carry too little of this one.
            if (Carried(item) < count) n.Modulate = new Color(1f, 0.75f, 0.45f);
            n.MouseFilter = Control.MouseFilterEnum.Ignore;
            row.AddChild(n);
        }
        column.AddChild(row);
        if (claimless)
        {
            var note = Hud.Text("Needs a claim", 12);
            note.HorizontalAlignment = HorizontalAlignment.Center;
            note.Modulate = new Color(1f, 0.75f, 0.45f);
            note.MouseFilter = Control.MouseFilterEnum.Ignore;
            column.AddChild(note);
        }
        return tile;
    }

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
        view.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-50, -35, 0), LightEnergy = 1.1f });
        view.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.75f, 0.78f, 0.85f),
                AmbientLightEnergy = 0.6f,
                BackgroundMode = Godot.Environment.BGMode.ClearColor,
            },
        });

        foreach (int kind in Structures.Kinds)
        {
            var model = new StructureView(kind);
            view.AddChild(model);
            model.Show(StructureModel.StageCount(kind), 1f, complete: true, animate: false);
            // Framed from the front left and a little above, sized to the footprint and height.
            var shape = Structures.Shape(kind);
            float size = Mathf.Max(shape.Round ? shape.Radius : Mathf.Max(shape.HalfWidth, shape.HalfDepth), 0.5f);
            if (shape.Fence || shape.Gate) size = Mathf.Max(size, 1.4f);
            float distance = size * 4.2f + 1.5f;
            var target = new Vector3(0, size * 0.55f, 0);
            camera.Position = target + new Vector3(-0.55f, 0.45f, 0.8f).Normalized() * distance;
            camera.LookAt(target, Vector3.Up);

            view.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            pictures[kind] = ImageTexture.CreateFromImage(view.GetTexture().GetImage());
            view.RemoveChild(model);
            model.QueueFree();
        }
        view.QueueFree();
        rendering = false;
        if (IsOpen) Refresh();
    }
}
