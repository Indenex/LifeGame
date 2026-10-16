using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// How to do something again, in one line for its journal card. Crafting jobs read
// from Processes.List; everything else gets a plain sentence. Keys come from the
// live InputMap so a rebinding reads right.
public static class Recipes
{
    // Sentences for processes that are not crafting jobs. {action} becomes that action's key.
    static readonly Dictionary<string, string> Hints = new()
    {
        [Skills.Forage] = "{pick_up} picks up sticks, stones and plant fiber lying at your feet.",
        [Skills.Berries] = "{use} at a ripe berry bush.",
        [Skills.Dig] = "{dig} digs the ground in front of you. Some ground holds clay; a digging stick reaches the finest, deepest clay.",
        [ClayRules.FillProcess] = "{use} at water with an empty fired pot.",
        [ClayRules.PitFireProcess] = "Set dry pots in a burning fire with {use}. Wet pots crack.",
        [ClayRules.KilnProcess] = "{build_mode} to lay out a kiln, then add bricks and daub with {use}.",
        [ClayRules.KilnFireProcess] = "Load dry pots into a finished kiln with {use}, then fire it.",
        ["build.frame"] = "{build_mode} to lay out a shelter, then add sticks with {use}. Anyone can help.",
        ["build.wattle"] = "Weave sticks and fiber into a hut's frame with {use}.",
        ["build.daub"] = "Plaster a hut's wattle with daub using {use}.",
        ["build.roof"] = "Thatch a shelter with plant fiber using {use}.",
        ["build.weave"] = "{build_mode} to lay out a storage basket, then weave it with {use}.",
        ["build.door"] = "Hang a door on a finished hut with {use}.",
        [Skills.Shear] = "{use} beside a calm sheep while you carry something sharp. Sneak up with {sneak}.",
        [Skills.Kill] = "{strike} to strike an animal. A spear hits harder.",
        [Skills.Butcher] = "Butcher a carcass with a cutting edge.",
    };

    public static string? HowTo(string processId)
    {
        if (Processes.Find(processId) is { } def) return Crafted(def);
        return Hints.TryGetValue(processId, out var hint) ? Keys(hint) : null;
    }

    static string Crafted(ProcessDef def)
    {
        var parts = def.Consumes.Select(i => Some(i.Item, i.Count)).ToList();
        string needs = parts.Count switch
        {
            0 => "Needs nothing but your hands",
            1 => $"Needs {parts[0]}",
            _ => $"Needs {string.Join(", ", parts.Take(parts.Count - 1))} and {parts[^1]}",
        };
        if (def.NeedsTool != ItemTags.None) needs += $", using {CraftingUi.ToolName(def.NeedsTool)}";

        string makes = def.Outcome switch
        {
            Outcome.Fire => "Makes a campfire.",
            Outcome.Roast => "Roasts over a burning fire.",
            Outcome.Placed => "Sets it down in front of you.",
            _ => $"Makes {Some(def.Makes, def.MakesCount)}.",
        };
        return $"{needs}. {makes} {Keys("{craft}")} to make it.";
    }

    // "2 sticks", "a tinder bundle", "cordage".
    static string Some(Item item, uint count)
    {
        if (!Items.All.TryGetValue(item, out var def)) return count.ToString();
        string name = def.Count(count).ToLowerInvariant();
        if (count != 1) return $"{count} {name}";
        if (def.Name == def.Plural) return name;
        return ("aeiou".Contains(name[0]) ? "an " : "a ") + name;
    }

    static string Keys(string text)
    {
        foreach (var action in new[] { "pick_up", "use", "dig", "build_mode", "sneak", "strike", "craft" })
            text = text.Replace("{" + action + "}", KeyOf(action));
        return text;
    }

    // The first key or mouse button bound to an action, as the player sees it.
    static string KeyOf(string action)
    {
        if (!InputMap.HasAction(action)) return action.Replace('_', ' ');
        foreach (var e in InputMap.ActionGetEvents(action))
        {
            if (e is InputEventKey key)
                return OS.GetKeycodeString(key.PhysicalKeycode != Godot.Key.None ? key.PhysicalKeycode : key.Keycode);
            if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left }) return "Click";
        }
        return action.Replace('_', ' ');
    }
}
