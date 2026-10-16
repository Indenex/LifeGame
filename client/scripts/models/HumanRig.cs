using Godot;

namespace LifeGame;

// The player's skeleton, shared by the Modeling lane (which binds meshes to it)
// and the Animation lane (which poses it). Bone indices never change once used;
// add new bones at the end. The figure faces +Z with its left side at +X.
// Rest positions are in model space, feet on y = 0, for a 1.75 m person.
public static class HumanRig
{
    public const int Hips = 0, Chest = 1, Head = 2;
    public const int UpperArmL = 3, ForearmL = 4, HandL = 5;
    public const int UpperArmR = 6, ForearmR = 7, HandR = 8;
    public const int ThighL = 9, ShinL = 10, FootL = 11;
    public const int ThighR = 12, ShinR = 13, FootR = 14;
    public const int Count = 15;

    public static readonly string[] Names =
    {
        "hips", "chest", "head",
        "upper_arm.L", "forearm.L", "hand.L",
        "upper_arm.R", "forearm.R", "hand.R",
        "thigh.L", "shin.L", "foot.L",
        "thigh.R", "shin.R", "foot.R",
    };

    public static readonly int[] Parents = { -1, Hips, Chest, Chest, UpperArmL, ForearmL, Chest, UpperArmR, ForearmR, Hips, ThighL, ShinL, Hips, ThighR, ShinR };

    // Joint positions for a figure of the given proportions, in model space.
    public sealed class Joints
    {
        public Vector3[] At = new Vector3[Count];
    }

    public static Joints Place(float hipY, float chestY, float neckY, float shoulderX, float shoulderY, float hipX,
        float upperArm, float forearm, float thigh, float shin, float ankleY)
    {
        var j = new Joints();
        j.At[Hips] = new Vector3(0, hipY, 0);
        j.At[Chest] = new Vector3(0, chestY, 0);
        j.At[Head] = new Vector3(0, neckY, 0);
        for (int s = 0; s < 2; s++)
        {
            float x = s == 0 ? shoulderX : -shoulderX;
            int u = s == 0 ? UpperArmL : UpperArmR;
            // Arms hang a little out from the body, elbows slightly back.
            j.At[u] = new Vector3(x, shoulderY, 0);
            j.At[u + 1] = j.At[u] + new Vector3(Mathf.Sign(x) * 0.03f, -upperArm, -0.02f);
            j.At[u + 2] = j.At[u + 1] + new Vector3(Mathf.Sign(x) * 0.01f, -forearm, 0.03f);
            float hx = s == 0 ? hipX : -hipX;
            int t = s == 0 ? ThighL : ThighR;
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
