using Godot;

namespace LifeGame;

// Hold F1 to see every world key. Built from Controls.World each time it
// opens, so it shows the keys as they really are; off screen the rest of the time
// (D-038).
public partial class KeyHelp : CanvasLayer
{
    // Cached names: a string here would build a new StringName every frame.
    static readonly StringName NKeyHelp = "key_help";

    readonly GridContainer grid = new() { Columns = 2, MouseFilter = Control.MouseFilterEnum.Ignore };
    Control root = null!;
    float alpha;
    bool open;

    // Kept open from the Esc menu's Keys entry until Esc.
    public bool Pinned;

    public override void _Ready()
    {
        Layer = 3;
        root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.Modulate = new Color(1, 1, 1, 0);
        AddChild(root);

        // A soft darkening toward the middle rather than a box.
        var shade = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        shade.Material = new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = """
                    shader_type canvas_item;
                    void fragment() {
                        vec2 uv = (SCREEN_UV - 0.5) * vec2(1.6, 1.0);
                        COLOR = vec4(0.03, 0.04, 0.03, 0.62 * (1.0 - smoothstep(0.1, 0.75, length(uv))));
                    }
                    """,
            },
        };
        root.AddChild(shade);

        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        column.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        column.AddThemeConstantOverride("separation", 18);
        root.AddChild(column);

        var title = new Label { Text = "Keys", HorizontalAlignment = HorizontalAlignment.Center };
        Hud.Soft(title, 22, 0.95f);
        column.AddChild(title);

        grid.AddThemeConstantOverride("h_separation", 28);
        grid.AddThemeConstantOverride("v_separation", 8);
        var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        center.AddChild(grid);
        column.AddChild(center);

        var footer = new Label { Text = "Panels and building show their own keys while open.  Esc for the menu.", HorizontalAlignment = HorizontalAlignment.Center };
        Hud.Soft(footer, 14, 0.6f);
        column.AddChild(footer);

        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshot-keys") >= 0)
            GetTree().CreateTimer(1).Timeout += () => { Fill(); open = true; };
    }

    public override void _Process(double delta)
    {
        bool held = Pinned || InputMap.HasAction("key_help") && Input.IsActionPressed(NKeyHelp);
        if (held && !open) Fill();
        open = held || open && System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshot-keys") >= 0;
        alpha = Mathf.MoveToward(alpha, open ? 1f : 0f, (float)delta * 7f);
        root.Modulate = new Color(1, 1, 1, alpha);
        root.Visible = alpha > 0f;
    }

    void Fill()
    {
        foreach (var child in grid.GetChildren())
        {
            grid.RemoveChild(child);
            child.QueueFree();
        }
        foreach (var b in Controls.World)
        {
            if (b.Help == null || Controls.KeysFor(b) is not { } keys) continue;
            var key = new Label { Text = keys, HorizontalAlignment = HorizontalAlignment.Right };
            Hud.Soft(key, 17, 1f);
            var what = new Label { Text = b.Help };
            Hud.Soft(what, 17, 0.8f);
            grid.AddChild(key);
            grid.AddChild(what);
        }
        // Rows other lanes add, such as admin chat commands.
        foreach (var (keys, help) in Extra?.Invoke() ?? System.Array.Empty<(string, string)>())
        {
            var key = new Label { Text = keys, HorizontalAlignment = HorizontalAlignment.Right };
            Hud.Soft(key, 17, 1f);
            var what = new Label { Text = help };
            Hud.Soft(what, 17, 0.8f);
            grid.AddChild(key);
            grid.AddChild(what);
        }
    }

    public static System.Func<System.Collections.Generic.IEnumerable<(string Keys, string Help)>>? Extra;
}
