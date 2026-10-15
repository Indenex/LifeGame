using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// --ceramics-demo lays out every stage of the clay chain beside the player with a
// close camera, for screenshots and for judging the look without a server.
public partial class CeramicsDemo : Node3D
{
    readonly Terrain terrain;
    readonly Node3D anchor;
    readonly Camera3D camera = new() { Fov = 38f, Near = 0.05f, Far = 200f };
    readonly Node3D row = new();

    public static bool Wanted => OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--ceramics-demo");

    public CeramicsDemo(Terrain terrain, Node3D anchor)
    {
        this.terrain = terrain;
        this.anchor = anchor;
    }

    public override void _Ready()
    {
        AddChild(row);
        var stages = new (ClayForm form, ClayStage stage, FireKind fire, float moisture, bool full)[]
        {
            (ClayForm.PinchPot, ClayStage.Drying, FireKind.Pit, 1f, false),
            (ClayForm.CoilPot, ClayStage.Drying, FireKind.Pit, 1f, false),
            (ClayForm.CoilPot, ClayStage.Drying, FireKind.Pit, 0.4f, false),
            (ClayForm.CoilPot, ClayStage.Drying, FireKind.Pit, 0f, false),
            (ClayForm.CoilPot, ClayStage.Fired, FireKind.Pit, 0f, false),
            (ClayForm.CoilPot, ClayStage.Cracked, FireKind.Pit, 0f, false),
            (ClayForm.CoilPot, ClayStage.Fired, FireKind.Kiln, 0f, true),
            (ClayForm.PinchPot, ClayStage.Fired, FireKind.Pit, 0f, true),
        };
        for (int i = 0; i < stages.Length; i++)
        {
            var s = stages[i];
            var pot = new PotView { Position = new Vector3((i - (stages.Length - 1) / 2f) * 0.55f, 0, 0) };
            row.AddChild(pot);
            pot.Show((ulong)i + 1, s.form, s.stage, s.fire, s.moisture, PotView.Now, 0f, s.full);
        }
        // A short stack of sun-dried bricks at the end of the row.
        for (int i = 0; i < 3; i++)
        {
            var brick = new MeshInstance3D
            {
                Mesh = PotMesh.For(ClayForm.Brick, (ulong)i),
                MaterialOverride = ClayLook.For(ClayStage.Drying, FireKind.Pit, i == 2 ? 0.5f : 0f, (ulong)i),
                Position = new Vector3(stages.Length / 2f * 0.55f + 0.35f, 0, (i % 2) * 0.05f),
                Rotation = new Vector3(0, i * 0.3f, 0),
                Scale = Vector3.One * PotView.Size,
            };
            brick.SetMeta("lift", i * 0.13f);
            row.AddChild(brick);
        }
        AddChild(camera);
        camera.MakeCurrent();
    }

    public override void _Process(double delta)
    {
        var at = anchor.Position + new Vector3(0, 0, -2.2f);
        row.Position = at;
        foreach (var child in row.GetChildren().OfType<Node3D>())
        {
            float lift = (float)child.GetMeta("lift", 0f);
            var p = at + child.Position;
            child.Position = new Vector3(child.Position.X, terrain.HeightAt(p.X, p.Z) - at.Y + lift, child.Position.Z);
        }
        anchor.Visible = false;
        camera.Position = at + new Vector3(0.3f, 1.5f, 4.0f);
        camera.LookAt(at + new Vector3(0.3f, 0.1f, 0), Vector3.Up);
    }
}
