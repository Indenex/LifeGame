using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The work in your hands is heard: while a hands-on minigame is open and while a
// craft job runs, SoundMix plays the sound of that kind of work, closing with a
// thunk, a snap when it failed, or a breath of flame when a drill catches.
// Other players' work is heard too, 60% quieter than your own and fading out
// with distance (D-072); the public craft_work table says who is working at what.
static class WorkSounds
{
    // Others' work, against your own: 60% quieter at arm's length.
    const float OthersGain = 0.4f;
    // Heard fully within Near metres, gone beyond Far.
    const float Near = 3f, Far = 30f;

    static SoundScape? sound;
    static DbConnection? conn;
    static string? minigame, job;
    static Work playing;
    static readonly HashSet<uint> working = new();
    static float placeIn;

    public static void Connect(DbConnection c, SoundScape scape)
    {
        conn = c;
        sound = scape;
        c.Db.MyCraftJob.OnInsert += (_, j) =>
        {
            job = j.ProcessId;
            Update();
        };
        c.Db.MyCraftJob.OnDelete += (_, j) =>
        {
            job = null;
            Update();
            sound?.WorkDone(SoundMix.WorkFor(j.ProcessId), j.Failed);
        };
        c.Db.CraftWork.OnInsert += (_, w) =>
        {
            if (IsMe(w.PlayerId)) return;
            working.Add(w.PlayerId);
            sound?.OtherWorking(w.PlayerId, SoundMix.WorkFor(w.ProcessId));
        };
        c.Db.CraftWork.OnDelete += (_, w) =>
        {
            if (!working.Remove(w.PlayerId)) return;
            sound?.OtherWorking(w.PlayerId, Work.None);
            sound?.OtherWorkDone(w.PlayerId, SoundMix.WorkFor(w.ProcessId), w.Failed);
        };
    }

    public static void Minigame(string processId, bool open)
    {
        minigame = open ? processId : null;
        Update();
    }

    // Each frame from Atmosphere: where the others at work are, from the listener.
    public static void Place(float dt, Vector3 listener, Vector3 right)
    {
        placeIn -= dt;
        if (placeIn > 0f || working.Count == 0 || conn == null || sound == null) return;
        placeIn = 0.1f;
        foreach (var id in working)
        {
            if (conn.Db.PlayerPos.PlayerId.Find(id) is not { } p)
            {
                sound.OtherPlace(id, 0f, 0f);
                continue;
            }
            var toward = new Vector3(p.X - listener.X, 0f, p.Z - listener.Z);
            float d = toward.Length();
            float gain = OthersGain * (1f - Mathf.SmoothStep(Near, Far, d));
            float pan = d > 0.5f ? Mathf.Clamp(toward.Normalized().Dot(new Vector3(right.X, 0f, right.Z).Normalized()), -1f, 1f) * Mathf.Min(1f, d / 4f) : 0f;
            sound.OtherPlace(id, gain, pan);
        }
    }

    static bool IsMe(uint playerId) =>
        conn?.Identity is { } me && conn.Db.Player.Identity.Find(me) is { } p && p.PlayerId == playerId;

    static void Update()
    {
        var work = SoundMix.WorkFor(job ?? minigame ?? "");
        if (work == playing) return;
        playing = work;
        sound?.Working(work);
    }
}

// Crafting's sound hook (CraftingUi.MinigameSound), implemented in Atmosphere's files.
public partial class CraftingUi
{
    static partial void MinigameSound(ProcessDef def, bool playing) => WorkSounds.Minigame(def.Id, playing);
}
