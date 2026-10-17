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

    // --ceramics-demo=lamp shows three lit clay lamps and a cold one, for judging the glow at night (ceramics-9).
    static bool ShowLamps => OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--ceramics-demo=lamp");

    // --ceramics-demo=marks shows kiln-fired coil pots with each of the twelve maker's marks
    // facing the camera, for judging the glyphs (ceramics-15, rough prototype art).
    const float SheetLift = 2.6f;
    static bool ShowMarkSheet => OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--ceramics-demo=marks-sheet");

    static bool ShowMarks => OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--ceramics-demo=marks");

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
        if (ShowMarkSheet)
        {
            // All the glyphs flat on a clay slab, for judging the line art (rough, until Justin picks).
            // _Process puts row children on the terrain plus their "lift", so the height is a lift.
            var slab = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(4.9f, 4.3f, 0.05f) },
                MaterialOverride = ClayLook.For(ClayStage.Fired, FireKind.Kiln, 0f, 3),
                Position = new Vector3(0, 0, -0.04f),
            };
            slab.SetMeta("lift", SheetLift);
            row.AddChild(slab);
            var sheet = new MeshInstance3D
            {
                Mesh = new QuadMesh { Size = new Vector2(4.8f, 4.8f * MakerStamp.Rows / MakerStamp.Cols) },
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoTexture = MakerStamp.Atlas,
                    AlbedoColor = new Color(Marks.Presets[1]),
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                },
            };
            sheet.SetMeta("lift", SheetLift);
            row.AddChild(sheet);
            AddChild(camera);
            camera.MakeCurrent();
            return;
        }
        if (ShowMarks)
        {
            // Twelve sample marks across the groups, in the preset colours, facing the camera.
            int[] slots = { 0, 3, 6, 10, 12, 17, 20, 22, 30, 34, 38, 44 };
            for (int g = 0; g < slots.Length; g++)
            {
                ulong id = (ulong)(70 + g);
                var plain = ClayLook.For(ClayStage.Fired, FireKind.Kiln, 0f, id);
                var form = g % 3 == 2 ? ClayForm.StorageJar : ClayForm.CoilPot;
                row.AddChild(new MeshInstance3D
                {
                    Mesh = PotMesh.For(form, id),
                    MaterialOverride = ClayLook.Stamped(plain, slots[g], new Color(Marks.Presets[g % Marks.Presets.Length]), form),
                    // Turned so the stamp faces the camera, which looks along -Z.
                    Position = new Vector3((g % 6 - 2.5f) * 0.7f, 0, -(g / 6) * 1.1f),
                    Rotation = new Vector3(0, -Mathf.Pi / 2f, 0),
                    Scale = Vector3.One * PotView.Size,
                });
            }
            AddChild(camera);
            camera.MakeCurrent();
            return;
        }
        if (ShowLamps)
        {
            for (int i = 0; i < 4; i++)
            {
                var lamp = new LampView { Position = new Vector3((i - 1.5f) * 1.1f, 0, (i % 2) * 0.4f) };
                row.AddChild(lamp);
                lamp.Show((ulong)i + 40, i == 3 ? 0 : PotView.Now + 1800);
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
        if (ShowMarkSheet)
        {
            // Straight on, centred on the slab, far enough back for all seven rows at this field of view.
            camera.Position = at + new Vector3(0f, SheetLift + 0.4f, 7.4f);
            camera.LookAt(at + new Vector3(0f, SheetLift, 0), Vector3.Up);
            return;
        }
        if (ShowMarks)
        {
            // High enough to see over the front row to the back one.
            camera.Position = at + new Vector3(0f, 2.3f, 4.6f);
            camera.LookAt(at + new Vector3(0f, 0.2f, -0.5f), Vector3.Up);
            return;
        }
        if (ShowLamps)
        {
            camera.Position = at + new Vector3(0f, 1.2f, 3.2f);
            camera.LookAt(at + new Vector3(0f, 0.1f, 0), Vector3.Up);
            return;
        }
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
