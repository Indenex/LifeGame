using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Choosing how you look (ui-21, D-233): skin tone, hair colour, haircut and beard, with
// your figure turning slowly beside the choices. It opens by itself a moment after you
// first join, when the server has no look for you yet, and again from Options ("Your
// look"). Done sends SetLook (Modeling's player_look, model-3); the server checks it
// with Looks.Valid, and every client redraws you from the row. Esc keeps the look you
// had. Main sets Conn and MyId.
public partial class LookPicker : CanvasLayer
{
    public Func<DbConnection?> Conn = () => null;
    public Func<uint> MyId = () => 0;
    // Where the server's answer to SetName goes when it isn't the picker's (/name in chat).
    public Action<string> Note = _ => { };
    // Options opens it through this; set when the picker is ready.
    public static Action? OpenFromMenu;

    public bool IsOpen => panel.Visible;

    Control panel = null!;
    Label title = null!, welcome = null!;
    ColorRect scrim = null!;
    SubViewport view = null!;
    Skeleton3D? figure;
    HumanFigure.Appearance look = HumanFigure.Appearance.Default;
    readonly Button[] skins = new Button[Looks.Skins], hairs = new Button[Looks.Hairs];
    readonly Button[] cuts = new Button[Looks.CutNames.Length], beards = new Button[Looks.BeardNames.Length];
    float spin, firstSeen = -1f;
    bool asked;
    // The name row at first join (ui-31), Done (kept off until the name is good), and the
    // name sent and not yet answered.
    NameField nameField = null!;
    Button done = null!;
    bool first, hooked;
    string? awaitingName;

    public override void _Ready()
    {
        Layer = 5;
        // The world dims behind the picker, so it is plain the game is waiting on this choice
        // (Justin missed it at first join and found the bag "not working"); it also keeps clicks
        // off the world while it is up.
        scrim = new ColorRect { Color = new Color(0.04f, 0.04f, 0.03f, 0.55f), MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
        scrim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(scrim);
        panel = MakePanel();
        panel.Visible = false;
        AddChild(panel);
        OpenFromMenu = () => Open(first: false);
        // --screenshot-look opens it on a default look, as at first join.
        if (OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--screenshot-look")))
            GetTree().CreateTimer(1.5).Timeout += () => Open(first: true);
    }

    public void Open(bool first)
    {
        // Start from the look you have now: your chosen row, or the one the game gave you.
        uint id = MyId();
        look = Conn()?.Db.PlayerLook.PlayerId.Find(id) is { } row
            ? HumanFigure.Appearance.From(row.Skin, row.Hair, row.Cut, row.Beard)
            : id != 0 ? HumanFigure.Appearance.For(id) : HumanFigure.Appearance.Default;
        title.Text = first ? "Choose how you look" : "Your look";
        welcome.Visible = first;
        this.first = first;
        nameField.Visible = first;
        if (first) nameField.Fill(Conn()?.Db.Player.PlayerId.Find(id)?.Name ?? "");
        UpdateDone();
        panel.Visible = scrim.Visible = true;
        Show(look);
    }

    public void Close() => panel.Visible = scrim.Visible = false;

    public override void _Process(double delta)
    {
        // At first join, once the rows have had a moment to arrive, ask if there is no look yet.
        if (!asked && Conn() is { } c && MyId() != 0)
        {
            float now = Time.GetTicksMsec() / 1000f;
            if (firstSeen < 0) firstSeen = now;
            else if (now - firstSeen > 3f)
            {
                asked = true;
                if (c.Db.PlayerLook.PlayerId.Find(MyId()) == null) Open(first: true);
            }
        }
        // The server's word on a name: a refusal of the one sent from here reopens the picker
        // with the reason under the field; any other (/name in chat) is a note as before.
        if (!hooked && Conn() is { } conn)
        {
            hooked = true;
            conn.Reducers.OnSetName += (ctx, name) =>
            {
                bool mine = awaitingName != null && Names.Key(name) == Names.Key(awaitingName);
                if (mine) awaitingName = null;
                if (ctx.Event.Status is not SpacetimeDB.Status.Failed(var reason)) return;
                if (!mine) { Note(reason); return; }
                Open(first: true);
                nameField.Refused(reason);
                UpdateDone();
            };
        }
        if (!IsOpen || figure == null) return;
        spin += (float)delta * 0.35f;
        figure.Rotation = new Vector3(0, Mathf.Sin(spin) * 0.6f, 0);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!IsOpen || e is not InputEventKey { Pressed: true, Echo: false }) return;
        if (e.IsActionPressed("ui_cancel")) Close();
        else if (e is InputEventKey { PhysicalKeycode: Key.Enter or Key.KpEnter }) Done();
        else return;
        GetViewport().SetInputAsHandled();
    }

    void Done()
    {
        if (first)
        {
            if (!nameField.Valid) return;
            string name = nameField.Value;
            if (Conn() is { } c && c.Db.Player.PlayerId.Find(MyId())?.Name != name)
            {
                awaitingName = name;
                c.Reducers.SetName(name);
            }
        }
        Conn()?.Reducers.SetLook((byte)look.Skin, (byte)look.Hair, (byte)look.Cut, (byte)(look.Beard ? 1 : 0));
        Close();
    }

    // --- Layout ------------------------------------------------------------

    Control MakePanel()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
        title = Hud.Text("Choose how you look", 26);
        box.AddChild(title);
        // At first join: say why the game is waiting, and that it isn't final.
        welcome = Hud.Text("Welcome. Pick how you look before you set out. Done (or Enter) starts your day; you can change it any time in Options.", 15);
        welcome.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        welcome.CustomMinimumSize = new Vector2(560, 0);
        welcome.Modulate = new Color(1f, 0.9f, 0.72f, 0.9f);
        box.AddChild(welcome);
        // At first join, your name comes first (ui-31).
        nameField = new NameField { Taken = TakenNames };
        nameField.Changed += UpdateDone;
        nameField.Submitted += Done;
        box.AddChild(nameField);

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 26);
        box.AddChild(columns);

