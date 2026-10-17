// The bag and the tools section of the player doll (ui-18). Shared by the server module
// and the client; keep it free of engine types.
namespace LifeGame.Shared
{
    public static class Bag
    {
        // Places on the tools section (Justin picked 6); tools there work by themselves,
        // take no bag space and add no weight.
        public const int BeltSlots = 6;
        // Places in the bag a player can arrange things into. Carrying is limited by
        // weight (Carry.cs), not by places, so this is only a sane upper bound.
        public const int Places = 200;

        // Only made tools go on the belt, one per place: an item whose Def says it is a tool,
        // or one that wears with use (a bow drill). Raw stones and flakes stay in the bag,
        // so the weightless belt can't carry loot.
        public static bool BeltOk(Item item) =>
            Items.IsKnown((ushort)item) && (Items.UseOf(item) == UseAction.Tool || Items.Def(item).Wears);

        // The order Sort puts the bag in: food and drink, tools, clothes, then the rest.
        public static int Group(Item item) => Items.UseOf(item) switch
        {
            UseAction.Eat or UseAction.Drink => 0,
            UseAction.Tool => 1,
            UseAction.Wear => 2,
            _ => 3,
        };
    }
}
