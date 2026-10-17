using Godot;
using LifeGame.Shared;

namespace LifeGame;

// How survival items look lying on the ground.
public static class SurvivalItemLook
{
    static Mesh? berries, roots;

    public static Mesh? MeshFor(Item item) => item switch
    {
        Item.Berries => berries ??= Look.Faceted(new SphereMesh { Radius = 0.07f, Height = 0.1f, RadialSegments = 6, Rings = 3 }, BerryBush.BerryColor),
        // A stand-in for wild roots (survival-36) until Modeling paints one: a pale tapered root.
        Item.WildRoots => roots ??= Look.Faceted(new CylinderMesh { TopRadius = 0.025f, BottomRadius = 0.004f, Height = 0.16f, RadialSegments = 5 }, new Color(0.82f, 0.72f, 0.52f)),
        _ => null,
    };
}
