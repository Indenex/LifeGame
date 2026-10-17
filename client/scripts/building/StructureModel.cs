using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The pieces a structure is made of, in the order they go up. Each piece belongs
// to a stage; as materials arrive, the matching share of that stage's pieces
// appears, so you watch the frame, ribs and thatch go on one by one.
// Pieces are built in the structure's own frame: +Z is the open front or door,
// y = 0 is the ground at the footprint's center.
public static partial class StructureModel
{
    public sealed class Piece
    {
        // -1 is the laid-out site (pegs and string), shown until the structure is finished.
        public int Stage;
        public Mesh Mesh = null!;
        public Material Material = null!;
        // Where the piece grows from when it appears.
        public Vector3 Anchor;
    }

    static readonly Dictionary<int, List<Piece>> Cache = new();
    static ShaderMaterial? wood, fiber, thatch, clay, stone;

    internal static ShaderMaterial Wood => wood ??= Tinted(Look.BarkSurface, Look.Bark.Lightened(0.18f));
    static ShaderMaterial Fiber => fiber ??= Tinted(Look.Foliage, Look.LeafWarm.Darkened(0.2f), sway: 0f);
    static ShaderMaterial Thatch => thatch ??= Tinted(Look.Foliage, new Color(0.78f, 0.64f, 0.34f), sway: 0.012f);
    internal static ShaderMaterial Clay => clay ??= Tinted(Look.Ground, Look.Clay.Lightened(0.06f));
    internal static ShaderMaterial Stone => stone ??= Tinted(Look.RockSurface, Look.Rock);
    static ShaderMaterial? weave, paleWood;
    // Freshly cut wood, for things that should catch the eye (a door's bar).
    internal static ShaderMaterial PaleWood => paleWood ??= Tinted(Look.BarkSurface, new Color(0.86f, 0.74f, 0.55f));
    internal static ShaderMaterial Weave => weave ??= Tinted(Look.Foliage, new Color(0.72f, 0.58f, 0.32f), sway: 0f);

    public static int StageCount(int kind) => LifeGame.Shared.Structures.Shape(kind).Stages.Length;

    public static List<Piece> Pieces(int kind)
    {
        if (Cache.TryGetValue(kind, out var pieces)) return pieces;
        // Stations by their shape's station kind, never by number (kinds are numbered by merge order).
        pieces = LifeGame.Shared.Structures.Shape(kind).Station switch
        {
            LifeGame.Shared.StationKind.Table or LifeGame.Shared.StationKind.Repair => CraftingTableModel.Pieces(),
            LifeGame.Shared.StationKind.PottersBench => StationMeshes.PottersBench(),
            LifeGame.Shared.StationKind.Spindle => StationMeshes.Distaff(),
            LifeGame.Shared.StationKind.DryingRack => StationMeshes.DryingRack(),
            _ => kind switch { 1 => ClayHut(), 2 => ClaimMarkerModel.Pieces(), 3 => KilnModel.Pieces(), 4 => Basket(), 5 => BrickHouse(), 6 => JarModel.Pieces(), _ when LifeGame.Shared.Structures.IsIndoor(kind) => Hearth(), _ when LifeGame.Shared.Structures.IsRoom(kind) => LogCabin(), _ when LifeGame.Shared.Structures.IsFence(kind) => Fence(LifeGame.Shared.Structures.IsGate(kind)), _ => LeanTo() },
        };
        Cache[kind] = pieces;
        return pieces;
    }

    // --- Lean-to: two forked uprights, a ridge pole, ribs down to a ground log, thatch.

