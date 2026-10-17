using Godot;

namespace LifeGame;

// Now and then a small flock of birds crosses the sky (env-35): five to twelve in a loose
// skein, 25 to 45 m up, drifting a little downwind, gone again in under half a minute.
// Rare on purpose: at Full one every two or three minutes of clear daylight, more likely
// at dusk, when a homeward flock goes over as soft dark shapes against the bright sky.
// Never at night or in rain. Lit by the scene like everything else (Atmosphere's ask).
public partial class AmbientLife
{
    const int MaxBirds = 12;

    // Raised on the main thread once per flock, at its closest point to the player: where
    // it is and how high (Audio's faint twitter).
    public static event System.Action<Vector3, float>? FlockPassing;
    // How far from the player a flock starts and ends its crossing.
    const float FlockReach = 70f;

    readonly Vector3[] birdOffsets = new Vector3[MaxBirds];
    readonly float[] birdPhases = new float[MaxBirds];
    MultiMeshInstance3D birds = null!;
    Vector3 flockAt, flockDir;
    float flockHeight, flockSpeed, nextFlock = 20f;
    int flockSize;
    bool flying, passed;

    void ReadyBirds()
    {
        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseCustomData = true,
            Mesh = BirdMesh(),
            InstanceCount = MaxBirds,
            VisibleInstanceCount = 0,
        };
        birds = new MultiMeshInstance3D
        {
            Multimesh = multi,
            MaterialOverride = new ShaderMaterial { Shader = Look.MakeShader(BirdShader) },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            TopLevel = true,
        };
        AddChild(birds);
    }

    // daylight 0 to 1; dry 1 in clear weather, 0 in rain; windDir the way the wind blows.
    void UpdateBirds(float dt, Vector3 focus, float daylight, float dry, Vector2 windDir, float density)
    {
        var multi = birds.Multimesh;
        if (!flying)
        {
            multi.VisibleInstanceCount = 0;
            bool day = daylight > 0.3f;
            // Dusk, just before the light goes: the homeward hour, flocks most likely.
            float dusk = Mathf.SmoothStep(0.3f, 0.45f, daylight) * (1f - Mathf.SmoothStep(0.55f, 0.8f, daylight));
            nextFlock -= dt * (forced ? 20f : density * (1f + 2f * dusk));
            if (nextFlock > 0f || !day || dry < 0.9f || density <= 0f) return;
            nextFlock = Rand(120f, 200f);
            StartFlock(focus, windDir);
        }
        flockAt += flockDir * flockSpeed * dt;
        // Done once it is well past the player on the far side.
        var rel = flockAt - focus;
        if (rel.Dot(flockDir) > FlockReach || rel.LengthSquared() > 4f * FlockReach * FlockReach)
        {
            flying = false;
            return;
        }
        if (!passed && rel.Dot(flockDir) >= 0f)
        {
            passed = true;
            FlockPassing?.Invoke(flockAt + Vector3.Up * flockHeight, flockHeight);
        }
        float yaw = Mathf.Atan2(flockDir.X, flockDir.Z);
        var turn = new Basis(Vector3.Up, yaw);
        for (int i = 0; i < flockSize; i++)
        {
            // Each bird wanders a little inside the skein, so it never looks drilled.
            float t = Time.GetTicksMsec() / 1000f + birdPhases[i];
            var wobble = new Vector3(Mathf.Sin(t * 0.7f) * 0.6f, Mathf.Sin(t * 0.9f + 1f) * 0.4f, Mathf.Sin(t * 0.5f) * 0.5f);
            var pos = flockAt + Vector3.Up * flockHeight + turn * (birdOffsets[i] + wobble);
            multi.SetInstanceTransform(i, new Transform3D(turn * Basis.FromScale(Vector3.One * 0.75f), pos));
            multi.SetInstanceCustomData(i, new Color(birdPhases[i], 0f, 0f, 0f));
        }
        multi.VisibleInstanceCount = flockSize;
    }

    void StartFlock(Vector3 focus, Vector2 windDir)
    {
        // Across the sky roughly with the wind, passing somewhere near overhead.
        float a = Mathf.Atan2(windDir.Y, windDir.X) + Rand(-0.9f, 0.9f);
        flockDir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
        var side = new Vector3(-flockDir.Z, 0f, flockDir.X);
        flockAt = focus - flockDir * FlockReach + side * Rand(-25f, 25f);
        flockAt.Y = focus.Y;
        flockHeight = Rand(25f, 45f);
        flockSpeed = Rand(7f, 10f);
        flockSize = 5 + random.Next(MaxBirds - 4);
        // A loose skein: rows trailing back to either side of a leader, gaps here and there.
        for (int i = 0; i < flockSize; i++)
        {
            int row = (i + 1) / 2;
            float s = i % 2 == 0 ? 1f : -1f;
            birdOffsets[i] = new Vector3(s * row * Rand(1.1f, 1.8f), Rand(-0.6f, 0.6f), -row * Rand(1.2f, 2.0f));
            birdPhases[i] = Rand(0f, 100f);
        }
        flying = true;
        passed = false;
    }

    // A small bird seen from below: two swept wings and a short body, along z (head forward).
    static ArrayMesh BirdMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        void Tri(Vector3 a, Vector3 b, Vector3 c, float wing)
        {
            foreach (var v in new[] { a, b, c })
            {
                st.SetUV(new Vector2(wing, 0f));
                st.SetNormal(Vector3.Up);
                st.AddVertex(v);
            }
        }
        Tri(new(0f, 0f, 0.25f), new(-0.06f, 0f, -0.2f), new(0.06f, 0f, -0.2f), 0f);
        foreach (float s in new[] { -1f, 1f })
        {
            Tri(new(0f, 0f, 0.1f), new(s * 0.5f, 0f, -0.08f), new(0f, 0f, -0.08f), 1f);
            Tri(new(s * 0.5f, 0f, -0.08f), new(s * 0.32f, 0f, -0.18f), new(0f, 0f, -0.08f), 1f);
        }
        var mesh = st.Commit();
        mesh.CustomAabb = new Aabb(new Vector3(-0.7f, -0.5f, -0.5f), new Vector3(1.4f, 1f, 1f));
        return mesh;
    }

    // Wings beat in bursts, then glide; soft warm grey-brown, lit, so the flock reads as
    // gentle dark shapes against a bright sky.
    const string BirdShader = """
        shader_type spatial;
        render_mode cull_disabled, diffuse_lambert_wrap, specular_disabled;

        void vertex() {
            if (UV.x > 0.5) {
                float t = TIME + INSTANCE_CUSTOM.x;
                float flapping = smoothstep(-0.2, 0.4, sin(t * 0.6));
                float beat = mix(0.12, sin(t * 11.0) * 0.55, flapping);
                float s = sign(VERTEX.x), r = abs(VERTEX.x);
                VERTEX = vec3(s * r * cos(beat), VERTEX.y + r * sin(beat), VERTEX.z);
            }
        }

        void fragment() {
            ALBEDO = vec3(0.3, 0.27, 0.26);
            ROUGHNESS = 1.0;
        }
        """;
}
