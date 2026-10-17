using System.Collections.Generic;
using Godot;

namespace LifeGame;

// A finished building is drawn as one mesh per material instead of one per stick,
// rib and brick course (building-24, from the old building-10 perf patch): a camp
// of twenty buildings went from about 1071 draw calls to 481, each also drawn again
// into every shadow split. Every finished building of a kind shares the same merged
// mesh. While a building is cut away for the player (see-through, D-168) it goes
// back to its separate pieces, which fade by role, and merges again once solid.
public partial class StructureView
{
    // --building-no-merge keeps the per-piece meshes, for measuring the difference.
    static readonly bool NoMerge = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--building-no-merge") >= 0;
    static readonly Dictionary<int, ArrayMesh?> MergedCache = new();

    MeshInstance3D? merged;
    readonly List<Node3D> hiddenForMerge = new();
    bool complete, mergePending;
    Material? overlay;

    bool Merged => merged != null;

    // Jars and troughs carry their maker's stamp per building, and batch devices
    // swap their pieces as they fill, so those stay piece by piece.
    bool Mergeable()
    {
        if (NoMerge || ghost != null) return false;
        var shape = LifeGame.Shared.Structures.Shape(Kind);
        return !shape.Jar && !shape.Trough
            && shape.Station is not (LifeGame.Shared.StationKind.ClayTub or LifeGame.Shared.StationKind.CrushBlock or LifeGame.Shared.StationKind.TemperTrough);
    }

    // Called from Show once the building stands: straight away, or after the last
    // pieces have settled into place.
    void MergeWhenSettled(bool animate)
    {
        if (Merged || mergePending || !Mergeable()) return;
        if (!animate || !IsInsideTree())
        {
            Merge();
            return;
        }
        mergePending = true;
        GetTree().CreateTimer(0.8).Timeout += () =>
        {
            mergePending = false;
            if (IsInstanceValid(this) && complete) Merge();
        };
    }

    void Merge()
    {
        if (Merged || Fading || !complete) return;
        if (MergedMesh(Kind) is not { } mesh) return;
        merged = new MeshInstance3D { Mesh = mesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.On, MaterialOverlay = overlay };
        AddChild(merged);
        foreach (var (_, holder, _) in pieces)
            if (holder.Visible)
            {
                holder.Visible = false;
                hiddenForMerge.Add(holder);
            }
    }

    // Back to separate pieces (a building being cut away, or one that stops being finished).
    void Unmerge()
    {
        if (!Merged) return;
        merged!.QueueFree();
        merged = null;
        foreach (var holder in hiddenForMerge) holder.Visible = true;
        hiddenForMerge.Clear();
    }

    // The finished pieces, one surface per material.
    static ArrayMesh? MergedMesh(int kind)
    {
        if (MergedCache.TryGetValue(kind, out var cached)) return cached;
        var groups = new Dictionary<Material, SurfaceTool>();
        var order = new List<Material>();
        foreach (var piece in StructureModel.Pieces(kind))
        {
            if (piece.Stage < 0) continue;
            if (!groups.TryGetValue(piece.Material, out var st))
            {
                st = new SurfaceTool();
                groups[piece.Material] = st;
                order.Add(piece.Material);
            }
            for (int s = 0; s < piece.Mesh.GetSurfaceCount(); s++)
                st.AppendFrom(piece.Mesh, s, Transform3D.Identity);
        }
        ArrayMesh? mesh = null;
        foreach (var material in order)
        {
            mesh ??= new ArrayMesh();
            groups[material].Commit(mesh);
            mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, material);
        }
        return MergedCache[kind] = mesh;
    }
}
