using System.Collections.Generic;
using System.Linq;
using Godot;

namespace LifeGame;

// A quiet overlay: what you carry, short notes and connection news that fade out
// on their own. There is no controls line (D-038); keys are taught by the G
// prompt, panel hints and first-time hints (ui/Hints.cs).
public partial class Hud : CanvasLayer
{
    readonly Label status = new();
    readonly HotbarBar inventory = new();
    // The ten slots along the bottom (ui-19); Main attaches the server link.
    public HotbarBar Hotbar => inventory;
    readonly Label toast = new();
    readonly NoteStack earlier = new();
    float toastTime, statusTime;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);
        ItemIcon.Attach(this);

        // Connection news is one quiet line at the top that fades (D-036, D-045).
        Soft(status, 14, 0.8f);
        status.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        status.GrowHorizontal = Control.GrowDirection.Both;
        status.HorizontalAlignment = HorizontalAlignment.Center;
        status.Position = new Vector2(0, 18);
        root.AddChild(status);

        root.AddChild(inventory);

        // Notes sit just above what you carry, small and slanted, with no box.
        Soft(toast, 16, 0.92f);
        toast.AddThemeFontOverride("font", new FontVariation
        {
            BaseFont = ThemeDB.FallbackFont,
            VariationTransform = new Transform2D(new Vector2(1, 0.2f), new Vector2(0, 1), Vector2.Zero),
        });
        toast.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        toast.GrowHorizontal = Control.GrowDirection.Both;
        toast.GrowVertical = Control.GrowDirection.Begin;
        toast.HorizontalAlignment = HorizontalAlignment.Center;
        toast.Position = new Vector2(0, -NoteLane);
        // Notes draw above panels (layer 2), so "You put on the wool tunic" never hides
        // behind the window that caused it.
        var notes = new CanvasLayer { Layer = 3 };
        AddChild(notes);
        var noteRoot = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        noteRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        notes.AddChild(noteRoot);
        noteRoot.AddChild(toast);
        // Notes still showing when a newer one arrives stack above it (ui/NoteStack.cs, UX).
        earlier.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        earlier.GrowHorizontal = Control.GrowDirection.Both;
        earlier.GrowVertical = Control.GrowDirection.Begin;
        noteRoot.AddChild(earlier);

        // --screenshot-hud fills the HUD with sample things, for screenshots without a server.
        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshot-hud") >= 0)
            GetTree().CreateTimer(2).Timeout += () =>
            {
                SetItems(new[]
                {
                    new InventoryEntry(LifeGame.Shared.Item.Stick, "Sticks", 4),
                    new InventoryEntry(LifeGame.Shared.Item.Stone, "Stones", 2),
                    new InventoryEntry(LifeGame.Shared.Item.Flake, "Fine stone flake", 1),
                });
                SetItems(new[]
                {
                    new InventoryEntry(LifeGame.Shared.Item.Stick, "Sticks", 4),
                    new InventoryEntry(LifeGame.Shared.Item.Stone, "Stones", 2),
                    new InventoryEntry(LifeGame.Shared.Item.Fiber, "Plant fiber", 6),
                    new InventoryEntry(LifeGame.Shared.Item.Flake, "Fine stone flake", 1),
                    new InventoryEntry(LifeGame.Shared.Item.Berries, "Berries", 5),
                });
                // --screenshot-many fills a long row, to check it fits between the corners.
                if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshot-many") >= 0)
                    SetItems(Enumerable.Range(1, 12).Select(i => (LifeGame.Shared.Item)i)
                        .Select(i => new InventoryEntry(i, i.ToString(), 2)).ToList());
                // --screenshot-clay shows each stage of clay side by side, to check they tell apart.
                if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--screenshot-clay") >= 0)
                    SetItems(new[]
                    {
                        new InventoryEntry(LifeGame.Shared.Item.Stick, "Sticks", 12),
                        new InventoryEntry(LifeGame.Shared.Item.Clay, "Clay", 5),
                        new InventoryEntry(LifeGame.Shared.Item.CleanClay, "Clean clay", 3),
                        new InventoryEntry(LifeGame.Shared.Item.TemperedClay, "Tempered clay", 2),
                        new InventoryEntry(LifeGame.Shared.Item.Daub, "Daub", 2),
                        new InventoryEntry(LifeGame.Shared.Item.Grit, "Stone grit", 1),
                    });
                GetTree().CreateTimer(3.5).Timeout += () => Note("The fire takes the stick and brightens");
                // A second note soon after, to show the earlier one moving up (ui-28).
                GetTree().CreateTimer(4.3).Timeout += () => Note("Made: Twisted cordage");
            };
    }

    public void SetStatus(string text)
    {
        if (status.Text == text) return;
        status.Text = text;
        statusTime = 6f;
    }

    // A status line that stays until replaced, for something the player must act on.
    public void Warn(string text)
    {
        status.Text = text;
        statusTime = float.PositiveInfinity;
    }

    public void Note(string text)
    {
        if (toastTime > 0f && toast.Text != text) earlier.Push(toast, toastTime);
        toast.Text = text;
        toastTime = 3f;
    }

    public void SetItems(IReadOnlyList<InventoryEntry> items) => inventory.SetBag(items);

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        toastTime -= dt;
        // While a panel covers the middle, a note sits near the top, under the status
        // line; otherwise it sits clear of the inventory, even when the row wraps.
        float noteY = Panels.Open() ? TopNote
            : toast.GetParentAreaSize().Y - NoteLane - Mathf.Max(0f, inventory.Top - 76f);
        toast.Position = new Vector2(toast.Position.X, noteY);
        earlier.Position = new Vector2(earlier.Position.X, noteY - earlier.Size.Y - 2);
        toast.Modulate = new Color(1, 1, 1, Mathf.Clamp(toastTime * 1.5f, 0f, 1f));
        statusTime -= dt;
        status.Modulate = new Color(1, 1, 1, Mathf.Clamp(statusTime * 0.7f, 0f, 1f));
    }

    // The lanes along the bottom, from the edge up: the inventory row, the name of a
    // gained item just above its slot, notes (toasts), then boxes you work in
    // (minigames) and the G prompt. Each sits clear of the one below, and all stay in
    // the lower third so the player in the middle is never covered.
    public const float NoteLane = 150f, WorkLane = 156f;
    // Where a note sits from the top edge while a full panel is open.
    const float TopNote = 52f;

    // Puts a box (a minigame card) in the work lane: centred, its foot WorkLane above
    // the bottom edge, growing upward. A box too tall for the lower third shrinks
    // (down to 70%) so its top stays below the player.
    public static void PlaceLow(Control box)
    {
        box.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        box.GrowHorizontal = Control.GrowDirection.Both;
        box.GrowVertical = Control.GrowDirection.Begin;
        box.OffsetLeft = box.OffsetRight = 0;
        box.OffsetTop = box.OffsetBottom = -WorkLane;
        void Fit()
        {
            float room = box.GetParentAreaSize().Y * 0.33f - WorkLane;
            float scale = box.Size.Y > 0 ? Mathf.Clamp(room / box.Size.Y, 0.7f, 1f) : 1f;
            box.PivotOffset = new Vector2(box.Size.X / 2, box.Size.Y);
            box.Scale = new Vector2(scale, scale);
        }
        box.ItemRectChanged += Fit;
        box.Ready += Fit;
    }

    // Text with a wide, faint shadow instead of a box behind it.
    internal static void Soft(Label label, int size, float alpha)
    {
        Style(label, size, alpha);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.45f));
        label.AddThemeConstantOverride("shadow_outline_size", 7);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
    }

    internal static Label Text(string text, int size)
    {
        var label = new Label { Text = text };
        Style(label, size, 1f);
        return label;
    }

    internal static void Style(Label label, int size, float alpha)
    {
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color(Look.Cream, alpha));
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.35f));
        label.AddThemeConstantOverride("shadow_offset_y", 1);
    }

    internal static PanelContainer Panel(Control content, Vector2 position)
    {
        var panel = new PanelContainer { Position = position, MouseFilter = Control.MouseFilterEnum.Ignore };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.11f, 0.09f, 0.42f),
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 7,
            ContentMarginBottom = 7,
        };
        style.SetCornerRadiusAll(12);
        panel.AddThemeStyleboxOverride("panel", style);
        panel.AddChild(content);
        return panel;
    }
}
