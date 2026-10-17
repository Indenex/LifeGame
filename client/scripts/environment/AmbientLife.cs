using System;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Small life in the air round the player (env-30, Justin's cutesy pick #12): butterflies
// by day over the wildflower patches, pollen and dandelion seeds drifting over open
// meadows in spring and summer, and more fireflies at dusk near water (Atmosphere's
// Fireflies, given a higher amount here). Atmosphere's falling leaves and snow already
// cover autumn and winter. All of it is client-only and only in a box round the player:
// one MultiMesh of a dozen butterflies moved on the CPU, two small GPU particle systems.
// How much there is follows Options -> Graphics -> Ambient life (Off, Some, Full).
//
// Command line: --life=1 holds butterflies, pollen and seeds at full whatever the time
// and season, for renders.
public partial class AmbientLife : Node3D
{
    public static readonly string[] Names = { "Off", "Some", "Full" };

    // Settings graphics/ambient_life, 0 Off, 1 Some, 2 Full.
    public static int Setting
    {
        get => Math.Clamp(Settings.Get("graphics", "ambient_life", 2), 0, 2);
        set => Settings.Set("graphics", "ambient_life", value);
    }

    static float Density => Setting * 0.5f;

    const int MaxButterflies = 14;
    // How far from the player butterflies find flowers, and lose them again.
    const float Reach = 18f, Leave = 26f;

    struct Butterfly
    {
        public Vector3 Home, Position, Target;
        public float Speed, Rest, Phase, Size, Shown;
        public bool Landed, Alive;
    }

    readonly Butterfly[] flies = new Butterfly[MaxButterflies];
    readonly MultiMeshInstance3D butterflies;
    readonly GpuParticles3D pollen, seeds;
    readonly ParticleProcessMaterial pollenProcess, seedProcess;
    readonly Random random = new(31);
    readonly bool forced = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--life=1");
    float waterNear, waterClock;
    Vector3 waterAt = new(float.MaxValue, 0, 0);

    // Soft pastel wings (D-313): brimstone, cabbage white, peach, lilac blue.
    static readonly Color[] Wings =
    {
        new(0.98f, 0.86f, 0.4f), new(0.97f, 0.95f, 0.88f), new(0.99f, 0.66f, 0.42f), new(0.62f, 0.68f, 0.98f),
    };

    public AmbientLife()
    {
        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            UseCustomData = true,
            Mesh = WingMesh(),
            InstanceCount = MaxButterflies,
            VisibleInstanceCount = 0,
        };
        butterflies = new MultiMeshInstance3D
        {
            Multimesh = multi,
            MaterialOverride = new ShaderMaterial { Shader = Look.MakeShader(WingShader) },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            TopLevel = true,
        };
        AddChild(butterflies);

        (pollen, pollenProcess) = Motes(160, 9f, 0.04f, new Color(1f, 0.93f, 0.62f), 0.55f, -0.02f, false);
        (seeds, seedProcess) = Motes(40, 12f, 0.1f, new Color(1f, 0.99f, 0.95f), 0.85f, 0.04f, true);
        AddChild(pollen);
        AddChild(seeds);
    }

    // daylight 0 night to 1 day, year 0 to 1, wind 0 to 1 blowing windDir, woods 0 open
    // to 1 deep woods, rain 0 to 1.
    public void Follow(float dt, Vector3 focus, Terrain? terrain, float daylight, float year, float wind, Vector2 windDir, float woods, float rain)
    {
        // Nothing to see when the camera is away from the player (free camera, map view).
        if (GetViewport()?.GetCamera3D() is { } camera && camera.GlobalPosition.DistanceSquaredTo(focus) > 60f * 60f)
        {
            pollen.Emitting = seeds.Emitting = false;
            butterflies.Visible = false;
            return;
        }
        butterflies.Visible = true;
        float density = forced ? 1f : Density;
        float warm = Mathf.SmoothStep(-0.4f, 0.2f, Seasons.Warmth(year));
        float day = Mathf.SmoothStep(0.45f, 0.8f, daylight);
        float dry = 1f - Mathf.Clamp(rain * 3f, 0f, 1f);
        float open = 1f - woods;

        // Butterflies: warm dry days, in the open.
        float wanted = forced ? 1f : warm * day * dry * Mathf.SmoothStep(0.1f, 0.6f, open) * density;
        UpdateButterflies(dt, focus, terrain, Mathf.RoundToInt(wanted * MaxButterflies), wind);

        // Pollen and seeds: spring into high summer, most in the open meadows; light
        // enough that the wind carries them along.
        float bloom = Mathf.SmoothStep(0.02f, 0.12f, year) * (1f - Mathf.SmoothStep(0.36f, 0.46f, year));
        float motes = forced ? 1f : bloom * day * dry * open * density;
        GlobalPosition = focus + new Vector3(0f, 1.2f, 0f);
        float push = 0.15f + wind * 1.2f;
        Emit(pollen, pollenProcess, motes, new Vector3(windDir.X * push, -0.02f, windDir.Y * push));
        Emit(seeds, seedProcess, motes * Mathf.SmoothStep(0.15f, 0.3f, year), new Vector3(windDir.X * push, 0.03f, windDir.Y * push));

        waterClock -= dt;
        if (waterClock <= 0f || focus.DistanceSquaredTo(waterAt) > 16f)
        {
            waterClock = 0.5f;
            waterAt = focus;
            waterNear = WaterNear(focus);
        }
    }

    // Atmosphere's firefly amount, raised near ponds, streams and wet ground: they come
    // out there at dusk, in the warm half of the year, before the open fields light up.
    public float Fireflies(float amount, float night, float year)
    {
        float warm = Mathf.SmoothStep(-0.2f, 0.4f, Seasons.Warmth(year));
        float dusk = waterNear * warm * Mathf.SmoothStep(0.45f, 0.75f, night);
        return Mathf.Max(amount, dusk) * Density;
    }

    static void Emit(GpuParticles3D system, ParticleProcessMaterial process, float amount, Vector3 drift)
    {
        system.Emitting = amount > 0.02f;
        system.AmountRatio = Mathf.Clamp(amount, 0f, 1f);
        process.Gravity = drift;
    }

    // 0 to 1, how much water or wet ground lies within about 12 m.
    static float WaterNear(Vector3 at)
    {
        int cx = World.CellOf(at.X), cz = World.CellOf(at.Z);
        float best = 0f;
        for (int dx = -12; dx <= 12; dx += 3)
            for (int dz = -12; dz <= 12; dz += 3)
            {
                int x = cx + dx, z = cz + dz;
                float water = Mathf.SmoothStep(World.WaterLevel + 1.2f, World.WaterLevel + 0.1f, World.BaseHeight(x, z));
                best = Mathf.Max(best, Mathf.Max(water, Forest.Wetland(x, z)));
            }
        return best;
    }

    // A wildflower patch cell, the same rule Terrain plants them by.
    static bool Flowers(int x, int z) =>
        Forest.Density(x, z) < 0.2f && Forest.Heath(x, z) < 0.5f
        && World.BaseHeight(x, z) > World.WaterLevel + 0.4f && World.Noise(x, z, 6f, 63) > 0.62f;

    float Ground(Terrain? terrain, float x, float z) =>
        terrain?.HeightAt(x, z) ?? World.BaseHeight(World.CellOf(x), World.CellOf(z));

    float Rand(float a, float b) => a + (float)random.NextDouble() * (b - a);

    void UpdateButterflies(float dt, Vector3 focus, Terrain? terrain, int wanted, float wind)
    {
        var multi = butterflies.Multimesh;
        int tries = 30;
        int visible = 0;
        for (int i = 0; i < MaxButterflies; i++)
        {
            ref var b = ref flies[i];
            bool keep = i < wanted;
            if (b.Alive && b.Home.DistanceSquaredTo(focus) > Leave * Leave) b.Alive = false;
            while (!b.Alive && keep && tries-- > 0)
            {
                // A flower patch somewhere near the player, not right underfoot.
                float angle = Rand(0f, Mathf.Tau), dist = Rand(5f, Reach);
                int x = World.CellOf(focus.X + Mathf.Cos(angle) * dist), z = World.CellOf(focus.Z + Mathf.Sin(angle) * dist);
                if (Flowers(x, z))
                {
                    var home = new Vector3(x + 0.5f, Ground(terrain, x + 0.5f, z + 0.5f), z + 0.5f);
                    b = new Butterfly
                    {
                        Home = home, Position = home + new Vector3(0f, 0.8f, 0f), Target = home,
                        Speed = Rand(0.9f, 1.4f), Phase = Rand(0f, 100f), Size = Rand(0.8f, 1.15f), Alive = true,
                    };
                    multi.SetInstanceColor(i, Wings[random.Next(Wings.Length)]);
                }
            }
            // Fade in on arrival, out when not wanted (or dark, or rain) and then gone.
            b.Shown = forced && b.Alive ? 1f : Mathf.MoveToward(b.Shown, b.Alive && keep ? 1f : 0f, dt * 0.7f);
            if (b.Shown <= 0f)
            {
                if (!keep) b.Alive = false;
                multi.SetInstanceTransform(i, new Transform3D(Basis.FromScale(Vector3.Zero), focus));
                continue;
            }
            Step(ref b, dt, terrain, wind);

            float flight = b.Landed ? 0f : 1f;
            var heading = new Vector3(b.Target.X - b.Position.X, 0f, b.Target.Z - b.Position.Z);
            float yaw = heading.LengthSquared() > 0.0001f ? Mathf.Atan2(heading.X, heading.Z) : 0f;
            var basis = new Basis(Vector3.Up, yaw) * new Basis(Vector3.Right, -0.25f * flight) * Basis.FromScale(Vector3.One * 0.17f * b.Size * b.Shown);
            multi.SetInstanceTransform(i, new Transform3D(basis, b.Position));
            multi.SetInstanceCustomData(i, new Color(b.Phase, b.Landed ? 1f : 0f, 0f, 0f));
            visible = i + 1;
        }
        multi.VisibleInstanceCount = visible;
    }

    // Fluttering flight from flower to flower round its patch: a wobbly line to a point,
    // now and then down onto a flower for a few seconds with the wings held up.
    void Step(ref Butterfly b, float dt, Terrain? terrain, float wind)
    {
        if (b.Landed)
        {
            b.Rest -= dt;
            if (b.Rest > 0f) return;
            b.Landed = false;
        }
        var to = b.Target - b.Position;
        if (to.LengthSquared() < 0.04f)
        {
            if (b.Target.Y - Ground(terrain, b.Target.X, b.Target.Z) < 0.5f)
            {
                b.Landed = true;
                b.Rest = Rand(2f, 5f);
            }
            // Next: a flower to land on, about one time in three, or a point in the air.
            float angle = Rand(0f, Mathf.Tau), dist = Rand(0.6f, 3f);
            float x = b.Home.X + Mathf.Cos(angle) * dist, z = b.Home.Z + Mathf.Sin(angle) * dist;
            bool land = random.NextDouble() < 0.33 && Flowers(World.CellOf(x), World.CellOf(z));
            b.Target = new Vector3(x, Ground(terrain, x, z) + (land ? 0.38f : Rand(0.6f, 1.8f)), z);
            return;
        }
        b.Phase += dt;
        float t = b.Phase;
        // Butterflies never fly straight: side-to-side and up-and-down wobble on the way.
        var dir = to.Normalized();
        var side = new Vector3(-dir.Z, 0f, dir.X);
        var wobble = side * Mathf.Sin(t * 3.1f) * 0.9f + Vector3.Up * Mathf.Sin(t * 4.7f + 1f) * 0.7f;
        var step = (dir + wobble * (1f - wind * 0.3f)) * b.Speed * dt;
        if (step.LengthSquared() > to.LengthSquared()) step = to;
        b.Position += step;
        float floor = Ground(terrain, b.Position.X, b.Position.Z) + 0.15f;
        if (b.Position.Y < floor) b.Position.Y = floor;
    }

    // Two wings hinged along the body (the z axis, head forward), each a unit quad the
    // shader cuts to a fore and hind wing.
    static ArrayMesh WingMesh()
    {
        var verts = new Vector3[8];
        var uvs = new Vector2[8];
        var normals = new Vector3[8];
        int[] index = { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6 };
        for (int side = 0; side < 2; side++)
        {
            float s = side == 0 ? -1f : 1f;
            for (int k = 0; k < 4; k++)
            {
                float u = k == 1 || k == 2 ? 1f : 0f, v = k >= 2 ? 1f : 0f;
                verts[side * 4 + k] = new Vector3(s * u, 0f, v * 1.6f - 0.7f);
                uvs[side * 4 + k] = new Vector2(u, v);
                normals[side * 4 + k] = Vector3.Up;
            }
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Index] = index;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.CustomAabb = new Aabb(new Vector3(-1.2f, -1.2f, -1f), new Vector3(2.4f, 2.4f, 2f));
        return mesh;
    }

    // Wings beat round the body axis (INSTANCE_CUSTOM.x the clock offset, .y 1 when
    // settled, wings held up together); rounded fore and hind wings in the instance's
    // pastel, a soft darker rim and a spot, a slim body down the middle.
    const string WingShader = """
        shader_type spatial;
        render_mode cull_disabled, diffuse_lambert_wrap, specular_disabled;

        varying vec3 wing_tint;
        varying float settled;

        void vertex() {
            settled = INSTANCE_CUSTOM.y;
            wing_tint = COLOR.rgb;
            float t = TIME + INSTANCE_CUSTOM.x;
            float beat = sin(t * 15.0) * 0.5 + 0.5;
            // A slower open-and-close while resting on a flower.
            float rest = 1.25 + 0.2 * sin(t * 1.3);
            float a = mix(mix(-0.35, 1.25, beat), rest, settled);
            float s = sign(VERTEX.x);
            float r = abs(VERTEX.x);
            VERTEX = vec3(s * r * cos(a), r * sin(a), VERTEX.z);
            NORMAL = normalize(vec3(-s * sin(a), cos(a), 0.0));
        }

        void fragment() {
            vec2 p = vec2(UV.x, UV.y * 1.6 - 0.7);
            float fore = length((p - vec2(0.5, 0.3)) / vec2(0.55, 0.45));
            float hind = length((p - vec2(0.38, -0.3)) / vec2(0.4, 0.34));
            float wing = min(fore, hind);
            bool body = UV.x < 0.06 && p.y > -0.55 && p.y < 0.75;
            if (wing > 1.0 && !body) discard;
            vec3 c = wing_tint;
            c *= mix(1.0, 0.55, smoothstep(0.72, 0.95, wing));
            c = mix(c, c * vec3(0.6, 0.5, 0.55), 1.0 - smoothstep(0.08, 0.13, length(p - vec2(0.6, 0.38))));
            if (body) c = vec3(0.22, 0.17, 0.14);
            ALBEDO = c;
            ROUGHNESS = 1.0;
            BACKLIGHT = c * 0.5;
        }
        """;

    // A box of soft round motes drifting with the wind round the player.
    static (GpuParticles3D, ParticleProcessMaterial) Motes(int amount, float lifetime, float size, Color color, float alpha, float lift, bool fluffy)
    {
        var fade = new Curve();
        fade.AddPoint(new Vector2(0f, 0f));
        fade.AddPoint(new Vector2(0.15f, 1f));
        fade.AddPoint(new Vector2(0.8f, 1f));
        fade.AddPoint(new Vector2(1f, 0f));
        var process = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            EmissionBoxExtents = new Vector3(13f, 1f, 13f),
            Direction = Vector3.Up,
            Spread = 180f,
            InitialVelocityMin = 0.02f,
            InitialVelocityMax = 0.12f,
            Gravity = new Vector3(0f, lift, 0f),
            DampingMin = 0.2f,
            DampingMax = 0.5f,
            TurbulenceEnabled = true,
            TurbulenceNoiseStrength = fluffy ? 1.2f : 0.8f,
            TurbulenceNoiseScale = 5f,
            TurbulenceInfluenceMin = 0.03f,
            TurbulenceInfluenceMax = 0.08f,
            ScaleMin = 0.6f,
            ScaleMax = 1.2f,
            ScaleCurve = new CurveTexture { Curve = fade },
        };
        // A soft dot; a dandelion seed's clock of fluff is a larger, fainter halo.
        var gradient = new Gradient
        {
            Offsets = fluffy ? new[] { 0f, 0.18f, 0.32f, 1f } : new[] { 0f, 0.45f, 1f },
            Colors = fluffy
                ? new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0.75f), new Color(1, 1, 1, 0.35f), new Color(1, 1, 1, 0) }
                : new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0.6f), new Color(1, 1, 1, 0) },
        };
        var texture = new GradientTexture2D
        {
            Gradient = gradient, Width = 32, Height = 32,
            Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1f, 0.5f),
        };
        var system = new GpuParticles3D
        {
            Amount = amount,
            Lifetime = lifetime,
            Preprocess = lifetime,
            LocalCoords = false,
            Emitting = false,
            VisibilityAabb = new Aabb(new Vector3(-18, -4, -18), new Vector3(36, 8, 36)),
            ProcessMaterial = process,
            DrawPass1 = new QuadMesh
            {
                Size = new Vector2(size, size),
                Material = new StandardMaterial3D
                {
                    BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    // Lit by the scene, so nothing left drifting at dusk glows in the dark.
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.PerVertex,
                    Roughness = 1f,
                    AlbedoTexture = texture,
                    AlbedoColor = new Color(color, alpha),
                    VertexColorUseAsAlbedo = true,
                },
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        return (system, process);
    }
}
