using System;
using System.Linq;
using Godot;

namespace LifeGame;

// ui-82: "Show the way" to a friend. A small painted arrow at top centre, under the sundial,
// turning as you turn, with their name and roughly how far ("Mara   40 m"). No box (D-045).
// It reads the server's rounded spot for friends who marked you back (claims-32), so it is
// never more exact than the Friends list. It lets go by itself when you reach them or they
// leave, with a quiet note, and fades under open panels. ui-83: the same arrow shows the way home
// (K away from your claim), pointing at the marker of your nearest claim.
public partial class FriendWay : CanvasLayer
{
    // Where a friend is, when the server says they're online; null otherwise.
    public Func<uint, Vector2?> Spot = _ => null;
    public Func<Vector3> MyPos = () => Vector3.Zero;
    public Func<uint, string> NameOf = _ => "Your friend";
    public Action<string> Note = _ => { };

    // Closer than this counts as found (the list's "close by").
    const float Found = 12f;
    const float Width = 200f, Height = 52f;

    // The friend it is showing the way to; 0 when none, or when it is showing the way home.
    public uint Target { get; private set; }
    public bool ShowingHome => home != null;
    string name = "";
    Vector2? home;
    WayView view = null!;
    float alpha;

    // --screenshot-way: points at a pretend friend 40 m ahead and a little to the right.
    static readonly bool Preview = OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--screenshot-way"));

    public override void _Ready()
    {
        Layer = 2;
        view = new WayView { CustomMinimumSize = new Vector2(Width, Height), MouseFilter = Control.MouseFilterEnum.Ignore };
        view.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        view.GrowHorizontal = Control.GrowDirection.Both;
        view.Size = new Vector2(Width, Height);
        AddChild(view);
        if (Preview) { Target = uint.MaxValue; name = "Mara"; }
    }

    // Start showing the way to this friend, or stop if it already is.
    public void Follow(uint friend)
    {
        if (Target == friend) { Stop(); return; }
        Target = friend;
        home = null;
        name = NameOf(friend);
    }

    // Show the way to your claim's marker, named by the place or "Home".
    public void Home(string label, Vector2 at)
    {
        Target = 0;
        home = at;
        name = label;
    }

    public void Stop()
    {
        Target = 0;
        home = null;
    }

    bool Showing => Target != 0 || home != null;

    public override void _Process(double delta)
    {
        bool show = Showing && !Panels.Open();
        if (Showing && !Preview && !Aim()) show = false;
        if (Preview) { view.Angle = 0.35f; view.Text = $"{name}   40 m"; }
        alpha = Mathf.MoveToward(alpha, show ? 1f : 0f, (float)delta * 3f);
        view.Modulate = new Color(1, 1, 1, alpha);
        view.Visible = alpha > 0.01f;
        view.Position = new Vector2(-Width / 2, 10 + SundialArc.Room);
        if (view.Visible) view.QueueRedraw();
    }

    // Turns the arrow toward the friend; false (and lets go) once they're found or gone.
    bool Aim()
    {
        if ((home ?? Spot(Target)) is not { } at) { Note($"{name} has gone"); Stop(); return false; }
        var me = MyPos();
        float dx = at.X - me.X, dz = at.Y - me.Z;
        float d = Mathf.Sqrt(dx * dx + dz * dz);
        if (d < Found) { Note(home != null ? "You're home" : $"You've found {name}"); Stop(); return false; }
        if (GetViewport().GetCamera3D() is { } cam)
        {
            // Ahead is where the camera looks, flattened onto the ground.
            var ahead = -cam.GlobalBasis.Z; ahead.Y = 0;
            var right = cam.GlobalBasis.X; right.Y = 0;
            var to = new Vector3(dx, 0, dz);
            view.Angle = Mathf.Atan2(to.Dot(right.Normalized()), to.Dot(ahead.Normalized()));
        }
        view.Text = d >= 300f ? $"{name}   far off" : $"{name}   {(int)Mathf.Round(d / 10f) * 10} m";
        return true;
    }
}

// The arrow (a soft chevron that points up for straight ahead) and the words under it.
public partial class WayView : Control
{
    public float Angle;
    public string Text = "";

    public override void _Draw()
    {
        var centre = new Vector2(Size.X / 2, 16);
        var turn = Transform2D.Identity.Rotated(Angle);
        Vector2 P(float x, float y) => centre + turn * new Vector2(x, y);
        var tip = P(0, -10); var left = P(-9, 6); var right = P(9, 6); var notch = P(0, 1);
        var shade = WarmLook.On ? new Color(0.22f, 0.12f, 0.06f, 0.35f) : new Color(0, 0, 0, 0.35f);
        var ink = new Color(Look.Cream, 0.95f);
        // A soft shadow first, then the arrowhead with a notched back, like a cut leaf.
        DrawColoredPolygon(new[] { tip + new Vector2(0, 2), right + new Vector2(0, 2), notch + new Vector2(0, 2), left + new Vector2(0, 2) }, shade);
        DrawColoredPolygon(new[] { tip, right, notch, left }, ink);
        DrawCircle(centre, 15f, new Color(Look.Cream, 0.06f));
        var font = Ui.Font();
        const int size = 14;
        float w = font.GetStringSize(Text, HorizontalAlignment.Left, -1, size).X;
        var at = new Vector2((Size.X - w) / 2, 46);
        DrawString(font, at + new Vector2(0, 2), Text, HorizontalAlignment.Left, -1, size, shade);
        DrawString(font, at, Text, HorizontalAlignment.Left, -1, size, new Color(Look.Cream, 0.92f));
    }
}
