using LifeGame.Shared;
using SpacetimeDB;

// Fences and gates (building-8). A fence is a 2 m run of posts and rails; a gate is
// a fence whose woven hurdle opens and bars like a hut door, through the same
// StructureDoor row, so ToggleDoor and ToggleLock work on it unchanged. Animals ask
// StructureBlocksStep before each step, so sheep stop at fences, shut gates and
// the walls of huts and houses.
public static partial class Module
{
    // A finished gate hangs its hurdle at once, shut, as if whoever laid it out had hung it.
    static void FinishGate(ReducerContext ctx, Structure s)
    {
        if (!Structures.IsGate((int)s.Kind) || ctx.Db.StructureDoor.StructureId.Find(s.Id) is not null) return;
        ctx.Db.StructureDoor.Insert(new StructureDoor { StructureId = s.Id, Closed = true, Locked = false, HungBy = s.Owner });
    }

    // How close an animal's middle may come to a fence line.
    const float FenceBody = 0.3f;

    // True when a step from one spot to another would go through a finished fence,
    // a shut gate, or a hut's or house's walls (their open doorway lets it through).
    public static bool StructureBlocksStep(ReducerContext ctx, float fromX, float fromZ, float toX, float toZ)
    {
        foreach (var s in StructuresNear(ctx, toX, toZ))
        {
            int kind = (int)s.Kind;
            if (Structures.IsFence(kind))
            {
                if (!s.Complete) continue;
                if (Structures.IsGate(kind) && !DoorClosed(ctx, s.Id)) continue;
                if (Structures.StepCrossesFence(s.X, s.Z, s.Yaw, fromX, fromZ, toX, toZ, FenceBody)) return true;
                continue;
            }
            // Walls stand once a hut's wattle or a house's brick walls are up, as the client draws them.
            bool walls = s.Kind switch
            {
                StructureKind.ClayHut => s.Complete || s.Stage >= 1,
                StructureKind.BrickHouse => s.Complete || s.Stage >= 2,
                _ => false,
            };
            if (walls && Structures.StepCrossesWalls(kind, s.X, s.Z, s.Yaw, DoorClosed(ctx, s.Id), fromX, fromZ, toX, toZ))
                return true;
        }
        return false;
    }
}
