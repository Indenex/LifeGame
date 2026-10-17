using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-31: the trading stall's window, opened with Use at a finished stall. Everyone sees the
// lots set out and buys one with a click; the claim's owner and members also set lots out from
// their bag (pick a thing, the price and how many), take lots back, empty the till and read the
// sales. The server decides everything (Stall.cs); this only shows and asks.
public partial class StallPanel : CanvasLayer
{
    public DbConnection? Conn;
    // Whether the local player keeps the stall at `id` (the claim's owner or a member).
    public Func<ulong, bool> Keeps = _ => false;

    ulong? stallId;
    bool dirty;
    VBoxContainer box = null!;
    Label title = null!;
    VBoxContainer lots = null!, keeper = null!;
    BagGrid carried = null!;
    OptionButton priceItem = null!;
    SpinBox priceCount = null!, setCount = null!;
    BagStack? picked;
    // The Use press that opened the window must not also close it.
    ulong openedFrame;

    public bool IsOpen => Visible && stallId != null;
    public ulong? OpenId => stallId;
    public bool Typing => priceCount.GetLineEdit().HasFocus() || setCount.GetLineEdit().HasFocus();

    public override void _Ready()
    {
        Layer = 2;
        Visible = false;
        Panels.Centre(this, Close);
        box = new VBoxContainer { CustomMinimumSize = new Vector2(460, 0) };
        box.AddThemeConstantOverride("separation", 8);
        title = Ui.Title("Trading stall");
        box.AddChild(title);
        lots = new VBoxContainer();
        lots.AddThemeConstantOverride("separation", 4);
        box.AddChild(lots);

        keeper = new VBoxContainer();
        keeper.AddThemeConstantOverride("separation", 6);
        carried = new BagGrid
        {
            Kind = "stallbag", Width = 7, Empty = "Nothing to set out",
            Source = () => Conn != null
                ? Conn.Db.MyInventory.Iter().Where(i => i.Count > 0 && !i.OnBelt).Select(i => new BagStack((Item)i.Item, i.Quality, i.Count, 0, -1, i.Maker != 0 ? i.Maker : Marks.MakerOf((Item)i.Item, i.Wear)))
                : Enumerable.Empty<BagStack>(),
            Clicked = (s, _) => { picked = s; dirty = true; },
            Accepts = _ => false,
        };
        priceItem = new OptionButton { FocusMode = Control.FocusModeEnum.None };
        foreach (var (item, def) in Items.All.OrderBy(p => p.Value.Name))
            priceItem.AddItem(def.Name, (int)item);
        priceCount = new SpinBox { MinValue = 1, MaxValue = Stalls.MaxPrice, Value = 1 };
        setCount = new SpinBox { MinValue = 1, MaxValue = 999, Value = 1 };

        var hint = Hud.Text($"{Controls.KeyFor("use") ?? "E"} or Esc closes", 13);
        hint.Modulate = new Color(1, 1, 1, 0.65f);
        box.AddChild(keeper);
        box.AddChild(hint);
        AddChild(Ui.Window(box));
    }

    public void OpenAt(ulong id)
    {
        Panels.Opening(this);
        stallId = id;
        openedFrame = Engine.GetProcessFrames();
        picked = null;
        Visible = true;
        dirty = true;
    }

    public void Close()
    {
        stallId = null;
        Visible = false;
    }

    public void MarkDirty() => dirty = true;

