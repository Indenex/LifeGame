using System;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// claims-12: lay fences along a claim's border. House building calls SnapFenceHook first
// in SnapFence; a fence ghost near the border of a claim you belong to snaps onto the
// circle, end to end. Social.SnapToClaimBorder's yaw is the Structures.ToWorld convention
// (the fence's local x runs (cos yaw, -sin yaw)), the same one the ghost uses.
public partial class BuildingSystem
{
    // The centre of the claim you belong to whose border is near this point, if any. Main wires it to Social.
    public Func<float, float, (float x, float z)?> ClaimBorderNear = (_, _) => null;

    partial void SnapFenceHook(Vector3 target, ref Vector3 snapped, ref float yaw, ref bool done)
    {
        if (ClaimBorderNear(target.X, target.Z) is not { } c) return;
        var (x, z, y) = Social.SnapToClaimBorder(c.x, c.z, target.X, target.Z);
        snapped = new Vector3(x, terrain.HeightAt(x, z), z);
        yaw = y;
        done = true;
    }
}
