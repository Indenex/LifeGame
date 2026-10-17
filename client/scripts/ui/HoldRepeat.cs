using Godot;

namespace LifeGame;

// Hold a key to repeat its action (Justin: harvesting, digging and the like without clicking
// each time). Timings are untouched: the server still checks every action (reach, cooldown,
// tools), so holding is never faster than tapping. A tap does one action; holding repeats it
// after a short delay, each time the last one is done, and stops by itself when the key is
// released, the action fails (target gone or used up, bag full, a tool or need missing), or
// the player walks away.
public sealed class HoldRepeat
{
    // Before the first repeat, so a tap still does one action.
    public const float Delay = 0.4f;
    // Walking this far from where the hold began stops it (a step, not a stroll: crafting-31).
    const float MoveAway = 0.3f;

    // Every hold in the game (Use, dig, pick up), so Active can say whether any is repeating.
    static readonly System.Collections.Generic.List<HoldRepeat> all = new();
    public HoldRepeat() => all.Add(this);

    // True while a held key is armed to repeat (key down past the delay and not stopped): the HUD
    // cue for "holding" reads this (UX).
    public static bool Active
    {
        get
        {
            foreach (var h in all)
                if (h.Armed) return true;
            return false;
        }
    }

    // Stops every hold at once (a job was cancelled by walking off, a panel or a change of hand);
    // each starts again only when its key is pressed anew.
    public static void StopAll()
    {
        foreach (var h in all) h.stopped = true;
    }

    bool Armed => held >= Delay && !stopped;

    float held;
    ulong lastActionMsec;
    bool stopped;
    Vector3 origin;

    // Call when the first action runs (the press), and for every repeat.
    public void Acted(Vector3 at)
    {
        if (held <= 0f) origin = at;
        lastActionMsec = Time.GetTicksMsec();
    }

    // True when the action should run again now. `down` is the key state, `interval` the
    // least time between actions, `busy` whether the last one is still going (a craft job).
    public bool Update(bool down, float dt, float interval, bool busy, Vector3 at)
    {
        if (!down)
        {
            held = 0f;
            stopped = false;
            return false;
        }
        held += dt;
        // A reducer error after the last action: there is nothing (more) to do, so stop until the key is pressed again.
        if (Use.FailedAtMsec >= lastActionMsec && lastActionMsec > 0) stopped = true;
        if ((at - origin).Length() > MoveAway) stopped = true;
        if (stopped || busy || held < Delay) return false;
        return (Time.GetTicksMsec() - lastActionMsec) / 1000f >= interval;
    }

    // The key went down: a new hold begins.
    public void Pressed(Vector3 at)
    {
        held = 0.0001f;
        stopped = false;
        origin = at;
        lastActionMsec = Time.GetTicksMsec();
    }
}
