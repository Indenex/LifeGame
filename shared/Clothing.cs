// What can be worn, in which slot, and how warm it keeps you (Animals lane).
// Clothes are put on in the equipment panel (D-074); only worn pieces count.
// Insulation is the share of the cold's bite a piece takes away: a tunic at 0.10
// makes you lose warmth 10% slower. Pieces in different slots add up. Numbers
// start low and climb slowly as better clothing arrives (D-075).
using System;
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public enum ClothingSlot : byte
    {
        Body = 0,
        Feet = 1,
    }

    public sealed class Wearable
    {
        public Item Item;
        public ClothingSlot Slot;
        // At full quality; a rough piece gives less (QualityShare).
        public float Insulation;
        public string Effect = "";
    }

    public static class Clothing
    {
        // Never more than this, however well dressed: fire and shelter still matter.
        public const float MaxInsulation = 0.6f;
        // A piece at quality 0 gives this share of its insulation, rising to all of it at 100.
        public const float MinQualityShare = 0.7f;

        public static readonly IReadOnlyDictionary<Item, Wearable> All = Build(
            new Wearable { Item = Item.WoolTunic, Slot = ClothingSlot.Body, Insulation = 0.10f, Effect = "Keeps a little of the cold off" },
            new Wearable { Item = Item.LeatherWraps, Slot = ClothingSlot.Feet, Insulation = 0.04f, Effect = "Keeps your feet off the cold ground" });

        public static Wearable? Find(ushort item) => All.TryGetValue((Item)item, out var w) ? w : null;

        public static float QualityShare(byte quality) =>
            MinQualityShare + (1f - MinQualityShare) * Math.Clamp(quality / 100f, 0f, 1f);

        // Insulation of one piece at a quality.
        public static float InsulationOf(ushort item, byte quality) =>
            Find(item) is { } w ? w.Insulation * QualityShare(quality) : 0f;

        // Total from the worn pieces, one per slot.
        public static float InsulationOf(IEnumerable<(ushort item, byte quality)> worn)
        {
            float sum = 0;
            foreach (var (item, quality) in worn) sum += InsulationOf(item, quality);
            return Math.Min(sum, MaxInsulation);
        }

        // "10% less cold" for the panel.
        public static string Percent(ushort item, byte quality) =>
            $"{MathF.Round(InsulationOf(item, quality) * 100f)}% less cold";

        static Dictionary<Item, Wearable> Build(params Wearable[] list)
        {
            var map = new Dictionary<Item, Wearable>();
            foreach (var w in list) map[w.Item] = w;
            return map;
        }
    }
}
