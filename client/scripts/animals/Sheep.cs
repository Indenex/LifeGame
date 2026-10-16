using System.Collections.Generic;
using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// A round, woolly sheep with a dark face and legs, or a red deer (animals-14): slim
// and long-legged, red-brown with a pale rump, the stags antlered. It glides between
// the positions the server sends and animates from how fast it is actually moving:
// legs swing when walking, it bounds when running, hops when startled, and drops
// its head to nibble grass when grazing. Both share the skeleton and animation.
public partial class Sheep : Node3D
{
    // Slightly longer than the server's half-second tick, so a sheep is still
    // gliding when the next update arrives and never stops between steps.
    const float GlideSeconds = 0.6f;
    const float HopSeconds = 0.35f;

    readonly Terrain terrain;
    // One skinned mesh per sheep: the parts used to be separate nodes, eight draw
    // calls a sheep. Now they are bones of one skeleton, and the mesh has two
    // surfaces (wool, and the skin, face and legs), so two draw calls.
    readonly Skeleton3D skeleton = new();
    const int BodyBone = 0, FleeceBone = 1, HeadBone = 2, FirstLegBone = 3;
    static readonly Vector3 HeadAt = new(0, 0.8f, 0.4f);
    static readonly Vector3[] Hips = { new(-0.15f, 0.46f, 0.26f), new(0.15f, 0.46f, 0.26f), new(-0.15f, 0.46f, -0.26f), new(0.15f, 0.46f, -0.26f) };
    // A deer stands on longer legs and carries its head high on a long neck.
    static readonly Vector3 DeerHeadAt = new(0, 1.32f, 0.62f);
    static readonly Vector3[] DeerHips = { new(-0.13f, 0.82f, 0.38f), new(0.13f, 0.82f, 0.38f), new(-0.13f, 0.82f, -0.38f), new(0.13f, 0.82f, -0.38f) };
    readonly float seed;

    Vector2 from, to;
    float glide = 1f, targetHeading, speed, gait, hop = HopSeconds, pitch, yaw;
    Vector3 lastPosition;
    AnimalState state = AnimalState.Graze;
    Color woolColor;
    float wool = 1f, woolTarget = 1f;
    // Grown size (varies per sheep), and how grown up this one is (lambs are small).
    readonly float adultSize;
    float growth = 1f, growthTarget = 1f;
    // Lying down for good: -1 while alive, then seconds since it lay down.
    float lying = -1f;
    // Lambs skip about now and then: seconds until the next little hop.
    float frolic = 4f;
    // Fading away after butchering: -1 until then.
    float fading = -1f;
    const float LieSeconds = 1.4f, FadeSeconds = 1.2f;

    static readonly Color Face = new(0.17f, 0.14f, 0.12f);
    // Mostly cream, some oatmeal, and the odd dark sheep.
    static readonly Color[] Fleeces =
    {
        // Unwashed wool, not paper white: a brighter fleece glows against the
        // grass at dusk and at night instead of dimming with everything else.
        new(0.74f, 0.71f, 0.63f),
        new(0.74f, 0.71f, 0.63f),
        new(0.74f, 0.71f, 0.63f),
        new(0.68f, 0.62f, 0.52f),
        new(0.68f, 0.62f, 0.52f),
        new(0.74f, 0.71f, 0.63f),
        new(0.74f, 0.71f, 0.63f),
        new(0.34f, 0.27f, 0.22f),
    };
    static readonly Dictionary<int, Material> WoolMaterials = new();
    static ArrayMesh? sheepMesh;
    // Red deer: coats from red-brown to grey-brown; the rump, belly and throat are pale.
    static readonly Color[] Coats = { new(0.47f, 0.30f, 0.19f), new(0.50f, 0.33f, 0.21f), new(0.42f, 0.32f, 0.24f) };
    static readonly Color DeerPale = new(0.78f, 0.68f, 0.54f);
    static readonly Color Hoof = new(0.15f, 0.12f, 0.10f);
    static readonly Color AntlerColor = new(0.66f, 0.58f, 0.46f);
    static readonly Dictionary<int, Material> CoatMaterials = new();
    static ArrayMesh? deerMesh, stagMesh;
    // Skin, face and legs share one material; their colors ride in the vertices.
    static Material? plainMaterial;
    static readonly Color Skin = new(0.80f, 0.66f, 0.58f);

