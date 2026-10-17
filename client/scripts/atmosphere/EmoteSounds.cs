using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Emotes heard (audio-25): when a player waves, cheers, dances, bows, claps or shrugs, the sounds of the
// gesture, timed to Animation's poses (client/scripts/animation/WorkMotion.cs, anim-16): a swish
// of cloth as the arm goes up and down, claps at the top of each cheer hop, a tap of the foot on
// each dance beat, a rustle at each end of a bow. No voices. Everyone nearby hears it.
[AudioSounds]
static class EmoteSounds
{
    const float Hearing = 22f;   // metres

    // When each sound of an emote falls, seconds from its start, and how strongly.
    static readonly (float At, CueDef Def, float Gain)[] Wave =
    {
        (0.20f, CueDefs.EmoteSwish, 0.9f), (1.65f, CueDefs.EmoteSwish, 0.6f),
    };
    static readonly (float At, CueDef Def, float Gain)[] Cheer =
    {
        (0.28f, CueDefs.EmoteClap, 1f), (0.60f, CueDefs.EmoteTap, 0.8f),
        (1.08f, CueDefs.EmoteClap, 1f), (1.40f, CueDefs.EmoteTap, 0.8f),
    };
    static readonly (float At, CueDef Def, float Gain)[] Bow =
    {
        (0.15f, CueDefs.EmoteSwish, 0.7f), (1.25f, CueDefs.EmoteSwish, 0.8f),
    };

    // Five claps, the hands meeting at 0.25 s and then every 1/3.2 s (anim, WorkMotion.Clap).
    static (float At, CueDef Def, float Gain)[] ClapBeats()
    {
        var beats = new List<(float, CueDef, float)>();
        for (int i = 0; i < 5; i++) beats.Add((0.25f + i / 3.2f, CueDefs.EmoteHandClap, 1f));
        return beats.ToArray();
    }
    static readonly (float At, CueDef Def, float Gain)[] Clap = ClapBeats();

    // A shrug: a soft rustle as the shoulders go up, and another as they drop (1.4 s long).
    static readonly (float At, CueDef Def, float Gain)[] Shrug =
    {
        (0.25f, CueDefs.EmoteSwish, 0.8f), (1.05f, CueDefs.EmoteSwish, 0.65f),
    };

    // One beat every 0.4 s through the 3.2 s dance, the foot alternating loud and soft.
    static (float At, CueDef Def, float Gain)[] DanceBeats()
    {
        var beats = new List<(float, CueDef, float)>();
        for (int i = 0; i < 7; i++) beats.Add((0.4f + 0.4f * i, CueDefs.EmoteTap, i % 2 == 0 ? 1f : 0.65f));
        return beats.ToArray();
    }
    static readonly (float At, CueDef Def, float Gain)[] Dance = DanceBeats();

    readonly record struct Due(double At, CueDef Def, Vector3 Where, float Gain);

    static readonly List<Due> due = new();
    static double now;

    public static void Connect(DbConnection c)
    {
        c.Db.PlayerAct.OnInsert += (ctx, act) =>
        {
            if (ctx.Event is not SpacetimeDB.Event<Reducer>.Reducer) return;
            var script = (WorkMotion)act.Motion switch
            {
                WorkMotion.Wave => Wave,
                WorkMotion.Cheer => Cheer,
                WorkMotion.Dance => Dance,
                WorkMotion.Bow => Bow,
                WorkMotion.Clap => Clap,
                WorkMotion.Shrug => Shrug,
                _ => null,
            };
            if (script == null || c.Db.PlayerPos.PlayerId.Find(act.PlayerId) is not { } at) return;
            var where = new Vector3(at.X, 0.8f, at.Z);
            foreach (var (t, def, gain) in script) due.Add(new Due(now + t, def, where, gain));
        };
    }

    // Each frame from AudioHooks.
    public static void Tick(double dt)
    {
        now += dt;
        for (int i = due.Count - 1; i >= 0; i--)
        {
            if (due[i].At > now) continue;
            Sfx.PlayAnimal(due[i].Def, due[i].Where, Hearing, due[i].Gain);
            due.RemoveAt(i);
        }
    }
}