    public override void _Process(double delta)
    {
        if (IsOpen && dirty) Rebuild();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!IsOpen || Engine.GetProcessFrames() == openedFrame || !(e.IsActionPressed("build_cancel") || e.IsActionPressed("use"))) return;
        Close();
        GetViewport().SetInputAsHandled();
    }

    void Rebuild()
    {
        dirty = false;
        if (Conn == null || stallId is not { } id) return;
        if (Conn.Db.Structure.Id.Find(id) is not { Complete: true }) { Close(); return; }
        bool mine = Keeps(id);
        var owner = Conn.Db.Claim.Iter().FirstOrDefault(c => c.ExpiresAt.MicrosecondsSinceUnixEpoch > SocialLayer.NowMicros
            && Conn.Db.Structure.Id.Find(id) is { } s && Social.InClaim(c.X, c.Z, s.X, s.Z))?.OwnerName;
        title.Text = owner != null ? $"{owner}'s stall" : "Trading stall";

        Clear(lots);
        var rows = Conn.Db.StallLot.Iter().Where(l => l.StructureId == id).OrderBy(l => l.Id).ToList();
        if (rows.Count == 0) lots.AddChild(Note(mine ? "Nothing set out yet. Pick something you carry below." : "Nothing for sale just now."));
        foreach (var lot in rows)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            var text = Hud.Text($"{lot.Count} {Name(lot.Item, lot.Count)}{Grade(lot.Quality)}    each for {lot.PriceCount} {Name(lot.PriceItem, lot.PriceCount)}", 15);
            text.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(text);
            ulong lotId = lot.Id;
            if (mine) row.AddChild(Small("Take back", () => Conn?.Reducers.TakeBackFromStall(lotId)));
            else row.AddChild(Small("Buy one", () => Conn?.Reducers.BuyFromStall(lotId, 1)));
            lots.AddChild(row);
        }

        keeper.Visible = mine;
        Clear(keeper);
        if (!mine) return;
        keeper.AddChild(Heading("Set something out"));
        keeper.AddChild(carried);
        carried.Refresh();
        if (picked is { } p && Conn.Db.MyInventory.Iter().Any(i => i.Item == (ushort)p.Item && i.Quality == p.Quality && !i.OnBelt))
        {
            var ask = new HBoxContainer();
            ask.AddThemeConstantOverride("separation", 8);
            setCount.MaxValue = Math.Max(1, p.Count);
            ask.AddChild(Hud.Text("Set out", 14));
            ask.AddChild(setCount);
            ask.AddChild(Hud.Text($"{Name((ushort)p.Item, 2)}, each for", 14));
            ask.AddChild(priceCount);
            ask.AddChild(priceItem);
            keeper.AddChild(ask);
            keeper.AddChild(Ui.MainButton("Set out", () =>
            {
                if (stallId is { } at && picked is { } q)
                    Conn?.Reducers.SetOutOnStall(at, (ushort)q.Item, q.Quality, (uint)setCount.Value, (ushort)priceItem.GetSelectedId(), (uint)priceCount.Value);
                picked = null;
                dirty = true;
            }));
        }
        else keeper.AddChild(Note("Click something you carry to set it out."));

        var till = Conn.Db.MyStallTill.Iter().Where(t => t.StructureId == id).ToList();
        keeper.AddChild(Heading("The till"));
        if (till.Count == 0) keeper.AddChild(Note("Empty."));
        else
        {
            keeper.AddChild(Note(string.Join(", ", till.GroupBy(t => t.Item).Select(g => $"{g.Sum(t => t.Count)} {Name(g.Key, (uint)g.Sum(t => t.Count))}"))));
            keeper.AddChild(Small("Take it all", () => Conn?.Reducers.TakeFromStallTill(id)));
        }
        var sales = Conn.Db.MyStallSales.Iter().Where(l => l.StructureId == id).OrderByDescending(l => l.Id).Take(5).ToList();
        if (sales.Count > 0)
        {
            keeper.AddChild(Heading("Sold"));
            foreach (var line in sales) keeper.AddChild(Note(line.Text));
        }
    }

    // Keeps the bag grid alive across rebuilds; everything else is made fresh.
    void Clear(Container c)
    {
        foreach (var child in c.GetChildren())
        {
            c.RemoveChild(child);
            if (child != carried) child.QueueFree();
        }
    }

    static string Name(ushort item, uint count) =>
        Items.IsKnown(item) ? Items.Def((Item)item).Count(count).ToLowerInvariant() : "things";

    static string Grade(byte quality) => quality > Items.RawQuality ? $" (quality {quality})" : "";

    static Label Heading(string text)
    {
        var l = Hud.Text(text, 16);
        l.Modulate = new Color(1, 1, 1, 0.85f);
        return l;
    }

    static Label Note(string text)
    {
        var l = Hud.Text(text, 14);
        l.Modulate = new Color(1, 1, 1, 0.7f);
        l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return l;
    }

    static Button Small(string text, Action pressed)
    {
        var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
        b.Pressed += pressed;
        return b;
    }
}
