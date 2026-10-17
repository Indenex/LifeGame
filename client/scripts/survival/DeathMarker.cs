using Godot;

namespace LifeGame;

// A faint shaft of warm light over the spot where you last died, so you can find your
// way back to what you dropped (survival-33). It fades in slowly and fades away once
// you reach it.
public partial class DeathMarker : Node3D
{
    const string ShaderCode = @"
shader_type spatial;
render_mode unshaded, blend_add, depth_draw_never, cull_disabled, shadows_disabled;
uniform float strength = 1.0;
varying float height;
void vertex() { height = VERTEX.y; }
void fragment() {
    // Brightest near the ground, thinning to nothing at the top; soft at the sides.
    float up = clamp(height / 7.0 + 0.5, 0.0, 1.0);
    float side = pow(clamp(abs(dot(NORMAL, VIEW)), 0.0, 1.0), 1.5);
    float pulse = 0.85 + 0.15 * sin(TIME * 1.3);
    ALBEDO = vec3(1.0, 0.86, 0.6);
    ALPHA = (1.0 - up) * (1.0 - up) * side * 0.22 * pulse * strength;
}";

    // Whether the shaft is shown at all; Justin picked line and light (2026-10-17). The card line has its own switch.
    public const bool Shown = true;
    // You have reached it when this close.
    const float ReachedDistance = 3f;

    readonly ShaderMaterial material = new() { Shader = new Shader { Code = ShaderCode } };
    Node3D? player;
    float strength;
    bool reached;

    public DeathMarker()
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.35f, BottomRadius = 0.35f, Height = 7f, RadialSegments = 16, Rings = 1, CapTop = false, CapBottom = false },
            MaterialOverride = material,
            Position = new Vector3(0, 3.5f, 0),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
        Visible = false;
    }

    public void Show(Vector3 at, Node3D follow)
    {
        if (!Shown) return;
        Position = at;
        player = follow;
        reached = false;
        strength = 0f;
        material.SetShaderParameter("strength", 0f);
        Visible = true;
    }

    public override void _Process(double delta)
    {
        if (!Visible || player == null) return;
        var d = new Vector2(player.Position.X - Position.X, player.Position.Z - Position.Z).Length();
        if (d < ReachedDistance) reached = true;
        strength = Mathf.MoveToward(strength, reached ? 0f : 1f, (float)delta * (reached ? 0.5f : 0.3f));
        material.SetShaderParameter("strength", strength);
        if (reached && strength <= 0f) Visible = false;
    }

    // Which way the spot lies from where you stand, in words: "to the west", or
    // "close by" when it is near enough to see.
    public static string Direction(Vector2 from, Vector2 to)
    {
        var v = to - from;
        if (v.Length() < 8f) return "close by";
        // -Z is north and +X is east.
        float angle = Mathf.RadToDeg(Mathf.Atan2(v.X, -v.Y));
        string[] names = { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };
        int i = (int)Mathf.PosMod(Mathf.Round(angle / 45f), 8f);
        return "to the " + names[i];
    }
}
