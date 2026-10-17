using Godot;

namespace LifeGame;

// Showcase --view=art --piece=faces --close --yaws=0,0,0,0 (art-rnd-1): the four
// expressions of the face atlas (open, closed, happy, sleepy), one per column, on
// Bramble's authored head (top) and Pebble's (bottom), front-on. Each Authored() call
// shows the next expression. Today's heads have no expressions, so Today() is empty.
public static class ArtFaces
{
    public static float Spacing = 0.68f;
    public static float Height = 1.3f;
    static int next;

    public static Node3D Today() => new();

    public static Node3D Authored()
    {
        int expression = next++ % 4;
        var root = new Node3D();
        // The heads are modelled in rig space, their centre 1.109 m up.
        foreach (var (race, y) in new[] { (ChibiFigure.Race.Bramble, 0.95f), (ChibiFigure.Race.Pebble, 0.33f) })
        {
            var head = ChibiFace.Head(new ChibiFigure.Look(race, 0, race == ChibiFigure.Race.Bramble ? 2 : 0, 1));
            head.Position = new Vector3(0, y - 1.109f, 0);
            ChibiFace.SetExpression(head, expression);
            root.AddChild(head);
        }
        return root;
    }
}
