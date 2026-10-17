using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Smoke signals (atmosphere-32, from a playtester via Justin): a fire stoked past
// three quarters of the most fuel it can hold puts up a tall, thick, soft column of
// smoke, 30 m or more, that leans with the wind and spreads at the top, so you can
// find a friend's camp from a long way off. Below that it keeps the light thread of
// smoke from AtmosphereHomes. Between 70% and 80% full the column swells and thins
// smoothly instead of popping. Only fires in the open signal, never one inside a home
// (Justin); an outdoor raised hearth counts its longer burn (D-132).
//
// Up close the column is soft, lit puffs: warm and pale at the foot where the
// firelight catches them, grey and wider higher up, lit by the sun and the moon.
// Further than about 60 m it cross-fades to one painted, camera-facing column,
// which is cheap enough to read from as far as the camera sees.
//
// Command line: --smoke-signal=1 raises every fire's column to full, whatever its
// fuel. With no live fire nearby it raises one where --screenshot-fire puts its
// campfire, or 12 m ahead of the camera.
public partial class Atmosphere
{
    // At most this many columns at once, the nearest fires first.
    const int SignalSlots = 6;
    // Fires the client holds (about 160 m round the player) are judged here from their
    // fuel; further ones come from the server's world-wide SignalFire list, out to this.
    const float SignalReach = 320f, SignalFarReach = 1500f;
    // Puffs are drawn out to this far; beyond it only the painted column shows.
    const float SignalPuffReach = 110f;
    // Where the puffs hand over to the painted column, in metres from the camera.
    const float SignalFadeNear = 50f, SignalFadeFar = 75f;
    // How long a puff rises: with its speed this sets the column's height, about 30 to 35 m.
    const float SignalLife = 16f;

    sealed class SignalSlot
    {
        public CpuParticles3D Puffs = null!;
        public MeshInstance3D Column = null!;
        public ShaderMaterial ColumnMaterial = null!;
        public bool Used, Chimney;
        public ulong Fire;
        public Vector3 At;
        public float Target, Amount;
    }

    readonly SignalSlot[] signals = new SignalSlot[SignalSlots];
    readonly List<(ulong Id, Vector3 At, float Signal, float Dist, bool Chimney)> signalPicks = new();
    float signalsIn, signalForce, signalClock;
    Vector3? signalDemo;
    const ulong SignalDemoId = ulong.MaxValue;

    static readonly StringName NSignalAmount = "amount", NSignalLean = "lean", NSignalHeight = "height",
        NSignalGlow = "glow", NSignalSeed = "seed";

    void ReadySignals()
    {
        signalForce = Mathf.Clamp(ArgFloat("smoke-signal") ?? 0f, 0f, 1f);
        var puffMaterial = new StandardMaterial3D
        {
            // Lit, so the sun and moon shade it, softly: light wraps round each puff and
            // shows through it when the sun is behind the column.
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
            DiffuseMode = BaseMaterial3D.DiffuseModeEnum.LambertWrap,
            SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled,
            BacklightEnabled = true,
            Backlight = new Color(0.45f, 0.43f, 0.4f),
            Roughness = 1f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            DisableReceiveShadows = true,
            VertexColorUseAsAlbedo = true,
            AlbedoTexture = SignalPuff,
            // Puffs melt into roofs and the ground instead of cutting a hard line.
            ProximityFadeEnabled = true,
            ProximityFadeDistance = 1.5f,
        };
        var puffMesh = new QuadMesh { Size = new Vector2(10f, 10f), Material = puffMaterial };
        for (int i = 0; i < SignalSlots; i++)
        {
            var slot = new SignalSlot { Puffs = SignalPuffs(puffMesh) };
            slot.ColumnMaterial = new ShaderMaterial { Shader = SignalColumnShader };
            slot.ColumnMaterial.SetShaderParameter(NSignalSeed, i * 7.31f);
            slot.Column = new MeshInstance3D
            {
                // One tall strip, cut into rows so it can bend downwind.
                Mesh = new QuadMesh { Size = Vector2.One, CenterOffset = new Vector3(0f, 0.5f, 0f), SubdivideDepth = 16 },
                MaterialOverride = slot.ColumnMaterial,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                // The shader stretches the strip to full size, so tell culling how big it gets.
                CustomAabb = new Aabb(new Vector3(-45f, -2f, -45f), new Vector3(90f, 50f, 90f)),
                Visible = false,
            };
            signals[i] = slot;
            AddChild(slot.Puffs);
            AddChild(slot.Column);
        }
    }

