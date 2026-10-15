// Processes are the verbs of crafting: take these inputs, use a tool with these
// properties, spend this long, get this result. They are plain data, so new
// tiers add rows here instead of new code paths. Process ids are stable strings
// that other systems (skills, the journal) key on: never rename one.
using System;
using System.Collections.Generic;

namespace LifeGame.Shared
{
    // The hands-on part of a key step. Everything else simply takes time.
    public enum Minigame : byte
    {
        None,
        Drill,  // spin a hand drill: alternate left and right to build heat
        Strike, // knap: strike when the mark is in the sweet spot
        Shape,  // pinch a pot: hold to thin the wall, release in the sweet spot
        Coil,   // build a coil pot: lay each coil as the turning pot comes round
    }

    // What a finished process makes.
    public enum Outcome : byte
    {
        Item,  // an item into the crafter's inventory
        Fire,  // a campfire on the ground in front of the crafter
        Placed, // something set down in front of the crafter; its lane's OnPlaced hook makes it
        Roast,  // set over the nearest burning fire; taken off later, cooked or burnt (see Roasting)
    }

    public sealed class Ingredient
    {
        public Item Item;
        public uint Count = 1;
        public Ingredient(Item item, uint count = 1) { Item = item; Count = count; }
    }

    public sealed class ProcessDef
    {
        public string Id = "";
        public string Name = "";
        // Shown while the job runs: "Twisting cordage".
        public string Doing = "";
        public int Tier;
        public Ingredient[] Consumes = Array.Empty<Ingredient>();
        // A tool you must hold, matched by property; it is not used up.
        public ItemTags NeedsTool;
        public float Seconds;
        public Minigame Minigame;
        public Outcome Outcome;
        public Item Makes;
        public uint MakesCount = 1;
        // Below this minigame score the attempt fails and the inputs are lost.
        public float FailBelow;
    }

