using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Hides drying on little stick racks (animals-5). Each rack is an A-frame of two
// leaning sticks and a crossbar, with the scraped hide hung over it. The hide
// starts pale pink and wet and darkens to tan leather as it dries; once it is dry,
// G takes it down as leather.
public partial class HideRacks : Node3D
{
    static readonly Color Wet = new(0.84f, 0.69f, 0.62f);
    static readonly Color Dry = new(0.55f, 0.37f, 0.22f);
    static readonly Color Wood = new(0.42f, 0.31f, 0.20f);

    readonly Dictionary<ulong, Rack> racks = new();
    Terrain terrain = null!;
    DbConnection? conn;

    public void Bind(DbConnection connection, Terrain ground)
    {
        conn = connection;
        terrain = ground;
        conn.Db.HideRack.OnInsert += (_, r) => Upsert(r);
        conn.Db.HideRack.OnUpdate += (_, _, r) => Upsert(r);
        conn.Db.HideRack.OnDelete += (_, r) =>
        {
            if (racks.Remove(r.Id, out var node)) node.QueueFree();
        };
        Use.Register(new UseHandler { Id = "animals.leather", Priority = 14, Find = OfferLeather });
    }

    void Upsert(HideRack r)
    {
        if (!racks.TryGetValue(r.Id, out var node))
        {
            node = new Rack();
            racks[r.Id] = node;
            AddChild(node);
            node.Position = new Vector3(r.X, terrain.HeightAt(r.X, r.Z), r.Z);
            node.Rotation = new Vector3(0, r.Heading, 0);
        }
        node.Set(r.Moisture, r.MoistureAt.MicrosecondsSinceUnixEpoch / 1_000_000.0, r.DryRate);
    }

    UseOffer? OfferLeather(Vector3 at)
    {
        if (conn == null) return null;
        HideRack? nearest = null;
        float best = World.Reach;
        foreach (var r in conn.Db.HideRack.Iter())
        {
            float d = new Vector2(r.X - at.X, r.Z - at.Z).Length();
            if (d <= best) (nearest, best) = (r, d);
        }
        if (nearest == null || !racks.TryGetValue(nearest.Id, out var node) || node.Moisture > 0f) return null;
        ulong id = nearest.Id;
        return new UseOffer("Take the leather", best, () => conn.Reducers.TakeLeather(id));
    }

    partial class Rack : Node3D
    {
        readonly MeshInstance3D hide = new();
        readonly StandardMaterial3D hideLook = new() { Roughness = 0.9f, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
        float moisture = 1f, rate;
        double at;
        int lastStep = -1;

        public float Moisture => ClayRules.MoistureAt(moisture, rate, (float)(PotView.Now - at));

        public Rack()
        {
            var wood = Look.Solid(Wood);
            // An A-frame of two crossed sticks at each end, a crossbar resting in the forks.
            foreach (float side in new[] { -0.55f, 0.55f })
                foreach (float foot in new[] { -1f, 1f })
                    AddChild(new MeshInstance3D
                    {
                        Mesh = new CylinderMesh { TopRadius = 0.016f, BottomRadius = 0.022f, Height = 1.16f, RadialSegments = 5, Rings = 1 },
                        MaterialOverride = wood,
                        Position = new Vector3(side, 0.52f, foot * 0.14f),
                        Rotation = new Vector3(-foot * 0.29f, 0, 0),
                    });
            AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.02f, Height = 1.3f, RadialSegments = 6, Rings = 1 },
                MaterialOverride = wood,
                Position = new Vector3(0, BarY, 0),
                Rotation = new Vector3(0, 0, Mathf.Pi / 2),
            });
            hide.Mesh = HideMesh();
            hide.MaterialOverride = hideLook;
            AddChild(hide);
        }

        const float BarY = 0.99f;

        // One side of a sheepskin, spine along the bar: u runs along the bar, v
        // down from it, with the leg flaps at the corners.
        static readonly Vector2[] Outline =
        {
            new(-0.44f, 0f), new(0.44f, 0f), new(0.41f, 0.17f), new(0.48f, 0.43f), new(0.39f, 0.47f),
            new(0.31f, 0.37f), new(0.12f, 0.43f), new(-0.08f, 0.45f), new(-0.27f, 0.38f),
            new(-0.35f, 0.49f), new(-0.46f, 0.45f), new(-0.40f, 0.16f),
        };

        // The hide folded over the bar, hanging down both sides a little apart.
        static ArrayMesh HideMesh()
        {
            var st = new SurfaceTool();
            st.Begin(Mesh.PrimitiveType.Triangles);
            var tris = Geometry2D.TriangulatePolygon(Outline);
            foreach (float side in new[] { 1f, -1f })
            {
                Vector3 At(Vector2 p)
                {
                    // Hangs out at an angle, sagging a touch in the middle.
                    float sag = 0.05f * p.Y * (1f - p.X * p.X * 4f);
                    float angle = 0.36f + 0.25f * p.Y;
                    return new Vector3(p.X, BarY + 0.02f - p.Y * Mathf.Cos(angle), side * (0.025f + p.Y * Mathf.Sin(angle) + sag));
                }
                for (int i = 0; i < tris.Length; i += 3)
                {
                    var a = At(Outline[tris[i]]);
                    var b = At(Outline[tris[i + 1]]);
                    var c = At(Outline[tris[i + 2]]);
                    var n = (b - a).Cross(c - a).Normalized();
                    if (n.Z * side < 0) (b, c, n) = (c, b, -n);
                    foreach (var v in new[] { a, b, c })
                    {
                        st.SetNormal(n);
                        st.AddVertex(v);
                    }
                }
            }
            return st.Commit();
        }

        public void Set(float m, double atSeconds, float dryRate)
        {
            moisture = m;
            at = atSeconds;
            rate = dryRate;
            lastStep = -1;
            Refresh();
        }

        public override void _Process(double delta) => Refresh();

        void Refresh()
        {
            float wet = Moisture;
            int step = Mathf.RoundToInt(wet * 40);
            if (step == lastStep) return;
            lastStep = step;
            hideLook.AlbedoColor = Dry.Lerp(Wet, wet);
            // Wet hide has a faint sheen; dry leather is matte.
            hideLook.Roughness = Mathf.Lerp(0.95f, 0.6f, wet);
        }
    }
}
