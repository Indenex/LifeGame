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

    public static bool Wanted => OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Any(a => a.StartsWith("--ceramics-demo"));

    // --ceramics-demo=kiln shows a cold kiln and a firing one instead of the pot row.
    static bool ShowKilns => OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--ceramics-demo=kiln");

    // --ceramics-demo=shape also opens the pinch-pot minigame part way through.
    static bool ShowMinigame => OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--ceramics-demo=shape");

    // --ceramics-demo=jar shows the storage jar wet, leather-hard, dry and kiln-fired, a coil pot for scale (ceramics-6).
    static bool ShowJars => OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--ceramics-demo=jar");

    public CeramicsDemo(Terrain terrain, Node3D anchor)
    {
        this.terrain = terrain;
        this.anchor = anchor;
    }

    public override void _Ready()
    {
        AddChild(row);
        if (ShowKilns)
        {
            var cold = new KilnView(withBricks: true) { Position = new Vector3(-1.1f, 0, 0), Rotation = new Vector3(0, 0.35f, 0) };
            cold.Show(0, false, 0, 0);
            row.AddChild(cold);
            var hot = new KilnView(withBricks: true) { Position = new Vector3(1.2f, 0, -0.3f), Rotation = new Vector3(0, -0.3f, 0) };
            hot.Show(4, true, PotView.Now - 30, PotView.Now + 150);
            row.AddChild(hot);
            for (int i = 0; i < 3; i++)
            {
                var pot = new PotView { Position = new Vector3(-0.6f + i * 0.45f, 0, 1.2f) };
                row.AddChild(pot);
                pot.Show((ulong)i + 20, i == 1 ? ClayForm.PinchPot : ClayForm.CoilPot, ClayStage.Fired, FireKind.Kiln, 0f, 0, 0f, i == 2);
            }
            AddChild(camera);
            camera.MakeCurrent();
            return;
        }
        if (ShowJars)
        {
            var jars = new (ClayForm form, ClayStage stage, FireKind fire, float moisture)[]
            {
                (ClayForm.StorageJar, ClayStage.Drying, FireKind.Pit, 1f),
                (ClayForm.StorageJar, ClayStage.Drying, FireKind.Pit, 0.4f),
                (ClayForm.StorageJar, ClayStage.Drying, FireKind.Pit, 0f),
                (ClayForm.StorageJar, ClayStage.Fired, FireKind.Kiln, 0f),
                (ClayForm.CoilPot, ClayStage.Fired, FireKind.Kiln, 0f),
            };
            for (int i = 0; i < jars.Length; i++)
            {
                var j = jars[i];
                var pot = new PotView { Position = new Vector3((i - (jars.Length - 1) / 2f) * 0.8f, 0, 0) };
                row.AddChild(pot);
                pot.Show((ulong)i + 30, j.form, j.stage, j.fire, j.moisture, PotView.Now, 0f, false);
            }
            AddChild(camera);
            camera.MakeCurrent();
            return;
        }
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
            // Drying clay glows underfoot as in play: the first as the one G would tend.
            if (s.stage == ClayStage.Drying) pot.Highlight = i == 0 ? 2 : 1;
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
        if (ShowMinigame)
            GetTree().CreateTimer(1.0).Timeout += () =>
            {
                foreach (var ui in GetTree().Root.FindChildren("*", "", true, false).OfType<CraftingUi>())
                    ui.DemoClayMinigame("clay.pinch_pot", 0.55f);
            };
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
        if (ShowJars)
        {
            camera.Position = at + new Vector3(0f, 1.6f, 4.6f);
            camera.LookAt(at + new Vector3(0f, 0.35f, 0), Vector3.Up);
            return;
        }
        if (ShowKilns)
        {
            camera.Position = at + new Vector3(0.2f, 2.0f, 5.6f);
            camera.LookAt(at + new Vector3(0.1f, 0.6f, 0), Vector3.Up);
            return;
        }
        camera.Position = at + new Vector3(0.3f, 1.5f, 4.0f);
        camera.LookAt(at + new Vector3(0.3f, 0.1f, 0), Vector3.Up);
    }
}
