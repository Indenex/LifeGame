using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Wading and floating, seen (atmosphere-26). Since bugfix-32 you can walk into ponds
// and float in deep water; now the water answers: a small splash throws up droplets as
// you step or jump in, and your legs come out darker and a little glossy up to the
// waterline, drying off over about half a minute. The ripples round a wader are the
// water's own now (WaterRipples, atmosphere-31), not painted rings.
//
// Other players in the water get the same splash (atmosphere-27), but stay dry-legged:
// the wet band is one global, so it can only follow you.
//
// The wet band is drawn by the painted shader (which the player figure uses) from two
// globals: wet_line, the world height it reaches, and wet_amount, how wet (0 to 1).
public partial class Atmosphere
{
    static readonly StringName NWetLine = "wet_line", NWetAmount = "wet_amount";

    // How long legs take to dry after leaving the water, in seconds.
    const float DryTime = 30f;

    // Other players' figures, for their splashes and ripples (Main hands these over).
    public System.Func<System.Collections.Generic.IEnumerable<Node3D>>? OtherFigures;

    // Others only splash this close, and at most this many at once.
    const float OthersReach = 60f;
    const int OthersMax = 12;

    readonly CpuParticles3D splash = NewSplash();

    // The splash following one other player, with what it needs from frame to frame.
    sealed class OtherWater
    {
        public readonly CpuParticles3D Splash = NewSplash();
        public Vector3 LastPos;
        public bool InWater, Seen;
    }
    readonly System.Collections.Generic.Dictionary<Node3D, OtherWater> othersWater = new();

    static CpuParticles3D NewSplash() => new()
    {
        Emitting = false,
        OneShot = true,
        Amount = 28,
        Lifetime = 0.9f,
        Explosiveness = 0.92f,
        Direction = Vector3.Up,
        Spread = 38f,
        InitialVelocityMin = 1.6f,
        InitialVelocityMax = 3.2f,
        Gravity = new Vector3(0f, -9.8f, 0f),
        EmissionShape = CpuParticles3D.EmissionShapeEnum.Ring,
        EmissionRingAxis = Vector3.Up,
        EmissionRingRadius = 0.35f,
        EmissionRingInnerRadius = 0.15f,
        EmissionRingHeight = 0.02f,
        ScaleAmountMin = 0.6f,
        ScaleAmountMax = 1.3f,
        TopLevel = true,
        Mesh = new SphereMesh
        {
            Radius = 0.035f,
            Height = 0.07f,
            RadialSegments = 6,
            Rings = 3,
            // Lit like everything else, so the drops don't glow white at night (env-38).
            Material = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.88f, 0.94f, 0.96f, 0.85f),
                Roughness = 0.6f,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            },
        },
    };

    bool wasInWater;
    float wetOffset, wetAmount;
    Vector3 waterLastPos;

    void ReadyWater()
    {
        AddChild(splash);
    }

    void UpdateWater(float dt, Vector3 focus, Color air)
    {
        if (Player == null || !Player.IsInsideTree() || terrain == null) return;
        UpdateOthersWater(dt, focus);
        var at = Player.GlobalPosition;
        float speed = dt > 0f ? new Vector2(at.X - waterLastPos.X, at.Z - waterLastPos.Z).Length() / dt : 0f;
        waterLastPos = at;

        float bed = terrain.HeightAt(at.X, at.Z);
        float depth = Wading.Depth(bed);
        // In the water when the surface is above the feet (not while hopping clear of it).
        bool inWater = depth > 0.03f && at.Y < World.WaterLevel - 0.02f;

        // Stepping or dropping in throws up a splash and a ring of waves.
        if (inWater && !wasInWater)
        {
            splash.GlobalPosition = new Vector3(at.X, World.WaterLevel + 0.02f, at.Z);
            splash.Restart();
            waterRipples.Disturb(at, 0.5f, 1f);
        }
        wasInWater = inWater;
        // Moving through it leaves a wake; standing still, a gentle stir.
        if (inWater) waterRipples.Disturb(at, 0.35f, Mathf.Clamp(0.08f + speed * 0.12f, 0f, 0.5f) * dt * 8f);

        // Legs are wet up to where the water reached on them, and dry off slowly.
        if (inWater)
        {
            wetOffset = Mathf.Clamp(Mathf.Max(wetOffset * (wetAmount > 0.6f ? 1f : 0f), World.WaterLevel - at.Y), 0f, 1.4f);
            wetAmount = 1f;
        }
        else wetAmount = Mathf.Max(0f, wetAmount - dt / DryTime);
        RenderingServer.GlobalShaderParameterSet(NWetLine, at.Y + wetOffset);
        RenderingServer.GlobalShaderParameterSet(NWetAmount, wetAmount);
    }

    void UpdateOthersWater(float dt, Vector3 focus)
    {
        foreach (var w in othersWater.Values) w.Seen = false;
        if (OtherFigures?.Invoke() is { } figures)
        {
            int shown = 0;
            foreach (var figure in figures)
            {
                if (!GodotObject.IsInstanceValid(figure) || !figure.IsInsideTree() || !figure.Visible) continue;
                var at = figure.GlobalPosition;
                if (at.DistanceSquaredTo(focus) > OthersReach * OthersReach) continue;
                if (!othersWater.TryGetValue(figure, out var w))
                {
                    if (shown >= OthersMax) continue;
                    w = new OtherWater { LastPos = at };
                    AddChild(w.Splash);
                    othersWater[figure] = w;
                }
                w.Seen = true;
                shown++;
                float speed = dt > 0f ? new Vector2(at.X - w.LastPos.X, at.Z - w.LastPos.Z).Length() / dt : 0f;
                w.LastPos = at;
                bool inWater = Wading.Depth(terrain!.HeightAt(at.X, at.Z)) > 0.03f && at.Y < World.WaterLevel - 0.02f;
                if (inWater && !w.InWater)
                {
                    w.Splash.GlobalPosition = new Vector3(at.X, World.WaterLevel + 0.02f, at.Z);
                    w.Splash.Restart();
                    waterRipples.Disturb(at, 0.5f, 1f);
                }
                w.InWater = inWater;
                // Remote positions arrive in steps, so their speed is rougher; ease it.
                if (inWater) waterRipples.Disturb(at, 0.35f, Mathf.Clamp(0.08f + speed * 0.1f, 0f, 0.5f) * dt * 8f);
            }
        }
        // Drop the ones whose player left, went out of reach or out of sight.
        System.Collections.Generic.List<Node3D>? gone = null;
        foreach (var (figure, w) in othersWater)
            if (!w.Seen) (gone ??= new()).Add(figure);
        if (gone == null) return;
        foreach (var figure in gone)
        {
            othersWater[figure].Splash.QueueFree();
            othersWater.Remove(figure);
        }
    }
}
