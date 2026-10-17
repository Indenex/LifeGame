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
}
