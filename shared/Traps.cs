// Traps that catch animals (animals-34, docs/CONTENT.md Trap row). A trap is a placeable item set on the
// ground; a TrapDef says which animals it takes and how fast, how many catches it lasts and how far its
// reach is. A new trap (a bird snare, a fish trap) is an item, a recipe and one TrapDef entry.
// Keep this file free of engine and SpacetimeDB types.
using System.Collections.Generic;
using System.Linq;

namespace LifeGame.Shared
{
    public sealed class TrapDef
    {
        public Item Item;
        // Which animals it takes: AnimalDefs ids.
        public byte[] Targets = System.Array.Empty<byte>();
        // How near an animal must come to the trap, in metres.
        public float Reach = 1f;
        // The chance each animal tick (half a second) that one animal within reach is caught.
        // Rabbits cross the reach in a second or two and graze beside it for longer, so about
        // one crossing in ten is caught, and one that lingers for five seconds is caught about
        // as often as not. Start low (D-075).
        public float CatchPerTick = 0.05f;
        // How many catches it lasts: it falls apart after the last one. Its use so far is counted in the
        // item's Wear (the low 16 bits, as for tools), so setting it down again does not reset it.
        public int Catches = 6;
        // What the owner does to take the catch, for the prompt.
        public string TakeText = "Take the catch";
    }

    public static class Traps
    {
        public static readonly IReadOnlyList<TrapDef> All = new[]
        {
            new TrapDef { Item = Item.RabbitSnare, Targets = new[] { AnimalDefs.Rabbit }, TakeText = "Take the rabbit" },
        };

        static readonly Dictionary<Item, TrapDef> ByItem = All.ToDictionary(t => t.Item);

        public static TrapDef? Find(Item item) => ByItem.TryGetValue(item, out var t) ? t : null;

        public static TrapDef? Find(ushort item) => Items.IsKnown(item) ? Find((Item)item) : null;
    }
}
