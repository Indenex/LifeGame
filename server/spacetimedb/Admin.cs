using LifeGame.Shared;
using SpacetimeDB;

// The admin item panel (admin-1). The server decides who may give themselves items:
// the admin list is private (WorldAdmin), and a client only ever sees whether it is
// itself on it (my_admin). The first admin is the publisher, who adds players with
// `spacetime call <db> make_admin '"Name"'` (docs/ADMIN.md).
public static partial class Module
{
    // A client reads only its own row of the private admin list, to know whether to show the panel.
    [SpacetimeDB.View(Accessor = "MyAdmin", Public = true)]
    public static List<WorldAdmin> MyAdmin(ViewContext ctx) =>
        ctx.Db.WorldAdmin.Identity.Find(ctx.Sender) is { } me ? new List<WorldAdmin> { me } : new List<WorldAdmin>();

    [SpacetimeDB.Reducer]
    public static void AdminGive(ReducerContext ctx, ushort item, uint count, byte quality)
    {
        if (!IsWorldAdmin(ctx)) throw new Exception("Only an admin can give items");
        RequirePlayer(ctx);
        if (!Items.IsKnown(item)) throw new Exception("No such item");
        if (count < 1 || count > AdminRules.GiveMax) throw new Exception($"Give between 1 and {AdminRules.GiveMax}");
        var def = Items.Def((Item)item);
        // Raw materials always come at a flat quality; made things at the quality asked, 1 to 100.
        byte made = def.HasQuality ? Math.Clamp(quality, (byte)1, (byte)100) : Items.RawQuality;
        AddToInventory(ctx, ctx.Sender, (Item)item, count, made);
        Log.Info($"admin_give: {ctx.Sender} gave themselves {count} x {def.Name} (quality {made})");
    }
}
