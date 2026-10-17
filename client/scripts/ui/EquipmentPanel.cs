using System;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// What you wear (D-074) and what you carry (ui-18): a paper doll with a slot on each
// part of the body that can be dressed, and the bag beside it. Click a slot, then pick
// something you carry to put on, or take off what's there; or drag between the bag,
// the worn slots and the hotbar, which stays up below while this is open. Dragging
// something out over the world sets it down. I opens and closes it; Esc closes. Main
// sets Link, Note and the Bag's actions.
public partial class EquipmentPanel : CanvasLayer
{
    // Talks to the server (animals-9) once Main calls Attach; a mock until then.
    public IEquipLink Link = new MockEquipLink();
    // A short line for the HUD ("You put on the wool tunic").
    public Action<string> Note = _ => { };

    static readonly StringName NEquipment = "equipment";
    const float SlotSize = 66f;

    public bool IsOpen => panel.Visible;
    // Everything you carry (ui-18); Main sets its link and actions.
    public readonly BagGrid Bag = new();
    // The tools section of the doll (Justin): tools here work by themselves, take no
    // bag space and add no weight. Shown once the server keeps it (Bag.Link.Arranges).
    public const int ToolSlots = 6;
    readonly BagGrid belt = new() { Kind = "belt", Width = 3, Empty = "" };
    Control beltSection = null!;
    Button sort = null!;
    // Whether a point on screen is over this panel.
    public bool Covers(Vector2 at) => IsOpen && panel.GetGlobalRect().HasPoint(at);

    // Swaps the mock for the server link once Main has a connection; a
    // --screenshot-equip preview keeps the mock.
    public void Attach(IEquipLink link)
    {
        if (previewing) return;
        Link = link;
        Link.Said += line => Note(line);
        live = true;
        shown = "";
    }
    bool previewing;

    Control panel = null!;
    readonly System.Collections.Generic.Dictionary<ClothingSlot, Button> slots = new();
    Label slotTitle = null!, wearing = null!, effect = null!;
    Button takeOff = null!;
    VBoxContainer choices = null!;
    ClothingSlot selected = ClothingSlot.Body;
    string shown = "";
    // I opens the panel once connected (Attach), or on the pretend wardrobe with
    // --mock-equip or a --screenshot-equip preview.
    bool live;

    public override void _Ready()
    {
        Layer = 2;
        Link.Said += line => Note(line);
        live = Link is not MockEquipLink
            || OS.GetCmdlineUserArgs().Any(a => a == "--mock-equip" || a.StartsWith("--screenshot-equip"));
        panel = MakePanel();
        panel.Visible = false;
        AddChild(panel);

        // --screenshot-equip opens the panel with the first wearable on and the rest to
        // hand; =feet picks the feet slot.
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--screenshot-equip") && (previewing = true) && (Bag.Link = new MockBagLink(true)) != null)
                GetTree().CreateTimer(1.5).Timeout += () =>
                {
                    // Put on the first piece there is, and show the slot named (=feet), or Body.
                    if (arg.EndsWith("=feet")) selected = ClothingSlot.Feet;
                    var first = Enum.GetValues<ClothingSlot>().SelectMany(Link.Carried).FirstOrDefault();
                    if (first != null) Link.Wear(first);
                    Open();
                };
    }

    public void Open()
    {
        panel.Visible = true;
        shown = "";
        Refresh();
        Bag.Refresh();
        belt.Refresh();
    }

