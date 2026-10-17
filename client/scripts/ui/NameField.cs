using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The name row at character creation (ui-31, Justin: "on character creation, people should
// get to choose their name"): a text field, a die that suggests a name fitting the world,
// and a quiet line saying what's wrong, live as you type. The rules are shared (Names); the
// server checks them again and that nobody else has the name, and its reason shows here too.
public partial class NameField : VBoxContainer
{
    // Names others already have (lower-case keys), for "That name is taken" before asking.
    public Func<ICollection<string>> Taken = () => Array.Empty<string>();
    public event Action? Changed;
    // Enter in the field.
    public event Action? Submitted;

    LineEdit field = null!;
    Label problem = null!;
    string? serverSays;
    readonly Random rng = new();

    public string Value => Names.Normalize(field.Text);
    public string? Problem => Names.Problem(Value) ?? (Taken().Contains(Names.Key(Value)) ? "That name is taken" : null) ?? serverSays;
    public bool Valid => Problem == null;
    public bool Typing => field.HasFocus();

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);
        var label = Hud.Text("Name", 14);
        label.Modulate = new Color(1, 1, 1, 0.75f);
        AddChild(label);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        AddChild(row);
        field = new LineEdit { MaxLength = Names.Max + 4, CustomMinimumSize = new Vector2(320, 40), PlaceholderText = "What should people call you?" };
        field.AddThemeFontSizeOverride("font_size", 18);
        var normal = new StyleBoxFlat { BgColor = new Color(0.05f, 0.05f, 0.04f, 0.9f), BorderColor = new Color(Look.Cream, 0.35f) };
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(6);
        normal.ContentMarginLeft = normal.ContentMarginRight = 10;
        var focus = (StyleBoxFlat)normal.Duplicate();
        focus.BorderColor = new Color(Look.Cream, 0.8f);
        field.AddThemeStyleboxOverride("normal", normal);
        field.AddThemeStyleboxOverride("focus", focus);
        field.TextChanged += _ => { serverSays = null; Refresh(); };
        field.TextSubmitted += _ => Submitted?.Invoke();
        row.AddChild(field);

        // The die: a name that fits the world, one nobody here has.
        var die = new Button { CustomMinimumSize = new Vector2(40, 40), FocusMode = FocusModeEnum.None, TooltipText = "Suggest a name" };
        var dieStyle = new StyleBoxFlat { BgColor = new Color(0.55f, 0.42f, 0.24f, 0.9f) };
        dieStyle.SetCornerRadiusAll(8);
        var dieHover = (StyleBoxFlat)dieStyle.Duplicate();
        dieHover.BgColor = new Color(0.64f, 0.5f, 0.3f, 1f);
        die.AddThemeStyleboxOverride("normal", dieStyle);
        die.AddThemeStyleboxOverride("hover", dieHover);
        die.AddThemeStyleboxOverride("pressed", dieHover);
        var pips = new DieFace { MouseFilter = MouseFilterEnum.Ignore };
        pips.SetAnchorsPreset(LayoutPreset.FullRect);
        die.AddChild(pips);
        die.Pressed += () => { Set(Names.Suggest(rng, Taken())); pips.Roll(); };
        row.AddChild(die);

        problem = Hud.Text("", 13);
        problem.Modulate = new Color(1f, 0.78f, 0.5f);
        AddChild(problem);
    }

    // Starts from the name you have when it's a good one; otherwise a suggestion to keep or change.
    public void Fill(string current)
    {
        string name = Names.Normalize(current);
        Set(Names.Problem(name) == null && !Taken().Contains(Names.Key(name)) ? name : Names.Suggest(rng, Taken()));
    }

    // The server refused the name (taken since, or a rule); shows its reason until you type.
    public void Refused(string reason)
    {
        serverSays = reason;
        Refresh();
    }

    void Set(string name)
    {
        field.Text = name;
        field.CaretColumn = name.Length;
        serverSays = null;
        Refresh();
    }

    void Refresh()
    {
        problem.Text = field.Text.Length == 0 ? "" : Problem ?? "";
        Changed?.Invoke();
    }
}

// Five pips on the die button, with a little shake when rolled.
public partial class DieFace : Control
{
    float shake;

    public void Roll()
    {
        shake = 0.35f;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (shake <= 0f) return;
        shake = Mathf.Max(0f, shake - (float)delta);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var c = Size / 2;
        float r = Mathf.Min(Size.X, Size.Y) * 0.2f, dot = Mathf.Min(Size.X, Size.Y) * 0.07f;
        float tilt = Mathf.Sin(shake * 40f) * shake * 0.6f;
        var turn = new Transform2D(tilt, c);
        foreach (var p in new[] { new Vector2(-r, -r), new Vector2(r, -r), Vector2.Zero, new Vector2(-r, r), new Vector2(r, r) })
            DrawCircle(turn * p, dot, Look.Cream);
    }
}
