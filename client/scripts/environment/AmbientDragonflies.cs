using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Dragonflies over ponds and streams on warm days (env-35, ambient life part two): each
// keeps to its own stretch of water, hovers a moment, then darts off to a new spot a few
// metres away, low over the surface. A handful at most, one MultiMesh moved on the CPU.
public partial class AmbientLife
{
    const int MaxDragonflies = 6;

    // Raised on the main thread once per dart, where it starts (Audio's soft wing whirr).
    public static event System.Action<Vector3>? DragonflyDart;

    struct Dragonfly
    {
        public Vector3 Home, Position, Target;
        public float Hover, Heading, Phase, Shown;
        public bool Alive;
    }

    readonly Dragonfly[] darters = new Dragonfly[MaxDragonflies];
    MultiMeshInstance3D dragonflies = null!;

    // Soft jewel bodies (D-313): teal, sky blue, a warm amber one now and then.
    static readonly Color[] DragonflyBodies = { new(0.22f, 0.62f, 0.6f), new(0.34f, 0.55f, 0.86f), new(0.82f, 0.56f, 0.28f) };

    void ReadyDragonflies()
    {
        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            UseCustomData = true,
            Mesh = DragonflyMesh(),
            InstanceCount = MaxDragonflies,
            VisibleInstanceCount = 0,
        };
        dragonflies = new MultiMeshInstance3D
        {
            Multimesh = multi,
            MaterialOverride = new ShaderMaterial { Shader = Look.MakeShader(DragonflyShader) },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            TopLevel = true,
        };
        AddChild(dragonflies);
    }

    // Open water: a cell whose ground lies below the water's surface.
    static bool Water(int x, int z) => World.BaseHeight(x, z) < World.WaterLevel - 0.1f;

    void UpdateDragonflies(float dt, Vector3 focus, int wanted)
    {
        var multi = dragonflies.Multimesh;
        int tries = 20, visible = 0;
        for (int i = 0; i < MaxDragonflies; i++)
        {
            ref var d = ref darters[i];
            bool keep = i < wanted;
            if (d.Alive && d.Home.DistanceSquaredTo(focus) > Leave * Leave) d.Alive = false;
            while (!d.Alive && keep && tries-- > 0)
            {
                float angle = Rand(0f, Mathf.Tau), dist = Rand(3f, Reach);
                int x = World.CellOf(focus.X + Mathf.Cos(angle) * dist), z = World.CellOf(focus.Z + Mathf.Sin(angle) * dist);
                if (!Water(x, z)) continue;
                var home = new Vector3(x + 0.5f, World.WaterLevel, z + 0.5f);
                d = new Dragonfly { Home = home, Position = home + Vector3.Up * 0.6f, Target = home + Vector3.Up * 0.6f, Hover = Rand(0.3f, 1.5f), Phase = Rand(0f, 100f), Alive = true };
                multi.SetInstanceColor(i, DragonflyBodies[random.Next(DragonflyBodies.Length)]);
            }
            d.Shown = forced && d.Alive ? 1f : Mathf.MoveToward(d.Shown, d.Alive && keep ? 1f : 0f, dt * 0.8f);
            if (d.Shown <= 0f)
            {
                if (!keep) d.Alive = false;
                multi.SetInstanceTransform(i, new Transform3D(Basis.FromScale(Vector3.Zero), focus));
                continue;
            }
            Dart(ref d, dt);
            var basis = new Basis(Vector3.Up, d.Heading) * Basis.FromScale(Vector3.One * 0.3f * d.Shown);
            multi.SetInstanceTransform(i, new Transform3D(basis, d.Position));
            multi.SetInstanceCustomData(i, new Color(d.Phase, 0f, 0f, 0f));
            visible = i + 1;
        }
        multi.VisibleInstanceCount = visible;
    }

    // Hover in place with a tiny bob, then a quick straight dash to a new point over the water.
    void Dart(ref Dragonfly d, float dt)
    {
        d.Phase += dt;
        var to = d.Target - d.Position;
        if (to.LengthSquared() > 0.0025f)
        {
            float step = Mathf.Min(to.Length(), 5.5f * dt);
            d.Position += to.Normalized() * step;
            d.Heading = Mathf.LerpAngle(d.Heading, Mathf.Atan2(to.X, to.Z), Mathf.Min(1f, dt * 14f));
            return;
        }
        d.Position = d.Target + new Vector3(0f, Mathf.Sin(d.Phase * 5f) * 0.02f, 0f);
        d.Hover -= dt;
        if (d.Hover > 0f) return;
        d.Hover = Rand(0.4f, 2.2f);
        for (int k = 0; k < 4; k++)
        {
            float angle = Rand(0f, Mathf.Tau), dist = Rand(0.8f, 3.5f);
            float x = d.Home.X + Mathf.Cos(angle) * dist, z = d.Home.Z + Mathf.Sin(angle) * dist;
            if (!Water(World.CellOf(x), World.CellOf(z))) continue;
            d.Target = new Vector3(x, World.WaterLevel + Rand(0.25f, 1.1f), z);
            DragonflyDart?.Invoke(d.Position);
            break;
        }
    }

    // A slim body along z (head forward) and four wings across it, fore and hind; UV.x is
    // 0 on the body and 1 on a wing, so the shader tells them apart.
    static ArrayMesh DragonflyMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 e, float wing)
        {
            foreach (var v in new[] { a, b, c, a, c, e })
            {
                st.SetUV(new Vector2(wing, 0f));
                st.SetNormal(Vector3.Up);
                st.AddVertex(v);
            }
        }
        // The body: a flat strip and an upright one, so it reads from above and the side.
        Quad(new(-0.035f, 0f, -0.55f), new(0.035f, 0f, -0.55f), new(0.035f, 0f, 0.45f), new(-0.035f, 0f, 0.45f), 0f);
        Quad(new(0f, -0.035f, -0.55f), new(0f, 0.035f, -0.55f), new(0f, 0.035f, 0.45f), new(0f, -0.035f, 0.45f), 0f);
        // The wings: long and narrow, the fore pair a little ahead of the hind.
        foreach (float s in new[] { -1f, 1f })
        {
            Quad(new(s * 0.03f, 0.02f, 0.26f), new(s * 0.62f, 0.02f, 0.3f), new(s * 0.62f, 0.02f, 0.18f), new(s * 0.03f, 0.02f, 0.14f), 1f);
            Quad(new(s * 0.03f, 0.02f, 0.1f), new(s * 0.58f, 0.02f, 0.06f), new(s * 0.58f, 0.02f, -0.06f), new(s * 0.03f, 0.02f, -0.02f), 1f);
        }
        var mesh = st.Commit();
        mesh.CustomAabb = new Aabb(new Vector3(-0.8f, -0.5f, -0.8f), new Vector3(1.6f, 1f, 1.6f));
        return mesh;
    }

    // Wings shimmer with a fast small beat and catch the light pale and see-through; the
    // body takes the instance's soft jewel colour.
    const string DragonflyShader = """
        shader_type spatial;
        render_mode cull_disabled, diffuse_lambert_wrap, specular_disabled, depth_draw_opaque;

        varying float wing;
        varying vec3 body_tint;

        void vertex() {
            wing = UV.x;
            body_tint = COLOR.rgb;
            if (wing > 0.5) {
                float beat = sin((TIME + INSTANCE_CUSTOM.x) * 38.0 + VERTEX.z * 4.0) * 0.32;
                float s = sign(VERTEX.x), r = abs(VERTEX.x);
                VERTEX = vec3(s * r * cos(beat), VERTEX.y + r * sin(beat), VERTEX.z);
            }
        }

        void fragment() {
            if (wing > 0.5) {
                ALBEDO = vec3(0.86, 0.93, 1.0);
                ALPHA = 0.42;
            } else {
                ALBEDO = body_tint;
            }
            ROUGHNESS = 0.6;
            BACKLIGHT = ALBEDO * 0.4;
        }
        """;
}
