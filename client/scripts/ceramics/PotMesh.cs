using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Hand-built pots as surfaces of revolution: an outside profile turned around
// the vertical axis, a wall of real thickness and a rounded rim. Every pot is a
// little uneven, seeded by its id, so no two look machine-made.
public static class PotMesh
{
    const int Segments = 28;
    static readonly Dictionary<(ClayForm, int), ArrayMesh> Cache = new();

    // Outside profile from the base center to the rim, as (radius, height) in meters.
    static readonly Vector2[] PinchProfile =
    {
        new(0.000f, 0.000f), new(0.050f, 0.000f), new(0.075f, 0.012f), new(0.092f, 0.035f),
        new(0.100f, 0.062f), new(0.100f, 0.090f), new(0.094f, 0.112f), new(0.090f, 0.122f),
    };

    static readonly Vector2[] CoilProfile =
    {
        new(0.000f, 0.000f), new(0.060f, 0.000f), new(0.092f, 0.020f), new(0.118f, 0.060f),
        new(0.128f, 0.105f), new(0.122f, 0.150f), new(0.100f, 0.190f), new(0.078f, 0.215f),
        new(0.072f, 0.232f), new(0.080f, 0.248f), new(0.088f, 0.256f),
    };

    // The storage jar (ceramics-6): a wide belly, a narrow neck and a flared rim.
    static readonly Vector2[] JarProfile =
    {
        new(0.000f, 0.000f), new(0.070f, 0.000f), new(0.110f, 0.030f), new(0.145f, 0.090f),
        new(0.160f, 0.160f), new(0.155f, 0.230f), new(0.135f, 0.290f), new(0.100f, 0.335f),
        new(0.082f, 0.360f), new(0.085f, 0.385f), new(0.095f, 0.400f),
    };

    // The oil lamp (ceramics-9): a low, open bowl; the spout is pinched in Turn's wobble.
    static readonly Vector2[] LampProfile =
    {
        new(0.000f, 0.000f), new(0.035f, 0.000f), new(0.055f, 0.010f), new(0.066f, 0.024f),
        new(0.068f, 0.036f), new(0.064f, 0.044f),
    };

    // The clay trough (ceramics-13): a wide, shallow basin with a rounded lip.
    static readonly Vector2[] TroughProfile =
    {
        new(0.000f, 0.000f), new(0.090f, 0.000f), new(0.140f, 0.014f), new(0.176f, 0.050f),
        new(0.190f, 0.092f), new(0.184f, 0.120f),
    };

    public static float Height(ClayForm form) => form switch
    {
        ClayForm.PinchPot => 0.122f,
        ClayForm.CoilPot => 0.256f,
        ClayForm.StorageJar => 0.400f,
        ClayForm.Lamp => 0.044f,
        ClayForm.Tile => 0.030f,
        ClayForm.Trough => 0.120f,
        _ => 0.08f,
    };

    // Where water sits when the pot is full, as a radius at a height.
    public static Vector2 WaterLine(ClayForm form) => form switch
    {
        ClayForm.PinchPot => new(0.080f, 0.095f),
        ClayForm.CoilPot => new(0.064f, 0.222f),
        _ => Vector2.Zero,
    };

    // A few variants per form are enough to look hand-made without one mesh per pot.
    public static ArrayMesh For(ClayForm form, ulong id)
    {
        int variant = (int)(id % 4);
        if (Cache.TryGetValue((form, variant), out var mesh)) return mesh;
        mesh = form switch
        {
            ClayForm.Brick => Brick(variant),
            ClayForm.CoilPot => Turn(CoilProfile, true, variant),
            ClayForm.StorageJar => Turn(JarProfile, true, variant),
            ClayForm.Lamp => Turn(LampProfile, false, variant),
            ClayForm.Tile => Tile(variant),
            ClayForm.Trough => Turn(TroughProfile, true, variant),
            _ => Turn(PinchProfile, false, variant),
        };
        Cache[(form, variant)] = mesh;
        return mesh;
    }

    static ArrayMesh Turn(Vector2[] outer, bool coils, int variant)
    {
        const float wall = 0.011f;
        float top = outer[^1].Y;

        // Outside up to the rim, a rounded lip, then back down the inside.
        var profile = new List<Vector2>();
        foreach (var p in Subdivide(outer, 3)) profile.Add(p);
        var rim = outer[^1];
        for (int i = 1; i < 4; i++)
        {
            float a = i / 4f * Mathf.Pi;
            profile.Add(new Vector2(rim.X - wall / 2f + Mathf.Cos(a) * wall / 2f, rim.Y + Mathf.Sin(a) * wall * 0.6f));
        }
        var inner = Subdivide(outer, 3);
        for (int i = inner.Count - 1; i >= 0; i--)
        {
            var p = inner[i];
            float r = Mathf.Max(0f, p.X - wall);
            profile.Add(new Vector2(r, Mathf.Max(p.Y, wall * 0.9f)));
        }

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        int rings = profile.Count;
        var verts = new Vector3[rings, Segments + 1];
        for (int j = 0; j < rings; j++)
        {
            var p = profile[j];
            for (int s = 0; s <= Segments; s++)
            {
                float angle = s % Segments / (float)Segments * Mathf.Tau;
                float h = p.Y / top;
                // Thumb-pressed unevenness, stronger toward the rim, plus a slight lean.
                float wobble = 1f + (Wobble(angle, h, variant) - 0.5f) * 0.09f * (0.4f + h);
                float ridge = coils && p.X > 0.02f ? Mathf.Abs(Mathf.Sin(h * Mathf.Pi * 6f)) * 0.004f : 0f;
                float r = (p.X + ridge) * wobble;
                float lean = h * 0.006f * (variant - 1.5f);
                verts[j, s] = new Vector3(Mathf.Cos(angle) * r + lean, p.Y, Mathf.Sin(angle) * r);
            }
        }
        for (int j = 0; j < rings - 1; j++)
            for (int s = 0; s < Segments; s++)
            {
                // UV.y carries height on the pot (0 base, 1 rim) for the shader.
                Quad(st, verts[j, s], verts[j, s + 1], verts[j + 1, s + 1], verts[j + 1, s], profile[j].Y / top, profile[j + 1].Y / top);
            }
        st.GenerateNormals();
        return st.Commit();
    }

