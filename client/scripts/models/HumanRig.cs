using Godot;

namespace LifeGame;

// The player's skeleton, shared by the Modeling lane (which binds meshes to it)
// and the Animation lane (which poses it). Bone indices never change once used;
// add new bones at the end. The figure faces +Z with its left side at +X.
// Rest positions are in model space, feet on y = 0, for a 1.75 m person.
public static class HumanRig
{
    // Names and hierarchy agreed with the Animation lane: each bone sits at its joint
    // with no rotation at rest (standing, arms hanging); animation writes rotations
    // and the Hips position only.
    public const int Hips = 0, Spine = 1, Neck = 2, Head = 3;
    public const int ShoulderL = 4, ElbowL = 5, HandL = 6;
    public const int ShoulderR = 7, ElbowR = 8, HandR = 9;
    public const int HipL = 10, KneeL = 11, FootL = 12;
    public const int HipR = 13, KneeR = 14, FootR = 15;
    public const int Count = 16;

    public static readonly string[] Names =
    {
        "Hips", "Spine", "Neck", "Head",
        "ShoulderL", "ElbowL", "HandL",
        "ShoulderR", "ElbowR", "HandR",
        "HipL", "KneeL", "FootL",
        "HipR", "KneeR", "FootR",
    };

    public static readonly int[] Parents = { -1, Hips, Spine, Neck, Spine, ShoulderL, ElbowL, Spine, ShoulderR, ElbowR, Hips, HipL, KneeL, Hips, HipR, KneeR };

    // Joint positions for a figure of the given proportions, in model space.
    public sealed class Joints
    {
        public Vector3[] At = new Vector3[Count];
    }

    public static Joints Place(float hipY, float waistY, float neckY, float headY, float shoulderX, float shoulderY, float hipX,
        float upperArm, float forearm, float thigh, float shin, float ankleY)
    {
        var j = new Joints();
        j.At[Hips] = new Vector3(0, hipY, 0);
        j.At[Spine] = new Vector3(0, waistY, 0);
        j.At[Neck] = new Vector3(0, neckY, 0);
        j.At[Head] = new Vector3(0, headY, 0);
        for (int s = 0; s < 2; s++)
        {
            float x = s == 0 ? shoulderX : -shoulderX;
            int u = s == 0 ? ShoulderL : ShoulderR;
            // Arms hang a little out from the body, elbows slightly back.
            j.At[u] = new Vector3(x, shoulderY, 0);
            j.At[u + 1] = j.At[u] + new Vector3(Mathf.Sign(x) * 0.03f, -upperArm, -0.02f);
            j.At[u + 2] = j.At[u + 1] + new Vector3(Mathf.Sign(x) * 0.01f, -forearm, 0.03f);
            float hx = s == 0 ? hipX : -hipX;
            int t = s == 0 ? HipL : HipR;
            j.At[t] = new Vector3(hx, hipY - 0.02f, 0);
            j.At[t + 1] = j.At[t] + new Vector3(0, -thigh, 0.01f);
            j.At[t + 2] = new Vector3(hx, ankleY, -0.01f);
        }
        return j;
    }

    // A skeleton whose rest pose is these joints (each bone's rest is relative to
    // its parent). A MeshInstance3D under it with vertices in model space binds to
    // the rest pose without a Skin resource.
    public static Skeleton3D Skeleton(Joints j)
    {
        var skeleton = new Skeleton3D();
        for (int i = 0; i < Count; i++)
        {
            skeleton.AddBone(Names[i]);
            if (Parents[i] >= 0) skeleton.SetBoneParent(i, Parents[i]);
            var local = Parents[i] >= 0 ? j.At[i] - j.At[Parents[i]] : j.At[i];
            skeleton.SetBoneRest(i, new Transform3D(Basis.Identity, local));
        }
        skeleton.ResetBonePoses();
        return skeleton;
    }
}
