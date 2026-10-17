using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// One of your characters, as the menu shows it.
public sealed record CharacterCard(ulong Id, string Name, string Race, HumanFigure.Appearance Look, DateTime? LastPlayed);

// What the character menu needs from the server (Survival's multi-character model). The server
// decides everything (D-182): who you may play, the slot limit, and that a delete names the
// character it deletes.
public interface ICharacterLink
{
    IReadOnlyList<CharacterCard> Characters { get; }
    // How many characters a login may have.
    int Slots { get; }
    void Play(ulong id);
    // Deletes the character; typedName is what the player typed to confirm (the server checks it).
    void Delete(ulong id, string typedName);
    event Action? Changed;
}

// Two pretend characters, for screenshots and for building the screen before the server lands.
public sealed class MockCharacterLink : ICharacterLink
{
    readonly List<CharacterCard> list = new()
    {
        new(1, "Mara Thistledown", "Human", new HumanFigure.Appearance(2, 1, 1, false), DateTime.Now.AddDays(-2)),
        new(2, "Bram", "Human", new HumanFigure.Appearance(0, 3, 0, true), null),
    };
    public IReadOnlyList<CharacterCard> Characters => list;
    public int Slots => 3;
    public event Action? Changed;
    public void Play(ulong id) { }
    public void Delete(ulong id, string typedName)
    {
        list.RemoveAll(c => c.Id == id && c.Name == typedName);
        Changed?.Invoke();
    }
}

// The first screen after connecting (ui-39, Justin: "Can you make a character menu when logging
// in, and a way to delete/create new characters?"). Justin's pick: "Cutesy page". One warm
// paper page with soft corners and little hand-drawn doodles round its edges (a sun, stars,
// leaves, hearts); each character in a rounded polaroid with the chibi bobbing gently on a
// pastel ground, the name in a friendly rounded font and the race as a small badge; a sketched
// empty frame for each free slot; a big soft Play button and a small quiet Delete. New character
// opens today's look picker (Justin: "can be used temporarily"); Delete asks you to type the
// name, saying plainly what is lost.
public partial class CharacterMenu : CanvasLayer
{
    public ICharacterLink Link = new MockCharacterLink();
    // Opens the look picker for a new character.
    public Action NewCharacter = () => { };

    public bool IsOpen => root.Visible;

    const float CardWidth = 200f, Portrait = 210f;

    // The page's palette: warm paper, soft brown ink, pastel accents.
    public static readonly Color Paper = new(0.99f, 0.95f, 0.88f), Ink = new(0.42f, 0.30f, 0.24f),
        Coral = new(0.95f, 0.56f, 0.47f), Butter = new(0.98f, 0.82f, 0.45f), Sage = new(0.60f, 0.78f, 0.55f),
        Sky = new(0.62f, 0.76f, 0.92f), Lilac = new(0.78f, 0.68f, 0.90f), Peach = new(0.99f, 0.80f, 0.70f);
    static readonly Color[] Grounds = { new(0.99f, 0.86f, 0.80f), new(0.82f, 0.93f, 0.84f), new(0.88f, 0.84f, 0.96f) };

    Control root = null!;
    HBoxContainer cards = null!;
    VBoxContainer actions = null!, confirm = null!;
    Button play = null!, deleteButton = null!, confirmButton = null!;
    Label confirmText = null!;
    LineEdit typed = null!;
    readonly List<Node3D> figures = new();
    ulong chosen;
    float time;

    // Nunito (SIL OFL, client/assets/fonts), rounded and friendly; the default font if it's missing.
    static Font? rounded;
    public static Font Rounded(int weight)
    {
        rounded ??= GD.Load<FontFile>("res://assets/fonts/Nunito.ttf");
        var face = new FontVariation { BaseFont = rounded ?? ThemeDB.FallbackFont };
        face.VariationOpentype = new Godot.Collections.Dictionary { { TextServerManager.GetPrimaryInterface().NameToTag("wght"), weight } };
        return face;
    }

    public static Label Text(string text, int size, int weight = 600, Color? colour = null)
    {
        var l = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        l.AddThemeFontOverride("font", Rounded(weight));
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", colour ?? Ink);
        return l;
    }

