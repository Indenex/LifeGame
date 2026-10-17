using Godot;
using LifeGame.Shared;

namespace LifeGame;

// /fly (admin-1): free flight for an admin. No gravity, no collision, very fast; the
// server skips the walking pace for a flying admin only (AdminFlyer). Space rises,
// Ctrl (sneak) sinks, the walk keys move across the view.
public partial class Main
{
    const float FlySpeed = 40f, FlyClimb = 20f;
    static readonly StringName FlyUp = "jump";
    bool flying;

    // Whether you are flying (the command flips it; Walk hands over to Fly).
    public bool Flying => flying;

    public void SetFlying(bool on)
    {
        flying = on;
        jump.Cancel();
        velocity = Vector3.Zero;
        if (!on) me.SetAirHeight(0f);
    }

    void Fly(float dt, Vector2 input)
    {
        float up = (Input.IsActionPressed(FlyUp) ? 1f : 0f) - (Input.IsActionPressed(NSneak) ? 1f : 0f);
        var wish = (cameraRig.Right * input.X + cameraRig.Forward * input.Y) * FlySpeed + Vector3.Up * up * FlyClimb;
        velocity = velocity.Lerp(wish, 1f - Mathf.Exp(-6f * dt));
        var pos = me.Position + velocity * dt;
        // Free in the air, but never under the ground.
        pos.Y = Mathf.Max(pos.Y, terrain.HeightAt(pos.X, pos.Z) + 0.2f);
        me.Position = pos;
        me.SetJump(false, false, 0f);
        if (new Vector2(velocity.X, velocity.Z).LengthSquared() > 1f)
        {
            heading = Mathf.Atan2(velocity.X, velocity.Z);
            me.Rotation = new Vector3(0, Mathf.LerpAngle(me.Rotation.Y, heading, 1f - Mathf.Exp(-12f * dt)), 0);
        }
    }
}
