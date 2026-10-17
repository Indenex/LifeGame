using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The hotbar (ui-19, Justin: "assign keybinds to slots on the bottom of the screen.
// These slots should fill up to 10 slots, and have the item be usable by hitting the
// assigned keybind"). Ten round slots along the bottom edge in the bag row's quiet
// style (D-045), each with its key in the corner. A new kind of item takes the first
// empty slot by itself; drag a slot onto another to swap them, or off the bar to clear
// it. A slot's key takes its item in hand and again puts it away (crafting-29, Justin:
// "for a tool to be used, it needs to be on your bar and equipped", then "every slot
// equips"); the slot in hand wears a gold ring. A click in the world uses what you hold:
// eat, drink, put on, or set a made thing down; with a tool or weapon it is the strike as
// before, and E still chops or digs with the tool in hand. A slot keeps its item while
// you have none left, shown faded.
// The row dims when nothing changes and brightens when you gain or use something; a
// newly gained item's name floats up above its slot.
public partial class HotbarBar : Control
{
    public const int Slots = IHotbarLink.Slots;
    public const float Slot = 54f;
    const float IdleAlpha = 0.45f, IdleAfter = 6f, Gap = 12f;

    public IHotbarLink Link { get; private set; } = new MockHotbarLink();
    // True while something else has the keys (a panel, chat, building).
    public Func<bool> Blocked = () => false;
    public Action<string> Note = _ => { };
    // A placeable item's key: Main hands it to Placing, which opens the ghost (ui-16).
    public event Action<Item>? PlaceRequested;

    readonly HBoxContainer row = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly HotbarSlotView[] views = new HotbarSlotView[Slots];
    readonly Label name = new() { HorizontalAlignment = HorizontalAlignment.Center };
    readonly Dictionary<Item, uint> carried = new();
    readonly Dictionary<Item, string> labels = new();
    float sinceChange = IdleAfter, nameTime, alpha = 1f, cover;
    int nameAt = -1;
    string? floatingName;
    // When gains may sound and fill slots; set on the first update after login.
    double quietUntil = -1;
    bool filledOnce;

    // How far above the bottom edge the row reaches; notes sit above it.
    public float Top => Size.Y - row.Position.Y;
    // Where the slots are on screen (a drag let go here isn't over the world).
    public Rect2 RowRect => row.GetGlobalRect().Grow(10f);
    // Stays up over a panel that drags onto it (the bag, ui-18).
    public Func<bool> ShowOverPanels = () => false;

    public void RequestPlace(Item item) => PlaceRequested?.Invoke(item);

    static readonly StringName[] Actions = Enumerable.Range(1, Slots).Select(i => new StringName($"hotbar_{i}")).ToArray();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        row.AddThemeConstantOverride("separation", (int)Gap);
        AddChild(row);
        for (int i = 0; i < Slots; i++)
        {
            views[i] = new HotbarSlotView { Index = i, Bar = this };
            row.AddChild(views[i]);
        }
        Hud.Style(name, 14, 0.85f);
        name.Modulate = new Color(1, 1, 1, 0);
        AddChild(name);
        Attach(Link);
        // --screenshot-hotbar: berries, a hand axe and a stone axe, with the stone axe in hand.
        if (OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--screenshot-hotbar")) && Link is MockHotbarLink mock)
        {
            SetBag(new[] { new InventoryEntry(Item.Berries, "Berries", 6), new InventoryEntry(Item.HandAxe, "Hand axe", 1), new InventoryEntry(Item.StoneAxe, "Stone axe", 1) });
            mock.Set(0, Item.Berries);
            mock.Set(1, Item.HandAxe);
            mock.Set(2, Item.StoneAxe);
            Equip(2, Item.StoneAxe);
        }
    }

    public void Attach(IHotbarLink link)
    {
        Link = link;
        Link.Changed += Refresh;
        filledOnce = false;
        Refresh();
    }

    // Everything you carry, from Main whenever the inventory changes.
    public void SetBag(IReadOnlyList<InventoryEntry> items)
    {
        var before = new Dictionary<Item, uint>(carried);
        carried.Clear();
        labels.Clear();
        foreach (var e in items)
        {
            carried[e.Item] = carried.GetValueOrDefault(e.Item) + e.Count;
            labels.TryAdd(e.Item, e.Label);
        }

        double now = Time.GetTicksMsec() / 1000.0;
        if (quietUntil < 0) quietUntil = now + 2.0;
        bool quiet = now < quietUntil && Link is ServerHotbarLink;
        var gained = items.Select(e => e.Item).Distinct()
            .Where(i => carried[i] > before.GetValueOrDefault(i)).ToList();
        if (gained.Count > 0 && !quiet) Sfx.Play(Cue.Pickup);

        // A new kind of item takes the first empty slot (Justin's "fill up to 10").
        // At login the bar fills once from the whole bag if it is empty (_Process).
        if (!quiet && filledOnce)
            foreach (var item in gained.Where(i => !before.ContainsKey(i))) Fill(item);

        if (gained.Count > 0 || before.Count != carried.Count || before.Any(kv => carried.GetValueOrDefault(kv.Key) != kv.Value))
        {
            sinceChange = 0f;
            if (gained.LastOrDefault() is var g && gained.Count > 0)
            {
                floatingName = labels[g];
                nameAt = SlotOf(g);
                nameTime = 2.6f;
            }
        }
        Refresh();
    }

    void Fill(Item item)
    {
        // Tools too since crafting-29: a tool works only from the bar, equipped.
        if (SlotOf(item) >= 0) return;
        int free = Enumerable.Range(0, Slots).FirstOrDefault(i => Link.SlotItem(i) == null, -1);
        if (free < 0) return;
        Link.Set(free, item);
        Hints.Request("hotbar_1");
    }

    int SlotOf(Item item) => Enumerable.Range(0, Slots).FirstOrDefault(i => Link.SlotItem(i) == item, -1);

    void Refresh()
    {
        for (int i = 0; i < Slots; i++)
        {
            var item = Link.SlotItem(i);
            uint n = item is { } it ? carried.GetValueOrDefault(it) : 0;
            views[i].Show(item, n, item is { } x && labels.TryGetValue(x, out var l) ? l : item is { } y ? ItemName(y) : "",
                KeyName(i), equipped: item != null && Link.Equipped == i);
        }
    }

    static string ItemName(Item item) => Items.All.TryGetValue(item, out var def) ? def.Name : "";

    static string KeyName(int i)
    {
        var events = InputMap.HasAction(Actions[i]) ? InputMap.ActionGetEvents(Actions[i]) : new Godot.Collections.Array<InputEvent>();
        return events.FirstOrDefault() is InputEventKey k ? OS.GetKeycodeString(k.PhysicalKeycode) : "";
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // A click in the world uses what you hold (Justin's pick (b): "every slot equips").
        if (InputMap.HasAction("strike") && e.IsActionPressed("strike") && !e.IsEcho() && !Blocked() && HeldUse() is { } use)
        {
            use();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (e is not InputEventKey { Pressed: true, Echo: false } || Blocked()) return;
        for (int i = 0; i < Slots; i++)
        {
            if (!InputMap.HasAction(Actions[i]) || !e.IsActionPressed(Actions[i])) continue;
            UseSlot(i);
            GetViewport().SetInputAsHandled();
            return;
        }
    }

    // A slot's key takes its item in hand, and again puts it away (crafting-29; Justin: "for a
    // tool to be used, it needs to be on your bar and equipped", then "every slot equips").
    // An empty slot's key puts away what you hold. A click then uses what you hold (HeldUse).
    public void UseSlot(int i)
    {
        if (Link.SlotItem(i) is not { } item)
        {
            if (Link.Equipped >= 0) Link.Equip(-1);
            return;
        }
        views[i].Pop();
        Equip(i, item);
    }

    void Equip(int i, Item item)
    {
        sinceChange = 0f;
        bool away = Link.Equipped == i;
        Link.Equip(i);
        string what = labels.TryGetValue(item, out var l) ? l : ItemName(item);
        floatingName = away ? $"{what} put away" : ClickVerb(item) is { } verb ? $"{what} in hand    Click to {verb}" : $"{what} in hand";
        nameAt = i;
        nameTime = away ? 1.6f : 2.6f;
    }

    // The item in hand, if any.
    public Item? Held => Link.Equipped >= 0 ? Link.SlotItem(Link.Equipped) : null;

    // What a click does with the item in hand, or null when a click is the strike as before
    // (tools and weapons swing, and E still chops or digs with the tool in hand).
    static string? ClickVerb(Item item) =>
        Placeable(item) ? "set it down"
        : Items.UseOf(item) switch { UseAction.Eat => "eat", UseAction.Drink => "drink", UseAction.Wear => "put it on", _ => null };

    // True while a click would use the held item, so the strike leaves the click alone (Main).
    public bool ClickUsesHeld => !Blocked() && Held is { } h && ClickVerb(h) != null;

    // The click: a placeable starts placing; anything else goes to the server's UseHeld, which
    // uses whatever it has in your hand (crafting-29, D-182).
    Action? HeldUse() => Held is { } item && ClickVerb(item) != null
        ? () =>
        {
            sinceChange = 0f;
            if (Placeable(item)) PlaceRequested?.Invoke(item);
            else Link.UseHeld();
        }
        : null;

    // Puts an item on the bar if it isn't there and takes its slot in hand, so the server's
    // "use or set down only what is in hand" holds for bag clicks and drags too. The calls
    // reach the server in order, before the use that follows. False when the bar is full.
    public bool TakeInHand(Item item)
    {
        int slot = SlotOf(item);
        if (slot < 0)
        {
            slot = Enumerable.Range(0, Slots).FirstOrDefault(i => Link.SlotItem(i) == null, -1);
            if (slot < 0) { Note("Make room on your bar first: you use things from your hand"); return false; }
            Link.Set(slot, item);
        }
        if (Link.Equipped != slot) Link.Equip(slot);
        return true;
    }

    // Uses one carried item: places it, or asks the server to eat, drink, wear or hold it.
    public void UseItem(Item item)
    {
        sinceChange = 0f;
        string what = labels.TryGetValue(item, out var l) ? l : ItemName(item);
        if (carried.GetValueOrDefault(item) == 0) { Note($"You have no {what.ToLowerInvariant()} left"); return; }
        if (Placeable(item)) { PlaceRequested?.Invoke(item); return; }
        if (Items.UseOf(item) == UseAction.None) { Note($"{what} is used in making things"); return; }
        // A tool from the bag: into your hand, ready to work with.
        if (Items.UseOf(item) == UseAction.Tool)
        {
            if (TakeInHand(item))
            {
                floatingName = $"{what} in hand";
                nameAt = SlotOf(item);
                nameTime = 1.6f;
            }
            return;
        }
        if (TakeInHand(item)) Link.Use(item);
    }

    // Whether an item is set down rather than used: Main reads ItemDef.Placeable (crafting-21).
    public static Func<Item, bool> Placeable = _ => false;

    // Drags between slots and off the bar (HotbarSlotView calls these).
    internal void Dropped(int from, int to)
    {
        if (from != to) Link.Move(from, to);
    }

    // The slot being dragged, if any (the drag data is gone by the time it ends).
    internal int DraggingFrom = -1;

    public override void _Notification(int what)
    {
        if (what != NotificationDragEnd || DraggingFrom < 0) return;
        // A slot let go anywhere that didn't take it comes off the bar; the items stay in the bag.
        if (!GetViewport().GuiIsDragSuccessful()) Link.Set(DraggingFrom, null);
        DraggingFrom = -1;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        // At login, once the rows have settled, an empty bar fills from the bag.
        if (!filledOnce && quietUntil > 0 && Time.GetTicksMsec() / 1000.0 >= quietUntil)
        {
            filledOnce = true;
            if (Enumerable.Range(0, Slots).All(i => Link.SlotItem(i) == null))
                foreach (var item in carried.Keys.OrderBy(i => i)) Fill(item);
        }
        sinceChange += dt;
        alpha = Mathf.MoveToward(alpha, sinceChange < IdleAfter ? 1f : IdleAlpha, dt * (sinceChange < IdleAfter ? 4f : 0.6f));
        // Steps aside while a panel covers the world.
        cover = Mathf.MoveToward(cover, Panels.Open() && !ShowOverPanels() ? 1f : 0f, dt * 6f);
        row.Modulate = new Color(1, 1, 1, alpha * (1f - cover));
        nameTime -= dt;
        name.Text = floatingName ?? "";
        name.Modulate = new Color(1, 1, 1, Mathf.Clamp(nameTime, 0f, 1f) * (1f - cover));

        // Centred along the bottom edge; a narrow window shrinks it a little.
        float natural = Slots * Slot + (Slots - 1) * Gap;
        float scale = Mathf.Clamp((Size.X - 80f) / natural, 0.7f, 1f);
        row.Scale = new Vector2(scale, scale);
        row.Size = new Vector2(natural, Slot);
        row.Position = new Vector2((Size.X - natural * scale) / 2, Size.Y - Slot * scale - 22);
        var nameSize = name.GetCombinedMinimumSize();
        name.Size = nameSize;
        float overX = nameAt >= 0 ? (nameAt * (Slot + Gap) + Slot / 2) * scale : natural * scale / 2;
        name.Position = row.Position + new Vector2(overX - nameSize.X / 2, -nameSize.Y - 6);
    }
}

// One round slot of the hotbar: the item's picture, how many you carry, the key in the
// corner. Drag it onto another slot to swap them.
public partial class HotbarSlotView : PanelContainer
{
    public int Index;
    public HotbarBar Bar = null!;

    readonly StyleBoxFlat style = new()
    {
        BgColor = new Color(0.09f, 0.10f, 0.07f, 0.62f),
        BorderColor = new Color(Look.Cream, 0.28f),
        ShadowColor = new Color(0, 0, 0, 0.25f),
        ShadowSize = 6,
        ShadowOffset = new Vector2(0, 2),
    };
    readonly TextureRect picture = new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        MouseFilter = MouseFilterEnum.Ignore,
    };
    Label count = null!, key = null!;
    Item? item;
    float pop;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(HotbarBar.Slot, HotbarBar.Slot);
        MouseFilter = MouseFilterEnum.Stop;
        style.SetBorderWidthAll(1);
        // Just under half the slot: a radius of exactly half leaves a seam down the middle.
        style.SetCornerRadiusAll((int)(HotbarBar.Slot / 2) - 2);
        AddThemeStyleboxOverride("panel", style);
        PivotOffset = new Vector2(HotbarBar.Slot, HotbarBar.Slot) / 2;

        var stack = new Control { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(stack);
        picture.SetAnchorsPreset(LayoutPreset.FullRect);
        picture.OffsetLeft = picture.OffsetTop = 2;
        picture.OffsetRight = picture.OffsetBottom = -2;
        stack.AddChild(picture);

        count = Hud.Text("", 13);
        count.SetAnchorsPreset(LayoutPreset.FullRect);
        count.HorizontalAlignment = HorizontalAlignment.Right;
        count.VerticalAlignment = VerticalAlignment.Bottom;
        count.OffsetRight = -3;
        count.OffsetBottom = 1;
        stack.AddChild(count);

        // The key sits just outside the top-left of the ring, small and soft.
        key = Hud.Text("", 11);
        key.Modulate = new Color(1, 1, 1, 0.7f);
        key.Position = new Vector2(-4, -8);
        stack.AddChild(key);
    }

    // The slot in hand wears a warm gold ring (crafting-29).
    static readonly Color InHand = new(0.96f, 0.78f, 0.42f);

    public void Show(Item? what, uint n, string label, string keyName, bool equipped = false)
    {
        item = what;
        picture.Texture = what is { } i ? ItemIcon.For(i) : null;
        // A slot whose item has run out keeps its picture, faded.
        picture.Modulate = new Color(1, 1, 1, n > 0 ? 1f : 0.3f);
        count.Text = n > 1 ? n.ToString() : "";
        key.Text = keyName;
        if (WarmLook.On)
        {
            // Paper buttons (ui-45, D-313): cream discs with a soft brown rim and shadow; the slot
            // in hand wears a butter-gold ring. An empty slot is paler.
            // ui-59: an empty slot is a slightly darker paper with a clear brown edge, so the bar
            // reads on bright grass.
            style.BgColor = equipped ? new Color(1f, 0.93f, 0.78f, 0.97f) : what == null ? new Color(WarmLook.Paper.Darkened(0.08f), 0.85f) : new Color(WarmLook.Paper, 0.95f);
            style.BorderColor = equipped ? WarmLook.Butter.Darkened(0.12f) : new Color(WarmLook.Ink, what == null ? 0.45f : 0.5f);
            style.SetBorderWidthAll(equipped ? 3 : 2);
            style.ShadowColor = equipped ? new Color(WarmLook.Butter, 0.55f) : new Color(0.20f, 0.10f, 0.05f, 0.30f);
            style.ShadowSize = equipped ? 9 : 6;
            InkLabels(equipped);
        }
        else
        {
            style.BorderColor = equipped ? InHand : new Color(Look.Cream, what == null ? 0.14f : 0.28f);
            style.SetBorderWidthAll(equipped ? 2 : 1);
            style.BgColor = equipped ? new Color(0.20f, 0.16f, 0.08f, 0.78f) : new Color(0.09f, 0.10f, 0.07f, what == null ? 0.35f : 0.62f);
            style.ShadowColor = equipped ? new Color(InHand, 0.35f) : new Color(0, 0, 0, 0.25f);
        }
        TooltipText = equipped ? $"{label} (in hand)" : label;
    }

    // On paper the count is brown ink; the key sits outside the disc, so it keeps its soft
    // shadow and gets a warm one.
    void InkLabels(bool equipped)
    {
        count.AddThemeColorOverride("font_color", WarmLook.Ink);
        count.AddThemeColorOverride("font_shadow_color", new Color(1f, 1f, 1f, 0.6f));
        key.AddThemeColorOverride("font_color", new Color(1f, 0.97f, 0.9f));
        key.AddThemeColorOverride("font_shadow_color", new Color(0.22f, 0.12f, 0.06f, 0.75f));
        key.Modulate = new Color(1, 1, 1, equipped ? 1f : 0.85f);
    }

    // A small bounce when its key is pressed.
    public void Pop() => pop = 1f;

    public override void _Process(double delta)
    {
        pop = Mathf.MoveToward(pop, 0f, (float)delta * 5f);
        float s = 1f + 0.12f * Mathf.Sin(pop * Mathf.Pi);
        Scale = new Vector2(s, s);
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (item is not { } i) return default;
        var preview = new TextureRect
        {
            Texture = ItemIcon.For(i),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Size = new Vector2(HotbarBar.Slot, HotbarBar.Slot),
            Position = -new Vector2(HotbarBar.Slot, HotbarBar.Slot) / 2,
            Modulate = new Color(1, 1, 1, 0.8f),
        };
        var holder = new Control();
        holder.AddChild(preview);
        SetDragPreview(holder);
        Bar.DraggingFrom = Index;
        return new Godot.Collections.Dictionary { ["hotbar"] = Index, ["item"] = (int)i };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) =>
        data.Obj is Godot.Collections.Dictionary d && (d.ContainsKey("hotbar") || d.ContainsKey("bag"));

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (data.Obj is not Godot.Collections.Dictionary d) return;
        if (d.TryGetValue("hotbar", out var from)) Bar.Dropped(from.AsInt32(), Index);
        // From the bag panel (ui-18): put that kind of item on this slot.
        else if (d.TryGetValue("bag", out var it)) Bar.Link.Set(Index, (Item)it.AsInt32());
    }
}
