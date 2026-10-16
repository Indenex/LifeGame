using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The work in your hands is heard: while a hands-on minigame is open and while a
// craft job runs, SoundMix plays the sound of that kind of work, closing with a
// thunk, a snap when it failed, or a breath of flame when a drill catches. Only
// your own work: the server shows each client only its own job.
static class WorkSounds
{
    static SoundScape? sound;
    static string? minigame, job;
    static Work playing;

    public static void Connect(DbConnection conn, SoundScape scape)
    {
        sound = scape;
        conn.Db.MyCraftJob.OnInsert += (_, j) =>
        {
            job = j.ProcessId;
            Update();
        };
        conn.Db.MyCraftJob.OnDelete += (_, j) =>
        {
            job = null;
            Update();
            sound?.WorkDone(SoundMix.WorkFor(j.ProcessId), j.Failed);
        };
    }

    public static void Minigame(string processId, bool open)
    {
        minigame = open ? processId : null;
        Update();
    }

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
