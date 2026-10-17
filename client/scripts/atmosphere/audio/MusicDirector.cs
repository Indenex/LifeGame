using System;
using System.Collections.Generic;
using System.Linq;

namespace LifeGame;

// Recorded music (audio-8): what plays when. Pure C# with no engine types, so SoundPreview
// can test it; MusicPlayer (a Godot node) does the actual streaming and fading.
//
// Tracks are data: a MusicDef per file in MusicDefs.All (see docs/MUSIC.md).

// What the world is like for choosing music.
public enum Mood { Day, Dusk, Night, Rest, Rain }

// One recorded track. Moods is a space-separated list of: day dusk night rest rain.
// Trim is a per-track volume adjustment in decibels (negative is quieter).
public sealed record MusicDef(string Id, string File, string Moods, float TrimDb = 0f)
{
    public bool Has(Mood mood) => Moods.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Any(m => string.Equals(m, mood.ToString(), StringComparison.OrdinalIgnoreCase));
}

public static class MusicDefs
{
    // Add one line per track, after dropping the .ogg into client/assets/music/. For example:
    //   new("kites", "res://assets/music/kites.ogg", "day"),
    //   new("naps", "res://assets/music/naps.ogg", "dusk night rest", -2f),
    // Nothing plays until tracks are listed.
    public static readonly MusicDef[] All =
    {
    };
}

// What the director looks at, refreshed by the game every frame.
public readonly record struct MusicContext(float TimeOfDay, float Rain, float Cover, float Fire, bool Enabled);

// What the director asks the player to do.
public enum MusicAction { None, Start, Stop }

public readonly record struct MusicCommand(MusicAction Action, MusicDef? Track = null, float FadeSeconds = 0f);

public sealed class MusicDirector
{
    public const float CrossFade = 4f;           // seconds, when the mood changes
    public const float FadeOut = 3f;
    public const float MoodHold = 6f;            // a new mood must last this long before music changes for it
    public const float MinGap = 75f, MaxGap = 200f; // silence after a track ends, seconds (never under a minute)
    public const float FirstDelayMin = 15f, FirstDelayMax = 45f;
    public const float RetryGap = 30f;           // nothing fits this mood: look again after this long

    readonly IReadOnlyList<MusicDef> defs;
    readonly Random rng;
    float gap;
    Mood mood = Mood.Day, pendingMood = Mood.Day;
    float pendingFor;
    string? lastId;

    public MusicDef? Playing { get; private set; }
    public Mood Mood => mood;
    public float GapLeft => gap;

    public MusicDirector(IReadOnlyList<MusicDef> defs, Random rng)
    {
        this.defs = defs;
        this.rng = rng;
        gap = FirstDelayMin + (float)rng.NextDouble() * (FirstDelayMax - FirstDelayMin);
    }

    // The time of day alone: dusk is around sunset (0.75 of the day), night either side of midnight.
    public static Mood TimeMood(float timeOfDay)
    {
        float t = timeOfDay - MathF.Floor(timeOfDay);
        if (t >= 0.22f && t < 0.68f) return Mood.Day;
        if (t >= 0.68f && t < 0.80f) return Mood.Dusk;
        return Mood.Night;
    }

    // The mood the world asks for: resting indoors or by a fire first, then rain, then the time of day.
    public static Mood MoodOf(MusicContext c) =>
        c.Cover > 0.5f || (c.Fire > 0.25f && TimeMood(c.TimeOfDay) != Mood.Day) ? Mood.Rest
        : c.Rain > 0.35f ? Mood.Rain
        : TimeMood(c.TimeOfDay);

    // Tracks tagged for the mood, then the time of day it falls back to.
    public static Mood[] Chain(Mood mood, float timeOfDay)
    {
        var time = TimeMood(timeOfDay);
        return mood switch
        {
            Mood.Rest or Mood.Rain => new[] { mood, time },
            Mood.Dusk => new[] { Mood.Dusk, Mood.Night },
            Mood.Night => new[] { Mood.Night, Mood.Dusk },
            _ => new[] { Mood.Day },
        };
    }

    bool Fits(MusicDef d, Mood[] chain) => chain.Any(d.Has);

    MusicDef? Pick(Mood[] chain)
    {
        // Prefer the first mood in the chain that has any track; avoid repeating the last one.
        foreach (var m in chain)
        {
            var options = defs.Where(d => d.Has(m)).ToList();
            if (options.Count == 0) continue;
            var fresh = options.Where(d => d.Id != lastId).ToList();
            var pool = fresh.Count > 0 ? fresh : options;
            return pool[rng.Next(pool.Count)];
        }
        return null;
    }

    // Call each frame; returns what the player should do now.
    public MusicCommand Update(float dt, MusicContext ctx)
    {
        if (!ctx.Enabled)
        {
            if (Playing != null)
            {
                Playing = null;
                gap = Math.Max(gap, 10f); // a short pause before music resumes when switched back on
                return new MusicCommand(MusicAction.Stop, null, FadeOut);
            }
            return default;
        }

        var wanted = MoodOf(ctx);
        if (wanted == pendingMood) pendingFor += dt;
        else { pendingMood = wanted; pendingFor = 0f; }
        if (pendingMood != mood && pendingFor >= MoodHold) mood = pendingMood;

        var chain = Chain(mood, ctx.TimeOfDay);

        if (Playing != null)
        {
            // The mood has changed to one this track doesn't fit: cross-fade to one that does,
            // or fade away if nothing fits.
            if (!Fits(Playing, chain))
            {
                var next = Pick(chain);
                if (next != null)
                {
                    Playing = next;
                    lastId = next.Id;
                    return new MusicCommand(MusicAction.Start, next, CrossFade);
                }
                Playing = null;
                gap = RetryGap;
                return new MusicCommand(MusicAction.Stop, null, FadeOut);
            }
            return default;
        }

        gap -= dt;
        if (gap > 0f) return default;
        var pick = Pick(chain);
        if (pick == null)
        {
            gap = RetryGap;
            return default;
        }
        Playing = pick;
        lastId = pick.Id;
        return new MusicCommand(MusicAction.Start, pick, CrossFade);
    }

    // The player calls this when the current track ran to its end: a long silence follows.
    public void TrackEnded()
    {
        Playing = null;
        gap = MinGap + (float)rng.NextDouble() * (MaxGap - MinGap);
    }
}
