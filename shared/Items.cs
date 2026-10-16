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

        // House building, 130-159.
        SleepingMat = 130,

        // Survival
        Berries = 160,

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

        public string Count(uint n) => n == 1 ? Name : Plural;
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

        public static readonly IReadOnlyDictionary<Item, ItemDef> All = Build(
            new ItemDef { Id = Item.Stick, Name = "Stick", Plural = "Sticks", FuelSeconds = 90 },
            new ItemDef { Id = Item.Stone, Name = "Stone", Plural = "Stones", Tags = ItemTags.Hammer },
            new ItemDef { Id = Item.Fiber, Name = "Plant fiber", Plural = "Plant fiber", FuelSeconds = 15 },
            new ItemDef { Id = Item.Clay, Name = "Clay", Plural = "Clay" },
            new ItemDef { Id = Item.Tinder, Name = "Tinder bundle", Plural = "Tinder bundles", FuelSeconds = 10, Tags = ItemTags.Kindling },
            new ItemDef { Id = Item.Flake, Name = "Stone flake", Plural = "Stone flakes", Tags = ItemTags.Cutting, HasQuality = true },
            new ItemDef { Id = Item.HandAxe, Name = "Hand axe", Plural = "Hand axes", Tags = ItemTags.Cutting | ItemTags.Hammer, HasQuality = true },
            new ItemDef { Id = Item.Cordage, Name = "Cordage", Plural = "Cordage", Tags = ItemTags.Binding, HasQuality = true },
            new ItemDef { Id = Item.Spear, Name = "Spear", Plural = "Spears", Tags = ItemTags.Piercing, HasQuality = true },
            new ItemDef { Id = Item.StoneAxe, Name = "Stone axe", Plural = "Stone axes", Tags = ItemTags.Chopping | ItemTags.Cutting, HasQuality = true },
            new ItemDef { Id = Item.CleanClay, Name = "Clean clay", Plural = "Clean clay" },
            new ItemDef { Id = Item.Grit, Name = "Stone grit", Plural = "Stone grit" },
            new ItemDef { Id = Item.TemperedClay, Name = "Tempered clay", Plural = "Tempered clay" },
            new ItemDef { Id = Item.Potsherd, Name = "Potsherd", Plural = "Potsherds" },
            new ItemDef { Id = Item.Grog, Name = "Grog", Plural = "Grog" },
            new ItemDef { Id = Item.PinchPot, Name = "Pinch pot", Plural = "Pinch pots", HasQuality = true },
            new ItemDef { Id = Item.CoilPot, Name = "Coil pot", Plural = "Coil pots", HasQuality = true },
            new ItemDef { Id = Item.PinchPotOfWater, Name = "Pinch pot of water", Plural = "Pinch pots of water", HasQuality = true },
            new ItemDef { Id = Item.CoilPotOfWater, Name = "Coil pot of water", Plural = "Coil pots of water", HasQuality = true },
            new ItemDef { Id = Item.Brick, Name = "Sun-dried brick", Plural = "Sun-dried bricks" },
            new ItemDef { Id = Item.Daub, Name = "Daub", Plural = "Daub" },
            new ItemDef { Id = Item.UnfiredPinchPot, Name = "Unfired pinch pot", Plural = "Unfired pinch pots", HasQuality = true },
            new ItemDef { Id = Item.UnfiredCoilPot, Name = "Unfired coil pot", Plural = "Unfired coil pots", HasQuality = true },
            new ItemDef { Id = Item.StoneKnife, Name = "Stone knife", Plural = "Stone knives", Tags = ItemTags.Cutting, HasQuality = true },
            new ItemDef { Id = Item.DiggingStick, Name = "Digging stick", Plural = "Digging sticks", Tags = ItemTags.Digging, HasQuality = true },
            new ItemDef { Id = Item.Basket, Name = "Basket", Plural = "Baskets", Tags = ItemTags.Container, HasQuality = true },
            new ItemDef { Id = Item.BowDrill, Name = "Bow drill", Plural = "Bow drills", Tags = ItemTags.FireBow, HasQuality = true },
            new ItemDef { Id = Item.Log, Name = "Log", Plural = "Logs", FuelSeconds = 360 },
            new ItemDef { Id = Item.SleepingMat, Name = "Sleeping mat", Plural = "Sleeping mats" },
            new ItemDef { Id = Item.Berries, Name = "Berries", Plural = "Berries" },
            new ItemDef { Id = Item.RawMutton, Name = "Raw mutton", Plural = "Raw mutton", HasQuality = true },
            new ItemDef { Id = Item.CookedMutton, Name = "Roast mutton", Plural = "Roast mutton", HasQuality = true },
            new ItemDef { Id = Item.BurntMutton, Name = "Burnt mutton", Plural = "Burnt mutton" },
            new ItemDef { Id = Item.Wool, Name = "Raw wool", Plural = "Raw wool", HasQuality = true },
            new ItemDef { Id = Item.SheepHide, Name = "Sheep hide", Plural = "Sheep hides", HasQuality = true },
            new ItemDef { Id = Item.ScrapedHide, Name = "Scraped hide", Plural = "Scraped hides", HasQuality = true },
            new ItemDef { Id = Item.Leather, Name = "Leather", Plural = "Leather", HasQuality = true },
            new ItemDef { Id = Item.DropSpindle, Name = "Drop spindle", Plural = "Drop spindles", Tags = ItemTags.Spindle, HasQuality = true },
            new ItemDef { Id = Item.Yarn, Name = "Wool yarn", Plural = "Wool yarn", HasQuality = true },
            new ItemDef { Id = Item.WoolCloth, Name = "Wool cloth", Plural = "Wool cloth", HasQuality = true });

        public static ItemDef Def(Item item) => All[item];

        public static bool IsKnown(ushort id) => All.ContainsKey((Item)id);

        static Dictionary<Item, ItemDef> Build(params ItemDef[] defs)
        {
            var map = new Dictionary<Item, ItemDef>();
            foreach (var def in defs) map[def.Id] = def;
            return map;
        }
    }
}
