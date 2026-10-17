using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Tool work heard (audio-14): CueDefs played on Animation's impact frames (Avatar.Impact, anim-7):
// the axe biting the trunk and the stick or spade going into the ground (and the spade's load
// thrown off), for everyone, yours and other players'. Also the creak and crash when a tree is
// felled. Yours plays at full volume; others' come from where the tool is, quieter.
static class ToolSounds
{
    // Crafting's spade (Item.Spade = 18); swap for the enum value once it is on main.
    const ushort SpadeItem = 18;
    const float TreeHearing = 70f;
    const double CrashDelay = 0.95;    // the creak ends in a snap, then the tree is on its way
    const double SyncSettle = 5.0;     // ignore felled rows that arrive in the first seconds (the initial sync)

    static DbConnection? conn;
    static double now;
    static readonly List<(double At, CueDef Def, Vector3 Where)> later = new();

    public static void Connect(DbConnection c)
    {
        conn = c;
        Avatar.Impact -= OnImpact;
        Avatar.Impact += OnImpact;
        c.Db.ChoppedTree.OnUpdate += (_, was, t) => { if (t.Felled && !was.Felled) Fell(t); };
        c.Db.ChoppedTree.OnInsert += (_, t) => { if (t.Felled && now > SyncSettle) Fell(t); };
    }

    // What an impact sounds like, or null for the ones other voices already cover.
    static CueDef? SoundOf(WorkMotion motion, bool thrown, ushort item) => motion switch
    {
        WorkMotion.Chop => CueDefs.ChopThunk,
        WorkMotion.Dig => thrown ? CueDefs.DirtThrow : item == SpadeItem ? CueDefs.DigSpade : item != 0 ? CueDefs.DigStick : CueDefs.DigHands,
        _ => null,
    };

    static void OnImpact(Avatar avatar, WorkMotion motion, Vector3 at, bool thrown, ushort item)
    {
        if (SoundOf(motion, thrown, item) is { } def) Sfx.PlayBy(def, at);
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
        for (int i = later.Count - 1; i >= 0; i--)
        {
            if (now < later[i].At) continue;
            Sfx.PlayAnimal(later[i].Def, later[i].Where, TreeHearing);
            later.RemoveAt(i);
        }
    }
}
