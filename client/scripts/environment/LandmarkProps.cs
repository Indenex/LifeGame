using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Builds the world features that shared/Features.cs places (env-5, env-10): huge old
// trees, rock outcrops and stream fords, one builder per FeatureDef.Model. Justin asked that no two look like copies of one
// another, so everything about each one comes from its own seed: the kind, size,
// proportions, lean, layout, the stones and limbs lying around it, moss and wear.
public static class LandmarkProps
{
    // Instance lists Terrain fills for its MultiMeshes: transform, color, custom data.
    // Props with several shape variants (Modeling's PropMeshes) go into one batch per
    // mesh and material. The stones that stop a walker are LandmarkBlocks (shared).
    public sealed class Lists
    {
        public readonly List<(Transform3D, Color, Color)> Trunks = new(), Leafy = new(), Pines = new();
        public readonly Dictionary<(Mesh, Material), List<(Transform3D, Color, Color)>> Batches = new();

        public void Put(Mesh mesh, Material material, (Transform3D, Color, Color) item)
        {
            if (!Batches.TryGetValue((mesh, material), out var list)) Batches[(mesh, material)] = list = new();
            list.Add(item);
        }
    }

    // Builds a feature standing on cell (x, z) at its trunk point `at`; heightAt gives
    // the ground under anything it lays round about.
    public delegate void Builder(int x, int z, Vector3 at, Func<float, float, float> heightAt, Lists into, Color custom);

    // A feature's look: its builder, and for one that blocks with a trunk, how broad
    // the trunk is as a multiple of an ordinary tree's.
    public sealed record Prop(Builder Build, Func<int, int, float>? Girth = null);

    static readonly Dictionary<string, Prop> Props = new()
    {
        ["landmark.giant_tree"] = new(GiantTree, GiantGirth),
        ["landmark.outcrop"] = new(Outcrop),
        ["landmark.ford"] = new(Ford),
        ["landmark.fallen_giant"] = new(FallenGiant),
    };

    public static Prop For(FeatureDef feature) => Props[feature.Model];

    static int Pick(int x, int z, uint salt, int count) => Math.Min((int)(H(x, z, salt) * count), count - 1);

    static float H(int x, int z, uint salt) => Terrain.Hash01(x, z, 1000u + salt);
    static float Range(int x, int z, uint salt, float from, float to) => from + H(x, z, salt) * (to - from);

    // How broad a giant tree's trunk is, as a multiple of an ordinary tree (collision
    // reads it too).
    public static float GiantGirth(int x, int z) => Landmarks.GiantGirth(x, z);

