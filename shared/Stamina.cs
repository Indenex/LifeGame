// Running and stamina (D-088). Shared by the server module and the Godot client;
// keep it free of engine types. Numbers are proposals until Justin picks (survival-7).
using System;

namespace LifeGame.Shared
{
    public static class Stamina
    {
        // Walking stays at the client's 4.2 m/s; running is a third faster and stays
        // under the server's World.MaxSpeed, so the server needs no change.
        public const float RunSpeed = 5.6f;
        // A full bar lasts this long at a run, and refills from empty in RefillSeconds
        // whenever you're not running (walking counts).
        public const float RunSeconds = 10f;
        public const float RefillSeconds = 8f;
        // Run dry and you walk until the bar is back to this, so it doesn't stutter.
        public const float MinToRun = 0.2f;

        // Stamina (0..1) after dt seconds, running or not.
        public static float Step(float stamina, bool running, float dt) =>
            Math.Clamp(stamina + (running ? -dt / RunSeconds : dt / RefillSeconds), 0f, 1f);

        // Whether a held run key actually runs: not while winded after running dry.
        public static bool CanRun(float stamina, bool winded) => stamina > 0f && (!winded || stamina >= MinToRun);
    }
}