    static CpuParticles3D SignalPuffs(Mesh mesh)
    {
        // Warm and pale low down, where the firelight catches it, then grey and fading high up.
        var ramp = new Gradient
        {
            Offsets = new[] { 0f, 0.06f, 0.3f, 0.7f, 1f },
            Colors = new[]
            {
                new Color(1f, 0.92f, 0.8f, 0f), new Color(0.96f, 0.89f, 0.79f, 0.5f), new Color(0.8f, 0.79f, 0.77f, 0.42f),
                new Color(0.68f, 0.68f, 0.7f, 0.28f), new Color(0.64f, 0.65f, 0.68f, 0f),
            },
        };
        // Small at the fire, spreading wide at the top.
        var scale = new Curve();
        scale.AddPoint(new Vector2(0f, 0.12f));
        scale.AddPoint(new Vector2(0.2f, 0.38f));
        scale.AddPoint(new Vector2(1f, 1f));
        return new CpuParticles3D
        {
            Amount = 40,
            Lifetime = SignalLife,
            // Starts with the column already up, so it never grows out of nothing on screen.
            Preprocess = SignalLife,
            Randomness = 0.3f,
            Emitting = false,
            LocalCoords = false,
            DrawOrder = CpuParticles3D.DrawOrderEnum.ViewDepth,
            Mesh = mesh,
            Direction = Vector3.Up,
            Spread = 6f,
            Gravity = new Vector3(0f, 0.06f, 0f),
            InitialVelocityMin = 2.3f,
            InitialVelocityMax = 2.9f,
            // Slows as it climbs, so it piles up and spreads instead of shooting off.
            DampingMin = 0.08f,
            DampingMax = 0.12f,
            AngleMin = -180f,
            AngleMax = 180f,
            AngularVelocityMin = -8f,
            AngularVelocityMax = 8f,
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 0.35f,
            ScaleAmountMin = 0.75f,
            ScaleAmountMax = 1.1f,
            ScaleAmountCurve = scale,
            ColorRamp = ramp,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            // Billboards turn past their bounds; a wide margin keeps the column from
            // blinking out when you stand under it and look up.
            ExtraCullMargin = 12f,
            Visible = false,
        };
    }

