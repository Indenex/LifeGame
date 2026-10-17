using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// One stack in the bag panel: what it is, how good, how many (ui-18). Id is its row
// (a basket's item row, or the bag's own row once the bag keeps an order), Slot its
// place in the bag or on the tools belt, -1 for none.
public record BagStack(Item Item, byte Quality, uint Count, ulong Id = 0, int Slot = -1)
{
    public string Label => ItemLook.Label(Item, Quality, Count);
}

// What the bag panel talks to: everything you carry, the tools section of the player
// doll (Justin: tools there take no bag space and add no weight, and work by
// themselves), and the moves between them. Every move is a server reducer (D-182).
public interface IBagLink
{
    // The bag keeps an order you set (and the tools belt exists) once the server has
    // the Slot and OnBelt columns (ui-18, after Crafting's #267).
    bool Arranges { get; }
    IEnumerable<BagStack> Carried();
    IEnumerable<BagStack> Belt();
    void Drop(Item item, byte quality);
    // Moves a bag row to a place in the bag (-1: the first free one); whatever was there swaps.
    void MoveTo(ulong id, int slot);
    // Puts the bag in order by kind.
    void Sort();
    void ToBelt(ulong id, int slot);
    void FromBelt(ulong id, int slot);
}

// A pretend bag for previews, arranged by hand, with a stone knife on the belt.
public sealed class MockBagLink : IBagLink
{
    readonly List<(BagStack stack, bool belt)> rows = new();

    public MockBagLink(bool filled)
    {
        if (!filled) return;
        var bag = new (Item, byte, uint)[]
        {
            (Item.Stick, 0, 8), (Item.Stone, 0, 3), (Item.Fiber, 0, 12), (Item.Flake, 60, 2),
            (Item.HandAxe, 72, 1), (Item.Berries, 0, 5), (Item.Clay, 0, 4), (Item.CoilPotOfWater, 55, 1),
        };
        for (int i = 0; i < bag.Length; i++) rows.Add((new BagStack(bag[i].Item1, bag[i].Item2, bag[i].Item3, (ulong)i + 1, i), false));
        rows.Add((new BagStack(Item.StoneKnife, 65, 1, 99, 0), true));
    }

    public bool Arranges => true;
    public IEnumerable<BagStack> Carried() => rows.Where(r => !r.belt).Select(r => r.stack);
    public IEnumerable<BagStack> Belt() => rows.Where(r => r.belt).Select(r => r.stack);

    public void Drop(Item item, byte quality)
    {
        int i = rows.FindIndex(r => !r.belt && r.stack.Item == item && r.stack.Quality == quality);
        if (i < 0) return;
        if (rows[i].stack.Count > 1) rows[i] = (rows[i].stack with { Count = rows[i].stack.Count - 1 }, false);
        else rows.RemoveAt(i);
    }

    public void MoveTo(ulong id, int slot) => Put(id, false, slot);
    public void ToBelt(ulong id, int slot) => Put(id, true, slot);
    public void FromBelt(ulong id, int slot) => Put(id, false, slot);

    // Moves a row to a place on the bag or belt; the row already there takes its old place.
    void Put(ulong id, bool belt, int slot)
    {
        int i = rows.FindIndex(r => r.stack.Id == id);
        if (i < 0) return;
        var (moving, wasBelt) = rows[i];
        if (slot < 0) slot = Enumerable.Range(0, 1000).First(n => !rows.Any(r => r.belt == belt && r.stack.Slot == n));
        int j = rows.FindIndex(r => r.belt == belt && r.stack.Slot == slot && r.stack.Id != id);
        if (j >= 0) rows[j] = (rows[j].stack with { Slot = moving.Slot }, wasBelt);
        rows[i] = (moving with { Slot = slot }, belt);
    }

    public void Sort()
    {
        var sorted = rows.Where(r => !r.belt).Select(r => r.stack).OrderBy(s => BagGrid.Group(s.Item)).ThenBy(s => s.Item).ThenByDescending(s => s.Quality).ToList();
        rows.RemoveAll(r => !r.belt);
        for (int i = 0; i < sorted.Count; i++) rows.Add((sorted[i] with { Slot = i }, false));
    }
}

public sealed class ServerBagLink : IBagLink
{
    readonly DbConnection conn;
    public ServerBagLink(DbConnection conn) => this.conn = conn;

    // The bag's rows in their places, and the tools on the doll (ui-18, server/spacetimedb/Inventory.cs).
    public IEnumerable<BagStack> Carried() => Rows(belt: false);
    public IEnumerable<BagStack> Belt() => Rows(belt: true);

    IEnumerable<BagStack> Rows(bool belt) => conn.Db.MyInventory.Iter()
        .Where(i => i.Count > 0 && i.OnBelt == belt && Items.IsKnown(i.Item))
        .Select(i => new BagStack((Item)i.Item, i.Quality, i.Count, i.Id, i.Slot));

    public void Drop(Item item, byte quality) => conn.Reducers.Drop((ushort)item, quality);

