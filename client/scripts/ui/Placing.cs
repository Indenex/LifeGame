using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// A crafted thing in the bag that can be set down in the world (crafting-21). Wear is
// the bag row's durability, passed back to the server as is (0 for everything
// placeable today).
public record Placeable(Item Item, byte Quality, uint Wear = 0);

// What placement talks to. Crafting owns the server side (crafting-21: the PlaceItem
// reducer, ItemDef.Placeable, CraftingUi.Finished, shared Placement.Check); until it
// lands, MockPlaceLink stands in.
public interface IPlaceLink
{
    // Placeable things in the bag, one entry per item and quality.
    IEnumerable<Placeable> InBag();
    // Why it can't go here, or null when it can. The server has the last word.
    string? Fits(Placeable thing, float x, float z, float yaw);
    void Place(Placeable thing, float x, float z, float yaw);
    // A placeable craft just finished (for the after-craft prompt).
    event Action<Placeable>? Crafted;
}

// Pretend bag for previews: a coil pot and some bricks. Empty unless filled, so a
// normal game never offers pretend things before Crafting's link is wired.
public sealed class MockPlaceLink : IPlaceLink
{
    readonly List<(Placeable thing, int count)> bag = new();

    public MockPlaceLink(bool filled = false)
    {
        if (!filled) return;
        bag.Add((new Placeable(Item.CoilPot, 60), 1));
        bag.Add((new Placeable(Item.Brick, 40), 3));
    }

    public event Action<Placeable>? Crafted;

    public IEnumerable<Placeable> InBag() => bag.Where(b => b.count > 0).Select(b => b.thing);
    public string? Fits(Placeable thing, float x, float z, float yaw) => null;

    public void Place(Placeable thing, float x, float z, float yaw)
    {
        int i = bag.FindIndex(b => b.thing == thing);
        if (i >= 0) bag[i] = (thing, bag[i].count - 1);
    }

    // As if a coil pot had just come out of a craft.
    public void PretendCrafted() => Crafted?.Invoke(bag[0].thing);
}

// Setting crafted things down (crafting-20, D-187). Two ways in, both opening the same
// ghost: drag a made thing out of the bag row onto the ground (Justin: "drag and drop
// it out of your inventory, and the ghost placement can lock onto your cursor"), or,
// right after a placeable craft, the Use prompt offers "Place the coil pot" for a few
// seconds. The ghost is the item's own model, see-through and pale green like
// Building's placement ghost, amber where it can't go, and follows the cursor on the
// ground within reach. Release (or click) sets it down; T, Shift+T or the scroll wheel
// turn it; Esc, or dropping it back on the bag, puts it back. Main sets the references.
public partial class Placing : Node
{
    public IPlaceLink Link = new MockPlaceLink();
    public Terrain Terrain = null!;
    public Node3D Me = null!;
    public CameraRig CameraRig = null!;
    public SubViewport View = null!;
    public Action<string> Note = _ => { };

    static readonly StringName NUse = "use", NRotate = "build_rotate", NNext = "build_next";
    const float Reach = 3f, OfferFor = 10f;
    static readonly Color Fits = new(0.85f, 1f, 0.8f, 0.38f), Blocked = new(1f, 0.6f, 0.5f, 0.3f);

    public bool Active => thing != null;

    Placeable? thing;
    MeshInstance3D? ghost;
    Vector3 ghostPos;
    float yaw, yawTarget;
    string? problem;
    Placeable? offer;
    float offerLeft;
    Label hint = null!;
    // True from a drag out of the bag until the mouse is released.
    bool dragging;

