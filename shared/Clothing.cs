// What can be worn and how warm it keeps you (Animals lane). Clothing you carry
// is worn: the best piece in each slot counts. Insulation is the share of the
// cold's bite it takes away, so a tunic at 0.25 makes you lose warmth a quarter
// slower; pieces in different slots add up.
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public enum ClothingSlot : byte
    {
        Body,
        Feet,
    }

    public sealed class Wearable
    {
        public Item Item;
        public ClothingSlot Slot;
        public float Insulation;
    }

    public static class Clothing
    {
        // Never more than this, however well dressed: fire and shelter still matter.
        public const float MaxInsulation = 0.6f;

        public static readonly IReadOnlyDictionary<Item, Wearable> All = Build(
            new Wearable { Item = Item.WoolTunic, Slot = ClothingSlot.Body, Insulation = 0.25f },
            new Wearable { Item = Item.LeatherWraps, Slot = ClothingSlot.Feet, Insulation = 0.10f });

        public static Wearable? Find(ushort item) => All.TryGetValue((Item)item, out var w) ? w : null;

        // Total insulation from a set of carried items: the warmest piece per slot.
        public static float InsulationOf(IEnumerable<ushort> carried)
        {
            var best = new Dictionary<ClothingSlot, float>();
            foreach (var id in carried)
                if (Find(id) is { } w && (!best.TryGetValue(w.Slot, out var b) || w.Insulation > b))
                    best[w.Slot] = w.Insulation;
            float sum = 0;
            foreach (var v in best.Values) sum += v;
            return System.Math.Min(sum, MaxInsulation);
        }

        static Dictionary<Item, Wearable> Build(params Wearable[] list)
        {
            var map = new Dictionary<Item, Wearable>();
            foreach (var w in list) map[w.Item] = w;
            return map;
        }
    }
}
