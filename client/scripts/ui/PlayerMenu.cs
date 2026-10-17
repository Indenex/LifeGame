using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// What the menu knows about the player you clicked, read fresh each time it opens.
public sealed record PlayerFacts(
    uint Id, string Name, float Distance,
    string? Town, string[] Offices, (Glyph glyph, Color ink)? Mark,
    bool AskedYou, bool Trading, bool MayInvite, string? InviteWhyNot);

// Right-click a player (ui-35, Justin: "make it so right clicking a person opens a menu to
// invite them to group, trade, etc. just being near them and hitting E shouldn't do
// anything"). A quick right click (CameraDrag.Clicked) on someone within Reach opens a
// small menu beside the cursor: Trade, Whisper, Invite to town, Inspect, Invite to group.
// Items you can't use now are greyed, with the reason on hover. The server checks every
// action again (reach for trades, roster rights for invites). Esc or a click away closes it.
public partial class PlayerMenu : CanvasLayer
{
    // How far away a player can be and still be right-clicked.
    public const float Reach = 8f;

    public Func<Vector2, uint?> PlayerAt = _ => null;
    public Func<uint, PlayerFacts?> Facts = _ => null;
    public Action<uint> AskTrade = _ => { };
    public Action<bool> AnswerTrade = _ => { };
    public Action<uint> InviteToTown = _ => { };
    public Action<string> OpenChat = _ => { };
    // True while something else has the screen (a panel, placing, typing).
    public Func<bool> Blocked = () => false;

    public bool IsOpen => root.Visible;

    Control root = null!;
    PanelContainer frame = null!;
    VBoxContainer column = null!;
    uint target;
    bool inspecting;
    Vector2 at;

