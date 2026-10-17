using System;
using Godot;

namespace LifeGame;

// One-line entry point for feedback sounds from any lane (audio-3):
//   Sfx.Play(Cue.Eat);
// Safe to call before the soundscape exists or from any thread; the sound is
// queued for the synth thread. Ask the Audio lane for a new Cue rather than
// making sounds elsewhere (D-079).
//
// Sounds that happen in the world (another player's jump) use PlayAt/LandAt: they
// get quieter with distance (like other players' work, D-072: heard from about 30 m,
// well under your own) and come from their side.
public static class Sfx
{
    const float Hearing = 30f;     // metres; silent beyond
    const float OthersGain = 0.4f; // other players sit well under your own sounds

    static SoundScape? scape;
    static Vector3 listener;
    static Vector3 right = Vector3.Right;

    internal static void Attach(SoundScape? soundScape) => scape = soundScape;

    // Atmosphere reports where the listener is and which way is right, every frame.
    internal static void Listen(Vector3 position, Vector3 rightward)
    {
        listener = position;
        right = rightward;
    }

    public static void Play(Cue cue) => scape?.Play(cue);

    // A cue that happens somewhere in the world.
    public static void PlayAt(Cue cue, Vector3 worldPosition)
    {
        if (Place(worldPosition, out float gain, out float pan)) scape?.Play(cue, gain, pan);
    }

    // A blow landing on an animal at `target`, struck from `attacker`. Yours (you are within a
    // couple of metres of where it came from) plays at full volume; someone else's plays
    // from where it happened, quieter with distance.
    public static void PlayHit(Cue cue, Vector3 target, Vector3 attacker)
    {
        var offset = new Vector3(attacker.X - listener.X, 0, attacker.Z - listener.Z);
        if (offset.LengthSquared() < 2.5f * 2.5f) Play(cue);
        else PlayAt(cue, target);
    }

    // A bow shot from `from` to `to`: the twang and whoosh now, `impact` after `delay` seconds
    // (the arrow's flight). Your own shot (you are within a couple of metres of `from`) is full
    // volume; someone else's comes from where it happened, quieter with distance.
    public static void PlayShot(Vector3 from, Vector3 to, Cue impact, float delay, SceneTree tree)
    {
        bool mine = new Vector3(from.X - listener.X, 0, from.Z - listener.Z).LengthSquared() < 2.5f * 2.5f;
        if (mine) Play(Cue.BowShot); else PlayAt(Cue.BowShot, from);
        tree.CreateTimer(Math.Max(0.02f, delay)).Timeout += () =>
        {
            if (mine) Play(impact); else PlayAt(impact, to);
        };
    }

    // Touching down after a jump: strength 0 (a hop) to 1 (a long fall), and the ground underfoot.
    public static void Land(float strength, Ground ground) => scape?.Land(ground, strength);

    // The same for someone else, at their position.
    public static void LandAt(float strength, Ground ground, Vector3 worldPosition)
    {
        if (Place(worldPosition, out float gain, out float pan)) scape?.Land(ground, strength, gain, pan);
    }

    static bool Place(Vector3 at, out float gain, out float pan)
    {
        var offset = new Vector3(at.X - listener.X, 0, at.Z - listener.Z);
        float distance = offset.Length();
        gain = pan = 0f;
        if (distance >= Hearing) return false;
        gain = OthersGain * (1f - Mathf.SmoothStep(3f, Hearing, distance));
        pan = distance < 0.5f ? 0f : Mathf.Clamp(offset.Normalized().Dot(new Vector3(right.X, 0, right.Z).Normalized()), -1f, 1f) * 0.8f;
        return gain > 0.01f;
    }
}
