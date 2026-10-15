using System.Collections.Generic;
using Godot;

namespace LifeGame;

// A quiet overlay: connection state, what you carry, controls, and short notes
// that fade out on their own.
public partial class Hud : CanvasLayer
{
    readonly Label status = new();
    readonly HBoxContainer inventory = new();
    readonly Label toast = new();
    float toastTime;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);

        var title = Panel(Text("LifeGame", 22), new Vector2(20, 18));
        root.AddChild(title);
        var statusPanel = Panel(status, new Vector2(20, 66));
        Style(status, 15, 0.85f);
        root.AddChild(statusPanel);

        var controls = Text("WASD walk    Q/E turn    scroll zoom    F dig    Space pick up    Enter talk    C claim land    R marker", 14);
        controls.Modulate = new Color(1, 1, 1, 0.7f);
        var controlsPanel = Panel(controls, Vector2.Zero);
        controlsPanel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        controlsPanel.Position = new Vector2(20, -56);
        root.AddChild(controlsPanel);

        inventory.AddThemeConstantOverride("separation", 10);
        inventory.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        inventory.GrowHorizontal = Control.GrowDirection.Both;
        inventory.Position = new Vector2(0, -64);
        root.AddChild(inventory);

        Style(toast, 18, 1f);
        toast.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        toast.GrowHorizontal = Control.GrowDirection.Both;
        toast.HorizontalAlignment = HorizontalAlignment.Center;
        toast.Position = new Vector2(0, 40);
        root.AddChild(toast);

        SetInventory(new Dictionary<string, uint>());
    }

    public void SetStatus(string text) => status.Text = text;

    public void Note(string text)
    {
        toast.Text = text;
        toastTime = 2.5f;
    }

    public void SetInventory(IReadOnlyDictionary<string, uint> items)
    {
        foreach (var child in inventory.GetChildren()) child.QueueFree();
        if (items.Count == 0)
        {
            var empty = Text("Empty hands", 15);
            empty.Modulate = new Color(1, 1, 1, 0.6f);
            inventory.AddChild(Panel(empty, Vector2.Zero));
            return;
        }
        foreach (var (name, count) in items)
            inventory.AddChild(Panel(Text($"{name}  {count}", 16), Vector2.Zero));
    }

    public override void _Process(double delta)
    {
        toastTime -= (float)delta;
        toast.Modulate = new Color(1, 1, 1, Mathf.Clamp(toastTime, 0f, 1f));
    }

    internal static Label Text(string text, int size)
    {
        var label = new Label { Text = text };
        Style(label, size, 1f);
        return label;
    }

    internal static void Style(Label label, int size, float alpha)
    {
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color(Look.Cream, alpha));
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.35f));
        label.AddThemeConstantOverride("shadow_offset_y", 1);
    }

    internal static PanelContainer Panel(Control content, Vector2 position)
    {
        var panel = new PanelContainer { Position = position, MouseFilter = Control.MouseFilterEnum.Ignore };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.11f, 0.09f, 0.42f),
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 7,
            ContentMarginBottom = 7,
        };
        style.SetCornerRadiusAll(12);
        panel.AddThemeStyleboxOverride("panel", style);
        panel.AddChild(content);
        return panel;
    }
}