    public void Close() => panel.Visible = false;

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false }) return;
        if (IsOpen && (e.IsActionPressed(NEquipment) || e.IsActionPressed("ui_cancel")))
            Close();
        else if (!IsOpen && live && InputMap.HasAction(NEquipment) && e.IsActionPressed(NEquipment)
                 && !Panels.Open() && GetViewport().GuiGetFocusOwner() == null)
            Open();
        else return;
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (IsOpen)
        {
            Refresh();
            Bag.Refresh();
            belt.Refresh();
            beltSection.Visible = sort.Visible = Bag.Link.Arranges;
        }
        // The first time you carry something, the one-time hint teaches I.
        else if (live && !hinted && Bag.Link.Carried().Any())
        {
            hinted = true;
            Hints.Request(NEquipment);
        }
    }
    bool hinted;

    // --- Layout ------------------------------------------------------------

    Control MakePanel()
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        box.AddChild(Hud.Text("What you wear", 20));

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 26);
        box.AddChild(columns);

        // The figure, with a slot over each part that can be dressed, and the tools below.
        var figure = new VBoxContainer();
        figure.AddThemeConstantOverride("separation", 8);
        columns.AddChild(figure);
        var doll = new EquipmentFigure { CustomMinimumSize = new Vector2(220, 380) };
        figure.AddChild(doll);
        var tools = new VBoxContainer();
        tools.AddThemeConstantOverride("separation", 6);
        var toolsTitle = Hud.Text("Tools", 15);
        toolsTitle.HorizontalAlignment = HorizontalAlignment.Center;
        tools.AddChild(toolsTitle);
        belt.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        tools.AddChild(belt);
        beltSection = tools;
        figure.AddChild(tools);
        WireBelt();
        AddSlot(doll, ClothingSlot.Body, new Vector2(110, 150));
        AddSlot(doll, ClothingSlot.Feet, new Vector2(110, 338));

        // The chosen slot: what's on it, what it does, and what you could wear there.
        var side = new VBoxContainer { CustomMinimumSize = new Vector2(300, 0) };
        side.AddThemeConstantOverride("separation", 6);
        columns.AddChild(side);
        slotTitle = Hud.Text("", 17);
        side.AddChild(slotTitle);
        wearing = Hud.Text("", 15);
        side.AddChild(wearing);
        effect = Hud.Text("", 14);
        effect.Modulate = new Color(1, 1, 1, 0.75f);
        effect.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        side.AddChild(effect);
        takeOff = new Button { Text = "Take it off", FocusMode = Control.FocusModeEnum.None, SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin };
        takeOff.AddThemeFontSizeOverride("font_size", 15);
        takeOff.Pressed += () => Link.TakeOff(selected);
        side.AddChild(takeOff);
        side.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        var could = Hud.Text("You carry", 14);
        could.Modulate = new Color(1, 1, 1, 0.75f);
        side.AddChild(could);
        choices = new VBoxContainer();
        side.AddChild(choices);

        // The bag (ui-18).
        var bagSide = new VBoxContainer();
        bagSide.AddThemeConstantOverride("separation", 10);
        columns.AddChild(bagSide);
        var bagHead = new HBoxContainer();
        bagHead.AddThemeConstantOverride("separation", 12);
        bagHead.AddChild(Hud.Text("What you carry", 17));
        bagHead.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        sort = new Button { Text = "Sort", FocusMode = Control.FocusModeEnum.None, Flat = true, TooltipText = "Put the bag in order by kind" };
        sort.AddThemeFontSizeOverride("font_size", 14);
        sort.Pressed += () => Bag.Link.Sort();
        bagHead.AddChild(sort);
        bagSide.AddChild(bagHead);
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(6 * BagGrid.Slot + 5 * 10 + 14, 330),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        scroll.AddChild(Bag);
        bagSide.AddChild(scroll);

        var hint = Hud.Text("Drag things onto the bar below, onto a slot to wear them, tools onto Tools, or out onto the ground    Double-click uses    I or Esc closes", 13);
        hint.Modulate = new Color(1, 1, 1, 0.65f);
        box.AddChild(hint);

        var outer = Ui.Window(box);
        return outer;
    }

    // The belt and the bag's own order: drag a tool from the bag onto the belt and back,
    // drag within either to rearrange. The server checks each move (ui-18).
    void WireBelt()
    {
        belt.Positioned = () => true;
        belt.Capacity = () => ToolSlots;
        belt.Source = () => Bag.Link.Belt();
        belt.Accepts = d => d["from"].AsString() == "bag" && Items.UseOf((Item)d["item"].AsInt32()) == UseAction.Tool;
        belt.ReceiveAt = (d, cell) => Bag.Link.ToBelt((ulong)d["id"].AsInt64(), cell);
        belt.Arrange = (d, cell) => Bag.Link.ToBelt((ulong)d["id"].AsInt64(), cell);
        Bag.Positioned = () => Bag.Link.Arranges;
        Bag.Arrange = (d, cell) => Bag.Link.MoveTo((ulong)d["id"].AsInt64(), cell);
        Bag.Accepts = d => d["from"].AsString() == "belt";
        Bag.ReceiveAt = (d, cell) => Bag.Link.FromBelt((ulong)d["id"].AsInt64(), cell);
    }

    void AddSlot(Control doll, ClothingSlot slot, Vector2 centre)
    {
        var button = new Button
        {
            FocusMode = Control.FocusModeEnum.None,
            Size = new Vector2(SlotSize, SlotSize),
            Position = centre - new Vector2(SlotSize, SlotSize) / 2,
            ExpandIcon = true,
            IconAlignment = HorizontalAlignment.Center,
            TooltipText = slot.ToString(),
        };
        button.AddThemeFontSizeOverride("font_size", 13);
        button.Pressed += () => { selected = slot; shown = ""; };
        // Drag a worn piece back onto the bag to take it off, or something you carry onto the slot to put it on.
        button.SetDragForwarding(
            Callable.From((Vector2 _) => Link.Worn(slot) is { } on
                ? (Variant)new Godot.Collections.Dictionary { ["worn"] = (int)slot, ["item"] = (int)on.Item }
                : default),
            Callable.From((Vector2 _, Variant data) => data.Obj is Godot.Collections.Dictionary d
                && d.TryGetValue("bag", out var it) && Clothing.Find((ushort)it.AsInt32())?.Slot == slot),
            Callable.From((Vector2 _, Variant data) =>
            {
                if (data.Obj is not Godot.Collections.Dictionary d) return;
                selected = slot;
                Link.Wear(new WornPiece((Item)d["bag"].AsInt32(), (byte)d["quality"].AsInt32(), slot));
            }));
        doll.AddChild(button);
        slots[slot] = button;
    }

    static StyleBoxFlat SlotStyle(bool chosen, bool filled)
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.10f, 0.07f, filled ? 0.8f : 0.55f),
            BorderColor = new Color(Look.Cream, chosen ? 0.9f : 0.3f),
            ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 6, ContentMarginBottom = 6,
        };
        style.SetBorderWidthAll(chosen ? 2 : 1);
        style.SetCornerRadiusAll((int)(SlotSize / 2) - 2);
        return style;
    }

    // --- Contents ----------------------------------------------------------

    void Refresh()
    {
        var carried = Link.Carried(selected).ToList();
        string now = selected + "|" + string.Join(",", Enum.GetValues<ClothingSlot>().Select(s => Link.Worn(s)))
            + "|" + string.Join(",", carried);
        if (now == shown) return;
        shown = now;

        foreach (var (slot, button) in slots)
        {
            var on = Link.Worn(slot);
            button.Icon = on != null ? ItemIcon.For(on.Item) : null;
            button.Text = on != null ? "" : slot.ToString();
            var style = SlotStyle(slot == selected, on != null);
            foreach (var state in new[] { "normal", "hover", "pressed", "focus" })
                button.AddThemeStyleboxOverride(state, style);
            button.AddThemeColorOverride("font_color", new Color(Look.Cream, 0.55f));
        }

        var worn = Link.Worn(selected);
        slotTitle.Text = selected.ToString();
        wearing.Text = worn != null ? $"Wearing {Label(worn)}" : "Nothing on";
        wearing.Modulate = new Color(1, 1, 1, worn != null ? 1f : 0.55f);
        // Words only; effect values stay hidden (D-107).
        effect.Text = worn?.Effect ?? "";
        takeOff.Visible = worn != null;

        foreach (var child in choices.GetChildren()) { choices.RemoveChild(child); child.QueueFree(); }
        foreach (var piece in carried)
        {
            var button = new Button
            {
                Text = Label(piece),
                Icon = ItemIcon.For(piece.Item),
                Flat = true,
                Alignment = HorizontalAlignment.Left,
                FocusMode = Control.FocusModeEnum.None,
                TooltipText = piece.Effect,
            };
            button.AddThemeFontSizeOverride("font_size", 15);
            button.AddThemeConstantOverride("icon_max_width", 30);
            button.Pressed += () => Link.Wear(piece);
            choices.AddChild(button);
        }
        if (carried.Count == 0)
        {
            var none = Hud.Text($"Nothing you carry goes on your {selected.ToString().ToLowerInvariant()}", 14);
            none.Modulate = new Color(1, 1, 1, 0.5f);
            choices.AddChild(none);
        }
    }

    static string Label(WornPiece w) => ItemLook.Label(w.Item, w.Quality, 1);

    // HUD lines for putting things on and taking them off.
    public static string PutOn(Item item) => $"You put on the {Plain(item)}";
    public static string TookOff(Item item) => $"You take off the {Plain(item)}";
    static string Plain(Item item) => Items.All.TryGetValue(item, out var def) ? def.Count(1).ToLowerInvariant() : "it";
}

// A plain standing figure, drawn faint so the slots read on top of it.
public partial class EquipmentFigure : Control
{
    public override void _Ready() => MouseFilter = MouseFilterEnum.Pass;

    public override void _Draw()
    {
        var fill = new Color(Look.Cream, 0.13f);
        var edge = new Color(Look.Cream, 0.32f);
        DrawCircle(new Vector2(110, 46), 26, fill);
        DrawArc(new Vector2(110, 46), 26, 0, Mathf.Tau, 48, edge, 1.5f, true);
        Part(new Rect2(96, 70, 28, 14), 4, fill, edge);    // neck
        Part(new Rect2(66, 84, 88, 128), 22, fill, edge);  // body
        Part(new Rect2(38, 92, 24, 118), 12, fill, edge);  // arms
        Part(new Rect2(158, 92, 24, 118), 12, fill, edge);
        Part(new Rect2(74, 206, 32, 118), 14, fill, edge); // legs
        Part(new Rect2(114, 206, 32, 118), 14, fill, edge);
    }

    void Part(Rect2 rect, int radius, Color fill, Color edge)
    {
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = edge, AntiAliasing = true };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(radius);
        DrawStyleBox(style, rect);
    }
}
