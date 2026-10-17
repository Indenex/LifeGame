using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Set snares (animals-34, shared/Traps.cs): what a snare has caught lies beside it until its owner, or the
// owner or a member of the claim it stands in, takes it. The server decides every catch and who may take it
// (Traps.cs); this only draws the animal and offers the prompt where the server would say yes.
public partial class Snares : Node3D
{
    readonly Dictionary<ulong, Sheep> caught = new();
    Terrain terrain = null!;
    DbConnection? conn;
    // Set by Herd from the Claims layer, as for the hide racks.
    public Func<float, float, bool> ClaimGoodsOpenAt = (_, _) => false;
    public Func<float, float, bool> ClaimStandingAt = (_, _) => false;

    public void Bind(DbConnection connection, Terrain ground)
    {
        conn = connection;
        terrain = ground;
        conn.Db.SnareState.OnInsert += (_, s) => Show(s);
        conn.Db.SnareState.OnUpdate += (_, _, s) => Show(s);
        conn.Db.SnareState.OnDelete += (_, s) => Clear(s.ItemId);
        Use.Register(new UseHandler { Id = "animals.snare", Priority = 15, Find = OfferCatch });
    }

    void Show(SnareState s)
    {
        if (s.Caught == 0)
        {
            Clear(s.ItemId);
            return;
        }
        if (caught.ContainsKey(s.ItemId)) return;
        // The caught animal lies a step from the noose; the id only seeds its coat.
        var animal = new Sheep(s.ItemId ^ (1UL << 40), terrain, (AnimalSpecies)(s.Caught - 1));
        AddChild(animal);
        animal.LieDown(s.X + 0.5f, s.Z + 0.2f, 1.2f, 100, 100, false);
        caught[s.ItemId] = animal;
    }

    void Clear(ulong itemId)
    {
        if (caught.Remove(itemId, out var animal)) animal.FadeAway();
    }

    UseOffer? OfferCatch(Vector3 at)
    {
        if (conn == null) return null;
        SnareState? nearest = null;
        float best = World.Reach;
        var mine = Me.Key(conn);
        foreach (var s in conn.Db.SnareState.Iter())
        {
            if (s.Caught == 0) continue;
            float d = new Vector2(s.X - at.X, s.Z - at.Z).Length();
            bool mayTake = (mine != null && s.Owner == mine) || (ClaimStandingAt(s.X, s.Z) && ClaimGoodsOpenAt(s.X, s.Z));
            if (d <= best && mayTake) (nearest, best) = (s, d);
        }
        if (nearest == null) return null;
        ulong id = nearest.ItemId;
        string text = conn.Db.WorldItem.Id.Find(id) is { } item && Traps.Find(item.Item) is { } def ? def.TakeText : "Take the catch";
        return new UseOffer(text, best, () => conn.Reducers.CollectSnare(id));
    }
}
