// What can be eaten or drunk, as data (docs/CONTENT.md, Food row). A new food is one
// FoodDef entry (plus a model and a Journal line); no switch to extend. Stats come from
// the constants in Needs.cs. Shared by the server module and the Godot client.
using System.Collections.Generic;
using System.Linq;

namespace LifeGame.Shared
{
    public sealed class FoodDef
    {
        public Item Id;
        public float Food;
        public float Water;
        // A cooked meal, which lets you run longer for a while (D-111). Berries and
        // burnt food don't count.
        public bool GoodMeal;
        // The container a food is eaten from, which goes back in your hands; null for none.
        public Item? LeavesBehind;
    }

    // A carried pot of water: drinking it quenches Water and gives back the empty pot.
    public sealed class PotDef
    {
        public Item Full;
        public Item Empty;
        public float Water;
    }

    // A raw food that roasts on a fire (Roasting timing): what it becomes when done well,
    // when left on too long, and which skill process it trains. A new roastable is one
    // entry here plus its recipe.
    public sealed class RoastDef
    {
        public Item Raw;
        public Item Cooked;
        public Item Burnt;
        public string Process = "";
        // How the recipe reads: "Roast mutton", "Setting mutton over the fire".
        public string Name = "";
        public string Doing = "";
    }

    public static class Foods
    {
        public static readonly IReadOnlyList<FoodDef> All = new[]
        {
            new FoodDef { Id = Item.Berries, Food = Needs.BerryFood, Water = Needs.BerryWater },
            new FoodDef { Id = Item.Hazelnuts, Food = Needs.HazelnutFood },
            new FoodDef { Id = Item.Mushrooms, Food = Needs.MushroomFood, Water = Needs.MushroomWater },
            new FoodDef { Id = Item.CookedMutton, Food = Needs.RoastMuttonFood, GoodMeal = true },
            new FoodDef { Id = Item.BurntMutton, Food = Needs.BurntMuttonFood },
            // Venison (animals-14) feeds like mutton for now: same numbers as the roast.
            new FoodDef { Id = Item.CookedVenison, Food = Needs.RoastMuttonFood, GoodMeal = true },
            new FoodDef { Id = Item.BurntVenison, Food = Needs.BurntMuttonFood },
            new FoodDef { Id = Item.Egg, Food = Needs.EggFood },
            new FoodDef { Id = Item.BoiledEgg, Food = Needs.BoiledEggFood },
            new FoodDef { Id = Item.BurntEgg, Food = Needs.BurntEggFood },
            new FoodDef { Id = Item.CookedChicken, Food = Needs.RoastChickenFood, GoodMeal = true },
            new FoodDef { Id = Item.BurntChicken, Food = Needs.BurntChickenFood },
            new FoodDef { Id = Item.CookedRabbit, Food = Needs.RoastRabbitFood, GoodMeal = true },
            new FoodDef { Id = Item.BurntRabbit, Food = Needs.BurntRabbitFood },
            new FoodDef { Id = Item.PotOfBroth, Food = Needs.BrothFood, Water = Needs.BrothWater, GoodMeal = true, LeavesBehind = Item.CoilPot },
            new FoodDef { Id = Item.PotOfStew, Food = Needs.StewFood, Water = Needs.StewWater, GoodMeal = true, LeavesBehind = Item.CoilPot },
        };

        // Smallest first, so the big pot is saved for later.
        public static readonly IReadOnlyList<PotDef> Pots = new[]
        {
            new PotDef { Full = Item.WaterskinOfWater, Empty = Item.Waterskin, Water = Needs.WaterskinWater },
            new PotDef { Full = Item.PinchPotOfWater, Empty = Item.PinchPot, Water = Needs.PinchPotWater },
            new PotDef { Full = Item.CoilPotOfWater, Empty = Item.CoilPot, Water = Needs.CoilPotWater },
        };

        public static readonly IReadOnlyList<RoastDef> Roasts = new[]
        {
            new RoastDef { Raw = Item.RawMutton, Cooked = Item.CookedMutton, Burnt = Item.BurntMutton, Process = "cook.meat", Name = "Roast mutton", Doing = "Setting mutton over the fire" },
            new RoastDef { Raw = Item.RawVenison, Cooked = Item.CookedVenison, Burnt = Item.BurntVenison, Process = "cook.venison", Name = "Roast venison", Doing = "Setting venison over the fire" },
            new RoastDef { Raw = Item.RawRabbit, Cooked = Item.CookedRabbit, Burnt = Item.BurntRabbit, Process = "cook.rabbit", Name = "Roast rabbit", Doing = "Setting a rabbit over the fire" },
            new RoastDef { Raw = Item.RawChicken, Cooked = Item.CookedChicken, Burnt = Item.BurntChicken, Process = "cook.chicken", Name = "Roast chicken", Doing = "Setting a chicken over the fire" },
            new RoastDef { Raw = Item.Egg, Cooked = Item.BoiledEgg, Burnt = Item.BurntEgg, Process = "cook.egg", Name = "Boil an egg", Doing = "Setting an egg in the embers" },
        };

        static readonly Dictionary<Item, RoastDef> RoastByRaw = Roasts.ToDictionary(r => r.Raw);
        static readonly Dictionary<Item, FoodDef> ById = All.ToDictionary(f => f.Id);
        static readonly Dictionary<Item, PotDef> PotById = Pots.ToDictionary(p => p.Full);
        static readonly Dictionary<Item, PotDef> PotByEmpty = Pots.ToDictionary(p => p.Empty);

        public static FoodDef? Find(Item item) => ById.TryGetValue(item, out var f) ? f : null;

        public static RoastDef? FindRoast(Item raw) => RoastByRaw.TryGetValue(raw, out var r) ? r : null;

        // The full water container an empty one becomes when filled at the water, or null.
        public static Item? FilledFrom(Item empty) => PotByEmpty.TryGetValue(empty, out var p) ? p.Full : null;

        public static PotDef? FindPot(Item item) => PotById.TryGetValue(item, out var p) ? p : null;
    }
}
