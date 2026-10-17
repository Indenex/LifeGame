using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The water's surface, kept by Atmosphere (atmosphere-31): the dense waving mesh round
// the player (WaterLook's near surface), the interactive ripples (WaterRipples), and
// the light the water shader needs each frame: the sky's colours, the sun or moon,
// the ambient light and how far its reflections may trace.
//
// Reflection quality is a Graphics option: 0 off (sky only), 1 normal, 2 high.
// Command line: --water-reflections=0|1|2 overrides it.
public partial class Atmosphere
{
    public static int WaterReflections = 1;

    // For anything else that moves through water (Animals' wading sheep and deer):
    // push the surface at a point, radius in metres, strength 0 to 1 (a splash is 1).
    public static System.Action<Vector3, float, float>? DisturbWater;
    static readonly float[] ReflectionSteps = { 0f, 16f, 32f };

    // 96 m across at half a metre a vertex, kept on that grid as it follows the player.
    const float NearSize = 96f, NearSpacing = 0.5f;

    readonly MeshInstance3D nearWater = new()
    {
        Mesh = new PlaneMesh
        {
            Size = new Vector2(NearSize, NearSize),
            SubdivideWidth = (int)(NearSize / NearSpacing) - 1,
            SubdivideDepth = (int)(NearSize / NearSpacing) - 1,
        },
        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        TopLevel = true,
        ExtraCullMargin = 2f,
    };
    readonly WaterRipples waterRipples = new();
    Color waterSkyTop = new(0.35f, 0.55f, 0.8f), waterSkyHorizon = new(0.7f, 0.78f, 0.82f);
    Vector3 waterSunDir = Vector3.Up;

    void ReadyWaterSurface()
    {
        WaterReflections = Mathf.Clamp(Settings.Get("graphics", "water_reflections", 1), 0, 2);
        if (ArgFloat("water-reflections") is { } level) WaterReflections = Mathf.Clamp((int)level, 0, 2);
        nearWater.MaterialOverride = Look.WaterNear;
        AddChild(nearWater);
        waterRipples.GroundAt = (x, z) => terrain?.HeightAt(x, z) ?? float.NaN;
        AddChild(waterRipples);
        DisturbWater = waterRipples.Disturb;
    }

    void UpdateWaterSurface(float dt, Vector3 focus)
    {
        nearWater.GlobalPosition = new Vector3(Mathf.Snapped(focus.X, NearSpacing), World.WaterLevel, Mathf.Snapped(focus.Z, NearSpacing));
        waterRipples.Tick(dt, focus, rainAmount);

        bool day = waterSunDir.Y > -0.05f;
        RenderingServer.GlobalShaderParameterSet(WaterLook.SkyTop, ToVec(waterSkyTop));
        RenderingServer.GlobalShaderParameterSet(WaterLook.SkyHorizon, ToVec(waterSkyHorizon));
        RenderingServer.GlobalShaderParameterSet(WaterLook.SunDir, day ? waterSunDir : -sun.GlobalBasis.Z);
        RenderingServer.GlobalShaderParameterSet(WaterLook.SunColor, ToVec(sun.LightColor) * (sun.Visible ? sun.LightEnergy : 0f));
        RenderingServer.GlobalShaderParameterSet(WaterLook.Ambient, ToVec(env.AmbientLightColor) * env.AmbientLightEnergy);
        RenderingServer.GlobalShaderParameterSet(WaterLook.ReflectSteps, ReflectionSteps[WaterReflections]);
    }

    static Vector3 ToVec(Color c) => new(c.R, c.G, c.B);
}