    public override void _Ready()
    {
        Layer = 4;
        // A clear sheet over the screen: a click anywhere off the menu closes it.
        root = new Control { MouseFilter = Control.MouseFilterEnum.Stop, Visible = false };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true }) Close();
        };
        AddChild(root);

        column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 2);
        frame = Ui.Window(column);
        frame.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        frame.GrowHorizontal = frame.GrowVertical = Control.GrowDirection.End;
        var style = (StyleBoxFlat)frame.GetThemeStylebox("panel");
        style.ContentMarginLeft = style.ContentMarginRight = 10;
        style.ContentMarginTop = style.ContentMarginBottom = 8;
        root.AddChild(frame);

        // --screenshot-playermenu opens it on a pretend Mara; =inspect shows her details,
        // =far has her out of reach so Trade and Whisper are greyed.
        foreach (var arg in OS.GetCmdlineUserArgs().Where(a => a.StartsWith("--screenshot-playermenu")))
        {
            string how = arg.Split('=').ElementAtOrDefault(1) ?? "";
            Facts = _ => new PlayerFacts(7, "Mara Thistledown", how == "far" ? 6.5f : 2.2f, "Fernhill", new[] { "Mayor" },
                Marks.Find(1) is { } g ? (g, new Color(0.36f, 0.45f, 0.62f)) : null,
                AskedYou: false, Trading: false, MayInvite: false, InviteWhyNot: "Mara is already on a town roster");
            GetTree().CreateTimer(1.5).Timeout += () =>
            {
                Open(7, GetViewport().GetVisibleRect().Size * new Vector2(0.55f, 0.4f));
                if (how == "inspect") { inspecting = true; Fill(); }
            };
        }
    }

    // A right click that didn't drag: open on whoever is under it, if anyone.
    public void Clicked(Vector2 point)
    {
        if (Blocked()) return;
        if (PlayerAt(point) is { } id) Open(id, point);
        else Close();
    }

    public void Open(uint id, Vector2 point)
    {
        target = id;
        at = point;
        inspecting = false;
        if (Facts(id) == null) { Close(); return; }
        root.Visible = true;
        Fill();
    }

    public void Close()
    {
        root.Visible = false;
        target = 0;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!IsOpen || !e.IsActionPressed("ui_cancel")) return;
        Close();
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!IsOpen) return;
        // Walking well away, the player leaving, or another panel opening closes it.
        if (Facts(target) is not { } f || f.Distance > Reach + 2f || Blocked()) Close();
    }

    void Fill()
    {
        foreach (var child in column.GetChildren()) { column.RemoveChild(child); child.QueueFree(); }
        if (Facts(target) is not { } f) { Close(); return; }

        var name = Hud.Text(f.Name, 16);
        name.Modulate = new Color(1, 1, 1, 0.95f);
        column.AddChild(name);
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });

        if (f.AskedYou)
            Item("Accept trade", null, () => AnswerTrade(true));
        Item("Trade",
            f.Trading ? "You're already trading"
            : f.Distance > Social.TradeReach ? $"Walk closer to trade (within {Social.TradeReach:0} m)"
            : null,
            () => AskTrade(f.Id));
        Item("Whisper",
            f.Distance > Social.WhisperRange ? $"Walk closer: a whisper carries {Social.WhisperRange:0} m" : null,
            () => OpenChat("/w "));
        if (f.MayInvite || f.InviteWhyNot != null)
            Item("Invite to town", f.MayInvite ? null : f.InviteWhyNot, () => InviteToTown(f.Id));
        Item(inspecting ? "Inspect  ▾" : "Inspect", null, () => { inspecting = !inspecting; Fill(); }, closes: false);
        if (inspecting) column.AddChild(Details(f));
        Item("Invite to group", "Coming soon", () => { });

        // Beside the cursor, kept on screen.
        frame.ResetSize();
        CallDeferred(nameof(Place));
    }

    void Place()
    {
        var screen = GetViewport().GetVisibleRect().Size;
        var size = frame.GetCombinedMinimumSize();
        frame.Position = new Vector2(
            Mathf.Clamp(at.X + 14, 8, screen.X - size.X - 8),
            Mathf.Clamp(at.Y - 10, 8, screen.Y - size.Y - 8));
    }

    void Item(string text, string? whyNot, Action act, bool closes = true)
    {
        var b = new Button
        {
            Text = text, Flat = true, FocusMode = Control.FocusModeEnum.None,
            Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(180, 30),
            Disabled = whyNot != null, TooltipText = whyNot ?? "",
            // A disabled button still shows its tooltip only if it takes the mouse.
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        b.AddThemeFontSizeOverride("font_size", 15);
        b.AddThemeColorOverride("font_color", Look.Cream);
        b.AddThemeColorOverride("font_hover_color", new Color(1f, 0.95f, 0.8f));
        b.AddThemeColorOverride("font_disabled_color", new Color(Look.Cream, 0.38f));
        var hover = new StyleBoxFlat { BgColor = new Color(Look.Cream, 0.08f) };
        hover.SetCornerRadiusAll(6);
        hover.ContentMarginLeft = hover.ContentMarginRight = 8;
        var plain = new StyleBoxEmpty { ContentMarginLeft = 8, ContentMarginRight = 8 };
        b.AddThemeStyleboxOverride("normal", plain);
        b.AddThemeStyleboxOverride("disabled", plain);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", hover);
        b.Pressed += () =>
        {
            act();
            if (closes) Close();
        };
        column.AddChild(b);
    }

    // Inspect: their town and offices, and their maker's mark.
    static Control Details(PlayerFacts f)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        var pad = new MarginContainer();
        pad.AddThemeConstantOverride("margin_left", 8);
        pad.AddThemeConstantOverride("margin_bottom", 6);
        pad.AddChild(row);

        var mark = new MarkView { CustomMinimumSize = new Vector2(40, 40), Width = 2.4f };
        if (f.Mark is { } m) mark.Show(m.glyph, m.ink);
        row.AddChild(mark);

        var lines = new VBoxContainer();
        lines.AddThemeConstantOverride("separation", 0);
        string town = f.Town is { } t ? $"Of {t}" : "No town yet";
        string offices = f.Offices.Length > 0 ? string.Join(", ", f.Offices) : "";
        foreach (var (text, alpha) in new[] { (town, 0.85f), (offices, 0.85f), (f.Mark == null ? "No maker's mark yet" : "Their maker's mark", 0.55f) })
        {
            if (text.Length == 0) continue;
            var l = Hud.Text(text, 13);
            l.Modulate = new Color(1, 1, 1, alpha);
            lines.AddChild(l);
        }
        row.AddChild(lines);
        return pad;
    }
}

