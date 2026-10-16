using System;
using System.Collections.Generic;
using System.Linq;
using LifeGame.Shared;

namespace LifeGame;

// "Things to try" under the journal's discoveries (skills-10, D-081 M2): a gentle
// hint toward a few things you haven't found yet but could reach from what you
// know. Hints name the goal, not the recipe (D-063). A process with no hint here
// is never suggested, so new processes stay quiet until someone writes a line.
public static class NextSteps
{
    // Steps of D-127's first evening that other lanes report: planting a claim marker
    // (Claims) and building the potter's bench (Ceramics, ceramics-8). Until they report
    // these ids, both stay quiet and nothing waits on them.
    const string Claim = "claim.mark", Bench = "build.bench";

    // One line per process. {action} becomes that action's key.
    static readonly Dictionary<string, string> Lines = new()
    {
        [Skills.Forage] = "Sticks, stones and plant fiber lie all around. {pick_up} picks them up.",
        [Skills.Run] = "Hold {run} to run. The more you run, the farther you can go.",
        [Skills.Berries] = "Some bushes carry ripe berries. Food for the road.",
        [Skills.Dig] = "The ground holds more than roots. Try {dig} where the soil is low and damp.",
        ["gather.tinder"] = "Fire needs something fine and dry to catch. Plant fiber can be shredded.",
        ["fire.hand_drill"] = "Two sticks and a bundle of tinder hold a fire, if you spin hard enough.",
        ["knap.flake"] = "Stone breaks sharp when it is struck with stone.",
        ["knap.hand_axe"] = "A stone shaped with patience makes a heavy tool for the hand.",
        ["cook.meat"] = "Raw meat is better over a fire.",
        ["carve.digging_stick"] = "A sharpened stick digs deeper than bare hands.",
        ["cord.twist"] = "Plant fiber, twisted tight, becomes cord.",
        ["lash.spear"] = "A sharp flake bound to a long stick reaches further.",
        ["lash.stone_axe"] = "A shaped stone bound to a handle could fell a tree.",
        ["haft.knife"] = "A flake cuts better with a handle to hold it.",
        ["weave.basket"] = "Sticks and plenty of fiber can be woven into something that carries more.",
        ["lash.bow_drill"] = "A bent stick and a cord turn a drill faster than your palms.",
        ["fire.bow_drill"] = "With a bow drill, fire comes quicker and more surely.",
        ["chop.log"] = "Standing trees give logs to an axe. A log burns for a long time.",
        ["hide.scrape"] = "A fresh hide must be scraped clean with an edge before it will keep.",
        ["hide.stretch"] = "A clean hide laced onto a stick frame dries stiff and tan into leather, slower in rain and faster by a fire.",
        [Skills.DryHide] = "Leave a stretched hide to the sun and wind until it turns to leather.",
        ["clay.clean"] = "Dug clay is full of stones and roots. Pick them out first.",
        ["clay.grit"] = "Crushed stone worked into clay helps it survive the fire.",
        ["clay.grog"] = "Broken pottery, crushed fine, is the best thing to work into new clay.",
        ["clay.temper_grit"] = "Clean clay alone cracks in the fire. It needs something worked into it.",
        ["clay.temper_grog"] = "Crushed sherds temper clay even better than grit.",
        ["clay.pinch_pot"] = "Tempered clay can be pinched into a small pot.",
        ["clay.storage_jar"] = "A big coiled jar, fired in a kiln, keeps a lot safe at home.",
        ["weave.mat"] = "A thick weave of fiber makes a mat to sleep on, off the cold ground.",
        [Claim] = "A marker of sticks and fiber, raised away from the waking clearing, makes the land around it yours.",
        [Bench] = "A potter's bench in your claim, made from a log and two stones, is where clay is shaped.",
        ["clay.coil_pot"] = "Rolled into coils, tempered clay builds a bigger pot.",
        ["clay.brick"] = "Clay and fiber, pressed into shape and dried, make a brick.",
        ["clay.daub"] = "Clay mixed with fiber makes a plaster for walls.",
        [ClayRules.PitFireProcess] = "A dry pot set in a fire comes out hard. A wet one cracks.",
        [ClayRules.FillProcess] = "A fired pot holds water.",
        [ClayRules.KilnProcess] = "Bricks and daub can build a kiln, which fires pots better than an open fire.",
        [ClayRules.KilnFireProcess] = "A finished kiln fires a whole load of dry pots at once.",
        ["build.frame"] = "Sticks driven into the ground make the frame of a shelter. {build_mode} to lay one out.",
        ["build.wattle"] = "A frame can be woven with sticks and fiber into walls.",
        ["build.daub"] = "Wattle walls plastered with daub keep the wind out.",
        ["build.roof"] = "A frame thatched with fiber keeps the rain off.",
        ["build.weave"] = "A woven storage basket keeps your things by your shelter.",
        ["build.door"] = "A finished hut wants a door.",
        [Skills.Shear] = "A calm sheep with a full fleece will stand for a blade. Come up slowly.",
        [Skills.Kill] = "A spear or blade can bring a sheep down, but it will run.",
        [Skills.Sneak] = "Wary animals bolt from a walker. Move slowly and stay low, and you can get closer.",
        [Skills.Butcher] = "A fallen sheep yields meat and a hide to a blade.",
        // Wool and clothing (Animals, animals-9); quiet until those processes land.
        ["carve.drop_spindle"] = "A stone whorl on a straight stick makes something that turns loose wool into thread.",
        ["cord.spin_wool"] = "Fleece twisted on a whirling spindle draws out into yarn. Keep it turning, but not too eagerly.",
        ["weave.wool_cloth"] = "Yarn crossed over and under on a frame of sticks becomes cloth.",
        ["sew.wool_tunic"] = "Cloth cut and sewn to fit keeps the night's chill off a little.",
        ["sew.leather_wraps"] = "Leather tied round the feet keeps them off the cold ground.",
    };