        // The figure, lit softly in a little scene of its own.
        var frame = new SubViewportContainer { CustomMinimumSize = new Vector2(240, 360), Stretch = true };
        view = new SubViewport { OwnWorld3D = true, TransparentBg = true, Msaa3D = Viewport.Msaa.Msaa4X, Size = new Vector2I(240, 360) };
        frame.AddChild(view);
        columns.AddChild(frame);
        var camera = new Camera3D { Fov = 28f, Position = new Vector3(0, 1.05f, 4.4f) };
        view.AddChild(camera);
        camera.LookAt(new Vector3(0, 0.92f, 0), Vector3.Up);
        view.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-35, 30, 0), LightEnergy = 1.1f });
        view.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.ClearColor,
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.78f, 0.76f, 0.72f),
                AmbientLightEnergy = 0.65f,
            },
        });

        var choices = new VBoxContainer { CustomMinimumSize = new Vector2(300, 0) };
        choices.AddThemeConstantOverride("separation", 10);
        columns.AddChild(choices);
        choices.AddChild(Swatches("Skin", HumanFigure.Skins, skins, i => look = look with { Skin = i }));
        choices.AddChild(Swatches("Hair", HumanFigure.Hairs, hairs, i => look = look with { Hair = i }));
        choices.AddChild(Words("Cut", Looks.CutNames, cuts, i => look = look with { Cut = i }));
        choices.AddChild(Words("Beard", Looks.BeardNames, beards, i => look = look with { Beard = i != 0 }));
        choices.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        done = ControlsPage.TextButton("Done", Done);
        done.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        choices.AddChild(done);

        var hint = Hud.Text("You can change this later in Options    Enter is Done    Esc keeps your look", 13);
        hint.Modulate = new Color(1, 1, 1, 0.65f);
        box.AddChild(hint);

        var outer = Ui.Window(box);
        return outer;
    }

    // Names other players have, as lower-case keys.
    System.Collections.Generic.ICollection<string> TakenNames() =>
        Conn()?.Db.Player.Iter().Where(p => p.PlayerId != MyId()).Select(p => Names.Key(p.Name)).ToHashSet()
        ?? new System.Collections.Generic.HashSet<string>();

    // Done stays off until the name is good (at first join); the field says why.
    void UpdateDone()
    {
        bool ok = !first || nameField.Valid;
        done.Disabled = !ok;
        done.Modulate = new Color(1, 1, 1, ok ? 1f : 0.4f);
    }

    // A labelled row of round colour swatches.
    Control Swatches(string name, Color[] colours, Button[] into, Action<int> pick)
    {
        var column = new VBoxContainer();
        column.AddChild(Label(name));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        for (int i = 0; i < into.Length && i < colours.Length; i++)
        {
            int at = i;
            var b = new Button { CustomMinimumSize = new Vector2(34, 34), FocusMode = Control.FocusModeEnum.None };
            b.Pressed += () => { pick(at); Show(look); };
            b.SetMeta("colour", colours[i]);
            into[i] = b;
            row.AddChild(b);
        }
        column.AddChild(row);
        return column;
    }

    // A labelled row of word choices.
    Control Words(string name, string[] words, Button[] into, Action<int> pick)
    {
        var column = new VBoxContainer();
        column.AddChild(Label(name));
        var row = new HFlowContainer();
        row.AddThemeConstantOverride("h_separation", 6);
        for (int i = 0; i < words.Length; i++)
        {
            int at = i;
            var b = new Button { Text = words[i], FocusMode = Control.FocusModeEnum.None };
            b.AddThemeFontSizeOverride("font_size", 15);
            b.Pressed += () => { pick(at); Show(look); };
            into[i] = b;
            row.AddChild(b);
        }
        column.AddChild(row);
        return column;
    }

    static Label Label(string text)
    {
        var l = Hud.Text(text, 14);
        l.Modulate = new Color(1, 1, 1, 0.75f);
        return l;
    }

    // --- Contents ----------------------------------------------------------

    void Show(HumanFigure.Appearance a)
    {
        if (figure == null)
        {
            figure = PlayerModel.Build(a);
            view.AddChild(figure);
        }
        else PlayerModel.SetLook(figure, a);

        for (int i = 0; i < skins.Length; i++) Ring(skins[i], i == a.Skin);
        for (int i = 0; i < hairs.Length; i++) Ring(hairs[i], i == a.Hair);
        for (int i = 0; i < cuts.Length; i++) Chosen(cuts[i], i == a.Cut);
        for (int i = 0; i < beards.Length; i++) Chosen(beards[i], i == (a.Beard ? 1 : 0));
    }

    // A swatch: its colour in a round, with a bright ring when chosen.
    static void Ring(Button b, bool chosen)
    {
        var style = new StyleBoxFlat { BgColor = b.GetMeta("colour").AsColor(), BorderColor = new Color(Look.Cream, chosen ? 0.95f : 0.25f) };
        style.SetBorderWidthAll(chosen ? 3 : 1);
        style.SetCornerRadiusAll(16);
        foreach (var state in new[] { "normal", "hover", "pressed" }) b.AddThemeStyleboxOverride(state, style);
    }

    static void Chosen(Button b, bool chosen)
    {
        b.Flat = !chosen;
        b.Modulate = new Color(1, 1, 1, chosen ? 1f : 0.65f);
    }
}
