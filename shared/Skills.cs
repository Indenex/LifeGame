// The skill rules, shared by the server module and the client so both agree on
// names, ranks and what a rank is worth. Every number here is a tunable
// starting value. Keep this file free of engine and SpacetimeDB types.
using System;
using System.Collections.Generic;

namespace LifeGame.Shared
{
    // A specialization is one skill from 0 to 100. Basics are survival skills
    // everyone needs; they never count toward the total cap.
    public sealed class Specialization
    {
        public readonly string Id, Name, Blurb;
        public readonly bool Basic;
        // Process ids starting with any of these ("knap" covers "knap.flake") train this skill.
        public readonly string[] Prefixes;

        public Specialization(string id, string name, bool basic, string blurb, params string[] prefixes)
        {
            Id = id;
            Name = name;
            Basic = basic;
            Blurb = blurb;
            Prefixes = prefixes;
        }
    }

    public static class Skills
    {
        public const int MaxRank = 100;
        public const int TotalCap = 300;

        // Skill is stored in hundredths of a rank, so small gains still add up.
        public const int PointsPerRank = 100;
        public const int MaxPoints = MaxRank * PointsPerRank;
        public const int CapPoints = TotalCap * PointsPerRank;

        // Players this close to someone finishing a process learn it by watching.
        public const float WatchRadius = 8f;

        // Each player can leave this many notes lying around; older ones blow away.
        public const int NotesPerPlayer = 5;

        // Processes that are not crafting jobs, reported by the base game.
        public const string Dig = "earth.dig";
        public const string Forage = "gather.forage";
        public const string Berries = "gather.berries";
        // Building stages, reported by the Building lane (Structures.FrameProcess and friends).
        static readonly string[] BuildStages = { "build.frame", "build.wattle", "build.daub", "build.roof" };

        public static readonly Specialization[] Specializations =
        {
            new("foraging", "Foraging", true, "Finding and gathering what the land offers.", "gather"),
            new("earthwork", "Earthwork", true, "Digging and finding clay.", "earth"),
            new("firecraft", "Firecraft", true, "Making fire and keeping it alive.", "fire"),
            new("toolmaking", "Toolmaking", true, "Knapping stone, twisting cord, lashing simple tools.", "knap", "cord", "lash"),
            new("potter", "Potter", false, "Shaping clay and firing it hard.", "clay"),
            new("builder", "Builder", false, "Raising frames, walls and roofs.", "build"),
        };

        // Names for processes that are not in Processes.List.
        static readonly Dictionary<string, string> ExtraNames = new()
        {
            [Dig] = "Digging",
            [Forage] = "Foraging",
            [ClayRules.PitFireProcess] = "Pit firing",
            [ClayRules.FillProcess] = "Filling a pot",
            ["build.frame"] = "Raising a frame",
            ["build.wattle"] = "Weaving wattle walls",
            ["build.daub"] = "Daubing walls",
            ["build.roof"] = "Thatching a roof",
            [Berries] = "Picking berries",
        };

        // Everything the journal counts toward "still to discover".
        public static IEnumerable<string> AllProcessIds()
        {
            yield return Forage;
            yield return Berries;
            yield return Dig;
            foreach (var def in Processes.List) yield return def.Id;
            yield return ClayRules.FillProcess;
            yield return ClayRules.PitFireProcess;
            foreach (var id in BuildStages) yield return id;
        }

        public static Specialization? Spec(string id)
        {
            foreach (var s in Specializations)
                if (s.Id == id) return s;
            return null;
        }

        // The skill a process trains, by the prefix before its first dot. Unknown
        // processes still go in the journal but train nothing.
        public static Specialization? SpecFor(string processId)
        {
            int dot = processId.IndexOf('.');
            string prefix = dot < 0 ? processId : processId[..dot];
            foreach (var s in Specializations)
                if (Array.IndexOf(s.Prefixes, prefix) >= 0) return s;
            return null;
        }

        public static string ProcessName(string processId)
        {
            if (Processes.Find(processId) is { } def) return def.Name;
            if (ExtraNames.TryGetValue(processId, out var name)) return name;
            string last = processId[(processId.LastIndexOf('.') + 1)..].Replace('_', ' ');
            return last.Length == 0 ? processId : char.ToUpperInvariant(last[0]) + last[1..];
        }

        public static int Rank(int points) => Math.Clamp(points / PointsPerRank, 0, MaxRank);

        // Points gained for finishing a process once. Fast at first so learning
        // feels good, slower near mastery, never zero: rank 10 after about 11 uses,
        // rank 50 after about 100, Master after about 800, so masters stay rare
        // (Justin's choice). A failed attempt still teaches, at half the rate.
        public static int Gain(int points, bool failed)
        {
            float left = 1f - Math.Clamp(points, 0, MaxPoints) / (float)MaxPoints;
            int gain = Math.Max(5, (int)MathF.Round(100f * left * left));
            return failed ? Math.Max(1, gain / 2) : gain;
        }

        // The best quality, 1 to 100, a player can produce: 35 at first, anything at Master.
        public static byte QualityCap(int rank) => (byte)MathF.Round(35f + 65f * Math.Clamp(rank, 0, MaxRank) / MaxRank);

        // Multiplier on how long a process takes: a Master works in 40% of the time.
        public static float DurationScale(int rank) => 1f - 0.6f * Math.Clamp(rank, 0, MaxRank) / MaxRank;

        // Minigames lose steps as you improve; at Master a craft is one action.
        public static int MinigameSteps(int baseSteps, int rank)
        {
            if (rank >= MaxRank) return 1;
            return Math.Max(1, (int)MathF.Round(baseSteps * (1f - 0.75f * Math.Clamp(rank, 0, MaxRank) / MaxRank)));
        }

        // Dug clay: a novice gets ordinary clay, a skilled digger picks out cleaner
        // lumps and sometimes two at once.
        // Steps of 5 keep a digger's clay in a few stacks instead of one per point.
        public static byte DugClayQuality(int rank) => (byte)Math.Clamp(Items.RawQuality + rank * 35 / MaxRank / 5 * 5, 1, 100);
        public static double SecondLumpChance(int rank) => rank / 200.0;

        public static string Title(int rank) => rank switch
        {
            >= 100 => "Master",
            >= 80 => "Artisan",
            >= 60 => "Expert",
            >= 40 => "Journeyman",
            >= 20 => "Apprentice",
            _ => "Novice",
        };
    }
}
