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
    const float Slot = 54f, IdleAlpha = 0.45f, IdleAfter = 6f;

    readonly HBoxContainer row = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly Label name = new() { HorizontalAlignment = HorizontalAlignment.Center };
    Dictionary<string, uint> last = new();
    float sinceChange = IdleAfter, nameTime, nameX, alpha = 1f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        row.AddThemeConstantOverride("separation", 14);
        row.Alignment = BoxContainer.AlignmentMode.Center;
        AddChild(row);
        Hud.Style(name, 14, 0.85f);
        name.Modulate = new Color(1, 1, 1, 0);
        AddChild(name);
    }

    public void SetItems(IReadOnlyList<InventoryEntry> items)
    {
        var now = items.ToDictionary(i => i.Label, i => i.Count);
        var gained = items.LastOrDefault(i => !last.TryGetValue(i.Label, out var before) || i.Count > before);
        bool changed = now.Count != last.Count || now.Any(kv => !last.TryGetValue(kv.Key, out var c) || c != kv.Value);
        last = now;

        foreach (var child in row.GetChildren()) child.QueueFree();
        int index = 0, gainedAt = -1;
        foreach (var item in items)
        {
            if (item == gained) gainedAt = index;
            row.AddChild(SlotFor(item));
            index++;
        }
        row.ResetSize();

        if (!changed) return;
        sinceChange = 0f;
        if (gained != null && gainedAt >= 0)
        {
            name.Text = gained.Label;
            name.ResetSize();
            nameX = gainedAt * (Slot + 14) + Slot / 2;
            nameTime = 2.6f;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        sinceChange += dt;
        alpha = Mathf.MoveToward(alpha, sinceChange < IdleAfter ? 1f : IdleAlpha, dt * (sinceChange < IdleAfter ? 4f : 0.6f));
        row.Modulate = new Color(1, 1, 1, alpha);
        nameTime -= dt;
        name.Modulate = new Color(1, 1, 1, Mathf.Clamp(nameTime, 0f, 1f));
        // Centred along the bottom edge as slots come and go; the name floats above.
        var size = row.GetCombinedMinimumSize();
        row.Position = new Vector2((Size.X - size.X) / 2, Size.Y - size.Y - 22);
        var nameSize = name.GetCombinedMinimumSize();
        name.Size = nameSize;
        name.Position = row.Position + new Vector2(nameX - nameSize.X / 2, -nameSize.Y - 6);
    }

    static Control SlotFor(InventoryEntry entry)
    {
        var slot = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(Slot, Slot) };
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
