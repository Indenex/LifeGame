using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Sleeping mats: G inside a finished hut or house you live in lays your mat out
// where you stand; G on your own mat rolls it up. You wake on it after dying.
public partial class BuildingSystem
{
    readonly Dictionary<uint, MeshInstance3D> mats = new();

    void BindMats(DbConnection c)
    {
        c.Db.SleepingMat.OnInsert += (_, m) => ShowMat(m);
        c.Db.SleepingMat.OnUpdate += (_, _, m) => ShowMat(m);
        c.Db.SleepingMat.OnDelete += (_, m) =>
        {
            if (mats.Remove(m.PlayerId, out var mesh)) mesh.QueueFree();
        };
        RegisterMatUse(c);
    }

    void ShowMat(SleepingMat m)
    {
        if (!mats.TryGetValue(m.PlayerId, out var mesh))
        {
            mesh = new MeshInstance3D { Mesh = StructureModel.Mat, MaterialOverride = StructureModel.Weave };
            root.AddChild(mesh);
            mats[m.PlayerId] = mesh;
        }
        mesh.Position = new Vector3(m.X, terrain.HeightAt(m.X, m.Z) + 0.01f, m.Z);
        mesh.Rotation = new Vector3(0, m.Yaw, 0);
    }

    // Shows a mat without a server, for the showcase.
    void ShowMat(uint player, float x, float z, float yaw) =>
        ShowMat(new SleepingMat(player, 0, x, z, yaw));

    uint MyPlayerId(DbConnection c) =>
        c.Identity is { } id && c.Db.Player.Identity.Find(id) is { } p ? p.PlayerId : 0;

    void RegisterMatUse(DbConnection c)
    {
        Use.Register(new UseHandler
        {
            Id = "building.mat",
            // Above opening the door, so standing on your mat by the doorway rolls it up.
            Priority = 29,
            Find = at =>
            {
                uint me = MyPlayerId(c);
                if (me != 0 && c.Db.SleepingMat.PlayerId.Find(me) is { } mine)
                {
                    float d = new Vector2(mine.X - at.X, mine.Z - at.Z).Length();
                    if (d < 1.1f)
                        return new UseOffer("Roll up your mat", d, () => c.Reducers.RollUpMat(), Target: new Vector3(mine.X, at.Y, mine.Z));
                }
                if (Carried(c, Item.SleepingMat) == 0) return null;
                var home = rows.Values.FirstOrDefault(s => s.Complete && Structures.HasDoorway((int)s.Kind)
                    && Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, at.X, at.Z, -0.4f));
                if (home == null) return null;
                return new UseOffer("Lay out your sleeping mat", 0f, () => c.Reducers.LayMat(home.Id));
            },
        });
    }
}
