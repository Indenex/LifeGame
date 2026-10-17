using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Choosing how you look (ui-21, D-233; races ui-22, D-249/D-255): a row of races on top,
// then one row per choice of the chosen race (colour swatches or words), with your
// figure turning slowly beside them. The rows come from Looks.Races, so a new race or
// choice needs no code here, only its colours below. It opens by itself a moment after
// you first join, when the server has no look for you yet, and again from Options
// ("Your look"). Done sends SetRaceLook (Modeling's player_look, model-6); the server
// checks it with Looks.Valid, and every client redraws you from the row. Esc keeps the
// look you had. Main sets Conn and MyId.
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
    // The chosen race and its option bytes, in player_look's slot order.
    int race;
    readonly int[] values = new int[Looks.OptionSlots];
    readonly Button[] races = new Button[Looks.Races.Length];
    VBoxContainer options = null!;
    // One row of buttons per option of the shown race.
    Button[][] rows = Array.Empty<Button[]>();
    int rowsFor = -1;
    float spin, firstSeen = -1f;
    // A shell or pattern lives on the back: picking one turns the figure round for a moment.
    float showBack, yaw;
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
        // One centre window at a time (D-283): opening the bag, journal or build menu puts the picker away (Esc
        // semantics: you keep the look you had). It never counts as a covering panel, so it can't trap input.
        Panels.Centre(this, Close);
        // --screenshot-look opens it on a default look, as at first join.
        if (OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--screenshot-look")))
            GetTree().CreateTimer(1.5).Timeout += () => Open(first: true);
    }

    public void Open(bool first)
    {
        // Start from the look you have now: your chosen row, or the one the game gave you.
        uint id = MyId();
        var look = Conn()?.Db.PlayerLook.PlayerId.Find(id) is { } row
            ? HumanFigure.Appearance.From(row.Skin, row.Hair, row.Cut, row.Beard, row.Race)
            : id != 0 ? HumanFigure.Appearance.For(id) : HumanFigure.Appearance.Default;
        race = look.Race < Looks.Races.Length ? look.Race : 0;
        values[0] = look.Skin; values[1] = look.Hair; values[2] = look.Cut; values[3] = look.Beard ? 1 : 0;
        Fit();
        title.Text = first ? "Choose how you look" : "Your look";
        if (!first) Panels.Opening(this);
        welcome.Visible = first;
        this.first = first;
        nameField.Visible = first;
        if (first) nameField.Fill(Conn()?.Db.Player.PlayerId.Find(id)?.Name ?? "");
        UpdateDone();
        panel.Visible = scrim.Visible = true;
        Show();
    }

    public void Close() => panel.Visible = scrim.Visible = false;

    // A new character from the character menu (ui-39): a fresh look and a suggested name; Done
    // hands both to create (CreateCharacter), Esc goes back (cancel). Justin: today's picker
    // "can be used temporarily for new characters".
    Action<string, HumanFigure.Appearance>? creating;
    Action? cancelled;

    public void OpenNew(Action<string, HumanFigure.Appearance> create, Action cancel, string? refused = null)
    {
        creating = create;
        cancelled = cancel;
        Open(first: true);
        title.Text = "Someone new";
        // A fresh start: the first race, its first choices.
        race = 0;
        Array.Clear(values);
        Fit();
        if (refused == null) nameField.Fill("");
        else nameField.Refused(refused);
        Show();
        UpdateDone();
    }

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
                // Once only (bugfix-41): a character that never chose a look is not asked again every session.
                if (c.Db.PlayerLook.PlayerId.Find(MyId()) == null && !Settings.Get("look", "asked", false))
                {
                    Settings.Set("look", "asked", true);
                    Open(first: true);
                }
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
        float dt = (float)delta;
        showBack = Mathf.Max(0f, showBack - dt);
        if (showBack <= 0f) spin += dt * 0.35f;
        float want = showBack > 0f ? Mathf.Pi : Mathf.Sin(spin) * 0.6f;
        yaw = Mathf.Lerp(yaw, want, 1f - Mathf.Exp(-dt * 6f));
        figure.Rotation = new Vector3(0, yaw, 0);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!IsOpen || e is not InputEventKey { Pressed: true, Echo: false }) return;
        if (e.IsActionPressed("ui_cancel"))
        {
            Close();
            var back = cancelled;
            creating = null;
            cancelled = null;
            back?.Invoke();
        }
        else if (e is InputEventKey { PhysicalKeycode: Key.Enter or Key.KpEnter }) Done();
        else return;
        GetViewport().SetInputAsHandled();
    }

    void Done()
    {
        if (creating is { } create)
        {
            if (!nameField.Valid) return;
            creating = null;
            cancelled = null;
            Close();
            create(nameField.Value, Appearance);
            return;
        }
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
        Conn()?.Reducers.SetRaceLook((byte)race, (byte)values[0], (byte)values[1], (byte)values[2], (byte)values[3]);
        Close();
    }

    // Keeps every option byte one the race has; a slot it doesn't use is 0 (as Looks.Valid wants).
    void Fit()
    {
        var opts = Looks.Races[race].Options;
        for (int i = 0; i < values.Length; i++)
            values[i] = i < opts.Length ? Math.Clamp(values[i], 0, opts[i].Count - 1) : 0;
    }

    HumanFigure.Appearance Appearance => new(values[0], values[1], values[2], values[3] != 0, race);

    // The colours of a colour option; Modeling keeps them with the figure (ChibiFigure).
    // A race added to Looks.Races gets a line here; until then its swatches are shades of grey.
    static Color[] Colours(int race, int option) => (Looks.Races[race].Name, option) switch
    {
        ("Bramble", 0) => ChibiFigure.Skins,
        ("Bramble", 1) => ChibiFigure.Hairs,
        ("Pebble", 0) => ChibiFigure.PebbleSkins,
        ("Pebble", 1) => ChibiFigure.Shells,
        _ => Enumerable.Range(0, Looks.Races[race].Options[option].Count)
            .Select(i => new Color(0.3f + 0.12f * i, 0.3f + 0.12f * i, 0.3f + 0.12f * i)).ToArray(),
    };

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
        // Framed for the 1.35 m race figures. Not in the tree yet, so aim it from a position
        // rather than with LookAt.
        var camera = new Camera3D { Fov = 28f };
        view.AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 0.82f, 3.5f), new Vector3(0, 0.68f, 0), Vector3.Up);
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
        // The races, as words along the top; the rows below follow the one chosen.
        if (Looks.Races.Length > 1)
            choices.AddChild(Words("Race", Looks.Races.Select(r => r.Name).ToArray(), races, i => { race = i; Fit(); }));
        options = new VBoxContainer();
        options.AddThemeConstantOverride("separation", 10);
        choices.AddChild(options);
        choices.AddChild(new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        done = Ui.MainButton("Done", Done);
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
            WarmLook.KeepColours(b);
            b.Pressed += () => { pick(at); Show(); };
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
            b.Pressed += () => { pick(at); Show(); };
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

    void Show()
    {
        var a = Appearance;
        // The picker always shows the race figures (D-249), whichever body the world uses yet.
        if (figure != null) { view.RemoveChild(figure); figure.QueueFree(); }
        figure = ChibiFigure.Create(PlayerModel.ChibiLook(a));
        view.AddChild(figure);

        if (rowsFor != race) Rebuild();
        for (int i = 0; i < races.Length; i++) if (races[i] != null) Chosen(races[i], i == race);
        var opts = Looks.Races[race].Options;
        for (int o = 0; o < rows.Length; o++)
            for (int i = 0; i < rows[o].Length; i++)
                if (opts[o].Names == null) Ring(rows[o][i], i == values[o]);
                else Chosen(rows[o][i], i == values[o]);
    }

    // One row per option of the chosen race: swatches for colours, words for parts.
    void Rebuild()
    {
        foreach (var child in options.GetChildren()) { options.RemoveChild(child); child.QueueFree(); }
        var opts = Looks.Races[race].Options;
        rows = new Button[opts.Length][];
        for (int o = 0; o < opts.Length; o++)
        {
            int slot = o;
            rows[o] = new Button[opts[o].Count];
            bool onBack = opts[o].Name is "Shell" or "Pattern";
            Action<int> pick = i =>
            {
                values[slot] = i;
                if (onBack) showBack = 2.2f;
            };
            options.AddChild(opts[o].Names is { } names
                ? Words(opts[o].Name, names, rows[o], pick)
                : Swatches(opts[o].Name, Colours(race, o), rows[o], pick));
        }
        rowsFor = race;
    }

    // A swatch: its colour in a round, with a bright ring when chosen.
    static void Ring(Button b, bool chosen)
    {
        var style = new StyleBoxFlat { BgColor = b.GetMeta("colour").AsColor(), BorderColor = WarmLook.RingColour(chosen ? 0.95f : 0.25f) };
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
