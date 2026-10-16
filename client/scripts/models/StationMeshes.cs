using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Work stations in the model-1 style (model-2), as StructureModel piece lists so
// the owning lane's StructureModel.Pieces switch can return them as they are.
// Built with MeshKit: one mesh per material, colors in the vertices. Each station
// is made in its own frame: +Z is where the worker stands, y = 0 the ground.
public static class StationMeshes
{
    static readonly Color Bark = new(0.40f, 0.30f, 0.22f);
    static readonly Color Split = new(0.72f, 0.58f, 0.40f);
    static readonly Color EndGrain = new(0.80f, 0.68f, 0.50f);
    static readonly Color Cord = new(0.55f, 0.42f, 0.24f);
    static readonly Color Flint = new(0.44f, 0.45f, 0.48f);
    static readonly Color WetClay = new(0.56f, 0.40f, 0.30f);

    static Material? wood, stone, plant, plain;

    static Material Relief(float scale, float bump, float cavity)
    {
        var material = Look.Solid(Colors.White);
        material.SetShaderParameter("detail_scale", scale);
        material.SetShaderParameter("bump", bump);
        material.SetShaderParameter("cavity", cavity);
        return material;
    }

    static void EnsureMaterials()
    {
        if (plain != null) return;
        wood = Relief(8f, 0.55f, 0.25f);
        stone = Relief(4f, 0.8f, 0.3f);
        plant = Relief(9f, 0.7f, 0.25f);
        plain = Look.Solid(Colors.White);
    }

    sealed class Kit
    {
        public readonly MeshKit Wood = New(), Stone = New(), Plant = New(), Plain = New();
        static MeshKit New() => new(true) { Skinned = false };

        // One piece per material that has anything in it, all at one stage.
        public void AddTo(List<StructureModel.Piece> list, int stage)
        {
            EnsureMaterials();
            foreach (var (k, material) in new[] { (Wood, wood!), (Stone, stone!), (Plant, plant!), (Plain, plain!) })
            {
                if (k.Empty) continue;
                var mesh = new ArrayMesh();
                k.Commit(mesh, material);
                list.Add(new StructureModel.Piece { Stage = stage, Mesh = mesh, Material = material, Anchor = Vector3.Zero });
            }
        }
    }

    static float H(int a, int b) => MeshKit.Hash(a, b + 70);

    static Vector3[] Corners(float hw, float hd) =>
        new[] { new Vector3(-hw, 0, hd), new Vector3(hw, 0, hd), new Vector3(hw, 0, -hd), new Vector3(-hw, 0, -hd) };

    // A cord lashing: a few tight turns round a post.
    static void Lash(MeshKit k, Vector3 at, Vector3 axis, float radius, float turns = 3f) =>
        k.Paint(Cord, 0.9f + H((int)(at.X * 100), (int)(at.Z * 100)) * 0.15f)
            .Tube(MeshKit.Circle(at - axis.Normalized() * 0.02f, axis, radius, 10 * (int)turns + 2, turns, 0.04f), 0.007f, 4);

