using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The player roster's shared chibi base (model-6, D-249/D-250/D-254): one body about
// 1.35 m tall and 2.8 heads high on HumanRig's 16 bones, so every race walks, works
// and jumps with Animation's poses. A race swaps parts on the same base: Bramble
// (chibi human) has hair; Kit (fox) has a muzzle, tall ears, a cream chest and a
// brush of a tail. Built like HumanFigure: one skinned mesh per look, the same
// Built record, so PlayerModel and Garments take either.
public static class ChibiFigure
{
    public const float Height = 1.35f;
    const float Heads = 2.8f;

    public enum Race { Bramble, Kit }

    // A chibi player's look: race, then a main colour, a second colour and a part
    // style, each an index into the race's lists below.
    public readonly record struct Look(Race Race, int Main, int Second, int Style)
    {
        public override string ToString() => $"{(int)Race}{Main}{Second}{Style}";
    }

    // Bramble: skin tones; hair colours; haircut (0 tousled, 1 cropped).
    public static readonly Color[] Skins = HumanFigure.Skins;
    public static readonly Color[] Hairs = HumanFigure.Hairs;
    // Kit: fur (russet, amber, grey, cocoa); chest and tail tip (cream, white);
    // ear style (0 tall, 1 rounded).
    public static readonly Color[] Furs = { new(0.86f, 0.46f, 0.2f), new(0.86f, 0.62f, 0.3f), new(0.56f, 0.55f, 0.56f), new(0.48f, 0.33f, 0.24f) };
    public static readonly Color[] Creams = { new(0.96f, 0.9f, 0.8f), new(0.98f, 0.97f, 0.94f) };

    static readonly Dictionary<string, HumanFigure.Built> Cache = new();
    static Material? material;
    static readonly Color Eye = new(0.1f, 0.08f, 0.07f);
    static readonly Color Nose = new(0.15f, 0.1f, 0.09f);
    static readonly Color Boot = new(0.24f, 0.18f, 0.16f);

