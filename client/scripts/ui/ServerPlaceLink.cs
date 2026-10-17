using System;
using System.Collections.Generic;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Placing's link to the server (crafting-21, Crafting's side): placeable rows of my
// bag, the shared water and slope check for the ghost, and the PlaceItem reducer. The
// server also checks reach, claims, trees and boulders; its errors reach the HUD
// through Main's reducer error note.
public sealed class ServerPlaceLink : IPlaceLink
{
    readonly DbConnection conn;
    readonly Func<float, float, float> groundAt;

    public event Action<Placeable>? Crafted;

    public ServerPlaceLink(DbConnection conn, CraftingUi crafting, Func<float, float, float> groundAt)
    {
        this.conn = conn;
        this.groundAt = groundAt;
        // Crafts whose outcome is an item (a basket, a log); bricks and fired pots come
        // out of the kiln and are set down from the bag instead.
        crafting.Finished += (_, item, quality) =>
        {
            if (InBag().FirstOrDefault(p => p.Item == item && p.Quality == quality) is { } t) Crafted?.Invoke(t);
        };
    }

    public IEnumerable<Placeable> InBag() => conn.Db.MyInventory.Iter()
        .Where(i => i.Count > 0 && Items.IsKnown(i.Item) && Items.Def((Item)i.Item).Placeable)
        .OrderBy(i => i.Item).ThenByDescending(i => i.Quality)
        .Select(i => new Placeable((Item)i.Item, i.Quality, i.Wear))
        .Distinct();

    public string? Fits(Placeable thing, float x, float z, float yaw) => Placement.Check(thing.Item, x, z, groundAt);

    // Takes the item in hand first (Main: the hotbar's TakeInHand), since the server sets down
    // only what is in hand (crafting-29).
    public Func<Item, bool> TakeInHand = _ => true;

    public void Place(Placeable thing, float x, float z, float yaw)
    {
        if (TakeInHand(thing.Item)) conn.Reducers.PlaceItem((ushort)thing.Item, thing.Quality, thing.Wear, x, z, yaw);
    }
}