    // The crafting table (D-098): four forked legs lashed to rails, and two split
    // planks laid on top, cut face up, with a few shavings and a flake left on it.
    public static List<StructureModel.Piece> CraftingTable()
    {
        var list = new List<StructureModel.Piece>();
        const float hw = 0.7f, hd = 0.33f, top = 0.8f;
        StructureModel.Site(list, Corners(hw + 0.05f, hd + 0.05f));
        var k = new Kit();

        foreach (var (x, z) in new[] { (-hw, -hd), (hw, -hd), (hw, hd), (-hw, hd) })
        {
            int i = (int)(x * 10 + z * 100);
            var foot = new Vector3(x * 1.07f, 0, z * 1.12f);
            var head = new Vector3(x, top - 0.05f, z);
            k.Wood.Paint(Bark, 0.9f + H(i, 1) * 0.15f).Tube(new[] { foot, foot.Lerp(head, 0.5f) + new Vector3(0.01f, 0, -0.008f), head }, 0.045f, 6, 0.038f);
            // The fork the rail sits in.
            k.Wood.Paint(Bark, 0.95f).Tube(new[] { head - new Vector3(0, 0.06f, 0), head + new Vector3(Mathf.Sign(x) * 0.05f, 0.02f, 0) }, 0.022f, 5, 0.014f);
        }
        // Long rails under the top, short stretchers low down.
        foreach (float z in new[] { -hd, hd })
        {
            k.Wood.Paint(Bark, 0.85f).Tube(new[] { new Vector3(-hw - 0.08f, top - 0.09f, z), new Vector3(hw + 0.08f, top - 0.085f, z) }, 0.03f, 6);
            foreach (float x in new[] { -hw, hw }) Lash(k.Plant, new Vector3(x, top - 0.09f, z), Vector3.Up, 0.05f);
        }
        foreach (float x in new[] { -hw, hw })
        {
            k.Wood.Paint(Bark, 0.8f).Tube(new[] { new Vector3(x * 1.03f, 0.28f, -hd - 0.06f), new Vector3(x * 1.03f, 0.29f, hd + 0.06f) }, 0.024f, 5);
            foreach (float z in new[] { -hd, hd }) Lash(k.Plant, new Vector3(x * 1.03f, 0.285f, z * 1.06f), Vector3.Up, 0.046f, 2f);
        }
        // Two split planks, flat face up, bark edges showing.
        for (int p = 0; p < 2; p++)
        {
            float z = p == 0 ? -0.17f : 0.17f, w = 0.05f;
            float y = top + w * 0.5f;
            k.Wood.Paint(Split, p == 0 ? 1f : 0.94f).Loft(new Vector3(-hw - 0.14f, y, z), new Vector3(hw + 0.12f + p * 0.03f, y, z), new MeshKit.Ring[]
            {
                new(0f, w, 0.17f), new(0.5f, w * 1.05f, 0.18f), new(1f, w, 0.165f),
            }, 6, 0.05f, 0f);
            foreach (float x in new[] { -hw - 0.14f, hw + 0.12f + p * 0.03f })
                k.Wood.Paint(EndGrain).Ellipsoid(new Vector3(x, y, z), new Vector3(0.004f, w * 0.85f, 0.15f), 6, 2);
        }
        float surface = top + 0.05f * 0.5f + 0.05f * 0.87f;
        // Work left lying about: curls of shavings and a flint flake.
        for (int i = 0; i < 7; i++)
        {
            var at = new Vector3(-0.35f + H(i, 3) * 0.3f, surface + 0.004f, -0.1f + H(i, 4) * 0.2f);
            var dir = new Vector3(H(i, 5) - 0.5f, 0, H(i, 6) - 0.5f).Normalized();
            k.Plant.Paint(EndGrain, 0.9f + H(i, 7) * 0.15f).Blade(at, at + dir * 0.08f, Vector3.Up, 0.03f, 0.02f);
        }
        k.Stone.Paint(Flint).Loft(new Vector3(0.3f, surface + 0.008f, 0.05f), new Vector3(0.42f, surface + 0.008f, 0.0f), new MeshKit.Ring[]
        {
            new(0f, 0.01f, 0.02f), new(0.4f, 0.012f, 0.035f), new(1f, 0.004f, 0.006f),
        }, 5, 0.3f, 0f);
        k.AddTo(list, 0);
        return list;
    }