    public override void _Ready()
    {
        Layer = 6;
        root = new Control { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        // A warm haze over the world, so the page sits in the camp, not in a void.
        var haze = new ColorRect { Color = new Color(0.30f, 0.20f, 0.18f, 0.45f), MouseFilter = Control.MouseFilterEnum.Ignore };
        haze.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(haze);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 16);
        var title = Text("Who's playing today?", 32, 800);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(title);
        cards = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        cards.AddThemeConstantOverride("separation", 26);
        box.AddChild(cards);

        actions = new VBoxContainer();
        actions.AddThemeConstantOverride("separation", 10);
        play = SoftButton("Play", Coral, Play);
        play.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        actions.AddChild(play);
        var quiet = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        quiet.AddThemeConstantOverride("separation", 28);
        quiet.AddChild(Link_("New character", () => NewCharacter(), 16, 0.8f));
        deleteButton = Link_("Delete", AskDelete, 14, 0.5f);
        quiet.AddChild(deleteButton);
        actions.AddChild(quiet);
        box.AddChild(actions);

        confirm = MakeConfirm();
        confirm.Visible = false;
        box.AddChild(confirm);

        // The page: warm paper, round corners, a soft shadow, doodles round the edge.
        var page = new PanelContainer();
        var paper = new StyleBoxFlat { BgColor = Paper, BorderColor = new Color(Ink, 0.25f), ShadowColor = new Color(0.15f, 0.08f, 0.05f, 0.30f), ShadowSize = 22, ShadowOffset = new Vector2(0, 8) };
        paper.SetBorderWidthAll(3);
        paper.SetCornerRadiusAll(34);
        paper.ContentMarginLeft = paper.ContentMarginRight = 52;
        paper.ContentMarginTop = 34;
        paper.ContentMarginBottom = 30;
        page.AddThemeStyleboxOverride("panel", paper);
        page.AddChild(new Doodles());
        page.AddChild(box);
        page.SetAnchorsPreset(Control.LayoutPreset.Center);
        page.GrowHorizontal = page.GrowVertical = Control.GrowDirection.Both;
        root.AddChild(page);

        Link.Changed += Fill;
        // --screenshot-characters shows the mock; =delete opens the delete check on Mara.
        foreach (var arg in OS.GetCmdlineUserArgs().Where(a => a.StartsWith("--screenshot-characters")))
            GetTree().CreateTimer(1.5).Timeout += () =>
            {
                Open();
                if (arg.EndsWith("=delete")) AskDelete();
            };
    }

    public void Attach(ICharacterLink link)
    {
        Link.Changed -= Fill;
        Link = link;
        Link.Changed += Fill;
        if (IsOpen) Fill();
    }

    public void Open()
    {
        root.Visible = true;
        chosen = Link.Characters.OrderByDescending(c => c.LastPlayed ?? DateTime.MinValue).FirstOrDefault()?.Id ?? 0;
        Fill();
    }

    public void Close() => root.Visible = false;

