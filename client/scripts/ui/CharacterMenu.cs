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
// in, and a way to delete/create new characters?"). Look (a), "Camp at dusk": the world dimmed
// behind, a painted frame low in the centre, a card per character (a small portrait of them
// turning slowly, their name, race and when they last played) and a faint card for each free
// slot. Play is the one filled button; New character opens today's look picker (Justin: "can be
// used temporarily"); Delete asks you to type the name, saying plainly what is lost.
public partial class CharacterMenu : CanvasLayer
{
    public ICharacterLink Link = new MockCharacterLink();
    // Opens the look picker for a new character.
    public Action NewCharacter = () => { };

    public bool IsOpen => root.Visible;

    const float CardWidth = 210f, Portrait = 230f;
    static readonly Color ChosenRing = new(0.96f, 0.78f, 0.42f);

    Control root = null!;
    HBoxContainer cards = null!;
    VBoxContainer actions = null!, confirm = null!;
    Button play = null!, deleteButton = null!, confirmButton = null!;
    Label confirmText = null!;
    LineEdit typed = null!;
    readonly List<Node3D> figures = new();
    ulong chosen;

    public override void _Ready()
    {
        Layer = 6;
        root = new Control { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        // The world, dimmed: the menu sits at the camp, not in a void.
        var scrim = new ColorRect { Color = new Color(0.03f, 0.03f, 0.02f, 0.5f), MouseFilter = Control.MouseFilterEnum.Ignore };
        scrim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(scrim);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 14);
        var title = Ui.Title("Who will you be today?");
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(title);
        cards = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        cards.AddThemeConstantOverride("separation", 16);
        box.AddChild(cards);

        actions = new VBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        play = Ui.MainButton("Play", Play);
        play.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        actions.AddChild(play);
        var quiet = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        quiet.AddThemeConstantOverride("separation", 24);
        quiet.AddChild(ControlsPage.TextButton("New character", () => NewCharacter()));
        deleteButton = ControlsPage.TextButton("Delete", AskDelete);
        deleteButton.Modulate = new Color(1, 1, 1, 0.6f);
        quiet.AddChild(deleteButton);
        actions.AddChild(quiet);
        box.AddChild(actions);

        confirm = MakeConfirm();
        confirm.Visible = false;
        box.AddChild(confirm);

        var hint = Hud.Text("Enter plays    Click a card to choose", 13);
        hint.Modulate = new Color(1, 1, 1, 0.55f);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(hint);

        var frame = Ui.Window(box, roomy: true);
        // Low in the centre, so the sky shows above it.
        frame.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        frame.GrowVertical = Control.GrowDirection.Begin;
        frame.OffsetBottom = -70;
        root.AddChild(frame);

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
        // The portraits turn slowly, as if they'd heard their name.
        foreach (var f in figures) f.RotateY((float)delta * 0.35f);
    }

    void Fill()
    {
        foreach (var child in cards.GetChildren()) { cards.RemoveChild(child); child.QueueFree(); }
        figures.Clear();
        var list = Link.Characters;
        if (list.All(c => c.Id != chosen)) chosen = list.FirstOrDefault()?.Id ?? 0;
        foreach (var c in list) cards.AddChild(CardFor(c));
        for (int i = list.Count; i < Link.Slots; i++) cards.AddChild(EmptySlot());
        play.Disabled = chosen == 0;
        deleteButton.Visible = chosen != 0;
        if (chosen == 0) { confirm.Visible = false; actions.Visible = true; }
    }

