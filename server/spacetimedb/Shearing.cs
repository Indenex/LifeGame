using LifeGame.Shared;
using SpacetimeDB;

// Shearing: a player with something sharp can take the fleece off a calm sheep.
// The wool grows back over a few in-game days.
public static partial class Module
{
    const string ShearProcess = "animal.shear";
    // A fleece takes this long to grow back from bare skin.
    const long FleeceRegrowMicros = 3L * WorldTime.DayLengthSeconds * 1_000_000;
    // Below this a sheep doesn't have enough wool worth taking.
    const byte FleeceToShear = 80;
    // A sheep this nervous won't stand still for the blade.
    const float ShearNervousLimit = 0.15f;

    [SpacetimeDB.Reducer]
    public static void ShearSheep(ReducerContext ctx, ulong animalId)
    {
        var player = RequirePlayer(ctx);
        var animal = ctx.Db.Animal.Id.Find(animalId) ?? throw new Exception("That sheep has wandered off");
        if (!AnimalDefs.Get((byte)animal.Species).Shearable) throw new Exception("You can only shear sheep");
        RequireReach(RequirePos(ctx, player), animal.X, animal.Z);
        var mind = ctx.Db.AnimalMind.AnimalId.Find(animalId) ?? throw new Exception("That sheep has wandered off");

        if (animal.Fleece < FleeceToShear) throw new Exception("Its wool hasn't grown back yet");
        if (animal.State == AnimalState.Run || mind.Nervous > ShearNervousLimit)
            throw new Exception("The sheep is too skittish. Sneak up and let it settle");
        var tool = BestTool(ctx, ItemTags.Cutting) ?? throw new Exception(NoToolMessage(ctx, ItemTags.Cutting, "You need something sharp to shear with"));

        // A better edge gives a cleaner fleece, up to what the shearer's skill allows.
        byte cap = 100;
        QualityCapFor(ctx, player.PlayerId, ShearProcess, ref cap);
        byte quality = Math.Min(tool.Quality, cap);
        AddToInventory(ctx, Me(ctx), Item.Wool, animal.Fleece >= 100 ? 2u : 1u, quality);
        OnProcessPerformed(ctx, player.PlayerId, ShearProcess, quality, false);
        NoteAct(ctx, player.PlayerId, WorkMotion.Shear, (Item)tool.Item);
        WearTool(ctx, tool);

        // A shorn sheep stands looking a bit put out, but it isn't frightened off.
        ctx.Db.Animal.Id.Update(animal with { Fleece = 0, State = AnimalState.Idle });
        ctx.Db.AnimalMind.AnimalId.Update(mind with
        {
            ShornAt = ctx.Timestamp.MicrosecondsSinceUnixEpoch,
            StateUntil = ctx.Timestamp.MicrosecondsSinceUnixEpoch + 3_000_000,
        });
    }

    // Wool comes back in steps of 5 so a regrowing sheep isn't resent every tick.
    static void RegrowFleece(ref Animal body, AnimalMind mind, long now)
    {
        if (body.Fleece >= 100 || mind.ShornAt == 0) return;
        long grown = Math.Clamp((now - mind.ShornAt) * 100 / FleeceRegrowMicros, 0, 100);
        byte stepped = (byte)(grown >= 100 ? 100 : grown / 5 * 5);
        if (stepped > body.Fleece) body.Fleece = stepped;
    }
}
