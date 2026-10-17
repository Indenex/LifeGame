using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Tool work is felt in the world (atmosphere-30, from Justin's "interact with the
// world more"): pale wood chips fly from the trunk on each axe hit, dirt clods and a
// little dust kick up on each dig, and a felled tree comes down in a puff of dust and
// a last shower of leaves, coloured by the season. Painted and simple: small faceted
// chips, soft round puffs, all short-lived one-shots that free themselves.
//
// Animation calls Atmosphere.ToolContact at the frame a swing lands (with the motion,
// the tool head's world position and the way the worker faces); a felled tree is
// read straight from the ChoppedTree table.
//
// Command line: --impacts=seconds throws a chop, a dig and a falling tree's dust
// and leaves in front of the player that often, for screenshots.
public partial class Atmosphere
{
    // Called by Animation when a tool strikes: the motion, where the tool head is,
    // and the way the worker faces (flat). Set once the atmosphere is ready.
    public static System.Action<WorkMotion, Vector3, Vector3>? ToolContact;

    // Effects only within this many metres of the player.
    const float ImpactReach = 50f;

    static readonly Color ChipLight = new(0.80f, 0.64f, 0.40f), ChipBark = new(0.36f, 0.25f, 0.17f);
    static readonly Color Clod = new(0.34f, 0.25f, 0.16f), Dust = new(0.72f, 0.64f, 0.50f);

    double connectedAt = -1;
    float? argImpacts;
    float impactClock;
    int impactStep;

    void ReadyImpacts()
    {
        ToolContact = OnToolContact;
        argImpacts = ArgFloat("impacts");
    }

    // Only for --impacts: take turns at a chop, a dig and a tree coming down.
    void UpdateImpacts(float dt)
    {
        if (argImpacts is not { } every || Player is not { } p || !p.IsInsideTree()) return;
        impactClock += dt;
        if (impactClock < every) return;
        impactClock = 0f;
        var facing = -p.GlobalBasis.Z;
        facing.Y = 0f;
        facing = facing.LengthSquared() > 1e-4f ? facing.Normalized() : Vector3.Forward;
        var ahead = p.GlobalPosition + facing * 0.9f;
        switch (impactStep++ % 3)
        {
            case 0: OnToolContact(WorkMotion.Chop, ahead + Vector3.Up * 0.9f, facing); break;
            case 1: OnToolContact(WorkMotion.Dig, ahead, facing); break;
            default:
            {
                var at = p.GlobalPosition + facing * 6f;
                FellAt(new Vector3(at.X, terrain?.HeightAt(at.X, at.Z) ?? at.Y, at.Z));
                break;
            }
        }
    }

    void ConnectImpacts(DbConnection conn)
    {
        connectedAt = Time.GetTicksMsec() / 1000.0;
        conn.Db.ChoppedTree.OnUpdate += (_, before, after) => { if (!before.Felled && after.Felled) TreeFell(after); };
        // A tree felled since we connected arrives as a new row; the ones felled
        // before are just the world as it is.
        conn.Db.ChoppedTree.OnInsert += (_, t) => { if (t.Felled && Time.GetTicksMsec() / 1000.0 - connectedAt > 5.0) TreeFell(t); };
    }

    void OnToolContact(WorkMotion motion, Vector3 at, Vector3 facing)
    {
        if (!IsInsideTree() || !Near(at)) return;
        facing.Y = 0f;
        facing = facing.LengthSquared() > 1e-4f ? facing.Normalized() : Vector3.Forward;
        switch (motion)
        {
            case WorkMotion.Chop:
                // Chips spring back out of the cut, toward and past the worker's side.
                Burst(at, (-facing + Vector3.Up * 0.8f).Normalized(), 55f, 2.2f, 4.2f, 12, 0.9f,
                    new BoxMesh { Size = new Vector3(0.07f, 0.02f, 0.045f) }, ChipLight, ChipBark, spin: true);
                break;
            case WorkMotion.Dig:
            {
                var ground = new Vector3(at.X, terrain?.HeightAt(at.X, at.Z) ?? at.Y, at.Z);
                Burst(ground, (Vector3.Up * 1.4f - facing * 0.4f).Normalized(), 45f, 1.4f, 2.6f, 8, 0.8f,
                    Faceted(new SphereMesh { Radius = 0.035f, Height = 0.06f, RadialSegments = 5, Rings = 3 }), Clod, Clod.Darkened(0.2f), spin: true);
                Puff(ground, 0.35f, 6, 1.1f, Dust, 0.45f);
                break;
            }
        }
    }

    void TreeFell(ChoppedTree t)
    {
        var (x, z) = Trees.Trunk(t.CellX, t.CellZ);
        FellAt(new Vector3(x, terrain?.HeightAt(x, z) ?? 0f, z));
    }

    void FellAt(Vector3 foot)
    {
        if (!Near(foot)) return;
        // Dust rolls out round the stump as the trunk hits the ground.
        Puff(foot + Vector3.Up * 0.2f, 1.6f, 18, 2.2f, Dust, 1.2f);
        // A last shower of leaves from where the crown was, unless the tree was
        // already bare; green in summer, turned in autumn.
        float bare = (1f - Mathf.SmoothStep(-0.85f, -0.35f, Seasons.Warmth(Year))) * (Seasons.Autumn(Year) > 0.3f ? 1f : 0f);
        if (bare > 0.9f) return;
        float turned = Mathf.Clamp(Seasons.Autumn(Year) * 1.6f, 0f, 1f);
        var green = new Color(0.30f, 0.42f, 0.15f);
        var light = green.Lerp(new Color(0.70f, 0.40f, 0.08f), turned);
        var dark = new Color(0.20f, 0.30f, 0.10f).Lerp(new Color(0.55f, 0.18f, 0.06f), turned);
        var leaves = Burst(foot + Vector3.Up * 6f, Vector3.Up, 180f, 0.2f, 1.0f, (int)(40 * (1f - bare)), 4.5f,
            new QuadMesh { Size = new Vector2(0.1f, 0.07f) }, light, dark, spin: true);
        leaves.EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere;
        leaves.EmissionSphereRadius = 2.2f;
        leaves.Gravity = new Vector3(WindDirection.X * 0.6f, -1.2f, WindDirection.Y * 0.6f);
        leaves.DampingMin = 1.2f;
        leaves.DampingMax = 1.8f;
    }

    bool Near(Vector3 at) => Player is not { } p || !p.IsInsideTree() || p.GlobalPosition.DistanceSquaredTo(at) < ImpactReach * ImpactReach;

    // A one-shot spray of small pieces in a cone, each one of two colours, freed
    // once it's done.
    CpuParticles3D Burst(Vector3 at, Vector3 direction, float spread, float speedMin, float speedMax, int amount, float life,
        Mesh mesh, Color a, Color b, bool spin)
    {
        mesh.SurfaceSetMaterial(0, new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            Roughness = 1f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        });
        var colors = new Gradient { Offsets = new[] { 0f, 0.5f }, Colors = new[] { a, b }, InterpolationMode = Gradient.InterpolationModeEnum.Constant };
        var fx = new CpuParticles3D
        {
            Emitting = false,
            OneShot = true,
            Amount = Mathf.Max(1, amount),
            Lifetime = life,
            Explosiveness = 0.95f,
            Direction = direction,
            Spread = spread,
            InitialVelocityMin = speedMin,
            InitialVelocityMax = speedMax,
            Gravity = new Vector3(0f, -9.8f, 0f),
            AngleMin = 0f,
            AngleMax = 360f,
            AngularVelocityMin = spin ? -540f : 0f,
            AngularVelocityMax = spin ? 540f : 0f,
            ParticleFlagRotateY = spin,
            ScaleAmountMin = 0.7f,
            ScaleAmountMax = 1.3f,
            ColorInitialRamp = colors,
            Mesh = mesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            TopLevel = true,
        };
        Spawn(fx, at, life);
        return fx;
    }

    // A soft round puff of dust that swells and fades.
    void Puff(Vector3 at, float radius, int amount, float life, Color color, float size)
    {
        var fade = new Gradient
        {
            Offsets = new[] { 0f, 0.15f, 1f },
            Colors = new[] { new Color(color, 0f), new Color(color, 0.45f), new Color(color, 0f) },
        };
        var grow = new Curve();
        grow.AddPoint(new Vector2(0f, 0.4f));
        grow.AddPoint(new Vector2(1f, 1f));
        var fx = new CpuParticles3D
        {
            Emitting = false,
            OneShot = true,
            Amount = amount,
            Lifetime = life,
            Explosiveness = 0.9f,
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Ring,
            EmissionRingAxis = Vector3.Up,
            EmissionRingRadius = radius,
            EmissionRingInnerRadius = radius * 0.3f,
            EmissionRingHeight = 0.1f,
            Direction = Vector3.Up,
            Spread = 70f,
            InitialVelocityMin = 0.3f,
            InitialVelocityMax = 0.9f,
            Gravity = new Vector3(WindDirection.X * 0.3f, 0.15f, WindDirection.Y * 0.3f),
            DampingMin = 0.8f,
            DampingMax = 1.2f,
            ScaleAmountMin = 0.8f,
            ScaleAmountMax = 1.3f,
            ScaleAmountCurve = grow,
            ColorRamp = fade,
            Mesh = new QuadMesh
            {
                Size = new Vector2(size, size),
                Material = new StandardMaterial3D
                {
                    VertexColorUseAsAlbedo = true,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
                    AlbedoTexture = SoftDot,
                    Roughness = 1f,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.PerVertex,
                },
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            TopLevel = true,
        };
        Spawn(fx, at, life);
    }

    void Spawn(CpuParticles3D fx, Vector3 at, float life)
    {
        AddChild(fx);
        fx.GlobalPosition = at;
        fx.Restart();
        GetTree().CreateTimer(life + 0.5f).Timeout += fx.QueueFree;
    }

    static Mesh Faceted(PrimitiveMesh mesh)
    {
        var st = new SurfaceTool();
        st.CreateFrom(mesh, 0);
        st.Deindex();
        st.GenerateNormals();
        return st.Commit();
    }

    static GradientTexture2D? softDot;
    static GradientTexture2D SoftDot => softDot ??= new GradientTexture2D
    {
        Width = 64,
        Height = 64,
        Fill = GradientTexture2D.FillEnum.Radial,
        FillFrom = new Vector2(0.5f, 0.5f),
        FillTo = new Vector2(1f, 0.5f),
        Gradient = new Gradient { Offsets = new[] { 0f, 0.6f, 1f }, Colors = new[] { Colors.White, new Color(1f, 1f, 1f, 0.5f), new Color(1f, 1f, 1f, 0f) } },
    };
}
