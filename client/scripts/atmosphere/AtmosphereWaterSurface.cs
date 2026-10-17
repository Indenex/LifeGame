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

    // The streams' flow round the player: 64 m at a metre a texel, rebuilt every 8 m.
    const int FlowRes = 64;
    const float FlowSize = 64f;
    readonly Image flowImage = Image.CreateEmpty(FlowRes, FlowRes, false, Image.Format.Rgba8);
    readonly ImageTexture flowTexture = new();
    Vector2 flowOrigin = new(float.MaxValue, 0f);
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
        var flowWant = new Vector2(Mathf.Round(focus.X) - FlowSize / 2f, Mathf.Round(focus.Z) - FlowSize / 2f);
        if (flowWant.DistanceTo(flowOrigin) > 8f) RebuildFlow(flowWant);

        bool day = waterSunDir.Y > -0.05f;
        RenderingServer.GlobalShaderParameterSet(WaterLook.SkyTop, ToVec(waterSkyTop));
        RenderingServer.GlobalShaderParameterSet(WaterLook.SkyHorizon, ToVec(waterSkyHorizon));
        RenderingServer.GlobalShaderParameterSet(WaterLook.SunDir, day ? waterSunDir : -sun.GlobalBasis.Z);
        RenderingServer.GlobalShaderParameterSet(WaterLook.SunColor, ToVec(sun.LightColor) * (sun.Visible ? sun.LightEnergy : 0f));
        RenderingServer.GlobalShaderParameterSet(WaterLook.Ambient, ToVec(env.AmbientLightColor) * env.AmbientLightEnergy);
        RenderingServer.GlobalShaderParameterSet(WaterLook.ReflectSteps, ReflectionSteps[WaterReflections]);
    }

    // Which way the streams run near the player: along the stream's line (Land.StreamAlong,
    // steady mid-channel; env-38 replaced the slope of the distance, which flipped there
    // and set the ripples and leaves jittering), downstream taken as south (+z)
    // everywhere, strongest mid-channel and gone by the edge of its water.
    void RebuildFlow(Vector2 origin)
    {
        flowOrigin = origin;
        bool any = false;
        for (int j = 0; j < FlowRes; j++)
            for (int i = 0; i < FlowRes; i++)
            {
                int x = (int)origin.X + i, z = (int)origin.Y + j;
                float d = Land.StreamDistance(x, z);
                var c = new Color(0.5f, 0.5f, 0f, 1f);
                if (d < 12f)
                {
                    var (ax, az) = Land.StreamAlong(x, z);
                    float strength = 1f - Mathf.SmoothStep(4f, 11f, d);
                    c = new Color(0.5f + 0.5f * ax * strength, 0.5f + 0.5f * az * strength, 0f, 1f);
                    any = true;
                }
                flowImage.SetPixel(i, j, c);
            }
        flowTexture.SetImage(flowImage);
        RenderingServer.GlobalShaderParameterSet(WaterLook.Flow, flowTexture);
        RenderingServer.GlobalShaderParameterSet(WaterLook.FlowRect, new Vector4(origin.X - 0.5f, origin.Y - 0.5f, FlowSize, any ? 1f : 0f));
    }

    static Vector3 ToVec(Color c) => new(c.R, c.G, c.B);
}
