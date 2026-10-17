using System;
using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// --building-camp: a busy finished camp, for measuring what buildings cost to draw
// (building-24): ten lean-tos, six clay huts, four brick houses and a fenced yard
// with a gate. Add --render-stats=1 to print draw calls, and --building-no-merge to
// see them drawn piece by piece as before.
public partial class BuildingSystem
{
    void ShowcaseCamp(Action<StructureKind, float, float, int, float, float> put)
    {
        void Done(StructureKind kind, float forward, float right, float turn) => put(kind, forward, right, 9, 1f, turn);
        foreach (float right in new[] { -10.5f, -7f, -3.5f, 3.5f, 7f, 10.5f })
            Done(StructureKind.LeanTo, 5f, right, (right * 0.37f) % 0.6f);
        foreach (float right in new[] { -12f, -8f, 8f, 12f })
            Done(StructureKind.LeanTo, 9.5f, right, right > 0 ? 0.4f : -0.4f);
        foreach (float right in new[] { -9f, -4.5f, 4.5f, 9f })
            Done(StructureKind.ClayHut, 14f, right, right * 0.11f);
        Done(StructureKind.ClayHut, 9.5f, -2.6f, 0.3f);
        Done(StructureKind.ClayHut, 9.5f, 2.6f, -0.3f);
        foreach (float right in new[] { -10f, -3.5f, 3.5f, 10f })
            Done(StructureKind.BrickHouse, 20f, right, right * -0.02f);
        // A yard behind the houses: a back fence with a gate in it, and two sides.
        foreach (float right in new[] { -6f, -4f, -2f, 2f, 4f, 6f })
            Done(StructureKind.Fence, 25.5f, right, 0f);
        Done(StructureKind.Gate, 25.5f, 0f, 0f);
        foreach (float side in new[] { -7f, 7f })
            for (int i = 0; i < 3; i++)
                Done(StructureKind.Fence, 24.5f - i * 2f, side, Mathf.Pi / 2);
        cameraRig.Zoom(14f);
    }
}
