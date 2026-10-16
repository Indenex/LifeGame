// Batch devices (crafting-23, D-195): a device runs a simple hand process over a loaded
// batch on its own. Left alone it makes poorer work; if the player stays and plays the
// minigame it makes better work and more of it. Doing the process by hand, one at a
// time, stays as it is. Every number here is a tunable starting value (Justin picked
// package A). Keep this file free of engine and SpacetimeDB types.
using System;
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public static class Devices
    {
        // Units in one load, and units worked in one round (the minigame covers a round).
        public const int BatchCap = 10, RoundUnits = 5;
        // Unattended: quality and time against working by hand, and the share of skill gained.
        public const float UnattendedQuality = 0.6f, UnattendedTime = 1.5f, UnattendedSkillShare = 0.25f;
        // Attended: time against by hand; the bonuses scale with the minigame score (0 to 1).
        public const float AttendedTime = 0.8f, AttendedQualityBonus = 0.1f, AttendedQuantityBonus = 0.2f;

        static readonly Dictionary<StationKind, string[]> runs = new()
        {
            [StationKind.ClayTub] = new[] { "clay.clean" },
            [StationKind.CrushBlock] = new[] { "clay.grit", "clay.grog" },
            [StationKind.TemperTrough] = new[] { "clay.temper_grit", "clay.temper_grog", "clay.daub" },
        };

        public static bool IsDevice(StationKind kind) => runs.ContainsKey(kind);

        public static IReadOnlyList<string> ProcessesOf(StationKind kind) =>
            runs.TryGetValue(kind, out var list) ? list : Array.Empty<string>();

        public static bool Runs(StationKind kind, string processId) =>
            runs.TryGetValue(kind, out var list) && Array.IndexOf(list, processId) >= 0;

        // The minigame played at a device: the block strikes, the tub and trough knead.
        public static Minigame MinigameOf(StationKind kind) => kind == StationKind.CrushBlock ? Minigame.Strike : Minigame.Shape;

        // Seconds one unit takes at a device against the same process by hand.
        public static float UnitSeconds(float handSeconds, bool attended) =>
            handSeconds * (attended ? AttendedTime : UnattendedTime);

        // Quality of a round's output: unattended work loses a large share; attended work
        // matches hand quality and gains up to AttendedQualityBonus with the score.
        public static byte Quality(byte handQuality, bool attended, float score) =>
            (byte)Math.Clamp((int)MathF.Round(handQuality * (attended ? 1f + AttendedQualityBonus * Math.Clamp(score, 0f, 1f) : UnattendedQuality)), 1, 100);

        // Items a round makes: attended work gains up to AttendedQuantityBonus with the score.
        public static uint Quantity(uint baseCount, bool attended, float score) =>
            attended ? (uint)MathF.Floor(baseCount * (1f + AttendedQuantityBonus * Math.Clamp(score, 0f, 1f))) : baseCount;

        // How many times a round reports the process to the skills system (full share when attended).
        public static int SkillReports(int units, bool attended) =>
            attended ? units : (int)MathF.Ceiling(units * UnattendedSkillShare);
    }
}
