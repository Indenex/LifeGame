using System.Linq;
using Godot;

namespace LifeGame;

// --berry-closeup puts a ripe berry bush beside the player and frames it with a
// close camera, for checking how the berries sit in the leaves (with --screenshot).
public partial class BerryCloseup : Node3D
{
    public static bool Wanted => OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Any(a => a.StartsWith("--berry-closeup"));

    readonly Terrain terrain;
    readonly Node3D player;

    public BerryCloseup(Terrain terrain, Node3D player)
    {
        this.terrain = terrain;
        this.player = player;
    }

    // Waits a moment so the player has spawned and Main has set up its own camera.
    public override void _Ready() => GetTree().CreateTimer(3).Timeout += Frame;

    void Frame()
    {
        var at = player.Position + new Vector3(3f, 0, 3f);
        at.Y = terrain.HeightAt(at.X, at.Z) - 0.05f;
        AddChild(new BerryBush(424242) { Position = at, Ripe = true });
        var camera = new Camera3D { Fov = 40 };
        AddChild(camera);
        camera.Position = at + new Vector3(1.1f, 0.85f, 1.3f);
        camera.LookAt(at + new Vector3(0, 0.3f, 0), Vector3.Up);
        camera.MakeCurrent();
    }
}
