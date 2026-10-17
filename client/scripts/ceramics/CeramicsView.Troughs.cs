using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Clay troughs (ceramics-13): the water in each set-down trough, and the Use key to
// pour a pot of water in, scoop a pot full, or take a drink.
public partial class CeramicsView
{
    readonly Dictionary<ulong, TroughView> troughs = new();

    void AttachTroughs(DbConnection c)
    {
        c.Db.Structure.OnInsert += (_, s) => TroughSite(s);
        c.Db.Structure.OnUpdate += (_, _, s) => TroughSite(s);
        c.Db.Structure.OnDelete += (_, s) =>
        {
            if (troughs.Remove(s.Id, out var view)) view.QueueFree();
        };
        c.Db.TroughWater.OnInsert += (_, t) => TroughLevel(t.StructureId, (int)t.Litres);
        c.Db.TroughWater.OnUpdate += (_, _, t) => TroughLevel(t.StructureId, (int)t.Litres);
        c.Db.TroughWater.OnDelete += (_, t) => TroughLevel(t.StructureId, 0);
    }

    void TroughSite(Structure s)
    {
        if (!s.Complete || !Structures.Shape((int)s.Kind).Trough) return;
        if (!troughs.TryGetValue(s.Id, out var view))
        {
            view = new TroughView();
            troughs[s.Id] = view;
            world.AddChild(view);
        }
        view.Position = new Vector3(s.X, heightAt(s.X, s.Z), s.Z);
        TroughLevel(s.Id, conn?.Db.TroughWater.StructureId.Find(s.Id) is { } t ? (int)t.Litres : 0);
    }

    void TroughLevel(ulong structureId, int litres)
    {
        if (!troughs.TryGetValue(structureId, out var view)) return;
        byte quality = conn?.Db.StructureProgress.StructureId.Find(structureId)?.Quality ?? Items.RawQuality;
        view.Show(litres, Structures.TroughLitres(quality));
    }

    // The nearest trough in reach: pour a carried pot of water in, scoop an empty pot
    // full, or drink.
    UseOffer? TroughOffer(Vector3 me3)
    {
        if (conn == null) return null;
        TroughView? best = null;
        ulong bestId = 0;
        float bestD = World.Reach;
        foreach (var (id, view) in troughs)
        {
            view.ShowLabel = false;
            float d = new Vector2(view.Position.X - me3.X, view.Position.Z - me3.Z).Length();
            if (d <= bestD) { best = view; bestId = id; bestD = d; }
        }
        if (best == null) return null;
        best.ShowLabel = true;
        ulong id2 = bestId;
        var target = best.Position;
        int room = best.Capacity - best.Litres;
        if (Carrying(i => ClayRules.PotLitres(i) > 0 && ClayRules.PotLitres(i) <= room))
            return new UseOffer("Pour water into the trough", bestD, () => conn.Reducers.PourIntoTrough(id2), Target: target);
        if (best.Litres > 0 && Carrying(i => ClayRules.FilledItem(i) != null))
            return new UseOffer("Fill a pot from the trough", bestD, () => conn.Reducers.ScoopFromTrough(id2), Target: target);
        if (best.Litres > 0)
            return new UseOffer("Drink from the trough", bestD, () => conn.Reducers.DrinkFromTrough(id2), Target: target);
        return null;
    }
}