    public static class Processes
    {
        public static readonly IReadOnlyList<ProcessDef> List = new[]
        {
            // Tier 0: bare hands.
            new ProcessDef
            {
                Id = "gather.tinder", Name = "Shred tinder", Doing = "Shredding fiber into tinder", Tier = 0,
                Consumes = new[] { new Ingredient(Item.Fiber, 2) },
                Seconds = 3, Makes = Item.Tinder,
            },

            // Tier 1: fire and the first stone edges.
            new ProcessDef
            {
                Id = "fire.hand_drill", Name = "Hand-drill fire", Doing = "Nursing an ember into flame", Tier = 1,
                Consumes = new[] { new Ingredient(Item.Stick, 2), new Ingredient(Item.Tinder) },
                Seconds = 4, Minigame = Minigame.Drill, Outcome = Outcome.Fire, FailBelow = 0.35f,
            },
            new ProcessDef
            {
                Id = "knap.flake", Name = "Knap a flake", Doing = "Knapping a flake", Tier = 1,
                Consumes = new[] { new Ingredient(Item.Stone) }, NeedsTool = ItemTags.Hammer,
                Seconds = 3, Minigame = Minigame.Strike, Makes = Item.Flake, FailBelow = 0.2f,
            },
            new ProcessDef
            {
                Id = "knap.hand_axe", Name = "Shape a hand axe", Doing = "Shaping a hand axe", Tier = 1,
                Consumes = new[] { new Ingredient(Item.Stone) }, NeedsTool = ItemTags.Hammer,
                Seconds = 8, Minigame = Minigame.Strike, Makes = Item.HandAxe, FailBelow = 0.3f,
            },
            new ProcessDef
            {
                Id = "cook.meat", Name = "Roast mutton", Doing = "Setting mutton over the fire", Tier = 1,
                Consumes = new[] { new Ingredient(Item.RawMutton) },
                Seconds = 1, Outcome = Outcome.Roast, Makes = Item.CookedMutton,
            },
            new ProcessDef
            {
                Id = "carve.digging_stick", Name = "Carve a digging stick", Doing = "Sharpening a digging stick", Tier = 1,
                Consumes = new[] { new Ingredient(Item.Stick) }, NeedsTool = ItemTags.Cutting,
                Seconds = 5, Makes = Item.DiggingStick,
            },

            // Tier 2: cordage and hafted tools.
            new ProcessDef
            {
                Id = "cord.twist", Name = "Twist cordage", Doing = "Twisting cordage", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Fiber, 3) },
                Seconds = 6, Makes = Item.Cordage,
            },
            new ProcessDef
            {
                Id = "lash.spear", Name = "Lash a spear", Doing = "Lashing a spear", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick), new Ingredient(Item.Flake), new Ingredient(Item.Cordage) },
                Seconds = 8, Makes = Item.Spear,
            },
            new ProcessDef
            {
                Id = "lash.stone_axe", Name = "Haft a stone axe", Doing = "Hafting a stone axe", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick), new Ingredient(Item.HandAxe), new Ingredient(Item.Cordage) },
                Seconds = 10, Makes = Item.StoneAxe,
            },
            new ProcessDef
            {
                Id = "haft.knife", Name = "Haft a stone knife", Doing = "Binding a flake into a handle", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick), new Ingredient(Item.Flake), new Ingredient(Item.Cordage) },
                Seconds = 8, Makes = Item.StoneKnife,
            },
            new ProcessDef
            {
                Id = "weave.basket", Name = "Weave a basket", Doing = "Weaving a basket", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick, 3), new Ingredient(Item.Fiber, 10) },
                Seconds = 15, Makes = Item.Basket,
            },

            // Tier 3: clay. Dug clay is cleaned, tempered so it survives the fire,
            // shaped, dried in the open, then fired (see ClayRules and Ceramics.cs).
            new ProcessDef
            {
                Id = "clay.clean", Name = "Clean clay", Doing = "Picking stones and roots out of the clay", Tier = 3,
                Consumes = new[] { new Ingredient(Item.Clay, 2) },
                Seconds = 4, Makes = Item.CleanClay,
            },
            new ProcessDef
            {
                Id = "clay.grit", Name = "Crush stone grit", Doing = "Crushing stone into grit", Tier = 3,
                Consumes = new[] { new Ingredient(Item.Stone) }, NeedsTool = ItemTags.Hammer,
                Seconds = 4, Makes = Item.Grit, MakesCount = 2,
            },
            new ProcessDef
            {
                Id = "clay.grog", Name = "Crush sherds into grog", Doing = "Crushing potsherds into grog", Tier = 3,
                Consumes = new[] { new Ingredient(Item.Potsherd) }, NeedsTool = ItemTags.Hammer,
                Seconds = 3, Makes = Item.Grog, MakesCount = 2,
            },
            new ProcessDef
            {
                Id = "clay.temper_grit", Name = "Temper clay with grit", Doing = "Wedging grit into the clay", Tier = 3,
                Consumes = new[] { new Ingredient(Item.CleanClay, 2), new Ingredient(Item.Grit) },
                Seconds = 5, Makes = Item.TemperedClay, MakesCount = 2,
            },
            // Old pots make the best temper: the clay goes further.
            new ProcessDef
            {
                Id = "clay.temper_grog", Name = "Temper clay with grog", Doing = "Wedging grog into the clay", Tier = 3,
                Consumes = new[] { new Ingredient(Item.CleanClay, 2), new Ingredient(Item.Grog) },
                Seconds = 5, Makes = Item.TemperedClay, MakesCount = 3,
            },
            new ProcessDef
            {
                Id = "clay.pinch_pot", Name = "Pinch a pot", Doing = "Pinching a pot", Tier = 3,
                Consumes = new[] { new Ingredient(Item.TemperedClay) },
                Seconds = 6, Minigame = Minigame.Shape, Outcome = Outcome.Placed, FailBelow = 0.15f,
            },
            new ProcessDef
            {
                Id = "clay.coil_pot", Name = "Build a coil pot", Doing = "Building a coil pot", Tier = 3,
                Consumes = new[] { new Ingredient(Item.TemperedClay, 3) },
                Seconds = 12, Minigame = Minigame.Coil, Outcome = Outcome.Placed, FailBelow = 0.15f,
            },
            new ProcessDef
            {
                Id = "clay.brick", Name = "Mold a brick", Doing = "Molding a brick", Tier = 3,
                Consumes = new[] { new Ingredient(Item.CleanClay, 2), new Ingredient(Item.Fiber) },
                Seconds = 5, Outcome = Outcome.Placed,
            },
            new ProcessDef
            {
                Id = "clay.daub", Name = "Mix daub", Doing = "Mixing clay and fiber into daub", Tier = 3,
                Consumes = new[] { new Ingredient(Item.CleanClay), new Ingredient(Item.Fiber) },
                Seconds = 4, Makes = Item.Daub, MakesCount = 2,
            },
        };

        static readonly Dictionary<string, ProcessDef> ById = Index();

        public static ProcessDef? Find(string id) => ById.TryGetValue(id, out var def) ? def : null;

        // Hands-off steps count as a steady, average effort.
        public const float DefaultScore = 0.6f;

        // Quality of the result, 0 to 100: half from the materials, half from the
        // hands-on step, never above the crafter's cap.
        public static byte Quality(float inputQuality, float score, byte cap)
        {
            float q = inputQuality * 0.5f + Math.Clamp(score, 0f, 1f) * 100f * 0.5f;
            return (byte)Math.Clamp(MathF.Round(q), 1f, cap);
        }

        // Words for a quality number, so players never see raw digits.
        public static string Grade(byte quality) => quality switch
        {
            < 25 => "crude",
            < 50 => "rough",
            < 70 => "decent",
            < 88 => "fine",
            _ => "excellent",
        };

        static Dictionary<string, ProcessDef> Index()
        {
            var map = new Dictionary<string, ProcessDef>();
            foreach (var def in List) map.Add(def.Id, def);
            return map;
        }
    }

    // Campfires: how long they burn and how far their warmth reaches.
    public static class Fires
    {
        // A freshly lit fire burns this long before it needs feeding.
        public const float StartSeconds = 60;
        // Fuel stacks up to this many seconds ahead.
        public const float MaxSeconds = 20 * 60;
        // Warmth falls off to nothing at this distance, in meters.
        public const float WarmthRadius = 5f;

        // 0 to 1: how warm a spot is from one burning fire.
        public static float WarmthFrom(float fireX, float fireZ, float x, float z)
        {
            float dx = x - fireX, dz = z - fireZ;
            float d = MathF.Sqrt(dx * dx + dz * dz);
            return Math.Clamp(1f - d / WarmthRadius, 0f, 1f);
        }
    }

    // Meat over a fire browns with time: take it off too soon and it isn't done,
    // leave it too long and it burns. Best at BestSeconds.
    public static class Roasting
    {
        public const float CookSeconds = 20f;
        public const float BestSeconds = 30f;
        public const float BurnSeconds = 60f;
        // Most roasts one fire holds at once.
        public const int PerFire = 4;

        public static bool Done(float seconds) => seconds >= CookSeconds;
        public static bool Burnt(float seconds) => seconds >= BurnSeconds;

        // How well it was timed, 0 to 1, for the quality of the roast.
        public static float Score(float seconds) => seconds <= BestSeconds
            ? Math.Clamp(0.6f + 0.4f * (seconds - CookSeconds) / (BestSeconds - CookSeconds), 0f, 1f)
            : Math.Clamp(1f - 0.6f * (seconds - BestSeconds) / (BurnSeconds - BestSeconds), 0f, 1f);

        public static string State(float seconds) =>
            Burnt(seconds) ? "burnt" : seconds >= BurnSeconds - 10f ? "starting to char" : Done(seconds) ? "ready" : "browning";
    }
}
