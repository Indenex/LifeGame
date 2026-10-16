using System;
using Godot;

namespace LifeGame;

// Runs the G key: asks the registry what G would do, shows it as a prompt
// ("G  Pick berries") and does it on press, or every Repeat seconds while held.
public partial class UseKey : CanvasLayer
{
    // Where the player stands; set by Main.
    public Func<Vector3> Player = () => Vector3.Zero;
    // Which way the player faces (flat); set by Main so G picks what's in front.
    public Func<Vector3> Facing = () => Vector3.Zero;
    // True while a panel, minigame or placement mode owns the keys.
    public Func<bool> Blocked = () => false;

    readonly Label verb = new();
    Label keyLabel = null!;
    Control panel = null!;
    UseOffer? offer;
    float repeatTimer, alpha;

    public override void _Ready()
    {
        Layer = 2;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 12);
        row.AddChild(KeyCap("G", out keyLabel));
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

        // --screenshot-use="Pick berries" shows a prompt without a server, for screenshots.
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--screenshot-use="))
                Use.Register(new UseHandler { Id = "ui.preview", Find = _ => new UseOffer(arg[17..], 0f, () => { }) });
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        offer = Blocked() ? null : Use.Best(Player(), Facing());
        if (offer != null)
        {
            verb.Text = offer.Prompt;
            // The player's own Use key, if they moved it.
            keyLabel.Text = Controls.KeyFor("use") ?? "G";
        }

        // Fade rather than pop, so walking past things stays calm.
        alpha = Mathf.MoveToward(alpha, offer != null ? 1f : 0f, dt * 6f);
        panel.Modulate = new Color(1, 1, 1, alpha);

        if (offer is { Repeat: > 0f } held && Input.IsActionPressed("use"))
        {
            repeatTimer -= dt;
            if (repeatTimer <= 0f)
            {
                repeatTimer = held.Repeat;
                held.Use();
            }
        }
        else repeatTimer = 0f;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // Hold actions run from _Process; a tap only fires one-shot offers.
        if (!e.IsActionPressed("use") || e.IsEcho() || offer is not { Repeat: <= 0f } tap) return;
        tap.Use();
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