    Control CardFor(CharacterCard c)
    {
        bool picked = c.Id == chosen;
        var card = new Card(CardWidth);
        var style = (StyleBoxFlat)card.GetThemeStylebox("normal");
        if (picked)
        {
            foreach (var state in new[] { "normal", "hover", "pressed" })
            {
                var s = (StyleBoxFlat)style.Duplicate();
                s.BorderColor = ChosenRing;
                s.SetBorderWidthAll(2);
                s.BgColor = new Color(0.16f, 0.13f, 0.08f, 0.75f);
                card.AddThemeStyleboxOverride(state, s);
            }
        }
        card.Pressed += () => { chosen = c.Id; Fill(); };

        // A little scene of their own: the chibi in their look, lit warm.
        var frame = new SubViewportContainer { CustomMinimumSize = new Vector2(card.Inner, Portrait), Stretch = true, MouseFilter = Control.MouseFilterEnum.Ignore };
        var view = new SubViewport { OwnWorld3D = true, TransparentBg = true, Msaa3D = Viewport.Msaa.Msaa4X, Size = new Vector2I((int)card.Inner, (int)Portrait) };
        frame.AddChild(view);
        var camera = new Camera3D { Fov = 26f };
        view.AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 1.0f, 4.0f), new Vector3(0, 0.9f, 0), Vector3.Up);
        view.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-30, 35, 0), LightEnergy = 1.1f, LightColor = new Color(1f, 0.9f, 0.78f) });
        view.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.ClearColor,
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.8f, 0.74f, 0.66f),
                AmbientLightEnergy = 0.6f,
            },
        });
        var figure = PlayerModel.Build(c.Look);
        figure.RotateY(Mathf.DegToRad(-20));
        view.AddChild(figure);
        figures.Add(figure);
        card.Column.AddChild(frame);

        var name = card.Line(c.Name, Ui.TitleSize - 2);
        card.Column.AddChild(name);
        // The race shows once #471's rows reach the character list.
        var about = card.Line(c.Race.Length > 0 ? $"{c.Race}  ·  {Played(c.LastPlayed)}" : Played(c.LastPlayed), 13);
        about.Modulate = new Color(1, 1, 1, 0.65f);
        card.Column.AddChild(about);
        return card;
    }

    Control EmptySlot()
    {
        var card = new Card(CardWidth, Portrait + 70);
        foreach (var state in new[] { "normal", "hover", "pressed" })
        {
            var s = new StyleBoxFlat { BgColor = new Color(0.09f, 0.10f, 0.07f, state == "normal" ? 0.25f : 0.4f), BorderColor = new Color(Look.Cream, state == "normal" ? 0.16f : 0.4f) };
            s.SetBorderWidthAll(1);
            s.SetCornerRadiusAll(10);
            card.AddThemeStyleboxOverride(state, s);
        }
        card.Column.Alignment = BoxContainer.AlignmentMode.Center;
        var plus = card.Line("+", 34);
        plus.Modulate = new Color(1, 1, 1, 0.45f);
        card.Column.AddChild(plus);
        var words = card.Line("New character", 15);
        words.Modulate = new Color(1, 1, 1, 0.55f);
        card.Column.AddChild(words);
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
        box.AddThemeConstantOverride("separation", 8);
        confirmText = Hud.Text("", 15);
        confirmText.HorizontalAlignment = HorizontalAlignment.Center;
        confirmText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        confirmText.CustomMinimumSize = new Vector2(3 * CardWidth, 0);
        box.AddChild(confirmText);
        typed = new LineEdit { CustomMinimumSize = new Vector2(300, 38), SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        typed.AddThemeFontSizeOverride("font_size", 16);
        typed.TextChanged += _ => confirmButton.Disabled = !Matches();
        typed.TextSubmitted += _ => { if (Matches()) DeleteNow(); };
        box.AddChild(typed);
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 24);
        confirmButton = Ui.MainButton("Delete for good", DeleteNow);
        // A deep clay red: the one place a button warns.
        foreach (var (state, c) in new[] { ("normal", new Color(0.55f, 0.24f, 0.18f, 0.95f)), ("hover", new Color(0.66f, 0.3f, 0.22f, 1f)), ("pressed", new Color(0.66f, 0.3f, 0.22f, 1f)) })
        {
            var s = new StyleBoxFlat { BgColor = c };
            s.SetCornerRadiusAll(8);
            confirmButton.AddThemeStyleboxOverride(state, s);
        }
        row.AddChild(confirmButton);
        row.AddChild(ControlsPage.TextButton("Keep them", KeepIt));
        box.AddChild(row);
        return box;
    }

    CharacterCard? Chosen() => Link.Characters.FirstOrDefault(c => c.Id == chosen);

    bool Matches() => Chosen() is { } c && typed.Text.Trim().Equals(c.Name, StringComparison.OrdinalIgnoreCase);

    void AskDelete()
    {
        if (Chosen() is not { } c) return;
        confirmText.Text = $"Delete {c.Name}? Their belongings are lost. Their claims will weather away over time.\nType their name to be sure.";
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
