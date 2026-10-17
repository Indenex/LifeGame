using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The chibi's face states (model-15), for Animation: PlayerModel.SetFace shows one.
// Append-only: the byte is the order of ChibiFigure.FaceNames.
public enum Face : byte { Open, Closed, Happy, Sleepy }

// The player roster's shared chibi base (model-6, D-249/D-250/D-254): one body about
// 1.35 m tall and 2.8 heads high on HumanRig's 16 bones, so every race walks, works
// and jumps with Animation's poses. A race swaps parts on the same base, after the
// roster-1 turnarounds (docs/character-concepts/roster-1): Bramble (chibi human:
// hair cap with a tuft, green tunic, trousers, satchel strap, boots); Pebble
// (tortoise: green skin, a shell on Spine kept behind the shoulders, a belly plate,
// a daisy); Kit (fox, on hold, D-255). Built like HumanFigure: one skinned mesh per look, the same
// Built record, so PlayerModel and Garments take either.
public static class ChibiFigure
{
    public const float Height = 1.35f;
    const float Heads = 2.8f;

    // In the order of the race byte on player_look (LifeGame.Shared.Looks.Races).
    public enum Race { Bramble, Pebble, Kit }

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
    static Material? flat;
    // The painted banded look (ChibiLook); false draws them with today's shader.
    public static bool Painted = true;
    static readonly Color Eye = new(0.1f, 0.08f, 0.07f);
    static readonly Color Nose = new(0.15f, 0.1f, 0.09f);
    static readonly Color Boot = new(0.24f, 0.18f, 0.16f);
    // The roster-1 colour keys.
    static readonly Color Tunic = new(0.44f, 0.56f, 0.31f), Trousers = new(0.42f, 0.35f, 0.30f), BrambleBoot = new(0.48f, 0.31f, 0.20f), Strap = new(0.55f, 0.37f, 0.24f);
    public static readonly Color[] PebbleSkins = { new(0.66f, 0.76f, 0.48f), new(0.62f, 0.72f, 0.5f), new(0.74f, 0.74f, 0.52f) };
    public static readonly Color[] Shells = { new(0.54f, 0.42f, 0.28f), new(0.5f, 0.42f, 0.36f), new(0.6f, 0.5f, 0.3f) };
    static readonly Color ShellRim = new(0.79f, 0.64f, 0.42f), BellyPlate = new(0.91f, 0.85f, 0.63f), PebbleFeet = new(0.56f, 0.66f, 0.40f);
    static readonly Color Petal = new(0.96f, 0.94f, 0.89f), PetalHeart = new(0.95f, 0.81f, 0.35f);

