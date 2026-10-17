using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Your own jump (bugfix-25): the arc, the frozen takeoff direction and the cooldown.
// Main.Walk asks it each frame; the arc and rules come from shared/Jumping.cs.
public sealed class JumpMotion
{
    float sinceTakeoff = float.MaxValue;
    Vector3 launch;
    // Wind-up prototype (anim jump wind-up, waits on Justin): the knees dip this long
    // before the feet leave the ground. 0 takes off on the press as before.
    public const float WindUpSeconds = 0.1f;
    float windUp = -1f;
    // 0 to 1 through the wind-up dip, 0 otherwise.
    public float WindUp => windUp >= 0f ? 1f - windUp / WindUpSeconds : 0f;

    public bool Airborne { get; private set; }
    public bool Rising => Airborne && Jumping.Rising(sinceTakeoff);
    // How high the feet are now.
    public float AirHeight => Airborne ? Jumping.Height(sinceTakeoff) : 0f;
    // Set for the single frame you touch down, 0 otherwise.
    public float Landing { get; private set; }
    // Where you were heading when you left the ground; you can't steer in the air.
    public Vector3 Launch => launch;

    public bool CanStart(bool sneaking) => windUp < 0f && Jumping.CanStart(!Airborne, sinceTakeoff, sneaking);

    public void Start(Vector3 velocity)
    {
        launch = velocity;
        if (WindUpSeconds > 0f) { windUp = WindUpSeconds; return; }
        TakeOff();
    }

    void TakeOff()
    {
        windUp = -1f;
        Airborne = true;
        sinceTakeoff = 0f;
    }

    // Drops the jump where it is (taking off into /fly mid-hop).
    public void Cancel()
    {
        windUp = -1f;
        Airborne = false;
        Landing = 0f;
    }

    // Advances the arc; true on the frame it ends.
    public bool Tick(float dt)
    {
        Landing = 0f;
        if (windUp >= 0f && (windUp -= dt) < 0f) TakeOff();
        if (sinceTakeoff < float.MaxValue / 2f) sinceTakeoff += dt;
        if (Airborne && sinceTakeoff >= Jumping.AirSeconds)
        {
            Airborne = false;
            Landing = Jumping.LandingStrength;
            return true;
        }
        return false;
    }
}
