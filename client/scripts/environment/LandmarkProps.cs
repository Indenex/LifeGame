using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Builds the landmarks that shared/Landmarks.cs places (env-5): huge old trees,
// rock outcrops and stream fords. Justin asked that no two look like copies of one
// another, so everything about each one comes from its own seed: the kind, size,
// proportions, lean, layout, the stones and limbs lying around it, moss and wear.
public static class LandmarkProps
{
    // Instance lists Terrain fills for its MultiMeshes: transform, color, custom data.
    public sealed class Lists
    {
        public readonly List<(Transform3D, Color, Color)> Trunks = new(), Leafy = new(), Pines = new(), Rocks = new();
    }

    static float H(int x, int z, uint salt) => Terrain.Hash01(x, z, 1000u + salt);
    static float Range(int x, int z, uint salt, float from, float to) => from + H(x, z, salt) * (to - from);

    // How broad a giant tree's trunk is, as a multiple of an ordinary tree (collision
    // reads it too).
    public static float GiantGirth(int x, int z) => Range(x, z, 2, 2.4f, 3.6f);

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
            var lying = new Basis(Vector3.Up, H(x, z, 98) * Mathf.Tau) * new Basis(Vector3.Back, Mathf.Pi / 2f)
                * Basis.FromScale(new Vector3(Range(x, z, 99, 0.6f, 1.0f), Range(x, z, 100, 0.6f, 1.1f), Range(x, z, 99, 0.6f, 1.0f)));
            into.Trunks.Add((new Transform3D(lying, new Vector3(px, heightAt(px, pz) + 0.08f, pz)), Look.Bark.Darkened(0.2f), custom));
        }
    }

    // A rock outcrop on the heath, in one of three kinds: a rounded cluster, a
    // ridge of stones running one way, or one great tilted slab with rubble.
    public static void Outcrop(int x, int z, Vector3 at, Func<float, float, float> heightAt, Lists into, Color custom)
    {
        int kind = (int)(H(x, z, 200) * 3f);
        float dir = H(x, z, 201) * Mathf.Tau;
        int count = kind switch { 0 => 4 + (int)(H(x, z, 202) * 4f), 1 => 3 + (int)(H(x, z, 202) * 4f), _ => 1 };
        for (int k = 0; k < count; k++)
        {
            Vector3 offset;
            float size;
            if (kind == 1)
            {
                // A ridge: stones strung along a line, biggest in the middle.
                float t = count == 1 ? 0f : k / (float)(count - 1) - 0.5f;
                float along = t * Range(x, z, 203, 5f, 9f);
                offset = new Vector3(Mathf.Cos(dir), 0, Mathf.Sin(dir)) * along + new Vector3(-Mathf.Sin(dir), 0, Mathf.Cos(dir)) * Range(x, z, 210 + (uint)k, -0.8f, 0.8f);
                size = Range(x, z, 220 + (uint)k, 1.2f, 2.2f) * (1.3f - Mathf.Abs(t));
            }
            else
            {
                float a = k * Mathf.Tau / Math.Max(count, 1) + H(x, z, 230 + (uint)k) * 0.9f;
                float r = k == 0 ? 0f : Range(x, z, 240 + (uint)k, 1.0f, 2.8f);
                offset = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                size = kind == 2 ? Range(x, z, 250, 3.0f, 4.0f) : k == 0 ? Range(x, z, 251, 2.2f, 3.2f) : Range(x, z, 260 + (uint)k, 1.1f, 2.3f);
            }
            float px = at.X + offset.X, pz = at.Z + offset.Z;
            Rock(x * 17 + k, z * 13, new Vector3(px, heightAt(px, pz), pz), size, into, custom, slab: kind == 2);
        }
        if (kind == 2)
        {
            // Rubble broken off the slab.
            int rubble = 2 + (int)(H(x, z, 270) * 4f);
            for (int k = 0; k < rubble; k++)
            {
                float a = H(x, z, 271 + (uint)k) * Mathf.Tau, r = Range(x, z, 280 + (uint)k, 2.2f, 4f);
                float px = at.X + Mathf.Cos(a) * r, pz = at.Z + Mathf.Sin(a) * r;
                Rock(x * 7 + k, z * 5 + k, new Vector3(px, heightAt(px, pz), pz), Range(x, z, 290 + (uint)k, 0.3f, 0.9f), into, custom);
            }
        }
    }

    // One stone with its own proportions, tilt and colour, some of them mossy.
    // A slab is broad, flat and tipped up on one edge.
    static void Rock(int x, int z, Vector3 at, float size, Lists into, Color custom, bool slab = false)
    {
        var stretch = slab
            ? new Vector3(Range(x, z, 300, 1.3f, 1.6f), Range(x, z, 301, 0.45f, 0.6f), Range(x, z, 302, 0.9f, 1.2f))
            : new Vector3(Range(x, z, 300, 0.7f, 1.35f), Range(x, z, 301, 0.55f, 1.2f), Range(x, z, 302, 0.7f, 1.35f));
        float tiltDir = H(x, z, 303) * Mathf.Tau, tilt = slab ? Range(x, z, 304, 0.25f, 0.45f) : Range(x, z, 304, 0f, 0.3f);
        var basis = new Basis(new Vector3(Mathf.Cos(tiltDir), 0, Mathf.Sin(tiltDir)), tilt) * new Basis(Vector3.Up, H(x, z, 305) * Mathf.Tau)
            * Basis.FromScale(stretch * size);
        var color = Look.Rock.Darkened(Range(x, z, 306, 0f, 0.15f)).Lerp(Look.Moss, H(x, z, 307) < 0.4f ? Range(x, z, 308, 0.1f, 0.35f) : 0f);
        into.Rocks.Add((new Transform3D(basis, at with { Y = at.Y - 0.18f * size * stretch.Y }), color, custom with { B = 1f }));
    }

    // Stepping stones across a stream, or on some crossings a fallen trunk laid
    // over the water instead. Stones vary in size and flatness, and a few are
    // missing, so each ford is its own.
    public static void Ford(int x, int z, float top, Lists into, Color custom)
    {
        int row = FloorDiv(z + 13, 40);
        if (Terrain.Hash01(row, 0, 1400u) < 0.3f)
        {
            // A log crossing: one trunk across the stream at its deepest point.
            if (Land.StreamDistance(x, z) > Land.StreamDistance(x - 1, z) || Land.StreamDistance(x, z) > Land.StreamDistance(x + 1, z)) return;
            var across = new Basis(Vector3.Up, Range(row, 1, 401, -0.25f, 0.25f)) * new Basis(Vector3.Back, Mathf.Pi / 2f)
                * Basis.FromScale(new Vector3(0.9f, Range(row, 1, 402, 2.0f, 2.6f), 0.9f));
            // Laid down, the trunk runs from its foot toward -x; start it on the east bank.
            into.Trunks.Add((new Transform3D(across, new Vector3(x + 3.2f, World.WaterLevel + 0.05f, z + 0.3f)), Look.Bark.Darkened(0.18f), custom));
            return;
        }
        if (H(x, z, 410) < 0.18f) return;
        var stretch = new Vector3(Range(x, z, 411, 0.45f, 0.8f), Range(x, z, 412, 0.22f, 0.4f), Range(x, z, 413, 0.45f, 0.8f));
        var basis = new Basis(Vector3.Up, H(x, z, 414) * Mathf.Tau) * Basis.FromScale(stretch);
        var pos = new Vector3(x + Range(x, z, 415, -0.25f, 0.25f), top, z + Range(x, z, 416, -0.3f, 0.3f));
        var color = Look.Rock.Lightened(Range(x, z, 417, 0f, 0.08f)).Lerp(Look.Moss, H(x, z, 418) < 0.3f ? 0.25f : 0f);
        into.Rocks.Add((new Transform3D(basis, pos), color, custom with { B = 1f }));
    }

    static int FloorDiv(int a, int b) => a >= 0 ? a / b : (a - b + 1) / b;
}
