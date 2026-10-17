using Godot;
using LifeGame.Shared;

namespace LifeGame;

// House building views for the Render lane (see Showcase.cs).
public partial class Showcase
{
    // The sundial (model-12): stage 0 and the finished dial, seen from the south-east; run
    // with --time-of-day to check the stick's shadow against the hour lines.
    [ShowcaseView("sundial")]
    void SundialView()
    {
        var pieces = StructureModel.Sundial();
        for (int c = 0; c < 2; c++)
            foreach (var piece in pieces)
                if (piece.Stage >= 0 && piece.Stage <= c)
                    AddChild(new MeshInstance3D { Mesh = piece.Mesh, MaterialOverride = piece.Material, Position = new Vector3(-0.9f + c * 1.1f, 0, c * 0.4f) });
        var camera = new Camera3D { Fov = 34f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0.4f, 2.1f, 2.4f), new Vector3(-0.2f, 0.4f, 0.1f), Vector3.Up);
        if (args.ContainsKey("time-of-day")) AddChild(new Atmosphere());
    }
}
