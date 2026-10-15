using System.Collections.Generic;
using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// A round, woolly sheep with a dark face and legs. It glides between the positions
// the server sends and animates from how fast it is actually moving: legs swing
// when walking, it bounds when running, hops when startled, and drops its head to
// nibble grass when grazing.
public partial class Sheep : Node3D
{
    // Slightly longer than the server's half-second tick, so a sheep is still
    // gliding when the next update arrives and never stops between steps.
    const float GlideSeconds = 0.6f;
    const float HopSeconds = 0.35f;

    readonly Terrain terrain;
    readonly Node3D body = new();
    readonly Node3D head = new() { Position = new Vector3(0, 0.8f, 0.4f) };
    readonly Node3D[] legs = new Node3D[4];
    readonly float seed;

    Vector2 from, to;
    float glide = 1f, targetHeading, speed, gait, hop = HopSeconds, pitch, yaw;
    Vector3 lastPosition;
    AnimalState state = AnimalState.Graze;
    readonly MeshInstance3D fleece;
    Color woolColor;
    float wool = 1f, woolTarget = 1f;

    static readonly Color Face = new(0.17f, 0.14f, 0.12f);
    // Mostly cream, some oatmeal, and the odd dark sheep.
    static readonly Color[] Fleeces =
    {
        new(0.87f, 0.84f, 0.75f),
        new(0.87f, 0.84f, 0.75f),
        new(0.87f, 0.84f, 0.75f),
        new(0.80f, 0.74f, 0.62f),
        new(0.80f, 0.74f, 0.62f),
        new(0.87f, 0.84f, 0.75f),
        new(0.87f, 0.84f, 0.75f),
        new(0.34f, 0.27f, 0.22f),
    };
    static readonly Dictionary<int, Material> WoolMaterials = new();
    static Mesh? woolMesh, skinMesh, headMesh, legMesh;
    static Material? skinMaterial;
    static readonly Color Skin = new(0.80f, 0.66f, 0.58f);
    static Material? faceMaterial;

    public Sheep(ulong id, Terrain terrain)
    {
        this.terrain = terrain;
        seed = Terrain.Hash01((int)id, 7, 33) * 100f;
        BuildMeshes();

        int color = (int)(Terrain.Hash01((int)id, 11, 5) * Fleeces.Length) % Fleeces.Length;
        if (!WoolMaterials.TryGetValue(color, out var wool))
            WoolMaterials[color] = wool = Look.Solid(Fleeces[color]);
        woolColor = Fleeces[color];

        // A shorn sheep is a slim pink body; the fleece shrinks into it when shorn
        // and swells back out as the wool regrows.
        body.AddChild(new MeshInstance3D { Mesh = skinMesh, MaterialOverride = skinMaterial });
        fleece = new MeshInstance3D { Mesh = woolMesh, MaterialOverride = wool };
        body.AddChild(fleece);
        head.AddChild(new MeshInstance3D { Mesh = headMesh, MaterialOverride = faceMaterial });
        // A tuft of wool on top of the head, in the same fleece as the body.
        head.AddChild(new MeshInstance3D { Mesh = WoolCap, MaterialOverride = wool });
        body.AddChild(head);

        var hips = new[] { new Vector3(-0.15f, 0.46f, 0.26f), new Vector3(0.15f, 0.46f, 0.26f), new Vector3(-0.15f, 0.46f, -0.26f), new Vector3(0.15f, 0.46f, -0.26f) };
        for (int i = 0; i < 4; i++)
        {
            legs[i] = new Node3D { Position = hips[i] };
            legs[i].AddChild(new MeshInstance3D { Mesh = legMesh, MaterialOverride = faceMaterial });
            body.AddChild(legs[i]);
        }
        AddChild(body);

        // Not every sheep is the same size.
        Scale = Vector3.One * (0.9f + Terrain.Hash01((int)id, 3, 9) * 0.2f);
    }