    // Wool takes the painted shader's relief, so the crevices between curls shade
    // in like the ground and rocks do.
    static ShaderMaterial WoolLook(Color color)
    {
        var material = Look.Solid(color);
        material.SetShaderParameter("detail_scale", 3.0f);
        material.SetShaderParameter("bump", 1.2f);
        material.SetShaderParameter("cavity", 0.3f);
        return material;
    }

    // About a third of deer are antlered stags; Audio uses this too so the roar
    // comes from the stags that are drawn.
    public static bool IsStag(ulong id) => Terrain.Hash01((int)id, 13, 21) < 0.35f;

    public Sheep(ulong id, Terrain terrain, AnimalSpecies species = AnimalSpecies.Sheep)
    {
        this.terrain = terrain;
        seed = Terrain.Hash01((int)id, 7, 33) * 100f;
        bool deer = species == AnimalSpecies.Deer;
        // About a third of deer are antlered stags.
        bool stag = deer && IsStag(id);
        BuildMeshes();
        if (deer) BuildDeerMeshes();
        var headAt = deer ? DeerHeadAt : HeadAt;
        var hips = deer ? DeerHips : Hips;

        int color = (int)(Terrain.Hash01((int)id, 11, 5) * Fleeces.Length) % Fleeces.Length;
        if (!WoolMaterials.TryGetValue(color, out var wool))
            WoolMaterials[color] = wool = WoolLook(Fleeces[color]);
        woolColor = Fleeces[color];
        if (deer)
        {
            // Coats vary a little, from red-brown to a greyer brown.
            int coat = (int)(Terrain.Hash01((int)id, 17, 3) * Coats.Length) % Coats.Length;
            if (!CoatMaterials.TryGetValue(coat, out wool))
                CoatMaterials[coat] = wool = Look.Solid(Coats[coat]);
            woolColor = Coats[coat];
        }

        // A shorn sheep is a slim pink body; the fleece bone shrinks into it when
        // shorn and swells back out as the wool regrows.
        skeleton.AddBone("body");
        skeleton.AddBone("fleece");
        skeleton.SetBoneParent(FleeceBone, BodyBone);
        skeleton.AddBone("head");
        skeleton.SetBoneParent(HeadBone, BodyBone);
        skeleton.SetBoneRest(HeadBone, new Transform3D(Basis.Identity, headAt));
        for (int i = 0; i < 4; i++)
        {
            skeleton.AddBone($"leg{i}");
            skeleton.SetBoneParent(FirstLegBone + i, BodyBone);
            skeleton.SetBoneRest(FirstLegBone + i, new Transform3D(Basis.Identity, hips[i]));
        }
        skeleton.ResetBonePoses();
        AddChild(skeleton);
        var mesh = new MeshInstance3D { Mesh = deer ? (stag ? stagMesh : deerMesh) : sheepMesh, Skeleton = new NodePath("..") };
        mesh.SetSurfaceOverrideMaterial(0, wool);
        skeleton.AddChild(mesh);

        // Not every sheep is the same size; stags are bigger than hinds.
        adultSize = 0.9f + Terrain.Hash01((int)id, 3, 9) * 0.2f + (stag ? 0.1f : 0f);
        Scale = Vector3.One * adultSize;
    }

    // Lambs are born at 35 and grow to 100. A lamb is a little over half size.
    static float GrownFraction(byte growth) => Mathf.Lerp(0.55f, 1f, Mathf.Clamp((growth - 35) / 65f, 0f, 1f));

