using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Small rings where the grass lies flat (model-37), so low things in the meadow (Survival's
// mushroom and root patches) read from the camera instead of drowning in tall grass. Things
// that want one call Track(node) once; while the node is in the tree its spot counts. The
// sixteen spots nearest the player go to the grass shader (Look.GrassShader) as two mat4
// globals, grass_clear_a and grass_clear_b, each column two (x, z) pairs; unused slots sit
// far away. The grass shader lays tufts down within 0.4 m of a spot, easing back up by 0.75 m.
public partial class GrassClearings : Node
{
    public const int Slots = 16;
    static readonly HashSet<Node3D> Spots = new();
    static GrassClearings? instance;
    static readonly StringName A = "grass_clear_a", B = "grass_clear_b";
    const float Far = 1e6f;
    double wait;

    // Both globals, added with Look's other globals before the grass shader compiles.
    public static void AddGlobals()
    {
        var none = new Vector2[Slots];
        System.Array.Fill(none, new Vector2(Far, Far));
        var far = Packed(none);
        RenderingServer.GlobalShaderParameterAdd(A, RenderingServer.GlobalShaderParameterType.Mat4, far.a);
        RenderingServer.GlobalShaderParameterAdd(B, RenderingServer.GlobalShaderParameterType.Mat4, far.b);
    }

    public static void Track(Node3D node)
    {
        node.TreeEntered += () =>
        {
            lock (Spots) Spots.Add(node);
            if (instance == null && node.GetTree() is { } tree)
            {
                instance = new GrassClearings { Name = "GrassClearings" };
                tree.Root.CallDeferred(Node.MethodName.AddChild, instance);
            }
        };
        node.TreeExiting += () => { lock (Spots) Spots.Remove(node); };
    }

    public override void _ExitTree()
    {
        if (instance == this) instance = null;
    }

    public override void _Process(double delta)
    {
        // The player moves slowly next to the size of a ring; four times a second is plenty.
        wait -= delta;
        if (wait > 0) return;
        wait = 0.25;
        var player = RenderingServer.GlobalShaderParameterGet("player_position").AsVector3();
        var near = new List<(float, Vector2)>();
        lock (Spots)
            foreach (var spot in Spots)
            {
                if (!spot.IsInsideTree() || !spot.IsVisibleInTree()) continue;
                var at = spot.GlobalPosition;
                near.Add(((at - player).LengthSquared(), new Vector2(at.X, at.Z)));
            }
        near.Sort((p, q) => p.Item1.CompareTo(q.Item1));
        var chosen = new Vector2[Slots];
        for (int i = 0; i < Slots; i++) chosen[i] = i < near.Count ? near[i].Item2 : new Vector2(Far, Far);
        var (a, b) = Packed(chosen);
        RenderingServer.GlobalShaderParameterSet(A, a);
        RenderingServer.GlobalShaderParameterSet(B, b);
    }

    static (Projection a, Projection b) Packed(Vector2[] spots)
    {
        Vector2 At(int i) => spots[i];
        Vector4 Column(int i) => new(At(2 * i).X, At(2 * i).Y, At(2 * i + 1).X, At(2 * i + 1).Y);
        return (new Projection(Column(0), Column(1), Column(2), Column(3)), new Projection(Column(4), Column(5), Column(6), Column(7)));
    }

    // The grass shader's side: how flat the tuft at p lies, 0 standing to 1 flat.
    public const string ShaderCode = """
            global uniform mat4 grass_clear_a;
            global uniform mat4 grass_clear_b;
            float clear_one(vec2 p, vec2 s) { return 1.0 - smoothstep(0.4, 0.75, distance(p, s)); }
            float grass_cleared(vec2 p) {
                float k = 0.0;
                for (int c = 0; c < 4; c++) {
                    vec4 a = grass_clear_a[c];
                    vec4 b = grass_clear_b[c];
                    k = max(k, max(max(clear_one(p, a.xy), clear_one(p, a.zw)), max(clear_one(p, b.xy), clear_one(p, b.zw))));
                }
                return k;
            }
            """;
}
