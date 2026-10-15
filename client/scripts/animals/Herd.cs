using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Every animal the server sends us. The server decides where animals go a couple
// of times a second; each animal node glides between those positions. Also
// offers shearing on the Use key (G) next to a sheep with a full fleece, and asks
// for the sneak hint the first time a flock bolts near the player.
public partial class Herd : Node3D
{
    readonly Dictionary<ulong, Sheep> animals = new();
    Terrain terrain = null!;
    DbConnection? conn;
    Func<Vector3> player = () => Vector3.Zero;

    public void Bind(DbConnection connection, Terrain ground, Func<Vector3> playerPosition)
    {
        conn = connection;
        terrain = ground;
        player = playerPosition;
        conn.Db.Animal.OnInsert += (_, a) => Upsert(a, true);
        conn.Db.Animal.OnUpdate += (_, _, a) => Upsert(a, false);
        conn.Db.Animal.OnDelete += (_, a) =>
        {
            if (animals.Remove(a.Id, out var node)) node.QueueFree();
        };
        conn.Db.Animal.OnUpdate += (_, old, a) =>
        {
            if (a.State == AnimalState.Run && old.State != AnimalState.Run && DistanceTo(a, player()) < BoltHintRange)
                Hints.Request("sneak");
        };
        Use.Register(new UseHandler { Id = "animals.shear", Priority = 15, Find = OfferShear });
    }

    const float BoltHintRange = 10f;

    static float DistanceTo(Animal a, Vector3 at) => new Vector2(a.X - at.X, a.Z - at.Z).Length();

    // Only offered when shearing can work: a sheep in reach with its wool grown back.
    UseOffer? OfferShear(Vector3 at)
    {
        if (conn == null) return null;
        Animal? nearest = null;
        float best = World.Reach;
        foreach (var a in conn.Db.Animal.Iter())
        {
            float d = DistanceTo(a, at);
            if (d <= best && a.Species == AnimalSpecies.Sheep && a.Fleece >= 80)
                (nearest, best) = (a, d);
        }
        if (nearest == null) return null;
        ulong id = nearest.Id;
        return new UseOffer("Shear", best, () => conn.Reducers.ShearSheep(id));
    }

    void Upsert(Animal a, bool inserted)
    {
        if (!animals.TryGetValue(a.Id, out var sheep))
        {
            sheep = new Sheep(a.Id, terrain);
            animals[a.Id] = sheep;
            AddChild(sheep);
            inserted = true;
        }
        sheep.Apply(a, inserted);
    }
}
