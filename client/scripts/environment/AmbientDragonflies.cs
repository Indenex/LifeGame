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

    // Jewel bodies (D-313): teal, sky blue, a warm amber one now and then. Stronger since
    // the first renders, where they were lost against the water at gameplay distance.
    static readonly Color[] DragonflyBodies = { new(0.08f, 0.72f, 0.66f), new(0.18f, 0.48f, 0.98f), new(0.98f, 0.55f, 0.14f) };

    // Their size: a little larger than life so they read from the follow camera.
    const float DragonflySize = 0.45f;

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

    // Open water: a cell whose bed lies well below the surface, off the reedy shelf.
    static bool Water(int x, int z) => World.BaseHeight(x, z) < World.WaterLevel - 0.18f;

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
                d = new Dragonfly { Home = home, Position = home + Vector3.Up * 0.35f, Target = home + Vector3.Up * 0.35f, Hover = Rand(0.3f, 1.5f), Phase = Rand(0f, 100f), Alive = true };
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
            var basis = new Basis(Vector3.Up, d.Heading) * Basis.FromScale(Vector3.One * DragonflySize * d.Shown);
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
            d.Target = new Vector3(x, World.WaterLevel + Rand(0.15f, 0.6f), z);
            DragonflyDart?.Invoke(d.Position);
            break;
        }
    }

    // A body along z (head forward): a round head, a stout thorax and a long tail that
    // tapers, as a flat strip and an upright one so it reads from above and the side; and
    // four wings across it, fore and hind. UV2.x is 0 on the body and 1 on a wing; UV is
    // the place on that part (across, along), so the shader can round the wings into
    // slim ovals with a painted edge and band the tail.
    static ArrayMesh DragonflyMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 e, float wing, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ue)
        {
            var vs = new[] { a, b, c, a, c, e };
            var us = new[] { ua, ub, uc, ua, uc, ue };
            for (int i = 0; i < 6; i++)
            {
                st.SetUV(us[i]);
                st.SetUV2(new Vector2(wing, 0f));
                st.SetNormal(Vector3.Up);
                st.AddVertex(vs[i]);
            }
        }
        // One piece of the body from z0 (width w0) to z1 (width w1), flat and upright; v0
        // and v1 say where it sits along the whole body, tail tip 0 to head 1.
        void Piece(float z0, float w0, float z1, float w1, float v0, float v1)
        {
            Quad(new(-w0, 0f, z0), new(w0, 0f, z0), new(w1, 0f, z1), new(-w1, 0f, z1), 0f, new(0, v0), new(1, v0), new(1, v1), new(0, v1));
            Quad(new(0f, -w0, z0), new(0f, w0, z0), new(0f, w1, z1), new(0f, -w1, z1), 0f, new(0, v0), new(1, v0), new(1, v1), new(0, v1));
        }
        Piece(-0.55f, 0.018f, 0.08f, 0.03f, 0f, 0.6f);   // the tail, thin at the tip
        Piece(0.08f, 0.05f, 0.4f, 0.055f, 0.6f, 0.9f);   // the thorax, where the wings sit
        Piece(0.4f, 0.075f, 0.52f, 0.06f, 0.9f, 1f);     // the head, big-eyed
        // The wings: long and narrow, the fore pair a little ahead of the hind.
        foreach (float s in new[] { -1f, 1f })
        {
            Quad(new(s * 0.03f, 0.02f, 0.3f), new(s * 0.62f, 0.02f, 0.33f), new(s * 0.62f, 0.02f, 0.17f), new(s * 0.03f, 0.02f, 0.13f), 1f,
                new(0, 1), new(1, 1), new(1, 0), new(0, 0));
            Quad(new(s * 0.03f, 0.02f, 0.12f), new(s * 0.58f, 0.02f, 0.08f), new(s * 0.58f, 0.02f, -0.08f), new(s * 0.03f, 0.02f, -0.04f), 1f,
                new(0, 1), new(1, 1), new(1, 0), new(0, 0));
        }
        var mesh = st.Commit();
        mesh.CustomAabb = new Aabb(new Vector3(-0.8f, -0.5f, -0.8f), new Vector3(1.6f, 1f, 1.6f));
        return mesh;
    }

    // Wings shimmer with a fast small beat, pale and see-through, and flash a bright glint
    // as each beat turns them to the light; the body takes the instance's jewel colour
    // with a faint glow so it reads against the water.
    const string DragonflyShader = """
        shader_type spatial;
        render_mode cull_disabled, diffuse_lambert_wrap, specular_disabled, depth_draw_opaque;

        varying float wing;
        varying float glint;
        varying vec3 body_tint;

        void vertex() {
            wing = UV2.x;
            body_tint = COLOR.rgb;
            glint = 0.0;
            if (wing > 0.5) {
                float phase = (TIME + INSTANCE_CUSTOM.x) * 38.0 + VERTEX.z * 4.0;
                float beat = sin(phase) * 0.32;
                // A short flash near the top of each slow sweep of the beat.
                glint = pow(max(sin(phase * 0.125 + INSTANCE_CUSTOM.x * 3.0), 0.0), 12.0);
                float s = sign(VERTEX.x), r = abs(VERTEX.x);
                VERTEX = vec3(s * r * cos(beat), VERTEX.y + r * sin(beat), VERTEX.z);
            }
        }

        void fragment() {
            if (wing > 0.5) {
                // A slim oval, rounder at the tip, with a darker painted rim and the
                // little dark spot near the tip that real dragonfly wings have.
                vec2 q = vec2(UV.x * 2.0 - 1.0, (UV.y * 2.0 - 1.0) * mix(1.25, 1.0, UV.x));
                float d = length(q);
                if (d > 1.0) discard;
                float rim = smoothstep(0.78, 0.9, d);
                float spot = (1.0 - smoothstep(0.06, 0.1, length(vec2((UV.x - 0.84) * 0.6, UV.y - 0.7))));
                ALBEDO = mix(vec3(0.88, 0.95, 1.0), vec3(0.32, 0.38, 0.42), max(rim * 0.7, spot));
                ALPHA = mix(0.28, 0.85, max(rim, spot)) + 0.4 * glint;
                EMISSION = vec3(0.9, 0.95, 1.0) * glint * 0.8;
            } else {
                // Rounded sides, soft dark bands along the tail, big dark eyes on the head.
                if (abs(UV.x - 0.5) > 0.5 - 0.5 * smoothstep(0.985, 1.0, UV.y)) discard;
                float bands = UV.y < 0.6 ? 0.25 * step(0.7, fract(UV.y * 14.0)) : 0.0;
                float eyes = smoothstep(0.92, 0.95, UV.y) * step(0.28, abs(UV.x - 0.5));
                vec3 c = body_tint * (1.0 - bands);
                c = mix(c, vec3(0.08, 0.1, 0.12), eyes);
                ALBEDO = c;
                EMISSION = c * 0.18;
            }
            ROUGHNESS = 0.6;
            BACKLIGHT = ALBEDO * 0.4;
        }
        """;
}
