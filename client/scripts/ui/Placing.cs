using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// A crafted thing in the bag that can be set down in the world (crafting-20).
public record Placeable(Item Item, byte Quality);

// What placement talks to. Crafting owns the server side (the reducer, which items
// are placeable, the rules); until its contract lands, MockPlaceLink stands in.
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

// Setting crafted things down (crafting-20, D-?): right after a placeable craft the G
// prompt offers "Place the coil pot" for a while, and the Place key (P) takes the first
// placeable thing in the bag (Tab cycles). Both open the same ghost: the item's own
// model, see-through and pale green like Building's placement ghost, amber where it
// can't go. T and Shift+T turn it, click, Enter or G sets it down, and Esc puts it back.
// Main sets the scene references and Note.
public partial class Placing : Node
{
    public IPlaceLink Link = new MockPlaceLink();
    public Terrain Terrain = null!;
    public Node3D Me = null!;
    public CameraRig CameraRig = null!;
    public SubViewport View = null!;
    public Action<string> Note = _ => { };

    static readonly StringName NPlace = "place_item", NUse = "use", NRotate = "build_rotate", NNext = "build_next";
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
    bool hinted;

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

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Active)
        {
            if (InputMap.HasAction(NPlace) && e.IsActionPressed(NPlace) && !e.IsEcho() && !Panels.Open())
            {
                if (Link.InBag().FirstOrDefault() is { } first) Start(first);
                else Note("You carry nothing to set down");
                GetViewport().SetInputAsHandled();
            }
            return;
        }
        bool handled = true;
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape }) Stop();
        else if (e.IsActionPressed(NUse) || e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Enter or Key.KpEnter }
                 || e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) SetDown();
        else if (e.IsActionPressed(NRotate)) yawTarget += Mathf.Pi / 4f * (Input.IsKeyPressed(Key.Shift) ? -1 : 1);
        else if (e.IsActionPressed(NNext)) Next();
        else if (e.IsActionPressed(NPlace)) Stop();
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
        // The first time you carry something to set down, the one-time hint teaches P.
        if (!hinted && !Active && Link.InBag().Any())
        {
            hinted = true;
            Hints.Request(NPlace);
        }
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
        ghost.Rotation = new Vector3(0, yaw, 0);

        problem = Link.Fits(t, ghostPos.X, ghostPos.Z, yawTarget);
        ghostMaterial.AlbedoColor = ghostMaterial.AlbedoColor.Lerp(problem == null ? Fits : Blocked, 1f - Mathf.Exp(-10f * dt));
        UpdateHint();
    }

    void UpdateHint()
    {
        if (thing is not { } t) return;
        string use = Controls.KeyFor("use") ?? "G";
        string more = Link.InBag().Count() > 1 ? "    Tab next" : "";
        hint.Text = problem ?? $"{Capital(Name(t))}: click or {use} sets it down    T turns{more}    Esc puts it back";
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
