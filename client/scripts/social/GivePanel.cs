using System;
using System.Linq;
using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-7: what you carry, to pick something to hand to the player you face.
// Picking offers it; they take it with G. Closes when you offer, walk away or press Esc.
public partial class GivePanel : CanvasLayer
{
    public DbConnection? Conn;
    public uint ToId { get; private set; }
    public bool IsOpen => box.Visible;

    readonly PanelContainer box = new();
    readonly VBoxContainer content = new();
    string toName = "";
    bool dirty;

    public override void _Ready()
    {
        Layer = 3;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.11f, 0.09f, 0.72f),
            ContentMarginLeft = 22,
            ContentMarginRight = 22,
            ContentMarginTop = 16,
            ContentMarginBottom = 18,
        };
        style.SetCornerRadiusAll(16);
        box.AddThemeStyleboxOverride("panel", style);
        box.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        box.GrowHorizontal = Control.GrowDirection.Begin;
        box.GrowVertical = Control.GrowDirection.Both;
        box.Position = new Vector2(-28, 0);
        box.CustomMinimumSize = new Vector2(320, 0);
        content.AddThemeConstantOverride("separation", 8);
        box.AddChild(content);
        box.Visible = false;
        root.AddChild(box);
    }

    public void Open(uint toId, string name)
    {
        ToId = toId;
        toName = name;
        box.Visible = true;
        Rebuild();
    }

    public void Close()
    {
        box.Visible = false;
        ToId = 0;
    }

    // What you carry changed; rebuild on the next frame.
    public void MarkDirty() => dirty = true;

    public override void _Process(double delta)
    {
        if (dirty && IsOpen) Rebuild();
        dirty = false;
    }

    void Rebuild()
    {
        foreach (var child in content.GetChildren()) child.QueueFree();
        if (Conn == null) return;
        content.AddChild(ClaimPanel.Heading($"Give to {toName}", 20));
        var carried = Conn.Db.MyInventory.Iter().OrderBy(i => i.Item).ThenByDescending(i => i.Quality).ToList();
        if (carried.Count == 0)
        {
            content.AddChild(ClaimPanel.Note("You carry nothing to give."));
            return;
        }
        foreach (var stack in carried)
        {
            var item = stack;
            var row = ClaimPanel.Row($"{ClaimPanel.Name(item.Item, item.Quality)}  {item.Count}");
            row.AddChild(ClaimPanel.Small("Give one", () => Offer(item, 1)));
            if (item.Count > 1) row.AddChild(ClaimPanel.Small($"All {item.Count}", () => Offer(item, item.Count)));
            content.AddChild(row);
        }
        content.AddChild(ClaimPanel.Note($"{toName} takes it with G. Nothing leaves your hands until then."));
    }

    void Offer(InventoryItem item, uint count)
    {
        Conn?.Reducers.OfferGift(ToId, item.Item, item.Quality, count);
        Close();
    }
}
