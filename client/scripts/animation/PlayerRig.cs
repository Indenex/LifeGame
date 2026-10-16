using Godot;

namespace LifeGame;

// The player figure's joints. A figure is either a tree of Node3D pivots with these
// exact names, each at its joint with its mesh hanging from it (today's capsules in
// Avatar.cs), or a Skeleton3D with bones of these names and rigidly weighted mesh (the
// Modeling lane's PlayerModel: one draw call per player). Rest pose: standing straight,
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
    readonly Skeleton3D? skeleton;
    readonly int[] bones = new int[Pose.Count];
    readonly Vector3 hipsRest;

    public PlayerRig(Node3D figure)
    {
        skeleton = figure as Skeleton3D;
        for (int i = 0; i < Pose.Count; i++)
        {
            string name = ((Joint)i).ToString();
            if (skeleton != null) bones[i] = skeleton.FindBone(name);
            else pivots[i] = figure.FindChild(name, true, false) as Node3D;
        }
        hipsRest = RestPosition(Joint.Hips);
        LegLength = -(RestPosition(Joint.KneeL).Y + RestPosition(Joint.FootL).Y);
        if (LegLength < 0.2f) LegLength = 0.8f;
    }

    Vector3 RestPosition(Joint j) => skeleton != null
        ? bones[(int)j] >= 0 ? skeleton.GetBoneRest(bones[(int)j]).Origin : Vector3.Zero
        : pivots[(int)j]?.Position ?? Vector3.Zero;

    // Hip joint to ankle at rest, for keeping the feet on the ground.
    public readonly float LegLength;

    public void Apply(Pose pose)
    {
        if (skeleton != null)
        {
            for (int i = 0; i < Pose.Count; i++)
                if (bones[i] >= 0) skeleton.SetBonePoseRotation(bones[i], Quaternion.FromEuler(pose.Rot[i]));
            if (bones[(int)Joint.Hips] >= 0) skeleton.SetBonePosePosition(bones[(int)Joint.Hips], hipsRest + pose.HipsOffset);
            return;
        }
        for (int i = 0; i < Pose.Count; i++)
            if (pivots[i] is { } p) p.Rotation = pose.Rot[i];
        if (pivots[(int)Joint.Hips] is { } hips) hips.Position = hipsRest + pose.HipsOffset;
    }
}
