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
        // What trains this skill: an exact process id ("animal.shear") or a recipe family
        // ("knap" covers every "knap.*" recipe). An exact id beats a family.
        public readonly string[] Trains;

        public Specialization(string id, string name, bool basic, string blurb, params string[] trains)
        {
            Id = id;
            Name = name;
            Basic = basic;
            Blurb = blurb;
            Trains = trains;
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
        // Shearing, reported by the Animals lane (Shearing.cs ShearProcess).
        public const string Shear = "animal.shear";
        // Reported on every strike that lands, and when a carcass is butchered (Animals lane).
        public const string Kill = "animal.kill";
        // Moving at sneak speed near a wary animal that has not bolted (Animals lane reports it).
        public const string Sneak = "animal.sneak";
        // Loosing an arrow, reported on every shot by the Animals lane (Bow.cs); a miss
        // reports it as failed, so a hit teaches twice as much (Skills.Gain).
        public const string Shoot = "animal.shoot";
        public const string Butcher = "animal.butcher";
        // Taking eggs from a hen (Animals lane, Eggs.cs).
        public const string Eggs = "animal.eggs";
        // Taking dried leather off its rack (Animals' Leather.cs); not a recipe.
        public const string DryHide = "hide.dry";
        // Running, reported by Survival's stamina every 20 s of actual running (D-123).
        public const string Run = "move.run";
        public static readonly Specialization[] Specializations =
        {
            new("foraging", "Foraging", true, "Finding and gathering what the land offers, and felling wood.", "gather", "chop"),
            new("earthwork", "Earthwork", true, "Digging and finding clay.", "earth"),
            new("firecraft", "Firecraft", true, "Making fire, keeping it alive and cooking over it.", "fire", "cook"),
            new("toolmaking", "Toolmaking", true, "Knapping stone, twisting cord, weaving baskets and mats, carving and lashing simple tools.", "knap", "cord", "weave", "lash", "carve", "haft"),
            new("potter", "Potter", false, "Shaping clay and firing it hard.", "clay"),
            new("builder", "Builder", false, "Raising frames, walls and roofs.", "build"),
            new("endurance", "Endurance", true, "Running farther and catching your breath faster.", "move"),
            // Animal work is a survival basic (D-049). Husbandry names its processes, so a new
            // animal.* process trains nothing until a skill claims it (CONTENT.md, Skill row).
            new("husbandry", "Husbandry", true, "Keeping animals calm and shearing them cleanly.", Shear, Butcher, Eggs),
            // Sneaking up on wary animals and striking before they bolt (skills-12).
            new("stalking", "Stalking", true, "Moving quietly near wary animals and striking before they bolt.", Kill, Sneak),
            // The bow (skills-12): a capped specialty, so it competes for the 300 total.
            new("archery", "Archery", false, "Shooting a bow true and quickly.", Shoot),
            // Specialties for the crafts that grew out of animals (D-065, D-071, skills-11). Wool
            // work shares the "cord" and "weave" families with Toolmaking; exact ids win.
            new("leatherwork", "Leatherwork", false, "Scraping and drying hides into leather, and working it.", "hide", "sew.leather_wraps"),
            new("weaver", "Weaver", false, "Spinning wool into yarn, weaving cloth and sewing it to wear.", "sew", "cord.spin_wool", "weave.wool_cloth"),
        };

        // Processes that are reported by the game rather than crafted from Processes.List: one table
        // for their journal name and their place in the journal's order (CONTENT.md, Process row).
        static readonly (string Id, string Name)[] ActionsBeforeRecipes =
        {
            (Forage, "Foraging"),
            (Berries, "Picking berries"),
            (Dig, "Digging"),
        };

        // Building stages are reported by the Building lane (Structures.FrameProcess and friends);
        // build.weave is the storage basket's one stage; build.door is reported by HangDoor (building-3).
        static readonly (string Id, string Name)[] ActionsAfterRecipes =
        {
            (ClayRules.FillProcess, "Filling a pot"),
            (ClayRules.PitFireProcess, "Pit firing"),
            (ClayRules.KilnProcess, "Building a kiln"),
            (ClayRules.KilnFireProcess, "Kiln firing"),
            ("build.frame", "Raising a frame"),
            ("build.wattle", "Weaving wattle walls"),
            ("build.daub", "Daubing walls"),
            ("build.roof", "Thatching a roof"),
            ("build.weave", "Weaving a storage basket"),
            ("build.door", "Hanging a door"),
            (Shear, "Shearing a sheep"),
            (Kill, "Hunting"),
            (Sneak, "Sneaking up on an animal"),
            (Shoot, "Shooting a bow"),
            (Butcher, "Butchering a carcass"),
            (Eggs, "Gathering eggs"),
            (DryHide, "Drying leather"),
            (Run, "Running"),
            // D-127 first-evening step: Claims reports the marker (#174).
            ("claim.mark", "Planting a claim"),
        };

        // Names for the build step of each crafting station (D-098). The ids come from
        // StationInfo.BuildProcess for each station that exists.
        static readonly Dictionary<string, string> StationBuildNames = new()
        {
            ["build.bench"] = "Building a potter's bench",
            ["build.table"] = "Building a crafting table",
            ["build.spindle"] = "Building a spindle station",
            ["build.rack"] = "Building a drying rack",
        };

        static readonly Dictionary<string, string> ActionNames = BuildActionNames();

        static Dictionary<string, string> BuildActionNames()
        {
            var names = new Dictionary<string, string>(StationBuildNames);
            foreach (var (id, name) in ActionsBeforeRecipes) names[id] = name;
            foreach (var (id, name) in ActionsAfterRecipes) names[id] = name;
            return names;
        }

        // Everything the journal counts toward "still to discover".
        public static IEnumerable<string> AllProcessIds()
        {
            foreach (var (id, _) in ActionsBeforeRecipes) yield return id;
            foreach (var def in Processes.List) yield return def.Id;
            foreach (var (id, _) in ActionsAfterRecipes) yield return id;
            // Building each kind of crafting station (D-098), as Structures reports it.
            var stations = new HashSet<string>();
            foreach (var kind in Structures.Kinds)
            {
                var shape = Structures.Shape(kind);
                if (shape.Station != StationKind.None && stations.Add(StationInfo.BuildProcess(shape.Station)))
                    yield return StationInfo.BuildProcess(shape.Station);
            }
        }

        // Some things can't be picked up by watching someone else do them.
        public static bool Watchable(string processId) => !processId.StartsWith("move.");

        public static Specialization? Spec(string id)
        {
            foreach (var s in Specializations)
                if (s.Id == id) return s;
            return null;
        }

        // The skill a process trains: the one that names its exact id, else the one that names
        // its family (the part before the first dot). Unknown processes still go in the journal
        // but train nothing.
        public static Specialization? SpecFor(string processId)
        {
            // An exact process id beats a recipe family (wool spinning shares "cord" with Toolmaking).
            foreach (var s in Specializations)
                if (Array.IndexOf(s.Trains, processId) >= 0) return s;
            int dot = processId.IndexOf('.');
            string family = dot < 0 ? processId : processId[..dot];
            foreach (var s in Specializations)
                if (Array.IndexOf(s.Trains, family) >= 0) return s;
            return null;
        }

        public static string ProcessName(string processId)
        {
            if (Processes.Find(processId) is { } def) return def.Name;
            if (ActionNames.TryGetValue(processId, out var name)) return name;
            string last = processId[(processId.LastIndexOf('.') + 1)..].Replace('_', ' ');
            return last.Length == 0 ? processId : char.ToUpperInvariant(last[0]) + last[1..];
        }

        public static int Rank(int points) => Math.Clamp(points / PointsPerRank, 0, MaxRank);

        // Points gained for finishing a process once. Fast at first so learning
        // feels good, slower near mastery, never zero: rank 10 after about 11 uses,
        // rank 50 after about 100, Master after about 800, so masters stay rare
        // (Justin's choice). A failed attempt still teaches, at half the rate.
        public static int Gain(int points, bool failed, string? specId = null)
        {
            float left = 1f - Math.Clamp(points, 0, MaxPoints) / (float)MaxPoints;
            int gain = Math.Max(5, (int)MathF.Round(100f * left * left));
            if (failed) gain = Math.Max(1, gain / 2);
            // One global pace for all skills (Progression.Pace, 1 now for testing); combat skills
            // level slower (Progression.CombatPaceFactor).
            return Math.Max(1, (int)MathF.Round(gain * Progression.PaceFor(specId)));
        }

        // The best quality, 1 to 100, a player can produce: 35 at first, anything at Master.
        public static byte QualityCap(int rank) => (byte)MathF.Round(Progression.Effect(EffectKind.QualityCap, rank));

        // Multiplier on how long a process takes: a Master works in 40% of the time.
        public static float DurationScale(int rank) => Progression.Effect(EffectKind.WorkTime, rank);

        // Minigames lose steps as you improve; at Master a craft is one action.
        public static int MinigameSteps(int baseSteps, int rank)
        {
            if (rank >= MaxRank) return 1;
            return Math.Max(1, (int)MathF.Round(baseSteps * Progression.Effect(EffectKind.MinigameSteps, rank)));
        }

        // A Master doesn't play the minigame; the craft scores as a steady hand.
        public const float MasterScore = 0.8f;
        public static bool SkipsMinigame(int rank) => rank >= MaxRank;

        // Dug clay: a novice gets ordinary clay, a skilled digger picks out cleaner
        // lumps and sometimes two at once.
        // Steps of 5 keep a digger's clay in a few stacks instead of one per point.
        public static byte DugClayQuality(int rank) => (byte)Math.Clamp(Items.RawQuality + (int)Progression.Effect(EffectKind.DugClayBonus, rank) / 5 * 5, 1, 100);
        public static double SecondLumpChance(int rank) => Progression.Effect(EffectKind.SecondLumpChance, rank);

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
