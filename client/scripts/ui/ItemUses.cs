using System.Collections.Generic;
using System.Linq;
using LifeGame.Shared;

namespace LifeGame;

// What an item is for, in a few words, for the bag's tooltip (ui-64). Read from the shared
// item data (Foods, Clothing, ItemDef tags and fuel), so a new item says what it does with
// no extra code. Words rather than numbers, except how long a thing burns.
public static class ItemUses
{
    static readonly (ItemTags Tag, string Words)[] Tags =
    {
        (ItemTags.Chopping, "chops wood"),
        (ItemTags.Cutting, "cuts"),
        (ItemTags.Hammer, "strikes stone"),
        (ItemTags.Piercing, "pierces"),
        (ItemTags.Digging, "digs faster than hands"),
        (ItemTags.Binding, "ties things together"),
        (ItemTags.Kindling, "catches a spark"),
        (ItemTags.Container, "holds loose things"),
        (ItemTags.FireBow, "spins a fire drill"),
        (ItemTags.Spindle, "spins fibre into yarn"),
    };

    public static List<string> Lines(Item item)
    {
        var lines = new List<string>();
        if (Foods.Find(item) is { } food)
        {
            string eat = "Eat: " + Filling(food.Food);
            if (food.Water >= 0.1f) eat += ", and a drink";
            lines.Add(eat);
            if (food.GoodMeal) lines.Add("A good meal: you can run longer for a while");
        }
        if (Foods.FindPot(item) is { } pot && pot.Full == item) lines.Add("Drink: " + Quenching(pot.Water));
        if (Clothing.Find((ushort)item) is { } worn && worn.Effect.Length > 0) lines.Add(worn.Effect);
        if (!Items.IsKnown((ushort)item)) return lines;
        var def = Items.Def(item);
        var does = Tags.Where(t => def.Tags.HasFlag(t.Tag)).Select(t => t.Words).ToList();
        if (does.Count > 0) lines.Add(char.ToUpperInvariant(does[0][0]) + string.Join(", ", does)[1..]);
        if (def.FuelSeconds > 0) lines.Add(Burns(def.FuelSeconds));
        return lines;
    }

    static string Filling(float food) =>
        food < 0.1f ? "a bite" : food < 0.25f ? "a snack" : food < 0.45f ? "a meal" : "a hearty meal";

    static string Quenching(float water) =>
        water < 0.3f ? "a sip" : water < 0.6f ? "a drink" : "a long drink";

    static string Burns(float seconds) =>
        seconds < 60f ? "Burns for a few seconds" : $"Burns for about {System.MathF.Round(seconds / 60f)} min";
}
