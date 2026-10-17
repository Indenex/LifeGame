using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Warm homes at night (atmosphere-19, Justin's pick): a hut or house with a fire
// burning inside spills warm light from its doorway onto the ground in front, faint
// when the door is shut, and smoke rises from its roof. The light alone does it: the
// flat glowing card that once stood in the doorway read as a lit panel, not an
// opening (bugfix-60), and the fire inside already shows through the door.
// Open campfires send up a thinner thread of smoke. All of it is strongest at night
// and fades as the fire burns down, so a camp reads as lived-in from afar.
// Everything comes from public rows (fire, structure, structure_door); House
// building's files are untouched.
public partial class Atmosphere
{
    const int HomeSlots = 6, SmokeSlots = 8;
    const float HomeReach = 90f;

    DbConnection? homesConn;
    float homesIn;
    readonly OmniLight3D[] doorLights = new OmniLight3D[HomeSlots];
    readonly CpuParticles3D[] smokes = new CpuParticles3D[SmokeSlots];
    readonly List<(Vector3 Door, float Yaw, bool Shut, float Strength, Vector3 Roof, float Dist, ulong Id, bool Chimney)> homes = new();

    // How see-through a building is now, 0 solid to 1 faded out of the camera's way
    // (House building's BuildingSystem.SeeThroughOf; Main wires it). A faded home's
    // doorway light fades with it, so the light doesn't hang in the air; its smoke stays.
    public Func<ulong, float>? SeeThrough;
    readonly List<(Vector3 At, float Strength, float Dist)> openFires = new();

    void ReadyHomes()
    {
        for (int i = 0; i < HomeSlots; i++)
        {
            doorLights[i] = new OmniLight3D
            {
                LightColor = new Color(1f, 0.6f, 0.28f),
                OmniRange = 5.5f,
                OmniAttenuation = 1.4f,
                ShadowEnabled = false,
                Visible = false,
            };
            AddChild(doorLights[i]);
        }
        for (int i = 0; i < SmokeSlots; i++)
        {
            smokes[i] = SmokePlume();
            AddChild(smokes[i]);
        }
    }

    // A round, soft-edged puff for each smoke particle.
    static Texture2D? smokePuff;
    static Texture2D SmokePuff => smokePuff ??= new GradientTexture2D
    {
        Width = 64,
        Height = 64,
        Fill = GradientTexture2D.FillEnum.Radial,
        FillFrom = new Vector2(0.5f, 0.5f),
        FillTo = new Vector2(0.5f, 0f),
        Gradient = new Gradient
        {
            Offsets = new[] { 0f, 1f },
            Colors = new[] { new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 0f) },
        },
    };

    static CpuParticles3D SmokePlume()
    {
        var ramp = new Gradient
        {
            Offsets = new[] { 0f, 0.25f, 1f },
            Colors = new[] { new Color(0.6f, 0.6f, 0.6f, 0f), new Color(0.62f, 0.62f, 0.64f, 0.22f), new Color(0.7f, 0.7f, 0.72f, 0f) },
        };
        var scale = new Curve();
        scale.AddPoint(new Vector2(0f, 0.4f));
        scale.AddPoint(new Vector2(1f, 1.8f));
        return new CpuParticles3D
        {
            // A particle's card would cast a hard square shadow that flickers (bugfix-65).
                        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Amount = 18,
            Lifetime = 7f,
            Emitting = false,
            Mesh = new QuadMesh { Size = new Vector2(0.9f, 0.9f), Material = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
                VertexColorUseAsAlbedo = true,
                AlbedoTexture = SmokePuff,
            } },
            Direction = Vector3.Up,
            Spread = 8f,
            Gravity = new Vector3(0f, 0.25f, 0f),
            InitialVelocityMin = 0.35f,
            InitialVelocityMax = 0.6f,
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 0.15f,
            ScaleAmountCurve = scale,
            ColorRamp = ramp,
            Visible = false,
        };
    }

    void ConnectHomes(DbConnection conn) => homesConn = conn;

    void UpdateHomes(float dt, Vector3 focus, float nightNow, Color air)
    {
        homesIn -= dt;
        if (homesIn <= 0f)
        {
            homesIn = 1f;
            FindHomes(focus);
        }
        float glow = 0.25f + 0.75f * nightNow;
        float flicker = FireFlicker(Look.Clock);
        for (int i = 0; i < HomeSlots; i++)
        {
            bool on = i < homes.Count;
            doorLights[i].Visible = on;
            if (!on) continue;
            var h = homes[i];
            float solid = 1f - Mathf.Clamp(SeeThrough?.Invoke(h.Id) ?? 0f, 0f, 1f);
            float s = h.Strength * glow * (h.Shut ? 0.25f : 1f) * solid;
            doorLights[i].GlobalPosition = h.Door + new Vector3(0f, 0.9f, 0f);
            doorLights[i].LightEnergy = 1.4f * s * flicker;
        }

        // Smoke: roofs first, then open fires, nearest first, a few at most. Lit by
        // the air around it, so it is pale by day and dim blue-grey at night.
        int k = 0;
        foreach (var h in homes)
            if (h.Chimney && k < SmokeSlots) SetSmoke(smokes[k++], h.Roof, h.Strength, air);
        foreach (var f in openFires)
            if (k < SmokeSlots) SetSmoke(smokes[k++], f.At + new Vector3(0f, 0.9f, 0f), f.Strength * 0.6f, air);
        for (; k < SmokeSlots; k++)
        {
            smokes[k].Emitting = false;
            if (smokes[k].Visible && !smokes[k].Emitting) smokes[k].Visible = false;
        }
    }

    void SetSmoke(CpuParticles3D smoke, Vector3 at, float strength, Color air)
    {
        smoke.Visible = true;
        smoke.Emitting = strength > 0.05f;
        smoke.GlobalPosition = at;
        // Drift with the wind, harder in a gale.
        var drift = new Vector3(WindDirection.X, 0f, WindDirection.Y) * (0.2f + wind * 1.2f);
        smoke.Gravity = new Vector3(drift.X, 0.25f, drift.Z);
        smoke.Color = air.Lerp(new Color(0.75f, 0.75f, 0.78f), 0.5f) with { A = Mathf.Clamp(strength, 0.2f, 1f) };
    }

    // Which fires burn inside a finished home with a doorway, and which burn in the open.
    void FindHomes(Vector3 focus)
    {
        homes.Clear();
        openFires.Clear();
        if (homesConn == null) return;
        long now = LocalMicros() + clockOffsetMicros;
        foreach (var (_, f) in fires)
        {
            float left = (f.FuelUntil - now) / 1_000_000f;
            if (left <= 0f) continue;
            float dist = new Vector2(f.X - focus.X, f.Z - focus.Z).Length();
            if (dist > HomeReach) continue;
            float strength = Math.Clamp(left / 60f, 0.15f, 1f);
            float ground = terrain?.TileHeight(World.CellOf(f.X), World.CellOf(f.Z)) ?? World.BaseHeight(World.CellOf(f.X), World.CellOf(f.Z));
            Structure? home = null;
            foreach (var s in homesConn.Db.Structure.Iter())
                if (s.Complete && Structures.HasDoorway((int)s.Kind) && Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, f.X, f.Z))
                {
                    home = s;
                    break;
                }
            if (home is { } hs)
            {
                var shape = Structures.Shape((int)hs.Kind);
                var (dx, dz) = Structures.ToWorld(hs.X, hs.Z, hs.Yaw, 0f, shape.DoorwayZ + 0.15f);
                bool shut = homesConn.Db.StructureDoor.StructureId.Find(hs.Id) is { Closed: true };
                float doorGround = terrain?.TileHeight(World.CellOf(dx), World.CellOf(dz)) ?? ground;
                // Smoke leaves at the shape's smoke outlet (StructureShape.SmokeY, D-206); a shape
                // with none sends up no smoke.
                homes.Add((new Vector3(dx, doorGround, dz), hs.Yaw, shut, strength, new Vector3(hs.X, ground + shape.SmokeY, hs.Z), dist, hs.Id, shape.SmokeY > 0f));
            }
            else openFires.Add((new Vector3(f.X, ground, f.Z), strength, dist));
        }
        homes.Sort((a, b) => a.Dist.CompareTo(b.Dist));
        if (homes.Count > HomeSlots) homes.RemoveRange(HomeSlots, homes.Count - HomeSlots);
        openFires.Sort((a, b) => a.Dist.CompareTo(b.Dist));
    }
}