    public static HumanFigure.Built Build(Look look)
    {
        string key = look.ToString();
        if (Cache.TryGetValue(key, out var done)) return done;
        material ??= LifeGame.Look.Solid(Colors.White);
        bool kit = look.Race == Race.Kit;
        var body = kit ? Furs[look.Main % Furs.Length] : Skins[look.Main % Skins.Length];
        var second = kit ? Creams[look.Second % Creams.Length] : Hairs[look.Second % Hairs.Length];

        const float H = Height;
        float hh = H / Heads;                       // the head, a touch under a third of the height
        float chin = H - hh;
        float neckY = chin + 0.01f;                         // next to no neck
        float shoulderY = chin - 0.04f;
        float hipY = 0.4f;
        const float ankleY = 0.06f;
        float thigh = (hipY - 0.02f - ankleY) * 0.5f;
        float waistY = MeshKit.Lerp(hipY, shoulderY, 0.4f);
        const float shoulderX = 0.19f, hipX = 0.085f;
        float arm = shoulderY - (hipY + 0.02f);
        var j = HumanRig.Place(hipY, waistY, neckY, chin + 0.02f, shoulderX, shoulderY, hipX, arm * 0.5f, arm * 0.5f, thigh, 0f, ankleY);

        var k = new MeshKit(false);
        // A round, soft body: pear-shaped, widest at the belly.
        k.Bone(HumanRig.Hips).Paint(body);
        k.Loft(new Vector3(0, hipY - 0.1f, 0), new Vector3(0, waistY + 0.04f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.15f, 0.13f), new(0.3f, 0.215f, 0.19f), new(0.7f, 0.22f, 0.195f), new(1f, 0.2f, 0.175f),
        }, 18, 0.5f);
        k.Bone(HumanRig.Spine).Paint(body);
        k.Loft(new Vector3(0, waistY - 0.03f, 0), new Vector3(0, chin + 0.03f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.2f, 0.175f), new(0.45f, 0.18f, 0.155f), new(0.85f, 0.14f, 0.12f), new(1f, 0.09f, 0.08f),
        }, 18, 0.4f);
        if (kit)
            // The cream chest patch, a soft oval on the belly.
            k.Paint(second).Ellipsoid(new Vector3(0, waistY + 0.0f, 0.145f), new Vector3(0.13f, 0.17f, 0.06f), 14, 8);

        // The head: big and round, a little wider than tall.
        k.Bone(HumanRig.Head).Paint(body);
        var headC = new Vector3(0, chin + hh * 0.5f, 0.01f);
        k.Ellipsoid(headC, new Vector3(hh * 0.54f, hh * 0.5f, hh * 0.48f), 16, 10);
        float faceZ = hh * 0.47f;
        if (kit)
        {
            // Muzzle: a cream snout pushed forward from the lower face, a dark nose.
            k.Paint(second).Ellipsoid(headC + new Vector3(0, -hh * 0.14f, faceZ * 0.85f), new Vector3(hh * 0.22f, hh * 0.15f, hh * 0.18f), 12, 6);
            k.Paint(Nose).Ellipsoid(headC + new Vector3(0, -hh * 0.08f, faceZ * 1.18f), new Vector3(hh * 0.055f, hh * 0.04f, hh * 0.035f), 8, 4);
            // Ears: tall pointed (style 0) or shorter and rounded (style 1), dark inside.
            bool round = look.Style % 2 == 1;
            for (int side = -1; side <= 1; side += 2)
            {
                var root = headC + new Vector3(side * hh * 0.3f, hh * 0.36f, -hh * 0.02f);
                var tip = root + new Vector3(side * hh * 0.1f, hh * (round ? 0.3f : 0.48f), -hh * 0.02f);
                k.Paint(body).Loft(root, tip, new MeshKit.Ring[]
                {
                    new(0f, hh * 0.14f, hh * 0.06f), new(0.6f, hh * (round ? 0.11f : 0.08f), hh * 0.05f), new(1f, hh * (round ? 0.05f : 0.008f), hh * 0.02f),
                }, 6, round ? 0.6f : 0.1f);
                k.Paint(Nose, 1.6f).Loft(root + new Vector3(0, hh * 0.04f, hh * 0.04f), tip + new Vector3(-side * hh * 0.01f, -hh * 0.08f, hh * 0.03f), new MeshKit.Ring[]
                {
                    new(0f, hh * 0.08f, hh * 0.015f), new(1f, hh * 0.01f, hh * 0.01f),
                }, 5, 0.1f);
            }
        }
        else
        {
            // Bramble: a small nose, rosy cheeks, and a tousled cap of hair with a
            // fringe (style 0) or cropped close (style 1).
            k.Paint(body, 0.95f).Ellipsoid(headC + new Vector3(0, -hh * 0.06f, faceZ * 0.98f), new Vector3(hh * 0.05f, hh * 0.045f, hh * 0.04f), 8, 4);
            bool cropped = look.Style % 2 == 1;
            k.Paint(second).Ellipsoid(headC + new Vector3(0, hh * 0.12f, -hh * 0.04f), new Vector3(hh * 0.57f, hh * (cropped ? 0.42f : 0.46f), hh * 0.5f), 16, 10, null, 1f);
            if (!cropped)
                for (int i = 0; i < 7; i++)
                {
                    float a = -0.9f + i * 0.3f;
                    var at = headC + new Vector3(Mathf.Sin(a) * hh * 0.42f, hh * 0.28f - Mathf.Abs(a) * hh * 0.05f, Mathf.Cos(a) * hh * 0.38f);
                    k.Paint(second, 0.92f + MeshKit.Hash(i, 3) * 0.1f).Ellipsoid(at, new Vector3(hh * 0.13f, hh * 0.1f, hh * 0.1f), 8, 5);
                }
        }
        // Eyes: big, dark and set wide, each with a catch-light; brows; cheeks.
        for (int side = -1; side <= 1; side += 2)
        {
            var eye = headC + new Vector3(side * hh * 0.2f, hh * 0.02f, faceZ * 0.93f);
            k.Paint(Eye).Ellipsoid(eye, new Vector3(hh * 0.07f, hh * 0.085f, hh * 0.035f), 10, 6);
            k.Paint(new Color(0.97f, 0.96f, 0.92f)).Ellipsoid(eye + new Vector3(hh * 0.02f, hh * 0.03f, hh * 0.025f), new Vector3(hh * 0.022f, hh * 0.022f, hh * 0.01f), 6, 3);
            k.Paint(body.Lerp(new Color(0.9f, 0.45f, 0.4f), 0.4f)).Ellipsoid(headC + new Vector3(side * hh * 0.3f, -hh * 0.12f, faceZ * 0.8f), new Vector3(hh * 0.07f, hh * 0.04f, hh * 0.02f), 8, 3);
        }

        // Short arms with round mitten hands.
        for (int side = 0; side < 2; side++)
        {
            int u = side == 0 ? HumanRig.ShoulderL : HumanRig.ShoulderR;
            Vector3 sh = j.At[u], el = j.At[u + 1], wr = j.At[u + 2];
            k.Bone(u).Paint(body).Loft(sh, el, new MeshKit.Ring[] { new(0f, 0.06f, 0.06f), new(1f, 0.05f, 0.05f) }, 10, 0.6f);
            k.Bone(u + 1).Loft(el, wr, new MeshKit.Ring[] { new(0f, 0.05f, 0.05f), new(1f, 0.045f, 0.045f) }, 10, 0.4f);
            k.Bone(u + 2).Ellipsoid(wr + Vector3.Down * 0.045f, new Vector3(0.068f, 0.072f, 0.065f), 12, 8);
        }
        // Short legs and chunky boots.
        for (int side = 0; side < 2; side++)
        {
            int t = side == 0 ? HumanRig.HipL : HumanRig.HipR;
            Vector3 hip = j.At[t], knee = j.At[t + 1], ankle = j.At[t + 2];
            k.Bone(t).Paint(body).Loft(hip + Vector3.Up * 0.03f, knee, new MeshKit.Ring[] { new(0f, 0.075f, 0.075f), new(1f, 0.062f, 0.062f) }, 10, 0.3f);
            k.Bone(t + 1).Loft(knee, ankle, new MeshKit.Ring[] { new(0f, 0.062f, 0.062f), new(1f, 0.055f, 0.055f) }, 10, 0.3f);
            k.Bone(t + 2).Paint(Boot).Ellipsoid(new Vector3(ankle.X, 0.05f, 0.03f), new Vector3(0.075f, 0.055f, 0.11f), 12, 6, null, 0.6f);
        }

        if (kit)
        {
            // The tail: a full brush curving out and up from the hips, the tip cream.
            k.Bone(HumanRig.Hips);
            var path = new[] { new Vector3(0, hipY - 0.02f, -0.12f), new Vector3(0.05f, hipY - 0.06f, -0.28f), new Vector3(0.12f, hipY + 0.04f, -0.42f), new Vector3(0.15f, hipY + 0.2f, -0.48f) };
            k.Paint(body).Loft(path[0], path[2], new MeshKit.Ring[]
            {
                new(0f, 0.06f, 0.06f), new(0.4f, 0.13f, 0.13f, 0.0f, 0.03f), new(1f, 0.14f, 0.14f, 0.02f, 0.06f),
            }, 12, 0.3f);
            k.Paint(second).Loft(path[2] + new Vector3(0.02f, -0.02f, 0), path[3], new MeshKit.Ring[]
            {
                new(0f, 0.135f, 0.135f), new(0.6f, 0.1f, 0.1f), new(1f, 0.01f, 0.01f),
            }, 12, 0.5f);
        }

        var mesh = new ArrayMesh();
        k.Commit(mesh, material);
        int tris = k.Triangles;

        // Kit wears a blue scarf by default; Bramble the leaf wrap round the middle.
        k = new MeshKit(false);
        if (kit)
        {
            k.Bone(HumanRig.Spine).Paint(new Color(0.42f, 0.6f, 0.76f)).Loft(new Vector3(0, chin - 0.01f, 0), new Vector3(0, chin + 0.05f, 0), new MeshKit.Ring[]
            {
                new(0f, 0.15f, 0.14f), new(1f, 0.14f, 0.13f),
            }, 16, 0.2f);
            k.Paint(new Color(0.38f, 0.55f, 0.72f)).Blade(new Vector3(0.02f, chin, 0.12f), new Vector3(0.05f, chin - 0.16f, 0.15f), Vector3.Forward * -1f, 0.1f, 0.01f);
        }
        else
        {
            k.Bone(HumanRig.Hips).Paint(LifeGame.Look.Leaf).Loft(new Vector3(0, hipY - 0.12f, 0), new Vector3(0, hipY + 0.02f, 0), new MeshKit.Ring[]
            {
                new(0f, 0.235f, 0.21f), new(1f, 0.225f, 0.2f),
            }, 11, 0.05f);
        }
        var wrap = new ArrayMesh();
        k.Commit(wrap, material);

        done = new HumanFigure.Built
        {
            Style = HumanFigure.Soft, Mesh = mesh, LeafWrap = wrap, Joints = j, Triangles = tris + k.Triangles, GripDrop = 0.04f,
            HeadHeight = hh, HipY = hipY, WaistY = waistY, ShoulderY = shoulderY, NeckY = neckY, PelvisWidth = 0.17f,
        };
        Cache[key] = done;
        return done;
    }

    // A ready figure: the skeleton with the body bound, and the starting wrap or scarf.
    public static Skeleton3D Create(Look look)
    {
        var built = Build(look);
        var skeleton = HumanRig.Skeleton(built.Joints);
        skeleton.AddChild(new MeshInstance3D { Name = "Body", Mesh = built.Mesh, Skeleton = new NodePath("..") });
        skeleton.AddChild(new MeshInstance3D { Name = PlayerModel.SlotNode(PlayerModel.Body), Mesh = built.LeafWrap, Skeleton = new NodePath("..") });
        return skeleton;
    }
}