    static void Quad(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float ha, float hc)
    {
        void V(Vector3 v, float h)
        {
            st.SetUV(new Vector2(0, h));
            st.AddVertex(v);
        }
        V(a, ha); V(b, ha); V(c, hc);
        V(a, ha); V(c, hc); V(d, hc);
    }

    // A curved roof tile (ceramics-10): a thin slab arched across its width, about
    // 0.2 m by 0.3 m once PotView.Size is applied, with hand-pressed unevenness.
    static ArrayMesh Tile(int variant)
    {
        const int across = 8, along = 6;
        const float halfW = 0.050f, halfL = 0.075f, arch = 0.012f, thick = 0.0045f, baseY = 0.002f;
        float Arch(float x, float z)
        {
            float u = x / halfW;
            float n = Wobble(x * 25f, z * 17f, variant) - 0.5f;
            return baseY + arch * (1f - u * u) + n * 0.003f;
        }
        var xs = new float[across + 1];
        var zs = new float[along + 1];
        for (int i = 0; i <= across; i++) xs[i] = -halfW + 2f * halfW * i / across;
        for (int j = 0; j <= along; j++) zs[j] = -halfL + 2f * halfL * j / along;
        Vector3 Top(int i, int j) => new(xs[i], Arch(xs[i], zs[j]) + thick, zs[j]);
        Vector3 Bot(int i, int j) => new(xs[i], Arch(xs[i], zs[j]), zs[j]);
        const float top = 0.03f;
        float H(Vector3 v) => Mathf.Clamp(v.Y / top, 0f, 1f);
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < across; i++)
            for (int j = 0; j < along; j++)
            {
                // Seen from above (x right, z down) clockwise is front-facing.
                Quad(st, Top(i, j), Top(i + 1, j), Top(i + 1, j + 1), Top(i, j + 1), H(Top(i, j)), H(Top(i, j + 1)));
                Quad(st, Bot(i, j), Bot(i, j + 1), Bot(i + 1, j + 1), Bot(i + 1, j), H(Bot(i, j)), H(Bot(i, j + 1)));
            }
        for (int j = 0; j < along; j++)
        {
            Quad(st, Top(0, j), Top(0, j + 1), Bot(0, j + 1), Bot(0, j), H(Top(0, j)), H(Bot(0, j)));
            Quad(st, Top(across, j + 1), Top(across, j), Bot(across, j), Bot(across, j + 1), H(Top(across, j)), H(Bot(across, j)));
        }
        for (int i = 0; i < across; i++)
        {
            Quad(st, Top(i, along), Top(i + 1, along), Bot(i + 1, along), Bot(i, along), H(Top(i, along)), H(Bot(i, along)));
            Quad(st, Top(i + 1, 0), Top(i, 0), Bot(i, 0), Bot(i + 1, 0), H(Top(i, 0)), H(Bot(i, 0)));
        }
        st.GenerateNormals();
        return st.Commit();
    }

    // A sun-dried brick: a slab with softened, slightly uneven corners.
    static ArrayMesh Brick(int variant)
    {
        var box = new BoxMesh { Size = new Vector3(0.30f, 0.08f, 0.15f), SubdivideWidth = 3, SubdivideDepth = 2, SubdivideHeight = 1 };
        var st = new SurfaceTool();
        st.CreateFrom(box, 0);
        var arrays = st.CommitToArrays();
        var v = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        for (int i = 0; i < v.Length; i++)
        {
            float n = Wobble(v[i].X * 20f, v[i].Z * 7f, variant) - 0.5f;
            v[i] = new Vector3(v[i].X * 0.96f, v[i].Y + 0.04f + n * 0.008f, v[i].Z * (0.96f + n * 0.04f));
        }
        arrays[(int)Mesh.ArrayType.Vertex] = v;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        var smooth = new SurfaceTool();
        smooth.CreateFrom(mesh, 0);
        smooth.Deindex();
        smooth.GenerateNormals();
        return smooth.Commit();
    }

    // Catmull-Rom through the profile points, so the silhouette is smooth.
    static List<Vector2> Subdivide(Vector2[] points, int steps)
    {
        var result = new List<Vector2>();
        for (int i = 0; i < points.Length - 1; i++)
        {
            var p0 = points[Math.Max(i - 1, 0)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Math.Min(i + 2, points.Length - 1)];
            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)steps;
                var p = 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
                result.Add(new Vector2(Mathf.Max(0f, p.X), p.Y));
            }
        }
        result.Add(points[^1]);
        return result;
    }

    // Smooth, periodic-in-angle noise for the hand-made wobble.
    static float Wobble(float angle, float h, int variant)
    {
        float a = Mathf.Sin(angle * 2f + variant * 1.7f + h * 2.1f) * 0.5f + 0.5f;
        float b = Mathf.Sin(angle * 3f - variant * 2.3f + h * 4.3f) * 0.5f + 0.5f;
        float c = Mathf.Sin(angle * 5f + variant * 0.9f - h * 3.1f) * 0.5f + 0.5f;
        return a * 0.5f + b * 0.3f + c * 0.2f;
    }
}
