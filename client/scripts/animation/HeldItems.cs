using System.Linq;
using LifeGame.Shared;

namespace LifeGame;

// What a player holds while a job runs (default until Justin picks, see anim-1): the
// right hand the tool the process needs, the left the first material it works. Derived
// from the process id alone, so other players hold the same things.
public static class HeldItems
{
    public static (Item? right, Item? left) For(string? processId)
    {
        if (processId == null || Processes.Find(processId) is not { } def) return (null, null);
        Item? tool = def.NeedsTool == 0 ? null : SimplestWith(def.NeedsTool);
        Item? material = def.Consumes.Length > 0 ? def.Consumes[0].Item : null;
        return (tool, material);
    }

    // The first (simplest) item listed with the tag: a stone for Hammer, a flake for Cutting.
    static Item? SimplestWith(ItemTags tag) =>
        Items.All.Values.FirstOrDefault(d => (d.Tags & tag) != 0) is { } d ? d.Id : null;
}
