using System;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// What you wear (D-074): a paper doll with a slot on each part of the body that can be
// dressed. Click a slot, then pick something you carry to put on, or take off what's
// there. I opens and closes it; Esc closes. Main sets Link and Note.
public partial class EquipmentPanel : CanvasLayer
{
    // Talks to the server (animals-9) once Main calls Attach; a mock until then.
    public IEquipLink Link = new MockEquipLink();
    // A short line for the HUD ("You put on the wool tunic").
    public Action<string> Note = _ => { };

    static readonly StringName NEquipment = "equipment";
    const float SlotSize = 66f;

    public bool IsOpen => panel.Visible;

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

        // --screenshot-equip opens the panel with a tunic on and shoes to hand;
        // =feet picks the feet slot.
        foreach (var arg in OS.GetCmdlineUserArgs())
            if (arg.StartsWith("--screenshot-equip") && (previewing = true))
                GetTree().CreateTimer(1.5).Timeout += () =>
                {
                    if (Link.Carried(ClothingSlot.Body).FirstOrDefault() is { } tunic) Link.Wear(tunic);
                    if (arg.EndsWith("=feet")) selected = ClothingSlot.Feet;
                    Open();
                };
    }

    public void Open()
    {
        panel.Visible = true;
        shown = "";
        Refresh();
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
        if (IsOpen) Refresh();
        // The first time you carry something to wear, the one-time hint teaches I.
        else if (live && !hinted && Enum.GetValues<ClothingSlot>().Any(s => Link.Carried(s).Any()))
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

        // The figure, with a slot over each part that can be dressed.
        var doll = new EquipmentFigure { CustomMinimumSize = new Vector2(220, 380) };
        columns.AddChild(doll);
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

        var hint = Hud.Text("Click a slot, then what to put on    I or Esc closes", 13);
        hint.Modulate = new Color(1, 1, 1, 0.65f);
        box.AddChild(hint);

        var outer = Hud.Panel(box, Vector2.Zero);
        var style = (StyleBoxFlat)outer.GetThemeStylebox("panel");
        style.BgColor = new Color(0.10f, 0.11f, 0.09f, 0.82f);
        style.ContentMarginTop = style.ContentMarginBottom = 14;
        style.ContentMarginLeft = style.ContentMarginRight = 18;
        outer.SetAnchorsPreset(Control.LayoutPreset.Center);
        outer.GrowHorizontal = Control.GrowDirection.Both;
        outer.GrowVertical = Control.GrowDirection.Both;
        return outer;
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
        effect.Text = worn != null ? $"{worn.Effect} ({worn.Amount})" : "";
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
                TooltipText = $"{piece.Effect} ({piece.Amount})",
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