    void UpdateSignals(float dt, Vector3 focus, float nightNow)
    {
        signalClock += dt;
        signalsIn -= dt;
        if (signalsIn <= 0f)
        {
            signalsIn = 1f;
            FindSignals(focus);
            AssignSignals();
        }
        var eye = GetViewport()?.GetCamera3D()?.GlobalPosition ?? focus;
        // Puffs drift downwind, harder in a gale; the painted column leans the same way.
        var downwind = new Vector3(WindDirection.X, 0f, WindDirection.Y);
        float push = 0.04f + wind * 0.2f;
        var lean = downwind * (push * 0.5f * SignalLife * SignalLife * 0.7f);
        foreach (var s in signals)
        {
            if (!s.Used)
            {
                s.Puffs.Emitting = false;
                s.Puffs.Visible = false;
                s.Column.Visible = false;
                continue;
            }
            // Swell and thin over a few seconds, on top of the slow change of the fuel itself.
            s.Amount += (s.Target - s.Amount) * Mathf.Min(1f, dt / 3f);
            if (s.Target <= 0f && s.Amount < 0.01f)
            {
                s.Used = false;
                continue;
            }
            float d = new Vector2(s.At.X - eye.X, s.At.Z - eye.Z).Length();
            float near = 1f - Mathf.SmoothStep(SignalFadeNear, SignalFadeFar, d);
            float far = Mathf.SmoothStep(SignalFadeNear - 5f, SignalFadeFar - 5f, d);

            // Close up: the puffs. A thinner column rises a little less high.
            var p = s.Puffs;
            bool emit = d < SignalPuffReach;
            p.Gravity = new Vector3(downwind.X * push, 0.06f, downwind.Z * push);
            float swell = 0.6f + 0.4f * s.Amount;
            p.InitialVelocityMin = 2.3f * swell;
            p.InitialVelocityMax = 2.9f * swell;
            p.ScaleAmountMin = 0.75f * (0.55f + 0.45f * s.Amount);
            p.ScaleAmountMax = 1.1f * (0.55f + 0.45f * s.Amount);
            p.Color = new Color(1f, 1f, 1f, Mathf.Clamp(s.Amount * near, 0f, 1f));
            if (emit && !p.Emitting)
            {
                p.GlobalPosition = s.At;
                p.Restart();
            }
            p.Emitting = emit;
            p.Visible = emit && near > 0.002f;

            // Far off: the painted column, turned to face the camera.
            float far01 = s.Amount * far;
            s.Column.Visible = far01 > 0.003f;
            if (!s.Column.Visible) continue;
            s.Column.GlobalPosition = s.At;
            var toEye = new Vector2(eye.X - s.At.X, eye.Z - s.At.Z);
            s.Column.GlobalRotation = new Vector3(0f, Mathf.Atan2(toEye.X, toEye.Y), 0f);
            var m = s.ColumnMaterial;
            m.SetShaderParameter(NSignalAmount, far01);
            m.SetShaderParameter(NSignalLean, lean);
            m.SetShaderParameter(NSignalHeight, 30f + 6f * s.Amount);
            // At night an open fire lights the foot of its column a faint orange.
            m.SetShaderParameter(NSignalGlow, (s.Chimney ? 0.06f : 0.3f) * nightNow);
        }
    }

    // The most fuel this fire can hold (Crafting's FuelCapSeconds: the lighter's cap,
    // or a feeder's if higher, hearth bonus included), so anyone can signal by filling
    // their own fire (Justin). 0 marks a fire from before the cap was kept. The map is
    // AtmosphereSound's (HearFireCaps), shared with the fire's sound.
    float FireMost(ulong id) => fireCaps.TryGetValue(id, out var cap) && cap > 0f ? cap : Fires.MaxSeconds;

