using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// The updraft kiln's brickwork: a beehive dome of sun-dried bricks in staggered
// courses, a low door on +Z, and a short chimney. Each course is one mesh, bottom
// up, so a blueprint can show the courses going on one at a time as bricks arrive.
// y = 0 is the ground at the kiln's center.
public static class KilnModel
{
    public const float Radius = 0.78f;     // outer radius at the ground
    public const float DomeHeight = 1.15f;
    public const float Course = 0.095f;    // one row of bricks
    public const int ChimneyCourses = 5;
    const float DoorHalfAngle = 0.42f;
    const float DoorHeight = 0.5f;
    const float ChimneyRadius = 0.2f;

    static readonly Color Brick = new(0.66f, 0.53f, 0.40f);
    public static readonly Color Soot = new(0.09f, 0.07f, 0.06f);

    static List<Mesh>? courses;
    static Mesh? interior;

    // Dome courses first, then the chimney.
    public static List<Mesh> Courses => courses ??= Build(1234);

    public static int DomeCourses => Mathf.CeilToInt(DomeHeight / Course);

    // The sooty inside, seen through the door and between the bricks.
    public static Mesh Interior => interior ??= MakeInterior();

    static Mesh MakeInterior()
    {
        var mesh = new SphereMesh { Radius = Radius * 0.93f, Height = DomeHeight * 1.86f, RadialSegments = 20, Rings = 10 };
        mesh.Material = new StandardMaterial3D { AlbedoColor = Soot, CullMode = BaseMaterial3D.CullModeEnum.Front, Roughness = 1f };
        return mesh;
    }

    static List<Mesh> Build(int seed)
    {
        var rng = new RandomNumberGenerator { Seed = (ulong)seed };
        var material = Look.Solid(Colors.White);
        var list = new List<Mesh>();
        int dome = DomeCourses;
        for (int c = 0; c < dome + ChimneyCourses; c++)
        {
            var st = new SurfaceTool();
            st.Begin(Mesh.PrimitiveType.Triangles);
            if (c < dome)
            {
                float y = c * Course + Course * 0.5f;
                float s = Mathf.Clamp(y / DomeHeight, 0f, 1f);
                float r = Mathf.Max(ChimneyRadius + 0.02f, Radius * Mathf.Sqrt(1f - s * s * s));
                // Slope of the dome here, so each brick leans in with the wall.
                float lean = Mathf.Atan2(Radius * 1.5f * s * s / Mathf.Max(0.2f, Mathf.Sqrt(1f - s * s * s)), DomeHeight) * 0.9f;
                Lay(st, rng, y, r, lean, c, door: y < DoorHeight, small: false);
            }
            else
                Lay(st, rng, DomeHeight + (c - dome) * Course + Course * 0.5f, ChimneyRadius, 0f, c, door: false, small: true);
            st.SetMaterial(material);
            list.Add(st.Commit());
        }
        return list;
    }

    static void Lay(SurfaceTool st, RandomNumberGenerator rng, float y, float r, float lean, int course, bool door, bool small)
    {
        float length = small ? 0.16f : 0.25f;
        int count = Math.Max(5, Mathf.FloorToInt(Mathf.Tau * r / length));
        float offset = course % 2 == 0 ? 0f : 0.5f;  // stagger the joints
        for (int i = 0; i < count; i++)
        {
            float a = (i + offset) / count * Mathf.Tau;
            float fromDoor = Mathf.Abs(Mathf.Wrap(a, -Mathf.Pi, Mathf.Pi));
            if (door && fromDoor < DoorHalfAngle) continue;
            float stretch = Mathf.Tau * r / count / 0.24f * 0.96f;
            var basis = new Basis(Vector3.Up, a) * new Basis(Vector3.Right, -lean)
                * Basis.FromScale(new Vector3(stretch * (small ? 0.75f : 1f), 1f, small ? 0.8f : 1f));
            var at = new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r);
            float shade = rng.RandfRange(0.86f, 1.08f);
            var tint = new Color(Brick.R * shade, Brick.G * shade * rng.RandfRange(0.97f, 1.02f), Brick.B * shade);
            // Bricks round the door and on the chimney are smoke-stained.
            float stain = Mathf.Clamp((door ? 0f : Mathf.Max(0f, 0.6f - fromDoor) * 0.8f) + (small ? 0.35f : 0f), 0f, 0.6f);
            Box(st, new Transform3D(basis, at), new Vector3(0.12f, Course * 0.45f, 0.06f), tint.Lerp(Soot, stain));
        }
    }

    // One brick: a box with flat-shaded faces in a single vertex color.
    static void Box(SurfaceTool st, Transform3D xf, Vector3 half, Color color)
    {
        Span<Vector3> axes = stackalloc Vector3[] { Vector3.Right, Vector3.Up, Vector3.Back };
        for (int axis = 0; axis < 3; axis++)
            for (int sign = -1; sign <= 1; sign += 2)
            {
                var n = axes[axis] * sign;
                var u = axes[(axis + 1) % 3];
                var v = axes[(axis + 2) % 3];
                Vector3 Corner(float a, float b) => (n * half[axis] + u * a * half[(axis + 1) % 3] + v * b * half[(axis + 2) % 3]);
                var c0 = xf * Corner(-1, -1);
                var c1 = xf * Corner(1, -1);
                var c2 = xf * Corner(1, 1);
                var c3 = xf * Corner(-1, 1);
                var normal = (xf.Basis * n).Normalized();
                st.SetColor(color);
                st.SetNormal(normal);
                // Godot treats clockwise as front-facing: wind each face clockwise seen from outside.
                bool flip = (c1 - c0).Cross(c2 - c0).Dot(normal) > 0;
                foreach (var p in flip ? new[] { c0, c2, c1, c0, c3, c2 } : new[] { c0, c1, c2, c0, c2, c3 })
                    st.AddVertex(p);
            }
    }
}
