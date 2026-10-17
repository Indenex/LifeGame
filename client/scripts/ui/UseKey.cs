using System;
using Godot;

namespace LifeGame;

// Runs the G key: asks the registry what G would do, shows it as a prompt
// ("E  Pick berries") and does it on press, or every Repeat seconds while held.
public partial class UseKey : CanvasLayer
{
    // Cached names: a string here would build a new StringName every frame.
    static readonly StringName NUse = "use";

    // Where the player stands; set by Main.
    public Func<Vector3> Player = () => Vector3.Zero;
    // Which way the player faces (flat); set by Main so G picks what's in front.
    public Func<Vector3> Facing = () => Vector3.Zero;
    // True while a panel, minigame or placement mode owns the keys.
    public Func<bool> Blocked = () => false;

    readonly Label verb = new();
    // A dimmer second line: what else G could do ("also: Chop tree"), or a hint when it can do nothing.
    readonly Label second = new();
    float hintLeft, secondAlpha;
    string hintText = "";
    Label keyLabel = null!;
    Control panel = null!;
    UseOffer? offer;
    float repeatTimer, alpha;
    readonly HoldRepeat hold = new();
    string? holdPrompt;
    // The key cap's look, so it can glow while a held key repeats (HoldRepeat.Active).
    StyleBoxFlat capStyle = null!;
    float holdGlow, holdClock;

    public override void _Ready()
    {
        Layer = 2;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 12);
        var cap = KeyCap("E", out keyLabel);
        capStyle = (StyleBoxFlat)cap.GetThemeStylebox("panel");
        row.AddChild(cap);
        Hud.Soft(verb, 18, 1f);
        verb.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(verb);

        // No box behind it: just the key and the words (D-045).
        panel = row;
        panel.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Begin;
        // Above the survival and building prompts, which go away as lanes move to G.
        panel.Position = new Vector2(0, -214);
        panel.Modulate = new Color(1, 1, 1, 0);
        root.AddChild(panel);

        Hud.Soft(second, 14, 0.8f);
        second.HorizontalAlignment = HorizontalAlignment.Center;
        second.MouseFilter = Control.MouseFilterEnum.Ignore;
        second.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        second.GrowHorizontal = Control.GrowDirection.Both;
        second.GrowVertical = Control.GrowDirection.Begin;
        second.Position = new Vector2(0, -186);
        second.Modulate = new Color(1, 1, 1, 0);
        root.AddChild(second);

        // --screenshot-use="Pick berries" shows a prompt without a server, for screenshots.
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--screenshot-use="))
                Use.Register(new UseHandler { Id = "ui.preview", Find = _ => new UseOffer(arg[17..], 0f, () => { }) });
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        offer = Use.Shown = Blocked() ? null : Use.Best(Player(), Facing());
        if (offer != null)
        {
            verb.Text = offer.Prompt;
            // The player's own Use key, if they moved it.
            keyLabel.Text = Controls.KeyFor("use") ?? "E";
        }

        // The second line: the runner-up offer, or (with no offer) now and then a hint.
        string secondText = "";
        if (offer != null)
        {
            hintLeft = 0f;
            var others = Use.Others(Player(), Facing(), offer);
            if (others.Count > 0) secondText = "also: " + others[0].Prompt;
        }
        else if (!Blocked())
        {
            if (hintLeft > 0f) hintLeft -= dt;
            else if (Use.Hint(Player(), Time.GetTicksMsec() / 1000.0) is { } found) { hintText = found; hintLeft = Use.HintShown; }
            if (hintLeft > 0f) secondText = hintText;
        }
        if (secondText != "") second.Text = secondText;
        secondAlpha = Mathf.MoveToward(secondAlpha, secondText != "" ? 1f : 0f, dt * 4f);
        second.Modulate = new Color(1, 1, 1, secondAlpha);

        // While a held key repeats (Use, dig or pick up), the cap's rim and glow pulse warm, so
        // you can see it will keep going; they ease back when it stops.
        holdGlow = Mathf.MoveToward(holdGlow, HoldRepeat.Active ? 1f : 0f, dt * 5f);
        holdClock += dt;
        float pulse = holdGlow * (0.6f + 0.4f * Mathf.Sin(holdClock * 6f));
        capStyle.BorderColor = new Color(Look.Cream, 0.8f).Lerp(new Color(1f, 0.82f, 0.5f, 1f), pulse);
        capStyle.ShadowColor = new Color(0, 0, 0, 0.25f).Lerp(new Color(1f, 0.75f, 0.35f, 0.45f), pulse);
        capStyle.ShadowSize = 5 + (int)(4 * pulse);

        // Fade rather than pop, so walking past things stays calm.
        alpha = Mathf.MoveToward(alpha, offer != null ? 1f : 0f, dt * 6f);
        panel.Modulate = new Color(1, 1, 1, alpha);

        if (offer is { Repeat: > 0f } held && Input.IsActionPressed(NUse))
        {
            repeatTimer -= dt;
            if (repeatTimer <= 0f)
            {
                repeatTimer = held.Repeat;
                held.Use();
            }
        }
        else repeatTimer = 0f;

        // Hold the key to repeat a repeatable action (Justin). Only the same action again: if
        // what is offered changes (the bush is bare and a pond is next), the hold ends.
        bool down = Input.IsActionPressed(NUse);
        if (!down) holdPrompt = null;
        if (offer is { Again: > 0f } again && holdPrompt == again.Prompt && !Blocked()
            && hold.Update(down, dt, again.Again, Use.Busy(), Player()))
        {
            hold.Acted(Player());
            again.Use();
        }
        else if (down && holdPrompt != null && (offer == null || offer.Prompt != holdPrompt))
            hold.Update(false, dt, 0f, false, Player());
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // Hold actions run from _Process; a tap only fires one-shot offers. Any offer
        // keeps the key, so it never also picks up what lies at your feet (ui-17: with
        // nothing to use, the same key falls through to Main's pick-up).
        if (!e.IsActionPressed("use") || offer == null) return;
        if (!e.IsEcho() && offer.Repeat <= 0f)
        {
            if (offer.Again > 0f)
            {
                hold.Pressed(Player());
                holdPrompt = offer.Prompt;
            }
            offer.Use();
        }
        GetViewport().SetInputAsHandled();
    }

    // A small rounded key, like a keyboard cap.
    internal static Control KeyCap(string key, out Label label)
    {
        label = Hud.Text(key, 16);
        Hud.Soft(label, 16, 1f);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.CustomMinimumSize = new Vector2(18, 0);
        var cap = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.10f, 0.07f, 0.35f),
            BorderColor = new Color(Look.Cream, 0.8f),
            ShadowColor = new Color(0, 0, 0, 0.25f),
            ShadowSize = 5,
            ShadowOffset = new Vector2(0, 2),
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 1,
            ContentMarginBottom = 3,
        };
        style.SetBorderWidthAll(1);
        style.BorderWidthBottom = 3;
        style.SetCornerRadiusAll(6);
        cap.AddThemeStyleboxOverride("panel", style);
        cap.AddChild(label);
        return cap;
    }
}
