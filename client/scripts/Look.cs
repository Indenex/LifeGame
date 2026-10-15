using Godot;

namespace LifeGame;

// The art direction in one place: a calm late-afternoon palette and the shared
// materials everything else draws with.
public static class Look
{
    public static readonly Color GrassLow = new(0.29f, 0.50f, 0.25f);
    public static readonly Color GrassHigh = new(0.50f, 0.64f, 0.28f);
    public static readonly Color Clay = new(0.72f, 0.45f, 0.32f);
    public static readonly Color DugEarth = new(0.38f, 0.27f, 0.20f);
    public static readonly Color Shore = new(0.78f, 0.72f, 0.55f);
    public static readonly Color Bark = new(0.36f, 0.27f, 0.21f);
    public static readonly Color Pine = new(0.22f, 0.40f, 0.30f);
    public static readonly Color Leaf = new(0.47f, 0.60f, 0.30f);
    public static readonly Color LeafWarm = new(0.80f, 0.62f, 0.26f);
    public static readonly Color Rock = new(0.47f, 0.49f, 0.45f);
    public static readonly Color Skin = new(0.86f, 0.66f, 0.52f);
    public static readonly Color Hair = new(0.25f, 0.18f, 0.14f);
    public static readonly Color Cream = new(0.97f, 0.94f, 0.87f);
    public static readonly Color Water = new(0.36f, 0.58f, 0.62f, 0.78f);

    public const float WaterLevel = -1.0f;

    static StandardMaterial3D? vertexColored;
    static ShaderMaterial? swaying, grass;

    // Faceted, matte surfaces whose color comes from the mesh or the multimesh instance.
    public static StandardMaterial3D VertexColored => vertexColored ??= new StandardMaterial3D
    {
        VertexColorUseAsAlbedo = true,
        // Palette colors are authored in sRGB, like the rest of the art direction.
        VertexColorIsSrgb = true,
        Roughness = 0.95f,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
    };

    // Foliage that drifts gently in the wind. Higher vertices move more.
    public static ShaderMaterial Swaying => swaying ??= MakeSway(0.10f, 2.5f);

    public static ShaderMaterial Grass => grass ??= MakeSway(0.12f, 0.45f);

    public static StandardMaterial3D Solid(Color color, float roughness = 0.9f) => new()
    {
        AlbedoColor = color,
        Roughness = roughness,
    };

    public static Mesh Faceted(Mesh mesh, Color color)
    {
        var st = new SurfaceTool();
        st.CreateFrom(mesh, 0);
        st.Deindex();
        st.GenerateNormals();
        var result = st.Commit();
        result.SurfaceSetMaterial(0, Solid(color));
        return result;
    }

    static ShaderMaterial MakeSway(float amount, float height)
    {
        var shader = new Shader
        {
            Code = """
                shader_type spatial;
                render_mode cull_disabled, diffuse_burley;

                uniform float sway = 0.1;
                uniform float sway_height = 2.0;

                varying vec3 tint;

                void vertex() {
                    vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
                    float h = clamp(VERTEX.y / sway_height, 0.0, 1.5);
                    float t = TIME * 0.9 + world.x * 0.21 + world.z * 0.17;
                    float gust = 0.6 + 0.4 * sin(TIME * 0.23 + world.x * 0.03);
                    VERTEX.x += sin(t) * sway * h * h * gust;
                    VERTEX.z += cos(t * 0.73) * sway * 0.6 * h * h * gust;
                    tint = pow(COLOR.rgb, vec3(2.2)); // palette is sRGB
                }

                void fragment() {
                    ALBEDO = tint;
                    ROUGHNESS = 0.92;
                    BACKLIGHT = tint * 0.25;
                }
                """,
        };
        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("sway", amount);
        material.SetShaderParameter("sway_height", height);
        return material;
    }
}
