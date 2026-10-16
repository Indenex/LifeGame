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

    // The potter's bench: a flat wedging stone on two log rounds, a stool for the
    // potter, clay and a coil being rolled, a smoothing pebble, a pinch pot drying,
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
        Round(new Vector3(0, 0, 0.5f), 0.15f, 0.34f, 3);

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
            Mesh = PlacedPot(ClayForm.PinchPot, 8, new Vector3(0.62f, 0f, 0.3f), 1.6f),
            Material = ClayLook.For(ClayStage.Fired, FireKind.Pit, 0f, 8),
            Anchor = Vector3.Zero,
        });
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
