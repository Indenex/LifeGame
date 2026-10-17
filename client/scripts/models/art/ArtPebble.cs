using Godot;

namespace LifeGame;

// Showcase --view=art --piece=pebble (art-rnd-1): Pebble's head as built today (one
// ellipsoid head, a daisy of little ellipsoids) beside the Blender-authored head
// (ChibiFace): a smooth turtle head with a soft beak and brow, big soft scales on the
// crown, a painted face from the face atlas and a daisy. Framed on the upper body.
public static class ArtPebble
{
    public static float Spacing = 0.75f;
    public static float Height = 0.85f;
    static readonly ChibiFigure.Look Look = new(ChibiFigure.Race.Pebble, 0, 0, 1);

    public static Node3D Today() => ArtBramble.Bust(Look, false);

    public static Node3D Authored() => ArtBramble.Bust(Look, true);
}