    public bool Arranges => true;
    public void MoveTo(ulong id, int slot) => conn.Reducers.MoveInBag(id, (ushort)Math.Max(slot, 0));
    public void Sort() => conn.Reducers.SortBag();
    public void ToBelt(ulong id, int slot) => conn.Reducers.PutOnBelt(id, slot < 0 ? (byte)255 : (byte)slot);
    public void FromBelt(ulong id, int slot) => conn.Reducers.TakeOffBelt(id, slot < 0 ? ushort.MaxValue : (ushort)slot);
}

// Everything you carry as a grid of round slots, sorted by kind (ui-18). Drag a slot
// onto the hotbar, onto a worn slot, or out of the panel onto the ground: a placeable
// thing opens the placing ghost, anything else is dropped at your feet. Double-click
// uses it the way its hotbar key would.
public partial class BagGrid : GridContainer
{
    public const float Slot = 50f;
    public IBagLink Link = new MockBagLink(false);
    // What the grid shows: the bag by default, or a store (a basket, the claim stash).
    public Func<IEnumerable<BagStack>>? Source;
    // "bag" or a store's name; a drag carries it as "from".
    public string Kind = "bag";
    // Takes a stack dragged from another grid (a basket taking from the bag, the bag taking from a basket).
    public Func<Godot.Collections.Dictionary, bool> Accepts = _ => false;
    public Action<Godot.Collections.Dictionary> Receive = _ => { };
    public string Empty = "Your hands are empty";
    // How many stacks a store holds (a basket more than a jar); free room shows as empty rings.
    public Func<int>? Capacity;
    // A click on a slot (with Shift held: all of it). Set by store panels, where a
    // click moves things; when it's null, a double-click uses the thing instead.
    public Action<BagStack, bool>? Clicked;
    public int Width = 6;
    // Each stack sits at its own Slot, with free places between (the bag once it keeps
    // an order, and the tools belt); otherwise stacks are sorted by kind.
    public Func<bool> Positioned = () => false;
    // A stack dropped on a place in this same grid (rearranging the bag).
    public Action<Godot.Collections.Dictionary, int> Arrange = (_, _) => { };
    // A drop from another grid, with the place it was let go on (-1: the grid itself).
    public Action<Godot.Collections.Dictionary, int>? ReceiveAt;
    public Action<Item> Use = _ => { };
    // A thing let go outside the panel (the panel says where it is).
    public Func<Vector2, bool> OverPanel = _ => true;
    public Action<Item, byte> PlaceOrDrop = (_, _) => { };
    // A worn piece dragged back onto the bag is taken off; a hotbar slot dragged here is cleared.
    public Action<ClothingSlot> TakeOff = _ => { };
    public Action<int> ClearHotbar = _ => { };

    string shown = "";
    internal (Item item, byte quality)? Dragging;

    public override void _Ready()
    {
        Columns = Width;
        AddThemeConstantOverride("h_separation", 10);
        AddThemeConstantOverride("v_separation", 10);
        MouseFilter = MouseFilterEnum.Pass;
    }

    // Sorted by kind: what you use (food, drink, tools, clothes) first, then the rest by item.
    public static int Group(Item item) => LifeGame.Shared.Bag.Group(item);

