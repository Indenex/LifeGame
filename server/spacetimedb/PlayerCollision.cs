using LifeGame.Shared;
using SpacetimeDB;

// Walls bind a hacked client too (bugfix-27, bugfix-24 item 2, D-182). Move asks whether
// a step goes through something solid and keeps the player where they were if it does.
// It reuses the rules animals already follow (StructureBlocksStep) and adds what
// only players do: hop low fences (a jump row says when), not claim fences they don't
// belong to, and bump round things. The client slides
// along these; the server just refuses the step, and is slightly more forgiving (a
// smaller body) so an honest client is never pushed back.
public static partial class Module
{
    const float PlayerBody = 0.25f;

    static float PlayerAirHeight(ReducerContext ctx, uint playerId) =>
        ctx.Db.PlayerJump.PlayerId.Find(playerId) is { } jump
            ? Jumping.AirHeightWithGrace(SecondsBetween(jump.At, ctx.Timestamp))
            : 0f;

    static bool PlayerStepBlocked(ReducerContext ctx, uint playerId, float fromX, float fromZ, float toX, float toZ)
    {
        float air = PlayerAirHeight(ctx, playerId);
        foreach (var s in StructuresNear(ctx, toX, toZ))
        {
            int kind = (int)s.Kind;
            var shape = Structures.Shape(kind);
            if (shape.Fence)
            {
                if (!s.Complete) continue;
                if (shape.Gate && !DoorClosed(ctx, s.Id)) continue;
                if (!Structures.StepCrossesFence(s.X, s.Z, s.Yaw, fromX, fromZ, toX, toZ, PlayerBody)) continue;
                // A low fence can be hopped, but not one that stands in a claim you don't belong to.
                if (Jumping.Clears(Jumping.FenceTop(shape.Gate), air)
                    && (ClaimAt(ctx, s.X, s.Z) is not { } claim || HasClaimRights(ctx, claim, Me(ctx))))
                    continue;
                return true;
            }
            if (shape.Solid && shape.Round && (s.Complete || s.Stage >= 1))
            {
                float keep = shape.Radius + PlayerBody;
                float to = Dist(toX, toZ, s.X, s.Z);
                if (to < keep && to < Dist(fromX, fromZ, s.X, s.Z)) return true;
                continue;
            }
            bool walls = Structures.Shape(kind).Hut ? s.Complete || s.Stage >= 1 : Structures.IsRoom(kind) && (s.Complete || s.Stage >= 2);
            if (walls && Structures.StepCrossesWalls(kind, s.X, s.Z, s.Yaw, DoorClosed(ctx, s.Id), fromX, fromZ, toX, toZ)) return true;
            // Lean-to sides (Structures.StepCrossesLeanTo) are not enforced yet: the open-fronted frame
            // wedges the straight-walking check bots (bugfix-27 follow-up).
        }

        // Trunks, boulders and landmarks are not enforced here yet: the check bots in tools/ walk
        // straight lines and would wedge on them (bugfix-27 follow-up: path around, then add trunks).
        return false;
    }
}
