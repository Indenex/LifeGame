using Godot;
using LifeGame.Shared;

namespace LifeGame;

// How survival items look lying on the ground.
public static class SurvivalItemLook
{
    static Mesh? berries, mushrooms;

    public static Mesh? MeshFor(Item item) => item switch
    {
        Item.Berries => berries ??= Look.Faceted(new SphereMesh { Radius = 0.07f, Height = 0.1f, RadialSegments = 6, Rings = 3 }, BerryBush.BerryColor),
        // A stand-in cap until Modeling makes a mushroom (survival-35).
        Item.Mushrooms => mushrooms ??= Look.Faceted(new SphereMesh { Radius = 0.06f, Height = 0.06f, IsHemisphere = true, RadialSegments = 8, Rings = 3 }, MushroomPatch.CapColor),
        _ => null,
    };
}
