using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Tool work heard (audio-14): the axe biting the trunk on each swing while a chop runs (yours and
// other players'), the creak and crash when a tree is felled, all CueDefs timed off Animation's
// motions. Digging is voiced from the player_act (Atmosphere.HearAct), not here.
static class ToolSounds
{
    // Animation's Chop: a swing every 1/0.85 s, the blade landing at 0.78 of it.
    const double ChopPeriod = 1.0 / 0.85, ChopContact = 0.78 / 0.85;
    const float TreeHearing = 70f;
    const double CrashDelay = 0.95;    // the creak ends in a snap, then the tree is on its way
    const double SyncSettle = 5.0;     // ignore felled rows that arrive in the first seconds (the initial sync)

    sealed class Loop { public CueDef Def = CueDefs.ChopThunk; public double NextAt; public uint Player; public bool Mine; }

    static DbConnection? conn;
    static double now;
    static readonly Dictionary<uint, Loop> loops = new();
    static readonly List<(double At, CueDef Def, Vector3 Where)> later = new();
    const uint Me = uint.MaxValue;

    public static void Connect(DbConnection c)
    {
        conn = c;
        c.Db.MyCraftJob.OnInsert += (_, j) => Start(Me, j.ProcessId, true);
        c.Db.MyCraftJob.OnDelete += (_, _) => loops.Remove(Me);
        c.Db.CraftWork.OnInsert += (_, w) => { if (!IsMe(w.PlayerId)) Start(w.PlayerId, w.ProcessId, false); };
        c.Db.CraftWork.OnDelete += (_, w) => loops.Remove(w.PlayerId);
        c.Db.ChoppedTree.OnUpdate += (_, was, t) => { if (t.Felled && !was.Felled) Fell(t); };
        c.Db.ChoppedTree.OnInsert += (_, t) => { if (t.Felled && now > SyncSettle) Fell(t); };
    }

    static void Start(uint player, string processId, bool mine)
    {
        if (WorkMotions.For(processId) != WorkMotion.Chop) return;
        loops[player] = new Loop { Player = player, Mine = mine, NextAt = now + ChopContact };
    }

    static void Fell(ChoppedTree t)
    {
        var (x, z) = Trees.Trunk(t.CellX, t.CellZ);
        var where = new Vector3(x, 0f, z);
        Sfx.PlayAnimal(CueDefs.TreeCreak, where, TreeHearing);
        later.Add((now + CrashDelay, CueDefs.TreeCrash, where));
    }

    // Each frame from Atmosphere.
    public static void Tick(double dt)
    {
        now += dt;
        if (conn == null) return;
        foreach (var loop in loops.Values)
        {
            while (now >= loop.NextAt)
            {
                loop.NextAt += ChopPeriod;
                if (now - loop.NextAt + ChopPeriod > 0.5) continue;   // far behind (a stalled frame): skip, don't burst
                if (loop.Mine) Sfx.Play(loop.Def);
                else if (conn.Db.PlayerPos.PlayerId.Find(loop.Player) is { } p) Sfx.PlayBy(loop.Def, new Vector3(p.X, 0f, p.Z));
            }
        }
        for (int i = later.Count - 1; i >= 0; i--)
        {
            if (now < later[i].At) continue;
            Sfx.PlayAnimal(later[i].Def, later[i].Where, TreeHearing);
            later.RemoveAt(i);
        }
    }

    static bool IsMe(uint playerId) =>
        conn?.Identity is { } me && conn.Db.Player.Identity.Find(me) is { } p && p.PlayerId == playerId;
}
