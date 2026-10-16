using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// What the hotbar talks to (ui-19): which kind of item sits on each of the ten slots,
// the tool in hand, and the reducers that change them. The server keeps the slots
// (server/spacetimedb/Hotbar.cs) so they follow the player; MockHotbarLink stands in
// for previews and until the connection is up.
public interface IHotbarLink
{
    const int Slots = 10;
    Item? SlotItem(int index);
    Item? Held { get; }
    // Puts a kind of item on a slot (null clears it); if it sat elsewhere, the two swap.
    void Set(int index, Item? item);
    void Move(int from, int to);
    // Uses one carried item the way its Def says (eat, drink, wear, hold).
    void Use(Item item);
    event Action? Changed;
}

public sealed class MockHotbarLink : IHotbarLink
{
    readonly Item?[] slots = new Item?[IHotbarLink.Slots];
    public Item? Held { get; private set; }
    public event Action? Changed;

    public Item? SlotItem(int index) => slots[index];

    public void Set(int index, Item? item)
    {
        int there = item == null ? -1 : Array.IndexOf(slots, item);
        if (there >= 0) slots[there] = slots[index];
        slots[index] = item;
        Changed?.Invoke();
    }

    public void Move(int from, int to)
    {
        (slots[from], slots[to]) = (slots[to], slots[from]);
        Changed?.Invoke();
    }

    public void Use(Item item)
    {
        if (Items.UseOf(item) == UseAction.Hold) Held = Held == item ? null : item;
        Changed?.Invoke();
    }
}

public sealed class ServerHotbarLink : IHotbarLink
{
    readonly DbConnection conn;
    readonly Func<uint> myId;
    public event Action? Changed;

    public ServerHotbarLink(DbConnection conn, Func<uint> myId)
    {
        this.conn = conn;
        this.myId = myId;
        conn.Db.MyHotbar.OnInsert += (_, _) => Changed?.Invoke();
        conn.Db.MyHotbar.OnDelete += (_, _) => Changed?.Invoke();
        conn.Db.MyHotbar.OnUpdate += (_, _, _) => Changed?.Invoke();
        conn.Db.HeldItem.OnInsert += (_, _) => Changed?.Invoke();
        conn.Db.HeldItem.OnDelete += (_, _) => Changed?.Invoke();
    }

    public Item? SlotItem(int index) =>
        conn.Db.MyHotbar.Iter().FirstOrDefault(s => s.Index == index) is { } s && Items.IsKnown(s.Item) ? (Item)s.Item : null;

    public Item? Held => conn.Db.HeldItem.PlayerId.Find(myId()) is { } h ? (Item)h.Item : null;

    public void Set(int index, Item? item) => conn.Reducers.SetHotbarSlot((byte)index, (ushort)(item ?? 0));
    public void Move(int from, int to) => conn.Reducers.MoveHotbarSlot((byte)from, (byte)to);
    public void Use(Item item) => conn.Reducers.UseItem((ushort)item);
}
