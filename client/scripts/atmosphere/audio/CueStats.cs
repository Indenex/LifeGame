using System.Collections.Generic;
using System.Linq;

namespace LifeGame;

// Counts every cue and footstep the synth starts (audio-37), so a headless run with no audio
// device can still show that sounds were triggered. Cues start on the audio thread, so access is locked.
public static class CueStats
{
    static readonly object gate = new();
    static readonly Dictionary<string, int> counts = new();
    static readonly Dictionary<string, int> total = new();

    public static void Count(string id)
    {
        lock (gate)
        {
            counts[id] = counts.GetValueOrDefault(id) + 1;
            total[id] = total.GetValueOrDefault(id) + 1;
        }
    }

    // One line: "id=N, id=N" for what played since the last call (null if nothing), and clears that window.
    public static string? TakeWindow() => Take(counts, clear: true);

    // The same for the whole run.
    public static string? Total() => Take(total, clear: false);

    static string? Take(Dictionary<string, int> d, bool clear)
    {
        lock (gate)
        {
            if (d.Count == 0) return null;
            var line = string.Join(", ", d.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"));
            if (clear) d.Clear();
            return line;
        }
    }
}