    public override void _UnhandledInput(InputEvent e)
    {
        if (!IsOpen || e is not InputEventKey { Pressed: true, Echo: false } k) return;
        if (confirm.Visible && e.IsActionPressed("ui_cancel")) KeepIt();
        else if (!confirm.Visible && k.PhysicalKeycode is Key.Enter or Key.KpEnter) Play();
        else return;
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!IsOpen) return;
        time += (float)delta;
        // Each chibi bobs gently on its own beat, with a little sway.
        for (int i = 0; i < figures.Count; i++)
        {
            float t = time * 2.2f + i * 1.7f;
            figures[i].Position = new Vector3(0, Mathf.Abs(Mathf.Sin(t)) * 0.06f, 0);
            figures[i].Rotation = new Vector3(0, -0.35f + Mathf.Sin(t * 0.5f) * 0.12f, Mathf.Sin(t) * 0.03f);
        }
    }

    void Fill()
    {
        foreach (var child in cards.GetChildren()) { cards.RemoveChild(child); child.QueueFree(); }
        figures.Clear();
        var list = Link.Characters;
        if (list.All(c => c.Id != chosen)) chosen = list.FirstOrDefault()?.Id ?? 0;
        for (int i = 0; i < list.Count; i++) cards.AddChild(Polaroid(list[i], i));
        for (int i = list.Count; i < Link.Slots; i++) cards.AddChild(EmptyFrame(i));
        play.Disabled = chosen == 0;
        deleteButton.Visible = chosen != 0;
        if (chosen == 0) { confirm.Visible = false; actions.Visible = true; }
    }

    // A polaroid: white with a soft shadow, tipped a little, the chibi on a pastel ground.
    Control Polaroid(CharacterCard c, int index)
    {
        bool picked = c.Id == chosen;
        // A Card measures what's inside it (ui-33), so the photo, name and badges never spill.
        var card = new Card(CardWidth) { ClipContents = false };
        foreach (var state in new[] { "normal", "hover", "pressed" })
        {
            var s = new StyleBoxFlat
            {
                BgColor = new Color(1f, 0.99f, 0.97f),
                BorderColor = picked ? Coral : new Color(Ink, state == "normal" ? 0.12f : 0.3f),
                ShadowColor = new Color(0.25f, 0.12f, 0.08f, picked ? 0.30f : 0.18f),
                ShadowSize = picked ? 14 : 8,
                ShadowOffset = new Vector2(0, 4),
            };
            s.SetBorderWidthAll(picked ? 4 : 2);
            s.SetCornerRadiusAll(18);
            card.AddThemeStyleboxOverride(state, s);
        }
        var column = card.Column;
        column.AddThemeConstantOverride("separation", 6);

        // The photo: the chibi in their look on a pastel ground, warm light.
        var frame = new SubViewportContainer { CustomMinimumSize = new Vector2(CardWidth - 16, Portrait), Stretch = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        var view = new SubViewport { OwnWorld3D = true, Msaa3D = Viewport.Msaa.Msaa4X, Size = new Vector2I((int)CardWidth - 16, (int)Portrait) };
        frame.AddChild(view);
        var camera = new Camera3D { Fov = 26f };
        view.AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 0.95f, 3.9f), new Vector3(0, 0.85f, 0), Vector3.Up);
        view.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-30, 35, 0), LightEnergy = 1.0f, LightColor = new Color(1f, 0.93f, 0.84f) });
        view.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = Grounds[index % Grounds.Length],
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.95f, 0.88f, 0.82f),
                AmbientLightEnergy = 0.7f,
            },
        });
        var figure = PlayerModel.Build(c.Look);
        view.AddChild(figure);
        figures.Add(figure);
        column.AddChild(frame);

        var name = Text(c.Name, 21, 800);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        name.CustomMinimumSize = new Vector2(card.Inner, 0);
        column.AddChild(name);
        var badges = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        badges.AddThemeConstantOverride("separation", 6);
        // The race badge shows once #471's rows reach the character list.
        if (c.Race.Length > 0) badges.AddChild(Badge(c.Race, Grounds[index % Grounds.Length].Darkened(0.08f)));
        badges.AddChild(Badge(Played(c.LastPlayed), new Color(Ink, 0.08f)));
        column.AddChild(badges);

        // Tipped a little, like photos pinned to a board; the chosen one sits straight and close.
        card.RotationDegrees = picked ? 0f : (index % 2 == 0 ? -2.5f : 2f);
        card.Scale = picked ? new Vector2(1.04f, 1.04f) : Vector2.One;
        card.Resized += () => card.PivotOffset = card.Size / 2;
        card.Pressed += () => { chosen = c.Id; Fill(); };
        if (picked) card.AddChild(new Sticker { Position = new Vector2(CardWidth - 24, -12), MouseFilter = Control.MouseFilterEnum.Ignore });
        return card;
    }

    static Control Badge(string text, Color ground)
    {
        var pill = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var s = new StyleBoxFlat { BgColor = ground };
        s.SetCornerRadiusAll(10);
        s.ContentMarginLeft = s.ContentMarginRight = 9;
        s.ContentMarginTop = s.ContentMarginBottom = 2;
        pill.AddThemeStyleboxOverride("panel", s);
        pill.AddChild(Text(text, 13, 700, new Color(Ink, 0.85f)));
        return pill;
    }

    // A free slot: a sketched dashed frame with a plus, inviting rather than empty.
    Control EmptyFrame(int index)
    {
        var card = new Button { FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(CardWidth, Portrait + 84) };
        foreach (var state in new[] { "normal", "hover", "pressed" })
            card.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        var sketch = new Sketch { MouseFilter = Control.MouseFilterEnum.Ignore };
        sketch.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        card.AddChild(sketch);
        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        column.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var plus = Text("+", 54, 700, new Color(Ink, 0.45f));
        plus.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(plus);
        var words = Text("New character", 17, 700, new Color(Ink, 0.6f));
        words.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(words);
        card.AddChild(column);
        card.MouseEntered += () => sketch.Hover = true;
        card.MouseExited += () => sketch.Hover = false;
        card.RotationDegrees = index % 2 == 0 ? -1.5f : 1.5f;
        card.Resized += () => card.PivotOffset = card.Size / 2;
        card.Pressed += () => NewCharacter();
        return card;
    }

    // "Played today", "Played yesterday", "Played 3 days ago", "Not played yet".
    public static string Played(DateTime? when)
    {
        if (when is not { } t) return "Not played yet";
        int days = (int)(DateTime.Now.Date - t.Date).TotalDays;
        return days <= 0 ? "Played today" : days == 1 ? "Played yesterday" : days < 60 ? $"Played {days} days ago" : "Played long ago";
    }

    // A big, soft, rounded button.
    static Button SoftButton(string text, Color colour, Action pressed)
    {
        var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(230, 58) };
        b.AddThemeFontOverride("font", Rounded(800));
        b.AddThemeFontSizeOverride("font_size", 26);
        foreach (var (state, c) in new[] { ("normal", colour), ("hover", colour.Lightened(0.1f)), ("pressed", colour.Darkened(0.06f)), ("disabled", colour.Lerp(Paper, 0.6f)) })
        {
            var s = new StyleBoxFlat { BgColor = c, ShadowColor = new Color(colour.Darkened(0.4f), 0.35f), ShadowSize = state == "pressed" ? 2 : 6, ShadowOffset = new Vector2(0, state == "pressed" ? 1 : 4) };
            s.SetCornerRadiusAll(29);
            b.AddThemeStyleboxOverride(state, s);
        }
        foreach (var key in new[] { "font_color", "font_hover_color", "font_pressed_color" }) b.AddThemeColorOverride(key, new Color(1f, 0.98f, 0.95f));
        b.AddThemeColorOverride("font_disabled_color", new Color(1f, 0.98f, 0.95f, 0.7f));
        b.Pressed += pressed;
        return b;
    }

    // A quiet text link in the page's ink.
    static Button Link_(string text, Action pressed, int size, float alpha)
    {
        var b = new Button { Text = text, Flat = true, FocusMode = Control.FocusModeEnum.None };
        b.AddThemeFontOverride("font", Rounded(700));
        b.AddThemeFontSizeOverride("font_size", size);
        b.AddThemeColorOverride("font_color", new Color(Ink, alpha));
        b.AddThemeColorOverride("font_hover_color", Ink);
        b.AddThemeColorOverride("font_pressed_color", Ink);
        b.Pressed += pressed;
        return b;
    }

    void Play()
    {
        if (chosen == 0) return;
        Link.Play(chosen);
        // Main reopens the menu if the server refuses.
        Close();
    }

    // --- Deleting ------------------------------------------------------------

    VBoxContainer MakeConfirm()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        confirmText = Text("", 17, 600);
        confirmText.HorizontalAlignment = HorizontalAlignment.Center;
        confirmText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        confirmText.CustomMinimumSize = new Vector2(3 * CardWidth, 0);
        box.AddChild(confirmText);
        typed = new LineEdit { CustomMinimumSize = new Vector2(300, 42), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        typed.AddThemeFontOverride("font", Rounded(700));
        typed.AddThemeFontSizeOverride("font_size", 18);
        typed.AddThemeColorOverride("font_color", Ink);
        typed.AddThemeColorOverride("font_placeholder_color", new Color(Ink, 0.35f));
        var field = new StyleBoxFlat { BgColor = new Color(1f, 1f, 1f, 0.9f), BorderColor = new Color(Ink, 0.3f) };
        field.SetBorderWidthAll(2);
        field.SetCornerRadiusAll(14);
        field.ContentMarginLeft = field.ContentMarginRight = 12;
        typed.AddThemeStyleboxOverride("normal", field);
        typed.AddThemeStyleboxOverride("focus", field);
        typed.TextChanged += _ => confirmButton.Disabled = !Matches();
        typed.TextSubmitted += _ => { if (Matches()) DeleteNow(); };
        box.AddChild(typed);
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 24);
        // A deep clay red: the one place a button warns.
        confirmButton = SoftButton("Delete for good", new Color(0.78f, 0.36f, 0.30f), DeleteNow);
        confirmButton.CustomMinimumSize = new Vector2(200, 46);
        confirmButton.AddThemeFontSizeOverride("font_size", 18);
        row.AddChild(confirmButton);
        row.AddChild(Link_("Keep them", KeepIt, 17, 0.8f));
        box.AddChild(row);
        return box;
    }

    CharacterCard? Chosen() => Link.Characters.FirstOrDefault(c => c.Id == chosen);

    bool Matches() => Chosen() is { } c && typed.Text.Trim().Equals(c.Name, StringComparison.OrdinalIgnoreCase);

    void AskDelete()
    {
        if (Chosen() is not { } c) return;
        confirmText.Text = $"Say goodbye to {c.Name}? Their belongings are lost. Their claims will weather away over time.\nType their name to be sure.";
        typed.Text = "";
        typed.PlaceholderText = c.Name;
        confirmButton.Disabled = true;
        actions.Visible = false;
        confirm.Visible = true;
        typed.GrabFocus();
    }

    void DeleteNow()
    {
        if (Chosen() is not { } c || !Matches()) return;
        Link.Delete(c.Id, typed.Text.Trim());
        KeepIt();
    }

    void KeepIt()
    {
        typed.ReleaseFocus();
        confirm.Visible = false;
        actions.Visible = true;
    }
}

