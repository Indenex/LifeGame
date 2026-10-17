using Godot;
using LifeGame.Shared;

namespace LifeGame;

// How survival items look lying on the ground.
public static class SurvivalItemLook
{
    static Mesh? berries, waterskin, waterskinFull;

    public static Mesh? MeshFor(Item item) => item switch
    {
        Item.Berries => berries ??= Look.Faceted(new SphereMesh { Radius = 0.07f, Height = 0.1f, RadialSegments = 6, Rings = 3 }, BerryBush.BerryColor),
        // Stand-ins until Modeling's waterskin models land (survival-20).
        Item.Waterskin => waterskin ??= Look.Faceted(new SphereMesh { Radius = 0.1f, Height = 0.14f, RadialSegments = 7, Rings = 4 }, new Color(0.55f, 0.40f, 0.26f)),
        Item.WaterskinOfWater => waterskinFull ??= Look.Faceted(new SphereMesh { Radius = 0.12f, Height = 0.2f, RadialSegments = 7, Rings = 4 }, new Color(0.50f, 0.38f, 0.30f)),
        _ => null,
    };
}
