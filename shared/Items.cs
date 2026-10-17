// Every material and made thing in the game, shared by the server and the client.
// Tables store an item as its numeric id, so new items only ever get new numbers:
// never renumber or reuse one.
//
// Id ranges per lane, so lanes can add items in parallel without colliding:
//   Crafting 1-99, Ceramics 100-129, House building 130-159, Survival 160-189,
//   Animals 190-219, Claims 220-239, Journal and skills 240-259.
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public enum Item : ushort
    {
        Stick = 1,
        Stone = 2,
        Fiber = 3,
        Clay = 4,
        Tinder = 5,
        Flake = 6,
        HandAxe = 7,
        Cordage = 8,
        Spear = 9,
        StoneAxe = 10,
        StoneKnife = 11,
        DiggingStick = 12,
        Basket = 13,
        BowDrill = 14,
        Log = 15,
        // Bow and arrow (animals-15): ids reserved here so Modeling and Animals can build on them.
        Bow = 16,
        Arrow = 17,

        // Clay and ceramics (tier 3) use 100 to 129.
        CleanClay = 100,
        Grit = 101,
        TemperedClay = 102,
        Potsherd = 103,
        Grog = 104,
        PinchPot = 105,
        CoilPot = 106,
        PinchPotOfWater = 107,
        CoilPotOfWater = 108,
        Brick = 109,
        Daub = 110,
        UnfiredPinchPot = 111,
        UnfiredCoilPot = 112,
        UnfiredStorageJar = 113,
        StorageJar = 114,
        PotOfBroth = 115,
        PotOfStew = 116,
        UnfiredLamp = 117,
        ClayLamp = 118,
        // Rendered from mutton fat in a pot (ceramics-9); lamp fuel.
        Tallow = 119,
        // Roof tiles (ceramics-10): shaped at the bench, kiln-fired only; House building roofs with them.
        UnfiredRoofTile = 120,
        RoofTile = 121,

        // House building, 130-159.
        SleepingMat = 130,

        // Survival
        Berries = 160,
        // A hide waterskin (survival-20): carries a little water before pottery.
        Waterskin = 161,
        WaterskinOfWater = 162,

        // Animals (190-219); mutton is cooked by Crafting
        RawMutton = 190,
        CookedMutton = 191,
        BurntMutton = 192,
        Wool = 193,
        SheepHide = 194,
        ScrapedHide = 195,
        Leather = 196,
        DropSpindle = 197,
        Yarn = 198,
        WoolCloth = 199,
        WoolTunic = 200,
        LeatherWraps = 201,
        RawVenison = 202,
        DeerHide = 203,
        Antler = 204,
        CookedVenison = 205,
        BurntVenison = 206,
        Egg = 207,
        RawChicken = 208,
        CookedChicken = 209,
        BurntChicken = 210,
        Feather = 211,
    }

    // What a material is and does. Properties drive processes rather than special
    // cases: anything with FuelSeconds burns, anything tagged Hammer can knap.
    public sealed class ItemDef
    {
        public Item Id;
        public string Name = "";
        public string Plural = "";
        // Seconds a fire burns on one of these; zero if it does not burn.
        public float FuelSeconds;
        public ItemTags Tags;
        // Quality matters for made things; raw materials always come at a flat quality.
        public bool HasQuality;
        // Shooting (animals-15): what a bow or an arrow is worth in the damage formula,
        // bowPower x arrowPower x quality terms. Zero for everything else.
        public float BowPower, ArrowPower;
        // A thing that sits visibly in the world, so it can be set down where you choose
        // (crafting-21). Small things like tinder and cordage just go to the bag.
        public bool Placeable;
        // Hand tools wear with use and stop working until mended at a repair table
        // (crafting-19, shared/Durability.cs). A wearing tool needs HasQuality. Mending
        // takes RepairCost besides the visit, and RepairNeedsTool in hand (a cutting tool).
        public bool Wears;
        public Ingredient[]? RepairCost;
        public ItemTags RepairNeedsTool;
        // What pressing its hotbar key does (ui-19). Clothing wears by Clothing.All,
        // so garments leave this at None; see Items.UseOf.
        public UseAction Use;

        public string Count(uint n) => n == 1 ? Name : Plural;
    }

    // What using an item does (its hotbar key, a double-click in the bag); the server's
    // UseItem runs the matching rule.
    public enum UseAction : byte
    {
        None = 0,
        Eat = 1,
        Drink = 2,
        Wear = 3,
        // A tool: it works from the tools section of the player doll (ui-18), or from the bag.
        Tool = 4,
    }

    [System.Flags]
    public enum ItemTags
    {
        None = 0,
        Hammer = 1,     // can strike stone
        Cutting = 2,    // has a sharp edge
        Chopping = 4,   // heavy edge for wood
        Piercing = 8,   // a point
        Binding = 16,   // ties things together
        Kindling = 32,  // catches a spark
        Digging = 64,   // breaks ground faster than hands
        Container = 128, // carries loose things
        FireBow = 256,   // spins a fire drill with a bowstring
        Spindle = 512,   // twists loose fibre into yarn
    }

    public static class Items
    {
        // Raw materials picked up or dug start at this quality (0 to 100).
        public const byte RawQuality = 50;

        // Mending a lashed tool takes a length of fresh cordage.
        static readonly Ingredient[] Lashing = { new Ingredient(Item.Cordage) };

        public static readonly IReadOnlyDictionary<Item, ItemDef> All = Build(
            new ItemDef { Id = Item.Stick, Name = "Stick", Plural = "Sticks", FuelSeconds = 90 },
            new ItemDef { Id = Item.Stone, Name = "Stone", Plural = "Stones", Tags = ItemTags.Hammer },
            new ItemDef { Id = Item.Fiber, Name = "Plant fiber", Plural = "Plant fiber", FuelSeconds = 15 },
            new ItemDef { Id = Item.Clay, Name = "Clay", Plural = "Clay" },
            new ItemDef { Id = Item.Tinder, Name = "Tinder bundle", Plural = "Tinder bundles", FuelSeconds = 10, Tags = ItemTags.Kindling },
            new ItemDef { Id = Item.Flake, Name = "Stone flake", Plural = "Stone flakes", Tags = ItemTags.Cutting, HasQuality = true },
            new ItemDef { Id = Item.HandAxe, Name = "Hand axe", Plural = "Hand axes", Tags = ItemTags.Cutting | ItemTags.Hammer, HasQuality = true, Use = UseAction.Tool },
            new ItemDef { Id = Item.Cordage, Name = "Cordage", Plural = "Cordage", Tags = ItemTags.Binding, HasQuality = true },
            new ItemDef { Id = Item.Spear, Name = "Spear", Plural = "Spears", Tags = ItemTags.Piercing, HasQuality = true, Wears = true, RepairCost = Lashing, Use = UseAction.Tool },
            new ItemDef { Id = Item.StoneAxe, Name = "Stone axe", Plural = "Stone axes", Tags = ItemTags.Chopping | ItemTags.Cutting, HasQuality = true, Wears = true, RepairCost = Lashing, Use = UseAction.Tool },
            new ItemDef { Id = Item.CleanClay, Name = "Clean clay", Plural = "Clean clay" },
            new ItemDef { Id = Item.Grit, Name = "Stone grit", Plural = "Stone grit" },
            new ItemDef { Id = Item.TemperedClay, Name = "Tempered clay", Plural = "Tempered clay" },
            new ItemDef { Id = Item.Potsherd, Name = "Potsherd", Plural = "Potsherds" },
            new ItemDef { Id = Item.Grog, Name = "Grog", Plural = "Grog" },
            new ItemDef { Id = Item.PinchPot, Name = "Pinch pot", Plural = "Pinch pots", HasQuality = true, Placeable = true },
            new ItemDef { Id = Item.CoilPot, Name = "Coil pot", Plural = "Coil pots", HasQuality = true, Placeable = true },
            new ItemDef { Id = Item.Waterskin, Name = "Waterskin", Plural = "Waterskins", HasQuality = true },
            new ItemDef { Id = Item.WaterskinOfWater, Name = "Waterskin of water", Plural = "Waterskins of water", HasQuality = true, Use = UseAction.Drink },
            new ItemDef { Id = Item.PinchPotOfWater, Name = "Pinch pot of water", Plural = "Pinch pots of water", HasQuality = true, Use = UseAction.Drink },
            new ItemDef { Id = Item.CoilPotOfWater, Name = "Coil pot of water", Plural = "Coil pots of water", HasQuality = true, Use = UseAction.Drink },
            new ItemDef { Id = Item.Brick, Name = "Sun-dried brick", Plural = "Sun-dried bricks", Placeable = true },
            new ItemDef { Id = Item.Daub, Name = "Daub", Plural = "Daub" },
            new ItemDef { Id = Item.UnfiredPinchPot, Name = "Unfired pinch pot", Plural = "Unfired pinch pots", HasQuality = true },
            new ItemDef { Id = Item.UnfiredCoilPot, Name = "Unfired coil pot", Plural = "Unfired coil pots", HasQuality = true },
            new ItemDef { Id = Item.UnfiredStorageJar, Name = "Unfired storage jar", Plural = "Unfired storage jars", HasQuality = true },
            new ItemDef { Id = Item.StorageJar, Name = "Storage jar", Plural = "Storage jars", HasQuality = true, Placeable = true },
            new ItemDef { Id = Item.PotOfBroth, Name = "Pot of berry broth", Plural = "Pots of berry broth", HasQuality = true, Use = UseAction.Eat },
            new ItemDef { Id = Item.PotOfStew, Name = "Pot of mutton stew", Plural = "Pots of mutton stew", HasQuality = true, Use = UseAction.Eat },
            new ItemDef { Id = Item.UnfiredLamp, Name = "Unfired lamp", Plural = "Unfired lamps", HasQuality = true },
            new ItemDef { Id = Item.ClayLamp, Name = "Clay lamp", Plural = "Clay lamps", HasQuality = true },
            new ItemDef { Id = Item.UnfiredRoofTile, Name = "Unfired roof tile", Plural = "Unfired roof tiles", HasQuality = true },
            new ItemDef { Id = Item.RoofTile, Name = "Roof tile", Plural = "Roof tiles", HasQuality = true },
            new ItemDef { Id = Item.Tallow, Name = "Tallow", Plural = "Tallow" },
            new ItemDef { Id = Item.StoneKnife, Name = "Stone knife", Plural = "Stone knives", Tags = ItemTags.Cutting, HasQuality = true, Wears = true, RepairCost = Lashing, Use = UseAction.Tool },
            new ItemDef { Id = Item.DiggingStick, Name = "Digging stick", Plural = "Digging sticks", Tags = ItemTags.Digging, HasQuality = true, Wears = true, RepairNeedsTool = ItemTags.Cutting, Use = UseAction.Tool },
            new ItemDef { Id = Item.Basket, Name = "Basket", Plural = "Baskets", Tags = ItemTags.Container, HasQuality = true, Placeable = true },
            new ItemDef { Id = Item.BowDrill, Name = "Bow drill", Plural = "Bow drills", Tags = ItemTags.FireBow, HasQuality = true, Wears = true, RepairCost = Lashing },
            new ItemDef { Id = Item.Log, Name = "Log", Plural = "Logs", FuelSeconds = 360, Placeable = true },
            new ItemDef { Id = Item.Bow, Name = "Bow", Plural = "Bows", HasQuality = true, BowPower = 60f, Wears = true, RepairCost = Lashing, Use = UseAction.Tool },
            new ItemDef { Id = Item.Arrow, Name = "Arrow", Plural = "Arrows", HasQuality = true, ArrowPower = 0.65f },
            new ItemDef { Id = Item.SleepingMat, Name = "Sleeping mat", Plural = "Sleeping mats" },
            new ItemDef { Id = Item.Berries, Name = "Berries", Plural = "Berries", Use = UseAction.Eat },
            new ItemDef { Id = Item.RawMutton, Name = "Raw mutton", Plural = "Raw mutton", HasQuality = true },
            new ItemDef { Id = Item.CookedMutton, Name = "Roast mutton", Plural = "Roast mutton", HasQuality = true, Use = UseAction.Eat },
            new ItemDef { Id = Item.BurntMutton, Name = "Burnt mutton", Plural = "Burnt mutton", Use = UseAction.Eat },
            new ItemDef { Id = Item.Wool, Name = "Raw wool", Plural = "Raw wool", HasQuality = true },
            new ItemDef { Id = Item.SheepHide, Name = "Sheep hide", Plural = "Sheep hides", HasQuality = true },
            new ItemDef { Id = Item.ScrapedHide, Name = "Scraped hide", Plural = "Scraped hides", HasQuality = true },
            new ItemDef { Id = Item.Leather, Name = "Leather", Plural = "Leather", HasQuality = true },
            new ItemDef { Id = Item.DropSpindle, Name = "Drop spindle", Plural = "Drop spindles", Tags = ItemTags.Spindle, HasQuality = true },
            new ItemDef { Id = Item.Yarn, Name = "Wool yarn", Plural = "Wool yarn", HasQuality = true },
            new ItemDef { Id = Item.WoolCloth, Name = "Wool cloth", Plural = "Wool cloth", HasQuality = true },
            new ItemDef { Id = Item.WoolTunic, Name = "Wool tunic", Plural = "Wool tunics", HasQuality = true },
            new ItemDef { Id = Item.LeatherWraps, Name = "Leather foot wraps", Plural = "Leather foot wraps", HasQuality = true },
            new ItemDef { Id = Item.RawVenison, Name = "Raw venison", Plural = "Raw venison", HasQuality = true },
            new ItemDef { Id = Item.DeerHide, Name = "Deer hide", Plural = "Deer hides", HasQuality = true },
            new ItemDef { Id = Item.Antler, Name = "Antler", Plural = "Antlers", HasQuality = true },
            new ItemDef { Id = Item.CookedVenison, Name = "Roast venison", Plural = "Roast venison", HasQuality = true },
            new ItemDef { Id = Item.BurntVenison, Name = "Burnt venison", Plural = "Burnt venison" },
            new ItemDef { Id = Item.Egg, Name = "Egg", Plural = "Eggs", HasQuality = true },
            new ItemDef { Id = Item.RawChicken, Name = "Raw chicken", Plural = "Raw chicken", HasQuality = true },
            new ItemDef { Id = Item.Feather, Name = "Feather", Plural = "Feathers", HasQuality = true },
            new ItemDef { Id = Item.CookedChicken, Name = "Roast chicken", Plural = "Roast chicken", HasQuality = true },
            new ItemDef { Id = Item.BurntChicken, Name = "Burnt chicken", Plural = "Burnt chicken" });

        public static ItemDef Def(Item item) => All[item];

        public static bool IsKnown(ushort id) => All.ContainsKey((Item)id);

        // What using one does: a garment is worn, everything else says on its Def.
        public static UseAction UseOf(Item item) =>
            Clothing.Find((ushort)item) != null ? UseAction.Wear
            : All.TryGetValue(item, out var def) ? def.Use : UseAction.None;

        static Dictionary<Item, ItemDef> Build(params ItemDef[] defs)
        {
            var map = new Dictionary<Item, ItemDef>();
            foreach (var def in defs) map[def.Id] = def;
            return map;
        }
    }
}
