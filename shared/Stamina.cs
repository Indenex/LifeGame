// Running and stamina (D-088, D-111). Shared by the server module and the Godot
// client; keep it free of engine types. Curves from docs/PROGRESSION.md, option 1.
using System;

namespace LifeGame.Shared
{
    public static class Stamina
    {
        // Walking stays at the client's 4.2 m/s; running is a third faster and stays
        // under the server's World.MaxSpeed, so the server needs no change (D-100).
        public const float RunSpeed = 5.6f;

        // A full bar lasts this long at a run, and refills from empty in this long,
        // growing linearly with rank from a beginner to a Master (D-111).
        public const float StartRunSeconds = 8f, MasterRunSeconds = 16f;
        public const float StartRefillSeconds = 12f, MasterRefillSeconds = 7f;
        // Refill waits this long after you stop running.
        public const float RefillDelay = 1f;
        // Run dry and you walk until the bar is back to this, so it doesn't stutter.
        public const float MinToRun = 0.2f;

        // One jump costs this share of the bar (bugfix-25; Justin picked 8%).
        public const float JumpCost = 0.08f;

        // A good meal (FoodDef.GoodMeal) adds this much run time for a while.
        public const float WellFedBonus = 0.25f;
        public const long WellFedMicros = 10L * 60 * 1_000_000;

        // Endurance (D-123): a basic skill trained by running. The client reports each
        // stretch of RunPerUse seconds run, and the server counts it as one use of
        // RunProcess, so Master (about 800 uses) takes roughly four and a half hours of running.
        public const string Skill = "endurance";
        public const string RunProcess = "move.run";
        public const float RunPerUse = 20f;
        // The server's check on that report (D-182): the player moved within the last RunReportMoveWindow
        // seconds and is at least RunReportMinMeters from where they last reported, so standing still
        // can't train Endurance. A real run of RunPerUse seconds covers about 110 m.
        public const float RunReportMoveWindow = 3f, RunReportMinMeters = 25f;

        public static float RunSeconds(int rank, bool wellFed) =>
            Lerp(StartRunSeconds, MasterRunSeconds, rank) * (wellFed ? 1f + WellFedBonus : 1f);

        public static float RefillSeconds(int rank) => Lerp(StartRefillSeconds, MasterRefillSeconds, rank);

        // Refill is faster when the bar is low: the rate falls from 1.5a at empty to
        // 0.5a at full, with a chosen so empty to full takes exactly RefillSeconds
        // (the integral of 1 / (a (1.5 - s)) from 0 to 1 is ln 3 / a).
        static float RefillRate(float stamina, float refillSeconds) =>
            MathF.Log(3f) / refillSeconds * (1.5f - stamina);

        // Stamina (0..1) after dt seconds. `rested` is seconds since you last ran.
        public static float Step(float stamina, bool running, float rested, int rank, bool wellFed, float dt)
        {
            if (running)
                return Math.Clamp(stamina - dt / RunSeconds(rank, wellFed), 0f, 1f);
            if (rested < RefillDelay)
                return stamina;
            return Math.Clamp(stamina + dt * RefillRate(stamina, RefillSeconds(rank)), 0f, 1f);
        }

        // Whether a held run key actually runs: not while winded after running dry.
        public static bool CanRun(float stamina, bool winded) => stamina > 0f && (!winded || stamina >= MinToRun);

        static float Lerp(float start, float master, int rank) =>
            Progression.Effect(rank, start, master);
    }
}
