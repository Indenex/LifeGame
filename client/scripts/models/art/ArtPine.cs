using Godot;

namespace LifeGame;

// art-rnd-1 showcase piece: today's painted pine (PaintedTrees, built at runtime from
// rings of drooping boughs) beside a Blender-authored pine (tools/art/trees/, rebuilt
// with tools/art/trees/build.sh, three seeded variants): four to six stacked, puffy tiers with rolled hems and rounded,
// drooping scallops and a soft rounded tip (about 4.4k to 6.4k triangles) on a short warm-brown trunk, with needle clusters, lit tier tops,
// violet-leaning undersides and the bark's plates painted into one 1024 px texture over
// baked AO. It stands at today's scale (about 4.4 m).
//
// Wind: vertex colour alpha holds a sway weight, 0 at the trunk base, rising with height
// squared and with distance out from the trunk, to 1 at the top and the tier tips. A
// wind shader reads it in vertex():
//     float w = COLOR.a;
//     vec2 phase = NODE_POSITION_WORLD.xz * 0.3;
//     VERTEX.xz += wind_dir * w * sway_amp * sin(TIME * 0.9 + phase.x + phase.y);
//     VERTEX.y -= w * w * bob_amp * (0.5 + 0.5 * sin(TIME * 2.5 + VERTEX.y * 2.0));
// The first line bends the whole tree from its planted foot; the second lets the hem
// scallops bob a little. PaintedAsset's shader ignores COLOR.a today, so the tree stands still.
public static class ArtPine
{
    public static float Spacing = 2.8f;
    public static float Height = 4.6f;

    const string Dir = "res://assets/art/trees/";

    public static Node3D Today()
    {
        var root = new Node3D();
        root.AddChild(new MeshInstance3D { Mesh = PaintedTrees.Pine(0, false), MaterialOverride = PaintedTrees.PineMaterial });
        return root;
    }

    // Three seeded variants (D-166), pine_0..2: Authored() hands them out in turn, so the
    // showcase's three authored slots show one of each.
    public const int Variants = 3;
    static int next;

    public static Node3D Authored() => Variant(next++ % Variants);

    public static Node3D Variant(int seed) =>
        PaintedAsset.Instance(Dir + $"pine_{seed}.glb", PaintedAsset.Material(Dir + $"pine_{seed}_albedo.png"));
}