    // The potter's bench (Ceramics, ceramics-8): a flat wedging stone on two log
    // rounds, a stool for the potter, clay and a coil being rolled, a smoothing pebble, a pinch pot drying,
    // and a fired bowl on the ground.
    public static List<StructureModel.Piece> PottersBench()
    {
        var list = new List<StructureModel.Piece>();
        StructureModel.Site(list, Corners(0.65f, 0.5f));
        var k = new Kit();

        // Log rounds holding up the slab, and a lower one for the stool.
        void Round(Vector3 at, float r, float h, int seed)
        {
            k.Wood.Paint(Bark, 0.85f + H(seed, 1) * 0.15f).Loft(at, at + Vector3.Up * h, new MeshKit.Ring[]
            {
                new(0f, r * 1.06f, r * 1.04f), new(0.5f, r, r), new(1f, r * 0.98f, r * 0.99f),
            }, 8, 0.02f);
            k.Wood.Paint(EndGrain).Ellipsoid(at + Vector3.Up * (h + 0.003f), new Vector3(r * 0.9f, 0.006f, r * 0.9f), 8, 2);
            k.Wood.Paint(EndGrain, 0.8f).Ellipsoid(at + Vector3.Up * (h + 0.006f), new Vector3(r * 0.4f, 0.004f, r * 0.4f), 6, 2);
        }
        Round(new Vector3(-0.34f, 0, -0.1f), 0.14f, 0.36f, 1);
        Round(new Vector3(0.34f, 0, -0.1f), 0.14f, 0.36f, 2);
        // The stool sits off to the side: the server sets wet pieces out along
        // the +Z edge, which stays clear.
        Round(new Vector3(-0.56f, 0, 0.22f), 0.13f, 0.32f, 3);

        // The wedging stone: a broad flat slab.
        const float slabY = 0.4f;
        k.Stone.Paint(Look.Rock, 0.95f).Ellipsoid(new Vector3(0, slabY, -0.1f), new Vector3(0.55f, 0.045f, 0.3f), 9, 3, new Basis(Vector3.Up, 0.05f), 0.6f);
        float onSlab = slabY + 0.04f;

        // Clay: a wedged lump, a coil being rolled, and a little pile of grit.
        k.Plain.Paint(WetClay).Ellipsoid(new Vector3(-0.28f, onSlab + 0.045f, -0.12f), new Vector3(0.09f, 0.055f, 0.075f), 7, 4, new Basis(Vector3.Up, 0.4f), 0.5f);
        k.Plain.Paint(WetClay, 1.05f).Tube(new[] { new Vector3(-0.08f, onSlab + 0.018f, 0.04f), new Vector3(0.05f, onSlab + 0.018f, 0.02f), new Vector3(0.17f, onSlab + 0.018f, 0.05f) }, 0.016f, 6, 0.013f);
        k.Stone.Paint(Look.Rock.Lightened(0.2f)).Ellipsoid(new Vector3(0.36f, onSlab + 0.01f, -0.2f), new Vector3(0.06f, 0.025f, 0.06f), 6, 2, null, 0.3f);
        // A smooth pebble for burnishing.
        k.Stone.Paint(new Color(0.30f, 0.30f, 0.32f)).Ellipsoid(new Vector3(0.12f, onSlab + 0.014f, -0.24f), new Vector3(0.03f, 0.016f, 0.022f), 7, 4);

        k.AddTo(list, 0);

        // A pot drying on the slab and a bowl of water by the bench, from the
        // Ceramics lane's own pot shapes, so they match the pots players make.
        list.Add(new StructureModel.Piece
        {
            Stage = 0,
            Mesh = PlacedPot(ClayForm.PinchPot, 7, new Vector3(0.12f, onSlab, -0.14f), 1.2f),
            Material = ClayLook.For(ClayStage.Drying, FireKind.Pit, 0f, 7),
            Anchor = Vector3.Zero,
        });
        list.Add(new StructureModel.Piece
        {
            Stage = 0,
            Mesh = PlacedPot(ClayForm.PinchPot, 8, new Vector3(0.5f, 0f, -0.42f), 1.6f),
            Material = ClayLook.For(ClayStage.Fired, FireKind.Pit, 0f, 8),
            Anchor = Vector3.Zero,
        });
        return list;
    }

    static readonly Color Fleece = new(0.86f, 0.82f, 0.72f);
    static readonly Color Yarn = new(0.80f, 0.76f, 0.66f);

