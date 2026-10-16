using System;
using System.Collections.Generic;

namespace LifeGame;

// What the player's hands are working at, which picks the sound of it.
public enum Work { None, Chop, Knap, HandDrill, BowDrill, Clay }

// Work sounds: the player's own work heard close by, and trees falling wherever
// they fall. Everything is built from noise and a few resonances like the rest of
// the soundscape, eases in and out, and never starts with a click.
public sealed partial class SoundMix
{
    readonly WorkVoice work;
    readonly Felling felling;

    // Start working at something for about this many seconds, or stop (Work.None).
    public void Working(Work kind, float seconds) => work.Start(kind, seconds);

    // A tree starts to fall: gain from distance (0 to 1), pan from -1 (left) to 1
    // (right), near from 0 (far off, dulled by the air) to 1 (close by).
    public void Fell(float gain, float pan, float near) => felling.Trigger(gain, pan, near);

    // How a fall d metres away is heard: its gain, and how near it sounds. A tree
    // crashing down a few steps away is the loudest thing in the woods without
    // startling; one 40 m off is a distant thump through the trees.
    public static float FellGain(float d) => 1f / (1f + MathF.Pow(d / 6f, 1.3f));
    public static float FellNear(float d) => 1f - Math.Clamp(d / 50f, 0f, 1f);

    // Which work sound a crafting process makes, from its id.
    public static Work WorkFor(string processId) => processId switch
    {
        _ when processId.StartsWith("chop.") => Work.Chop,
        _ when processId.StartsWith("knap.") || processId is "clay.grit" or "clay.grog" => Work.Knap,
        "fire.hand_drill" => Work.HandDrill,
        "fire.bow_drill" => Work.BowDrill,
        _ when processId.StartsWith("clay.") => Work.Clay,
        _ => Work.None,
    };

    // --- The player's work ------------------------------------------------------

    sealed class WorkVoice
    {
        readonly int rate;
        readonly Noise noise;
        readonly Random rng;
        readonly List<Hit> hits = new();
        Work kind;
        bool on;
        float seconds, elapsed, next, level;
        int swings, breather;
        // Drilling: the stroke, its heat, and the grit and squeak of wood on wood.
        float stroke, grit, rubbing = 1f;
        BandPass rub, gritBand, squeak, breath;
        Lowpass crackle;

        sealed class Hit
        {
            public Work Kind;
            public bool Light, Flake, Pat;
            public float Age, Gain, Pitch, Pan, Phase, Phase2, Phase3;
            public float Chip1, Chip2, Chip3, Pull;
            public BandPass Band, Band2;
            public Lowpass Low;
        }

