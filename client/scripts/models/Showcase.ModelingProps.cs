using Godot;

namespace LifeGame;

// Modeling's prop views (#679), kept apart from Showcase.Modeling.cs.
public partial class Showcase
{
    // Environment's fallen giant (model-19): FallenLog's broken variant at landmark scale
    // in front, then four of FallenGiantLog's variants with their root plates, a person for scale.
    [ShowcaseView("giantlog")]
    void GiantLogs()
    {
        var logs = new (Mesh Mesh, Vector3 At, float Yaw)[]
        {
            (PropMeshes.FallenLog(1), new Vector3(-7f, 0, -4f), 0.75f),
            (PropMeshes.FallenGiantLog(0), new Vector3(4f, 0, 2f), 0.75f),
            (PropMeshes.FallenGiantLog(1), new Vector3(8f, 0, -10f), 0.75f),
            (PropMeshes.FallenGiantLog(2), new Vector3(-3f, 0, -14f), 0.6f),
            (PropMeshes.FallenGiantLog(5), new Vector3(15f, 0, -3f), 0.9f),
        };
        foreach (var (mesh, at, yaw) in logs)
        {
            var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, UseCustomData = true, Mesh = mesh, InstanceCount = 1 };
            multi.SetInstanceTransform(0, new Transform3D(new Basis(Vector3.Up, yaw).Scaled(Vector3.One * 3.5f), at));
            multi.SetInstanceColor(0, mesh == logs[0].Mesh ? Look.Bark.Darkened(0.1f) : PropMeshes.GiantTint);
            AddChild(new MultiMeshInstance3D { Multimesh = multi, MaterialOverride = Painted.Bark });
        }
        var person = PlayerModel.Build();
        person.Position = new Vector3(-1f, 0, 6f);
        AddChild(person);
        var camera = new Camera3D { Fov = 45f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(-9f, 7f, 18f), new Vector3(0f, 1f, -1f), Vector3.Up);
    }
}
