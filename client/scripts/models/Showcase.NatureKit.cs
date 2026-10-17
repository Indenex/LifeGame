using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The nature kit side by side with our own pieces (naturekit-1). --kit=ours | raw | blend:
// our painted trees, rocks, bush and mushrooms; the kit's models with its own materials;
// the kit's models in our light (NatureKit). Same spots in each, so the three line up.
// --time-of-day adds the game's Atmosphere (0.5 noon, 0.72 golden hour).
public partial class Showcase
{
    [ShowcaseView("naturekit")]
    void NatureKitView()
    {
        string kit = args.GetValueOrDefault("kit", "blend");
        bool raw = kit == "raw";
        void Ours(Mesh mesh, Material material, Vector3 at, Color tint, float scale, float turn = 0.6f)
        {
            var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = mesh, InstanceCount = 1 };
            multi.SetInstanceTransform(0, new Transform3D(new Basis(Vector3.Up, turn).Scaled(Vector3.One * scale), at));
            multi.SetInstanceColor(0, tint);
            AddChild(new MultiMeshInstance3D { Multimesh = multi, MaterialOverride = material });
        }
        void Kit(string name, Vector3 at, float scale = 1f, float turn = 0.6f)
        {
            if (NatureKit.Mesh(name, raw) is { } mesh)
                AddChild(new MeshInstance3D { Mesh = mesh, Position = at, Rotation = new Vector3(0, turn, 0), Scale = Vector3.One * scale });
        }
        // Trees: two broadleaf, two pines, then the kit's twisted and dead trees (we have neither).
        var spots = new[] { new Vector3(-12f, 0, -4f), new Vector3(-7f, 0, -2f), new Vector3(-2f, 0, -4f), new Vector3(2.5f, 0, -2.5f), new Vector3(8f, 0, -5f), new Vector3(13f, 0, -3f) };
        if (kit == "ours")
        {
            Ours(PaintedTrees.Broadleaf(0, false), PaintedTrees.BroadleafMaterial, spots[0], Colors.White, 1.6f);
            Ours(PaintedTrees.Broadleaf(1, false), PaintedTrees.BroadleafMaterial, spots[1], Colors.White, 1.5f);
            Ours(PaintedTrees.Pine(0, false), PaintedTrees.PineMaterial, spots[2], Colors.White, 1.6f);
            Ours(PaintedTrees.Pine(1, false), PaintedTrees.PineMaterial, spots[3], Colors.White, 1.5f);
            for (int v = 0; v < 3; v++)
                Ours(PropMeshes.Rock(v), Painted.Rock, new Vector3(-6f + v * 2.6f, 0, 3f), Look.WorldRock, 1.1f, v * 1.1f);
            Ours(PaintedBushes.Bush, Painted.Bush, new Vector3(3.2f, 0, 3.2f), Look.Leaf, 1.2f);
            AddChild(new MeshInstance3D { Mesh = Mushrooms.Patch(0), Position = new Vector3(5.4f, 0, 4.4f), Scale = Vector3.One * 1.5f });
        }
        else
        {
            Kit("CommonTree_1", spots[0]);
            Kit("CommonTree_3", spots[1]);
            Kit("Pine_1", spots[2]);
            Kit("Pine_3", spots[3]);
            Kit("TwistedTree_1", spots[4], 0.55f);
            Kit("DeadTree_1", spots[5], 0.8f);
            for (int v = 0; v < 3; v++)
                Kit($"Rock_Medium_{v + 1}", new Vector3(-6f + v * 2.6f, 0, 3f), 0.45f, v * 1.1f);
            Kit("Bush_Common_Flowers", new Vector3(3.2f, 0, 3.2f), 0.9f);
            Kit("Mushroom_Common", new Vector3(5.4f, 0, 4.4f), 1.5f);
            Kit("Fern_1", new Vector3(-1.2f, 0, 4.6f), 1f);
            Kit("Flower_3_Group", new Vector3(0.6f, 0, 5.2f), 1f);
            Kit("Grass_Common_Tall", new Vector3(1.8f, 0, 5.5f), 1f);
        }
        var camera = new Camera3D { Fov = 45f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0f, 5.5f, 17f), new Vector3(0f, 2.6f, -1f), Vector3.Up);
        if (args.ContainsKey("time-of-day")) AddChild(new Atmosphere());
    }

    // The village kit's props in our light (village-1) in a homestead corner, with the
    // nature kit's birches, giant pines and big rocks behind. --kit=raw for the kits' own.
    [ShowcaseView("villagekit")]
    void VillageKitView()
    {
        bool raw = args.GetValueOrDefault("kit", "blend") == "raw";
        void Kit(string name, Vector3 at, float scale = 1f, float turn = 0f)
        {
            if (NatureKit.Mesh(name, raw) is { } mesh)
                AddChild(new MeshInstance3D { Mesh = mesh, Position = at, Rotation = new Vector3(0, turn, 0), Scale = Vector3.One * scale });
        }
        Kit("Birch_1", new Vector3(-9f, 0, -7f), 0.8f);
        Kit("Birch_3", new Vector3(-5.5f, 0, -9f), 0.8f, 1f);
        Kit("GiantPine_1", new Vector3(5f, 0, -11f), 0.5f);
        Kit("GiantPine_3", new Vector3(10f, 0, -8f), 0.45f, 2f);
        Kit("Rock_Big_1", new Vector3(-10f, 0, 1f), 0.35f, 0.5f);
        Kit("Bush_Large", new Vector3(8f, 0, 0f), 0.8f);
        // A fence run along the back, a crate stack, a post with a vine, rubble and edging.
        Kit("Prop_WoodenFence_Single", new Vector3(-4f, 0, -2f));
        Kit("Prop_WoodenFence_Extension1", new Vector3(-2f, 0, -2f));
        Kit("Prop_WoodenFence_Extension2", new Vector3(0f, 0, -2f));
        Kit("Prop_WoodenFence_Extension1", new Vector3(2f, 0, -2f));
        Kit("Prop_Crate", new Vector3(-3.4f, 0, 2f), 0.6f, 0.3f);
        Kit("Prop_Crate", new Vector3(-2.4f, 0, 2.5f), 0.6f, -0.2f);
        Kit("Prop_Crate", new Vector3(-2.9f, 0.62f, 2.2f), 0.6f, 0.9f);
        Kit("Prop_Vine3", new Vector3(-6f, 0, -1.85f));
        Kit("Prop_Wagon", new Vector3(3f, 0, 3.5f), 1f, -0.5f);
        Kit("Prop_Brick1", new Vector3(0.2f, 0, 4f), 1f, 0.4f);
        Kit("Prop_Brick3", new Vector3(0.9f, 0, 4.6f), 1f, 1.7f);
        Kit("Prop_ExteriorBorder_Straight1", new Vector3(-1f, 0, 6f));
        var camera = new Camera3D { Fov = 45f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0f, 4.5f, 13f), new Vector3(0f, 1.2f, 0f), Vector3.Up);
        if (args.ContainsKey("time-of-day")) AddChild(new Atmosphere());
    }
}