    static List<Piece> LeanTo()
    {
        var list = new List<Piece>();
        const float hw = 1.5f, hd = 1.25f, ridgeY = 1.7f, ridgeZ = 0.85f;

        Site(list, new[] { new Vector3(-hw, 0, hd), new Vector3(hw, 0, hd), new Vector3(hw, 0, -hd), new Vector3(-hw, 0, -hd) });

        // Frame
        Add(list, 0, Wood, Stick(new Vector3(-1.35f, -0.3f, ridgeZ), new Vector3(-1.3f, ridgeY + 0.12f, ridgeZ + 0.04f), 0.06f, fork: true));
        Add(list, 0, Wood, Stick(new Vector3(1.35f, -0.3f, ridgeZ), new Vector3(1.32f, ridgeY + 0.14f, ridgeZ - 0.03f), 0.06f, fork: true));
        Add(list, 0, Wood, Stick(new Vector3(-1.7f, ridgeY - 0.02f, ridgeZ), new Vector3(1.72f, ridgeY + 0.03f, ridgeZ), 0.055f));
        Add(list, 0, Wood, Stick(new Vector3(-1.55f, 0.06f, -hd), new Vector3(1.55f, 0.08f, -hd + 0.02f), 0.08f));

        // Lashing: vine wrapped where the ridge sits in the forks, and at the middle.
        foreach (float x in new[] { -1.3f, 1.32f, 0f })
            Add(list, 1, Fiber, Wrap(new Vector3(x, ridgeY + 0.01f, ridgeZ), Vector3.Right, 0.085f, 0.16f));

        // Ribs lean from the ground log up over the ridge.
        for (int i = 0; i < 7; i++)
        {
            float x = -1.38f + i * 0.46f;
            float j = (Hash(i, 1) - 0.5f) * 0.08f;
            Add(list, 2, Wood, Stick(new Vector3(x + j, -0.05f, -hd - 0.05f), new Vector3(x - j, ridgeY + 0.2f, ridgeZ + 0.28f), 0.035f));
        }

        // Thatch laid in overlapping rows from the bottom up, like shingles.
        var low = new Vector3(0, 0.02f, -hd - 0.08f);
        var high = new Vector3(0, ridgeY + 0.12f, ridgeZ + 0.2f);
        const int rows = 5;
        for (int r = 0; r < rows; r++)
        {
            float t0 = r / (float)rows - 0.04f, t1 = (r + 1) / (float)rows + 0.1f;
            Add(list, 3, Thatch, ThatchRow(low.Lerp(high, Math.Max(0, t0)), low.Lerp(high, Math.Min(1.04f, t1)), 3.5f, 0.11f + r * 0.012f, r));
        }
        Add(list, 3, Thatch, Wrap(high + new Vector3(0, 0.06f, -0.02f), Vector3.Right, 0.13f, 3.5f, lumpy: true));
        return list;
    }

    // --- Clay hut: a ring of posts, woven wattle, daubed clay walls, a thatched cone.

