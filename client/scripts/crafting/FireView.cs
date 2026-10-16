using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Draws the campfires the server knows about: a stone ring, crossed logs,
// flickering flames, drifting sparks and a warm light that dims as the fuel runs low.
public partial class FireView : Node3D
{
    readonly Dictionary<ulong, Campfire> fires = new();
    readonly Dictionary<ulong, RoastView> roasts = new();
    readonly Func<float, float, float> heightAt;

    public FireView(Func<float, float, float> heightAt) => this.heightAt = heightAt;

    public void Attach(DbConnection conn)
    {
        conn.Db.Fire.OnInsert += (_, f) => Upsert(f);
        conn.Db.Fire.OnUpdate += (_, _, f) => Upsert(f);
        conn.Db.Fire.OnDelete += (_, f) =>
        {
            if (fires.Remove(f.Id, out var node)) node.QueueFree();
        };
        conn.Db.Roast.OnInsert += (_, r) =>
        {
            if (roasts.ContainsKey(r.Id)) return;
            var node = new RoastView(r.PutAt.MicrosecondsSinceUnixEpoch)
            {
                Position = new Vector3(r.X, heightAt(r.X, r.Z) + 0.38f, r.Z),
                Rotation = new Vector3(0, Mathf.Atan2(r.X % 1f, r.Z % 1f), 0),
            };
            roasts[r.Id] = node;
            AddChild(node);
        };
        conn.Db.Roast.OnDelete += (_, r) =>
        {
            if (roasts.Remove(r.Id, out var node)) node.QueueFree();
        };
    }

    // The nearest fire within reach of a spot, if any.
    public Fire? NearestWithin(DbConnection conn, Vector3 at, float reach)
    {
        Fire? best = null;
        float bestDist = reach;
        foreach (var f in conn.Db.Fire.Iter())
        {
            float d = new Vector2(f.X - at.X, f.Z - at.Z).Length();
            if (d <= bestDist) { best = f; bestDist = d; }
        }
        return best;
    }

    // How long a fire burns before it needs more fuel, the way a player would say it:
    // "1 min left", "about 5 min left", "dying".
    public static string TimeLeft(Fire fire)
    {
        float left = (fire.FuelUntil.MicrosecondsSinceUnixEpoch - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000) / 1_000_000f;
        if (left < 30f) return "dying";
        int minutes = (int)MathF.Round(left / 60f);
        return minutes <= 1 ? "1 min left" : $"about {minutes} min left";
    }

    // A local-only fire, for checking the look in screenshots without a server.
    public void Preview(Vector3 at)
    {
        AddChild(new Campfire
        {
            Position = new Vector3(at.X, heightAt(at.X, at.Z), at.Z),
            FuelUntilMicros = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 + 600_000_000L,
        });
        // Two roasts, one nicely browned and one just put on.
        foreach (var (dx, dz, age) in new[] { (0.18f, 0.05f, 28f), (-0.12f, -0.16f, 4f) })
            AddChild(new RoastView(0) { FixedAge = age,
                Position = new Vector3(at.X + dx, heightAt(at.X, at.Z) + 0.38f, at.Z + dz),
                Rotation = new Vector3(0, dx * 9f, 0),
            });
        // The bow drill that lit it, set down beside the fire.
        float bx = at.X + 0.55f, bz = at.Z + 0.6f;
        AddChild(new MeshInstance3D { Mesh = ItemLook.MeshFor(Item.BowDrill),
            Position = new Vector3(bx, heightAt(bx, bz), bz), Rotation = new Vector3(0, 0.6f, 0) });
    }

    // A chunk of meat on a green stick over the coals, browning and then
    // blackening as it cooks (see Roasting for the timings).
    partial class RoastView : Node3D
    {
        static readonly Color Raw = new(0.78f, 0.36f, 0.34f);
        static readonly Color Cooked = new(0.50f, 0.27f, 0.12f);
        static readonly Color Charred = new(0.09f, 0.07f, 0.06f);
        readonly long putAtMicros;
        readonly ShaderMaterial meat = Look.Solid(Raw);

        // Previews hold one doneness instead of cooking in real time.
        public float? FixedAge;

        public RoastView(long putAtMicros) => this.putAtMicros = putAtMicros;

        public override void _Ready()
        {
            AddChild(new MeshInstance3D
            {
                Mesh = Look.Faceted(new CylinderMesh { TopRadius = 0.012f, BottomRadius = 0.015f, Height = 0.9f, RadialSegments = 5 }, Look.Bark.Lightened(0.3f)),
                Rotation = new Vector3(0, 0, Mathf.Pi / 2f - 0.5f),
                Position = new Vector3(0.18f, -0.1f, 0),
            });
            AddChild(new MeshInstance3D
            {
                Mesh = Look.Faceted(new SphereMesh { Radius = 0.13f, Height = 0.2f, RadialSegments = 8, Rings = 5 }, Raw),
                Scale = new Vector3(1.3f, 1f, 1f),
                MaterialOverride = meat,
            });
        }

