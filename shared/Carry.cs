// How much a player can carry (Justin chose a carrying limit, D-042). Shared by
// the server module and the Godot client; keep it free of engine types.
using System;
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public static class Carry
    {
        // What you can carry with bare hands, in load units (roughly kilograms), and
        // what a woven basket in your inventory adds (Justin, D-100): only one basket
        // counts, leaving room for later carriers to climb slowly (D-075).
        public const float BaseLimit = 20f;
        public const float BasketBonus = 10f;
        // Crafting's woven basket.
        public const Item Basket = Item.Basket;

        // Over the limit you slow down, reaching this fraction of walking speed
        // at OverloadCap times the limit, and no slower beyond it.
        public const float SlowestWalk = 0.5f;
        public const float OverloadCap = 1.5f;

        // Load per item. Anything not listed weighs DefaultWeight.
        public const float DefaultWeight = 1f;
        static readonly Dictionary<Item, float> weights = new()
        {
            [Item.Stick] = 1f,
            [Item.Stone] = 2f,
            [Item.Fiber] = 0.2f,
            [Item.Clay] = 2f,
            [Item.Tinder] = 0.1f,
            [Item.Flake] = 0.3f,
            [Item.HandAxe] = 1f,
            [Item.Cordage] = 0.2f,
            [Item.Spear] = 1.5f,
            [Item.StoneAxe] = 2f,
            [Item.CleanClay] = 2f,
            [Item.Grit] = 1f,
            [Item.TemperedClay] = 2f,
            [Item.Potsherd] = 0.5f,
            [Item.Grog] = 1f,
            [Item.PinchPot] = 1f,
            [Item.CoilPot] = 2f,
            [Item.PinchPotOfWater] = 2f,
            [Item.CoilPotOfWater] = 4f,
            [Item.Brick] = 3f,
            [Item.Daub] = 2f,
            [Item.UnfiredPinchPot] = 1f,
            [Item.UnfiredCoilPot] = 2f,
            [Item.UnfiredStorageJar] = 8f,
            [Item.StorageJar] = 8f,
            [Item.PotOfBroth] = 4f,
            [Item.PotOfStew] = 4f,
            [Item.Berries] = 0.1f,
            [Basket] = 0.5f,
        };

        public static float Weight(Item item) => weights.TryGetValue(item, out var w) ? w : DefaultWeight;

        public static float Limit(bool hasBasket) => BaseLimit + (hasBasket ? BasketBonus : 0f);

        // Load and limit from (item, count) stacks.
        public static (float load, float limit) Of(IEnumerable<(Item item, uint count)> stacks)
        {
            float load = 0f;
            bool basket = false;
            foreach (var (item, count) in stacks)
            {
                load += Weight(item) * count;
                if (item == Basket && count > 0) basket = true;
            }
            return (load, Limit(basket));
        }

        // Walking speed as a fraction of normal: 1 up to the limit, easing down to
        // SlowestWalk at OverloadCap times the limit.
        public static float WalkFactor(float load, float limit)
        {
            if (load <= limit || limit <= 0f) return 1f;
            float over = Math.Clamp((load / limit - 1f) / (OverloadCap - 1f), 0f, 1f);
            return 1f - (1f - SlowestWalk) * over;
        }
    }
}
