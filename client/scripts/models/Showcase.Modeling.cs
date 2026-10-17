using Godot;

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
}