    // The animal has died: it settles onto its side and stays there. Kept quiet
    // and gentle on purpose (D-036). Called on a fresh node too, already lying.
    public void LieDown(float x, float z, float heading, byte fleece, byte grown, bool gently)
    {
        to = from = new Vector2(x, z);
        glide = 1f;
        targetHeading = heading;
        woolTarget = fleece / 100f;
        growthTarget = GrownFraction(grown);
        if (lying < 0f && !gently)
        {
            wool = woolTarget;
            growth = growthTarget;
            Position = new Vector3(x, terrain.HeightAt(x, z), z);
            Rotation = new Vector3(0, heading, 0);
        }
        lying = gently ? Mathf.Max(lying, 0f) : LieSeconds;
        state = AnimalState.Idle;
    }

    // Butchered: sink softly into the grass and go.
    public void FadeAway() => fading = 0f;


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
        growthTarget = GrownFraction(a.Growth);
        if (snap) (wool, growth) = (woolTarget, growthTarget);
        // Just shorn: the fleece comes off in a soft burst of tufts.
        else if (woolTarget < wool - 0.5f) Tufts();
        state = a.State;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        float time = Time.GetTicksMsec() / 1000f + seed;
        if (lying >= 0f)
        {
            Lie(dt, time);
            return;
        }

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
        Leg(0, new Vector3(Mathf.Lerp(s, front, run), 0, 0));
        Leg(1, new Vector3(Mathf.Lerp(-s, front, run), 0, 0));
        Leg(2, new Vector3(Mathf.Lerp(-s, back, run), 0, 0));
        Leg(3, new Vector3(Mathf.Lerp(s, back, run), 0, 0));

        // A lamb at rest gives a little skip every so often.
        if (growthTarget < 0.95f && state != AnimalState.Run && speed < 0.4f && (frolic -= dt) <= 0f)
        {
            hop = 0f;
            frolic = 5f + Terrain.Hash01((int)(time * 10f), 5, 3) * 9f;
        }

        // Startled hop, bouncing run, gentle bob while walking, slow breathing at rest.
        hop = Mathf.Min(hop + dt, HopSeconds);
        float hopLift = Mathf.Sin(Mathf.Pi * hop / HopSeconds) * 0.28f;
        float bounce = Mathf.Abs(Mathf.Sin(gait)) * (0.025f * walk * (1f - run) + 0.12f * run);
        float breathe = Mathf.Sin(time * 1.7f) * 0.006f * (1f - walk);
        Body(new Vector3(0, hopLift + bounce, 0), new Vector3(-run * Mathf.Cos(gait) * 0.08f, 0, 0), new Vector3(1f + breathe, 1f - breathe * 0.5f, 1f));

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
        Head(new Vector3(pitch, yaw, 0));

