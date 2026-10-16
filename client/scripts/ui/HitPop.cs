using Godot;

namespace LifeGame;

// The cue for a sneak attack's bonus hit (Animals, PR #219): an amber "!" that pops
// over the animal with a short starburst behind it, rises a little and fades, all in
// under a second. No numbers. Plain hits get nothing, so a crit stands out. Animals
// calls HitPop.Show from Herd.cs for each AnimalHit event; Main adds one HitPop.
public partial class HitPop : CanvasLayer
{
    static HitPop? instance;
    internal static readonly Color Amber = new(0.98f, 0.76f, 0.38f);

    public override void _Ready()
    {
        Layer = 2;
        instance = this;
        // --screenshot-hitpop shows a crit every 1.2 s, 4 m in front of the camera.
        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshot-hitpop") >= 0)
        {
            var timer = new Timer { WaitTime = 1.2, Autostart = true };
            timer.Timeout += () =>
            {
                if (GetViewport().GetCamera3D() is { } cam)
                    Show(cam.GlobalPosition - cam.GlobalBasis.Z * 4f - Vector3.Up * 1.1f, true, false);
            };
            AddChild(timer);
        }
    }

    public override void _ExitTree()
    {
        if (instance == this) instance = null;
    }

    // At (x, z) on the ground; height lifts the pop above the animal's back.
    public static void Show(Vector3 at, bool crit, bool killed)
    {
        if (!crit || instance == null) return;
        instance.AddChild(new HitPopMark(at + Vector3.Up * 1.1f, killed));
    }
}

// One pop on screen, following the point it was shown at.
public partial class HitPopMark : Control
{
    const float Life = 0.85f;
    readonly Vector3 world;
    readonly bool big;
    float age;

    public HitPopMark(Vector3 world, bool killed)
    {
        this.world = world;
        big = killed;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        age += (float)delta;
        if (age >= Life) { QueueFree(); return; }
        var camera = GetViewport().GetCamera3D();
        if (camera == null || camera.IsPositionBehind(world)) { Visible = false; return; }
        Visible = true;
        // Rises about 24 px over its life.
        Position = camera.UnprojectPosition(world) - new Vector2(0, 24f * age / Life);
        QueueRedraw();
    }

    public override void _Draw()
    {
        float t = age / Life;
        // Pops in past full size, settles, then fades over the last half.
        float pop = t < 0.15f ? Mathf.Lerp(0.4f, 1.25f, t / 0.15f) : Mathf.Lerp(1.25f, 1f, Mathf.Min(1f, (t - 0.15f) / 0.2f));
        float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) / 0.5f;
        float scale = (big ? 1.3f : 1f) * pop;

        // A starburst of eight short rays that fly out and thin.
        int rays = 8;
        float inner = 14f * scale + 18f * t, outer = inner + 10f * (1f - t);
        for (int i = 0; i < rays; i++)
        {
            float a = Mathf.Tau * i / rays + 0.2f;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            DrawLine(dir * inner, dir * outer, new Color(HitPop.Amber, alpha * 0.85f), 2.5f * (1f - t) + 0.5f, true);
        }

        // The "!" itself, with a dark edge so it reads on grass and sky alike.
        var font = ThemeDB.FallbackFont;
        int size = (int)(30 * scale);
        var mark = "!";
        var w = font.GetStringSize(mark, HorizontalAlignment.Left, -1, size);
        var at = new Vector2(-w.X / 2, size * 0.35f);
        DrawStringOutline(font, at, mark, HorizontalAlignment.Left, -1, size, 6, new Color(0, 0, 0, alpha * 0.55f));
        DrawString(font, at, mark, HorizontalAlignment.Left, -1, size, new Color(HitPop.Amber, alpha));
    }
}
