using System;
using Godot;

namespace LifeGame;

// Steady, soft sun shadows (atmos-shadows-1, after the Unity spike, unity-1; cosy
// look D-313: soft, rounded, no harsh edges, never shimmering).
//
// - The four cascades are fitted to the follow camera: the first ends just past the
//   player, so the player and the ground round them get the sharpest shadow map
//   (Godot's default splits left them in the third). Splits are refitted only when
//   the zoom settles somewhere new, so they never creep while walking.
// - Neighbouring cascades blend, so there is no visible seam where one hands over.
// - The sun's shadow direction moves in small steps instead of every frame. Godot
//   already snaps the cascades to whole texels as the camera moves; a sun that turns
//   a little every frame defeats that and makes every edge crawl.
// - A wider, softer penumbra (D-313): shadow edges round off gently instead of
//   cutting hard; the shade's colour comes from Atmosphere's ambient.
// - Depth and normal bias a little under Godot's defaults: shadows stay attached
//   to the feet of trees and the player, with margin against acne where the
//   painted shader's wrapped light reaches past the terminator.
// - Shadows quality (Options page): Low, Medium, High. One setting picks the atlas size,
//   the cascades, the reach and the softness filter.
public partial class SunShadows : Node
{
    public enum Level { Low, Medium, High }
    public static readonly string[] Names = { "Low", "Medium", "High" };

    public static Level Setting
    {
        get => Override ?? (Level)Math.Clamp(Settings.Get("graphics", "shadows", (int)Level.Medium), 0, Names.Length - 1);
        set
        {
            Settings.Set("graphics", "shadows", (int)value);
            Changed?.Invoke();
        }
    }

    static event Action? Changed;

    // --shadows=low|medium|high on the command line wins over the saved setting (renders, benchmarks).
    static readonly Level? Override = ReadOverride();

    static Level? ReadOverride()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--shadows=") && Enum.TryParse<Level>(arg["--shadows=".Length..], true, out var level))
                return level;
        return null;
    }

    // What each level costs: shadow atlas size (px, shared by the cascades), cascade
    // count, reach (m), Godot's soft-shadow filter, and how far (m) past the player the
    // first, second and third cascades end. Bigger cascades hold more shadow casters,
    // so the near ones stay tight on Medium to keep its draw calls near the old ones.
    readonly record struct Tier(int Atlas, int Splits, float Reach, RenderingServer.ShadowQuality Filter,
        float FirstPast, float SecondPast, float ThirdPast);
    static readonly Tier[] Tiers =
    {
        new(2048, 2, 40f, RenderingServer.ShadowQuality.SoftLow, 8f, 8f, 8f),
        new(4096, 4, 50f, RenderingServer.ShadowQuality.SoftMedium, 2f, 8f, 20f),
        new(8192, 4, 70f, RenderingServer.ShadowQuality.SoftHigh, 5f, 16f, 34f),
    };
    // The sun's shadow direction steps by this much (degrees). At the normal day
    // length that is one small step about every 1.5 s; a 10 m tree's shadow tip moves
    // under 2 cm, within one texel of the nearest cascade.
    public const float SunStepDegrees = 0.1f;
    // The sun's apparent size (degrees), which sets how wide the soft shadow edge is.
    const float PenumbraDegrees = 1.1f;
    // Splits refit once the zoom settles this many metres from where they were fitted.
    const float RefitMetres = 1.5f;

    readonly DirectionalLight3D sun;
    float fittedFor = -1f;
    Level applied = (Level)(-1);

    public SunShadows(DirectionalLight3D sun) => this.sun = sun;

    public override void _Ready()
    {
        Changed += OnChanged;
        sun.ShadowEnabled = true;
        sun.DirectionalShadowBlendSplits = true;
        sun.DirectionalShadowFadeStart = 0.85f;
        // Godot's defaults (0.1 depth, 2.0 normal) float the shadows off the feet of
        // trees. Lower holds them on; not much lower, or the wrapped light past the
        // terminator ((N.L + 0.3) / 1.3) shows acne at a low sun (Atmosphere).
        sun.ShadowBias = 0.05f;
        sun.ShadowNormalBias = 1.5f;
        // Soft, rounded edges for the cosy look (D-313): a wider sun disc for the
        // penumbra and a little extra blur.
        sun.LightAngularDistance = PenumbraDegrees;
        sun.ShadowBlur = 1.5f;
        ApplyLevel();
    }

    public override void _ExitTree() => Changed -= OnChanged;

    void OnChanged() => ApplyLevel();

    void ApplyLevel()
    {
        var level = Setting;
        if (level == applied) return;
        applied = level;
        var tier = Tiers[(int)level];
        RenderingServer.DirectionalShadowAtlasSetSize(tier.Atlas, true);
        RenderingServer.DirectionalSoftShadowFilterSetQuality(tier.Filter);
        sun.DirectionalShadowMode = tier.Splits == 4
            ? DirectionalLight3D.ShadowMode.Parallel4Splits
            : DirectionalLight3D.ShadowMode.Parallel2Splits;
        sun.DirectionalShadowMaxDistance = tier.Reach;
        fittedFor = -1f;
    }

    public override void _Process(double delta)
    {
        // The follow camera sits Camera.Position.Z metres behind the player (CameraRig).
        if (GetViewport().GetCamera3D() is not { } camera) return;
        float toPlayer = camera.GetParent() is CameraRig ? camera.Position.Z : 16f;
        if (fittedFor >= 0f && Mathf.Abs(toPlayer - fittedFor) < RefitMetres) return;
        fittedFor = toPlayer;
        var tier = Tiers[(int)applied];
        float reach = tier.Reach;
        float Split(float past, float min) => Mathf.Clamp((toPlayer + past) / reach, min, 0.95f);
        float s1 = Split(tier.FirstPast, 0.08f);
        float s2 = Mathf.Max(Split(tier.SecondPast, 0.2f), s1 + 0.05f);
        float s3 = Mathf.Max(Split(tier.ThirdPast, 0.4f), s2 + 0.05f);
        sun.DirectionalShadowSplit1 = s1;
        sun.DirectionalShadowSplit2 = s2;
        sun.DirectionalShadowSplit3 = s3;
    }

    // The sun's rotation (degrees: elevation, azimuth) snapped to SunStepDegrees, so
    // the shadow map's texel grid holds still between steps.
    public static Vector3 Steady(float elevationDegrees, float azimuthDegrees) => new(
        Mathf.DegToRad(-Mathf.Snapped(elevationDegrees, SunStepDegrees)),
        Mathf.DegToRad(Mathf.Snapped(azimuthDegrees, SunStepDegrees)),
        0f);
}
