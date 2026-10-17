using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The admin item menu (admin-1). /give opens a searchable list of every item; pick one,
// set how many (and how good, for made things) and press Give. The server decides who
// may (admin_give) and the panel only appears for players the server says are admins.
// The list comes from the item registry, so new items show up by themselves.
public partial class AdminPanel : CanvasLayer
{
    const int MaxRows = 60;

    public Action<string> Note = _ => { };
    public bool IsOpen => window?.Visible == true;

    DbConnection? conn;
    Control window = null!;
    LineEdit search = null!;
    VBoxContainer rows = null!;
    Label picked = null!, hint = null!;
    SpinBox quantity = null!, quality = null!;
    Button give = null!;
    ItemDef? selected;

    public void Attach(DbConnection c) => conn = c;

    public override void _Ready()
    {
        Layer = 3;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(520, 560) };
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        window = panel;
        window.Visible = false;
        root.AddChild(window);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        panel.AddChild(box);

        box.AddChild(new Label { Text = "Items (admin)" });
        search = new LineEdit { PlaceholderText = "Search by name", ClearButtonEnabled = true };
        search.TextChanged += _ => Refill();
        box.AddChild(search);

        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(rows);
        box.AddChild(scroll);

        hint = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        box.AddChild(hint);

        picked = new Label { Text = "Pick an item" };
        box.AddChild(picked);

        var line = new HBoxContainer();
        line.AddThemeConstantOverride("separation", 8);
        line.AddChild(new Label { Text = "How many" });
        quantity = new SpinBox { MinValue = 1, MaxValue = AdminRules.GiveMax, Value = 1, Step = 1 };
        line.AddChild(quantity);
        line.AddChild(new Label { Text = "Quality" });
        quality = new SpinBox { MinValue = 1, MaxValue = 100, Value = 80, Step = 1 };
        line.AddChild(quality);
        give = new Button { Text = "Give", Disabled = true };
        give.Pressed += Give;
        line.AddChild(give);
        box.AddChild(line);
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (IsOpen && e is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    // Opens the menu (the /give command; the caller has checked the player is an admin).
    public void Show(string prefill = "")
    {
        window.Visible = true;
        search.Text = prefill;
        Refill();
        search.GrabFocus();
    }

    void Close()
    {
        window.Visible = false;
        GetViewport().GuiReleaseFocus();
    }

    void Refill()
    {
        foreach (var child in rows.GetChildren()) child.QueueFree();
        string q = search.Text.Trim();
        var found = ItemRegistry().Where(d => q.Length == 0 || d.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || d.Plural.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var def in found.Take(MaxRows))
        {
            var row = new Button { Alignment = HorizontalAlignment.Left, Text = def.Name, Flat = true, ExpandIcon = false };
            if (ItemIcon.For(def.Id) is { } icon) { row.Icon = icon; row.ExpandIcon = true; row.CustomMinimumSize = new Vector2(0, 40); }
            var picks = def;
            row.Pressed += () => Pick(picks);
            rows.AddChild(row);
        }
        hint.Text = found.Count > MaxRows ? $"{found.Count} items match; showing {MaxRows}. Type more to narrow it." : found.Count == 0 ? "Nothing matches." : "";
    }

    static System.Collections.Generic.IEnumerable<ItemDef> ItemRegistry() => Items.All.Values.OrderBy(d => d.Name);

    void Pick(ItemDef def)
    {
        selected = def;
        picked.Text = $"{def.Name}  (item {(ushort)def.Id})";
        quality.Editable = def.HasQuality;
        give.Disabled = false;
    }

    void Give()
    {
        if (selected == null || conn == null) return;
        uint n = (uint)Mathf.Clamp(quantity.Value, 1, AdminRules.GiveMax);
        conn.Reducers.AdminGive((ushort)selected.Id, n, (byte)Mathf.Clamp(quality.Value, 1, 100));
        Note($"Gave yourself {n} x {selected.Name}");
    }
}