    static List<Piece> ClayHut()
    {
        var list = new List<Piece>();
        const float r = 1.7f, wallTop = 1.7f, door = 0.5f; // door half-angle in radians
        const int posts = 10;

        var ring = new Vector3[16];
        for (int i = 0; i < ring.Length; i++)
            ring[i] = Around(1.9f, i * Mathf.Tau / ring.Length, 0);
        Site(list, ring);

        // Posts, from one side of the door around to the other, plus two flat threshold stones.
        float Angle(int i) => door + i * (Mathf.Tau - 2 * door) / (posts - 1);
        for (int i = 0; i < posts; i++)
        {
            float a = Angle(i);
            Add(list, 0, Wood, Stick(Around(r, a, -0.3f), Around(r - 0.02f, a + (Hash(i, 2) - 0.5f) * 0.03f, wallTop + 0.08f + Hash(i, 3) * 0.1f), 0.065f));
        }
        Add(list, 0, Stone, Rock(new Vector3(-0.32f, 0.02f, r + 0.05f), new Vector3(0.34f, 0.09f, 0.28f), 4));
        Add(list, 0, Stone, Rock(new Vector3(0.3f, 0.02f, r + 0.02f), new Vector3(0.3f, 0.08f, 0.3f), 5));

        // Wattle: rods woven in and out between the posts, one band at a time.
        const int bands = 6;
        for (int b = 0; b < bands; b++)
        {
            float y = 0.18f + b * (wallTop - 0.3f) / (bands - 1);
            var st = Begin();
            for (int i = 0; i < posts - 1; i++)
            {
                float weave = ((i + b) % 2 == 0 ? 1 : -1) * 0.035f;
                StickInto(st, Around(r + weave, Angle(i) - 0.04f, y), Around(r - weave, Angle(i + 1) + 0.04f, y + 0.02f), 0.025f, Shade(b * 31 + i));
            }
            Add(list, 1, Wood, st.Commit());
        }

        // Daub: clay pressed over the wattle in rows, bottom to top.
        const int courses = 4;
        for (int c = 0; c < courses; c++)
        {
            float y0 = -0.05f + c * (wallTop + 0.05f) / courses, y1 = -0.05f + (c + 1) * (wallTop + 0.05f) / courses;
            Add(list, 2, Clay, Wall(r, 0.17f, y0, y1, door - 0.06f, Mathf.Tau - door + 0.06f, top: c == courses - 1));
        }

        // Roof: rafters to a peak, then a thatched cone in layers from the eaves up.
        const float peak = 3.15f, eave = 2.25f;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.Tau / 8 + 0.2f;
            Add(list, 3, Wood, Stick(Around(eave, a, wallTop - 0.35f), new Vector3(0, peak + 0.1f, 0), 0.04f));
        }
        const int layers = 4;
        for (int l = 0; l < layers; l++)
        {
            float t0 = l / (float)layers, t1 = Math.Min(1f, (l + 1.35f) / layers);
            float rb = Mathf.Lerp(eave + 0.12f, 0, t0), rt = Mathf.Lerp(eave + 0.12f, 0, t1);
            float yb = Mathf.Lerp(wallTop - 0.4f, peak, t0) + 0.06f, yt = Mathf.Lerp(wallTop - 0.4f, peak, t1) + 0.1f;
            Add(list, 3, Thatch, Cone(rb, rt + 0.03f, yb, yt, 28, l));
        }
        return list;
    }

    // --- Storage basket: coils of twisted fiber stacked and stitched, a little wider at the top.

    static List<Piece> Basket()
    {
        var list = new List<Piece>();
        var ring = new Vector3[8];
        for (int i = 0; i < ring.Length; i++)
            ring[i] = Around(0.42f, i * Mathf.Tau / ring.Length, 0);
        Site(list, ring);

        // A flat coiled base, then the wall coil by coil.
        Add(list, 0, Weave, Cone(0.27f, 0.02f, 0.0f, 0.05f, 18, 3));
        const int coils = 6;
        for (int c = 0; c < coils; c++)
        {
            float y = 0.04f + c * 0.075f, r = 0.27f + c * 0.012f;
            Add(list, 0, Weave, Wall(r, 0.045f, y, y + 0.072f, 0f, Mathf.Tau, top: c == coils - 1));
        }
        return list;
    }

    // A woven hurdle door for a hut's doorway, in the hinge's frame: it runs from the
    // hinge (origin) along +X for `width`, and stands `height` tall.
    public static Mesh Door(float width, float height)
    {
        var st = Begin();
        const int uprights = 7, rods = 13;
        for (int i = 0; i < uprights; i++)
        {
            float x = 0.05f + i * (width - 0.1f) / (uprights - 1);
            StickInto(st, new Vector3(x, 0.02f, 0), new Vector3(x + (Hash(i, 11) - 0.5f) * 0.03f, height, 0), 0.03f, Shade(i * 5));
        }
        for (int r = 0; r < rods; r++)
        {
            float y = 0.1f + r * (height - 0.18f) / (rods - 1), weave = (r % 2 == 0 ? 1 : -1) * 0.025f;
            StickInto(st, new Vector3(0.0f, y, weave), new Vector3(width, y + 0.02f, -weave), 0.05f, Shade(r * 13 + 2));
        }
        return st.Commit();
    }

    // The bar across a locked door, in the door's frame: a pole a little longer than
    // the door, resting on two short pegs.
    public static Mesh Bar(float width)
    {
        var st = Begin();
        // Fresh, pale wood so it reads at a glance against the weathered door.
        var pale = Shade(91).Lightened(0.55f);
        StickInto(st, new Vector3(-0.15f, 0.02f, -0.05f), new Vector3(width + 0.15f, 0f, -0.05f), 0.06f, pale);
        foreach (float x in new[] { 0.16f, width - 0.16f })
            StickInto(st, new Vector3(x, -0.09f, 0.04f), new Vector3(x, -0.07f, -0.14f), 0.03f, pale.Darkened(0.15f));
        return st.Commit();
    }

    // Pegs at each corner of the footprint with a fiber string between them.
    internal static void Site(List<Piece> list, Vector3[] corners)
    {
        var st = Begin();
        for (int i = 0; i < corners.Length; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) % corners.Length];
            if (corners.Length <= 4 || i % 2 == 0)
                StickInto(st, a + new Vector3(0, -0.15f, 0), a + new Vector3(0, 0.32f, 0), 0.025f, Shade(i));
            StickInto(st, a + new Vector3(0, 0.2f, 0), b + new Vector3(0, 0.2f, 0), 0.006f, new Color(1.3f, 1.2f, 0.8f));
        }
        Add(list, -1, Wood, st.Commit());
    }

    // --- Piece meshes -----------------------------------------------------

    static void Add(List<Piece> list, int stage, Material material, Mesh mesh)
    {
        var aabb = mesh.GetAabb();
        list.Add(new Piece
        {
            Stage = stage,
            Mesh = mesh,
            Material = material,
            Anchor = new Vector3(aabb.GetCenter().X, aabb.Position.Y, aabb.GetCenter().Z),
        });
    }

    static Mesh Stick(Vector3 a, Vector3 b, float radius, bool fork = false)
    {
        var st = Begin();
        StickInto(st, a, b, radius, Shade((int)(a.X * 100 + b.Z * 37)));
        if (fork)
        {
            // A short branch stub at the top where the ridge pole rests.
            var dir = (b - a).Normalized();
            var side = dir.Cross(Vector3.Forward).Normalized();
            StickInto(st, b - dir * 0.25f, b + side * 0.14f + dir * 0.05f, radius * 0.6f, Shade(7));
        }
        return st.Commit();
    }

    // A slightly tapered, slightly bent round stick from a to b.
    static void StickInto(SurfaceTool st, Vector3 a, Vector3 b, float radius, Color shade)
    {
        const int sides = 6, rings = 4;
        var axis = b - a;
        float length = axis.Length();
        var dir = axis / length;
        var u = Mathf.Abs(dir.Y) < 0.9f ? dir.Cross(Vector3.Up).Normalized() : dir.Cross(Vector3.Right).Normalized();
        var v = dir.Cross(u);
        float bend = (Hash((int)(length * 100), 9) - 0.5f) * 0.05f * length;

        Vector3 Center(float t) => a + axis * t + u * (Mathf.Sin(t * Mathf.Pi) * bend);
        float Radius(float t) => radius * (1.1f - 0.25f * t);

        for (int ring = 0; ring < rings; ring++)
        {
            float t0 = ring / (float)rings, t1 = (ring + 1) / (float)rings;
            for (int s = 0; s < sides; s++)
            {
                float a0 = s * Mathf.Tau / sides, a1 = (s + 1) * Mathf.Tau / sides;
                var n00 = u * Mathf.Cos(a0) + v * Mathf.Sin(a0);
                var n01 = u * Mathf.Cos(a1) + v * Mathf.Sin(a1);
                var p00 = Center(t0) + n00 * Radius(t0);
                var p01 = Center(t0) + n01 * Radius(t0);
                var p10 = Center(t1) + n00 * Radius(t1);
                var p11 = Center(t1) + n01 * Radius(t1);
                Vert(st, p00, n00, shade); Vert(st, p10, n00, shade); Vert(st, p11, n01, shade);
                Vert(st, p00, n00, shade); Vert(st, p11, n01, shade); Vert(st, p01, n01, shade);
            }
        }
        // Cut ends, a little lighter like fresh wood.
        var end = shade.Lightened(0.25f);
        for (int s = 0; s < sides; s++)
        {
            float a0 = s * Mathf.Tau / sides, a1 = (s + 1) * Mathf.Tau / sides;
            var c = Center(1f);
            Vert(st, c, dir, end);
            Vert(st, c + (u * Mathf.Cos(a1) + v * Mathf.Sin(a1)) * Radius(1f), dir, end);
            Vert(st, c + (u * Mathf.Cos(a0) + v * Mathf.Sin(a0)) * Radius(1f), dir, end);
        }
    }

    // A thick wrap around an axis: vine lashing, or a bundled thatch ridge cap.
    static Mesh Wrap(Vector3 center, Vector3 axis, float radius, float length, bool lumpy = false)
    {
        var st = Begin();
        const int sides = 8, rings = 6;
        var u = Vector3.Up;
        var v = axis.Cross(u).Normalized();
        for (int ring = 0; ring < rings; ring++)
        {
            float t0 = ring / (float)rings - 0.5f, t1 = (ring + 1) / (float)rings - 0.5f;
            for (int s = 0; s < sides; s++)
            {
                float a0 = s * Mathf.Tau / sides, a1 = (s + 1) * Mathf.Tau / sides;
                Vector3 P(float t, float a)
                {
                    var n = u * Mathf.Cos(a) + v * Mathf.Sin(a);
                    float bulge = lumpy ? 1f + 0.18f * Mathf.Sin(t * 23f + a * 3f) : 1f - 0.3f * Mathf.Abs(t) * 2f;
                    return center + axis * (t * length) + n * radius * bulge;
                }
                var n0 = u * Mathf.Cos(a0) + v * Mathf.Sin(a0);
                var n1 = u * Mathf.Cos(a1) + v * Mathf.Sin(a1);
                var shade = Shade(ring * 13 + s);
                Vert(st, P(t0, a0), n0, shade); Vert(st, P(t1, a0), n0, shade); Vert(st, P(t1, a1), n1, shade);
                Vert(st, P(t0, a0), n0, shade); Vert(st, P(t1, a1), n1, shade); Vert(st, P(t0, a1), n1, shade);
            }
        }
        return st.Commit();
    }

    // A shaggy slab of thatch lying on the slope from low to high, with a ragged lower edge.
    static Mesh ThatchRow(Vector3 low, Vector3 high, float width, float thickness, int seed)
    {
        var st = Begin();
        var up = high - low;
        var normal = Vector3.Right.Cross(up).Normalized();
        if (normal.Y < 0) normal = -normal;
        const int cols = 14;
        for (int c = 0; c < cols; c++)
        {
            float x0 = -width / 2 + c * width / cols, x1 = x0 + width / cols;
            // Each column hangs a little lower or higher, so the edge reads as straw.
            float drop = (Hash(c, seed + 40) - 0.3f) * 0.2f;
            var shade = Shade(c * 7 + seed * 3).Lerp(new Color(0.85f, 0.85f, 0.8f), Hash(c, seed + 50) * 0.3f);
            var b0 = low - up.Normalized() * drop + new Vector3(x0, 0, 0);
            var b1 = low - up.Normalized() * drop + new Vector3(x1, 0, 0);
            var t0 = high + new Vector3(x0, 0, 0);
            var t1 = high + new Vector3(x1, 0, 0);
            var lift = normal * thickness * (0.6f + Hash(c, seed + 60) * 0.9f);
            var top = normal.Lerp(Vector3.Up, 0.3f).Normalized();
            // Outer face, inner face, and the visible lower edge.
            Quad(st, b0 + lift, t0 + lift, t1 + lift, b1 + lift, top, shade);
            // The underside is wound both ways, so it shows from under the roof whichever way the slope faces.
            Quad(st, b1, t1, t0, b0, -normal, shade.Darkened(0.25f));
            Quad(st, b0, t0, t1, b1, -normal, shade.Darkened(0.25f));
            Quad(st, b0, b0 + lift, b1 + lift, b1, -up.Normalized(), shade.Darkened(0.1f));
        }
        return st.Commit();
    }

    // A clay wall following an arc, lumpy like hand-pressed daub.
    static Mesh Wall(float radius, float thickness, float y0, float y1, float from, float to, bool top)
    {
        var st = Begin();
        const int segs = 40, rows = 3;
        float Lump(float a, float y) => 0.022f * Mathf.Sin(a * 17f + y * 9f) + 0.015f * Mathf.Sin(a * 41f - y * 23f);
        Vector3 P(float a, float y, float rr) => Around(rr + Lump(a, y), a, y);
        for (int s = 0; s < segs; s++)
        {
            float a0 = Mathf.Lerp(from, to, s / (float)segs), a1 = Mathf.Lerp(from, to, (s + 1) / (float)segs);
            for (int r = 0; r < rows; r++)
            {
                float h0 = Mathf.Lerp(y0, y1, r / (float)rows), h1 = Mathf.Lerp(y0, y1, (r + 1) / (float)rows);
                var shade = Shade(s * 5 + r).Lerp(Colors.White, 0.5f);
                float ro = radius + thickness / 2, ri = radius - thickness / 2;
                Quad(st, P(a0, h0, ro), P(a0, h1, ro), P(a1, h1, ro), P(a1, h0, ro), Around(1, (a0 + a1) / 2, 0), shade);
                Quad(st, P(a1, h0, ri), P(a1, h1, ri), P(a0, h1, ri), P(a0, h0, ri), -Around(1, (a0 + a1) / 2, 0), shade.Darkened(0.1f));
            }
            if (top)
                Quad(st, P(a0, y1, radius + thickness / 2), P(a0, y1, radius - thickness / 2), P(a1, y1, radius - thickness / 2), P(a1, y1, radius + thickness / 2), Vector3.Up, Colors.White);
        }
        // Rounded jambs either side of the door.
        foreach (float a in new[] { from, to })
            Quad(st, P(a, y0, radius - thickness / 2), P(a, y1, radius - thickness / 2), P(a, y1, radius + thickness / 2), P(a, y0, radius + thickness / 2),
                Around(1, a + Mathf.Pi / 2 * (a == from ? -1 : 1), 0), Colors.White);
        st.GenerateNormals();
        return st.Commit();
    }

    // One layer of a thatched cone, with a shaggy lower rim.
    static Mesh Cone(float rBottom, float rTop, float yBottom, float yTop, int segs, int seed)
    {
        var st = Begin();
        for (int s = 0; s < segs; s++)
        {
            float a0 = s * Mathf.Tau / segs, a1 = (s + 1) * Mathf.Tau / segs;
            float drop = (Hash(s, seed + 70) - 0.3f) * 0.14f;
            var shade = Shade(s * 3 + seed * 11).Lerp(new Color(0.85f, 0.85f, 0.8f), Hash(s, seed + 80) * 0.3f);
            var b0 = Around(rBottom + 0.04f, a0, yBottom - drop);
            var b1 = Around(rBottom + 0.04f, a1, yBottom - drop);
            var t0 = Around(rTop, a0, yTop);
            var t1 = Around(rTop, a1, yTop);
            var n = Around(1, (a0 + a1) / 2, 0).Lerp(Vector3.Up, 0.6f).Normalized();
            Quad(st, b0, t0, t1, b1, n, shade);
            Quad(st, b1, t1, t0, b0, -n, shade.Darkened(0.35f));
            // Thickness at the rim.
            var lift = new Vector3(0, 0.1f, 0);
            Quad(st, b1, b1 + lift, b0 + lift, b0, Around(1, (a0 + a1) / 2, 0), shade.Darkened(0.1f));
        }
        return st.Commit();
    }

    // A flattened, faceted stone.
    static Mesh Rock(Vector3 center, Vector3 size, int seed)
    {
        var st = Begin();
        const int segs = 7, rings = 3;
        Vector3 P(int ring, int s)
        {
            float lat = Mathf.Pi * ring / rings - Mathf.Pi / 2, lon = Mathf.Tau * s / segs;
            float j = 0.85f + Hash(ring * 17 + s, seed) * 0.3f;
            if (ring == 0 || ring == rings) j = 1f;
            return center + new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon) * size.X, Mathf.Sin(lat) * size.Y, Mathf.Cos(lat) * Mathf.Sin(lon) * size.Z) * j;
        }
        for (int ring = 0; ring < rings; ring++)
            for (int s = 0; s < segs; s++)
                Quad(st, P(ring, s), P(ring + 1, s), P(ring + 1, s + 1), P(ring, s + 1), Vector3.Up, Shade(ring + s));
        st.Deindex();
        st.GenerateNormals();
        return st.Commit();
    }

    // --- Helpers ----------------------------------------------------------

    static Vector3 Around(float radius, float angle, float y) => new(Mathf.Sin(angle) * radius, y, Mathf.Cos(angle) * radius);

    static SurfaceTool Begin()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        return st;
    }

    static void Vert(SurfaceTool st, Vector3 p, Vector3 n, Color c)
    {
        st.SetNormal(n);
        st.SetColor(c);
        st.AddVertex(p);
    }

    // a, b, c, d counter-clockwise seen from the side the normal points to.
    static void Quad(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, Color shade)
    {
        Vert(st, a, n, shade); Vert(st, b, n, shade); Vert(st, c, n, shade);
        Vert(st, a, n, shade); Vert(st, c, n, shade); Vert(st, d, n, shade);
    }

    // A gentle per-piece shade so no two sticks or bundles look like copies.
    static Color Shade(int seed)
    {
        float k = 0.86f + Hash(seed, 99) * 0.2f;
        return new Color(k, k * (0.97f + Hash(seed, 98) * 0.05f), k * 0.97f);
    }

    static float Hash(int x, int salt) => Terrain.Hash01(x, salt, 777);

    static ShaderMaterial Tinted(ShaderMaterial source, Color color, float? sway = null)
    {
        var material = (ShaderMaterial)source.Duplicate();
        material.SetShaderParameter("albedo", color);
        if (sway is { } s)
        {
            material.SetShaderParameter("sway", s);
            material.SetShaderParameter("sway_height", 2f);
        }
        return material;
    }
}
