using Godot;

namespace LifeGame;

// A screen-wide painterly pass (atmosphere-29, prototype for Justin's "more of the
// painted texture look"). Each pixel takes the calmest of four neighbouring patches
// (a Kuwahara filter), so flat areas settle into soft blobs of colour while edges
// stay crisp, like dabs of paint. The patches are turned along a slow brush-noise
// field, so the dabs read as strokes rather than squares. Drawn on a quad in front of
// the camera after everything else in the 3D view; the HUD is untouched.
//
// Cutesy options (D-313, "cutesy, relaxing, with a hint of realism"): soft toon
// light bands, so light and shade fall into a few gentle steps the way a picture
// book paints them, alone or with a light paint wash.
//
// Off by default until Justin picks (D-154). Command line: --painterly=
//   1 paint dabs, soft (radius 2)      2 paint dabs, strong (radius 3)
//   3 cosy wash: light dabs + soft bands  4 soft bands only (cheapest)
// --painterly-mix=0..1 how much of the dabs show, --painterly-bands=N steps.
// --gpu-bench logs the 3D view's average GPU time every 5 s, to measure its cost.
public partial class Painterly : Node
{
    static readonly StringName NRadius = "radius", NAmount = "amount", NBands = "bands", NBandAmount = "band_amount";

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
        level = Mathf.Clamp((int)(ArgFloat("painterly") ?? 0f), 0, 4);
        float defaultMix = level switch { 3 => 0.55f, 4 => 0f, _ => 1f };
        float amount = Mathf.Clamp(ArgFloat("painterly-mix") ?? defaultMix, 0f, 1f);
        bench = HasArg("--gpu-bench");
        material.SetShaderParameter(NRadius, level == 2 ? 3 : 2);
        material.SetShaderParameter(NAmount, amount);
        // Soft light bands: 5 gentle steps blended half in for the wash, 4 firmer
        // steps for bands only.
        float bands = ArgFloat("painterly-bands") ?? (level == 4 ? 4f : 5f);
        material.SetShaderParameter(NBands, level >= 3 ? bands : 0f);
        material.SetShaderParameter(NBandAmount, level == 4 ? 0.7f : 0.5f);
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
            uniform float bands = 0.0;
            uniform float band_amount = 0.5;

            // Light falls into soft steps: the brightness (compressed, so bright HDR
            // light still lands in a step) snaps toward a few levels with soft edges
            // between them, and the colour keeps its hue.
            vec3 soft_bands(vec3 c) {
                float l = dot(c, vec3(0.2126, 0.7152, 0.0722));
                float t = l / (1.0 + l);
                float x = t * bands;
                float stepped = (floor(x) + smoothstep(0.3, 0.7, fract(x))) / bands;
                // Shade never snaps to black: the darkest step keeps most of its value.
                stepped = max(stepped, t * 0.75);
                float back = stepped / max(1.0 - stepped, 1e-3);
                return c * mix(1.0, back / max(l, 1e-4), band_amount);
            }

            void vertex() {
                // Cover the whole screen, whatever the camera does.
                POSITION = vec4(VERTEX.xy, 1.0, 1.0);
            }

            void fragment() {
                vec3 original = texture(screen, SCREEN_UV).rgb;
                if (amount <= 0.0) {
                    ALBEDO = bands > 0.0 ? soft_bands(original) : original;
                    ALPHA = 1.0;
                    return;
                }
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
                vec3 best = original;
                float least = 1e9;
                for (int q = 0; q < 4; q++) {
                    vec3 m = mean[q] / n;
                    vec3 v = abs(sq[q] / n - m * m);
                    float s = v.r + v.g + v.b;
                    if (s < least) { least = s; best = m; }
                }
                vec3 col = mix(original, best, amount);
                ALBEDO = bands > 0.0 ? soft_bands(col) : col;
                ALPHA = 1.0;
            }
            """,
    };
}