    public void Apply(Animal a, bool snap)
    {
        to = new Vector2(a.X, a.Z);
        targetHeading = a.Heading;
        if (snap)
        {
            from = to;
            glide = 1f;
            Position = new Vector3(a.X, terrain.HeightAt(a.X, a.Z), a.Z);
            lastPosition = Position;
            Rotation = new Vector3(0, a.Heading, 0);
        }
        else
        {
            from = new Vector2(Position.X, Position.Z);
            glide = 0f;
        }
        if (a.State == AnimalState.Run && state != AnimalState.Run && !snap) hop = 0f;
        woolTarget = a.Fleece / 100f;
        if (snap) wool = woolTarget;
        // Just shorn: the fleece comes off in a soft burst of tufts.
        else if (woolTarget < wool - 0.5f) Tufts();
        state = a.State;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        float time = Time.GetTicksMsec() / 1000f + seed;

        glide = Mathf.Min(glide + dt / GlideSeconds, 1f);
        var flat = from.Lerp(to, glide);
        float ground = terrain.HeightAt(flat.X, flat.Y);
        Position = new Vector3(flat.X, Mathf.Lerp(Position.Y, ground, 1f - Mathf.Exp(-12f * dt)), flat.Y);
        Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, targetHeading, 1f - Mathf.Exp(-7f * dt)), 0);

        var moved = Position - lastPosition;
        moved.Y = 0;
        lastPosition = Position;
        speed = Mathf.Lerp(speed, Mathf.Min(moved.Length() / Mathf.Max(dt, 1e-4f), 6f), 1f - Mathf.Exp(-8f * dt));

        float walk = Mathf.Clamp(speed / 1.0f, 0f, 1f);
        float run = Mathf.Clamp((speed - 2f) / 1.5f, 0f, 1f);
        gait += dt * (speed * 5.5f + run * 4f);

        // Walking moves diagonal legs together; running bounds front pair, then back pair.
        float s = Mathf.Sin(gait) * 0.45f * walk;
        float front = Mathf.Sin(gait) * 0.7f, back = Mathf.Sin(gait + 2.2f) * 0.7f;
        legs[0].Rotation = new Vector3(Mathf.Lerp(s, front, run), 0, 0);
        legs[1].Rotation = new Vector3(Mathf.Lerp(-s, front, run), 0, 0);
        legs[2].Rotation = new Vector3(Mathf.Lerp(-s, back, run), 0, 0);
        legs[3].Rotation = new Vector3(Mathf.Lerp(s, back, run), 0, 0);

        // Startled hop, bouncing run, gentle bob while walking, slow breathing at rest.
        hop = Mathf.Min(hop + dt, HopSeconds);
        float hopLift = Mathf.Sin(Mathf.Pi * hop / HopSeconds) * 0.28f;
        float bounce = Mathf.Abs(Mathf.Sin(gait)) * (0.025f * walk * (1f - run) + 0.12f * run);
        float breathe = Mathf.Sin(time * 1.7f) * 0.006f * (1f - walk);
        body.Position = new Vector3(0, hopLift + bounce, 0);
        body.Scale = new Vector3(1f + breathe, 1f - breathe * 0.5f, 1f);
        body.Rotation = new Vector3(-run * Mathf.Cos(gait) * 0.08f, 0, 0);

        // Head down to nibble when grazing, up and looking about when idle or alarmed.
        float targetPitch, targetYaw = 0f;
        if (state == AnimalState.Graze && speed < 0.3f)
        {
            // Every so often a grazing sheep looks up to chew and glance around.
            bool lookUp = Mathf.Sin(time * 0.31f) > 0.82f;
            targetPitch = lookUp ? 0.05f : 0.95f + Mathf.Sin(time * 7f) * 0.05f;
            if (lookUp) targetYaw = Mathf.Sin(time * 0.7f) * 0.5f;
        }
        else if (state == AnimalState.Run)
            targetPitch = -0.15f;
        else if (state == AnimalState.Idle)
        {
            targetPitch = -0.08f;
            targetYaw = Mathf.Sin(time * 0.45f) * 0.6f;
        }
        else
            targetPitch = 0.15f;
        pitch = Mathf.Lerp(pitch, targetPitch, 1f - Mathf.Exp(-4f * dt));
        yaw = Mathf.Lerp(yaw, targetYaw, 1f - Mathf.Exp(-3f * dt));
        head.Rotation = new Vector3(pitch, yaw, 0);

        // Wool shrinks away quickly when shorn and creeps back as it regrows.
        wool = Mathf.Lerp(wool, woolTarget, 1f - Mathf.Exp(-(woolTarget < wool ? 10f : 1f) * dt));
        fleece.Scale = Vector3.One * Mathf.Lerp(0.6f, 1f, wool);
    }

    void Tufts()
    {
        var puff = new CpuParticles3D
        {
            Emitting = true,
            OneShot = true,
            Amount = 18,
            Lifetime = 1.4f,
            Explosiveness = 0.85f,
            Direction = Vector3.Up,
            Spread = 70f,
            InitialVelocityMin = 0.8f,
            InitialVelocityMax = 1.8f,
            Gravity = new Vector3(0, -1.6f, 0),
            ScaleAmountMin = 0.7f,
            ScaleAmountMax = 1.3f,
            Mesh = new SphereMesh { Radius = 0.05f, Height = 0.08f, RadialSegments = 6, Rings = 3, Material = Look.Solid(woolColor) },
            Position = new Vector3(0, 0.7f, 0),
        };
        AddChild(puff);
        GetTree().CreateTimer(1.8).Timeout += puff.QueueFree;
    }

    // --- Meshes, built once and shared by every sheep ----------------------

    static Mesh? woolCap;
    static Mesh WoolCap => woolCap!;

    static void BuildMeshes()
    {
        if (woolMesh != null) return;
        faceMaterial = Look.Solid(Face);

        // A fleece made of overlapping puffs around a round core.
        var st = Begin();
        var core = new Vector3(0.30f, 0.27f, 0.45f);
        var center = new Vector3(0, 0.66f, 0);
        Ellipsoid(st, center, core * 0.92f, 1f, 12, 7);
        const int puffs = 26;
        for (int i = 0; i < puffs; i++)
        {
            // Spread puffs evenly over the core with a golden-angle spiral.
            float y = 1f - (i + 0.5f) / puffs * 2f;
            if (y < -0.55f) continue;
            float r = Mathf.Sqrt(1f - y * y);
            float a = i * 2.39996f;
            var dir = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
            float size = 0.11f + Terrain.Hash01(i, 1, 77) * 0.08f;
            float shade = 0.9f + Terrain.Hash01(i, 2, 77) * 0.1f;
            Ellipsoid(st, center + dir * core * 0.82f, Vector3.One * size, shade, 9, 5);
        }
        // A stubby tail.
        Ellipsoid(st, center + new Vector3(0, 0.04f, -0.48f), new Vector3(0.08f, 0.09f, 0.07f), 0.96f, 8, 5);
        woolMesh = st.Commit();

        st = Begin();
        Ellipsoid(st, center + new Vector3(0, -0.03f, 0), core * 0.78f, 1f, 12, 7);
        skinMesh = st.Commit();
        skinMaterial = Look.Solid(Skin);

        st = Begin();
        // Skull, muzzle and ears that stick out sideways.
        Ellipsoid(st, new Vector3(0, 0.02f, 0.13f), new Vector3(0.105f, 0.115f, 0.16f), 1f, 10, 6);
        Ellipsoid(st, new Vector3(0, -0.05f, 0.25f), new Vector3(0.075f, 0.075f, 0.085f), 0.9f, 9, 5);
        Ellipsoid(st, new Vector3(-0.14f, 0.05f, 0.08f), new Vector3(0.085f, 0.028f, 0.042f), 1.1f, 8, 4);
        Ellipsoid(st, new Vector3(0.14f, 0.05f, 0.08f), new Vector3(0.085f, 0.028f, 0.042f), 1.1f, 8, 4);
        headMesh = st.Commit();

        st = Begin();
        Ellipsoid(st, new Vector3(0, 0.11f, 0.06f), new Vector3(0.1f, 0.07f, 0.09f), 1f, 9, 5);
        Ellipsoid(st, new Vector3(-0.05f, 0.08f, -0.01f), new Vector3(0.08f, 0.08f, 0.08f), 0.95f, 8, 5);
        Ellipsoid(st, new Vector3(0.05f, 0.08f, -0.01f), new Vector3(0.08f, 0.08f, 0.08f), 0.97f, 8, 5);
        woolCap = st.Commit();

        st = Begin();
        Ellipsoid(st, new Vector3(0, -0.22f, 0), new Vector3(0.045f, 0.24f, 0.045f), 1f, 7, 4);
        Ellipsoid(st, new Vector3(0, -0.43f, 0.01f), new Vector3(0.052f, 0.04f, 0.058f), 0.7f, 7, 3);
        legMesh = st.Commit();
    }

    static SurfaceTool Begin()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        return st;
    }

    static void Ellipsoid(SurfaceTool st, Vector3 center, Vector3 radii, float shade, int segments, int rings)
    {
        var arrays = new SphereMesh { Radius = 1f, Height = 2f, RadialSegments = segments, Rings = rings }.GetMeshArrays();
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        var normals = (Vector3[])arrays[(int)Mesh.ArrayType.Normal];
        var indices = (int[])arrays[(int)Mesh.ArrayType.Index];
        var color = new Color(shade, shade, shade);
        foreach (int i in indices)
        {
            st.SetColor(color);
            st.SetNormal((normals[i] / radii).Normalized());
            st.AddVertex(center + verts[i] * radii);
        }
    }
}
