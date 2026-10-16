using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Pots of broth and stew simmering by fires (ceramics-7), and G to take yours off.
public partial class CeramicsView
{
    readonly Dictionary<ulong, SimmerView> simmers = new();

    void AttachSimmer(DbConnection c)
    {
        c.Db.SimmerPot.OnInsert += (_, p) => UpsertSimmer(p);
        c.Db.SimmerPot.OnUpdate += (_, _, p) => UpsertSimmer(p);
        c.Db.SimmerPot.OnDelete += (_, p) =>
        {
            if (simmers.Remove(p.Id, out var view)) view.QueueFree();
        };
    }

    void UpsertSimmer(SimmerPot p)
    {
        if (!simmers.TryGetValue(p.Id, out var view))
        {
            view = new SimmerView();
            simmers[p.Id] = view;
            world.AddChild(view);
        }
        // Sits in the embers at the fire's edge.
        view.Position = new Vector3(p.X, heightAt(p.X, p.Z) - 0.02f, p.Z);
        view.Show(p.Id, (Item)p.Dish, Seconds(p.PutAt));
    }

    // Your own pot within reach: G takes it off when it's done, or says how long it has left.
    UseOffer? SimmerOffer(Vector3 me3)
    {
        if (conn == null) return null;
        SimmerView? best = null;
        ulong bestId = 0;
        float bestD = World.Reach;
        foreach (var (id, view) in simmers)
        {
            view.ShowLabel = false;
            if (conn.Db.SimmerPot.Id.Find(id) is not { } row || row.PlayerId != MyId) continue;
            float d = new Vector2(view.Position.X - me3.X, view.Position.Z - me3.Z).Length();
            if (d <= bestD) { best = view; bestId = id; bestD = d; }
        }
        if (best == null) return null;
        best.ShowLabel = true;
        var target = new Vector3(best.Position.X, best.Position.Y, best.Position.Z);
        ulong potId = bestId;
        double left = best.SecondsLeft;
        if (left > 0)
            return new UseOffer($"Simmering, {Mathf.CeilToInt((float)left)} s left", bestD,
                () => note("It is still simmering. It won't burn, so there's no hurry."), Target: target);
        return new UseOffer("Take the pot off the fire", bestD, () => conn.Reducers.TakeOffPot(potId), Target: target);
    }
}
