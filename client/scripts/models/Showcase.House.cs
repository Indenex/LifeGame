using Godot;
using LifeGame.Shared;

namespace LifeGame;

// House building views for the Render lane (see Showcase.cs).
public partial class Showcase
{
    // The stone-ringed hearth (model-9, D-286): its first course on the left, the
    // finished kerb on the right, the player beside for scale.
    [ShowcaseView("stonehearth")]
    void StoneHearthView()
    {
        var pieces = StructureModel.StoneHearth();
        for (int c = 0; c < 2; c++)
            foreach (var piece in pieces)
                if (piece.Stage >= 0 && piece.Stage <= c)
                    AddChild(new MeshInstance3D { Mesh = piece.Mesh, MaterialOverride = piece.Material, Position = new Vector3(-0.75f + c * 1.5f, 0, 0) });
        var figure = PlayerModel.Build();
        figure.Position = new Vector3(2.0f, 0, -0.4f);
        figure.Rotation = new Vector3(0, -0.6f, 0);
        AddChild(figure);
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0.3f, 1.9f, 3.0f), new Vector3(0.3f, 0.15f, 0), Vector3.Up);
    }

    // The hen coop (model-7) at each build stage, frame, walls and roof, left to right,
    // turned a little so the pop-hole, ramp and side show.
    [ShowcaseView("coop")]
    void Coop()
    {
        int kind = (int)SpacetimeDB.Types.StructureKind.HenCoop;
        for (int c = 0; c < 3; c++)
            foreach (var piece in StructureModel.Pieces(kind))
                if (piece.Stage >= 0 && piece.Stage <= c)
                    AddChild(new MeshInstance3D { Mesh = piece.Mesh, MaterialOverride = piece.Material, Position = new Vector3(-2.6f + c * 2.6f, 0, 0), Rotation = new Vector3(0, 0.55f, 0) });
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 2.4f, 6.2f), new Vector3(0, 0.55f, 0), Vector3.Up);
    }

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