    // A huge old tree standing alone in a glade: most are oaks with a broad,
    // lumpy crown of several lobes; some are ancient pines. Each has its own girth,
    // height and lean, and a few stones and fallen limbs round its foot.
    public static void GiantTree(int x, int z, Vector3 at, Func<float, float, float> heightAt, Lists into, Color custom)
    {
        float yaw = H(x, z, 1) * Mathf.Tau;
        float girth = GiantGirth(x, z), height = Range(x, z, 3, 2.0f, 3.0f);
        // A lean of up to about eight degrees, in its own direction.
        float leanDir = H(x, z, 4) * Mathf.Tau, lean = Range(x, z, 5, 0f, 0.14f);
        var tilt = new Basis(new Vector3(Mathf.Cos(leanDir), 0, Mathf.Sin(leanDir)), lean);
        var trunkBasis = tilt * new Basis(Vector3.Up, yaw) * Basis.FromScale(new Vector3(girth, height, girth * Range(x, z, 6, 0.85f, 1.15f)));
        var foot = at with { Y = at.Y - 0.15f };
        into.Trunks.Add((new Transform3D(trunkBasis, foot), Look.Bark.Darkened(Range(x, z, 7, 0.05f, 0.2f)), custom));

        Color custom1 = custom with { A = Terrain.SeasonBroadleaf };
        if (H(x, z, 8) < 0.25f)
        {
            // An ancient pine: tall and narrow-crowned.
            var pine = tilt * new Basis(Vector3.Up, yaw) * Basis.FromScale(new Vector3(girth * 0.8f, height * 1.25f, girth * 0.8f));
            into.Pines.Add((new Transform3D(pine, foot), Look.Pine.Lightened(Range(x, z, 9, 0f, 0.1f)), custom));
        }
        else
        {
            // An oak: a main crown and two to four lobes pushed out to one side or
            // another, so the silhouette is never the same twice.
            var leaf = Look.Leaf.Lerp(Look.LeafLight, Range(x, z, 10, 0f, 0.35f)).Lerp(Look.LeafWarm, Range(x, z, 11, 0f, 0.15f));
            into.Leafy.Add((new Transform3D(trunkBasis.Scaled(new Vector3(1.05f, 0.95f, 1.05f)), foot), leaf, custom1));
            int lobes = 2 + (int)(H(x, z, 12) * 3f);
            for (int k = 0; k < lobes; k++)
            {
                float a = H(x, z, 20 + (uint)k) * Mathf.Tau, r = Range(x, z, 30 + (uint)k, 1.2f, 2.6f) * girth / 3f;
                float s = Range(x, z, 40 + (uint)k, 0.5f, 0.8f);
                var lobe = new Basis(Vector3.Up, a) * Basis.FromScale(new Vector3(girth * s, height * Range(x, z, 50 + (uint)k, 0.75f, 1.0f), girth * s));
                var origin = foot + tilt * new Vector3(Mathf.Cos(a) * r, Range(x, z, 60 + (uint)k, -0.4f, 0.6f), Mathf.Sin(a) * r);
                into.Leafy.Add((new Transform3D(lobe, origin), leaf.Darkened(Range(x, z, 70 + (uint)k, 0f, 0.08f)), custom1));
            }
        }

        // Round its foot: a few mossy stones and the odd fallen limb.
        int stones = (int)(H(x, z, 80) * 4f);
        for (int k = 0; k < stones; k++)
        {
            float a = H(x, z, 81 + (uint)k) * Mathf.Tau, r = Range(x, z, 85 + (uint)k, 2.5f, 6f);
            float px = at.X + Mathf.Cos(a) * r, pz = at.Z + Mathf.Sin(a) * r;
            Rock(x * 31 + k, z, new Vector3(px, heightAt(px, pz), pz), Range(x, z, 89 + (uint)k, 0.3f, 0.8f), into, custom);
        }
        if (H(x, z, 95) < 0.6f)
        {
            float a = H(x, z, 96) * Mathf.Tau, r = Range(x, z, 97, 3f, 6f);
            float px = at.X + Mathf.Cos(a) * r, pz = at.Z + Mathf.Sin(a) * r;
            var lying = new Basis(Vector3.Up, H(x, z, 98) * Mathf.Tau) * Basis.FromScale(Vector3.One * Range(x, z, 99, 0.6f, 1.0f));
            into.Put(PropMeshes.FallenLog(Pick(x, z, 100, PropMeshes.FallenLogVariants)), Painted.Bark,
                (new Transform3D(lying, new Vector3(px, heightAt(px, pz), pz)), Look.Bark.Darkened(0.2f), custom));
        }
    }

