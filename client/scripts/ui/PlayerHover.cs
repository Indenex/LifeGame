using System.Collections.Generic;
using Godot;

namespace LifeGame;

// A small note beside the cursor when it rests on another player (ui-72, PM under "Home and
// friends": "Show a friend's name and status when you hover them"). Their name, then how they
// stand with you (in your group, in your town, asking you to trade or to join their group), then
// their town and offices, and a soft "Right-click for more". It waits a moment before showing and
// fades in and out, so sweeping the camera past people doesn't flicker notes. PlayerMenu owns it
// and hides it while the menu is open or anything else has the screen.
public partial class PlayerHover : Control
{
    const float Delay = 0.3f;

    public PlayerMenu Menu = null!;

    readonly PanelContainer note = new() { MouseFilter = MouseFilterEnum.Ignore };
    readonly VBoxContainer lines = new() { MouseFilter = MouseFilterEnum.Ignore };
    uint shownFor, resting;
    float restTime, alpha;
    string shownText = "";

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
        var style = new StyleBoxFlat
        {
            BgColor = WarmLook.On ? WarmLook.Paper : Ui.Frame,
            BorderColor = WarmLook.On ? new Color(WarmLook.Ink, 0.28f) : new Color(Look.Cream, 0.2f),
            ShadowColor = new Color(0.15f, 0.08f, 0.05f, 0.25f),
            ShadowSize = 6,
            ShadowOffset = new Vector2(0, 2),
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 7, ContentMarginBottom = 8,
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(14);
        note.AddThemeStyleboxOverride("panel", style);
        lines.AddThemeConstantOverride("separation", 1);
        note.AddChild(lines);
        note.Modulate = new Color(1, 1, 1, 0);
        AddChild(note);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        var mouse = GetViewport().GetMousePosition();
        uint? under = Menu.IsOpen || Menu.Blocked() || GetViewport().GuiGetHoveredControl() != null ? null : Menu.PlayerAt(mouse);
        if (under != resting)
        {
            resting = under ?? 0;
            restTime = 0f;
        }
        restTime += dt;
        var facts = resting != 0 && restTime >= Delay ? Menu.Facts(resting) : null;
        if (facts != null) Fill(facts);
        alpha = Mathf.MoveToward(alpha, facts != null ? 1f : 0f, dt * (facts != null ? 7f : 9f));
        note.Modulate = new Color(1, 1, 1, alpha);
        if (alpha <= 0f) { shownFor = 0; return; }

        // Beside the cursor, kept on screen.
        var screen = GetViewport().GetVisibleRect().Size;
        var size = note.GetCombinedMinimumSize();
        note.Size = size;
        note.Position = new Vector2(Mathf.Clamp(mouse.X + 18, 8, screen.X - size.X - 8), Mathf.Clamp(mouse.Y + 14, 8, screen.Y - size.Y - 8));
    }

    void Fill(PlayerFacts f)
    {
        var rows = Rows(f);
        string text = string.Join("\n", rows.ConvertAll(r => r.text));
        if (f.Id == shownFor && text == shownText) return;
        shownFor = f.Id;
        shownText = text;
        foreach (var child in lines.GetChildren()) { lines.RemoveChild(child); child.QueueFree(); }
        foreach (var (line, size, colour) in rows)
        {
            var l = Hud.Text(line, size);
            l.MouseFilter = MouseFilterEnum.Ignore;
            if (WarmLook.On)
            {
                // Brown ink on the paper note, with no shadow behind the words.
                l.AddThemeFontOverride("font", Ui.Font(size > 14 ? 800 : 650));
                l.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0));
                l.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0));
                l.AddThemeColorOverride("font_color", colour ?? WarmLook.Ink);
            }
            else if (colour is { } c) l.AddThemeColorOverride("font_color", c);
            lines.AddChild(l);
        }
    }

    // Name first, then how they stand with you, then who they are, then the hint.
    static List<(string text, int size, Color? colour)> Rows(PlayerFacts f)
    {
        var good = WarmLook.On ? WarmLook.Sage.Darkened(0.35f) : WarmLook.Sage;
        var ask = WarmLook.On ? WarmLook.Coral.Darkened(0.2f) : WarmLook.Coral;
        var soft = WarmLook.On ? new Color(WarmLook.Ink, 0.6f) : new Color(Look.Cream, 0.6f);
        var rows = new List<(string, int, Color?)> { (f.Name, 16, null) };
        if (f.AskedYou) rows.Add(("Asking you to trade", 13, ask));
        if (f.GroupAskedYou) rows.Add(("Asking you to join their group", 13, ask));
        if (f.InYourGroup) rows.Add(("In your group", 13, good));
        else if (f.InYourTown) rows.Add(("In your town", 13, good));
        string who = f.Town is { } t ? $"Of {t}" : "No town yet";
        if (f.Offices.Length > 0) who += ", " + string.Join(", ", f.Offices);
        rows.Add((who, 13, soft));
        rows.Add(("Right-click for more", 12, soft));
        return rows;
    }
}
