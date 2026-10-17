using System;
using Godot;

namespace LifeGame;

// Hold the right mouse button and drag sideways to turn the view freely (D-050); drag up or
// down to tilt it a little.
// The camera rig eases toward the new angle, so it never snaps; Q and E still turn
// in steps. A quick right click that doesn't drag (ui-35) is a click instead: Clicked fires
// with where it was, and the player menu opens on whoever is under the cursor.
public partial class CameraDrag : Node
{
    const float RadiansPerPixel = 0.006f;
    // Tilt is gentler than turning, and the rig keeps it inside a narrow range.
    const float TiltRadiansPerPixel = 0.003f;

    // Mouse sensitivity from the Controls page (1 = the default feel).
    public static float Sensitivity = 1f;

    public CameraRig Rig = null!;
    // True while something else owns the right button (placing a building cancels on it).
    public Func<bool> Blocked = () => false;
    // A right click that didn't turn into a drag, at this screen point.
    public event Action<Vector2>? Clicked;

    // A press becomes a drag once it moves this far; let go sooner than ClickMsec without
    // getting that far and it's a click.
    const float DragPixels = 6f;
    const ulong ClickMsec = 250;

    bool held, dragging;
    Vector2 pressedAt, moved;
    ulong pressedMsec;

    public override void _UnhandledInput(InputEvent e)
    {
        // The drag button is the camera_drag action, so players can move it.
        if (e is InputEventMouseButton or InputEventKey && InputMap.HasAction("camera_drag") && e.IsAction("camera_drag") && !e.IsEcho())
        {
            if (e.IsPressed())
            {
                held = !Blocked();
                pressedAt = GetViewport().GetMousePosition();
                pressedMsec = Time.GetTicksMsec();
                moved = Vector2.Zero;
                return;
            }
            bool click = held && !dragging && Time.GetTicksMsec() - pressedMsec <= ClickMsec;
            held = dragging = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            if (click) Clicked?.Invoke(pressedAt);
        }
        else if (held && !dragging && e is InputEventMouseMotion early)
        {
            moved += early.Relative;
            if (moved.Length() < DragPixels) return;
            // Hide and hold the cursor while dragging so it never runs off the edge.
            dragging = true;
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        else if (dragging && e is InputEventMouseMotion motion)
        {
            Rig.TurnBy(-motion.Relative.X * RadiansPerPixel * Sensitivity);
            // Up and down tilts the view a little: drag up for more sky, down to look from higher.
            Rig.TiltBy(motion.Relative.Y * TiltRadiansPerPixel * Sensitivity);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Notification(int what)
    {
        // Let go if the window loses focus mid-drag.
        if (what == NotificationWMWindowFocusOut && (dragging || held))
        {
            held = dragging = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }
}
