using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The water's own ripples (atmosphere-31): a small wave simulation on the GPU, a 32 m
// square round the player at 12.5 cm a texel. Anything that disturbs the water (a wader,
// a swimmer, a splash, raindrops) pushes the surface down where it is; the waves spread
// out, bounce back off the shore and die away. WaterLook reads the result for the
// surface's height and slope.
//
// Two small viewports take turns: each step draws the next heights from the other's
// last two (red: now, green: a step ago), so no texture is read while it is written.
// The square follows the player in whole texels, and each step reads the old heights
// shifted by however far it moved, so the waves stay put in the world.
public partial class WaterRipples : Node
{
    public const float Size = 32f;
    const int Res = 256;
    const float Texel = Size / Res;
    // Steps a second; the waves travel at the same speed whatever the frame rate, up to it.
    const float StepRate = 60f;
    const int MaxImpulses = 16;
    // The shore map: a wider square at half a metre a texel, rebuilt as the player moves.
    const float LandSize = 48f;
    const int LandRes = 96;

    static readonly StringName NPrev = "prev", NShift = "shift", NImpulses = "impulses", NCount = "count",
        NOrigin = "origin", NLand = "land", NLandRect = "land_rect";

    readonly SubViewport[] buffers = new SubViewport[2];
    readonly ShaderMaterial[] steps = new ShaderMaterial[2];
    readonly List<Vector4> impulses = new();
    readonly Vector4[] impulseArray = new Vector4[MaxImpulses];
    readonly ImageTexture landTexture = new();
    readonly Image land = Image.CreateEmpty(LandRes, LandRes, false, Image.Format.R8);
    readonly RandomNumberGenerator rng = new();
    int current;
    float clock;
    Vector2 origin, landOrigin = new(float.MaxValue, 0f);
    bool started;

    // The ground height at a point, for the shore map (Atmosphere hands it over; NaN
    // until the terrain is loaded).
    public System.Func<float, float, float>? GroundAt;

    public override void _Ready()
    {
        for (int i = 0; i < 2; i++)
        {
            var view = new SubViewport
            {
                Size = new Vector2I(Res, Res),
                UseHdr2D = true,
                TransparentBg = false,
                Disable3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
                RenderTargetClearMode = SubViewport.ClearMode.Never,
            };
            steps[i] = new ShaderMaterial { Shader = StepShader };
            view.AddChild(new ColorRect { Size = new Vector2(Res, Res), Material = steps[i], Color = new Color(0.5f, 0.5f, 0f) });
            AddChild(view);
            buffers[i] = view;
        }
        land.Fill(Colors.Black);
        landTexture.SetImage(land);
        foreach (var m in steps)
        {
            m.SetShaderParameter(NLand, landTexture);
            m.SetShaderParameter("texel", 1f / Res);
        }
    }

    // Push the water down at a point: radius in metres, strength 0 to 1 (a splash is 1).
    public void Disturb(Vector3 at, float radius, float strength)
    {
        if (impulses.Count >= MaxImpulses || strength <= 0f) return;
        var uv = (new Vector2(at.X, at.Z) - origin) / Size;
        if (uv.X < 0f || uv.Y < 0f || uv.X > 1f || uv.Y > 1f) return;
        impulses.Add(new Vector4(uv.X, uv.Y, Mathf.Max(radius, Texel * 1.5f) / Size, Mathf.Min(strength, 1f) * 0.3f));
    }

