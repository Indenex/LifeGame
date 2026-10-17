using Godot;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LifeGame;

// Baskets and storage jars heard (audio-21): a rustle or clink when something is put in or taken
// out, played where the store stands (Sfx.PlayAnimal: full beside it, fading out to 12 m). Reads
// the public basket_item rows, so everyone nearby hears it. Rows that arrive with a subscription
// (walking into a chunk) are not events and stay silent.
static class StorageSounds
{
    const float Hearing = 12f;

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
        bool jar = Structures.Shape((int)s.Kind).Jar;
        var def = jar ? (put ? CueDefs.JarPut : CueDefs.JarTake) : (put ? CueDefs.BasketPut : CueDefs.BasketTake);
        Sfx.PlayAnimal(def, new Vector3(s.X, 0.5f, s.Z), Hearing);
    }
}
