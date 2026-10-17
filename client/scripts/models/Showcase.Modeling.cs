using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Modeling's own Showcase views (#679): found by their [ShowcaseView] name.
public partial class Showcase
{
    // The wild rabbit (model-18) in three coats, one mid-hop and one stretched in a
    // bound, with a hen behind for scale.
    [ShowcaseView("rabbit")]
    void Rabbits()
    {
        if (args.ContainsKey("side"))
        {
            // One rabbit at rest and one mid-bound, square side on, to check the posture.
            for (int i = 0; i < 2; i++)
            {
                var one = RabbitFigure.Create(PaintedAnimals.Coat(Look.Solid(new Color(0.55f, 0.42f, 0.3f))));
                one.Position = new Vector3(i * 0.45f - 0.22f, 0, 0);
                one.Rotation = new Vector3(0, Mathf.Pi / 2f, 0);
                if (i == 1)
                {
                    var (lift, pitch, front, hind) = RabbitFigure.Hop(Mathf.Tau * 0.3f, 1f);
                    one.SetBonePosePosition(RabbitFigure.BodyBone, new Vector3(0, lift, 0));
                    one.SetBonePoseRotation(RabbitFigure.BodyBone, Quaternion.FromEuler(new Vector3(pitch, 0, 0)));
                    for (int leg = 0; leg < 4; leg++)
                        one.SetBonePoseRotation(RabbitFigure.FirstLegBone + leg, Quaternion.FromEuler(new Vector3(leg < 2 ? front : hind, 0, 0)));
                }
                AddChild(one);
            }
            var side = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 0.5f, Current = true };
            AddChild(side);
            side.LookAtFromPosition(new Vector3(0, 0.14f, 2f), new Vector3(0, 0.14f, 0), Vector3.Up);
            return;
        }
        var coats = new[] { new Color(0.55f, 0.42f, 0.3f), new Color(0.6f, 0.57f, 0.53f), new Color(0.74f, 0.6f, 0.42f) };
        for (int i = 0; i < 5; i++)
        {
            var rabbit = RabbitFigure.Create(PaintedAnimals.Coat(Look.Solid(coats[i % 3])));
            rabbit.Position = new Vector3(-0.6f + i * 0.3f, 0, 0.2f * (i % 2));
            rabbit.Rotation = new Vector3(0, 0.9f - i * 0.35f, 0);
            float amount = i == 3 ? 0.6f : i == 4 ? 1f : 0f;
            if (amount > 0f)
            {
                var (lift, pitch, front, hind) = RabbitFigure.Hop(Mathf.Tau * (i == 3 ? 0.3f : 0.45f), amount);
                rabbit.SetBonePosePosition(RabbitFigure.BodyBone, new Vector3(0, lift, 0));
                rabbit.SetBonePoseRotation(RabbitFigure.BodyBone, Quaternion.FromEuler(new Vector3(pitch, 0, 0)));
                for (int leg = 0; leg < 4; leg++)
                    rabbit.SetBonePoseRotation(RabbitFigure.FirstLegBone + leg, Quaternion.FromEuler(new Vector3(leg < 2 ? front : hind, 0, 0)));
            }
            AddChild(rabbit);
        }
        var hen = HenFigure.Create(Look.Solid(new Color(0.95f, 0.93f, 0.88f)));
        hen.Position = new Vector3(0.9f, 0, -0.5f);
        hen.Rotation = new Vector3(0, -0.6f, 0);
        AddChild(hen);
        var camera = new Camera3D { Fov = 30f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0.1f, 0.65f, 1.9f), new Vector3(0.1f, 0.12f, 0), Vector3.Up);
    }

    // Every item's inventory icon at its real size (ItemIcon, 96 px), labelled, on the
    // bag's dark backing: an icon pass (model-20) checks each one reads at a glance.
    [ShowcaseView("icons")]
    void Icons()
    {
        ItemIcon.Attach(this);
        var items = new System.Collections.Generic.List<LifeGame.Shared.Item>(LifeGame.Shared.Items.All.Keys);
        items.Sort();
        var layer = new CanvasLayer();
        AddChild(layer);
        layer.AddChild(new ColorRect { Color = new Color(0.16f, 0.14f, 0.12f), Size = new Vector2(1600, 900) });
        const int cols = 14, cell = 112;
        for (int i = 0; i < items.Count; i++)
        {
            var at = new Vector2(12 + i % cols * cell, 8 + i / cols * (cell + 6));
            layer.AddChild(new ColorRect { Color = new Color(0.27f, 0.23f, 0.19f), Position = at, Size = new Vector2(100, 100) });
            layer.AddChild(new TextureRect { Texture = ItemIcon.For(items[i]), Position = at + new Vector2(2, 2), Size = new Vector2(96, 96) });
            layer.AddChild(new Label
            {
                Text = LifeGame.Shared.Items.All[items[i]].Name, Position = at + new Vector2(0, 96), Size = new Vector2(108, 16),
                HorizontalAlignment = HorizontalAlignment.Center, ClipText = true,
                LabelSettings = new LabelSettings { FontSize = 11, FontColor = new Color(0.95f, 0.9f, 0.8f) },
            });
        }
        AddChild(new Camera3D { Current = true });
    }

    // Hazelnuts (model-21): two real hazel bushes (BerryBush, as the world draws them) and
    // a berry bush for comparison, with a person for scale; in front, close-ups of the
    // hazelnut item and the bush clusters' three variants, each twice life size.
    [ShowcaseView("hazel")]
    void Hazel()
    {
        var bushes = new (ulong Id, Vector3 At)[] { (3, new Vector3(-1.3f, 0, -0.8f)), (9, new Vector3(1.4f, 0, -1.1f)), (4, new Vector3(0.1f, 0, -2.4f)) };
        foreach (var (id, at) in bushes)
            AddChild(new BerryBush(id) { Position = at, Rotation = new Vector3(0, id * 0.7f, 0) });
        var person = PlayerModel.Build();
        person.Position = new Vector3(0.1f, 0, -0.5f);
        person.Rotation = new Vector3(0, 0.3f, 0);
        AddChild(person);
        // The close-up row: the item on the ground, then the clusters as they hang.
        AddChild(new MeshInstance3D { Mesh = ItemLook.MeshFor(LifeGame.Shared.Item.Hazelnuts), Scale = Vector3.One * 2f, Position = new Vector3(-0.75f, 0.01f, 0.85f), Rotation = new Vector3(0, 0.5f, 0) });
        for (int v = 0; v < PropMeshes.HazelClusterVariants; v++)
            AddChild(new MeshInstance3D { Mesh = PropMeshes.HazelCluster(v), MaterialOverride = PropMeshes.HazelMaterial, Scale = Vector3.One * 2f, Position = new Vector3(-0.2f + v * 0.42f, 0.18f, 0.85f), Rotation = new Vector3(0.3f, Mathf.Pi, 0) });
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0.1f, 2.0f, 3.8f), new Vector3(0.1f, 0.35f, -0.5f), Vector3.Up);
    }

    // The new wild things at world scale (model-27): a player, a sheep, a hen, a hazel bush
    // and a berry bush as the world draws them, rabbits in the fleece colours Sheep.cs gives
    // them today (left) and in RabbitFigure.Coats (right), all at their in-game sizes.
    [ShowcaseView("wildlife")]
    void Wildlife()
    {
        AddChild(new BerryBush(3) { Position = new Vector3(-1.8f, 0, -1.6f) });
        AddChild(new BerryBush(4) { Position = new Vector3(1.9f, 0, -1.8f) });
        var person = PlayerModel.Build();
        person.Position = new Vector3(0f, 0, -0.9f);
        AddChild(person);
        var sheep = SheepFigure.Create(SheepFigure.Variant, PaintedAnimals.Wool(Look.Solid(new Color(0.74f, 0.71f, 0.63f))));
        sheep.Position = new Vector3(1.1f, 0, -0.6f);
        sheep.Rotation = new Vector3(0, -0.9f, 0);
        AddChild(sheep);
        var hen = HenFigure.Create(PaintedAnimals.Wool(Look.Solid(new Color(0.74f, 0.71f, 0.63f))));
        hen.Position = new Vector3(-0.9f, 0, -0.4f);
        hen.Rotation = new Vector3(0, 0.6f, 0);
        AddChild(hen);
        var fleeces = new[] { new Color(0.74f, 0.71f, 0.63f), new Color(0.68f, 0.62f, 0.52f), new Color(0.74f, 0.71f, 0.63f) };
        for (int i = 0; i < 6; i++)
        {
            bool now = i < 3;
            var fur = now ? PaintedAnimals.Wool(Look.Solid(fleeces[i])) : PaintedAnimals.Coat(Look.Solid(RabbitFigure.Coats[i - 3]));
            var rabbit = RabbitFigure.Create(fur);
            rabbit.Position = new Vector3((now ? -1.5f : 0.6f) + (i % 3) * 0.35f, 0, 0.3f + (i % 2) * 0.2f);
            rabbit.Rotation = new Vector3(0, 0.8f - i * 0.3f, 0);
            rabbit.Scale = Vector3.One * (0.9f + (i % 3) * 0.1f);
            AddChild(rabbit);
        }
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 2.2f, 4.2f), new Vector3(0, 0.35f, -0.4f), Vector3.Up);
    }

    // The rabbit snare (model-33): set across a run, sprung with its catch, and bundled to
    // carry, with a rabbit at rest beside for scale. --side looks square on at the set snare.
    [ShowcaseView("snare")]
    void Snares()
    {
        var fur = PaintedAnimals.Coat(Look.Solid(RabbitFigure.Coats[0]));
        AddChild(new MeshInstance3D { Mesh = ItemModels.SnareSet, Position = new Vector3(-0.35f, 0, 0), Rotation = new Vector3(0, 0.35f, 0) });
        var sprung = ItemModels.SnareSprungWith(fur);
        sprung.Position = new Vector3(0.12f, 0, -0.05f);
        sprung.Rotation = new Vector3(0, 1.75f, 0);
        AddChild(sprung);
        AddChild(new MeshInstance3D { Mesh = ItemModels.SnareBundled, Position = new Vector3(0.5f, 0, 0.18f), Rotation = new Vector3(0, 0.4f, 0) });
        var live = RabbitFigure.Create(PaintedAnimals.Coat(Look.Solid(RabbitFigure.Coats[2])));
        live.Position = new Vector3(-0.68f, 0, 0.12f);
        live.Rotation = new Vector3(0, 1.1f, 0);
        AddChild(live);
        var camera = new Camera3D { Fov = 30f, Current = true };
        AddChild(camera);
        if (args.ContainsKey("side")) camera.LookAtFromPosition(new Vector3(-0.35f, 0.12f, 0.9f), new Vector3(-0.35f, 0.1f, 0), Vector3.Up);
        else camera.LookAtFromPosition(new Vector3(0.0f, 0.75f, 1.55f), new Vector3(0.0f, 0.06f, 0), Vector3.Up);
    }
    // The camp stragglers on the painted look (painted-19): a hide drying on its A-frame
    // (wet left, dry right), meat roasting by the fire ring (raw, done, charred) and the
    // cooking pot on its three stones with a stew in it.
    [ShowcaseView("camp")]
    void Camp()
    {
        AddChild(new MeshInstance3D { Mesh = HearthModel.Ring, Position = new Vector3(0.2f, 0, -0.2f) });
        for (int i = 0; i < 2; i++)
        {
            var rack = new HideRacks.Rack(true) { Position = new Vector3(-1.9f + i * 1.45f, 0, -0.6f), Rotation = new Vector3(0, 0.25f, 0) };
            AddChild(rack);
            rack.Set(i == 0 ? 1f : 0f, 0, 0f);
        }
        float[] ages = { 0f, Roasting.BestSeconds, Roasting.BurnSeconds };
        for (int i = 0; i < ages.Length; i++)
            AddChild(new FireView.RoastView(0) { FixedAge = ages[i], Position = new Vector3(-0.15f + i * 0.35f, 0.32f, 0.35f + i * 0.05f), Rotation = new Vector3(0, -0.3f, 0) });
        var pot = new Node3D { Position = new Vector3(1.55f, -0.02f, 0.1f) };
        AddChild(pot);
        var line = PotMesh.WaterLine(ClayForm.CoilPot) * PotView.Size;
        pot.AddChild(new MeshInstance3D { Mesh = PotMesh.For(ClayForm.CoilPot, 0), Scale = Vector3.One * PotView.Size, MaterialOverride = ClayLook.For(ClayStage.Fired, FireKind.Pit, 0f, 0) });
        pot.AddChild(new MeshInstance3D { Mesh = CookingMeshes.Under });
        pot.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = line.X, BottomRadius = line.X, Height = 0.002f, RadialSegments = 20 }, MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.38f, 0.24f, 0.12f), Roughness = 0.2f }, Position = new Vector3(0, line.Y, 0) });
        pot.AddChild(new MeshInstance3D { Mesh = CookingMeshes.Floaters(CookingMeshes.Dish.Stew, line.X), Position = new Vector3(0, line.Y + 0.002f, 0) });
        var camera = new Camera3D { Fov = 50f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(-0.3f, 1.45f, 2.9f), new Vector3(-0.3f, 0.4f, -0.2f), Vector3.Up);
    }
    // Woodland mushrooms (model-34): the four patch variants ripe (back row) and picked
    // (front row), and the picked item lying beside, with a rabbit for scale.
    [ShowcaseView("mushrooms")]
    void MushroomView()
    {
        for (int v = 0; v < Mushrooms.Variants; v++)
        {
            float x = (v - 1.5f) * 0.5f;
            AddChild(new MeshInstance3D { Mesh = Mushrooms.Patch(v), Position = new Vector3(x, 0, -0.3f), Rotation = new Vector3(0, v * 1.3f, 0) });
            AddChild(new MeshInstance3D { Mesh = Mushrooms.Picked(v), Position = new Vector3(x, 0, 0.25f), Rotation = new Vector3(0, v * 1.3f, 0) });
        }
        AddChild(new MeshInstance3D { Mesh = Mushrooms.Pile, Position = new Vector3(1.15f, 0, 0.05f), Rotation = new Vector3(0, 0.6f, 0) });
        var rabbit = RabbitFigure.Create(PaintedAnimals.Coat(Look.Solid(RabbitFigure.Coats[0])));
        rabbit.Position = new Vector3(-1.25f, 0, 0);
        rabbit.Rotation = new Vector3(0, 1.2f, 0);
        AddChild(rabbit);
        var camera = new Camera3D { Fov = 34f, Current = true };
        AddChild(camera);
        if (args.ContainsKey("near")) camera.LookAtFromPosition(new Vector3(-0.5f, 0.35f, 0.75f), new Vector3(-0.5f, 0.04f, -0.15f), Vector3.Up);
        else camera.LookAtFromPosition(new Vector3(0, 1.3f, 2.4f), new Vector3(0, 0.05f, 0), Vector3.Up);
    }
}
