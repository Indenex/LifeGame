using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Wading and floating, seen (atmosphere-26). Since bugfix-32 you can walk into ponds
// and float in deep water; now the water answers: soft painted ripple rings spread
// around you while you're in it (faster and brighter when you move), a small splash
// throws up droplets as you step or jump in, and your legs come out darker and a
// little glossy up to the waterline, drying off over about half a minute.
//
// The wet band is drawn by the painted shader (which the player figure uses) from two
// globals: wet_line, the world height it reaches, and wet_amount, how wet (0 to 1).
public partial class Atmosphere
{
    static readonly StringName NWetLine = "wet_line", NWetAmount = "wet_amount", NStir = "stir", NRippleTint = "tint", NRippleAmount = "amount";

    // How long legs take to dry after leaving the water, in seconds.
    const float DryTime = 30f;

    readonly MeshInstance3D ripples = new()
    {
        Mesh = new PlaneMesh { Size = new Vector2(2.6f, 2.6f) },
        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        Visible = false,
        TopLevel = true,
    };
    readonly ShaderMaterial rippleMaterial = new() { Shader = RippleShader };
    readonly CpuParticles3D splash = new()
    {
        Emitting = false,
        OneShot = true,
        Amount = 28,
        Lifetime = 0.9f,
        Explosiveness = 0.92f,
        Direction = Vector3.Up,
        Spread = 38f,
        InitialVelocityMin = 1.6f,
        InitialVelocityMax = 3.2f,
        Gravity = new Vector3(0f, -9.8f, 0f),
        EmissionShape = CpuParticles3D.EmissionShapeEnum.Ring,
        EmissionRingAxis = Vector3.Up,
        EmissionRingRadius = 0.35f,
        EmissionRingInnerRadius = 0.15f,
        EmissionRingHeight = 0.02f,
        ScaleAmountMin = 0.6f,
        ScaleAmountMax = 1.3f,
        TopLevel = true,
        Mesh = new SphereMesh
        {
            Radius = 0.035f,
            Height = 0.07f,
            RadialSegments = 6,
            Rings = 3,
            Material = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = new Color(0.86f, 0.92f, 0.95f, 0.85f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            },
        },
    };

    bool wasInWater;
    float wetOffset, wetAmount, rippleAmount;
    Vector3 waterLastPos;

    void ReadyWater()
    {
        ripples.MaterialOverride = rippleMaterial;
        AddChild(ripples);
        AddChild(splash);
    }

    void UpdateWater(float dt, Vector3 focus, Color air)
    {
        if (Player == null || !Player.IsInsideTree() || terrain == null) return;
        var at = Player.GlobalPosition;
        float speed = dt > 0f ? new Vector2(at.X - waterLastPos.X, at.Z - waterLastPos.Z).Length() / dt : 0f;
        waterLastPos = at;

        float bed = terrain.HeightAt(at.X, at.Z);
        float depth = Wading.Depth(bed);
        // In the water when the surface is above the feet (not while hopping clear of it).
        bool inWater = depth > 0.03f && at.Y < World.WaterLevel - 0.02f;

        // Stepping or dropping in throws up a splash.
        if (inWater && !wasInWater)
        {
            splash.GlobalPosition = new Vector3(at.X, World.WaterLevel + 0.02f, at.Z);
            splash.Restart();
        }
        wasInWater = inWater;

        // Legs are wet up to where the water reached on them, and dry off slowly.
        if (inWater)
        {
            wetOffset = Mathf.Clamp(Mathf.Max(wetOffset * (wetAmount > 0.6f ? 1f : 0f), World.WaterLevel - at.Y), 0f, 1.4f);
            wetAmount = 1f;
        }
        else wetAmount = Mathf.Max(0f, wetAmount - dt / DryTime);
        RenderingServer.GlobalShaderParameterSet(NWetLine, at.Y + wetOffset);
        RenderingServer.GlobalShaderParameterSet(NWetAmount, wetAmount);

        // Ripple rings follow you on the surface, livelier when you move.
        rippleAmount = Mathf.Lerp(rippleAmount, inWater ? 1f : 0f, 1f - Mathf.Exp(-dt * 4f));
        ripples.Visible = rippleAmount > 0.02f;
        if (ripples.Visible)
        {
            ripples.GlobalPosition = new Vector3(at.X, World.WaterLevel + 0.015f, at.Z);
            rippleMaterial.SetShaderParameter(NRippleAmount, rippleAmount);
            rippleMaterial.SetShaderParameter(NStir, Mathf.Clamp(speed / 3f, 0f, 1f));
            rippleMaterial.SetShaderParameter(NRippleTint, air.Lerp(Colors.White, 0.55f));
        }
    }

    // Three painted rings at a time, each widening and fading out; they come faster
    // and stand out more when stirred by walking or swimming.
    static readonly Shader RippleShader = new()
    {
        Code = """
            shader_type spatial;
            render_mode unshaded, blend_mix, depth_draw_never, cull_disabled, shadows_disabled;

            uniform float amount = 0.0;
            uniform float stir = 0.0;
            uniform vec4 tint : source_color = vec4(0.9, 0.95, 1.0, 1.0);

            void fragment() {
                vec2 p = UV * 2.0 - 1.0;
                float r = length(p);
                float a = 0.0;
                for (int i = 0; i < 3; i++) {
                    float phase = fract(TIME * (0.35 + 0.45 * stir) + float(i) / 3.0);
                    float radius = 0.22 + phase * 0.72;
                    float band = 1.0 - smoothstep(0.0, 0.03 + 0.025 * phase, abs(r - radius));
                    a += band * (1.0 - phase);
                }
                ALBEDO = tint.rgb;
                ALPHA = clamp(a, 0.0, 1.0) * amount * (0.4 + 0.3 * stir) * (1.0 - smoothstep(0.8, 1.0, r));
            }
            """,
    };
}