        public WorkVoice(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        public void Start(Work kind, float seconds)
        {
            if (kind == Work.None) { on = false; return; }
            if (!on || kind != this.kind)
            {
                elapsed = 0f;
                // A moment to reach in before the first stroke.
                next = 0.35f;
                swings = 0;
                breather = rng.Next(4, 7);
            }
            this.kind = kind;
            this.seconds = Math.Max(seconds, 0.5f);
            on = true;
        }

        public void Render(float[] l, float[] r, int n)
        {
            float dt = 1f / rate;
            float ease = 1f - MathF.Exp(-dt / (on ? 0.25f : 0.5f));
            bool drilling = kind is Work.HandDrill or Work.BowDrill;
            for (int i = 0; i < n; i++)
            {
                level += ease * ((on ? 1f : 0f) - level);
                if (on)
                {
                    elapsed += dt;
                    next -= dt;
                    if (next <= 0f && !drilling) Strike();
                }
                if (drilling && level > 0.0005f)
                {
                    float s = Drill(dt) * level * 0.75f;
                    l[i] += s;
                    r[i] += s;
                }
            }

            for (int k = hits.Count - 1; k >= 0; k--)
            {
                var hit = hits[k];
                float pl = Pan(hit.Pan, true), pr = Pan(hit.Pan, false);
                for (int i = 0; i < n; i++)
                {
                    float s = hit.Kind switch
                    {
                        Work.Chop => Chop(hit, dt) * 0.7f,
                        Work.Knap => Knap(hit, dt) * 0.45f,
                        _ => Clay(hit, dt),
                    } * hit.Gain;
                    l[i] += s * pl;
                    r[i] += s * pr;
                    hit.Age += dt;
                }
                if (hit.Age > 0.7f) hits.RemoveAt(k);
            }
        }

        // The next stroke: an axe swing, a strike of stone on stone, a press of clay.
        void Strike()
        {
            if (hits.Count > 8) hits.RemoveAt(0);
            var hit = new Hit
            {
                Kind = kind,
                Gain = Range(rng, 0.85f, 1.1f),
                Pitch = Range(rng, 0.9f, 1.1f),
                Pan = Range(rng, -0.1f, 0.1f),
                Chip1 = Range(rng, 0.03f, 0.08f),
                Chip2 = Range(rng, 0.08f, 0.16f),
                Chip3 = Range(rng, 0.14f, 0.26f),
                Pull = Range(rng, 0.09f, 0.16f),
            };
            switch (kind)
            {
                case Work.Chop:
                    // The swing comes first; the blade lands 0.2 s into the sound.
                    hit.Age = -0.2f;
                    next = Range(rng, 1.15f, 1.4f);
                    // Now and then a breath and a fresh grip.
                    if (++swings >= breather) { swings = 0; breather = rng.Next(4, 7); next += Range(rng, 0.9f, 1.5f); }
                    break;
                case Work.Knap:
                    hit.Flake = rng.NextDouble() < 0.45;
                    next = Range(rng, 0.8f, 1.5f);
                    // Sometimes a light tap to set the stone before the real blow.
                    if (rng.NextDouble() < 0.25)
                        hits.Add(new Hit { Kind = kind, Light = true, Gain = 0.35f, Pitch = hit.Pitch * 1.08f, Pan = hit.Pan, Age = -0.28f });
                    break;
                default:
                    hit.Pat = rng.NextDouble() < 0.3;
                    next = Range(rng, 0.55f, 1.1f);
                    break;
            }
            hits.Add(hit);
        }

        // Axe on wood: a short whoosh, then a hollow thunk with a bright bite of the
        // blade, and a few chips of wood ticking away.
        float Chop(Hit h, float dt)
        {
            float t = h.Age, x = noise.Next();
            if (t < 0f)
            {
                float w = -t / 0.2f;
                float env = MathF.Sin(MathF.PI * (1f - w)) * (1f - w);
                return h.Band2.Process(x, (600f + 900f * (1f - w)) * h.Pitch, 0.8f, rate) * env * 0.04f;
            }
            float attack = Math.Min(1f, t / 0.002f);
            h.Phase += 2f * MathF.PI * 150f * h.Pitch * (0.85f + 0.15f * MathF.Exp(-t / 0.02f)) * dt;
            h.Phase2 += 2f * MathF.PI * 410f * h.Pitch * dt;
            h.Phase3 += 2f * MathF.PI * 72f * h.Pitch * dt;
            float body = MathF.Sin(h.Phase) * MathF.Exp(-t / 0.07f) * 0.25f
                + MathF.Sin(h.Phase2) * MathF.Exp(-t / 0.025f) * 0.25f
                + MathF.Sin(h.Phase3) * MathF.Exp(-t / 0.05f) * 0.1f;
            // The bite of the blade and the dry tock of the wood around it.
            float bite = h.Band.Process(x, 1500f * h.Pitch, 0.6f, rate) * MathF.Exp(-t / 0.01f) * 1.6f
                + h.Low.Process(x, Lowpass.Coef(900f, rate)) * MathF.Exp(-t / 0.02f) * 1.2f;
            float chips = Tick(t, h.Chip1, 0.004f) + 0.7f * Tick(t, h.Chip2, 0.004f) + 0.45f * Tick(t, h.Chip3, 0.003f);
            float chip = chips > 0f ? h.Band2.Process(x, 4200f * h.Pitch, 0.4f, rate) * chips * 1.0f : 0f;
            return (body + bite) * attack + chip;
        }

        // Stone on stone: a sharp click with a short glassy ring, a dull knock in
        // the hand, and sometimes a flake skittering off.
        float Knap(Hit h, float dt)
        {
            float t = h.Age, x = noise.Next();
            if (t < 0f) return 0f;
            float attack = Math.Min(1f, t / 0.001f);
            h.Phase += 2f * MathF.PI * 2600f * h.Pitch * dt;
            h.Phase2 += 2f * MathF.PI * 4100f * h.Pitch * dt;
            h.Phase3 += 2f * MathF.PI * 320f * h.Pitch * dt;
            float click = h.Band.Process(x, 3200f * h.Pitch, 0.5f, rate) * MathF.Exp(-t / 0.005f) * 1.4f;
            float ring = MathF.Sin(h.Phase) * MathF.Exp(-t / 0.03f) * 0.1f + MathF.Sin(h.Phase2) * MathF.Exp(-t / 0.015f) * 0.05f;
            float knock = MathF.Sin(h.Phase3) * MathF.Exp(-t / 0.02f) * 0.22f;
            float s = (click + ring + knock) * attack;
            if (h.Flake)
            {
                float ticks = Tick(t, h.Chip2, 0.003f) + 0.6f * Tick(t, h.Chip3, 0.003f);
                if (ticks > 0f) s += h.Band2.Process(x, 5200f * h.Pitch, 0.4f, rate) * ticks * 0.35f;
            }
            return s;
        }

        // Wet clay: a soft squelch as the hands press, the suck of it letting go,
        // and now and then a flat pat to firm it.
        float Clay(Hit h, float dt)
        {
            float t = h.Age, x = noise.Next();
            if (h.Pat)
            {
                float a = Math.Min(1f, t / 0.002f);
                h.Phase += 2f * MathF.PI * 130f * h.Pitch * dt;
                return (h.Band.Process(x, 1200f * h.Pitch, 0.7f, rate) * MathF.Exp(-t / 0.02f) * 0.8f
                    + MathF.Sin(h.Phase) * MathF.Exp(-t / 0.04f) * 0.25f) * a;
            }
            float env = (1f - MathF.Exp(-t / 0.015f)) * MathF.Exp(-t / 0.12f);
            float press = h.Low.Process(x, Lowpass.Coef(700f * h.Pitch, rate)) * env * 1.6f;
            float u = t - h.Pull;
            float suck = u > 0f
                ? h.Band.Process(x, Math.Min(600f + 6000f * u, 2600f) * h.Pitch, 0.25f, rate) * MathF.Exp(-u / 0.05f) * 0.3f * Math.Min(1f, u / 0.01f)
                : 0f;
            return press + suck;
        }

        // Wood twisting on wood: a dry rasp swelling with each stroke, grittier and
        // brighter as it heats, then the ember nursed with a couple of soft breaths.
        float Drill(float dt)
        {
            float x = noise.Next();
            float progress = elapsed / seconds;
            bool bow = kind == Work.BowDrill;
            // Friction for most of the work, then blowing the ember into flame.
            // (Once the work stops it just fades out as it was.)
            if (on) rubbing = 1f - Smooth(0.5f, 0.6f, progress);
            float heat = Math.Min(1f, elapsed / (seconds * 0.5f));
            stroke += 2f * MathF.PI * (bow ? 0.8f : 1.1f) * dt;
            float speed = MathF.Abs(MathF.Sin(stroke));
            float s = rub.Process(x, (bow ? 900f + 500f * heat : 500f + 600f * heat) + 300f * speed, 0.5f, rate)
                * speed * MathF.Sqrt(speed) * 0.5f * rubbing;
            if (rng.NextDouble() < speed * 40f * dt) grit = 1f;
            grit *= MathF.Exp(-dt / 0.003f);
            s += gritBand.Process(x, 2800f, 0.5f, rate) * grit * (0.05f + 0.15f * heat) * rubbing;
            if (bow) s += squeak.Process(x, 1900f + 150f * speed, 0.05f, rate) * speed * heat * 0.02f * rubbing;
            if (on && progress > 0.5f)
            {
                // Two slow breaths over the ember, and the tinder starting to crackle.
                float b = (progress - 0.55f) * seconds;
                float puff = b > 0f ? MathF.Pow(MathF.Max(0f, MathF.Sin(MathF.PI * b / 0.9f)), 2f) * (b < 1.8f ? 1f : 0f) : 0f;
                s += breath.Process(x, 900f, 1.2f, rate) * puff * 0.3f;
                if (rng.NextDouble() < Smooth(0.6f, 1f, progress) * 25f * dt) grit = Math.Max(grit, 0.8f);
                s += crackle.Process(x, Lowpass.Coef(5000f, rate)) * grit * 0.25f * Smooth(0.6f, 1f, progress);
            }
            return s;
        }

        static float Tick(float t, float at, float decay) => t > at ? MathF.Exp(-(t - at) / decay) * Math.Min(1f, (t - at) / 0.0005f) : 0f;
    }