    public static HumanFigure.Built Build(Look look)
    {
        string key = look.ToString() + Painted;
        if (Cache.TryGetValue(key, out var done)) return done;
        var material = Painted ? (Material)ChibiLook.Material : flat ??= LifeGame.Look.Solid(Colors.White);
        bool kit = look.Race == Race.Kit, pebble = look.Race == Race.Pebble, bramble = look.Race == Race.Bramble;
        var body = kit ? Furs[look.Main % Furs.Length] : pebble ? PebbleSkins[look.Main % PebbleSkins.Length] : Skins[look.Main % Skins.Length];
        var second = kit ? Creams[look.Second % Creams.Length] : pebble ? Shells[look.Second % Shells.Length] : Hairs[look.Second % Hairs.Length];
        // What the torso and legs are dressed in: Bramble's tunic and trousers, or bare.
        var torso = bramble ? Tunic : body;
        var legs = bramble ? Trousers : body;
        var feet = bramble ? BrambleBoot : pebble ? PebbleFeet : Boot;

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
        k.Bone(HumanRig.Hips).Paint(torso);
        k.Loft(new Vector3(0, hipY - 0.1f, 0), new Vector3(0, waistY + 0.04f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.15f, 0.13f), new(0.3f, 0.215f, 0.19f), new(0.7f, 0.22f, 0.195f), new(1f, 0.2f, 0.175f),
        }, 18, 0.5f);
        k.Bone(HumanRig.Spine).Paint(torso);
        k.Loft(new Vector3(0, waistY - 0.03f, 0), new Vector3(0, chin + 0.03f, 0), new MeshKit.Ring[]
        {
            new(0f, 0.2f, 0.175f), new(0.45f, 0.18f, 0.155f), new(0.85f, 0.14f, 0.12f), new(1f, 0.09f, 0.08f),
        }, 18, 0.4f);
        if (kit)
            // The cream chest patch, a soft oval on the belly.
            k.Paint(second).Ellipsoid(new Vector3(0, waistY + 0.0f, 0.145f), new Vector3(0.13f, 0.17f, 0.06f), 14, 8);
        if (bramble)
        {
            // The tunic's hem flares a little over the trousers; a satchel strap
            // runs from the right shoulder to the left hip.
            k.Bone(HumanRig.Hips).Paint(Tunic, 0.9f).Loft(new Vector3(0, hipY - 0.12f, 0), new Vector3(0, hipY - 0.06f, 0), new MeshKit.Ring[]
            {
                new(0f, 0.2f, 0.18f), new(1f, 0.215f, 0.19f),
            }, 18, 0.1f);
            k.Bone(HumanRig.Spine).Paint(Strap).Tube(new[]
            {
                new Vector3(-0.16f, shoulderY + 0.02f, 0.07f), new Vector3(-0.05f, waistY + 0.12f, 0.19f), new Vector3(0.1f, waistY - 0.04f, 0.2f), new Vector3(0.19f, hipY - 0.02f, 0.1f),
            }, 0.018f, 5);
        }
        if (pebble)
        {
            // The belly plate: a pale shield on the front with painted seam lines.
            k.Bone(HumanRig.Spine).Paint(BellyPlate).Ellipsoid(new Vector3(0, waistY - 0.02f, 0.15f), new Vector3(0.14f, 0.19f, 0.06f), 14, 8);
            foreach (float y in new[] { -0.08f, 0.04f })
                k.Paint(BellyPlate, 0.72f).Tube(new[] { new Vector3(-0.12f, waistY - 0.02f + y, 0.19f), new Vector3(0, waistY - 0.02f + y, 0.208f), new Vector3(0.12f, waistY - 0.02f + y, 0.19f) }, 0.006f, 4);
            k.Paint(BellyPlate, 0.72f).Tube(new[] { new Vector3(0, waistY + 0.14f, 0.18f), new Vector3(0, waistY - 0.02f, 0.21f), new Vector3(0, waistY - 0.18f, 0.18f) }, 0.006f, 4);
            // The shell: a dome on the back from just under the shoulders to the hips,
            // a touch wider than the torso so it shows from the front, its front edge
            // behind the shoulder joints. A paler rim band round its lower edge.
            // A tall arch with a flat lower edge, as on the sheet's back view.
            var dome = new Vector3(0, hipY - 0.06f, -0.08f);
            k.Bone(HumanRig.Spine).Paint(second).Ellipsoid(dome, new Vector3(0.28f, 0.5f, 0.23f), 18, 14, null, 0.02f);
            k.Paint(ShellRim).Loft(new Vector3(0, hipY - 0.08f, -0.06f), new Vector3(0, hipY - 0.03f, -0.06f), new MeshKit.Ring[]
            {
                new(0f, 0.265f, 0.2f), new(1f, 0.27f, 0.205f),
            }, 18, 0.05f);
            // Pattern 1, scutes: pale plates set into the shell's back in rows.
            if (look.Style % 2 == 1)
            {
                var radii = new Vector3(0.28f, 0.5f, 0.23f);
                foreach (var (u, cols) in new[] { (0.36f, 1), (0.76f, 2), (1.16f, 3) })
                    for (int c = 0; c < cols; c++)
                    {
                        float a = (c - (cols - 1) * 0.5f) * 0.78f;
                        var at = dome + new Vector3(radii.X * Mathf.Sin(u) * Mathf.Sin(a), radii.Y * Mathf.Cos(u), -radii.Z * Mathf.Sin(u) * Mathf.Cos(a));
                        var off = at - dome;
                        var normal = new Vector3(off.X / (radii.X * radii.X), off.Y / (radii.Y * radii.Y), off.Z / (radii.Z * radii.Z)).Normalized();
                        k.Paint(second.Lerp(ShellRim, 0.55f)).Ellipsoid(at + normal * 0.003f, new Vector3(0.112f, 0.105f, 0.012f), 6, 3, MeshKit.Aim(normal) * new Basis(Vector3.Back, Mathf.Pi / 6));
                    }
            }
        }

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
        else if (pebble)
        {
            // A small daisy on the crown.
            var flower = headC + new Vector3(hh * 0.06f, hh * 0.5f, 0);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.Tau / 6;
                k.Paint(Petal).Ellipsoid(flower + new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a)) * hh * 0.07f, new Vector3(hh * 0.05f, hh * 0.02f, hh * 0.035f), 6, 3, new Basis(Vector3.Up, -a));
            }
            k.Paint(PetalHeart).Ellipsoid(flower + Vector3.Up * hh * 0.02f, Vector3.One * hh * 0.035f, 6, 4);
            k.Paint(PebbleFeet).Tube(new[] { flower + Vector3.Down * hh * 0.01f, flower + Vector3.Down * hh * 0.06f }, hh * 0.01f, 4);
        }
        else
        {
            // Bramble: a small nose, rosy cheeks, and a tousled cap of hair with a
            // fringe (style 0) or cropped close (style 1).
            k.Paint(body, 0.95f).Ellipsoid(headC + new Vector3(0, -hh * 0.06f, faceZ * 0.98f), new Vector3(hh * 0.05f, hh * 0.045f, hh * 0.04f), 8, 4);
            bool cropped = look.Style % 2 == 1;
            k.Paint(second).Ellipsoid(headC + new Vector3(0, hh * 0.12f, -hh * 0.04f), new Vector3(hh * 0.57f, hh * (cropped ? 0.42f : 0.46f), hh * 0.5f), 16, 10, null, 1f);
            // A single tuft standing up from the crown.
            var crown = headC + new Vector3(0, hh * 0.52f, -hh * 0.04f);
            k.Paint(second, 0.95f).Loft(crown, crown + new Vector3(hh * 0.06f, hh * 0.2f, -hh * 0.04f), new MeshKit.Ring[]
            {
                new(0f, hh * 0.08f, hh * 0.05f), new(1f, hh * 0.01f, hh * 0.01f),
            }, 6, 0.2f);
            if (!cropped)
                for (int i = 0; i < 7; i++)
                {
                    float a = -0.9f + i * 0.3f;
                    var at = headC + new Vector3(Mathf.Sin(a) * hh * 0.42f, hh * 0.28f - Mathf.Abs(a) * hh * 0.05f, Mathf.Cos(a) * hh * 0.38f);
                    k.Paint(second, 0.92f + MeshKit.Hash(i, 3) * 0.1f).Ellipsoid(at, new Vector3(hh * 0.13f, hh * 0.1f, hh * 0.1f), 8, 5);
                }
        }
        // Rosy cheeks, shared by every face. The eyes and mouth are separate small
        // meshes on the Head bone (Faces), so they can be switched (model-15).
        for (int side = -1; side <= 1; side += 2)
            k.Paint(body.Lerp(new Color(0.9f, 0.45f, 0.4f), 0.4f)).Ellipsoid(headC + new Vector3(side * hh * 0.3f, -hh * 0.12f, faceZ * 0.8f), new Vector3(hh * 0.07f, hh * 0.04f, hh * 0.02f), 8, 3);

        // Short arms with round mitten hands.
        for (int side = 0; side < 2; side++)
        {
            int u = side == 0 ? HumanRig.ShoulderL : HumanRig.ShoulderR;
            Vector3 sh = j.At[u], el = j.At[u + 1], wr = j.At[u + 2];
            k.Bone(u).Paint(torso).Loft(sh, el, new MeshKit.Ring[] { new(0f, 0.06f, 0.06f), new(1f, 0.05f, 0.05f) }, 10, 0.6f);
            k.Bone(u + 1).Loft(el, wr, new MeshKit.Ring[] { new(0f, 0.05f, 0.05f), new(1f, 0.045f, 0.045f) }, 10, 0.4f);
            k.Bone(u + 2).Paint(body).Ellipsoid(wr + Vector3.Down * 0.045f, new Vector3(0.068f, 0.072f, 0.065f), 12, 8);
        }
        // Short legs and chunky boots.
        for (int side = 0; side < 2; side++)
        {
            int t = side == 0 ? HumanRig.HipL : HumanRig.HipR;
            Vector3 hip = j.At[t], knee = j.At[t + 1], ankle = j.At[t + 2];
            k.Bone(t).Paint(legs).Loft(hip + Vector3.Up * 0.03f, knee, new MeshKit.Ring[] { new(0f, 0.075f, 0.075f), new(1f, 0.062f, 0.062f) }, 10, 0.3f);
            k.Bone(t + 1).Loft(knee, ankle, new MeshKit.Ring[] { new(0f, 0.062f, 0.062f), new(1f, 0.055f, 0.055f) }, 10, 0.3f);
            k.Bone(t + 2).Paint(feet).Ellipsoid(new Vector3(ankle.X, 0.05f, 0.03f), new Vector3(0.075f, 0.055f, 0.11f), 12, 6, null, 0.6f);
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
        else if (bramble)
        {
            // Bramble's outfit is on the body; a thin leaf belt marks the starting wrap.
            k.Bone(HumanRig.Hips).Paint(LifeGame.Look.Leaf).Loft(new Vector3(0, waistY - 0.07f, 0), new Vector3(0, waistY - 0.04f, 0), new MeshKit.Ring[]
            {
                new(0f, 0.225f, 0.2f), new(1f, 0.222f, 0.197f),
            }, 18, 0.05f);
        }
        else if (false)
        {
            k.Bone(HumanRig.Hips).Paint(LifeGame.Look.Leaf).Loft(new Vector3(0, hipY - 0.12f, 0), new Vector3(0, hipY + 0.02f, 0), new MeshKit.Ring[]
            {
                new(0f, 0.235f, 0.21f), new(1f, 0.225f, 0.2f),
            }, 11, 0.05f);
        }
        // Pebble starts with nothing on (the shell is the body); an empty wrap.
        var wrap = new ArrayMesh();
        if (!k.Empty) k.Commit(wrap, material);

        done = new HumanFigure.Built
        {
            Style = HumanFigure.Soft, Mesh = mesh, LeafWrap = wrap, Joints = j, Triangles = tris + k.Triangles, GripDrop = 0.04f,
            HeadHeight = hh, HipY = hipY, WaistY = waistY, ShoulderY = shoulderY, NeckY = neckY, PelvisWidth = 0.17f,
        };
        Cache[key] = done;
        return done;
    }

    // The face set (model-15): four small meshes in the Head bone's own frame, one
    // shown at a time under a BoneAttachment3D on Head ("FaceAttach"), in Face
    // order. PlayerModel.SetFace flips between them. Built per race and skin, since
    // the lids are a darker skin tone and Kit's mouth sits on its muzzle.
    public const string FaceAttach = "FaceAttach";
    public static readonly string[] FaceNames = { "Face_Open", "Face_Closed", "Face_Happy", "Face_Sleepy" };
    public static readonly StringName FaceMeta = "face";
    static readonly Dictionary<string, ArrayMesh[]> FaceCache = new();
    static readonly Color Mouth = new(0.32f, 0.13f, 0.12f), Tongue = new(0.9f, 0.52f, 0.52f), CatchLight = new(0.97f, 0.96f, 0.92f);

    public static ArrayMesh[] Faces(Look look)
    {
        string key = $"{(int)look.Race}:{look.Main}:{Painted}";
        if (FaceCache.TryGetValue(key, out var done)) return done;
        var material = Painted ? (Material)ChibiLook.Material : flat ??= LifeGame.Look.Solid(Colors.White);
        bool kit = look.Race == Race.Kit, pebble = look.Race == Race.Pebble;
        var body = kit ? Furs[look.Main % Furs.Length] : pebble ? PebbleSkins[look.Main % PebbleSkins.Length] : Skins[look.Main % Skins.Length];
        // Lids: a darker, warmer shade of the skin, soft rather than ink-black.
        var lid = body.Lerp(new Color(0.22f, 0.11f, 0.09f), 0.72f);
        // The same head as Build: its centre and radii, and the Head joint, so the
        // parts are written in the Head bone's frame.
        float hh = Height / Heads, chin = Height - hh;
        var headC = new Vector3(0, chin + hh * 0.5f, 0.01f);
        var headR = new Vector3(hh * 0.54f, hh * 0.5f, hh * 0.48f);
        var headJoint = new Vector3(0, chin + 0.02f, 0);
        float faceZ = hh * 0.47f;
        // Where the mouth goes: the face below the nose, or Kit's muzzle.
        var mouthC = kit ? headC + new Vector3(0, -hh * 0.14f, faceZ * 0.85f) : headC;
        var mouthR = kit ? new Vector3(hh * 0.22f, hh * 0.15f, hh * 0.18f) : headR;
        float mouthY = kit ? -hh * 0.075f : -hh * 0.165f;
        float ey = hh * 0.02f;
        float line = hh * 0.024f;

        var meshes = new ArrayMesh[4];
        for (int f = 0; f < 4; f++)
        {
            var k = new MeshKit(false) { Skinned = false, Xform = new Transform3D(Basis.Identity, -headJoint) };
            for (int side = -1; side <= 1; side += 2)
            {
                float ex = side * hh * 0.2f;
                switch ((Face)f)
                {
                    case Face.Open:
                        // Today's eyes: big, dark and set wide, each with a catch-light.
                        var eye = headC + new Vector3(ex, ey, faceZ * 0.93f);
                        k.Paint(Eye).Ellipsoid(eye, new Vector3(hh * 0.07f, hh * 0.085f, hh * 0.035f), 10, 6);
                        k.Paint(CatchLight).Ellipsoid(eye + new Vector3(hh * 0.02f, hh * 0.03f, hh * 0.025f), new Vector3(hh * 0.022f, hh * 0.022f, hh * 0.01f), 6, 3);
                        break;
                    case Face.Closed:
                        // A blink: a soft lid line curving down, a little below the eye's middle.
                        k.Paint(lid);
                        Stroke(k, headC, headR, t => new Vector2(ex + t * hh * 0.075f, ey - hh * 0.02f - hh * 0.03f * (1f - t * t)), line);
                        break;
                    case Face.Happy:
                        // ^ ^ : arcs curving up, in the eye colour.
                        k.Paint(Eye);
                        Stroke(k, headC, headR, t => new Vector2(ex + t * hh * 0.07f, ey - hh * 0.025f + hh * 0.05f * (1f - t * t)), line * 1.1f);
                        break;
                    case Face.Sleepy:
                        // Half-closed: the lower half of the eye under a flat, heavy lid
                        // line that droops a touch at the outer corner.
                        k.Paint(Eye);
                        float top = ey - hh * 0.012f;
                        Fan(k, headC, headR, new Vector2(ex, top - hh * 0.02f), t => new Vector2(ex + Mathf.Cos(t * Mathf.Tau) * hh * 0.066f, top + Mathf.Min(0f, Mathf.Sin(t * Mathf.Tau)) * hh * 0.06f), hh * 0.01f);
                        k.Paint(lid);
                        int s = side;
                        Stroke(k, headC, headR, t => new Vector2(ex + t * hh * 0.082f, top + hh * 0.004f - hh * 0.014f * Mathf.Max(0f, t * s) * Mathf.Max(0f, t * s)), line);
                        break;
                }
            }
            if ((Face)f == Face.Happy)
            {
                // A small open smile: a soft D, flat on top, with a hint of tongue.
                float w = hh * 0.075f, d = hh * 0.062f;
                k.Paint(Mouth);
                Fan(k, mouthC, mouthR, new Vector2(0, mouthY - d * 0.35f), t => D(t, mouthY, w, d), hh * 0.008f);
                k.Paint(Tongue);
                Fan(k, mouthC, mouthR, new Vector2(0, mouthY - d * 0.68f), t => new Vector2(Mathf.Cos(t * Mathf.Tau) * w * 0.5f, mouthY - d * 0.68f + Mathf.Sin(t * Mathf.Tau) * d * 0.2f), hh * 0.011f);
            }
            else if ((Face)f == Face.Sleepy)
            {
                // A small "o", as if mid-yawn.
                k.Paint(Mouth);
                Fan(k, mouthC, mouthR, new Vector2(0, mouthY), t => new Vector2(Mathf.Cos(t * Mathf.Tau) * hh * 0.026f, mouthY + Mathf.Sin(t * Mathf.Tau) * hh * 0.032f), hh * 0.008f);
            }
            var mesh = new ArrayMesh();
            k.Commit(mesh, material);
            meshes[f] = mesh;
        }
        FaceCache[key] = meshes;
        return meshes;
    }

    // A point on the front of an ellipsoid (centre c, radii r) at (x, y) from its
    // centre, lifted along the surface normal, so face parts sit just proud of it.
    static (Vector3 at, Vector3 normal) OnFront(Vector3 c, Vector3 r, Vector2 xy, float lift)
    {
        float u = 1f - xy.X * xy.X / (r.X * r.X) - xy.Y * xy.Y / (r.Y * r.Y);
        float z = r.Z * Mathf.Sqrt(Mathf.Max(u, 0.02f));
        var n = new Vector3(xy.X / (r.X * r.X), xy.Y / (r.Y * r.Y), z / (r.Z * r.Z)).Normalized();
        return (c + new Vector3(xy.X, xy.Y, z) + n * lift, n);
    }

    // A painted line along the face, t from -1 to 1: thickest in the middle and
    // tapering to soft round ends, like a brush stroke.
    static void Stroke(MeshKit k, Vector3 c, Vector3 r, System.Func<float, Vector2> at, float radius)
    {
        const int n = 6;
        for (int half = -1; half <= 1; half += 2)
        {
            var path = new Vector3[n + 1];
            for (int i = 0; i <= n; i++) path[i] = OnFront(c, r, at(half * i / (float)n), radius * 0.6f).at;
            k.Tube(path, radius, 6, radius * 0.55f);
            k.Ellipsoid(path[n], Vector3.One * radius * 0.55f, 6, 3);
        }
    }

    // A flat patch on the face: a fan from centre out to the outline at(t), t from 0 to 1.
    static void Fan(MeshKit k, Vector3 c, Vector3 r, Vector2 centre, System.Func<float, Vector2> at, float lift, int n = 20)
    {
        var (mid, mn) = OnFront(c, r, centre, lift);
        var (prev, pn) = OnFront(c, r, at(0f), lift);
        for (int i = 1; i <= n; i++)
        {
            var (p, nn) = OnFront(c, r, at(i / (float)n), lift);
            k.Tri(mid, prev, p, mn, pn, nn);
            (prev, pn) = (p, nn);
        }
    }

    // The smile's outline: a flat top edge, then a half-ellipse below, once round
    // as t runs from 0 to 1.
    static Vector2 D(float t, float top, float w, float d)
    {
        if (t < 0.25f) return new Vector2(Mathf.Lerp(-w, w, t / 0.25f), top);
        float a = (t - 0.25f) / 0.75f * Mathf.Pi;
        return new Vector2(Mathf.Cos(a) * w, top - Mathf.Sin(a) * d);
    }

    // Gives a figure the face set for a look: the attachment and its four parts the
    // first time, then only new meshes, so the face shown stays.
    public static void DressFace(Skeleton3D figure, Look look)
    {
        var faces = Faces(look);
        var attach = figure.GetNodeOrNull<BoneAttachment3D>(FaceAttach);
        if (attach == null)
        {
            attach = new BoneAttachment3D { Name = FaceAttach, BoneName = HumanRig.Names[HumanRig.Head] };
            for (int i = 0; i < FaceNames.Length; i++)
                attach.AddChild(new MeshInstance3D { Name = FaceNames[i], Visible = i == 0, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
            figure.AddChild(attach);
            figure.SetMeta(FaceMeta, (int)Face.Open);
        }
        for (int i = 0; i < FaceNames.Length; i++)
            attach.GetChild<MeshInstance3D>(i).Mesh = faces[i];
    }

    // Clothes cut to the chibi (D-074 items on the model-6 body): the wool tunic and
    // the foot wraps. An empty body slot shows the race's own wrap. Pebble's shell
    // leaves no room at the back, so Pebble wears the tunic as a bib over its belly.
    static readonly Dictionary<string, ArrayMesh?> GarmentCache = new();

    public static ArrayMesh? Garment(Look look, byte slot, ushort item)
    {
        var built = Build(look);
        if (slot == PlayerModel.Body && item != Garments.WoolTunic) return built.LeafWrap;
        bool pebble = look.Race == Race.Pebble;
        string key = $"{(pebble ? 1 : 0)}:{slot}:{item}";
        if (GarmentCache.TryGetValue(key, out var done)) return done;
        var wool = new Color(0.78f, 0.74f, 0.64f);
        var leather = new Color(0.55f, 0.37f, 0.22f);
        var cord = new Color(0.45f, 0.34f, 0.18f);
        var k = new MeshKit(false);
        float hipY = built.HipY, waistY = built.WaistY, chin = Height - Height / Heads;
        if (slot == PlayerModel.Body && item == Garments.WoolTunic)
        {
            if (pebble)
            {
                // A bib over the belly plate, hung from a cord round the neck.
                k.Bone(HumanRig.Spine).Paint(wool).Ellipsoid(new Vector3(0, waistY + 0.0f, 0.165f), new Vector3(0.18f, 0.24f, 0.065f), 14, 8);
                k.Paint(wool, 0.88f).Tube(new[] { new Vector3(-0.17f, waistY - 0.2f, 0.19f), new Vector3(0, waistY - 0.235f, 0.215f), new Vector3(0.17f, waistY - 0.2f, 0.19f) }, 0.012f, 4);
                k.Paint(cord).Tube(MeshKit.Circle(new Vector3(0, chin - 0.01f, 0.0f), Vector3.Up, 0.11f, 20), 0.008f, 4, -1f, false);
            }
            else
            {
                k.Bone(HumanRig.Spine).Paint(wool).Loft(new Vector3(0, waistY - 0.03f, 0), new Vector3(0, chin + 0.01f, 0), new MeshKit.Ring[]
                {
                    new(0f, 0.215f, 0.19f), new(0.45f, 0.195f, 0.17f), new(0.85f, 0.155f, 0.135f), new(1f, 0.105f, 0.095f),
                }, 18, 0.3f);
                // Short sleeves over the tops of the arms.
                for (int side = 0; side < 2; side++)
                {
                    int u = side == 0 ? HumanRig.ShoulderL : HumanRig.ShoulderR;
                    Vector3 sh = built.Joints.At[u], el = built.Joints.At[u + 1];
                    var down = (el - sh).Normalized();
                    k.Bone(u).Paint(wool, 0.97f).Loft(sh + Vector3.Up * 0.02f, sh + down * 0.09f, new MeshKit.Ring[]
                    {
                        new(0f, 0.07f, 0.072f), new(1f, 0.075f, 0.077f),
                    }, 10, 0.4f);
                }
                // The skirt flares from the waist over the hips; a darker hem and a cord belt.
                k.Bone(HumanRig.Hips).Paint(wool).Loft(new Vector3(0, waistY + 0.04f, 0), new Vector3(0, hipY - 0.15f, 0), new MeshKit.Ring[]
                {
                    new(0f, 0.215f, 0.19f), new(0.5f, 0.235f, 0.21f), new(1f, 0.25f, 0.225f),
                }, 18, 0.05f);
                k.Paint(wool, 0.88f).Loft(new Vector3(0, hipY - 0.135f, 0), new Vector3(0, hipY - 0.155f, 0), new MeshKit.Ring[]
                {
                    new(0f, 0.25f, 0.225f), new(1f, 0.253f, 0.228f),
                }, 18, 0.05f);
                k.Paint(cord).Loft(new Vector3(0, waistY - 0.01f, 0), new Vector3(0, waistY + 0.015f, 0), new MeshKit.Ring[]
                {
                    new(0f, 0.222f, 0.197f), new(1f, 0.222f, 0.197f),
                }, 18, 0.05f);
            }
        }
        else if (slot == PlayerModel.Feet && item == Garments.FootWraps)
        {
            for (int side = 0; side < 2; side++)
            {
                int t = side == 0 ? HumanRig.HipL : HumanRig.HipR;
                var ankle = built.Joints.At[t + 2];
                k.Bone(t + 2).Paint(leather).Ellipsoid(new Vector3(ankle.X, 0.052f, 0.03f), new Vector3(0.082f, 0.061f, 0.118f), 12, 6, null, 0.6f);
                k.Bone(t + 1).Paint(leather, 0.92f).Loft(new Vector3(ankle.X, 0.06f, -0.005f), new Vector3(ankle.X, 0.15f, -0.005f), new MeshKit.Ring[]
                {
                    new(0f, 0.066f, 0.066f), new(1f, 0.062f, 0.062f),
                }, 10, 0.1f);
                foreach (float y in new[] { 0.085f, 0.13f })
                    k.Paint(cord).Tube(MeshKit.Circle(new Vector3(ankle.X, y, -0.005f), Vector3.Up, 0.066f, 16), 0.006f, 4, -1f, false);
            }
        }
        ArrayMesh? mesh = null;
        if (!k.Empty)
        {
            mesh = new ArrayMesh();
            k.Commit(mesh, Painted ? ChibiLook.Material : LifeGame.Look.Solid(Colors.White));
        }
        GarmentCache[key] = mesh;
        return mesh;
    }

    // A ready figure: the skeleton with the body bound, the starting wrap or scarf,
    // and the face set showing Face.Open.
    public static Skeleton3D Create(Look look)
    {
        var built = Build(look);
        var skeleton = HumanRig.Skeleton(built.Joints);
        skeleton.AddChild(new MeshInstance3D { Name = "Body", Mesh = built.Mesh, Skeleton = new NodePath("..") });
        skeleton.AddChild(new MeshInstance3D { Name = PlayerModel.SlotNode(PlayerModel.Body), Mesh = built.LeafWrap, Skeleton = new NodePath("..") });
        DressFace(skeleton, look);
        return skeleton;
    }
}