        // Wool shrinks away quickly when shorn and creeps back as it regrows.
        wool = Mathf.Lerp(wool, woolTarget, 1f - Mathf.Exp(-(woolTarget < wool ? 10f : 1f) * dt));
        Fleece(Mathf.Lerp(0.6f, 1f, wool));
        growth = Mathf.Lerp(growth, growthTarget, 1f - Mathf.Exp(-0.5f * dt));
        Scale = Vector3.One * adultSize * growth;
    }

    void Lie(float dt, float time)
    {
        lying = Mathf.Min(lying + dt, LieSeconds);
        // Ease down: legs fold, then the body rolls gently onto its side.
        float t = Mathf.SmoothStep(0f, 1f, lying / LieSeconds);
        float roll = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((lying / LieSeconds - 0.3f) / 0.7f, 0f, 1f));
        float ground = terrain.HeightAt(to.X, to.Y);
        Position = new Vector3(to.X, ground, to.Y);
        Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, targetHeading, 1f - Mathf.Exp(-4f * dt)), 0);
        // The body pivots at the hooves, so rolling over needs a lift to keep the
        // fleece resting on the grass rather than sinking into the ground.
        Body(new Vector3(0, -0.12f * t * (1f - roll) + 0.24f * roll, 0), new Vector3(0, 0, roll * 1.35f), Vector3.One);
        // Legs tuck in, front ones back and hind ones forward, like a sheep asleep.
        for (int i = 0; i < 4; i++)
            Leg(i, new Vector3((i < 2 ? -1.1f : 1.1f) * t, 0, -roll * 0.2f));
        pitch = Mathf.Lerp(pitch, 0.35f, 1f - Mathf.Exp(-3f * dt));
        Head(new Vector3(pitch, 0, 0));
        Fleece(Mathf.Lerp(0.6f, 1f, woolTarget));
        Scale = Vector3.One * adultSize * growthTarget;

        if (fading < 0f) return;
        fading += dt;
        float f = Mathf.Clamp(fading / FadeSeconds, 0f, 1f);
        Scale = Vector3.One * adultSize * growthTarget * (1f - f * 0.6f);
        Position += Vector3.Down * f * 0.3f;
        if (f >= 1f) QueueFree();
    }

    // Bone poses replace what used to be the part nodes' transforms. Euler angles
    // are in Node3D's YXZ order, so the motion is exactly as before.
    void Body(Vector3 position, Vector3 euler, Vector3 scale)
    {
        skeleton.SetBonePosePosition(BodyBone, position);
        skeleton.SetBonePoseRotation(BodyBone, Quaternion.FromEuler(euler));
        skeleton.SetBonePoseScale(BodyBone, scale);
    }

    void Head(Vector3 euler) => skeleton.SetBonePoseRotation(HeadBone, Quaternion.FromEuler(euler));

    void Leg(int i, Vector3 euler) => skeleton.SetBonePoseRotation(FirstLegBone + i, Quaternion.FromEuler(euler));

    void Fleece(float scale) => skeleton.SetBonePoseScale(FleeceBone, Vector3.One * scale);

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

    static void BuildMeshes()
    {
        if (sheepMesh != null) return;
        plainMaterial = Look.Solid(Colors.White);
        sheepMesh = new ArrayMesh();

        // Surface 0, wool: a fleece made of overlapping puffs around a round core,
        // and a tuft on top of the head. Its color comes from the sheep's material.
        var st = Begin();
        var core = new Vector3(0.30f, 0.27f, 0.45f);
        var center = new Vector3(0, 0.66f, 0);
        var white = Colors.White;
        Ellipsoid(st, FleeceBone, Vector3.Zero, white, center, core * 0.92f, 1f, 12, 7);
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
            Ellipsoid(st, FleeceBone, Vector3.Zero, white, center + dir * core * 0.82f, Vector3.One * size, shade, 9, 5);
        }
        // A stubby tail.
        Ellipsoid(st, FleeceBone, Vector3.Zero, white, center + new Vector3(0, 0.04f, -0.48f), new Vector3(0.08f, 0.09f, 0.07f), 0.96f, 8, 5);
        Ellipsoid(st, HeadBone, HeadAt, white, new Vector3(0, 0.11f, 0.06f), new Vector3(0.1f, 0.07f, 0.09f), 1f, 9, 5);
        Ellipsoid(st, HeadBone, HeadAt, white, new Vector3(-0.05f, 0.08f, -0.01f), new Vector3(0.08f, 0.08f, 0.08f), 0.95f, 8, 5);
        Ellipsoid(st, HeadBone, HeadAt, white, new Vector3(0.05f, 0.08f, -0.01f), new Vector3(0.08f, 0.08f, 0.08f), 0.97f, 8, 5);
        st.Commit(sheepMesh);

        // Surface 1: the pink body under the wool, the dark face and the legs.
        st = Begin();
        Ellipsoid(st, BodyBone, Vector3.Zero, Skin, center + new Vector3(0, -0.03f, 0), core * 0.78f, 1f, 12, 7);
        // Skull, muzzle and ears that stick out sideways.
        Ellipsoid(st, HeadBone, HeadAt, Face, new Vector3(0, 0.02f, 0.13f), new Vector3(0.105f, 0.115f, 0.16f), 1f, 10, 6);
        Ellipsoid(st, HeadBone, HeadAt, Face, new Vector3(0, -0.05f, 0.25f), new Vector3(0.075f, 0.075f, 0.085f), 0.9f, 9, 5);
        Ellipsoid(st, HeadBone, HeadAt, Face, new Vector3(-0.14f, 0.05f, 0.08f), new Vector3(0.085f, 0.028f, 0.042f), 1.1f, 8, 4);
        Ellipsoid(st, HeadBone, HeadAt, Face, new Vector3(0.14f, 0.05f, 0.08f), new Vector3(0.085f, 0.028f, 0.042f), 1.1f, 8, 4);
        for (int i = 0; i < 4; i++)
        {
            Ellipsoid(st, FirstLegBone + i, Hips[i], Face, new Vector3(0, -0.22f, 0), new Vector3(0.045f, 0.24f, 0.045f), 1f, 7, 4);
            Ellipsoid(st, FirstLegBone + i, Hips[i], Face, new Vector3(0, -0.43f, 0.01f), new Vector3(0.052f, 0.04f, 0.058f), 0.7f, 7, 3);
        }
        st.Commit(sheepMesh);
        sheepMesh.SurfaceSetMaterial(1, plainMaterial);
    }

    // A red deer's mesh, with the sheep's bones: surface 0 is the coat (its color from
    // the deer's material, like the sheep's wool), surface 1 the pale patches, dark
    // muzzle, hooves and, on a stag, antlers. The fleece bone carries the coat, so the
    // shared animation's breathing and lying down work unchanged.
    static void BuildDeerMeshes()
    {
        if (deerMesh != null) return;
        deerMesh = BuildDeer(false);
        stagMesh = BuildDeer(true);
    }

    static ArrayMesh BuildDeer(bool antlers)
    {
        var mesh = new ArrayMesh();
        var white = Colors.White;
        var center = new Vector3(0, 1.0f, 0);
        var st = Begin();
        // A long, slim barrel of a body, a deep chest, and a neck rising to the head.
        Ellipsoid(st, FleeceBone, Vector3.Zero, white, center, new Vector3(0.21f, 0.24f, 0.55f), 1f, 12, 7);
        Ellipsoid(st, FleeceBone, Vector3.Zero, white, center + new Vector3(0, -0.02f, 0.3f), new Vector3(0.2f, 0.27f, 0.24f), 0.97f, 10, 6);
        for (int k = 0; k < 3; k++)
            Ellipsoid(st, FleeceBone, Vector3.Zero, white, center + new Vector3(0, 0.12f + k * 0.08f, 0.5f + k * 0.04f), new Vector3(0.085f, 0.13f, 0.1f), 0.98f, 8, 5);
        // Head: a tapering skull and muzzle, and big ears set back.
        Ellipsoid(st, HeadBone, DeerHeadAt, white, new Vector3(0, 0.02f, 0.08f), new Vector3(0.085f, 0.095f, 0.15f), 1f, 10, 6);
        Ellipsoid(st, HeadBone, DeerHeadAt, white, new Vector3(0, -0.04f, 0.22f), new Vector3(0.055f, 0.06f, 0.1f), 0.95f, 9, 5);
        foreach (float x in new[] { -1f, 1f })
            Ellipsoid(st, HeadBone, DeerHeadAt, white, new Vector3(x * 0.1f, 0.11f, -0.01f), new Vector3(0.03f, 0.08f, 0.02f), 1f, 7, 4);
        // Legs: long and thin, a little thicker above the knee.
        for (int i = 0; i < 4; i++)
        {
            Ellipsoid(st, FirstLegBone + i, DeerHips[i], white, new Vector3(0, -0.2f, 0), new Vector3(0.05f, 0.22f, 0.06f), 0.95f, 7, 4);
            Ellipsoid(st, FirstLegBone + i, DeerHips[i], white, new Vector3(0, -0.55f, 0), new Vector3(0.028f, 0.25f, 0.03f), 0.9f, 6, 4);
        }
        st.Commit(mesh);

        st = Begin();
        // Pale rump patch and short tail, pale belly and throat, dark nose, hooves.
        Ellipsoid(st, FleeceBone, Vector3.Zero, DeerPale, center + new Vector3(0, 0.02f, -0.5f), new Vector3(0.17f, 0.18f, 0.08f), 1f, 9, 5);
        Ellipsoid(st, FleeceBone, Vector3.Zero, DeerPale, center + new Vector3(0, -0.15f, 0), new Vector3(0.16f, 0.1f, 0.42f), 1f, 10, 5);
        Ellipsoid(st, FleeceBone, Vector3.Zero, DeerPale, center + new Vector3(0, 0.1f, 0.58f), new Vector3(0.06f, 0.1f, 0.06f), 1f, 8, 4);
        Ellipsoid(st, HeadBone, DeerHeadAt, Hoof, new Vector3(0, -0.05f, 0.31f), new Vector3(0.03f, 0.025f, 0.025f), 1f, 7, 4);
        for (int i = 0; i < 4; i++)
            Ellipsoid(st, FirstLegBone + i, DeerHips[i], Hoof, new Vector3(0, -0.79f, 0.01f), new Vector3(0.032f, 0.03f, 0.04f), 1f, 6, 3);
        if (antlers)
            foreach (float x in new[] { -1f, 1f })
            {
                // A main beam sweeping up and back, with three tines forward.
                for (int k = 0; k < 5; k++)
                    Ellipsoid(st, HeadBone, DeerHeadAt, AntlerColor, new Vector3(x * (0.06f + k * 0.035f), 0.14f + k * 0.07f, -0.02f - k * 0.03f), new Vector3(0.018f, 0.045f, 0.018f), 1f, 6, 3);
                for (int k = 0; k < 3; k++)
                    Ellipsoid(st, HeadBone, DeerHeadAt, AntlerColor, new Vector3(x * (0.09f + k * 0.04f), 0.2f + k * 0.08f, 0.04f - k * 0.02f), new Vector3(0.014f, 0.014f, 0.05f), 1f, 5, 3);
            }
        st.Commit(mesh);
        mesh.SurfaceSetMaterial(1, plainMaterial);
        return mesh;
    }

    static SurfaceTool Begin()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        return st;
    }

    // An ellipsoid in a part's own space (center and radii), placed at that part's
    // rest offset and bound wholly to its bone. The part color is multiplied into
    // the vertex color, which the painted shader reads as sRGB.
    static void Ellipsoid(SurfaceTool st, int bone, Vector3 offset, Color part, Vector3 center, Vector3 radii, float shade, int segments, int rings)
    {
        var arrays = new SphereMesh { Radius = 1f, Height = 2f, RadialSegments = segments, Rings = rings }.GetMeshArrays();
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        var normals = (Vector3[])arrays[(int)Mesh.ArrayType.Normal];
        var indices = (int[])arrays[(int)Mesh.ArrayType.Index];
        // The shader raises vertex colors to 2.2; this undoes that for the part color
        // so it matches what the part's own material (an sRGB albedo) used to give.
        var lin = part.SrgbToLinear();
        float g = 1f / 2.2f;
        var color = new Color(Mathf.Pow(lin.R, g) * shade, Mathf.Pow(lin.G, g) * shade, Mathf.Pow(lin.B, g) * shade);
        var bones = new[] { bone, 0, 0, 0 };
        var weights = new[] { 1f, 0f, 0f, 0f };
        foreach (int i in indices)
        {
            st.SetColor(color);
            st.SetBones(bones);
            st.SetWeights(weights);
            st.SetNormal((normals[i] / radii).Normalized());
            st.AddVertex(offset + center + verts[i] * radii);
        }
    }
}
