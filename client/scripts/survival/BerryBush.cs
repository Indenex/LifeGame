using Godot;

namespace LifeGame;

// A low, rounded shrub. When ripe it is dotted with dark red berries; picked,
// it is just leaves until they grow back.
public partial class BerryBush : Node3D
{
    public static readonly Color BerryColor = new(0.55f, 0.08f, 0.16f);
    static readonly Color BushLeaf = new(0.20f, 0.33f, 0.14f);

    static Mesh? lobeMesh, berryMesh, berryDarkMesh, stemMesh;
    readonly Node3D berries = new();

    // A lobe is a squashed sphere: center and radii in the bush's own space.
    readonly record struct Lobe(Vector3 Center, Vector3 Radii)
    {
        public bool Contains(Vector3 p, float margin)
        {
            var d = (p - Center) / (Radii * margin);
            return d.LengthSquared() < 1f;
        }
    }

    public BerryBush(ulong id)
    {
        // Painted leaf clumps (painted-2); berries and stems on the painted twin of their colour.
        lobeMesh ??= PaintedBushes.Clump(PaintedBushes.Tint(BushLeaf));
        berryMesh ??= PaintedMesh(Look.Faceted(new SphereMesh { Radius = 0.04f, Height = 0.08f, RadialSegments = 6, Rings = 3 }, BerryColor));
        berryDarkMesh ??= PaintedMesh(Look.Faceted(new SphereMesh { Radius = 0.04f, Height = 0.08f, RadialSegments = 6, Rings = 3 }, BerryColor.Darkened(0.35f)));
        stemMesh ??= PaintedMesh(Look.Faceted(new CylinderMesh { TopRadius = 0.006f, BottomRadius = 0.009f, Height = 0.12f, RadialSegments = 4 }, Look.Bark));

        int seed = (int)(id % 100_000);
        Rotation = new Vector3(0, Terrain.Hash01(seed, 1, 40) * Mathf.Tau, 0);
        float size = 0.85f + Terrain.Hash01(seed, 2, 40) * 0.35f;
        Scale = Vector3.One * size;

        // A few overlapping lobes read as one soft shrub. Each sits on the ground.
        Vector3[] centers = { new(0, 0.3f, 0), new(0.3f, 0.22f, 0.1f), new(-0.25f, 0.2f, 0.18f), new(0.05f, 0.24f, -0.3f) };
        var lobes = new Lobe[centers.Length];
        for (int i = 0; i < centers.Length; i++)
        {
            float s = 0.75f + Terrain.Hash01(seed, i, 41) * 0.35f;
            var scale = new Vector3(s, s * 0.85f, s);
            lobes[i] = new Lobe(centers[i], new Vector3(0.42f, 0.3f, 0.42f) * scale);
            AddChild(new MeshInstance3D { Mesh = lobeMesh, Position = centers[i], Scale = scale, MaterialOverride = Painted.Bush });
        }

        // Berries hang in small clusters on short stems, tucked into the leaves of
        // the outer, upper surface so they read as part of the bush.
        for (int c = 0; c < 7; c++)
        {
            if (FindSurfaceSpot(lobes, seed, c) is not var (spot, normal)) continue;
            var cluster = new Node3D { Position = spot };
            cluster.Basis = Basis.LookingAt(normal, Mathf.Abs(normal.Y) > 0.95f ? Vector3.Forward : Vector3.Up);
            // The stem runs back into the foliage.
            cluster.AddChild(new MeshInstance3D { Mesh = stemMesh, Position = new Vector3(0, 0, 0.03f), Rotation = new Vector3(Mathf.Pi / 2f, 0, 0) });
            int count = 2 + (int)(Terrain.Hash01(seed, c, 44) * 3f);
            for (int b = 0; b < count; b++)
            {
                float a = b * Mathf.Tau / count + Terrain.Hash01(seed, c * 7 + b, 45);
                float r = count == 1 ? 0 : 0.035f;
                float scale = 0.8f + Terrain.Hash01(seed, c * 7 + b, 46) * 0.45f;
                cluster.AddChild(new MeshInstance3D
                {
                    Mesh = Terrain.Hash01(seed, c * 7 + b, 47) < 0.3f ? berryDarkMesh : berryMesh,
                    // Local -Z points out of the bush; berries sit just proud of the leaves.
                    Position = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r - 0.01f, -0.015f),
                    Scale = Vector3.One * scale,
                });
            }
            berries.AddChild(cluster);
        }
        AddChild(berries);
    }

    // A point on the visible skin of the bush (not buried in another lobe), on its
    // upper or outer side, with the outward surface normal there.
    static (Vector3, Vector3)? FindSurfaceSpot(Lobe[] lobes, int seed, int cluster)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            int k = cluster * 10 + attempt;
            var lobe = lobes[(int)(Terrain.Hash01(seed, k, 48) * lobes.Length) % lobes.Length];
            float a = Terrain.Hash01(seed, k, 42) * Mathf.Tau;
            float up = 0.05f + Terrain.Hash01(seed, k, 43) * 0.8f;
            var dir = new Vector3(Mathf.Cos(a) * (1f - up), up * 1.4f, Mathf.Sin(a) * (1f - up)).Normalized();
            // In among the leaf lobes round the clump's heart, so berries nestle into them.
            var spot = lobe.Center + dir * lobe.Radii * 1.0f;
            if (spot.Y < 0.12f) continue;
            bool buried = false;
            foreach (var other in lobes)
                if (other != lobe && other.Contains(spot, 0.97f)) buried = true;
            if (buried) continue;
            var normal = ((spot - lobe.Center) / (lobe.Radii * lobe.Radii)).Normalized();
            return (spot, normal);
        }
        return null;
    }

    static Mesh PaintedMesh(Mesh mesh)
    {
        if (mesh is ArrayMesh array && array.SurfaceGetMaterial(0) is ShaderMaterial solid) array.SurfaceSetMaterial(0, Painted.Of(solid));
        return mesh;
    }

    public bool Ripe
    {
        get => berries.Visible;
        set => berries.Visible = value;
    }
}
