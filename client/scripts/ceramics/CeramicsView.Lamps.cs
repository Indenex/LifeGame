using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Clay oil lamps (ceramics-9): drawing them, and G to set one down, feed it tallow,
// or pick it up once it's out.
public partial class CeramicsView
{
    readonly Dictionary<ulong, LampView> lamps = new();

    void AttachLamps(DbConnection c)
    {
        c.Db.Lamp.OnInsert += (_, l) => UpsertLamp(l);
        c.Db.Lamp.OnUpdate += (_, _, l) => UpsertLamp(l);
        c.Db.Lamp.OnDelete += (_, l) =>
        {
            if (lamps.Remove(l.Id, out var view)) view.QueueFree();
        };
    }

    void UpsertLamp(Lamp l)
    {
        if (!lamps.TryGetValue(l.Id, out var view))
        {
            view = new LampView();
            lamps[l.Id] = view;
            world.AddChild(view);
        }
        view.Position = new Vector3(l.X, heightAt(l.X, l.Z), l.Z);
        view.Show(l.Id, Seconds(l.BurnsUntil));
    }

    // Carrying a lamp and tallow, near a flame: set one down a step ahead.
    UseOffer? SetLampOffer(Vector3 me3)
    {
        if (conn == null || !Carrying(i => i == Item.ClayLamp) || !Carrying(i => i == Item.Tallow)) return null;
        var forward = new Vector3(Mathf.Sin(me.Rotation.Y), 0, Mathf.Cos(me.Rotation.Y));
        var spot = me3 + forward * 1.0f;
        if (!FlameNear(new Vector2(spot.X, spot.Z))) return null;
        float x = spot.X, z = spot.Z;
        return new UseOffer("Set down a lamp", 1.0f, () => conn.Reducers.SetLamp(x, z));
    }

    // The nearest lamp within reach: feed it, or pick it up when it's out.
    UseOffer? LampOffer(Vector3 me3)
    {
        if (conn == null) return null;
        LampView? best = null;
        ulong bestId = 0;
        float bestD = World.Reach;
        foreach (var (id, view) in lamps)
        {
            view.ShowLabel = false;
            float d = new Vector2(view.Position.X - me3.X, view.Position.Z - me3.Z).Length();
            if (d <= bestD) { best = view; bestId = id; bestD = d; }
        }
        if (best == null) return null;
        best.ShowLabel = true;
        ulong lampId = bestId;
        var target = best.Position;
        bool tallow = Carrying(i => i == Item.Tallow);
        if (tallow && best.SecondsLeft + ClayRules.LampSecondsPerTallow <= ClayRules.LampSecondsPerTallow * ClayRules.MaxLampTallow
            && (best.Lit || FlameNear(new Vector2(target.X, target.Z))))
            return new UseOffer(best.Lit ? "Add tallow to the lamp" : "Light the lamp", bestD, () => conn.Reducers.TendLamp(lampId), Target: target);
        if (!best.Lit)
            return new UseOffer("Pick up the lamp", bestD, () => conn.Reducers.TendLamp(lampId), Target: target);
        return null;
    }

    // A burning fire or a lit lamp near enough to light a lamp from (as the server's FlameNear).
    bool FlameNear(Vector2 at)
    {
        if (BurningFireNear(at, ClayRules.LightFromReach) != null) return true;
        foreach (var view in lamps.Values)
            if (view.Lit && new Vector2(view.Position.X, view.Position.Z).DistanceTo(at) <= ClayRules.LightFromReach) return true;
        return false;
    }
}