// Little hand-drawn doodles round the page's edge: a sun, stars, leaves and hearts in pastel
// ink, wobbly like a pencil and twinkling gently. They keep to the margins.
public partial class Doodles : Control
{
    float time;

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Process(double delta)
    {
        time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // The panel's content rect; the doodles sit in the margin just outside it.
        var r = new Rect2(Vector2.Zero, Size);
        Sun(new Vector2(-24, -6), 15f);
        Star(new Vector2(r.End.X + 24, 8), 9f, CharacterMenu.Butter, 0);
        Star(new Vector2(r.End.X + 10, 46), 5f, CharacterMenu.Coral, 1);
        Star(new Vector2(r.Size.X * 0.32f, -20), 6f, CharacterMenu.Lilac, 2);
        Star(new Vector2(-30, r.Size.Y * 0.55f), 6f, CharacterMenu.Sky, 3);
        Heart(new Vector2(r.End.X + 26, r.Size.Y * 0.5f), 8f, CharacterMenu.Coral);
        Heart(new Vector2(r.Size.X * 0.7f, r.End.Y + 16), 6f, CharacterMenu.Peach.Darkened(0.1f));
        Leaf(new Vector2(-28, r.End.Y - 6), -0.6f);
        Leaf(new Vector2(-12, r.End.Y + 12), 0.2f);
        Leaf(new Vector2(r.End.X + 20, r.End.Y - 4), 2.4f);
        Star(new Vector2(r.Size.X * 0.15f, r.End.Y + 16), 5f, CharacterMenu.Butter, 4);
    }