    public void Refresh()
    {
        bool placed = Positioned();
        var stacks = (Source?.Invoke() ?? Link.Carried())
            .OrderBy(s => placed ? s.Slot : 0).ThenBy(s => Group(s.Item)).ThenBy(s => s.Item).ThenByDescending(s => s.Quality).ToList();
        int room = Capacity?.Invoke() ?? 0;
        string now = $"{placed}|{room}|" + string.Join(",", stacks.Select(s => $"{(int)s.Item}:{s.Quality}:{s.Count}:{s.Slot}"));
        if (now == shown) return;
        shown = now;
        foreach (var child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        if (placed)
        {
            // Each stack at its place; two on one place (old rows) take the next free one.
            var cells = new Dictionary<int, BagStack>();
            foreach (var s in stacks)
            {
                int at = Math.Max(0, s.Slot);
                while (cells.ContainsKey(at)) at++;
                cells[at] = s;
            }
            int last = cells.Count == 0 ? -1 : cells.Keys.Max();
            // A fixed number of places (the belt), or room for everything plus a free row to drag into.
            int count = room > 0 ? Math.Max(room, last + 1) : ((last + Width) / Width + 1) * Width;
            for (int i = 0; i < count; i++)
                AddChild(new BagSlotView { Stack = cells.GetValueOrDefault(i), Cell = i, Grid = this });
            return;
        }
        foreach (var s in stacks) AddChild(new BagSlotView { Stack = s, Grid = this });
        for (int i = stacks.Count; i < room; i++) AddChild(new BagSlotView { Grid = this });
        if (stacks.Count == 0 && room == 0)
        {
            var none = Hud.Text(Empty, 14);
            none.Modulate = new Color(1, 1, 1, 0.5f);
            AddChild(none);
        }
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) => CanTake(data, -1);
    public override void _DropData(Vector2 atPosition, Variant data) => Take(data, -1);

    // Drops on the grid or on one of its places (cell, -1 for the grid itself).
    internal bool CanTake(Variant data, int cell) =>
        data.Obj is Godot.Collections.Dictionary d
        && (Kind == "bag" && (d.ContainsKey("worn") || d.ContainsKey("hotbar"))
            || d.TryGetValue("from", out var from) && (from.AsString() != Kind ? Accepts(d) : cell >= 0 && Positioned()));

    internal void Take(Variant data, int cell)
    {
        if (data.Obj is not Godot.Collections.Dictionary d) return;
        if (d.TryGetValue("from", out var from) && from.AsString() == Kind) Arrange(d, cell);
        else if (d.ContainsKey("from")) { if (ReceiveAt != null) ReceiveAt(d, cell); else Receive(d); }
        else if (d.TryGetValue("worn", out var slot)) TakeOff((ClothingSlot)slot.AsInt32());
        else if (d.TryGetValue("hotbar", out var i)) ClearHotbar(i.AsInt32());
    }

    public override void _Notification(int what)
    {
        if (what != NotificationDragEnd || Dragging is not { } d) return;
        Dragging = null;
        if (Kind != "bag") return;
        // Let go over the world rather than over a panel: set it down or drop it.
        if (!GetViewport().GuiIsDragSuccessful() && !OverPanel(GetViewport().GetMousePosition()))
            PlaceOrDrop(d.item, d.quality);
    }
}

// One round slot in the bag grid.
public partial class BagSlotView : PanelContainer
{
    // Null for a free place in a store: an empty ring that still takes drops.
    public BagStack? Stack;
    public BagGrid Grid = null!;
    // Its place in a positioned grid, -1 otherwise.
    public int Cell = -1;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(BagGrid.Slot, BagGrid.Slot);
        MouseFilter = MouseFilterEnum.Stop;
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.10f, 0.07f, Stack == null ? 0.35f : 0.7f),
            BorderColor = new Color(Look.Cream, Stack == null ? 0.14f : 0.28f),
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll((int)(BagGrid.Slot / 2) - 2);
        AddThemeStyleboxOverride("panel", style);
        if (Stack == null) return;
        TooltipText = Stack.Label;

        var stack = new Control { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(stack);
        var picture = new TextureRect
        {
            Texture = ItemIcon.For(Stack.Item),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        picture.SetAnchorsPreset(LayoutPreset.FullRect);
        picture.OffsetLeft = picture.OffsetTop = 2;
        picture.OffsetRight = picture.OffsetBottom = -2;
        stack.AddChild(picture);
        if (Stack.Count > 1)
        {
            var count = Hud.Text(Stack.Count.ToString(), 13);
            count.SetAnchorsPreset(LayoutPreset.FullRect);
            count.HorizontalAlignment = HorizontalAlignment.Right;
            count.VerticalAlignment = VerticalAlignment.Bottom;
            count.OffsetRight = -3;
            count.OffsetBottom = 1;
            stack.AddChild(count);
        }
    }

    public override void _GuiInput(InputEvent e)
    {
        if (Stack == null || e is not InputEventMouseButton { ButtonIndex: MouseButton.Left } b) return;
        // A click acts on release, so pressing to start a drag doesn't move anything.
        if (Grid.Clicked != null && !b.Pressed && GetGlobalRect().HasPoint(b.GlobalPosition))
        {
            Grid.Clicked(Stack, Input.IsKeyPressed(Key.Shift));
            AcceptEvent();
        }
        else if (Grid.Clicked == null && b.DoubleClick && b.Pressed && Grid.Kind == "bag")
        {
            Grid.Use(Stack.Item);
            AcceptEvent();
        }
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) => Grid.CanTake(data, Cell);
    public override void _DropData(Vector2 atPosition, Variant data) => Grid.Take(data, Cell);

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (Stack == null) return default;
        var preview = new TextureRect
        {
            Texture = ItemIcon.For(Stack.Item),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Size = new Vector2(BagGrid.Slot, BagGrid.Slot),
            Position = -new Vector2(BagGrid.Slot, BagGrid.Slot) / 2,
            Modulate = new Color(1, 1, 1, 0.8f),
        };
        // Shift-drag takes half the stack (rounded up) into a basket or the stash; a plain drag takes it all.
        uint count = Input.IsKeyPressed(Key.Shift) && Stack.Count > 1 ? (Stack.Count + 1) / 2 : Stack.Count;
        var holder = new Control();
        holder.AddChild(preview);
        if (count > 1)
        {
            var label = Hud.Text(count.ToString(), 13);
            label.Position = new Vector2(BagGrid.Slot * 0.1f, BagGrid.Slot * 0.05f);
            holder.AddChild(label);
        }
        SetDragPreview(holder);
        Grid.Dragging = (Stack.Item, Stack.Quality);
        var data = new Godot.Collections.Dictionary
        {
            ["from"] = Grid.Kind, ["item"] = (int)Stack.Item, ["quality"] = (int)Stack.Quality,
            ["count"] = (long)count, ["id"] = (long)Stack.Id,
        };
        // Only the bag's own things go onto the hotbar or a worn slot.
        if (Grid.Kind == "bag") data["bag"] = (int)Stack.Item;
        return data;
    }
}