    // The distaff stand (Animals' spindle station): a forked staff set in a ring
    // of stones with a fleece bound on top, a drop spindle hanging from a thread,
    // a log stool, and a basket of carded wool at its foot.
    public static List<StructureModel.Piece> Distaff()
    {
        var list = new List<StructureModel.Piece>();
        StructureModel.Site(list, Corners(0.6f, 0.4f));
        var k = new Kit();
        var foot = new Vector3(-0.25f, 0, -0.15f);
        var top = foot + new Vector3(0.05f, 1.3f, 0.06f);
        k.Wood.Paint(Bark).Tube(new[] { foot, foot.Lerp(top, 0.5f) + new Vector3(-0.01f, 0, 0.01f), top }, 0.032f, 6, 0.024f);
        k.Wood.Paint(Bark, 0.92f).Tube(new[] { top - new Vector3(0, 0.12f, 0), top + new Vector3(0.07f, 0.1f, 0.02f) }, 0.016f, 5, 0.01f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.Tau / 6f + H(i, 11);
            k.Stone.Paint(Look.Rock, 0.8f + H(i, 12) * 0.25f).Ellipsoid(foot + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.13f + Vector3.Up * 0.04f,
                new Vector3(0.07f, 0.055f, 0.06f) * (0.8f + H(i, 13) * 0.4f), 6, 3, new Basis(Vector3.Up, a), 0.5f);
        }
        // The fleece, wound on and tied with a cord.
        var fleeceAt = top - new Vector3(0, 0.2f, 0);
        k.Plant.Paint(Fleece).Ellipsoid(fleeceAt, new Vector3(0.1f, 0.18f, 0.1f), 8, 5);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 2.4f;
            k.Plant.Paint(Fleece, 0.92f + H(i, 14) * 0.1f).Ellipsoid(fleeceAt + new Vector3(Mathf.Cos(a) * 0.07f, (H(i, 15) - 0.5f) * 0.25f, Mathf.Sin(a) * 0.07f), Vector3.One * 0.06f, 6, 3);
        }
        k.Plant.Paint(Cord).Tube(MeshKit.Circle(fleeceAt + Vector3.Down * 0.02f, Vector3.Up, 0.105f, 18, 1.5f, 0.06f), 0.006f, 4);
        // A drop spindle hanging from a thread drawn off the fleece.
        var spindleTop = fleeceAt + new Vector3(0.22f, -0.55f, 0.12f);
        k.Plain.Paint(Yarn).Tube(new[] { fleeceAt + new Vector3(0.08f, -0.08f, 0.04f), spindleTop }, 0.003f, 3);
        k.Wood.Paint(Split).Tube(new[] { spindleTop, spindleTop - Vector3.Up * 0.27f }, 0.007f, 5, 0.006f);
        k.Plain.Paint(Yarn).Ellipsoid(spindleTop - Vector3.Up * 0.09f, new Vector3(0.022f, 0.055f, 0.022f), 7, 5);
        k.Stone.Paint(Look.Rock.Lightened(0.1f)).Ellipsoid(spindleTop - Vector3.Up * 0.16f, new Vector3(0.036f, 0.008f, 0.036f), 8, 3);
        // A log stool for the spinner, and a basket of carded wool.
        var stool = new Vector3(0.3f, 0, 0.25f);
        k.Wood.Paint(Bark, 0.85f).Loft(stool, stool + Vector3.Up * 0.38f, new MeshKit.Ring[] { new(0f, 0.18f, 0.17f), new(1f, 0.16f, 0.16f) }, 8, 0.02f);
        k.Wood.Paint(EndGrain).Ellipsoid(stool + Vector3.Up * 0.383f, new Vector3(0.145f, 0.006f, 0.145f), 8, 2);
        var basket = new Vector3(0.35f, 0, -0.25f);
        k.Plant.Paint(Weave()).Loft(basket, basket + Vector3.Up * 0.16f, new MeshKit.Ring[] { new(0f, 0.11f, 0.11f), new(1f, 0.15f, 0.15f) }, 9, 0.02f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 2.4f;
            k.Plant.Paint(Fleece, 0.95f + H(i, 16) * 0.08f).Ellipsoid(basket + new Vector3(Mathf.Cos(a) * 0.06f, 0.16f, Mathf.Sin(a) * 0.06f), new Vector3(0.06f, 0.035f, 0.035f), 6, 3, new Basis(Vector3.Up, a));
        }
        k.AddTo(list, 0);
        return list;
    }

    static Color Weave() => new(0.72f, 0.58f, 0.32f);

    // The frame of the leather drying rack (Animals' station): two forked posts
    // braced against the pull, a crossbar lashed into the forks at 0.99 m where the
    // hides hang (the hides themselves are Animals' HideRacks meshes).
    public const float RackBarY = 0.99f, RackHalfWidth = 1.15f;

    public static List<StructureModel.Piece> DryingRack()
    {
        var list = new List<StructureModel.Piece>();
        StructureModel.Site(list, Corners(1.2f, 0.35f));
        var k = new Kit();
        foreach (float x in new[] { -RackHalfWidth, RackHalfWidth })
        {
            int i = x < 0 ? 1 : 2;
            var foot = new Vector3(x * 1.02f, 0, 0);
            var fork = new Vector3(x, RackBarY - 0.04f, 0);
            k.Wood.Paint(Bark, 0.9f + H(i, 17) * 0.1f).Tube(new[] { foot, foot.Lerp(fork, 0.5f) + new Vector3(0, 0, 0.012f), fork }, 0.045f, 6, 0.038f);
            // The two prongs of the fork.
            k.Wood.Paint(Bark, 0.95f).Tube(new[] { fork, fork + new Vector3(0, 0.12f, 0.05f) }, 0.022f, 5, 0.014f);
            k.Wood.Paint(Bark, 0.95f).Tube(new[] { fork, fork + new Vector3(0, 0.12f, -0.05f) }, 0.022f, 5, 0.014f);
            // A brace leaning in from outside, against the weight of wet hides.
            k.Wood.Paint(Bark, 0.8f).Tube(new[] { new Vector3(x * 1.32f, 0, 0.02f), new Vector3(x * 1.01f, 0.62f, 0) }, 0.025f, 5, 0.02f);
            Lash(k.Plant, new Vector3(x * 1.01f, 0.62f, 0), Vector3.Up, 0.05f, 2f);
            Lash(k.Plant, new Vector3(x, RackBarY + 0.01f, 0), Vector3.Right, 0.04f, 3f);
        }
        k.Wood.Paint(Split, 0.82f).Tube(new[] { new Vector3(-RackHalfWidth - 0.15f, RackBarY + 0.01f, 0), new Vector3(0, RackBarY + 0.025f, 0.005f), new Vector3(RackHalfWidth + 0.17f, RackBarY + 0.01f, 0) }, 0.032f, 6, 0.028f);
        // Three peg loops where Animals hangs the hides, 0.75 m apart.
        foreach (float x in new[] { -0.75f, 0f, 0.75f })
            Lash(k.Plant, new Vector3(x, RackBarY + 0.015f, 0), Vector3.Right, 0.042f, 2f);
        k.AddTo(list, 0);
        return list;
    }

    // The repair table (Crafting, D-155): where worn tools are mended. Heavier
    // than the crafting table, so the two read apart at a glance: a thick split
    // log on two log rounds, a flat whetstone set into it with a hammerstone, a
    // coil of cord, spare shafts leaning on one end and a worn axe waiting.
    // Footprint 1.6 x 0.9 m, +Z where the worker stands.
    public static List<StructureModel.Piece> RepairTable()
    {
        var list = new List<StructureModel.Piece>();
        StructureModel.Site(list, Corners(0.8f, 0.45f));
        var k = new Kit();
        foreach (float x in new[] { -0.5f, 0.5f })
        {
            var at = new Vector3(x, 0, 0);
            k.Wood.Paint(Bark, 0.85f).Loft(at, at + Vector3.Up * 0.55f, new MeshKit.Ring[] { new(0f, 0.2f, 0.19f), new(1f, 0.18f, 0.18f) }, 8, 0.02f);
        }
        // The top: one thick half-log, flat face up.
        const float top = 0.66f;
        k.Wood.Paint(Split, 0.92f).Loft(new Vector3(-0.78f, top - 0.06f, 0), new Vector3(0.78f, top - 0.06f, 0.01f), new MeshKit.Ring[]
        {
            new(0f, 0.11f, 0.3f), new(0.5f, 0.12f, 0.32f), new(1f, 0.11f, 0.29f),
        }, 6, 0.04f, 0f);
        foreach (float x in new[] { -0.78f, 0.78f })
            k.Wood.Paint(EndGrain).Ellipsoid(new Vector3(x, top - 0.06f, 0), new Vector3(0.005f, 0.1f, 0.27f), 6, 2);
        float surface = top - 0.06f + 0.12f * 0.87f;
        // A flat grey whetstone set into the top, and a round hammerstone.
        k.Stone.Paint(new Color(0.5f, 0.5f, 0.52f)).Ellipsoid(new Vector3(-0.2f, surface + 0.02f, -0.05f), new Vector3(0.22f, 0.03f, 0.12f), 8, 3, new Basis(Vector3.Up, 0.15f), 0.5f);
        k.Stone.Paint(Look.Rock, 0.9f).Ellipsoid(new Vector3(0.22f, surface + 0.045f, -0.12f), new Vector3(0.06f, 0.05f, 0.055f), 7, 4);
        // A coil of cord for rebinding.
        k.Plant.Paint(Cord).Tube(MeshKit.Circle(new Vector3(0.45f, surface + 0.01f, 0.05f), Vector3.Up, 0.06f, 30, 2.2f, 0.018f), 0.009f, 4);
        // A worn stone axe waiting to be mended: chipped head, loosened binding.
        var axe = new Vector3(0.05f, surface + 0.025f, 0.12f);
        k.Wood.Paint(Bark).Tube(new[] { axe + new Vector3(-0.25f, 0, 0.02f), axe + new Vector3(0.2f, 0, -0.02f) }, 0.022f, 6);
        k.Stone.Paint(new Color(0.42f, 0.4f, 0.38f)).Loft(axe + new Vector3(0.17f, 0, -0.08f), axe + new Vector3(0.17f, 0, 0.11f), new MeshKit.Ring[]
        {
            new(0f, 0.025f, 0.03f), new(0.6f, 0.022f, 0.045f), new(1f, 0.008f, 0.03f),
        }, 5, 0.3f, 0f);
        k.Plant.Paint(Cord, 0.8f).Tube(new[] { axe + new Vector3(0.12f, 0.02f, 0.0f), axe + new Vector3(0.05f, -0.005f, 0.08f), axe + new Vector3(-0.02f, -0.01f, 0.14f) }, 0.005f, 3);
        // Spare shafts leaning against the far end.
        for (int i = 0; i < 3; i++)
        {
            var foot = new Vector3(0.95f + i * 0.05f, 0, -0.15f + i * 0.12f);
            k.Wood.Paint(EndGrain, 0.85f + i * 0.05f).Tube(new[] { foot, foot + new Vector3(-0.17f, 0.75f + i * 0.05f, 0.02f) }, 0.016f, 5);
        }
        k.AddTo(list, 0);
        return list;
    }

    static ArrayMesh PlacedPot(ClayForm form, ulong seed, Vector3 at, float scale)
    {
        var arrays = PotMesh.For(form, seed).SurfaceGetArrays(0);
        var verts = (Vector3[])arrays[(int)Mesh.ArrayType.Vertex];
        for (int i = 0; i < verts.Length; i++) verts[i] = verts[i] * scale + at;
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }
}
