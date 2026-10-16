using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// --frame-stats=path.csv writes one line per second of frame timing, script time,
// draw calls and managed allocations, and logs any single frame that took more
// than twice the median as a hitch. Used to find stutters on busy scenes.
public partial class FrameStats : Node
{
    readonly string path;
    readonly List<double> frames = new();
    readonly List<double> process = new();
    FileAccess? file;
    double second;
    long allocated = GC.GetTotalAllocatedBytes();
    int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
    ulong lastTicks = Time.GetTicksUsec();

    public FrameStats(string path) { this.path = path; ProcessPriority = int.MaxValue; }

    public override void _Ready()
    {
        file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file?.StoreLine("t,fps,frame_avg_ms,frame_p99_ms,frame_max_ms,script_avg_ms,script_max_ms,draw_calls,objects,primitives,nodes,alloc_kb_per_s,gc0,gc1,gc2,hitches");
    }

    public override void _Process(double delta)
    {
        // Wall time between frames, so render and GC pauses count too.
        ulong now = Time.GetTicksUsec();
        frames.Add((now - lastTicks) / 1000.0);
        lastTicks = now;
        process.Add(Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000);
        second += delta;
        if (second < 1) return;

        var sorted = frames.OrderBy(f => f).ToList();
        double median = sorted[sorted.Count / 2];
        int hitches = frames.Count(f => f > 2 * median && f > 20);
        long alloc = GC.GetTotalAllocatedBytes();
        int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
        file?.StoreLine(string.Join(",",
            Math.Round(Time.GetTicksMsec() / 1000.0, 1),
            Engine.GetFramesPerSecond(),
            frames.Average().ToString("F1"),
            sorted[(int)(sorted.Count * 0.99)].ToString("F1"),
            sorted[^1].ToString("F1"),
            process.Average().ToString("F2"),
            process.Max().ToString("F2"),
            Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame),
            Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame),
            Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame),
            Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),
            ((alloc - allocated) / 1024.0 / second).ToString("F0"),
            g0 - gen0, g1 - gen1, g2 - gen2,
            hitches));
        file?.Flush();
        allocated = alloc; gen0 = g0; gen1 = g1; gen2 = g2;
        frames.Clear(); process.Clear(); second = 0;
    }
}
