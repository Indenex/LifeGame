// Tool wear and repair (crafting-19, D-155), shared by the server module and the client.
// Hand tools wear with use; stations and structures never do. A spent tool stops
// working until it is repaired at a repair table. Repair always costs less than a new
// tool, and loses a little quality each time, down to half the tool's original quality.
// Every number here is a tunable starting value; players only ever see the words.
// Keep this file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    public static class Durability
    {
        // How many uses a tool lasts between repairs, by quality (package A, "Steady"):
        // about 150 at quality 35, 265 at 65 and 400 at 100.
        public const float LifeAtZero = 15f;
        public const float LifePerQuality = 3.85f;

        // The share of the remaining distance to the quality floor lost on each repair.
        public const float RepairLossShare = 0.2f;
        // A repaired tool never falls below this share of its original quality.
        public const float QualityFloor = 0.5f;

        // "Worn" shows with this share of life left, "nearly spent" with this share.
        public const float WornBelow = 0.25f;
        public const float NearlySpentBelow = 0.08f;

        public static int Life(byte quality) => (int)MathF.Round(LifeAtZero + LifePerQuality * quality);

        // Wear lives in one uint on every item row: the uses so far in the low 16 bits,
        // and the quality the tool was made at (0 = never repaired) in the next 8.
        public static uint Pack(int used, byte origin) => (uint)Math.Clamp(used, 0, ushort.MaxValue) | ((uint)origin << 16);
        public static int Used(uint wear) => (int)(wear & 0xFFFF);
        public static byte Origin(uint wear) => (byte)((wear >> 16) & 0xFF);

        // True for the tools that wear: the ones put together from a handle and lashing
        // (ItemDef.Wears). Loose flakes, hand axes and plain stones are one stone each and never wear.
        public static bool Wears(Item item) => Items.IsKnown((ushort)item) && Items.Def(item).Wears;

        public static bool IsSpent(byte quality, uint wear) => Used(wear) >= Life(quality);

        // The share of life left, 0 to 1.
        public static float Left(byte quality, uint wear) =>
            Math.Clamp(1f - Used(wear) / (float)Math.Max(1, Life(quality)), 0f, 1f);

        // A word for the tool's state, or "" while it is as good as new.
        public static string Condition(byte quality, uint wear)
        {
            float left = Left(quality, wear);
            if (left <= 0f) return "spent";
            if (left <= NearlySpentBelow) return "nearly spent";
            if (left <= WornBelow) return "worn";
            return "";
        }

        // The quality a tool comes back at after a repair. The original is the quality it
        // was made at, which a repaired tool remembers in its wear.
        public static byte RepairedQuality(byte quality, uint wear)
        {
            byte origin = Origin(wear) == 0 ? quality : Origin(wear);
            float floor = origin * QualityFloor;
            float loss = MathF.Max(0f, quality - floor) * RepairLossShare;
            return (byte)Math.Clamp((int)MathF.Round(quality - loss, MidpointRounding.AwayFromZero), 1, 100);
        }

        // The wear after a repair: no uses so far, the original quality kept.
        public static uint Repaired(byte quality, uint wear) =>
            Pack(0, Origin(wear) == 0 ? quality : Origin(wear));

        // What a repair takes besides a visit to the table (ItemDef.RepairCost): mostly fresh
        // lashing, always less than the tool costs new. A digging stick only needs sharpening,
        // which takes a cutting tool (ItemDef.RepairNeedsTool) and nothing else.
        public static Ingredient[] RepairCost(Item item) => Items.Def(item).RepairCost ?? Array.Empty<Ingredient>();

        public static ItemTags RepairNeedsTool(Item item) => Items.Def(item).RepairNeedsTool;
    }
}