    readonly StandardMaterial3D ghostMaterial = new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        NoDepthTest = true,
        RenderPriority = 1,
        AlbedoColor = Fits,
    };

    public override void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--screenshot-place"))) Link = new MockPlaceLink(filled: true);
        Attach(Link);
        Use.Register(new UseHandler { Id = "ui.place", Priority = 90, Find = FindUse });
        InventoryBar.CanDrag = item => Link.InBag().Any(p => p.Item == item);
        InventoryBar.DraggedOut += item =>
        {
            if (Active || Panels.Open() || Link.InBag().FirstOrDefault(p => p.Item == item) is not { } t) return;
            Start(t);
            dragging = true;
        };

        // One quiet line where the G prompt sits, while placing.
        var layer = new CanvasLayer { Layer = 2 };
        AddChild(layer);
        hint = new Label { HorizontalAlignment = HorizontalAlignment.Center, Visible = false };
        Hud.Soft(hint, 16, 0.95f);
        hint.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        hint.GrowHorizontal = Control.GrowDirection.Both;
        hint.GrowVertical = Control.GrowDirection.Begin;
        hint.Position = new Vector2(0, -214);
        layer.AddChild(hint);

        // --screenshot-place starts placing the pretend coil pot; =offer shows the after-craft prompt.
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--screenshot-place") && Link is MockPlaceLink mock)
                GetTree().CreateTimer(1.5).Timeout += () =>
                {
                    if (arg.EndsWith("=offer")) mock.PretendCrafted();
                    else Start(mock.InBag().First());
                };
    }

    // Swaps the mock for the server link once Main has a connection.
    public void Attach(IPlaceLink link)
    {
        Link = link;
        Link.Crafted += t => { offer = t; offerLeft = OfferFor; };
    }

    UseOffer? FindUse(Vector3 at)
    {
        if (Active || offer is not { } t || offerLeft <= 0f || !Link.InBag().Contains(t)) return null;
        // Targeted where you stand, so it wins over things around you for these few seconds.
        return new UseOffer($"Place the {Name(t)}", 0f, () => Start(t), Target: at);
    }

    void Start(Placeable t)
    {
        thing = t;
        offer = null;
        ghost?.QueueFree();
        ghost = new MeshInstance3D { Mesh = ItemLook.MeshFor(t.Item), MaterialOverride = ghostMaterial };
        View.AddChild(ghost);
        yaw = yawTarget = Mathf.Atan2(-CameraRig.Forward.X, -CameraRig.Forward.Z);
        ghostPos = Me.Position + CameraRig.Forward * 1.5f;
        hint.Visible = true;
        UpdateHint();
    }

    void Stop()
    {
        thing = null;
        dragging = false;
        ghost?.QueueFree();
        ghost = null;
        hint.Visible = false;
    }

    void SetDown()
    {
        if (thing is not { } t) return;
        if (problem != null) { Note(problem); return; }
        Link.Place(t, ghostPos.X, ghostPos.Z, yawTarget);
        Stop();
    }

    // The mouse while placing goes here first: a drag out of the bag keeps the mouse on
    // its slot, so the release and the wheel must be caught before the GUI sees them.
    public override void _Input(InputEvent e)
    {
        if (!Active || e is not InputEventMouseButton b) return;
        if (b.ButtonIndex == MouseButton.Left && !b.Pressed && dragging)
        {
            dragging = false;
            if (InventoryBar.RowRect.HasPoint(b.Position)) Stop();
            else SetDown();
        }
        else if (b.ButtonIndex == MouseButton.Left && b.Pressed && !dragging) SetDown();
        else if (b.Pressed && b.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            yawTarget += Mathf.Pi / 8f * (b.ButtonIndex == MouseButton.WheelUp ? 1 : -1);
        else return;
        GetViewport().SetInputAsHandled();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Active) return;
        bool handled = true;
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape }) Stop();
        else if (e.IsActionPressed(NUse) || e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Enter or Key.KpEnter }) SetDown();
        else if (e.IsActionPressed(NRotate)) yawTarget += Mathf.Pi / 4f * (Input.IsKeyPressed(Key.Shift) ? -1 : 1);
        else if (e.IsActionPressed(NNext)) Next();
        else handled = false;
        if (handled) GetViewport().SetInputAsHandled();
    }

    void Next()
    {
        var all = Link.InBag().ToList();
        if (all.Count < 2 || thing == null) return;
        Start(all[(all.IndexOf(thing) + 1) % all.Count]);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        offerLeft -= dt;
        if (thing is not { } t || ghost == null) return;
        // Put back when it's gone from the bag (traded, dropped) or a panel opens.
        if (!Link.InBag().Contains(t) || Panels.Open()) { Stop(); return; }

        var target = PointUnderMouse() ?? Me.Position + CameraRig.Forward * 1.5f;
        var offset = target - Me.Position;
        offset.Y = 0;
        if (offset.Length() > Reach) target = Me.Position + offset.Normalized() * Reach;
        ghostPos = ghostPos.Lerp(target, 1f - Mathf.Exp(-14f * dt));
        yaw = Mathf.LerpAngle(yaw, yawTarget, 1f - Mathf.Exp(-12f * dt));
        ghost.Position = new Vector3(ghostPos.X, Terrain.HeightAt(ghostPos.X, ghostPos.Z), ghostPos.Z);
        // Over the bag row it would go back, so the ghost hides.
        ghost.Visible = !InventoryBar.RowRect.HasPoint(GetViewport().GetMousePosition());
        ghost.Rotation = new Vector3(0, yaw, 0);

        problem = Link.Fits(t, ghostPos.X, ghostPos.Z, yawTarget);
        ghostMaterial.AlbedoColor = ghostMaterial.AlbedoColor.Lerp(problem == null ? Fits : Blocked, 1f - Mathf.Exp(-10f * dt));
        UpdateHint();
    }

    void UpdateHint()
    {
        if (thing is not { } t) return;
        string more = Link.InBag().Count() > 1 ? "    Tab next" : "";
        hint.Text = problem ?? (dragging
            ? $"{Capital(Name(t))}: let go to set it down    T or the wheel turns it    drop it on your bag to put it back"
            : $"{Capital(Name(t))}: click to set it down    T or the wheel turns it{more}    Esc puts it back");
    }

    Vector3? PointUnderMouse()
    {
        var camera = CameraRig.Camera;
        var screen = GetViewport().GetVisibleRect().Size;
        var mouse = GetViewport().GetMousePosition() * new Vector2(View.Size.X / screen.X, View.Size.Y / screen.Y);
        var origin = camera.ProjectRayOrigin(mouse);
        var dir = camera.ProjectRayNormal(mouse);
        float prev = 0f;
        for (float s = 0.5f; s < 60f; s += 0.5f)
        {
            var p = origin + dir * s;
            if (p.Y > Terrain.HeightAt(p.X, p.Z)) { prev = s; continue; }
            float lo = prev, hi = s;
            for (int i = 0; i < 8; i++)
            {
                float mid = (lo + hi) / 2;
                var m = origin + dir * mid;
                if (m.Y > Terrain.HeightAt(m.X, m.Z)) lo = mid; else hi = mid;
            }
            return origin + dir * hi;
        }
        return null;
    }

    static string Name(Placeable t) => Items.All.TryGetValue(t.Item, out var def) ? def.Count(1).ToLowerInvariant() : "thing";
    static string Capital(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
