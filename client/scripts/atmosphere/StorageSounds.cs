using Godot;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LifeGame;

// Baskets and storage jars heard (audio-21): a rustle or clink when something is put in or taken
// out, played where the store stands (Sfx.PlayAnimal: full beside it, fading out to 12 m). Reads
// the public basket_item rows, so everyone nearby hears it. Rows that arrive with a subscription
// (walking into a chunk) are not events and stay silent.
[AudioSounds]
static class StorageSounds
{
    const float Hearing = 12f;
    const double Cooldown = 0.6;   // seconds before the same store sounds again
    static readonly System.Collections.Generic.Dictionary<ulong, double> lastHeard = new();

    public static void Connect(DbConnection c)
    {
        c.Db.BasketItem.OnInsert += (ctx, row) => Heard(c, ctx, row.StructureId, put: true);
        c.Db.BasketItem.OnUpdate += (ctx, was, row) =>
        {
            if (row.Count != was.Count) Heard(c, ctx, row.StructureId, put: row.Count > was.Count);
        };
        c.Db.BasketItem.OnDelete += (ctx, row) => Heard(c, ctx, row.StructureId, put: false);
    }

    static void Heard(DbConnection c, EventContext ctx, ulong structureId, bool put)
    {
        if (ctx.Event is not Event<Reducer>.Reducer) return;
        if (c.Db.Structure.Id.Find(structureId) is not { } s) return;
        // Putting several things in at once is one lid, not a clunk each.
        double now = Time.GetTicksMsec() / 1000.0;
        if (lastHeard.TryGetValue(structureId, out double was) && now - was < Cooldown) return;
        lastHeard[structureId] = now;
        var shape = Structures.Shape((int)s.Kind);
        var def = shape.Jar ? (put ? CueDefs.JarPut : CueDefs.JarTake)
            : shape.Model == "Chest" ? (put ? CueDefs.ChestPut : CueDefs.ChestTake)
            : put ? CueDefs.BasketPut : CueDefs.BasketTake;
        Sfx.PlayAnimal(def, new Vector3(s.X, 0.5f, s.Z), Hearing);
    }
}