    // A huge old trunk lying across a glade (env-33): one of Modeling's giant logs (model-19),
    // straight or broken in two, each with its own root plate torn up. Each has its
    // own length, girth, direction and lie on the slope, its own rot (from fresh bark to
    // a soft, moss-green hulk), ferns crowding along its sides and the odd stone.
    public static void FallenGiant(int x, int z, Vector3 at, Func<float, float, float> heightAt, Lists into, Color custom)
    {
        int variant = Pick(x, z, 500, PropMeshes.FallenGiantLogVariants);
        // Its direction, length and girth come from shared/LandmarkBlocks.cs, which the server's
        // collision reads too (bugfix-49).
        var shape = LandmarkBlocks.FallenGiantOf(x, z);
        float yaw = shape.Yaw, length = shape.Length, girth = shape.Girth;
        float rot = H(x, z, 504);
        // Modeling's log lies along x, about 3 m long and 0.4 m thick at scale 1.
        var axis = new Vector3(shape.AxisX, 0f, shape.AxisZ);
        float half = shape.Half;
        float h0 = heightAt(at.X - axis.X * half, at.Z - axis.Z * half), h1 = heightAt(at.X + axis.X * half, at.Z + axis.Z * half);
        float pitch = Mathf.Atan2(h1 - h0, 2f * half);
        var basis = new Basis(Vector3.Up, yaw) * new Basis(Vector3.Back, pitch)
            * Basis.FromScale(new Vector3(length, girth * Range(x, z, 505, 0.85f, 1.05f), girth));
        var middle = new Vector3(at.X, (h0 + h1) * 0.5f - 0.12f * girth - 0.1f * rot, at.Z);
        // Modeling's giant log paints its own bark, earth and moss, so the tint only darkens it a
        // little, more on an old rotten one.
        var tint = PropMeshes.GiantTint.Darkened(Range(x, z, 506, 0f, 0.12f) + rot * 0.08f);
        into.Put(PropMeshes.FallenGiantLog(variant), Painted.Bark, (new Transform3D(basis, middle), tint, custom));

        // It blocks the way along its whole length (LandmarkBlocks.Circles).
        float radius = shape.Radius;

        // Ferns crowd along its sides, more on an old rotten one.
        var side = new Vector3(-axis.Z, 0f, axis.X);
        int ferns = 4 + (int)(H(x, z, 510) * (4f + 5f * rot));
        for (int k = 0; k < ferns; k++)
        {
            float t = Range(x, z, 520 + (uint)k, -0.9f, 0.9f) * half;
            float off = (H(x, z, 540 + (uint)k) < 0.5f ? -1f : 1f) * (radius + Range(x, z, 560 + (uint)k, 0.2f, 1.1f));
            var p = at + axis * t + side * off;
            var fern = new Basis(Vector3.Up, H(x, z, 580 + (uint)k) * Mathf.Tau) * Basis.FromScale(Vector3.One * Range(x, z, 600 + (uint)k, 1.6f, 2.6f));
            into.Put(Undergrowth.Fern, Look.Grass, (new Transform3D(fern, p with { Y = heightAt(p.X, p.Z) }),
                Look.Leaf.Lerp(Look.LeafLight, Range(x, z, 620 + (uint)k, 0f, 0.4f)), custom));
        }

        // A stone or two by one end.
        foreach (var p in LandmarkBlocks.FallenGiantStones(x, z, at.X, at.Z))
            Rock(p.SeedX, p.SeedZ, new Vector3(p.X, heightAt(p.X, p.Z), p.Z), p.Size, into, custom);
    }

    // A rock outcrop on the heath, in one of three kinds: a rounded cluster, a
    // ridge of stones running one way, or one great tilted slab with rubble.
    public static void Outcrop(int x, int z, Vector3 at, Func<float, float, float> heightAt, Lists into, Color custom)
    {
        // Where each stone lies and how big it is comes from shared/LandmarkBlocks.cs, which the
        // server's collision reads too (bugfix-49).
        foreach (var p in LandmarkBlocks.Outcrop(x, z, at.X, at.Z))
        {
            var pos = new Vector3(p.X, heightAt(p.X, p.Z), p.Z);
            if (p.Kind == LandmarkBlocks.PieceKind.Formation) Formation(p.SeedX, p.SeedZ, pos, p.Size, into, custom, p.Tipped);
            else Rock(p.SeedX, p.SeedZ, pos, p.Size, into, custom);
        }
    }

    // One boulder (one of Modeling's rock shapes) with its own proportions, tilt and
    // colour, some of them mossy.
    static void Rock(int x, int z, Vector3 at, float size, Lists into, Color custom)
    {
        var (sx, sy, sz) = LandmarkBlocks.RockStretch(x, z);
        var stretch = new Vector3(sx, sy, sz);
        float tiltDir = H(x, z, 303) * Mathf.Tau, tilt = Range(x, z, 304, 0f, 0.3f);
        var basis = new Basis(new Vector3(Mathf.Cos(tiltDir), 0, Mathf.Sin(tiltDir)), tilt) * new Basis(Vector3.Up, H(x, z, 305) * Mathf.Tau)
            * Basis.FromScale(stretch * size);
        var color = Look.WorldRock.Darkened(Range(x, z, 306, 0f, 0.15f)).Lerp(Look.Moss, H(x, z, 307) < 0.4f ? Range(x, z, 308, 0.1f, 0.35f) : 0f);
        into.Put(PropMeshes.Rock(Pick(x, z, 309, PropMeshes.RockVariants)), Painted.Rock,
            (new Transform3D(basis, at with { Y = at.Y - 0.18f * size * stretch.Y }), color, custom with { B = 1f }));
    }

