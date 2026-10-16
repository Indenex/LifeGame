using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Getting wet (survival-5) shown on the body, with no HUD element (D-045): skin
// and leaves darken with a soft sheen, and a faint drip falls while you are wet.
public partial class WetLook : Node3D
{
    const string ShaderCode = @"
shader_type spatial;
render_mode blend_mix, depth_draw_never, cull_back, diffuse_burley, specular_schlick_ggx;
uniform float wetness : hint_range(0.0, 1.0) = 0.0;
void fragment() {
    // Wet surfaces read darker and a little glossy, never mirror-like.
    ALBEDO = vec3(0.02, 0.025, 0.03);
    ROUGHNESS = 0.35;
    SPECULAR = 0.35;
    float rim = pow(1.0 - clamp(dot(NORMAL, VIEW), 0.0, 1.0), 2.0);
    ALPHA = wetness * (0.32 + 0.12 * rim);
}";

    // How far the darkening goes at full soak, and how fast the look follows the
    // server value so it never jumps.
    const float Follow = 0.6f;

    readonly ShaderMaterial overlay = new() { Shader = new Shader { Code = ShaderCode } };
    GpuParticles3D drip = null!;
    float shown;

    public float Wetness { get; set; }

    // The wet look on a body, made the first time it is needed.
    public static WetLook On(Node3D body)
    {
        if (body.GetNodeOrNull<WetLook>(nameof(WetLook)) is { } look) return look;
        look = new WetLook { Name = nameof(WetLook) };
        look.Dress(body);
        body.AddChild(look);
        return look;
    }

    void Dress(Node3D body)
    {
        foreach (var mesh in Meshes(body)) mesh.MaterialOverlay = overlay;

        drip = new GpuParticles3D
        {
            Amount = 24,
            Lifetime = 0.7f,
            Emitting = false,
            Position = new Vector3(0, 1.1f, 0),
            LocalCoords = false,
            ProcessMaterial = new ParticleProcessMaterial
            {
                EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
                EmissionBoxExtents = new Vector3(0.24f, 0.45f, 0.14f),
                Direction = Vector3.Down,
                Spread = 4f,
                InitialVelocityMin = 0.1f,
                InitialVelocityMax = 0.3f,
                Gravity = new Vector3(0, -9f, 0),
                ScaleMin = 0.7f,
                ScaleMax = 1.2f,
            },
            DrawPass1 = new SphereMesh
            {
                Radius = 0.008f,
                Height = 0.03f,
                RadialSegments = 4,
                Rings = 2,
                Material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.8f, 0.88f, 0.95f, 0.45f),
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    Roughness = 0.1f,
                },
            },
        };
        AddChild(drip);
    }

    static IEnumerable<MeshInstance3D> Meshes(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is MeshInstance3D m) yield return m;
            foreach (var deeper in Meshes(child)) yield return deeper;
        }
    }

    public override void _Process(double delta)
    {
        shown = Mathf.Lerp(shown, Mathf.Clamp(Wetness, 0f, 1f), 1f - Mathf.Exp(-Follow * (float)delta));
        overlay.SetShaderParameter("wetness", shown);
        if (drip == null) return;
        // Only a soaked body drips, and gently: a few drops, more the wetter.
        drip.Emitting = shown > 0.15f;
        drip.AmountRatio = Mathf.Clamp((shown - 0.15f) / 0.85f, 0.1f, 1f);
    }
}