    // The stoked fires near the player, nearest first, a few at most.
    void FindSignals(Vector3 focus)
    {
        signalPicks.Clear();
        long now = LocalMicros() + clockOffsetMicros;
        foreach (var (id, f) in fires)
        {
            float left = (f.FuelUntil - now) / 1_000_000f;
            if (left <= 0f) continue;
            float dist = new Vector2(f.X - focus.X, f.Z - focus.Z).Length();
            if (dist > SignalReach) continue;
            // Only fires out in the open signal (Justin): none from inside a home.
            float most = FireMost(id);
            bool indoors = false;
            if (homesConn != null)
                foreach (var s in homesConn.Db.Structure.Iter())
                    if (s.Complete && Structures.HasDoorway((int)s.Kind) && Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, f.X, f.Z))
                    {
                        indoors = true;
                        break;
                    }
            if (indoors) continue;
            float signal = Math.Max(Mathf.SmoothStep(0.70f, 0.80f, left / most), signalForce);
            if (signal < 0.01f) continue;
            float ground = terrain?.TileHeight(World.CellOf(f.X), World.CellOf(f.Z)) ?? World.BaseHeight(World.CellOf(f.X), World.CellOf(f.Z));
            signalPicks.Add((id, new Vector3(f.X, ground + 0.9f, f.Z), signal, dist, false));
        }
        // Far fires the server says are signalling (SignalFire): full until their fuel
        // drops below the line, then fading over ten seconds.
        if (homesConn != null)
            foreach (var row in homesConn.Db.SignalFire.Iter())
            {
                if (fires.ContainsKey(row.FireId)) continue;
                float dist = new Vector2(row.X - focus.X, row.Z - focus.Z).Length();
                if (dist > SignalFarReach) continue;
                float left = (row.Until.MicrosecondsSinceUnixEpoch - now) / 1_000_000f;
                float signal = Math.Max(Mathf.Clamp(left / 10f, 0f, 1f), signalForce);
                if (signal < 0.01f) continue;
                float ground = terrain?.TileHeight(World.CellOf(row.X), World.CellOf(row.Z)) ?? World.BaseHeight(World.CellOf(row.X), World.CellOf(row.Z));
                signalPicks.Add((row.FireId, new Vector3(row.X, ground + 0.9f, row.Z), signal, dist, false));
            }
        // For screenshots with no live fire about: one column where the preview campfire goes.
        if (signalForce > 0f && signalPicks.Count == 0 && signalClock > 3f)
        {
            signalDemo ??= SignalDemoSpot(focus);
            signalPicks.Add((SignalDemoId, signalDemo.Value, signalForce, 0f, false));
        }
        signalPicks.Sort((a, b) => a.Dist.CompareTo(b.Dist));
        if (signalPicks.Count > SignalSlots) signalPicks.RemoveRange(SignalSlots, signalPicks.Count - SignalSlots);
    }

    // Keeps each column on its own fire, so a column never jumps from one camp to another;
    // a newly stoked fire takes a free slot, or waits for one to fade out.
    void AssignSignals()
    {
        foreach (var s in signals) s.Target = 0f;
        foreach (var pick in signalPicks)
        {
            SignalSlot? slot = null;
            foreach (var s in signals)
                if (s.Used && s.Fire == pick.Id) { slot = s; break; }
            if (slot == null)
            {
                foreach (var s in signals)
                    if (!s.Used) { slot = s; break; }
                if (slot == null) continue;
                slot.Used = true;
                slot.Fire = pick.Id;
                slot.Amount = 0f;
                slot.Puffs.Emitting = false;
            }
            slot.Target = pick.Signal;
            slot.At = pick.At;
            slot.Chimney = pick.Chimney;
            slot.Puffs.GlobalPosition = pick.At;
        }
    }

    // Where Main's --screenshot-fire sets its campfire, or else 12 m ahead of the camera.
    Vector3 SignalDemoSpot(Vector3 focus)
    {
        var back = GetViewport()?.GetCamera3D()?.GlobalBasis.Z ?? Vector3.Back;
        var ahead = -new Vector3(back.X, 0f, back.Z).Normalized();
        var at = Arg("screenshot-fire") is { } a && a != "cold"
            ? focus + ahead * 1.6f + new Vector3(1.2f, 0f, 0f)
            : focus + ahead * 12f;
        float ground = terrain?.TileHeight(World.CellOf(at.X), World.CellOf(at.Z)) ?? World.BaseHeight(World.CellOf(at.X), World.CellOf(at.Z));
        return new Vector3(at.X, ground + 0.9f, at.Z);
    }

    // A lumpy, soft puff: a few overlapping round lobes, a touch lighter on top, so each
    // particle reads as a painted cloud of smoke rather than a disc.
    static Texture2D? signalPuff;
    static Texture2D SignalPuff
    {
        get
        {
            if (signalPuff != null) return signalPuff;
            const int n = 64;
            var img = Image.CreateEmpty(n, n, false, Image.Format.Rgba8);
            var lobes = new (float X, float Y, float R)[]
            {
                (0.5f, 0.56f, 0.34f), (0.35f, 0.42f, 0.22f), (0.64f, 0.4f, 0.22f),
                (0.5f, 0.3f, 0.2f), (0.3f, 0.62f, 0.2f), (0.7f, 0.64f, 0.2f),
            };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n, a = 0f;
                    foreach (var (lx, ly, lr) in lobes)
                    {
                        float d = new Vector2(u - lx, v - ly).Length() / lr;
                        if (d < 1f) a += (1f - d) * (1f - d) * 0.8f;
                    }
                    // Never reach the square's edge, so no puff shows a straight line.
                    float r = new Vector2(u - 0.5f, v - 0.5f).Length();
                    a = Mathf.Min(1f, a) * Mathf.Clamp((0.5f - r) / 0.14f, 0f, 1f);
                    float c = 0.86f + 0.14f * (1f - v);
                    img.SetPixel(x, y, new Color(c, c, c, a));
                }
            img.GenerateMipmaps();
            return signalPuff = ImageTexture.CreateFromImage(img);
        }
    }

    // The far column: a strip as tall as the puffs climb, narrow at the fire and wide at
    // the top, bending downwind. Soft noise rises through it so it seems to billow, and
    // it is lit like the puffs, by the sun and moon, wrapping round and glowing through.
    static readonly Shader SignalColumnShader = new()
    {
        Code = """
            shader_type spatial;
            render_mode blend_mix, depth_draw_never, cull_disabled, diffuse_lambert_wrap, specular_disabled, shadows_disabled;

            uniform float amount = 0.0;
            uniform float height = 32.0;
            uniform float base_width = 1.6;
            uniform float top_width = 14.0;
            uniform vec3 lean = vec3(0.0);
            uniform float glow = 0.0;
            uniform float seed = 0.0;
            uniform vec3 base_tint : source_color = vec3(0.95, 0.89, 0.8);
            uniform vec3 top_tint : source_color = vec3(0.64, 0.65, 0.68);

            varying float up;

            float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
            float noise(vec2 p) {
                vec2 i = floor(p);
                vec2 f = fract(p);
                f = f * f * (3.0 - 2.0 * f);
                return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
                           mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
            }

            void vertex() {
                // The strip comes in 0..1 tall; stretch it to the column and bend the top downwind.
                up = VERTEX.y;
                float w = mix(base_width, top_width, pow(up, 0.8));
                vec3 bend = transpose(mat3(MODEL_MATRIX)) * lean * pow(up, 1.7);
                VERTEX = vec3(VERTEX.x * w, up * height, 0.0) + bend;
            }

            void fragment() {
                float x = UV.x * 2.0 - 1.0;
                // Billows drift up the column; two sizes of noise so it isn't one even smear.
                vec2 p = vec2(x * 1.3 + seed, (up - TIME * 0.05) * 7.0 + seed);
                float n = noise(p) * 0.65 + noise(p * 2.3 + vec2(7.0, 3.0)) * 0.35;
                float edge = 1.0 - smoothstep(0.3 + 0.35 * n, 1.0, abs(x + (n - 0.5) * 0.35));
                float foot = smoothstep(0.0, 0.06, up);
                float crown = 1.0 - smoothstep(0.55, 1.0, up);
                float dens = edge * foot * crown * (0.55 + 0.45 * n) * mix(0.95, 0.55, up);
                ALBEDO = mix(base_tint, top_tint, smoothstep(0.0, 0.45, up));
                ALPHA = clamp(dens * amount, 0.0, 1.0);
                ROUGHNESS = 1.0;
                BACKLIGHT = vec3(0.45, 0.43, 0.4);
                EMISSION = vec3(1.0, 0.55, 0.25) * glow * (1.0 - smoothstep(0.0, 0.12, up));
            }
            """,
    };
}
