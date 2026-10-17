using System;
using Godot;

namespace LifeGame;

// Steadier, crisper sun shadows (atmos-shadows-1, after the Unity spike, unity-1).
//
// - The four cascades are fitted to the follow camera: the first ends just past the
//   player, so the player and the ground round them get the sharpest shadow map
//   (Godot's default splits left them in the third). Splits are refitted only when
//   the zoom settles somewhere new, so they never creep while walking.
// - Neighbouring cascades blend, so there is no visible seam where one hands over.
// - The sun's shadow direction moves in small steps instead of every frame. Godot
//   already snaps the cascades to whole texels as the camera moves; a sun that turns
//   a little every frame defeats that and makes every edge crawl.
// - Lower depth bias and a moderate normal bias: no acne on the painted surfaces,
//   and shadows that stay attached to the feet of trees and the player.
// - Shadows quality (Options page): Low, Medium, High. One setting picks the atlas size,
//   the cascades, the reach and the softness filter.
public partial class SunShadows : Node
{
    public enum Level { Low, Medium, High }
    public static readonly string[] Names = { "Low", "Medium", "High" };

    public static Level Setting
    {
        get => (Level)Math.Clamp(Settings.Get("graphics", "shadows", (int)Level.High), 0, Names.Length - 1);
        set
        {
            Settings.Set("graphics", "shadows", (int)value);
            Changed?.Invoke();
        }
    }

    static event Action? Changed;

    // What each level costs: shadow atlas size (px, shared by the cascades), cascade
    // count, reach (m) and Godot's soft-shadow filter.
    readonly record struct Tier(int Atlas, int Splits, float Reach, RenderingServer.ShadowQuality Filter);
    static readonly Tier[] Tiers =
    {
        new(2048, 2, 40f, RenderingServer.ShadowQuality.SoftLow),
        new(4096, 4, 60f, RenderingServer.ShadowQuality.SoftMedium),
        new(8192, 4, 70f, RenderingServer.ShadowQuality.SoftHigh),
    };

    // How far (m) past the player the first cascade reaches, and the second and third.
    const float FirstPast = 5f, SecondPast = 16f, ThirdPast = 34f;
    // The sun's shadow direction steps by this much (degrees). At the normal day
    // length that is one small step about every 1.5 s; a 10 m tree's shadow tip moves
    // under 2 cm, within one texel of the nearest cascade.
    public const float SunStepDegrees = 0.1f;
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
        // trees; these keep them attached with no acne on the painted ground.
        sun.ShadowBias = 0.03f;
        sun.ShadowNormalBias = 1.2f;
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
        float reach = sun.DirectionalShadowMaxDistance;
        float Split(float past, float min) => Mathf.Clamp((toPlayer + past) / reach, min, 0.95f);
        float s1 = Split(FirstPast, 0.08f);
        float s2 = Mathf.Max(Split(SecondPast, 0.2f), s1 + 0.05f);
        float s3 = Mathf.Max(Split(ThirdPast, 0.4f), s2 + 0.05f);
        if (sun.DirectionalShadowMode == DirectionalLight3D.ShadowMode.Parallel2Splits)
            s1 = Split(SecondPast, 0.25f);
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
