using Godot;

namespace LifeGame;

// The Compatibility renderer (older and low-end PCs) has no volumetric fog, so
// the sun shafts in the woods (atmosphere-13) and the soft mist in the air would
// be missing there. On that renderer only: the volumetric mist is folded into the
// ordinary fog, dawn mist lies as height fog over the low ground, and a few soft
// columns of light stand toward a low sun in the woods. Forward+ is unchanged.
public partial class Atmosphere
{
    // True when the game runs on the Compatibility renderer.
    public static bool LowEnd { get; } = RenderingServer.GetCurrentRenderingMethod() == "gl_compatibility";

    const int ShaftCards = 5;
    readonly MeshInstance3D[] shaftCards = new MeshInstance3D[ShaftCards];
    StandardMaterial3D? shaftMaterial;
    float shaftsNow;

    void ReadyLowEnd()
    {
        if (!LowEnd) return;
        shaftMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.FixedY,
            NoDepthTest = false,
            DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
            // Brightest at the top, fading to nothing at the ground and the sides.
            AlbedoTexture = new GradientTexture2D
            {
                Width = 32,
                Height = 128,
                Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(0.5f, 0.15f),
                FillTo = new Vector2(1f, 1f),
                Gradient = new Gradient
                {
                    Offsets = new[] { 0f, 1f },
                    Colors = new[] { new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 0f) },
                },
            },
        };
        for (int i = 0; i < ShaftCards; i++)
        {
            shaftCards[i] = new MeshInstance3D
            {
                Mesh = new QuadMesh { Size = new Vector2(2.2f + i % 2 * 1.2f, 16f) },
                MaterialOverride = shaftMaterial,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Visible = false,
            };
            AddChild(shaftCards[i]);
        }
    }

    // The fog the volumetric mist would have given, added to the plain fog, plus
    // dawn mist as height fog. Called from ApplyLight after the fog is set.
    void LowEndFog(float mistDensity, float storm, float shafts)
    {
        shaftsNow = shafts;
        if (!LowEnd) return;
        env.FogDensity += mistDensity * 0.6f * (1f + storm);
        float dawnMist = Mist.Amount(timeOfDay, wind, rainAmount, wetness);
        env.FogHeight = Look.WaterLevel + 1.2f;
        env.FogHeightDensity = 0.12f * dawnMist;
    }

    // Light columns toward the sun, a few metres to tens of metres out, swaying
    // apart slowly so they never sit still in a row.
    void UpdateLowEndShafts(Vector3 focus, Vector3 sunDir, Color sunColor)
    {
        if (!LowEnd || shaftMaterial == null) return;
        bool show = shaftsNow > 0.02f;
        var toward = new Vector2(sunDir.X, sunDir.Z);
        if (toward.LengthSquared() < 1e-4f) show = false;
        toward = toward.Normalized();
        var side = new Vector2(-toward.Y, toward.X);
        for (int i = 0; i < ShaftCards; i++)
        {
            var card = shaftCards[i];
            card.Visible = show;
            if (!show) continue;
            float along = 6f + i * 3.5f;
            float across = (i - 2) * 3.2f + Mathf.Sin(Look.Clock * 0.05f + i * 1.7f) * 1.2f;
            var p = new Vector2(focus.X, focus.Z) + toward * along + side * across;
            card.GlobalPosition = new Vector3(p.X, focus.Y + 6f, p.Y);
        }
        if (show)
            shaftMaterial.AlbedoColor = new Color(sunColor.R, sunColor.G, sunColor.B, 0.09f * shaftsNow);
    }
}
