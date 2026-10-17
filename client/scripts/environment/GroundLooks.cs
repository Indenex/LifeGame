using Godot;

namespace LifeGame;

// Two more ground prototypes for Justin's side-by-side (env-31; he passed on old, A and
// B+), chosen with --ground=; the grass carries both looks. D-256's grass shape lock is
// lifted for this comparison only.
//   --ground=botw    Breath of the Wild / Genshin: dense, longer, softly swaying blades
//                    that hide most of the ground, large soft colour fields from
//                    sun-warmed yellow-green to cool blue-green, wind waves rolling
//                    across with a bright sheen on the bent tips.
//   --ground=ghibli  Ghibli-like painted: rich hand-painted greens with gentle colour
//                    blooms, rounder and softer blades on the stepped painted light,
//                    the painted ground (A) beneath, and many more wildflowers in
//                    clusters of white, yellow, pink and blue.
// The colours are in Look's grass shader (the look uniform); placement is here.
public static class GroundLooks
{
    static PaintedTerrain.Style Style => PaintedTerrain.Chosen;

    // The tuft for the chosen look, or null for the usual one.
    public static Mesh? Tuft() => Style switch
    {
        // About 1.7x the blades, nearly twice as tall, slimmer.
        PaintedTerrain.Style.Botw => Build(13, 0.36f, 0.34f, 0.028f, 0.018f, 0.2f),
        // Fewer, wider, softer blades, a little taller than now.
        PaintedTerrain.Style.Ghibli => Build(10, 0.26f, 0.24f, 0.06f, 0.035f, 0.16f),
        _ => null,
    };

    static Mesh Build(int grid, float height, float heightRange, float width, float widthRange, float leanBy)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int gx = 0; gx < grid; gx++)
            for (int gz = 0; gz < grid; gz++)
            {
                float px = (gx + 0.5f) / grid - 0.5f + (Terrain.Hash01(gx, gz, 300) - 0.5f) / grid;
                float pz = (gz + 0.5f) / grid - 0.5f + (Terrain.Hash01(gx, gz, 301) - 0.5f) / grid;
                float angle = Terrain.Hash01(gx, gz, 302) * Mathf.Pi;
                float h = height + Terrain.Hash01(gx, gz, 303) * heightRange;
                float w = width + Terrain.Hash01(gx, gz, 304) * widthRange;
                var side = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * w;
                var lean = new Vector3(Terrain.Hash01(gx, gz, 305) - 0.5f, 0, Terrain.Hash01(gx, gz, 306) - 0.5f) * leanBy;
                var root = new Vector3(px, -0.02f, pz);
                Vertex(st, root - side, 0f);
                Vertex(st, root + side, 0f);
                Vertex(st, root + lean + new Vector3(0, h, 0), 1f);
            }
        return st.Commit();

        static void Vertex(SurfaceTool st, Vector3 v, float h)
        {
            st.SetNormal(Vector3.Up);
            st.SetUV(new Vector2(0, h));
            st.SetColor(Colors.White);
            st.AddVertex(v);
        }
    }
}

public partial class Terrain
{
    // How much of an open grassy tile carries a tuft (before woods, wet and trails thin it).
    static readonly float TuftCover = PaintedTerrain.Chosen == PaintedTerrain.Style.Botw ? 1.02f : 0.93f;

    // Wildflower patches: where (noise above this), how full, and the lone ones between.
    static readonly bool Ghibli = PaintedTerrain.Chosen == PaintedTerrain.Style.Ghibli;
    static readonly float FlowerPatch = Ghibli ? 0.52f : 0.62f;
    static readonly float FlowerFill = Ghibli ? 0.75f : 0.45f;
    static readonly float FlowerLone = Ghibli ? 0.06f : 0.04f;

    // The lone flowers are white, or in the Ghibli look any of the four colours.
    static Color LoneFlower(int x, int z) => Ghibli
        ? Undergrowth.FlowerColors[(int)(Hash01(x, z, 72) * Undergrowth.FlowerColors.Length * 0.999f)]
        : Look.FlowerWhite;
}