    // Things that cut. Declared before After, which reads it.
    static readonly string[] Cutting = { "knap.flake", "knap.hand_axe", "haft.knife" };

    // What the activities that are not crafting jobs follow from: every group needs
    // one of its processes known. An empty list means it can be tried at once.
    static readonly Dictionary<string, string[][]> After = new()
    {
        [Skills.Forage] = Array.Empty<string[]>(),
        [Skills.Berries] = Array.Empty<string[]>(),
        [Skills.Dig] = Array.Empty<string[]>(),
        [Skills.Run] = Array.Empty<string[]>(),
        [ClayRules.PitFireProcess] = new[] { new[] { "fire.hand_drill", "fire.bow_drill" }, new[] { "clay.pinch_pot", "clay.coil_pot" } },
        [ClayRules.FillProcess] = new[] { new[] { ClayRules.PitFireProcess, ClayRules.KilnFireProcess } },
        [ClayRules.KilnProcess] = new[] { new[] { "clay.brick" }, new[] { "clay.daub" } },
        [ClayRules.KilnFireProcess] = new[] { new[] { ClayRules.KilnProcess } },
        ["build.frame"] = new[] { new[] { Skills.Forage } },
        ["build.wattle"] = new[] { new[] { "build.frame" } },
        ["build.roof"] = new[] { new[] { "build.frame" } },
        ["build.daub"] = new[] { new[] { "build.wattle" }, new[] { "clay.daub" } },
        ["build.door"] = new[] { new[] { "build.daub" } },
        ["build.weave"] = new[] { new[] { "weave.basket" } },
        [Skills.Shear] = new[] { Cutting },
        [Skills.Kill] = new[] { Cutting.Append("lash.spear").ToArray() },
        [Skills.Butcher] = new[] { new[] { Skills.Kill } },
        [Skills.Sneak] = new[] { new[] { Skills.Forage } },
        [Skills.DryHide] = new[] { new[] { "hide.stretch" } },
        [Claim] = new[] { new[] { Skills.Forage } },
        [Bench] = new[] { new[] { "chop.log" }, new[] { Claim } },
    };

    // Crafting jobs that also need a place to work, on top of their ingredients.
    static readonly Dictionary<string, string[][]> AlsoAfter = new()
    {
        ["clay.pinch_pot"] = new[] { new[] { Bench } },
        ["clay.coil_pot"] = new[] { new[] { Bench } },
        // The jar is fired only in a kiln (D-076).
        ["clay.storage_jar"] = new[] { new[] { Bench }, new[] { "clay.coil_pot" }, new[] { ClayRules.KilnProcess } },
    };

    // Where things come from when no crafting job makes them.
    static readonly Dictionary<string, Item[]> Yields = new()
    {
        [Skills.Forage] = new[] { Item.Stick, Item.Stone, Item.Fiber },
        [Skills.Berries] = new[] { Item.Berries },
        [Skills.Dig] = new[] { Item.Clay },
        [ClayRules.PitFireProcess] = new[] { Item.PinchPot, Item.CoilPot, Item.Potsherd },
        [ClayRules.KilnFireProcess] = new[] { Item.PinchPot, Item.CoilPot, Item.Potsherd },
        [Skills.Shear] = new[] { Item.Wool },
        [Skills.Butcher] = new[] { Item.RawMutton, Item.SheepHide },
        [Skills.DryHide] = new[] { Item.Leather },
    };

