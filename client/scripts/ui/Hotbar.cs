using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// What the hotbar talks to (ui-19): which kind of item sits on each of the ten slots,
// and the reducers that change them. The server keeps the slots
// (server/spacetimedb/Hotbar.cs) so they follow the player; MockHotbarLink stands in
// for previews and until the connection is up.
public interface IHotbarLink
{
    const int Slots = 10;
    Item? SlotItem(int index);
    // Puts a kind of item on a slot (null clears it); if it sat elsewhere, the two swap.
    void Set(int index, Item? item);
    void Move(int from, int to);
    // Uses one carried item the way its Def says (eat, drink, wear).
    void Use(Item item);
    // The slot in hand, or -1 (crafting-29: a tool works only from the equipped slot).
    int Equipped { get; }
    // Takes a slot in hand; the slot already in hand, or -1, puts it away.
    void Equip(int index);
    // Uses what is in hand (eat, drink, wear); the server reads which item from PlayerEquip.
    void UseHeld();
    event Action? Changed;
}

public sealed class MockHotbarLink : IHotbarLink
{
    readonly Item?[] slots = new Item?[IHotbarLink.Slots];
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

    public void Use(Item item) => Changed?.Invoke();

    public int Equipped { get; private set; } = -1;
    public void UseHeld() => Changed?.Invoke();

    public void Equip(int index)
    {
        Equipped = index == Equipped ? -1 : index;
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
        conn.Db.PlayerEquip.OnInsert += (_, _) => Changed?.Invoke();
        conn.Db.PlayerEquip.OnDelete += (_, _) => Changed?.Invoke();
        conn.Db.PlayerEquip.OnUpdate += (_, _, _) => Changed?.Invoke();
    }

    public Item? SlotItem(int index) =>
        conn.Db.MyHotbar.Iter().FirstOrDefault(s => s.Index == index) is { } s && Items.IsKnown(s.Item) ? (Item)s.Item : null;

    public void Set(int index, Item? item) => conn.Reducers.SetHotbarSlot((byte)index, (ushort)(item ?? 0));
    public void Move(int from, int to) => conn.Reducers.MoveHotbarSlot((byte)from, (byte)to);
    public void Use(Item item) => conn.Reducers.UseItem((ushort)item);

    public int Equipped => Me.Key(conn) is { } me && conn.Db.PlayerEquip.Owner.Find(me) is { } e ? e.Slot : -1;
    public void Equip(int index) => conn.Reducers.EquipSlot(index < 0 ? (byte)255 : (byte)index);
    public void UseHeld() => conn.Reducers.UseHeld();
}
