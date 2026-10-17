using System.Linq;
using Godot;

namespace LifeGame;

// Prints how many of each cue played (audio-37), only when the client starts with --audio-stats.
// First report after 20 s, then once a minute, and the run total on quit.
[AudioSounds]
static class CueStatsLog
{
    static readonly bool enabled = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--audio-stats");
    static double clock, next = 20;
    static bool quitHooked;

    public static void Tick(double dt)
    {
        if (!enabled) return;
        if (!quitHooked)
        {
            quitHooked = true;
            (Engine.GetMainLoop() as SceneTree)?.Root.Connect(Node.SignalName.TreeExiting, Callable.From(() =>
                GD.Print($"Audio cues (run total): {CueStats.Total() ?? "none"}")));
        }
        clock += dt;
        if (clock < next) return;
        next = clock + 60;
        GD.Print($"Audio cues (since last report): {CueStats.TakeWindow() ?? "none"}");
    }
}