    // Rough tier for activities outside Processes.List, so hints come in a sensible order.
    static readonly Dictionary<string, int> Tier = new()
    {
        [Skills.Forage] = 0, [Skills.Berries] = 0, [Skills.Run] = 0, [Skills.Dig] = 1, ["build.frame"] = 1,
        ["build.wattle"] = 2, ["build.roof"] = 2, ["build.weave"] = 2,
        [Skills.Shear] = 2, [Skills.Kill] = 2, [Skills.Sneak] = 2, [Skills.Butcher] = 2, [Skills.DryHide] = 2,
    };

    // The first evening's road (D-081, D-127): fire, a claim, food and shelter, a stone
    // axe and a log for the potter's bench, then clay to a fired pot. Hints on it come
    // first; one slot is kept for something off the road.
    static readonly string[] Road =
    {
        Skills.Forage, "gather.tinder", "fire.hand_drill", Claim, Skills.Berries,
        "build.frame", "build.roof", "knap.hand_axe", "cord.twist", "lash.stone_axe", "chop.log", Bench,
        Skills.Dig, "clay.clean", "clay.grit", "clay.temper_grit",
        "clay.pinch_pot", ClayRules.PitFireProcess, ClayRules.FillProcess,
    };

    // Up to `max` hints for things not yet known.
    public static List<string> For(IReadOnlySet<string> known, int max = 3) =>
        Next(known, max).Select(id => Recipes.Keys(Lines[id])).ToList();

    // The process ids behind those hints, best first.
    public static List<string> Next(IReadOnlySet<string> known, int max = 3)
    {
        var have = Reachable(known);
        var order = Skills.AllProcessIds().ToList();
        var exists = order.ToHashSet();
        var open = order.Where(id => !known.Contains(id) && Lines.ContainsKey(id) && CanTry(id, known, have, exists)).ToList();
        // Steps past the furthest one you've reached come first, so a skipped
        // early step doesn't hide the way on; skipped ones follow.
        int reached = Array.FindLastIndex(Road, known.Contains);
        var road = Road.Skip(reached + 1).Concat(Road.Take(reached + 1)).Where(open.Contains).ToList();
        var side = open.Except(road)
            .OrderBy(id => Processes.Find(id)?.Tier ?? Tier.GetValueOrDefault(id, 3))
            .ThenBy(order.IndexOf)
            .ToList();
        var picked = road.Take(side.Count > 0 ? max - 1 : max).ToList();
        picked.AddRange(side.Take(max - picked.Count));
        picked.AddRange(road.Skip(picked.Count(road.Contains)).Take(max - picked.Count));
        return picked;
    }

    static bool CanTry(string id, IReadOnlySet<string> known, HashSet<Item> have, HashSet<string> exists)
    {
        if (After.TryGetValue(id, out var groups)) return Met(groups, known, exists);
        if (Processes.Find(id) is not { } def) return false;
        if (AlsoAfter.TryGetValue(id, out var also) && !Met(also, known, exists)) return false;
        if (def.NeedsTool != ItemTags.None && !have.Any(i => (Items.Def(i).Tags & def.NeedsTool) != 0)) return false;
        return def.Consumes.All(c => have.Contains(c.Item));
    }

    // Every group needs one of its processes known. Processes the game doesn't have
    // yet don't count, and a group made only of those is skipped.
    static bool Met(string[][] groups, IReadOnlySet<string> known, HashSet<string> exists) =>
        groups.Select(g => g.Where(exists.Contains).ToArray())
              .Where(g => g.Length > 0)
              .All(g => g.Any(known.Contains));

    // Items you could get hold of: what you know how to make or find, plus anything
    // nothing makes at all, which just lies around.
    static HashSet<Item> Reachable(IReadOnlySet<string> known)
    {
        var made = new Dictionary<Item, List<string>>();
        void Add(Item item, string by)
        {
            if (!made.TryGetValue(item, out var list)) made[item] = list = new List<string>();
            list.Add(by);
        }
        foreach (var def in Processes.List)
            if (Items.All.ContainsKey(def.Makes)) Add(def.Makes, def.Id);
        foreach (var (by, items) in Yields)
            foreach (var item in items) Add(item, by);

        return Items.All.Keys
            .Where(i => !made.TryGetValue(i, out var by) || by.Any(known.Contains))
            .ToHashSet();
    }
}