        public override void _Process(double delta)
        {
            float t = FixedAge ?? (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 - putAtMicros) / 1_000_000f;
            var color = t < Roasting.BestSeconds
                ? Raw.Lerp(Cooked, Mathf.Clamp(t / Roasting.BestSeconds, 0f, 1f))
                : Cooked.Lerp(Charred, Mathf.Clamp((t - Roasting.BestSeconds) / (Roasting.BurnSeconds - Roasting.BestSeconds), 0f, 1f));
            meat.SetShaderParameter("albedo", color);
        }
    }

    void Upsert(Fire f)
    {
        if (!fires.TryGetValue(f.Id, out var node))
        {
            node = new Campfire { Position = new Vector3(f.X, heightAt(f.X, f.Z), f.Z) };
            fires[f.Id] = node;
            AddChild(node);
        }
        node.FuelUntilMicros = f.FuelUntil.MicrosecondsSinceUnixEpoch;
    }

    partial class Campfire : Node3D
    {
        public long FuelUntilMicros;
        readonly OmniLight3D light = new()
        {
            LightColor = new Color(1.0f, 0.58f, 0.24f),
            OmniRange = 7f,
            OmniAttenuation = 1.4f,
            ShadowEnabled = true,
            Position = new Vector3(0, 0.6f, 0),
            LightVolumetricFogEnergy = 2f,
        };
        readonly CpuParticles3D flames = Particles(26, 0.7f, new Color(1f, 0.62f, 0.2f), 0.32f, 0.9f, 1.6f);
        readonly CpuParticles3D sparks = Particles(10, 1.8f, new Color(1f, 0.75f, 0.35f), 0.05f, 1.6f, 2.8f);
        float time;

        public override void _Ready()
        {
            // A ring of stones around crossed logs.
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.Tau / 8f;
                AddChild(new MeshInstance3D
                {
                    Mesh = Stone,
                    Position = new Vector3(Mathf.Cos(a) * 0.45f, 0.04f, Mathf.Sin(a) * 0.45f),
                    Rotation = new Vector3(0, a * 1.7f, 0),
                });
            }
            for (int i = 0; i < 3; i++)
                AddChild(new MeshInstance3D
                {
                    Mesh = Log,
                    Position = new Vector3(0, 0.1f, 0),
                    Rotation = new Vector3(0.35f, i * Mathf.Tau / 3f, 0),
                });
            flames.Position = new Vector3(0, 0.12f, 0);
            sparks.Position = new Vector3(0, 0.25f, 0);
            AddChild(flames);
            AddChild(sparks);
            AddChild(light);
        }

        public override void _Process(double delta)
        {
            time += (float)delta;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
            float left = Math.Clamp((FuelUntilMicros - now) / 1_000_000f, 0f, Fires.MaxSeconds);
            // Full strength with a minute or more of fuel, fading to embers as it runs out.
            float strength = Math.Clamp(left / 60f, 0.15f, 1f);
            float flicker = 0.85f + 0.1f * Mathf.Sin(time * 13.1f) + 0.06f * Mathf.Sin(time * 23.7f + 1.3f);
            light.LightEnergy = 2.6f * strength * flicker;
            flames.ScaleAmountMax = 1.6f * strength;
            flames.Emitting = left > 0;
            sparks.Emitting = left > 2;
        }

        static Mesh? stone, log;
        static Texture2D? softDot;

        // A round, soft-edged blob, so each flame particle reads as a lick of fire, not a square.
        static Texture2D SoftDot => softDot ??= new GradientTexture2D
        {
            Width = 64,
            Height = 64,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(0.5f, 0f),
            Gradient = new Gradient
            {
                Offsets = new[] { 0f, 0.5f, 1f },
                Colors = new[] { new Color(1, 1, 1, 1), new Color(1, 1, 1, 0.45f), new Color(1, 1, 1, 0) },
            },
        };
        static Mesh Stone => stone ??= Look.Faceted(new SphereMesh { Radius = 0.12f, Height = 0.14f, RadialSegments = 6, Rings = 3 }, Look.Rock);
        static Mesh Log => log ??= Look.Faceted(new CylinderMesh { TopRadius = 0.05f, BottomRadius = 0.06f, Height = 0.75f, RadialSegments = 6 }, Look.Bark);

        static CpuParticles3D Particles(int amount, float lifetime, Color color, float size, float speedMin, float speedMax)
        {
            var material = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                BlendMode = BaseMaterial3D.BlendModeEnum.Add,
                BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                VertexColorUseAsAlbedo = true,
                AlbedoColor = color,
                AlbedoTexture = SoftDot,
            };
            var ramp = new Gradient();
            ramp.SetColor(0, new Color(1f, 0.9f, 0.55f, 0.9f));
            ramp.SetColor(1, new Color(0.9f, 0.25f, 0.05f, 0f));
            return new CpuParticles3D
            {
                Amount = amount,
                Lifetime = lifetime,
                Mesh = new QuadMesh { Size = Vector2.One * size, Material = material },
                Direction = Vector3.Up,
                Spread = 12f,
                Gravity = new Vector3(0, 0.6f, 0),
                InitialVelocityMin = speedMin,
                InitialVelocityMax = speedMax,
                EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
                EmissionSphereRadius = 0.12f,
                ScaleAmountMin = 0.5f,
                ScaleAmountMax = 1.6f,
                ColorRamp = ramp,
            };
        }
    }
}