    // --- A tree falling ---------------------------------------------------------

    // Timed to the toppling animation (Look's tree shader): the trunk creaks and its
    // fibres crack as it starts to lean, the crown rushes through the air faster and
    // faster, lands with a heavy thud and a crunch of branches, and the leaves settle.
    sealed class Felling
    {
        readonly int rate;
        readonly Noise noise;
        readonly Random rng;
        readonly List<Fall> active = new();
        // When the crown hits the ground, seconds after it starts to lean.
        const float Lands = 1.55f;

        sealed class Fall
        {
            public float Age, Gain, Pan, Near, Pitch, Seed;
            public float Slip, Crack, Twig, Boom, Body;
            public BandPass Grain1, Grain2, Snap, Swish, Leaves, Crunch, Settle;
            public Lowpass Dirt, Dull;
        }

        public Felling(int rate, Noise noise, Random rng)
        {
            this.rate = rate;
            this.noise = noise;
            this.rng = rng;
        }

        public void Trigger(float gain, float pan, float near)
        {
            if (gain < 0.01f) return;
            if (active.Count >= 3) active.RemoveAt(0);
            active.Add(new Fall
            {
                Gain = gain,
                Pan = Math.Clamp(pan, -1f, 1f),
                Near = Math.Clamp(near, 0f, 1f),
                Pitch = Range(rng, 0.85f, 1.15f),
                Seed = Range(rng, 0f, 6.28f),
            });
        }

