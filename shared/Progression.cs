// One progression formula for every skill (skills-12, Justin: "I don't want each
// skill to have its own formulas"). A skill rank (0 to 100) turns into an effect
// through the single curve below; each effect only declares where it starts and
// where Master ends. Skills.Gain is the matching single formula for how fast rank
// is earned. Keep this file free of engine and SpacetimeDB types.
using System;
using System.Collections.Generic;

namespace LifeGame.Shared
{
    // What a rank is worth. Add a kind here and a row in Progression.Effects; do not
    // write a new rank formula elsewhere.
    public enum EffectKind
    {
        QualityCap,       // best quality a player can make, 1 to 100
        WorkTime,         // multiplier on how long a process takes
        MinigameSteps,    // multiplier on minigame steps (Master skips the minigame)
        DugClayBonus,     // quality added to dug clay
        SecondLumpChance, // chance of a second lump of clay, 0 to 1
        FireSeconds,      // longest a fire can be fed in one go (Firecraft)
        RunSeconds,       // seconds of running from a full bar (Endurance)
        RefillSeconds,    // seconds to refill an empty bar (Endurance)
        SneakDistance,    // meters a sneaking player can get to a wary animal before it bolts (Stalking)
        SneakCritChance,  // chance a sneak attack crits, 0 to 1 (Stalking)
        SneakCritBonus,   // extra damage on a sneak crit, as a multiple of normal damage (Stalking)
        ArcheryDamage,    // damage multiplier for arrows (Archery)
        ArcheryHitChance, // multiplier on the chance an arrow hits (Archery)
        ArcheryDrawTime,  // seconds between shots (Archery)
        WadeSpeed,        // fraction of walking speed left at float depth (Swimming)
        FloatSpeed,       // fraction of walking speed once afloat (Swimming)
    }

    // How an effect's value moves between its start and Master values.
    public enum EffectShape
    {
        // The one front-loaded curve (the default for everything).
        Curve,
        // A deliberate exception for combat damage multipliers (Animals, Justin): the
        // multiplier's reciprocal is linear in rank, so the number of hits needed to kill
        // falls evenly with rank (one fewer hit per equal step of ranks).
        EvenInHits,
    }

    public sealed class EffectDef
    {
        public readonly EffectKind Kind;
        public readonly string Name, Unit;
        // Value at rank 0 and at Master. Either can be the larger one.
        public readonly float Lo, Hi;
        public readonly EffectShape Shape;

        public EffectDef(EffectKind kind, string name, float lo, float hi, string unit, EffectShape shape = EffectShape.Curve)
        {
            Shape = shape;
            Kind = kind;
            Name = name;
            Lo = lo;
            Hi = hi;
            Unit = unit;
        }
    }

    public static class Progression
    {
        // Front-loaded (D-075, Justin's pick): the first 15% of ranks give about 40%
        // of the benefit, then a long slow climb. This is curve family B in
        // docs/PROGRESSION.md: ln(1 + b p) / ln(1 + b) with b = 12.
        public const float CurveB = 12f;

        // Global leveling pace, applied to every point gain in Skills.Gain. 1 is today's
        // fast testing pace (Master after about 800 uses). Later this is lowered many-fold
        // toward OSRS / Retro Dofus-like leveling (Justin): 0.05 is about 20x slower. Each
        // gain is at least 1 point (a hundredth of a rank), so below about 0.02 raise
        // Skills.PointsPerRank as well.
        public const float Pace = 1f;

        // Combat skills level slower (Justin): their pace is this fraction of the base Pace.
        public const float SlowPaceFactor = 0.5f;

        // Skills whose gains use SlowPaceFactor (combat skills and swimming); every other skill
        // uses the base Pace.
        public static bool IsCombat(string specId) => specId == "stalking" || specId == "archery";
        public static bool IsSlow(string specId) => IsCombat(specId) || specId == "swimming";

        public static float PaceFor(string? specId) =>
            specId != null && IsSlow(specId) ? Pace * SlowPaceFactor : Pace;

        // The top value (at Master) of every effect that is not already a named constant
        // elsewhere. Raising the game's ceiling later means changing one number here.
        public static class Ceiling
        {
            public const float QualityCap = 100f;
            public const float WorkTime = 0.4f;
            public const float MinigameSteps = 0.25f;
            public const float DugClayBonus = 35f;
            public const float SecondLumpChance = 0.5f;
            public const float SneakDistance = 3f;
            public const float SneakCritChance = 0.4f;
            public const float SneakCritBonus = 2f;
            public const float ArcheryDamage = 1.6f;
            public const float ArcheryHitChance = 1f;
            public const float ArcheryDrawTime = 0.8f;
            public const float WadeSpeed = 0.75f;
            public const float FloatSpeed = 0.7f;
        }

