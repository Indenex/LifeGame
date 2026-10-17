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
        Spin,   // spin yarn: flick the drop spindle as it slows, before it stops (Animals, D-070)
    }

    // What a finished process makes.
    public enum Outcome : byte
    {
        Item,  // an item into the crafter's inventory
        Fire,  // a campfire on the ground in front of the crafter
        Placed, // something set down in front of the crafter; its lane's OnPlaced hook makes it
        Roast,  // set over the nearest burning fire; taken off later, cooked or burnt (see Roasting)
        Chop,   // a log from the tree within reach of the crafter (see Trees and Woodcutting.cs)
    }

    public sealed class Ingredient
    {
        public Item Item;
        public uint Count = 1;
        public Ingredient(Item item, uint count = 1) { Item = item; Count = count; }
    }

    // Where a process is made (D-098, D-112): by hand anywhere, or at a finished
    // station inside a claim you have rights to. A station is a structure whose
    // shape names its kind (StructureShape.Station). Append new kinds at the end.
    public enum StationKind
    {
        None,
        Table,
        PottersBench,
        Spindle,
        DryingRack,
        Repair,
    }

    public static class StationInfo
    {
        // A station draws ingredients from storage this close to it, after the bag (D-112).
        public const float StorageReach = 4f;

        public static string Name(StationKind kind) => kind switch
        {
            StationKind.Table => "crafting table",
            StationKind.PottersBench => "potter's bench",
            StationKind.Spindle => "spindle station",
            StationKind.DryingRack => "leather drying rack",
            StationKind.Repair => "repair table",
            _ => "",
        };

        // The skill-practice id a station's building reports (build.* trains Builder).
        public static string BuildProcess(StationKind kind) => kind switch
        {
            StationKind.Table => "build.table",
            StationKind.PottersBench => "build.bench",
            StationKind.Spindle => "build.spindle",
            StationKind.DryingRack => "build.rack",
            StationKind.Repair => "build.table",
            _ => "build.frame",
        };
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
        // The station it is made at; None is a hand craft from the C menu.
        public StationKind Station;
        // How the body moves while it runs (shared/WorkMotion.cs). None lets the client
        // guess from the id; set it on new processes.
        public WorkMotion Motion;
    }

    public static class Processes
    {
        // A recipe's base time (D-113): its tier's seconds times how much work it is,
        // from 0.25 (crush, mix) to 3 (a storage jar). Fires and roasting keep their own
        // times. The job then takes longer for a better result (QualityTimeScale), and
        // shorter with skill (Journal's DurationScale) and a better tool (ToolSpeedup).
        static float Base(int tier, float effort) => effort * tier switch { 0 => 3f, 1 => 6f, 2 => 12f, _ => 20f };

        // How much longer a job takes for the quality it will come out at (D-113):
        // x0.88 for crude work (20), x1.04 for decent (60), x1.18 for excellent (95).
        public static float QualityTimeScale(byte quality) => 0.8f + 0.4f * quality / 100f;

        // A roast recipe comes from its Foods.Roasts entry, so the item and wording live in one place.
        static ProcessDef Roast(Item raw)
        {
            var d = Foods.FindRoast(raw)!;
            return new ProcessDef
            {
                Id = d.Process, Name = d.Name, Doing = d.Doing, Tier = 1,
                Consumes = new[] { new Ingredient(d.Raw) },
                Seconds = 1, Outcome = Outcome.Roast, Makes = d.Cooked,
            };
        }

        public static readonly IReadOnlyList<ProcessDef> List = new[]
        {
            // Tier 0: bare hands.
            new ProcessDef
            {
                Id = "gather.tinder", Name = "Shred tinder", Doing = "Shredding fiber into tinder", Tier = 0,
                Consumes = new[] { new Ingredient(Item.Fiber, 2) },
                Seconds = Base(0, 1f), Makes = Item.Tinder,
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
                Seconds = Base(1, 0.5f), Minigame = Minigame.Strike, Makes = Item.Flake, FailBelow = 0.2f,
            },
            new ProcessDef
            {
                Id = "knap.hand_axe", Name = "Shape a hand axe", Doing = "Shaping a hand axe", Tier = 1,
                Consumes = new[] { new Ingredient(Item.Stone) }, NeedsTool = ItemTags.Hammer,
                Seconds = Base(1, 1.5f), Minigame = Minigame.Strike, Makes = Item.HandAxe, FailBelow = 0.3f,
            },
            Roast(Item.RawMutton),
            // Cooking in a fired pot (ceramics-7): set at a burning fire, it simmers
            // on its own and is taken off with Use (server/spacetimedb/CookingPot.cs).
            new ProcessDef
            {
                Id = "cook.broth", Name = "Simmer berry broth", Doing = "Setting a pot of broth by the fire", Tier = 3,
                Consumes = new[] { new Ingredient(Item.CoilPotOfWater), new Ingredient(Item.Berries, 3) },
                Seconds = 2, Outcome = Outcome.Placed, Makes = Item.PotOfBroth,
            },
            new ProcessDef
            {
                Id = "cook.stew", Name = "Simmer mutton stew", Doing = "Setting a pot of stew by the fire", Tier = 3,
                Consumes = new[] { new Ingredient(Item.CoilPotOfWater), new Ingredient(Item.RawMutton), new Ingredient(Item.Berries, 2) },
                Seconds = 2, Outcome = Outcome.Placed, Makes = Item.PotOfStew,
            },
            new ProcessDef
            {
                // Mutton fat rendered in a pot of water by a fire (ceramics-9): lamp fuel.
                Id = "cook.tallow", Name = "Render tallow", Doing = "Setting a pot of mutton fat by the fire", Tier = 3,
                Consumes = new[] { new Ingredient(Item.CoilPotOfWater), new Ingredient(Item.RawMutton, 2) },
                Seconds = 2, Outcome = Outcome.Placed, Makes = Item.Tallow,
            },
            Roast(Item.RawVenison),
            Roast(Item.RawChicken),
            new ProcessDef
            {
                Id = "carve.digging_stick", Name = "Carve a digging stick", Doing = "Sharpening a digging stick", Tier = 1,
                Consumes = new[] { new Ingredient(Item.Stick) }, NeedsTool = ItemTags.Cutting,
                Seconds = Base(1, 1f), Makes = Item.DiggingStick,
            },

            // Tier 2: cordage and hafted tools.
            new ProcessDef
            {
                Id = "cord.twist", Name = "Twist cordage", Doing = "Twisting cordage", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Fiber, 3) },
                Seconds = Base(2, 0.5f), Makes = Item.Cordage,
            },
            new ProcessDef
            {
                Id = "lash.spear", Name = "Lash a spear", Doing = "Lashing a spear", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick), new Ingredient(Item.Flake), new Ingredient(Item.Cordage) },
                Seconds = Base(2, 1f), Makes = Item.Spear,
            },
            new ProcessDef
            {
                Id = "lash.stone_axe", Name = "Haft a stone axe", Doing = "Hafting a stone axe", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick), new Ingredient(Item.HandAxe), new Ingredient(Item.Cordage) },
                Seconds = Base(2, 1.5f), Makes = Item.StoneAxe,
            },
            new ProcessDef
            {
                Id = "haft.knife", Name = "Haft a stone knife", Doing = "Binding a flake into a handle", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick), new Ingredient(Item.Flake), new Ingredient(Item.Cordage) },
                Seconds = Base(2, 1f), Makes = Item.StoneKnife,
            },
            new ProcessDef
            {
                Id = "weave.basket", Name = "Weave a basket", Doing = "Weaving a basket", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick, 3), new Ingredient(Item.Fiber, 10) },
                Seconds = Base(2, 2.5f), Makes = Item.Basket, Station = StationKind.Table,
            },
            // House building: a woven grass mat to lay inside a hut or house and wake on (BuildingBed.cs).
            new ProcessDef
            {
                Id = "weave.mat", Name = "Weave a sleeping mat", Doing = "Weaving a sleeping mat", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Fiber, 8) },
                Seconds = Base(2, 2f), Makes = Item.SleepingMat,
            },
            // Animals' bow and arrows (animals-15). Powers are on the items (shared/Items.cs).
            new ProcessDef
            {
                Id = "lash.bow", Name = "String a bow", Doing = "Bending and stringing a bow", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick, 3), new Ingredient(Item.Cordage, 2) }, NeedsTool = ItemTags.Cutting,
                Seconds = Base(2, 1.5f), Makes = Item.Bow,
            },
            new ProcessDef
            {
                Id = "carve.arrows", Name = "Make arrows", Doing = "Shaving and fletching arrows", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick, 2), new Ingredient(Item.Fiber, 2) }, NeedsTool = ItemTags.Cutting,
                Seconds = Base(2, 1f), Makes = Item.Arrow, MakesCount = 3,
            },
            new ProcessDef
            {
                Id = "lash.bow_drill", Name = "String a bow drill", Doing = "Stringing a bow drill", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick, 2), new Ingredient(Item.Cordage) },
                Seconds = Base(2, 1f), Makes = Item.BowDrill,
            },
            // The bow spins the drill for you: half the strokes of a hand drill, and surer.
            new ProcessDef
            {
                Id = "fire.bow_drill", Name = "Bow-drill fire", Doing = "Nursing an ember into flame", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick, 2), new Ingredient(Item.Tinder) }, NeedsTool = ItemTags.FireBow,
                Seconds = 3, Minigame = Minigame.Drill, Outcome = Outcome.Fire, FailBelow = 0.2f,
            },
            // An axe takes a log from a standing tree; enough logs fell it (Trees).
            new ProcessDef
            {
                Id = "chop.log", Name = "Chop a log", Doing = "Chopping a log", Tier = 2,
                NeedsTool = ItemTags.Chopping, Seconds = Base(2, 1.25f), Outcome = Outcome.Chop, Makes = Item.Log,
            },

            // Hides (Animals lane): at a leather drying rack (D-098), a sheep hide is
            // scraped clean, then laced onto the rack's bar to dry into leather (Leather.cs).
            new ProcessDef
            {
                Id = "hide.scrape", Name = "Scrape a hide", Doing = "Scraping the hide clean", Tier = 2,
                Consumes = new[] { new Ingredient(Item.SheepHide) }, NeedsTool = ItemTags.Cutting,
                Seconds = Base(2, 1f), Makes = Item.ScrapedHide, Station = StationKind.DryingRack,
            },
            new ProcessDef
            {
                Id = "hide.scrape_deer", Name = "Scrape a deer hide", Doing = "Scraping the deer hide clean", Tier = 2,
                Consumes = new[] { new Ingredient(Item.DeerHide) }, NeedsTool = ItemTags.Cutting,
                Seconds = 8, Makes = Item.ScrapedHide,
            },
            new ProcessDef
            {
                Id = "hide.stretch", Name = "Stretch a hide to dry", Doing = "Lacing the hide onto a rack", Tier = 2,
                Consumes = new[] { new Ingredient(Item.ScrapedHide), new Ingredient(Item.Stick, 2), new Ingredient(Item.Cordage) },
                Seconds = Base(2, 0.5f), Outcome = Outcome.Placed, Station = StationKind.DryingRack,
            },

            // Wool (Animals lane): a stick and a stone whorl make a drop spindle at the
            // crafting table; built into a spindle station (D-098) it spins raw wool into
            // yarn, and yarn is woven on the station's stick frame into cloth. Spinning
            // and weaving train Toolmaking until a Weaver specialty exists (D-071).
            new ProcessDef
            {
                Id = "carve.drop_spindle", Name = "Make a drop spindle", Doing = "Fitting a stone whorl onto a stick", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Stick), new Ingredient(Item.Stone) }, NeedsTool = ItemTags.Cutting,
                Seconds = Base(2, 0.5f), Makes = Item.DropSpindle, Station = StationKind.Table,
            },
            new ProcessDef
            {
                Id = "cord.spin_wool", Name = "Spin wool yarn", Doing = "Spinning wool into yarn", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Wool, 2) },
                Seconds = Base(2, 1f), Minigame = Minigame.Spin, Makes = Item.Yarn, FailBelow = 0.1f, Station = StationKind.Spindle,
            },
            new ProcessDef
            {
                Id = "weave.wool_cloth", Name = "Weave wool cloth", Doing = "Weaving yarn over a stick frame", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Yarn, 4), new Ingredient(Item.Stick, 2) },
                Seconds = Base(2, 2f), Makes = Item.WoolCloth, Station = StationKind.Spindle,
            },

            // Clothing (Animals lane): worn in the equipment panel, slows the cold (see
            // Clothing.cs). Foot wraps are cut and laced at the leather drying rack (D-139);
            // the tunic is sewn from wool cloth at the crafting table.
            new ProcessDef
            {
                Id = "sew.wool_tunic", Name = "Sew a wool tunic", Doing = "Cutting and sewing a tunic", Tier = 2,
                Consumes = new[] { new Ingredient(Item.WoolCloth, 3), new Ingredient(Item.Yarn) }, NeedsTool = ItemTags.Cutting,
                Seconds = Base(2, 2f), Makes = Item.WoolTunic, Station = StationKind.Table,
            },
            new ProcessDef
            {
                Id = "sew.leather_wraps", Name = "Make leather foot wraps", Doing = "Cutting and lacing foot wraps", Tier = 2,
                Consumes = new[] { new Ingredient(Item.Leather), new Ingredient(Item.Cordage) }, NeedsTool = ItemTags.Cutting,
                Seconds = 10, Makes = Item.LeatherWraps, Station = StationKind.DryingRack,
            },

            // Tier 3: clay. Dug clay is cleaned, tempered so it survives the fire,
            // shaped, dried in the open, then fired (see ClayRules and Ceramics.cs).
            new ProcessDef
            {
                Id = "clay.clean", Name = "Clean clay", Doing = "Picking stones and roots out of the clay", Tier = 3,
                Consumes = new[] { new Ingredient(Item.Clay, 2) },
                Seconds = Base(3, 0.25f), Makes = Item.CleanClay,
            },
            new ProcessDef
            {
                Id = "clay.grit", Name = "Crush stone grit", Doing = "Crushing stone into grit", Tier = 3,
                Consumes = new[] { new Ingredient(Item.Stone) }, NeedsTool = ItemTags.Hammer,
                Seconds = Base(3, 0.25f), Makes = Item.Grit, MakesCount = 2,
            },
            new ProcessDef
            {
                Id = "clay.grog", Name = "Crush sherds into grog", Doing = "Crushing potsherds into grog", Tier = 3,
                Consumes = new[] { new Ingredient(Item.Potsherd) }, NeedsTool = ItemTags.Hammer,
                Seconds = Base(3, 0.25f), Makes = Item.Grog, MakesCount = 2,
            },
            new ProcessDef
            {
                Id = "clay.temper_grit", Name = "Temper clay with grit", Doing = "Wedging grit into the clay", Tier = 3,
                Consumes = new[] { new Ingredient(Item.CleanClay, 2), new Ingredient(Item.Grit) },
                Seconds = Base(3, 0.25f), Makes = Item.TemperedClay, MakesCount = 2,
            },
            // Old pots make the best temper: the clay goes further.
            new ProcessDef
            {
                Id = "clay.temper_grog", Name = "Temper clay with grog", Doing = "Wedging grog into the clay", Tier = 3,
                Consumes = new[] { new Ingredient(Item.CleanClay, 2), new Ingredient(Item.Grog) },
                Seconds = Base(3, 0.25f), Makes = Item.TemperedClay, MakesCount = 3,
            },
            new ProcessDef
            {
                Id = "clay.pinch_pot", Name = "Pinch a pot", Doing = "Pinching a pot", Tier = 3,
                Consumes = new[] { new Ingredient(Item.TemperedClay) },
                Seconds = Base(3, 0.75f), Minigame = Minigame.Shape, Station = StationKind.PottersBench, Outcome = Outcome.Placed, FailBelow = 0.15f,
            },
            new ProcessDef
            {
                Id = "clay.coil_pot", Name = "Build a coil pot", Doing = "Building a coil pot", Tier = 3,
                Consumes = new[] { new Ingredient(Item.TemperedClay, 3) },
                Seconds = Base(3, 2f), Minigame = Minigame.Coil, Station = StationKind.PottersBench, Outcome = Outcome.Placed, FailBelow = 0.15f,
            },
            new ProcessDef
            {
                Id = "clay.lamp", Name = "Pinch a lamp", Doing = "Pinching a lamp", Tier = 3,
                Consumes = new[] { new Ingredient(Item.TemperedClay) },
                Seconds = 6, Minigame = Minigame.Shape, Station = StationKind.PottersBench, Outcome = Outcome.Placed, FailBelow = 0.15f,
            },
            new ProcessDef
            {
                // A pat of clay pressed over a curve into a roof tile; kiln-fired only (ceramics-10).
                Id = "clay.tile", Name = "Press a roof tile", Doing = "Pressing a roof tile", Tier = 3,
                Consumes = new[] { new Ingredient(Item.TemperedClay) },
                Seconds = Base(3, 0.75f), Minigame = Minigame.Shape, Station = StationKind.PottersBench, Outcome = Outcome.Placed, FailBelow = 0.15f,
            },
            new ProcessDef
            {
                // Too big for a campfire: it is fired in a kiln (ceramics-6).
                Id = "clay.storage_jar", Name = "Build a storage jar", Doing = "Coiling a storage jar", Tier = 3,
                Consumes = new[] { new Ingredient(Item.TemperedClay, ClayRules.JarClay) },
                Seconds = Base(3, 3f), Minigame = Minigame.Coil, Station = StationKind.PottersBench, Outcome = Outcome.Placed, FailBelow = 0.15f,
            },
            new ProcessDef
            {
                Id = "clay.brick", Name = "Mold a brick", Doing = "Molding a brick", Tier = 3,
                Consumes = new[] { new Ingredient(Item.CleanClay, 2), new Ingredient(Item.Fiber) },
                Seconds = Base(3, 0.5f), Outcome = Outcome.Placed,
            },
            new ProcessDef
            {
                Id = "clay.daub", Name = "Mix daub", Doing = "Mixing clay and fiber into daub", Tier = 3,
                Consumes = new[] { new Ingredient(Item.CleanClay), new Ingredient(Item.Fiber) },
                Seconds = Base(3, 0.25f), Makes = Item.Daub, MakesCount = 2,
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

        // How much faster a better tool works (D-089): a power curve over the tool's
        // quality (docs/PROGRESSION.md), about 7% at quality 35 and MaxToolSpeedup at 100.
        public const float MaxToolSpeedup = 0.15f;
        public const float ToolSpeedupPower = 0.73f;
        public static float ToolSpeedup(byte toolQuality) =>
            MaxToolSpeedup * MathF.Pow(Math.Clamp(toolQuality, (byte)0, (byte)100) / 100f, ToolSpeedupPower);

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
        // A freshly lit fire burns this long before it needs feeding (D-054, D-090): long
        // enough to shape, dry and fire a first pot without tending it.
        public const float StartSeconds = 20 * 60;
        // Fuel stacks up to this many seconds ahead. The cap grows with the feeder's
        // Firecraft rank, from MaxSeconds at rank 0 to MasterMaxSeconds at Master (D-090).
        public const float MaxSeconds = 20 * 60;
        public const float MasterMaxSeconds = 40 * 60;
        public static float MaxSecondsFor(int firecraftRank) =>
            Progression.Effect(EffectKind.FireSeconds, firecraftRank);
        // Warmth falls off to nothing at this distance, in meters.
        public const float WarmthRadius = 5f;
        // A burned-out fire leaves a cold hearth of stones and ash this long outside a
        // claim; inside one it stays while the claim stands (D-109). Lighting
        // a fire beside it rekindles it in place.
        public const float ColdSeconds = 60 * 60;
        // The stone ring's reach from the fire's middle, in meters, and how far apart
        // two fires (or a fire and a cold hearth) must be.
        public const float RingRadius = 0.6f;
        public const float Spacing = 1.5f;

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
