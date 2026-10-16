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

    public static class Foods
    {
        public static readonly IReadOnlyList<FoodDef> All = new[]
        {
            new FoodDef { Id = Item.Berries, Food = Needs.BerryFood, Water = Needs.BerryWater },
            new FoodDef { Id = Item.CookedMutton, Food = Needs.RoastMuttonFood, GoodMeal = true },
            new FoodDef { Id = Item.BurntMutton, Food = Needs.BurntMuttonFood },
            // Venison (animals-14) feeds like mutton for now: same numbers as the roast.
            new FoodDef { Id = Item.CookedVenison, Food = Needs.RoastMuttonFood, GoodMeal = true },
            new FoodDef { Id = Item.BurntVenison, Food = Needs.BurntMuttonFood },
            new FoodDef { Id = Item.PotOfBroth, Food = Needs.BrothFood, Water = Needs.BrothWater, GoodMeal = true, LeavesBehind = Item.CoilPot },
            new FoodDef { Id = Item.PotOfStew, Food = Needs.StewFood, Water = Needs.StewWater, GoodMeal = true, LeavesBehind = Item.CoilPot },
        };

        // Smallest first, so the big pot is saved for later.
        public static readonly IReadOnlyList<PotDef> Pots = new[]
        {
            new PotDef { Full = Item.PinchPotOfWater, Empty = Item.PinchPot, Water = Needs.PinchPotWater },
            new PotDef { Full = Item.CoilPotOfWater, Empty = Item.CoilPot, Water = Needs.CoilPotWater },
        };

        static readonly Dictionary<Item, FoodDef> ById = All.ToDictionary(f => f.Id);
        static readonly Dictionary<Item, PotDef> PotById = Pots.ToDictionary(p => p.Full);

        public static FoodDef? Find(Item item) => ById.TryGetValue(item, out var f) ? f : null;

        public static PotDef? FindPot(Item item) => PotById.TryGetValue(item, out var p) ? p : null;
    }
}
