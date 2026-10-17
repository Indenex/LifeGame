using Godot;

namespace LifeGame;

// art-rnd-1 showcase piece: today's painted broadleaf (PaintedTrees, built at runtime
// from rows of small leaf lobes) beside a Blender-authored birch (tools/art/trees/,
// rebuilt with tools/art/trees/build.sh, three seeded variants). The authored crown is a dozen chunky leaf
// clumps fused into one surface (about 5.6k triangles, 6.9k with the wood), with the leaf clusters, lit
// tops, violet-leaning undersides and the white bark's dark marks painted into one
// 1024 px texture over baked AO. It stands at today's scale (about 4.4 m).
//
// Wind: vertex colour alpha holds a sway weight, 0 at the trunk base, rising with height
// squared and with distance out from the crown's axis, to 1 at the crown tips. A wind
// shader reads it in vertex():
//     float w = COLOR.a;
//     vec2 phase = NODE_POSITION_WORLD.xz * 0.3;
//     VERTEX.xz += wind_dir * w * sway_amp * sin(TIME * 0.9 + phase.x + phase.y);
//     VERTEX += NORMAL * w * w * flutter_amp * sin(TIME * 4.0 + dot(VERTEX, vec3(3.0)));
// The first line bends the whole tree (the foot, alpha 0, stays planted; the trunk
// barely moves because the weight rises with height squared); the second makes only the
// outer leaves shiver. PaintedAsset's shader ignores COLOR.a today, so the tree stands still.
public static class ArtBirch
{
    public static float Spacing = 4.7f;
    public static float Height = 4.6f;

    const string Dir = "res://assets/art/trees/";

    public static Node3D Today()
    {
        var root = new Node3D();
        root.AddChild(new MeshInstance3D { Mesh = PaintedTrees.Broadleaf(0, false), MaterialOverride = PaintedTrees.BroadleafMaterial });
        return root;
    }

    // Three seeded variants (D-166), birch_0..2: Authored() hands them out in turn, so the
    // showcase's three authored slots show one of each.
    public const int Variants = 3;
    static int next;

    public static Node3D Authored() => Variant(next++ % Variants);

    public static Node3D Variant(int seed) =>
        PaintedAsset.Instance(Dir + $"birch_{seed}.glb", PaintedAsset.Material(Dir + $"birch_{seed}_albedo.png"));
}
