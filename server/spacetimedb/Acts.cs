using LifeGame.Shared;
using SpacetimeDB;

// Quick body actions (Animation lane, anim-4): drinking at water, eating, picking
// berries, picking something up. The reducer that does the thing notes it here once it
// has succeeded, and every client near enough plays the motion on that player, their own
// included, so a refused action never animates.
public static partial class Module
{
    [SpacetimeDB.Table(Accessor = "PlayerAct", Public = true, Event = true)]
    public partial struct PlayerAct
    {
        public uint PlayerId;
        // A shared WorkMotion (Drink, Eat, Pluck, PickUp).
        public byte Motion;
        // What the hand holds for it (the food eaten); 0 for nothing.
        public ushort Item;
    }

    static void NoteAct(ReducerContext ctx, uint playerId, WorkMotion motion, Item item = 0) =>
        ctx.Db.PlayerAct.Insert(new PlayerAct { PlayerId = playerId, Motion = (byte)motion, Item = (ushort)item });
}
