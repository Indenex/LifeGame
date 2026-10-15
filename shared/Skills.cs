// The skill and journal rules, shared by the server module and the client so
// both agree on names, ranks and what a rank is worth. Every number here is a
// tunable starting value. Keep this file free of engine and SpacetimeDB types.
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

        public Specialization(string id, string name, bool basic, string blurb)
        {
            Id = id;
            Name = name;
            Basic = basic;
            Blurb = blurb;
        }
    }

    // A process is something a player does that the journal can record:
    // digging, knapping a flake, twisting cordage. Crafting code reports it by id.
    public sealed class Process
    {
        public readonly string Id, Name, Spec, Hint;

        public Process(string id, string name, string spec, string hint)
        {
            Id = id;
            Name = name;
            Spec = spec;
            Hint = hint;
        }
    }

    public static class Skills
    {
        public const int MaxRank = 100;
        public const int TotalCap = 300;

        // Skill is stored in tenths of a rank, so small gains still add up.
        public const int PointsPerRank = 10;
        public const int MaxPoints = MaxRank * PointsPerRank;
        public const int CapPoints = TotalCap * PointsPerRank;

        // Players this close to someone finishing a process learn it by watching.
        public const float WatchRadius = 8f;

        // Each player can leave this many notes lying around; older ones blow away.
        public const int NotesPerPlayer = 5;

        public static readonly Specialization[] Specializations =
        {
            new("foraging", "Foraging", true, "Finding and gathering what the land offers."),
            new("earthwork", "Earthwork", true, "Digging, shifting soil, finding clay."),
            new("firecraft", "Firecraft", false, "Making fire and keeping it alive."),
            new("knapping", "Knapping", false, "Shaping stone into edges and tools."),
            new("cordage", "Cordage", false, "Twisting fiber into cord and lashing things together."),
            new("pottery", "Pottery", false, "Shaping clay and firing it hard."),
        };

        public static readonly Process[] Processes =
        {
            new("gather", "Gathering", "foraging", "Pick up what lies on the ground."),
            new("dig", "Digging", "earthwork", "Dig into the earth with your hands. Low wet ground hides clay."),
            new("fire_drill", "Hand-drill fire", "firecraft", "Spin a stick against dry wood until it smokes."),
            new("knap_flake", "Knapping a flake", "knapping", "Strike stone on stone to break off a sharp flake."),
            new("knap_tool", "Knapping a tool", "knapping", "Work a flake into a shaped edge."),
            new("twist_cordage", "Twisting cordage", "cordage", "Twist plant fiber into cord."),
            new("lash", "Lashing", "cordage", "Bind parts together with cord."),
            new("shape_clay", "Shaping clay", "pottery", "Form a lump of clay into a vessel."),
            new("fire_pottery", "Firing pottery", "pottery", "Harden shaped clay in a fire."),
        };

        static Dictionary<string, Specialization>? specsById;
        static Dictionary<string, Process>? processesById;

        public static Specialization? Spec(string id)
        {
            specsById ??= Index(Specializations, s => s.Id);
            return specsById.TryGetValue(id, out var s) ? s : null;
        }

        // Unknown ids are allowed: they still go in the journal but train nothing.
        public static Process? ProcessById(string id)
        {
            processesById ??= Index(Processes, p => p.Id);
            return processesById.TryGetValue(id, out var p) ? p : null;
        }

        public static Specialization? SpecFor(string processId) =>
            ProcessById(processId) is { } p ? Spec(p.Spec) : null;

        public static string ProcessName(string processId) => ProcessById(processId)?.Name ?? Humanize(processId);

        public static int Rank(int points) => Math.Clamp(points / PointsPerRank, 0, MaxRank);

        // Points gained for finishing a process once. Fast at first so learning
        // feels good, slower near mastery, never zero: rank 10 after about 6 uses,
        // rank 50 after about 50, Master after about 400.
        public static int Gain(int points)
        {
            float left = 1f - Math.Clamp(points, 0, MaxPoints) / (float)MaxPoints;
            return Math.Max(1, (int)MathF.Round(20f * left * left));
        }

        // The best quality a player can produce: rough at first, anything at Master.
        public static float QualityCap(int rank) => 0.35f + 0.65f * Math.Clamp(rank, 0, MaxRank) / MaxRank;

        // Multiplier on how long a process takes: a Master works in 40% of the time.
        public static float DurationScale(int rank) => 1f - 0.6f * Math.Clamp(rank, 0, MaxRank) / MaxRank;

        // Minigames lose steps as you improve; at Master a craft is one action.
        public static int MinigameSteps(int baseSteps, int rank)
        {
            if (rank >= MaxRank) return 1;
            return Math.Max(1, (int)MathF.Round(baseSteps * (1f - 0.75f * Math.Clamp(rank, 0, MaxRank) / MaxRank)));
        }

        public static string Title(int rank) => rank switch
        {
            >= 100 => "Master",
            >= 80 => "Artisan",
            >= 60 => "Expert",
            >= 40 => "Journeyman",
            >= 20 => "Apprentice",
            _ => "Novice",
        };

        static Dictionary<string, T> Index<T>(T[] items, Func<T, string> key)
        {
            var map = new Dictionary<string, T>();
            foreach (var item in items) map[key(item)] = item;
            return map;
        }

        static string Humanize(string id)
        {
            var words = id.Replace('_', ' ');
            return words.Length == 0 ? id : char.ToUpperInvariant(words[0]) + words[1..];
        }
    }
}
