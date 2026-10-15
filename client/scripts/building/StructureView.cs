using System.Collections.Generic;
using Godot;

namespace LifeGame;

// One structure in the world, showing as many of its pieces as the materials
// added so far allow. New pieces settle into place instead of popping in.
public partial class StructureView : Node3D
{
    public readonly int Kind;
    readonly List<(StructureModel.Piece piece, Node3D holder, MeshInstance3D mesh)> pieces = new();

    public StructureView(int kind, Material? ghost = null)
    {
        Kind = kind;
        foreach (var piece in StructureModel.Pieces(kind))
        {
            var holder = new Node3D { Position = piece.Anchor, Visible = false };
            var mesh = new MeshInstance3D
            {
                Mesh = piece.Mesh,
                MaterialOverride = ghost ?? piece.Material,
                Position = -piece.Anchor,
                CastShadow = ghost == null ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off,
            };
            holder.AddChild(mesh);
            AddChild(holder);
            pieces.Add((piece, holder, mesh));
        }
    }

    // stage: the stage being built; fraction: how much of it is in, 0 to 1.
    // Returns how many pieces newly appeared, so callers can add a puff of dust.
    public int Show(int stage, float fraction, bool complete, bool animate)
    {
        var perStage = new Dictionary<int, int>();
        foreach (var (piece, _, _) in pieces)
            perStage[piece.Stage] = perStage.GetValueOrDefault(piece.Stage) + 1;

        int appeared = 0;
        var seen = new Dictionary<int, int>();
        foreach (var (piece, holder, _) in pieces)
        {
            int index = seen[piece.Stage] = seen.GetValueOrDefault(piece.Stage) + 1;
            bool show = piece.Stage switch
            {
                -1 => !complete,
                _ when complete => true,
                _ when piece.Stage < stage => true,
                _ when piece.Stage == stage => index <= Mathf.FloorToInt(fraction * perStage[piece.Stage] + 0.001f),
                _ => false,
            };
            if (show == holder.Visible) continue;
            if (show) appeared++;
            if (animate && IsInsideTree()) Animate(holder, piece.Anchor, show);
            else
            {
                holder.Visible = show;
                holder.Position = piece.Anchor;
                holder.Scale = Vector3.One;
            }
        }
        return appeared;
    }

    public void SetGhost(Material material)
    {
        foreach (var (_, _, mesh) in pieces) mesh.MaterialOverride = material;
    }

    void Animate(Node3D holder, Vector3 anchor, bool show)
    {
        var tween = CreateTween();
        if (show)
        {
            holder.Visible = true;
            holder.Scale = new Vector3(0.85f, 0.3f, 0.85f);
            holder.Position = anchor + new Vector3(0, 0.45f, 0);
            tween.SetParallel();
            tween.TweenProperty(holder, "position", anchor, 0.5).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(holder, "scale", Vector3.One, 0.55).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
        else
        {
            tween.TweenProperty(holder, "scale", new Vector3(1f, 0.01f, 1f), 0.4).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
            tween.TweenCallback(Callable.From(() => holder.Visible = false));
        }
    }
}
