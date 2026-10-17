using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Pieces for Ceramics' cooking pot at the fire (SimmerView, ceramics-7), model-2:
// three stones the pot sits on over the embers, and the bits floating in what is
// cooking. Both are built in the pot's own frame (origin at the base center), with
// their materials baked in, so SimmerView adds them as plain MeshInstance3Ds.
public static class CookingMeshes
{
    public enum Dish { Broth, Stew, Tallow }

    static Mesh? under;
    static readonly Dictionary<(Dish, int), Mesh> floaters = new();
    // On the painted look (painted-19): the stones as structure stone, the floaters as food.
    static Material? Floating;

    static float H(int a, int b) => MeshKit.Hash(a, b + 130);

    static ArrayMesh Done(MeshKit k, Material material)
    {
        var mesh = new ArrayMesh();
        k.Commit(mesh, material);
        return mesh;
    }

    static Material Relief(float scale, float bump, float cavity)
    {
        var material = Look.Solid(Colors.White);
        material.SetShaderParameter("detail_scale", scale);
        material.SetShaderParameter("bump", bump);
        material.SetShaderParameter("cavity", cavity);
        return material;
    }

    // Three fist-sized stones wedged under the coil pot's base, sooty where the
    // fire licks them. Sized to the pot as SimmerView draws it (PotMesh at
    // PotView.Size).
    public static Mesh Under => under ??= BuildUnder();

    static ArrayMesh BuildUnder()
    {
        float baseRadius = BaseRadius(PotMesh.For(ClayForm.CoilPot, 0)) * PotView.Size;
        var k = new MeshKit(true) { Skinned = false };
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.Tau / 3f + 0.3f;
            var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            var radii = new Vector3(0.1f, 0.075f, 0.085f) * (0.9f + H(i, 1) * 0.25f);
            var at = dir * (baseRadius + radii.X * 0.6f) + Vector3.Up * radii.Y * 0.5f;
            var turn = new Basis(Vector3.Up, -a + H(i, 2));
            k.Paint(Look.Rock, 0.75f + H(i, 3) * 0.2f).Ellipsoid(at, radii, 6, 4, turn, 0.5f);
            k.Paint(new Color(0.18f, 0.15f, 0.13f)).Ellipsoid(at - dir * radii.X * 0.4f, radii * new Vector3(0.6f, 0.75f, 0.85f), 6, 3, turn, 0.5f);
        }
        return Done(k, PaintedStructures.Of(Relief(5f, 0.7f, 0.3f), PaintedStructures.Family.Stone));
    }

    // The pot's radius where it meets the ground.
    static float BaseRadius(ArrayMesh pot)
    {
        var verts = (Vector3[])pot.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex];
        float lowest = float.MaxValue, radius = 0f;
        foreach (var v in verts) lowest = Mathf.Min(lowest, v.Y);
        foreach (var v in verts)
            if (v.Y < lowest + 0.01f) radius = Mathf.Max(radius, new Vector2(v.X, v.Z).Length());
        return radius;
    }

    // What floats on the surface, for a liquid disc of this radius, centered at
    // the origin with the surface at y = 0 (place it at the water line):
    // broth, berries and a torn leaf or two; stew, chunks of mutton and fat;
    // tallow, pale lumps of fat not yet melted.
    public static Mesh Floaters(Dish dish, float radius)
    {
        int key = Mathf.RoundToInt(radius * 1000f);
        if (floaters.TryGetValue((dish, key), out var mesh)) return mesh;
        var k = new MeshKit(false) { Skinned = false };
        int count = dish == Dish.Broth ? 7 : dish == Dish.Stew ? 6 : 5;
        for (int i = 0; i < count; i++)
        {
            float a = i * 2.4f + H(i, 4);
            float r = radius * (0.1f + 0.55f * H(i, 5));
            var at = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            switch (dish)
            {
                case Dish.Broth:
                    k.Paint(BerryBush.BerryColor, 0.7f + H(i, 6) * 0.3f).Ellipsoid(at, Vector3.One * radius * 0.13f, 7, 4);
                    break;
                case Dish.Stew:
                    k.Paint(new Color(0.48f, 0.27f, 0.14f), 0.85f + H(i, 6) * 0.2f).Ellipsoid(at, new Vector3(0.24f, 0.11f, 0.18f) * radius, 6, 3, new Basis(Vector3.Up, a), 0.6f);
                    if (i % 2 == 0)
                        k.Paint(new Color(0.86f, 0.78f, 0.58f)).Ellipsoid(at + new Vector3(0.1f, 0.015f, 0.05f) * radius, new Vector3(0.1f, 0.05f, 0.08f) * radius, 6, 3);
                    break;
                default:
                    k.Paint(new Color(0.93f, 0.89f, 0.78f), 0.92f + H(i, 6) * 0.1f).Ellipsoid(at, new Vector3(0.22f, 0.07f, 0.18f) * radius * (0.7f + H(i, 7) * 0.6f), 7, 3, new Basis(Vector3.Up, a), 0.5f);
                    break;
            }
        }
        if (dish == Dish.Broth)
            for (int i = 0; i < 2; i++)
            {
                float a = 1.1f + i * 2.6f;
                var root = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * 0.35f + Vector3.Up * 0.003f;
                k.Paint(Look.Leaf.Lightened(0.1f)).Blade(root, root + new Vector3(Mathf.Cos(a + 1f), 0f, Mathf.Sin(a + 1f)) * radius * 0.35f, Vector3.Up, radius * 0.18f, 0.002f);
            }
        mesh = Done(k, Floating ??= PaintedItems.Of(Look.Solid(Colors.White), PaintedItems.Family.Food));
        floaters[(dish, key)] = mesh;
        return mesh;
    }
}
