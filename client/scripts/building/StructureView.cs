using System.Collections.Generic;
using Godot;

namespace LifeGame;

// One structure in the world. While it is being built, the whole finished
// building stands as a see-through blueprint, and each piece turns solid as the
// materials for it arrive, settling into place instead of popping in.
public partial class StructureView : Node3D
{
    public readonly int Kind;
    readonly List<(StructureModel.Piece piece, Node3D holder, MeshInstance3D mesh)> pieces = new();
    readonly HashSet<int> solid = new();
    readonly Material? ghost;

    // Pale and cool, so it reads as a plan rather than something built.
    static readonly StandardMaterial3D Blueprint = new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        CullMode = BaseMaterial3D.CullModeEnum.Back,
        AlbedoColor = new Color(0.82f, 0.92f, 1f, 0.13f),
    };

    // With a ghost material every piece is drawn in it (the placement preview).
    public StructureView(int kind, Material? ghost = null)
    {
        Kind = kind;
        this.ghost = ghost;
        foreach (var piece in StructureModel.Pieces(kind))
        {
            var holder = new Node3D { Position = piece.Anchor, Visible = false };
            var mesh = new MeshInstance3D
            {
                Mesh = piece.Mesh,
                MaterialOverride = ghost ?? Blueprint,
                Position = -piece.Anchor,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            holder.AddChild(mesh);
            AddChild(holder);
            pieces.Add((piece, holder, mesh));
        }
    }

    // stage: the stage being built; fraction: how much of it is in, 0 to 1.
    // Returns how many pieces newly turned solid, so callers can add a puff of dust.
    public int Show(int stage, float fraction, bool complete, bool animate)
    {
        var perStage = new Dictionary<int, int>();
        foreach (var (piece, _, _) in pieces)
            perStage[piece.Stage] = perStage.GetValueOrDefault(piece.Stage) + 1;

        int appeared = 0;
        var seen = new Dictionary<int, int>();
        for (int i = 0; i < pieces.Count; i++)
        {
            var (piece, holder, mesh) = pieces[i];
            int index = seen[piece.Stage] = seen.GetValueOrDefault(piece.Stage) + 1;
            bool built = piece.Stage switch
            {
                -1 => !complete,
                _ when complete => true,
                _ when piece.Stage < stage => true,
                _ when piece.Stage == stage => index <= Mathf.FloorToInt(fraction * perStage[piece.Stage] + 0.001f),
                _ => false,
            };

            if (ghost != null)
            {
                holder.Visible = built;
                continue;
            }

            // Unbuilt pieces of an unfinished building stay as blueprint; the site pegs go once it stands.
            bool visible = built || (!complete && piece.Stage >= 0);
            if (built == solid.Contains(i) && visible == holder.Visible) continue;

            if (built && !solid.Contains(i))
            {
                solid.Add(i);
                appeared++;
                mesh.MaterialOverride = piece.Material;
                mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
                holder.Visible = true;
                if (animate && IsInsideTree()) Settle(holder, piece.Anchor);
            }
            else if (!built && solid.Remove(i))
            {
                mesh.MaterialOverride = Blueprint;
                mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            }

            if (!visible && holder.Visible)
            {
                if (animate && IsInsideTree()) Vanish(holder);
                else holder.Visible = false;
            }
            else if (visible) holder.Visible = true;
        }
        return appeared;
    }

    void Settle(Node3D holder, Vector3 anchor)
    {
        var tween = CreateTween();
        holder.Scale = new Vector3(0.85f, 0.3f, 0.85f);
        holder.Position = anchor + new Vector3(0, 0.45f, 0);
        tween.SetParallel();
        tween.TweenProperty(holder, "position", anchor, 0.5).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(holder, "scale", Vector3.One, 0.55).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    void Vanish(Node3D holder)
    {
        var tween = CreateTween();
        tween.TweenProperty(holder, "scale", new Vector3(1f, 0.01f, 1f), 0.4).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(() =>
        {
            holder.Visible = false;
            holder.Scale = Vector3.One;
        }));
    }
}
