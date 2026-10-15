using Godot;
using LifeGame.Shared;

namespace LifeGame;

// How survival items look lying on the ground.
public static class SurvivalItemLook
{
    static Mesh? berries;

    public static Mesh? MeshFor(Item item) => item switch
    {
        Item.Berries => berries ??= Look.Faceted(new SphereMesh { Radius = 0.07f, Height = 0.1f, RadialSegments = 6, Rings = 3 }, BerryBush.BerryColor),
        _ => null,
    };
}
