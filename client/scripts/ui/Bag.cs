using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// One stack in the bag panel: what it is, how good, how many (ui-18).
// Id is the row in a store (a basket's item row), 0 for the bag.
public record BagStack(Item Item, byte Quality, uint Count, ulong Id = 0)
{
    public string Label => ItemLook.Label(Item, Quality, Count);
}

// What the bag panel talks to: everything you carry, and dropping one at your feet.
public interface IBagLink
{
    IEnumerable<BagStack> Carried();
    void Drop(Item item, byte quality);
}

// A pretend bag for previews.
public sealed class MockBagLink : IBagLink
{
    readonly List<BagStack> bag = new()
    {
        new(Item.Stick, 0, 8), new(Item.Stone, 0, 3), new(Item.Fiber, 0, 12), new(Item.Flake, 60, 2),
        new(Item.HandAxe, 72, 1), new(Item.Berries, 0, 5), new(Item.Clay, 0, 4), new(Item.CoilPotOfWater, 55, 1),
    };

    public MockBagLink(bool filled) { if (!filled) bag.Clear(); }

    public IEnumerable<BagStack> Carried() => bag;

    public void Drop(Item item, byte quality)
    {
        int i = bag.FindIndex(s => s.Item == item && s.Quality == quality);
        if (i < 0) return;
        if (bag[i].Count > 1) bag[i] = bag[i] with { Count = bag[i].Count - 1 };
        else bag.RemoveAt(i);
    }
}

public sealed class ServerBagLink : IBagLink
{
    readonly DbConnection conn;
    public ServerBagLink(DbConnection conn) => this.conn = conn;

    public IEnumerable<BagStack> Carried() => conn.Db.MyInventory.Iter()
        .Where(i => i.Count > 0 && Items.IsKnown(i.Item))
        .GroupBy(i => (i.Item, i.Quality))
        .Select(g => new BagStack((Item)g.Key.Item, g.Key.Quality, (uint)g.Sum(i => i.Count)));

    public void Drop(Item item, byte quality) => conn.Reducers.Drop((ushort)item, quality);
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
    static int Group(Item item) => Items.UseOf(item) switch
    {
        UseAction.Eat or UseAction.Drink => 0,
        UseAction.Tool => 1,
        UseAction.Wear => 2,
        _ => 3,
    };

    public void Refresh()
    {
        var stacks = (Source?.Invoke() ?? Link.Carried())
            .OrderBy(s => Group(s.Item)).ThenBy(s => s.Item).ThenByDescending(s => s.Quality).ToList();
        string now = (Capacity?.Invoke() ?? 0) + "|" + string.Join(",", stacks.Select(s => $"{(int)s.Item}:{s.Quality}:{s.Count}"));
        if (now == shown) return;
        shown = now;
        foreach (var child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        foreach (var s in stacks) AddChild(new BagSlotView { Stack = s, Grid = this });
        int room = Capacity?.Invoke() ?? 0;
        for (int i = stacks.Count; i < room; i++) AddChild(new BagSlotView { Grid = this });
        if (stacks.Count == 0 && room == 0)
        {
            var none = Hud.Text(Empty, 14);
            none.Modulate = new Color(1, 1, 1, 0.5f);
            AddChild(none);
        }
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) =>
        data.Obj is Godot.Collections.Dictionary d
        && (Kind == "bag" && (d.ContainsKey("worn") || d.ContainsKey("hotbar"))
            || d.TryGetValue("from", out var from) && from.AsString() != Kind && Accepts(d));

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.Obj is not Godot.Collections.Dictionary d) return;
        if (d.TryGetValue("from", out var from) && from.AsString() != Kind) Receive(d);
        else if (d.TryGetValue("worn", out var slot)) TakeOff((ClothingSlot)slot.AsInt32());
        else if (d.TryGetValue("hotbar", out var i)) ClearHotbar(i.AsInt32());
    }

    // Slots pass drops through to the grid.
    internal bool CanTake(Variant data) => _CanDropData(Vector2.Zero, data);
    internal void Take(Variant data) => _DropData(Vector2.Zero, data);

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

    public override bool _CanDropData(Vector2 atPosition, Variant data) => Grid.CanTake(data);
    public override void _DropData(Vector2 atPosition, Variant data) => Grid.Take(data);

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
        var holder = new Control();
        holder.AddChild(preview);
        SetDragPreview(holder);
        Grid.Dragging = (Stack.Item, Stack.Quality);
        var data = new Godot.Collections.Dictionary
        {
            ["from"] = Grid.Kind, ["item"] = (int)Stack.Item, ["quality"] = (int)Stack.Quality,
            ["count"] = (long)Stack.Count, ["id"] = (long)Stack.Id,
        };
        // Only the bag's own things go onto the hotbar or a worn slot.
        if (Grid.Kind == "bag") data["bag"] = (int)Stack.Item;
        return data;
    }
}