    // Each frame from Atmosphere: follow the focus, add the rain's drops, take a step.
    public void Tick(float dt, Vector3 focus, float rain)
    {
        clock += dt;
        if (clock < 1f / StepRate) return;
        clock = Mathf.Min(clock - 1f / StepRate, 1f / StepRate);

        // Follow the player in whole texels.
        var want = new Vector2(Mathf.Snapped(focus.X - Size / 2f, Texel), Mathf.Snapped(focus.Z - Size / 2f, Texel));
        var shift = started ? (want - origin) / Size : Vector2.Zero;
        origin = want;
        if (landOrigin.DistanceTo(origin + Vector2.One * (Size / 2f - LandSize / 2f)) > 6f) RebuildLand();

        // Raindrops: a few small pokes a step, more in a downpour.
        int drops = Mathf.RoundToInt(rain * 5f);
        for (int i = 0; i < drops && impulses.Count < MaxImpulses; i++)
            impulses.Add(new Vector4(rng.Randf(), rng.Randf(), Texel * 1.2f / Size, 0.05f + 0.05f * rain));

        int next = 1 - current;
        var step = steps[next];
        step.SetShaderParameter(NPrev, buffers[current].GetTexture());
        step.SetShaderParameter(NShift, shift);
        step.SetShaderParameter(NOrigin, origin);
        for (int i = 0; i < MaxImpulses; i++) impulseArray[i] = i < impulses.Count ? impulses[i] : Vector4.Zero;
        step.SetShaderParameter(NImpulses, impulseArray);
        step.SetShaderParameter(NCount, impulses.Count);
        impulses.Clear();
        buffers[next].RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        current = next;
        started = true;

        RenderingServer.GlobalShaderParameterSet(WaterLook.Sim, buffers[current].GetTexture());
        RenderingServer.GlobalShaderParameterSet(WaterLook.SimRect, new Vector4(origin.X, origin.Y, Size, 1f));
    }

    // Where the ground stands above the water, the waves stop (and bounce back).
    void RebuildLand()
    {
        // Not until the terrain is there to ask.
        if (GroundAt == null || float.IsNaN(GroundAt(origin.X, origin.Y))) return;
        landOrigin = origin + Vector2.One * (Size / 2f - LandSize / 2f);
        float step = LandSize / LandRes;
        for (int j = 0; j < LandRes; j++)
            for (int i = 0; i < LandRes; i++)
            {
                float x = landOrigin.X + (i + 0.5f) * step, z = landOrigin.Y + (j + 0.5f) * step;
                land.SetPixel(i, j, GroundAt(x, z) > World.WaterLevel - 0.02f ? Colors.White : Colors.Black);
            }
        landTexture.Update(land);
        foreach (var m in steps) m.SetShaderParameter(NLandRect, new Vector4(landOrigin.X, landOrigin.Y, LandSize, 0f));
    }

    static readonly Shader StepShader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded, blend_disabled;

            uniform sampler2D prev : filter_nearest, repeat_disable;
            uniform sampler2D land : filter_linear, repeat_disable;
            uniform vec2 shift;
            uniform vec2 origin;
            uniform vec4 land_rect;
            uniform float texel;
            uniform vec4 impulses[16];
            uniform int count;

            // Heights are stored plus a half, so the surface at rest reads 0.5.
            float height(vec2 uv) {
                if (uv.x < 0.0 || uv.y < 0.0 || uv.x > 1.0 || uv.y > 1.0) return 0.0;
                return texture(prev, uv).r - 0.5;
            }

            void fragment() {
                vec2 uv = UV + shift;
                float now = height(uv);
                float before = (uv.x < 0.0 || uv.y < 0.0 || uv.x > 1.0 || uv.y > 1.0) ? 0.0 : texture(prev, uv).g - 0.5;
                float around = height(uv + vec2(texel, 0.0)) + height(uv - vec2(texel, 0.0))
                             + height(uv + vec2(0.0, texel)) + height(uv - vec2(0.0, texel));
                // The wave equation, a step at a time, losing a little each step.
                float next = (2.0 * now - before + 0.3 * (around - 4.0 * now)) * 0.988;
                for (int i = 0; i < 16; i++) {
                    if (i >= count) break;
                    vec4 p = impulses[i];
                    next -= p.w * (1.0 - smoothstep(0.0, p.z, distance(UV, p.xy)));
                }
                // On land there is no water to move.
                vec2 world = origin + UV * 32.0;
                vec2 luv = (world - land_rect.xy) / land_rect.z;
                if (texture(land, luv).r > 0.5) next = 0.0;
                next = clamp(next, -0.49, 0.49);
                COLOR = vec4(next + 0.5, now + 0.5, 0.0, 1.0);
            }
            """,
    };
}