// The live game's answers for the menu, from the tables the client already has.
public static class PlayerMenuFacts
{
    public static PlayerFacts? Read(DbConnection conn, uint me, uint them, Vector3 myPos, uint incomingTradeFrom)
    {
        if (them == 0 || them == me || conn.Db.Player.PlayerId.Find(them) is not { Online: true } p) return null;
        if (conn.Db.PlayerPos.PlayerId.Find(them) is not { } pos) return null;
        float d = new Vector2(pos.X - myPos.X, pos.Z - myPos.Z).Length();

        var theirSigner = conn.Db.TownSigner.Iter().FirstOrDefault(s => s.PlayerId == them);
        var mySigner = conn.Db.TownSigner.Iter().FirstOrDefault(s => s.PlayerId == me);
        string? town = theirSigner is { } ts ? conn.Db.Town.Id.Find(ts.TownId)?.Name : null;
        var offices = theirSigner is { } ts2
            ? conn.Db.TownOffice.ByTown.Filter(ts2.TownId).Where(o => o.HolderId == them)
                .Select(o => Towns.Get(o.Office)?.Name).OfType<string>().ToArray()
            : Array.Empty<string>();

        (Glyph, Color)? mark = conn.Db.PlayerMark.PlayerId.Find(them) is { } m && Marks.Find(m.Glyph) is { } g
            ? (g, MarkColour.ToColor(m.Color)) : null;

        // Invite to town: only offered once you've signed a roster and they aren't on yours.
        bool mayInvite = false;
        string? whyNot = null;
        if (mySigner is { } mine && theirSigner?.TownId != mine.TownId)
        {
            // Whether they have a claim near enough is the server's to say (InviteToTown).
            if (theirSigner != null) whyNot = $"{p.Name} is already on a town roster";
            else mayInvite = true;
        }

        return new PlayerFacts(them, p.Name, d, town, offices, mark,
            AskedYou: incomingTradeFrom == them, Trading: conn.Db.MyTrade.Count > 0,
            MayInvite: mayInvite, InviteWhyNot: whyNot);
    }

    // The other player under a screen point: their body or name tag, nearest first.
    public static uint? At(Vector2 point, Camera3D camera, Vector2 viewSize, Vector2 screenSize,
        Vector3 myPos, IEnumerable<(uint id, Node3D body)> others)
    {
        var scale = new Vector2(screenSize.X / viewSize.X, screenSize.Y / viewSize.Y);
        Vector2 Screen(Vector3 p) => camera.UnprojectPosition(p) * scale;
        uint? best = null;
        float bestDepth = float.MaxValue;
        foreach (var (id, body) in others)
        {
            if (!body.Visible) continue;
            var feet = body.GlobalPosition;
            if (new Vector2(feet.X - myPos.X, feet.Z - myPos.Z).Length() > PlayerMenu.Reach) continue;
            var head = feet + new Vector3(0, 2.3f, 0);
            if (camera.IsPositionBehind(feet) || camera.IsPositionBehind(head)) continue;
            Vector2 top = Screen(head), bottom = Screen(feet);
            // About a body's width, and never less than a comfortable target.
            float half = Mathf.Max(18f, (bottom.Y - top.Y) * 0.2f);
            var box = new Rect2(Mathf.Min(top.X, bottom.X) - half, top.Y, Mathf.Abs(top.X - bottom.X) + 2 * half, bottom.Y - top.Y);
            if (!box.HasPoint(point)) continue;
            float depth = camera.GlobalPosition.DistanceTo(feet);
            if (depth < bestDepth) { best = id; bestDepth = depth; }
        }
        return best;
    }
}
