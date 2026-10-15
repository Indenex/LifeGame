using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Every animal the server sends us. The server decides where animals go a couple
// of times a second; each animal node glides between those positions. Also
// handles shearing: the Use key (G) next to a sheep with a full fleece.
public partial class Herd : Node3D
{
    readonly Dictionary<ulong, Sheep> animals = new();
    Terrain terrain = null!;
    DbConnection? conn;
    Func<Vector3> player = () => Vector3.Zero;
    Action<string> note = _ => { };

    public override void _Ready()
    {
        // G is the shared Use key (D-025). Until the Use dispatcher lands, shearing
        // listens for it directly and only acts when a sheep is within reach.
        if (!InputMap.HasAction("shear"))
        {
            InputMap.AddAction("shear");
            InputMap.ActionAddEvent("shear", new InputEventKey { PhysicalKeycode = Key.G });
        }
    }

    public void Bind(DbConnection connection, Terrain ground, Func<Vector3> playerPosition, Action<string> showNote)
    {
        conn = connection;
        terrain = ground;
        player = playerPosition;
        note = showNote;
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

    public override void _UnhandledInput(InputEvent e)
    {
        if (conn == null || !e.IsActionPressed("shear")) return;
        var me = player();
        Animal? nearest = null;
        float best = World.Reach;
        foreach (var a in conn.Db.Animal.Iter())
        {
            float d = new Vector2(a.X - me.X, a.Z - me.Z).Length();
            if (d <= best)
                (nearest, best) = (a, d);
        }
        if (nearest == null) return;
        if (nearest.Fleece < 80)
        {
            note("Its wool hasn't grown back yet");
            return;
        }
        conn.Reducers.ShearSheep(nearest.Id);
    }
}
