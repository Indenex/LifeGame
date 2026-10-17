using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The trading stall (model-36, for Claims' stall, structure kind 26 "Stall"): a small
// wooden counter about 1.6 m wide under a woven awning, its top clear for a few goods, in the
// homes' wood and weave with the village kit's crates stacked at its side. Built in the
// structure's own frame like StructureModel's: +Z is the front where buyers stand (the
// seller is behind, at -Z), y = 0 the ground at the footprint's centre.
//
// Stages: -1 the laid-out site; 0 the four posts and the counter's frame; 1 the counter's
// boards, the top and a shelf behind; 2 the awning, its fringe and the crates.
// CounterTop is the height goods stand at; Goods are spots along it (Claims puts its wares there).
public static class StallModel
{
    public const float CounterTop = 0.96f;
    public static readonly Vector3[] Goods = { new(-0.5f, CounterTop, 0.02f), new(0f, CounterTop, 0.04f), new(0.5f, CounterTop, 0.02f) };

    const float HalfWidth = 0.8f, Front = 0.3f, Back = -0.3f;

    public static List<StructureModel.Piece> Pieces()
    {
        var list = new List<StructureModel.Piece>();
        StructureModel.Site(list, new[] { new Vector3(-1f, 0, 0.75f), new Vector3(1f, 0, 0.75f), new Vector3(1f, 0, -0.55f), new Vector3(-1f, 0, -0.55f) });

        // Stage 0: corner posts, the front pair shorter so the awning slopes to the buyers, and
        // the rails that hold the counter.
        var frame = Begin();
        foreach (var (x, z, height) in new[] { (-HalfWidth, Front + 0.12f, 2.02f), (HalfWidth, Front + 0.12f, 2.02f), (-HalfWidth, Back - 0.1f, 2.32f), (HalfWidth, Back - 0.1f, 2.32f) })
            Post(frame, new Vector3(x, -0.05f, z), new Vector3(x + (H((int)(x * 10 + z * 7), 1) - 0.5f) * 0.02f, height, z), 0.045f, Shade((int)(x * 13 + z * 5)));
        foreach (float y in new[] { 0.12f, CounterTop - 0.06f })
        {
            Plank(frame, new Vector3(-HalfWidth, y, Front), new Vector3(HalfWidth, y, Front), 0.035f, 0.03f, Shade(20 + (int)(y * 10)));
            Plank(frame, new Vector3(-HalfWidth, y, Back), new Vector3(HalfWidth, y, Back), 0.035f, 0.03f, Shade(30 + (int)(y * 10)));
            foreach (float x in new[] { -HalfWidth, HalfWidth })
                Plank(frame, new Vector3(x, y, Back), new Vector3(x, y, Front), 0.035f, 0.03f, Shade(40 + (int)(x * 10)));
        }
        Add(list, 0, StructureModel.Wood, frame.Commit());

        // Stage 1: upright boards down the front and the ends, the top of three long planks
        // overhanging a little toward the buyers, and a low shelf behind for the seller's stock.
        var boards = Begin();
        const int FrontBoards = 9;
        for (int i = 0; i < FrontBoards; i++)
        {
            float x0 = -HalfWidth + 0.02f + i * (2f * HalfWidth - 0.04f) / FrontBoards;
            float x1 = x0 + (2f * HalfWidth - 0.04f) / FrontBoards - 0.012f;
            float boardTop = CounterTop - 0.05f - H(i, 2) * 0.03f;
            Panel(boards, new Vector3(x0, 0.06f, Front + 0.04f), new Vector3(x1, boardTop, Front + 0.04f), 0.014f, Vector3.Back, Shade(60 + i));
        }
        foreach (float x in new[] { -HalfWidth - 0.04f, HalfWidth + 0.04f })
            for (int i = 0; i < 4; i++)
            {
                float z0 = Back + i * (Front - Back) / 4f, z1 = z0 + (Front - Back) / 4f - 0.01f;
                Panel(boards, new Vector3(x, 0.06f, z0), new Vector3(x, CounterTop - 0.06f, z1), 0.014f, x > 0 ? Vector3.Right : Vector3.Left, Shade(80 + i + (x > 0 ? 10 : 0)));
            }
        Add(list, 1, StructureModel.Wood, boards.Commit());
        var top = Begin();
        for (int i = 0; i < 3; i++)
        {
            float z = Back + 0.06f + i * 0.22f;
            Plank(top, new Vector3(-HalfWidth - 0.07f, CounterTop - 0.025f, z + 0.03f), new Vector3(HalfWidth + 0.07f, CounterTop - 0.025f, z + 0.03f), 0.11f, 0.025f, Shade(100 + i));
        }
        Plank(top, new Vector3(-HalfWidth + 0.05f, 0.42f, Back - 0.1f), new Vector3(HalfWidth - 0.05f, 0.42f, Back - 0.1f), 0.1f, 0.02f, Shade(110));
        Add(list, 1, StructureModel.PaleWood, top.Commit());

        // Stage 2: rafters from the back posts down to the front ones, and on them a woven mat
        // sagging a little between, running out past the front to shade the counter.
        var rafters = Begin();
        foreach (float x in new[] { -HalfWidth, 0f, HalfWidth })
            Post(rafters, new Vector3(x, 2.36f, Back - 0.22f), new Vector3(x, 1.96f, Front + 0.5f), 0.03f, Shade(120 + (int)(x * 10)));
        Plank(rafters, new Vector3(-HalfWidth - 0.1f, 2.02f, Front + 0.12f), new Vector3(HalfWidth + 0.1f, 2.02f, Front + 0.12f), 0.035f, 0.03f, Shade(130));
        Plank(rafters, new Vector3(-HalfWidth - 0.1f, 2.32f, Back - 0.1f), new Vector3(HalfWidth + 0.1f, 2.32f, Back - 0.1f), 0.035f, 0.03f, Shade(131));
        Add(list, 2, StructureModel.Wood, rafters.Commit());
        list.Add(Roof(Awning(), StructureModel.Weave));

        // A couple of the village kit's crates by the seller's end, one on the other.
        if (NatureKit.Mesh("Prop_Crate") is { } crate)
        {
            var at = new[] { (new Vector3(HalfWidth + 0.38f, 0, Back + 0.05f), 0.25f, 0.5f), (new Vector3(HalfWidth + 0.36f, 0.41f, Back + 0.02f), -0.4f, 0.42f) };
            foreach (var (pos, turn, size) in at)
                list.Add(new StructureModel.Piece { Stage = 2, Mesh = Placed(crate, pos, turn, size), Material = null!, Anchor = pos, Role = StructureModel.PieceRole.Fixed });
        }
        return list;
    }

    // The awning: a woven mat over the rafters, from the back top down to past the front,
    // dipping between rafters, with a ragged fringe hanging from its front edge.
    static Mesh Awning()
    {
        var st = Begin();
        const int Across = 12, Along = 6;
        var back = new Vector3(0, 2.4f, Back - 0.26f);
        var front = new Vector3(0, 1.99f, Front + 0.56f);
        Vector3 At(int i, int j)
        {
            float x = -HalfWidth - 0.14f + i * (2f * HalfWidth + 0.28f) / Across;
            float t = j / (float)Along;
            var p = back.Lerp(front, t);
            // Sags between the three rafters (at x = -0.8, 0, 0.8) and a little toward the middle of its run.
            float between = Mathf.Abs(Mathf.Sin(x / HalfWidth * Mathf.Pi));
            p.Y -= 0.035f * between + 0.03f * Mathf.Sin(t * Mathf.Pi);
            return new Vector3(x, p.Y + (H(i * 31 + j, 3) - 0.5f) * 0.008f, p.Z);
        }
        var n = (front - back).Cross(Vector3.Right).Normalized();
        if (n.Y < 0) n = -n;
        for (int i = 0; i < Across; i++)
            for (int j = 0; j < Along; j++)
            {
                // Bands of slightly different weave shade, as a mat of plaited strips has.
                var shade = Colors.White.Darkened(((i + j) % 2) * 0.06f + H(i, 4 + j) * 0.05f);
                Quad(st, At(i, j), At(i + 1, j), At(i + 1, j + 1), At(i, j + 1), n, shade);
            }
        // The fringe: short tufts hanging along the front edge, lit as the mat is (a straight
        // forward normal leaves them dark under a high sun).
        for (int i = 0; i < Across * 2; i++)
        {
            float x = -HalfWidth - 0.12f + i * (2f * HalfWidth + 0.24f) / (Across * 2);
            var edge = new Vector3(x, 1.99f - 0.03f * Mathf.Abs(Mathf.Sin(x / HalfWidth * Mathf.Pi)), Front + 0.56f);
            float len = 0.08f + H(i, 5) * 0.06f;
            var shade = Colors.White.Darkened(0.08f + H(i, 6) * 0.1f);
            Quad(st, edge + new Vector3(-0.035f, 0, 0), edge + new Vector3(0.035f, 0, 0), edge + new Vector3(0.02f, -len, 0.01f), edge + new Vector3(-0.02f, -len, 0.01f), (n + Vector3.Back * 0.5f).Normalized(), shade);
        }
        return st.Commit();
    }

    static StructureModel.Piece Roof(Mesh mesh, Material material)
    {
        var aabb = mesh.GetAabb();
        return new StructureModel.Piece { Stage = 2, Mesh = mesh, Material = material, Anchor = new Vector3(aabb.GetCenter().X, aabb.Position.Y, aabb.GetCenter().Z), Role = StructureModel.PieceRole.Roof };
    }

    static void Add(List<StructureModel.Piece> list, int stage, Material material, Mesh mesh)
    {
        var aabb = mesh.GetAabb();
        list.Add(new StructureModel.Piece
        {
            Stage = stage,
            Mesh = mesh,
            Material = material,
            Anchor = new Vector3(aabb.GetCenter().X, aabb.Position.Y, aabb.GetCenter().Z),
            Role = aabb.End.Y < 0.6f ? StructureModel.PieceRole.Fixed : StructureModel.PieceRole.Wall,
        });
    }

    // A kit mesh moved, turned and scaled into a piece of its own (its materials kept).
    static Mesh Placed(Mesh source, Vector3 at, float turn, float size)
    {
        var xf = new Transform3D(new Basis(Vector3.Up, turn).Scaled(Vector3.One * size), at);
        var mesh = new ArrayMesh();
        for (int s = 0; s < source.GetSurfaceCount(); s++)
        {
            var arrays = source.SurfaceGetArrays(s);
            var v = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            var n = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
            for (int i = 0; i < v.Length; i++) v[i] = xf * v[i];
            for (int i = 0; i < n.Length; i++) n[i] = (xf.Basis * n[i]).Normalized();
            arrays[(int)Mesh.ArrayType.Vertex] = v;
            arrays[(int)Mesh.ArrayType.Normal] = n;
            arrays[(int)Mesh.ArrayType.Tangent] = default;
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(s, source.SurfaceGetMaterial(s));
        }
        return mesh;
    }

    // --- Mesh helpers --------------------------------------------------------

    static SurfaceTool Begin()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        return st;
    }

    static void Quad(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, Color shade)
    {
        foreach (var p in new[] { a, b, c, a, c, d })
        {
            st.SetNormal(n);
            st.SetColor(shade);
            st.AddVertex(p);
        }
    }

    // A squared timber from a to b, half-width w across and half-height h up.
    static void Plank(SurfaceTool st, Vector3 a, Vector3 b, float w, float h, Color shade)
    {
        var dir = (b - a).Normalized();
        var side = Mathf.Abs(dir.Y) < 0.9f ? dir.Cross(Vector3.Up).Normalized() : Vector3.Right;
        var up = side.Cross(dir).Normalized();
        Box(st, a, b, side * w, up * h, shade);
    }

    // A board standing upright between corners lo and hi, thickness t, facing out.
    static void Panel(SurfaceTool st, Vector3 lo, Vector3 hi, float t, Vector3 facing, Color shade)
    {
        var bottom = new Vector3((lo.X + hi.X) / 2, lo.Y, (lo.Z + hi.Z) / 2);
        var top = new Vector3(bottom.X, hi.Y, bottom.Z);
        var across = new Vector3(hi.X - lo.X, 0, hi.Z - lo.Z) / 2;
        Box(st, bottom, top, across, facing * t, shade);
    }

    // A post: a squared, slightly tapering timber.
    static void Post(SurfaceTool st, Vector3 a, Vector3 b, float r, Color shade)
    {
        var dir = (b - a).Normalized();
        var side = Mathf.Abs(dir.Y) < 0.9f ? dir.Cross(Vector3.Up).Normalized() : dir.Cross(Vector3.Forward).Normalized();
        var up = side.Cross(dir).Normalized();
        Box(st, a, b, side * r, up * r, shade);
    }

    // A box along a to b with half-axes u and v across it.
    static void Box(SurfaceTool st, Vector3 a, Vector3 b, Vector3 u, Vector3 v, Color shade)
    {
        var dir = (b - a).Normalized();
        Vector3 P(Vector3 at, float su, float sv) => at + u * su + v * sv;
        var nu = u.Normalized();
        var nv = v.Normalized();
        Quad(st, P(a, 1, -1), P(b, 1, -1), P(b, 1, 1), P(a, 1, 1), nu, shade);
        Quad(st, P(a, -1, 1), P(b, -1, 1), P(b, -1, -1), P(a, -1, -1), -nu, shade.Darkened(0.06f));
        Quad(st, P(a, 1, 1), P(b, 1, 1), P(b, -1, 1), P(a, -1, 1), nv, shade.Lightened(0.04f));
        Quad(st, P(a, -1, -1), P(b, -1, -1), P(b, 1, -1), P(a, 1, -1), -nv, shade.Darkened(0.1f));
        Quad(st, P(b, 1, -1), P(b, -1, -1), P(b, -1, 1), P(b, 1, 1), dir, shade.Darkened(0.03f));
        Quad(st, P(a, -1, -1), P(a, 1, -1), P(a, 1, 1), P(a, -1, 1), -dir, shade.Darkened(0.03f));
    }

    static Color Shade(int seed)
    {
        float k = 0.86f + H(seed, 99) * 0.2f;
        return new Color(k, k * (0.97f + H(seed, 98) * 0.05f), k * 0.97f);
    }

    static float H(int x, int salt) => Terrain.Hash01(x, salt, 778);
}