        // 0 to 1 share of the full effect at progress p (rank / MaxRank).
        public static float Curve(float p)
        {
            p = Math.Clamp(p, 0f, 1f);
            return MathF.Log(1f + CurveB * p) / MathF.Log(1f + CurveB);
        }

        public static float Effect(int rank, float lo, float hi) =>
            lo + (hi - lo) * Curve(rank / (float)Skills.MaxRank);

        public static float Effect(EffectKind kind, int rank)
        {
            var e = Get(kind);
            if (e.Shape == EffectShape.EvenInHits)
            {
                float x = Math.Clamp(rank, 0, Skills.MaxRank) / (float)Skills.MaxRank;
                return 1f / (1f / e.Lo + (1f / e.Hi - 1f / e.Lo) * x);
            }
            return Effect(rank, e.Lo, e.Hi);
        }

        // Every effect a skill has, with its start and Master values. Numbers marked
        // "proposal" are for Animals to tune with the PM.
        public static readonly EffectDef[] Effects =
        {
            new(EffectKind.QualityCap, "Best quality", 35f, Ceiling.QualityCap, "quality"),
            new(EffectKind.WorkTime, "Work time", 1f, Ceiling.WorkTime, "x"),
            new(EffectKind.MinigameSteps, "Minigame steps", 1f, Ceiling.MinigameSteps, "x"),
            new(EffectKind.DugClayBonus, "Dug clay quality", 0f, Ceiling.DugClayBonus, "quality"),
            new(EffectKind.SecondLumpChance, "Second lump of clay", 0f, Ceiling.SecondLumpChance, "chance"),
            new(EffectKind.FireSeconds, "Longest fire", Fires.MaxSeconds, Fires.MasterMaxSeconds, "s"),
            new(EffectKind.RunSeconds, "Run time", Stamina.StartRunSeconds, Stamina.MasterRunSeconds, "s"),
            new(EffectKind.RefillSeconds, "Breath refill", Stamina.StartRefillSeconds, Stamina.MasterRefillSeconds, "s"),
            // Proposal: sheep notice a walker at 14 m and never a sneaker, so a wary animal
            // (deer) bolts a novice sneaker at 10 m and a Master at 3 m.
            new(EffectKind.SneakDistance, "Sneak distance", 10f, Ceiling.SneakDistance, "m"),
            // Animals' values (Justin: normal damage scaling plus a bonus-damage trigger chance
            // when Stalking adds its bonus). With a ~60 spear hit on a 100-health deer, a
            // novice crit adds ~18 (wounded) and a Master crit ~72 (kills).
            new(EffectKind.SneakCritChance, "Sneak crit chance", 0.03f, Ceiling.SneakCritChance, "chance"),
            new(EffectKind.SneakCritBonus, "Sneak crit bonus", 0.5f, Ceiling.SneakCritBonus, "x damage"),
            // Animals' proposal for Archery (values go to Justin through the PM).
            new(EffectKind.ArcheryDamage, "Arrow damage", 0.44f, Ceiling.ArcheryDamage, "x", EffectShape.EvenInHits),
            new(EffectKind.ArcheryHitChance, "Arrow hit chance", 0.8f, Ceiling.ArcheryHitChance, "x"),
            // Swimming (starts at today's wading numbers, D-075): a little less slow-down while
            // wading and a faster float. Proposal for Justin via the PM.
            new(EffectKind.WadeSpeed, "Wading speed", Wading.WadeFloor, Ceiling.WadeSpeed, "x"),
            new(EffectKind.FloatSpeed, "Float speed", Wading.FloatSpeed, Ceiling.FloatSpeed, "x"),
            new(EffectKind.ArcheryDrawTime, "Draw time", 2f, Ceiling.ArcheryDrawTime, "s"),
        };

        public static EffectDef Get(EffectKind kind)
        {
            foreach (var e in Effects)
                if (e.Kind == kind) return e;
            throw new ArgumentException("no effect " + kind);
        }

        // Effects that truly need their own shape (none yet). Anything listed here
        // must say why the shared curve does not fit.
        public static readonly IReadOnlyDictionary<string, string> Exceptions = new Dictionary<string, string>
        {
            ["Skills.Gain"] = "Rank is earned, not spent: it already uses a single quadratic falloff for every skill, which is the time-side half of the same front-loading (docs/PROGRESSION.md, do not front-load twice).",
            ["Skills.SkipsMinigame"] = "A threshold (Master only), not a scale.",
        };
    }
}
