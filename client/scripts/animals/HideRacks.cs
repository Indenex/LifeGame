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
    static readonly Color Wet = new(0.78f, 0.66f, 0.58f);
    static readonly Color Dry = new(0.62f, 0.45f, 0.30f);
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
            // Two A-frames of leaning sticks, one at each end of the crossbar.
            foreach (float side in new[] { -0.45f, 0.45f })
                foreach (float lean in new[] { -0.22f, 0.22f })
                    AddChild(new MeshInstance3D
                    {
                        Mesh = new CylinderMesh { TopRadius = 0.018f, BottomRadius = 0.022f, Height = 1.05f, RadialSegments = 5, Rings = 1 },
                        MaterialOverride = wood,
                        Position = new Vector3(side, 0.48f, lean * 0.5f),
                        Rotation = new Vector3(-lean, 0, 0),
                    });
            AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.02f, Height = 1.1f, RadialSegments = 5, Rings = 1 },
                MaterialOverride = wood,
                Position = new Vector3(0, 0.98f, 0),
                Rotation = new Vector3(0, 0, Mathf.Pi / 2),
            });
            // The hide hangs over the bar on both sides, a little uneven.
            hide.Mesh = new BoxMesh { Size = new Vector3(0.8f, 0.62f, 0.015f), SubdivideWidth = 2, SubdivideHeight = 2 };
            hide.MaterialOverride = hideLook;
            hide.Position = new Vector3(0, 0.7f, 0.06f);
            hide.Rotation = new Vector3(-0.2f, 0, 0.03f);
            AddChild(hide);
            var back = (MeshInstance3D)hide.Duplicate();
            back.MaterialOverride = hideLook;
            back.Position = new Vector3(0.02f, 0.74f, -0.05f);
            back.Rotation = new Vector3(0.17f, 0, -0.04f);
            back.Scale = new Vector3(0.95f, 0.85f, 1f);
            AddChild(back);
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