    // A pencil line: a little wobble, so it reads hand-drawn rather than ruled.
    void Wobbly(Vector2[] points, Color colour, float width = 2.4f)
    {
        var w = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
            w[i] = points[i] + new Vector2(Mathf.Sin(i * 2.3f) * 0.7f, Mathf.Cos(i * 1.7f) * 0.7f);
        DrawPolyline(w, colour, width, true);
    }

    void Sun(Vector2 c, float radius)
    {
        var ring = Enumerable.Range(0, 25).Select(i => c + Vector2.FromAngle(i * Mathf.Tau / 24) * radius).ToArray();
        DrawCircle(c, radius, new Color(CharacterMenu.Butter, 0.55f));
        Wobbly(ring, CharacterMenu.Butter.Darkened(0.15f));
        float spin = time * 0.2f;
        for (int i = 0; i < 8; i++)
        {
            var d = Vector2.FromAngle(spin + i * Mathf.Tau / 8);
            DrawLine(c + d * (radius + 4), c + d * (radius + 10), CharacterMenu.Butter.Darkened(0.1f), 2.4f, true);
        }
    }

    void Star(Vector2 c, float size, Color colour, int seed)
    {
        float s = size * (1f + 0.12f * Mathf.Sin(time * 2f + seed * 1.3f));
        var pts = Enumerable.Range(0, 11).Select(i =>
            c + Vector2.FromAngle(-Mathf.Pi / 2 + i * Mathf.Pi / 5) * (i % 2 == 0 ? s : s * 0.45f)).ToArray();
        DrawColoredPolygon(pts[..10], new Color(colour, 0.45f));
        Wobbly(pts, colour.Darkened(0.12f), 2f);
    }

