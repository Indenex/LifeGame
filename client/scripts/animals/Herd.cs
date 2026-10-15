using System.Collections.Generic;
using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// Every animal the server sends us. The server decides where animals go a couple
// of times a second; each animal node glides between those positions.
public partial class Herd : Node3D
{
    readonly Dictionary<ulong, Sheep> animals = new();
    Terrain terrain = null!;

    public void Bind(DbConnection conn, Terrain ground)
    {
        terrain = ground;
        conn.Db.Animal.OnInsert += (_, a) => Upsert(a, true);
        conn.Db.Animal.OnUpdate += (_, _, a) => Upsert(a, false);
        conn.Db.Animal.OnDelete += (_, a) =>
        {
            if (animals.Remove(a.Id, out var node)) node.QueueFree();
        };
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
