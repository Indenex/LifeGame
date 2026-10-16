using Godot;

namespace LifeGame;

// The player figure's joints. Every figure (today's capsules in Avatar.cs, later the
// Modeling lane's PlayerModel) is a tree of Node3D pivots with these exact names, each
// sitting at its joint with its mesh hanging from it. Rest pose: standing straight,
// arms down, +Z forward, every pivot rotation zero. Animation writes only the pivots'
// rotations and the Hips position, never meshes or scales.
public enum Joint
{
    Hips, Spine, Neck, Head,
    ShoulderL, ElbowL, HandL,
    ShoulderR, ElbowR, HandR,
    HipL, KneeL, FootL,
    HipR, KneeR, FootR,
}

// One frame of joint rotations (radians, Euler XYZ) plus how far the hips move from rest.
// Positive X swings a hanging limb backward and tips the spine forward.
public sealed class Pose
{
    public const int Count = (int)Joint.FootR + 1;
    public readonly Vector3[] Rot = new Vector3[Count];
    public Vector3 HipsOffset;

    public ref Vector3 this[Joint j] => ref Rot[(int)j];

    public void Clear()
    {
        System.Array.Clear(Rot);
        HipsOffset = Vector3.Zero;
    }

    // this = a + (b - a) * t, without allocating.
    public void Blend(Pose a, Pose b, float t)
    {
        for (int i = 0; i < Count; i++) Rot[i] = a.Rot[i].Lerp(b.Rot[i], t);
        HipsOffset = a.HipsOffset.Lerp(b.HipsOffset, t);
    }
}

public sealed class PlayerRig
{
    readonly Node3D?[] pivots = new Node3D?[Pose.Count];
    readonly Vector3 hipsRest;

    public PlayerRig(Node3D figure)
    {
        for (int i = 0; i < Pose.Count; i++)
            pivots[i] = figure.FindChild(((Joint)i).ToString(), true, false) as Node3D;
        hipsRest = pivots[(int)Joint.Hips]?.Position ?? Vector3.Zero;
        if (this[Joint.KneeL] is { } knee && this[Joint.FootL] is { } foot)
            LegLength = -(knee.Position.Y + foot.Position.Y);
    }

    public Node3D? this[Joint j] => pivots[(int)j];

    // Hip joint to ankle at rest, for keeping the feet on the ground.
    public readonly float LegLength = 0.8f;

    public void Apply(Pose pose)
    {
        for (int i = 0; i < Pose.Count; i++)
            if (pivots[i] is { } p) p.Rotation = pose.Rot[i];
        if (pivots[(int)Joint.Hips] is { } hips) hips.Position = hipsRest + pose.HipsOffset;
    }
}
