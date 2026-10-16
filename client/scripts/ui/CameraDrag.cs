using System;
using Godot;

namespace LifeGame;

// Hold the right mouse button and drag sideways to turn the view freely (D-050).
// The camera rig eases toward the new angle, so it never snaps; Q and E still turn
// in steps.
public partial class CameraDrag : Node
{
    const float RadiansPerPixel = 0.006f;

    // Mouse sensitivity from the Controls page (1 = the default feel).
    public static float Sensitivity = 1f;

    public CameraRig Rig = null!;
    // True while something else owns the right button (placing a building cancels on it).
    public Func<bool> Blocked = () => false;

    bool dragging;

    public override void _UnhandledInput(InputEvent e)
    {
        // The drag button is the camera_drag action, so players can move it.
        if (e is InputEventMouseButton or InputEventKey && InputMap.HasAction("camera_drag") && e.IsAction("camera_drag") && !e.IsEcho())
        {
            dragging = e.IsPressed() && !Blocked();
            // Hide and hold the cursor while dragging so it never runs off the edge.
            Input.MouseMode = dragging ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        }
        else if (dragging && e is InputEventMouseMotion motion)
        {
            Rig.TurnBy(-motion.Relative.X * RadiansPerPixel * Sensitivity);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Notification(int what)
    {
        // Let go if the window loses focus mid-drag.
        if (what == NotificationWMWindowFocusOut && dragging)
        {
            dragging = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }
}