        public void Render(float[] l, float[] r, float[] sl, float[] sr, int n)
        {
            float dt = 1f / rate;
            for (int k = active.Count - 1; k >= 0; k--)
            {
                var f = active[k];
                float pl = Pan(f.Pan, true), pr = Pan(f.Pan, false);
                // Far off, the air takes the top off it and the thud carries.
                float dull = Lowpass.Coef(1500f + 10000f * f.Near * f.Near, rate);
                float send = 0.25f + 0.35f * (1f - f.Near);
                for (int i = 0; i < n; i++)
                {
                    float s = f.Dull.Process(Sample(f, dt), dull) * f.Gain * 0.32f;
                    l[i] += s * pl;
                    r[i] += s * pr;
                    sl[i] += s * send * pl;
                    sr[i] += s * send * pr;
                    f.Age += dt;
                }
                if (f.Age > 4.5f) active.RemoveAt(k);
            }
        }

        float Sample(Fall f, float dt)
        {
            float t = f.Age, x = noise.Next(), s = 0f;

            // Creak: the trunk's fibres sticking and slipping, through two wood
            // resonances, the rate wandering as it bends.
            if (t < 1.4f)
            {
                float env = Smooth(0f, 0.3f, t) * (1f - Smooth(1.0f, 1.4f, t));
                float slipRate = (28f + 18f * MathF.Sin(t * 3.1f + f.Seed) + 22f * t) * f.Pitch;
                f.Slip += slipRate * dt;
                float pulse = 0f;
                if (f.Slip >= 1f) { f.Slip -= 1f; pulse = Range(rng, 0.6f, 1f); }
                float grain = f.Grain1.Process(pulse, 340f * f.Pitch, 0.08f, rate) + 0.6f * f.Grain2.Process(pulse, 920f * f.Pitch, 0.12f, rate);
                s += grain * env * 7f;
                // Fibres snapping as the cut gives.
                if (rng.NextDouble() < 7f * Smooth(0.35f, 1.1f, t) * dt) f.Crack = Range(rng, 0.5f, 1f);
            }
            f.Crack *= MathF.Exp(-dt / 0.006f);
            s += f.Snap.Process(x, 1800f * f.Pitch, 0.6f, rate) * f.Crack * 1.1f;

            // Rush: the crown swinging through the air, fastest just before it lands.
            if (t > 0.5f && t < Lands + 0.03f)
            {
                float speed = MathF.Pow((t - 0.5f) / (Lands - 0.5f), 1.5f);
                float end = Math.Clamp((Lands + 0.03f - t) / 0.03f, 0f, 1f);
                s += (f.Swish.Process(x, 500f + 1800f * speed, 0.7f, rate) * 1.1f
                    + f.Leaves.Process(x, 4500f, 0.8f, rate) * 0.5f) * speed * speed * end;
            }

            // Thud: a deep boom in the ground, the trunk's own thump, a burst of
            // earth, and branches crunching as the crown folds.
            float u = t - Lands;
            if (u > 0f)
            {
                float attack = Math.Min(1f, u / 0.006f);
                f.Boom += 2f * MathF.PI * 52f * f.Pitch * (1f + 0.4f * MathF.Exp(-u / 0.05f)) * dt;
                f.Body += 2f * MathF.PI * 105f * f.Pitch * dt;
                float thud = MathF.Sin(f.Boom) * MathF.Exp(-u / 0.3f) * 0.9f
                    + MathF.Sin(f.Body) * MathF.Exp(-u / 0.12f) * 0.4f
                    + f.Dirt.Process(x, Lowpass.Coef(350f, rate)) * MathF.Exp(-u / 0.1f) * 3f;
                float crunch = MathF.Exp(-u / 0.05f) + 0.6f * (u > 0.07f ? MathF.Exp(-(u - 0.07f) / 0.04f) : 0f)
                    + 0.4f * (u > 0.16f ? MathF.Exp(-(u - 0.16f) / 0.05f) : 0f);
                s += thud * attack + f.Crunch.Process(x, 1400f * f.Pitch, 0.7f, rate) * crunch * 0.45f * attack;

                // Settling: leaves hissing to rest and a few last twigs snapping.
                float settle = Smooth(0f, 0.15f, u) * MathF.Exp(-u / 0.9f);
                s += f.Settle.Process(x, 3800f, 0.9f, rate) * settle * 0.3f;
                if (rng.NextDouble() < 10f * MathF.Exp(-u / 0.8f) * dt) f.Twig = Range(rng, 0.3f, 0.8f);
            }
            f.Twig *= MathF.Exp(-dt / 0.003f);
            s += f.Leaves.Process(x, 3000f, 0.5f, rate) * f.Twig * 0.3f;
            return s;
        }
    }
}
