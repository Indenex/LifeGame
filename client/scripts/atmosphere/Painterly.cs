using Godot;

namespace LifeGame;

// A screen-wide painterly pass (atmosphere-29, prototype for Justin's "more of the
// painted texture look"). Each pixel takes the calmest of four neighbouring patches
// (a Kuwahara filter), so flat areas settle into soft blobs of colour while edges
// stay crisp, like dabs of paint. The patches are turned along a slow brush-noise
// field, so the dabs read as strokes rather than squares. Drawn on a quad in front of
// the camera after everything else in the 3D view; the HUD is untouched.
//
// Off by default until Justin picks (D-154). Command line: --painterly=1 (soft,
// radius 2) or 2 (strong, radius 3), --painterly-mix=0..1 how much of it shows.
// --gpu-bench logs the 3D view's average GPU time every 5 s, to measure its cost.
public partial class Painterly : Node
{
    static readonly StringName NRadius = "radius", NAmount = "amount";

    readonly MeshInstance3D quad = new()
    {
        Mesh = new QuadMesh { Size = new Vector2(2f, 2f) },
        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        // Never culled: it is placed on screen by its shader, not by its transform.
        ExtraCullMargin = 16384f,
        IgnoreOcclusionCulling = true,
    };
    readonly ShaderMaterial material = new() { Shader = PaintShader, RenderPriority = 127 };

    int level;
    bool bench;
    double benchSum;
    int benchFrames;
    float benchClock;

    public override void _Ready()
    {
        level = Mathf.Clamp((int)(ArgFloat("painterly") ?? 0f), 0, 2);
        float amount = Mathf.Clamp(ArgFloat("painterly-mix") ?? 1f, 0f, 1f);
        bench = HasArg("--gpu-bench");
        material.SetShaderParameter(NRadius, level >= 2 ? 3 : 2);
        material.SetShaderParameter(NAmount, amount);
        material.SetShaderParameter("brush", new NoiseTexture2D
        {
            Width = 256,
            Height = 256,
            Seamless = true,
            Noise = new FastNoiseLite { Frequency = 0.012f, FractalOctaves = 2 },
        });
        quad.MaterialOverride = material;
        quad.Visible = level > 0;
        if (bench) RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(), true);
    }

    public override void _Process(double delta)
    {
        if (level > 0 && GetViewport()?.GetCamera3D() is { } camera && quad.GetParent() != camera)
        {
            quad.GetParent()?.RemoveChild(quad);
            camera.AddChild(quad);
        }
        if (!bench) return;
        benchSum += RenderingServer.ViewportGetMeasuredRenderTimeGpu(GetViewport().GetViewportRid());
        benchFrames++;
        benchClock += (float)delta;
        if (benchClock < 5f) return;
        GD.Print($"gpu-bench: painterly={level} gpu={benchSum / Mathf.Max(1, benchFrames):0.00} ms over {benchFrames} frames, fps={Engine.GetFramesPerSecond():0}");
        benchSum = 0;
        benchFrames = 0;
        benchClock = 0f;
    }

    public override void _ExitTree() => quad.QueueFree();

    static bool HasArg(string flag) =>
        System.Array.IndexOf(OS.GetCmdlineUserArgs(), flag) >= 0 || System.Array.IndexOf(OS.GetCmdlineArgs(), flag) >= 0;

    static float? ArgFloat(string name)
    {
        foreach (var args in new[] { OS.GetCmdlineUserArgs(), OS.GetCmdlineArgs() })
            foreach (var arg in args)
                if (arg.StartsWith($"--{name}=") && float.TryParse(arg[(name.Length + 3)..], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v))
                    return v;
        return null;
    }

    static readonly Shader PaintShader = new()
    {
        Code = """
            shader_type spatial;
            render_mode unshaded, depth_test_disabled, depth_draw_never, cull_disabled, fog_disabled, shadows_disabled;

            uniform sampler2D screen : hint_screen_texture, filter_nearest;
            uniform sampler2D brush : repeat_enable, filter_linear;
            uniform int radius = 2;
            uniform float amount = 1.0;

            void vertex() {
                // Cover the whole screen, whatever the camera does.
                POSITION = vec4(VERTEX.xy, 1.0, 1.0);
            }

            void fragment() {
                vec2 px = 1.0 / VIEWPORT_SIZE;
                // The stroke direction drifts slowly across the screen.
                float angle = texture(brush, SCREEN_UV * vec2(VIEWPORT_SIZE.x / VIEWPORT_SIZE.y, 1.0) * 1.5).r * 6.2832;
                mat2 turn = mat2(vec2(cos(angle), sin(angle)), vec2(-sin(angle), cos(angle)));
                // Patches are stretched along the stroke, so the dabs look brushed.
                vec2 stretch = vec2(1.5, 0.75);

                vec3 mean[4];
                vec3 sq[4];
                for (int q = 0; q < 4; q++) { mean[q] = vec3(0.0); sq[q] = vec3(0.0); }
                float n = float((radius + 1) * (radius + 1));
                for (int j = 0; j <= radius; j++) {
                    for (int i = 0; i <= radius; i++) {
                        vec2 o = vec2(float(i), float(j));
                        vec2 offs[4] = vec2[](vec2(-o.x, -o.y), vec2(o.x, -o.y), vec2(-o.x, o.y), vec2(o.x, o.y));
                        for (int q = 0; q < 4; q++) {
                            vec3 c = texture(screen, SCREEN_UV + turn * (offs[q] * stretch) * px).rgb;
                            mean[q] += c;
                            sq[q] += c * c;
                        }
                    }
                }
                vec3 best = texture(screen, SCREEN_UV).rgb;
                vec3 original = best;
                float least = 1e9;
                for (int q = 0; q < 4; q++) {
                    vec3 m = mean[q] / n;
                    vec3 v = abs(sq[q] / n - m * m);
                    float s = v.r + v.g + v.b;
                    if (s < least) { least = s; best = m; }
                }
                ALBEDO = mix(original, best, amount);
                ALPHA = 1.0;
            }
            """,
    };
}