    void Heart(Vector2 c, float size, Color colour)
    {
        var pts = Enumerable.Range(0, 33).Select(i =>
        {
            float t = i * Mathf.Tau / 32;
            float x = 16 * Mathf.Pow(Mathf.Sin(t), 3);
            float y = -(13 * Mathf.Cos(t) - 5 * Mathf.Cos(2 * t) - 2 * Mathf.Cos(3 * t) - Mathf.Cos(4 * t));
            return c + new Vector2(x, y) * (size / 16f);
        }).ToArray();
        DrawColoredPolygon(pts[..32], new Color(colour, 0.5f));
        Wobbly(pts, colour.Darkened(0.15f), 2f);
    }

    void Leaf(Vector2 c, float angle)
    {
        var t = new Transform2D(angle + Mathf.Sin(time * 1.2f + c.X) * 0.06f, c);
        var edge = Enumerable.Range(0, 9).Select(i => t * new Vector2(i * 2.6f, -Mathf.Sin(i / 8f * Mathf.Pi) * 7f))
            .Concat(Enumerable.Range(0, 9).Select(i => t * new Vector2((8 - i) * 2.6f, Mathf.Sin((8 - i) / 8f * Mathf.Pi) * 7f))).ToArray();
        DrawColoredPolygon(edge, new Color(CharacterMenu.Sage, 0.5f));
        Wobbly(edge.Append(edge[0]).ToArray(), CharacterMenu.Sage.Darkened(0.2f), 2f);
        DrawLine(t * Vector2.Zero, t * new Vector2(21f, 0), CharacterMenu.Sage.Darkened(0.25f), 1.6f, true);
    }
}

// A free slot's frame: a dashed, slightly wobbly rounded outline, darker on hover.
public partial class Sketch : Control
{
    bool hover;
    public bool Hover { set { hover = value; QueueRedraw(); } }

    public override void _Draw()
    {
        var r = new Rect2(Vector2.One * 3, Size - Vector2.One * 6);
        const float radius = 18f;
        var outline = new List<Vector2>();
        void Arc(Vector2 c, float from)
        {
            for (int i = 0; i <= 6; i++) outline.Add(c + Vector2.FromAngle(from + i * Mathf.Pi / 12) * radius);
        }
        Arc(new Vector2(r.End.X - radius, r.Position.Y + radius), -Mathf.Pi / 2);
        Arc(new Vector2(r.End.X - radius, r.End.Y - radius), 0);
        Arc(new Vector2(r.Position.X + radius, r.End.Y - radius), Mathf.Pi / 2);
        Arc(new Vector2(r.Position.X + radius, r.Position.Y + radius), Mathf.Pi);
        outline.Add(outline[0]);
        if (hover) DrawRect(r.Grow(-2), new Color(1f, 1f, 1f, 0.35f));
        var ink = new Color(CharacterMenu.Ink, hover ? 0.6f : 0.35f);
        // Dashes along the outline.
        float dash = 9f, gap = 7f, along = 0f;
        bool on = true;
        for (int i = 0; i + 1 < outline.Count; i++)
        {
            Vector2 a = outline[i], b = outline[i + 1];
            float len = a.DistanceTo(b), at = 0f;
            while (at < len)
            {
                float left = (on ? dash : gap) - along, step = Mathf.Min(left, len - at);
                if (on) DrawLine(a.Lerp(b, at / len), a.Lerp(b, (at + step) / len), ink, 2.4f, true);
                at += step;
                along += step;
                if (along >= (on ? dash : gap)) { along = 0f; on = !on; }
            }
        }
    }
}

// A little star sticker on the chosen polaroid's corner.
public partial class Sticker : Control
{
    public override void _Draw()
    {
        var c = new Vector2(16, 16);
        var pts = Enumerable.Range(0, 10).Select(i =>
            c + Vector2.FromAngle(-Mathf.Pi / 2 + i * Mathf.Pi / 5) * (i % 2 == 0 ? 16f : 7.5f)).ToArray();
        DrawColoredPolygon(pts, CharacterMenu.Butter);
        DrawPolyline(pts.Append(pts[0]).ToArray(), CharacterMenu.Butter.Darkened(0.25f), 2f, true);
    }
}
