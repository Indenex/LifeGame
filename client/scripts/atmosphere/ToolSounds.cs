using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Tool work heard (audio-14): CueDefs played on Animation's impact frames (Avatar.Impact, anim-7):
// the axe biting the trunk and the stick or spade going into the ground (and the spade's load
// thrown off), for everyone, yours and other players'. Yours plays at full volume; others'
// come from where the tool is, quieter. Also a felled tree (audio-17): the creak and the rush
// of leaves as it tips, then the impact, on Environment's TreeFall events.
static class ToolSounds
{
    const float TreeHearing = 70f;

    public static void Connect(DbConnection c)
    {
        Avatar.Impact -= OnImpact;
        Avatar.Impact += OnImpact;
        TreeFall.Falling -= OnFalling;
        TreeFall.Falling += OnFalling;
        TreeFall.Landed -= OnLanded;
        TreeFall.Landed += OnLanded;
    }

    // What an impact sounds like, or null for the ones other voices already cover.
    static CueDef? SoundOf(WorkMotion motion, bool thrown, ushort item) => motion switch
    {
        WorkMotion.Chop => CueDefs.ChopThunk,
        WorkMotion.Dig => thrown ? CueDefs.DirtThrow : item == (ushort)Item.Spade ? CueDefs.DigSpade : item != 0 ? CueDefs.DigStick : CueDefs.DigHands,
        WorkMotion.Pound => CueDefs.PoundThud,
        _ => null,
    };

    static void OnImpact(Avatar avatar, WorkMotion motion, Vector3 at, bool thrown, ushort item)
    {
        if (SoundOf(motion, thrown, item) is { } def) Sfx.PlayBy(def, at);
        // Digging in clay adds the wet suck of it to whatever the tool does.
        if (motion == WorkMotion.Dig && World.IsClay(World.CellOf(at.X), World.CellOf(at.Z)))
            Sfx.PlayBy(thrown ? CueDefs.ClayThrow : CueDefs.DigClay, at);
    }

    // The trunk starts to tip: the creak at its foot, the crown's rush a little way along the fall.
    static void OnFalling(Vector3 foot, Vector3 direction)
    {
        Sfx.PlayAnimal(CueDefs.TreeCreak, foot, TreeHearing);
        Sfx.PlayAnimal(CueDefs.TreeRush, foot + direction * 2f, TreeHearing);
    }

    static void OnLanded(Vector3 crownImpact, Vector3 direction) =>
        Sfx.PlayAnimal(CueDefs.TreeImpact, crownImpact, TreeHearing);

    // Each frame from Atmosphere (nothing to time now that the tree falls on events).
    public static void Tick(double dt) { }
}
