using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// What people nearby have said, in the lower left. Lines drift out after a while;
// opening the chat box brings the recent ones back.
public partial class ChatPanel : CanvasLayer
{
    const int MaxLines = 6;
    const float LineLife = 18f;

    readonly VBoxContainer lines = new();
    readonly LineEdit input = new();
    readonly List<(Control node, float age)> entries = new();

    public event Action<string>? Sent;
    public bool Typing => input.HasFocus();

    public override void _Ready()
    {
        Layer = 2;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 6);
        column.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        column.GrowVertical = Control.GrowDirection.Begin;
        column.Position = new Vector2(20, -112);
        column.CustomMinimumSize = new Vector2(440, 0);
        root.AddChild(column);

        lines.AddThemeConstantOverride("separation", 4);
        lines.MouseFilter = Control.MouseFilterEnum.Ignore;
        column.AddChild(lines);

        input.PlaceholderText = "Say something   (Enter to send, Esc to close, /name to rename)";
        input.MaxLength = LifeGame.Shared.Social.ChatMaxLength;
        input.AddThemeFontSizeOverride("font_size", 15);
        input.AddThemeColorOverride("font_color", Look.Cream);
        input.AddThemeColorOverride("font_placeholder_color", new Color(Look.Cream, 0.45f));
        input.AddThemeColorOverride("caret_color", Look.Cream);
        var box = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.11f, 0.09f, 0.55f),
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
        box.SetCornerRadiusAll(12);
        input.AddThemeStyleboxOverride("normal", box);
        input.AddThemeStyleboxOverride("focus", box);
        input.Visible = false;
        input.TextSubmitted += Submit;
        input.GuiInput += e =>
        {
            if (e.IsActionPressed("ui_cancel"))
            {
                Close();
                GetViewport().SetInputAsHandled();
            }
        };
        column.AddChild(input);
    }

    public void Open()
    {
        input.Visible = true;
        input.GrabFocus();
        // Bring back what was said recently while the box is open.
        for (int i = 0; i < entries.Count; i++)
            entries[i] = (entries[i].node, Mathf.Min(entries[i].age, LineLife - 6f));
    }

    void Close()
    {
        input.Text = "";
        input.ReleaseFocus();
        input.Visible = false;
    }

    void Submit(string text)
    {
        text = text.Trim();
        if (text.Length > 0) Sent?.Invoke(text);
        Close();
    }

    public void Add(string who, string text, bool mine)
    {
        var label = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(440, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        string nameColor = mine ? "#e8d6a8" : "#cfe0b4";
        label.Text = $"[color={nameColor}]{Escape(who)}[/color]  {Escape(text)}";
        label.AddThemeFontSizeOverride("normal_font_size", 15);
        label.AddThemeColorOverride("default_color", Look.Cream);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.45f));
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        var panel = Hud.Panel(label, Vector2.Zero);
        panel.Modulate = new Color(1, 1, 1, 0);
        lines.AddChild(panel);
        entries.Add((panel, 0f));
        while (entries.Count > MaxLines)
        {
            entries[0].node.QueueFree();
            entries.RemoveAt(0);
        }
    }

    static string Escape(string s) => s.Replace("[", "[lb]");

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        for (int i = 0; i < entries.Count; i++)
        {
            var (node, age) = entries[i];
            if (!Typing) age += dt;
            entries[i] = (node, age);
            // Ease in over a third of a second, linger, then fade over the last few.
            float alpha = Mathf.Min(age / 0.35f, 1f) * Mathf.Clamp((LineLife - age) / 4f, 0f, 1f);
            if (Typing) alpha = Mathf.Max(alpha, 0.9f);
            node.Modulate = new Color(1, 1, 1, alpha);
        }
    }
}
