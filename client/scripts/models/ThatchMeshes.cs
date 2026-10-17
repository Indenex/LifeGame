using Godot;

namespace LifeGame;

// Thatch for roofs in the reference artist's style (docs/art-reference, D-232):
// soft rounded rows like bolsters of straw, each with a ragged fringe of points
// hanging over the row below. A drop-in for StructureModel.ThatchRow: same
// arguments (the row from its low edge to its high edge across a roof slope that
// runs along X), near-white vertex colours so the caller's Thatch material tints it.
public static class ThatchMeshes
{
    public static Mesh Row(Vector3 low, Vector3 high, float width, float thickness, int seed)
    {
        var k = new MeshKit(false) { Skinned = false };
        var up = high - low;
        var normal = Vector3.Right.Cross(up).Normalized();
        if (normal.Y < 0) normal = -normal;
        var down = -up.Normalized();
        const int cols = 28, rows = 6;
        float H(int a, int b) => MeshKit.Hash(a + seed * 131, b + 140);

        // The bolster: a curved sheet bulging out from the slope, fullest a third of
        // the way up, thinning to nothing at the top where the next row covers it.
        Vector3 Surface(int c, int r)
        {
            float x = -width / 2 + c * width / cols, s = r / (float)rows;
            float bulge = thickness * Mathf.Pow(Mathf.Sin(Mathf.Pi * Mathf.Clamp(s * 0.9f + 0.1f, 0f, 1f)), 0.6f);
            bulge *= 0.85f + 0.3f * H(c, 1);
            return low + up * s + new Vector3(x, 0, 0) + normal * bulge;
        }
        for (int c = 0; c < cols; c++)
            for (int r = 0; r < rows; r++)
            {
                var a = Surface(c, r);
                var b = Surface(c + 1, r);
                var d = Surface(c, r + 1);
                var e = Surface(c + 1, r + 1);
                float shade = 0.82f + 0.18f * r / rows + (H(c, 2) - 0.5f) * 0.08f;
                k.Paint(Colors.White, shade).Tri(a, b, e, normal, normal, normal);
                k.Paint(Colors.White, shade).Tri(a, e, d, normal, normal, normal);
            }

        // The fringe: a point of straw under each column, hanging past the edge,
        // each its own length and a little to one side.
        for (int c = 0; c < cols; c++)
        {
            var a = Surface(c, 0);
            var b = Surface(c + 1, 0);
            float hang = 0.06f + 0.1f * H(c, 3);
            var tip = (a + b) * 0.5f + down * hang + new Vector3((H(c, 4) - 0.5f) * width / cols * 0.6f, 0, 0) - normal * thickness * 0.2f;
            var face = (normal + down * 0.3f).Normalized();
            k.Paint(Colors.White, 0.74f + 0.12f * H(c, 5)).Tri(a, b, tip, face, face, face);
            // The fringe's underside, darker, closing the bolster to the slope.
            var root = (a + b) * 0.5f - normal * thickness * 0.8f;
            k.Paint(Colors.White, 0.55f).Tri(a, tip, root, -face, -face, -face);
            k.Paint(Colors.White, 0.55f).Tri(b, root, tip, -face, -face, -face);
        }

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, k.CommitToArrays());
        return mesh;
    }
}
