using Godot;

namespace LifeGame;

// Showcase --view=art --piece=bramble (art-rnd-1): Bramble's head as built today (one
// ellipsoid head, an ellipsoid hair cap with a tuft, eyes as little ellipsoids) beside
// the Blender-authored head (ChibiFace): chunky sculpted hair clumps, a painted face from
// the face atlas, soft blush. Framed on the upper body, where the head is seen.
public static class ArtBramble
{
    public static float Spacing = 0.75f;
    public static float Height = 0.85f;
    // Showcase frames 0..Height; the figure is lowered so its head and shoulders fill it.
    const float Drop = -0.62f;
    static readonly ChibiFigure.Look Look = new(ChibiFigure.Race.Bramble, 0, 2, 0);

    public static Node3D Today() => Bust(Look, false);

    public static Node3D Authored() => Bust(Look, true);

    public static Node3D Bust(ChibiFigure.Look look, bool authored)
    {
        ChibiFigure.AuthoredHeads = authored;
        var figure = ChibiFigure.Create(look);
        ChibiFigure.AuthoredHeads = false;
        if (authored) ChibiFace.Sync(figure, look);
        figure.Position = new Vector3(0, Drop, 0);
        var root = new Node3D();
        root.AddChild(figure);
        return root;
    }
}
