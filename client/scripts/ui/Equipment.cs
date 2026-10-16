using System;
using System.Collections.Generic;
using System.Linq;
using LifeGame.Shared;

namespace LifeGame;

// A piece of clothing, worn or carried, as the panel shows it. Slot and Effect come
// from shared/Clothing.cs (Animals lane), so the panel and the server agree.
public record WornPiece(Item Item, byte Quality, ClothingSlot Slot)
{
    public string Effect => Clothing.Find((ushort)Item)?.Effect ?? "";
    // "6% less cold" at this piece's quality.
    public string Amount => Clothing.Percent((ushort)Item, Quality);
}

// What the equipment panel (D-074) talks to: animals-9 on the server through
// ServerEquipLink, or MockEquipLink for screenshots.
public interface IEquipLink
{
    // What you wear in a slot, or null.
    WornPiece? Worn(ClothingSlot slot);
    // What you carry that could go in a slot.
    IEnumerable<WornPiece> Carried(ClothingSlot slot);
    // The server picks the slot and swaps out what was there.
    void Wear(WornPiece piece);
    void TakeOff(ClothingSlot slot);
    // A line for the HUD ("You put on the wool tunic").
    event Action<string>? Said;
}

// Pretend clothes for screenshots: a tunic and foot wraps, plus a rougher tunic to swap.
public sealed class MockEquipLink : IEquipLink
{
    readonly Dictionary<ClothingSlot, WornPiece> worn = new();
    readonly List<WornPiece> carried = new()
    {
        new(Item.WoolTunic, 70, ClothingSlot.Body),
        new(Item.WoolTunic, 25, ClothingSlot.Body),
        new(Item.LeatherWraps, 50, ClothingSlot.Feet),
    };

    public event Action<string>? Said;

    public WornPiece? Worn(ClothingSlot slot) => worn.GetValueOrDefault(slot);
    public IEnumerable<WornPiece> Carried(ClothingSlot slot) => carried.Where(w => w.Slot == slot);

    public void Wear(WornPiece piece)
    {
        if (!carried.Remove(piece)) return;
        if (worn.Remove(piece.Slot, out var old)) carried.Add(old);
        worn[piece.Slot] = piece;
        Said?.Invoke(EquipmentPanel.PutOn(piece.Item));
    }

    public void TakeOff(ClothingSlot slot)
    {
        if (!worn.Remove(slot, out var old)) return;
        carried.Add(old);
        Said?.Invoke(EquipmentPanel.TookOff(old.Item));
    }
}

// The real link: my_worn for what you wear, my_inventory for what you could put on,
// and the Wear and TakeOff reducers. Server errors ("You can't wear that") reach the
// HUD through Main's unhandled reducer error note.
public sealed class ServerEquipLink : IEquipLink
{
    readonly SpacetimeDB.Types.DbConnection conn;
    public event Action<string>? Said;

    public ServerEquipLink(SpacetimeDB.Types.DbConnection conn)
    {
        this.conn = conn;
        // Only changes you asked for are noted, not the rows that arrive when you log in.
        // A swap deletes and inserts in one go, so only the put-on line shows.
        conn.Db.MyWorn.OnInsert += (_, w) => { if (asked) Said?.Invoke(EquipmentPanel.PutOn((Item)w.Item)); asked = false; };
        conn.Db.MyWorn.OnDelete += (_, w) => { if (asked && takingOff) Said?.Invoke(EquipmentPanel.TookOff((Item)w.Item)); if (takingOff) asked = false; };
    }

    bool asked, takingOff;

    public WornPiece? Worn(ClothingSlot slot) =>
        conn.Db.MyWorn.Iter().FirstOrDefault(w => w.Slot == (byte)slot) is { } w
            ? new WornPiece((Item)w.Item, w.Quality, slot)
            : null;

    public IEnumerable<WornPiece> Carried(ClothingSlot slot) => conn.Db.MyInventory.Iter()
        .Where(i => i.Count > 0 && Clothing.Find(i.Item)?.Slot == slot)
        .Select(i => (i.Item, i.Quality)).Distinct()
        .Select(k => new WornPiece((Item)k.Item, k.Quality, slot));

    public void Wear(WornPiece piece)
    {
        asked = true;
        takingOff = false;
        conn.Reducers.Wear((ushort)piece.Item, piece.Quality);
    }

    public void TakeOff(ClothingSlot slot)
    {
        asked = takingOff = true;
        conn.Reducers.TakeOff((byte)slot);
    }
}
