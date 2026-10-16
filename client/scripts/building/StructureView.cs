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

    // Hides the see-through plan of what isn't built yet (showcase shots only).
    public void HideUnbuilt()
    {
        for (int i = 0; i < pieces.Count; i++)
            if (!solid.Contains(i)) pieces[i].holder.Visible = false;
    }

    // --- Door (clay huts and brick houses) -------------------------------

    // The doorway in the building's frame: the door hangs from jamb a and closes
    // onto jamb b, swinging inward when open.
    const float DoorRadius = 1.7f, DoorHalfAngle = 0.44f, DoorHeight = 1.45f, DoorOpenAngle = 1.75f;
    Node3D? doorHinge, doorBar;
    bool? doorClosed;

    (Vector3 a, Vector3 b, float height) DoorFrame() => Kind == 5
        ? StructureModel.HouseDoor
        : (new Vector3(Mathf.Sin(DoorHalfAngle), 0, Mathf.Cos(DoorHalfAngle)) * DoorRadius,
           new Vector3(Mathf.Sin(-DoorHalfAngle), 0, Mathf.Cos(-DoorHalfAngle)) * DoorRadius, DoorHeight);

    // closed: null takes the door away (none hung yet). locked shows the bar across it.
    public void SetDoor(bool? closed, bool locked, bool animate)
    {
        if (doorBar != null && doorBar.Visible != locked)
        {
            doorBar.Visible = locked;
            if (locked && animate && IsInsideTree()) Settle(doorBar, doorBar.Position);
        }
        if (closed == doorClosed) return;
        if (closed == null)
        {
            doorHinge?.QueueFree();
            doorHinge = null;
            doorBar = null;
            doorClosed = null;
            return;
        }
        var (a, b, height) = DoorFrame();
        float baseYaw = Mathf.Atan2(-(b - a).Z, (b - a).X);
        bool fresh = doorHinge == null;
        if (doorHinge == null)
        {
            doorHinge = new Node3D { Position = a, Rotation = new Vector3(0, baseYaw, 0) };
            doorHinge.AddChild(new MeshInstance3D
            {
                Mesh = StructureModel.Door(a.DistanceTo(b), height),
                MaterialOverride = StructureModel.Wood,
            });
            // The bar: a stout pole dropped into two pegs across the middle of the door, outside
            // (the hinge's -Z faces out of the building).
            float width = a.DistanceTo(b);
            doorBar = new Node3D { Position = new Vector3(0, height * 0.52f, -0.08f), Visible = locked };
            doorBar.AddChild(new MeshInstance3D { Mesh = StructureModel.Bar(width), MaterialOverride = StructureModel.PaleWood });
            doorHinge.AddChild(doorBar);
            AddChild(doorHinge);
            if (animate && IsInsideTree()) Settle(doorHinge, doorHinge.Position);
        }
        doorClosed = closed;
        float yaw = baseYaw - (closed.Value ? 0f : DoorOpenAngle);
        if (animate && !fresh && IsInsideTree())
            CreateTween().TweenProperty(doorHinge, "rotation:y", yaw, 0.6).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        else
            doorHinge.Rotation = new Vector3(0, yaw, 0);
    }

    // --- Weathering ---------------------------------------------------------

    // 0 is sound; toward 1 it darkens and sags, in the last day before it is gone (D-046).
    float weathered;
    static readonly StandardMaterial3D Grime = new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        BlendMode = BaseMaterial3D.BlendModeEnum.Mul,
        AlbedoColor = new Color(0.55f, 0.5f, 0.45f),
    };

    public void SetWeathered(float amount)
    {
        amount = Mathf.Clamp(amount, 0f, 1f);
        if (Mathf.Abs(amount - weathered) < 0.02f) return;
        weathered = amount;
        var grime = amount > 0 ? Grime.Duplicate() as StandardMaterial3D : null;
        if (grime != null)
            grime.AlbedoColor = Colors.White.Lerp(new Color(0.55f, 0.5f, 0.45f), amount);
        foreach (var (_, _, mesh) in pieces)
            mesh.MaterialOverlay = grime;
        // Sag: it settles and leans a little as it gives way.
        Scale = new Vector3(1f, 1f - 0.12f * amount, 1f);
        RotationDegrees = new Vector3(4f * amount, RotationDegrees.Y, -3f * amount);
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
