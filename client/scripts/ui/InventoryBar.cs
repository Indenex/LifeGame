using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// One stack in your hands: what it is, how many, and what to call it.
public record InventoryEntry(Item Item, string Label, uint Count);

// What you carry, as a quiet row of round slots with a picture of each item and a
// small count (D-045). The row dims when nothing changes and brightens when you
// gain or use something; a newly gained item's name floats up and fades.
public partial class InventoryBar : Control
{
    // Dragging a made thing out of the bag sets it down in the world (ui-16, D-187).
    // Placing says which items can be dragged and is told when a drag leaves a slot.
    public static Func<Item, bool> CanDrag = _ => false;
    public static event Action<Item>? DraggedOut;
    // Where the row is on screen, so dropping a thing back on it puts it away.
    public static Rect2 RowRect { get; private set; }

    const float Slot = 54f, IdleAlpha = 0.45f, IdleAfter = 6f;
    // Kept clear on each side: the needs rings bottom right, the hint line bottom left.
    const float SideRoom = 430f, MinScale = 0.8f, Gap = 14f;

    readonly HFlowContainer row = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly Label name = new() { HorizontalAlignment = HorizontalAlignment.Center };
    Dictionary<string, uint> last = new();
    float sinceChange = IdleAfter, nameTime, alpha = 1f, cover;
    int nameAt = -1;

    // How far above the bottom edge the row reaches; notes sit above it.
    public float Top => Size.Y - row.Position.Y;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        row.AddThemeConstantOverride("h_separation", (int)Gap);
        row.AddThemeConstantOverride("v_separation", 10);
        row.Alignment = FlowContainer.AlignmentMode.Center;
        AddChild(row);
        Hud.Style(name, 14, 0.85f);
        name.Modulate = new Color(1, 1, 1, 0);
        AddChild(name);
    }

    public void SetItems(IReadOnlyList<InventoryEntry> items)
    {
        var now = items.GroupBy(i => i.Label).ToDictionary(g => g.Key, g => (uint)g.Sum(i => i.Count));
        var gained = items.LastOrDefault(i => !last.TryGetValue(i.Label, out var before) || i.Count > before);
        bool changed = now.Count != last.Count || now.Any(kv => !last.TryGetValue(kv.Key, out var c) || c != kv.Value);
        last = now;

        foreach (var child in row.GetChildren())
        {
            row.RemoveChild(child);
            child.QueueFree();
        }
        int index = 0, gainedAt = -1;
        foreach (var item in items)
        {
            if (item == gained) gainedAt = index;
            row.AddChild(SlotFor(item));
            index++;
        }

        if (!changed) return;
        sinceChange = 0f;
        if (gained != null && gainedAt >= 0)
        {
            name.Text = gained.Label;
            name.ResetSize();
            nameAt = gainedAt;
            nameTime = 2.6f;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        sinceChange += dt;
        alpha = Mathf.MoveToward(alpha, sinceChange < IdleAfter ? 1f : IdleAlpha, dt * (sinceChange < IdleAfter ? 4f : 0.6f));
        // Steps aside while a panel covers the world.
        cover = Mathf.MoveToward(cover, Panels.Open() ? 1f : 0f, dt * 6f);
        row.Modulate = new Color(1, 1, 1, alpha * (1f - cover));
        RowRect = row.GetGlobalRect();
        nameTime -= dt;
        name.Modulate = new Color(1, 1, 1, Mathf.Clamp(nameTime, 0f, 1f) * (1f - cover));
        // Centred along the bottom edge as slots come and go; the name floats above.
        // A long row first shrinks a little, then wraps onto a second row, so it
        // never runs into the corners.
        int count = row.GetChildCount();
        float natural = Mathf.Max(count * Slot + (count - 1) * Gap, Slot);
        float room = Mathf.Max(Size.X - 2 * SideRoom, 3 * Slot);
        float scale = Mathf.Clamp(room / natural, MinScale, 1f);
        float width = Mathf.Min(natural, room / scale);
        row.Size = new Vector2(width, 0);
        var size = new Vector2(width, row.GetCombinedMinimumSize().Y);
        row.Scale = new Vector2(scale, scale);
        row.Position = new Vector2((Size.X - width * scale) / 2, Size.Y - size.Y * scale - 22);
        var nameSize = name.GetCombinedMinimumSize();
        name.Size = nameSize;
        var over = nameAt >= 0 && nameAt < count ? row.GetChild<Control>(nameAt).Position : Vector2.Zero;
        name.Position = row.Position + new Vector2((over.X + Slot / 2) * scale - nameSize.X / 2, over.Y * scale - nameSize.Y - 6);
    }

    static Control SlotFor(InventoryEntry entry)
    {
        var slot = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(Slot, Slot) };
        if (CanDrag(entry.Item))
        {
            // Press on the slot and drag a little way out: the thing comes out as a ghost.
            slot.MouseFilter = MouseFilterEnum.Stop;
            slot.MouseDefaultCursorShape = CursorShape.Drag;
            Vector2? down = null;
            slot.GuiInput += e =>
            {
                if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left } b) down = b.Pressed ? b.GlobalPosition : null;
                else if (e is InputEventMouseMotion m && down is { } from && m.GlobalPosition.DistanceTo(from) > 10f)
                {
                    down = null;
                    DraggedOut?.Invoke(entry.Item);
                }
            };
        }
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.09f, 0.10f, 0.07f, 0.62f),
            BorderColor = new Color(Look.Cream, 0.28f),
            ShadowColor = new Color(0, 0, 0, 0.25f),
            ShadowSize = 6,
            ShadowOffset = new Vector2(0, 2),
        };
        style.SetBorderWidthAll(1);
        // Just under half the slot: a radius of exactly half leaves a seam down the middle.
        style.SetCornerRadiusAll((int)(Slot / 2) - 2);
        slot.AddThemeStyleboxOverride("panel", style);

        var stack = new Control { MouseFilter = MouseFilterEnum.Ignore };
        slot.AddChild(stack);
        var picture = new TextureRect
        {
            Texture = ItemIcon.For(entry.Item),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        picture.SetAnchorsPreset(LayoutPreset.FullRect);
        picture.OffsetLeft = picture.OffsetTop = 2;
        picture.OffsetRight = picture.OffsetBottom = -2;
        stack.AddChild(picture);

        if (entry.Count > 1)
        {
            var count = Hud.Text(entry.Count.ToString(), 13);
            count.SetAnchorsPreset(LayoutPreset.FullRect);
            count.HorizontalAlignment = HorizontalAlignment.Right;
            count.VerticalAlignment = VerticalAlignment.Bottom;
            count.OffsetRight = -3;
            count.OffsetBottom = 1;
            stack.AddChild(count);
        }
        return slot;
    }
}