    // One layered rock formation (one of Modeling's six outcrop shapes, about 2.6 m
    // across at scale 1), stretched, turned and tinted by its own seed. Tipped ones
    // lean up on one edge like a slab.
    static void Formation(int x, int z, Vector3 at, float scale, Lists into, Color custom, bool tipped)
    {
        var (sx, sy, sz) = LandmarkBlocks.FormationStretch(x, z);
        var stretch = new Vector3(sx, sy, sz);
        float tiltDir = H(x, z, 313) * Mathf.Tau, tilt = tipped ? Range(x, z, 314, 0.15f, 0.3f) : Range(x, z, 314, 0f, 0.1f);
        var basis = new Basis(new Vector3(Mathf.Cos(tiltDir), 0, Mathf.Sin(tiltDir)), tilt) * new Basis(Vector3.Up, H(x, z, 315) * Mathf.Tau)
            * Basis.FromScale(stretch * scale);
        var color = Look.WorldRock.Darkened(Range(x, z, 316, 0f, 0.12f));
        into.Put(PropMeshes.Outcrop(Pick(x, z, 317, PropMeshes.OutcropVariants)), Painted.Rock,
            (new Transform3D(basis, at with { Y = at.Y - 0.1f * scale }), color, custom with { B = 1f }));
    }

    // Stepping stones across a stream, or on some crossings a fallen trunk laid
    // over the water instead. Stones vary in size and flatness, and a few are
    // missing, so each ford is its own.
    public static void Ford(int x, int z, Vector3 at, Func<float, float, float> heightAt, Lists into, Color custom)
    {
        float top = at.Y;
        int row = FloorDiv(z + 13, 40);
        if (Terrain.Hash01(row, 0, 1400u) < 0.3f)
        {
            // A log crossing: one trunk across the stream at its deepest point.
            if (Land.StreamDistance(x, z) > Land.StreamDistance(x - 1, z) || Land.StreamDistance(x, z) > Land.StreamDistance(x + 1, z)) return;
            // Modeling's fallen log lies along x, centred on its origin, about 3 m long.
            var across = new Basis(Vector3.Up, Range(row, 1, 401, -0.25f, 0.25f)) * Basis.FromScale(new Vector3(Range(row, 1, 402, 1.7f, 2.1f), 1f, 1f));
            into.Put(PropMeshes.FallenLog(Pick(row, 1, 403, PropMeshes.FallenLogVariants)), Painted.Bark,
                (new Transform3D(across, new Vector3(x, World.WaterLevel - 0.05f, z + 0.3f)), Look.Bark.Darkened(0.18f), custom));
            return;
        }
        if (H(x, z, 410) < 0.18f) return;
        // Modeling's river stones are about 0.3 m across at scale 1.
        var stretch = new Vector3(Range(x, z, 411, 1.5f, 2.7f), Range(x, z, 412, 1.0f, 1.6f), Range(x, z, 413, 1.5f, 2.7f));
        var basis = new Basis(Vector3.Up, H(x, z, 414) * Mathf.Tau) * Basis.FromScale(stretch);
        var pos = new Vector3(x + Range(x, z, 415, -0.25f, 0.25f), top, z + Range(x, z, 416, -0.3f, 0.3f));
        var color = Look.WorldRock.Lightened(Range(x, z, 417, 0f, 0.08f)).Lerp(Look.Moss, H(x, z, 418) < 0.3f ? 0.25f : 0f);
        into.Put(PropMeshes.RiverStone(Pick(x, z, 419, PropMeshes.RiverStoneVariants)), Painted.Rock, (new Transform3D(basis, pos), color, custom with { B = 1f }));
    }

    static int FloorDiv(int a, int b) => a >= 0 ? a / b : (a - b + 1) / b;
}
