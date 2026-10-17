using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Batch devices on the Use key (crafting-23, D-195): stand at a settling tub, crushing
// block or tempering trough and the key loads it, collects from it, or, for a process
// that has a minigame by hand, works it for better and more. Processes with no minigame
// are plain queues (D-209). A fuller panel (choose the process and the count) is UX's.
public static class DeviceUse
{
    public static void Register(DbConnection conn, CraftingUi crafting)
    {
        Use.Register(new UseHandler
        {
            Id = "crafting.device",
            Priority = 22,
            Find = at =>
            {
                Structure? best = null;
                float bestD = float.MaxValue;
                foreach (var s in conn.Db.Structure.Iter())
                {
                    if (!s.Complete || !Devices.IsDevice(Structures.Shape((int)s.Kind).Station)) continue;
                    float d = new Vector2(s.X - at.X, s.Z - at.Z).Length();
                    if (d - Radius(s) > Structures.WorkReach || d >= bestD) continue;
                    best = s;
                    bestD = d;
                }
                if (best == null) return null;
                var device = best;
                var kind = Structures.Shape((int)device.Kind).Station;
                string name = StationInfo.Name(kind);
                var job = conn.Db.DeviceStatus.StructureId.Find(device.Id);

                if (conn.Db.DeviceOutput.Iter().Any(o => o.StructureId == device.Id))
                    return new UseOffer($"Collect from the {name}", bestD, () => conn.Reducers.CollectDevice(device.Id));
                if (job != null)
                {
                    var process = Processes.Find(job.ProcessId)!;
                    if (job.Attended || Devices.Plain(process)) return null;
                    return new UseOffer($"Work the {name} (better and more)", bestD,
                        () => crafting.PlayDeviceGame(process, Devices.MinigameOf(kind), score => conn.Reducers.WorkDevice(device.Id, score)));
                }
                // Idle: offer a full load of the first thing it can make from what you hold.
                foreach (var id in Devices.ProcessesOf(kind))
                {
                    var def = Processes.Find(id)!;
                    uint units = Devices.BatchCap;
                    foreach (var input in def.Consumes)
                    {
                        uint have = conn.Db.MyInventory.Iter().Where(r => r.Item == (ushort)input.Item).Aggregate(0u, (n, r) => n + r.Count);
                        units = System.Math.Min(units, have / input.Count);
                    }
                    if (units == 0) continue;
                    return new UseOffer($"Load the {name}: {units} x {def.Name.ToLowerInvariant()}", bestD,
                        () => conn.Reducers.LoadDevice(device.Id, id, units));
                }
                return null;
            },
        });
    }

    static float Radius(Structure s)
    {
        var shape = Structures.Shape((int)s.Kind);
        return shape.Round ? shape.Radius : Mathf.Max(shape.HalfWidth, shape.HalfDepth);
    }
}
