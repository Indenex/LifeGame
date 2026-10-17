using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-32: your friends. Friends who marked you back show whether they are here and, when they
// are, how far and which way ("Mara   40 m north"); people you marked who haven't marked you yet
// wait under "Asked"; people who marked you show under "Marked you" with Add back. Type a name to
// add someone. "/friends" opens it (UX may bind a key and a player-menu entry). The server
// decides everything (Friends.cs); this reads my_friends and my_friend_spots.
public partial class FriendsPanel : CanvasLayer
{
    public DbConnection? Conn;
    public Func<uint> MyId = () => 0;
    public Func<Vector3> MyPos = () => Vector3.Zero;
    public Action<string> Note = _ => { };

    VBoxContainer list = null!;
    LineEdit nameBox = null!;
    HBoxContainer addRow = null!;
    string shown = "";
    float tick;

    public bool IsOpen => Visible;
    public bool Typing => nameBox.HasFocus();

    public override void _Ready()
    {
        Layer = 2;
        Visible = false;
        Panels.Centre(this, Close);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(380, 0) };
        box.AddThemeConstantOverride("separation", 8);
        box.AddChild(Ui.Title("Friends"));
        list = new VBoxContainer();
        list.AddThemeConstantOverride("separation", 3);
        box.AddChild(list);
        addRow = new HBoxContainer();
        addRow.AddThemeConstantOverride("separation", 8);
        nameBox = new LineEdit { PlaceholderText = "Add a friend by name", CustomMinimumSize = new Vector2(220, 0) };
        nameBox.TextSubmitted += _ => Add();
        addRow.AddChild(nameBox);
        addRow.AddChild(Small("Add", Add));
        box.AddChild(addRow);
        var hint = Hud.Text("Friends who add you back see when you're here and which way you are.    Esc closes", 13);
        hint.Modulate = new Color(1, 1, 1, 0.6f);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(hint);
        AddChild(Ui.Window(box));
    }

    public void Open()
    {
        Panels.Opening(this);
        Visible = true;
        shown = "";
    }

    public void Close()
    {
        Visible = false;
        nameBox.ReleaseFocus();
    }

    public void Toggle()
    {
        if (Visible) Close(); else Open();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible || !e.IsActionPressed("build_cancel")) return;
        Close();
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!Visible || Conn == null) return;
        tick -= (float)delta;
        if (tick > 0f) return;
        tick = 0.5f;
        var (friends, asked, markedMe) = Read();
        string key = string.Join("|", friends.Select(f => $"{f.Id}:{f.Here}:{f.Far}")) + "/" + string.Join(",", asked.Select(a => a.Id)) + "/" + string.Join(",", markedMe.Select(m => m.Id));
        if (key == shown) return;
        shown = key;
        Fill(friends, asked, markedMe);
    }

    // Friends both ways (with where they are), the ones you asked, and the ones who marked you.
    (System.Collections.Generic.List<(uint Id, string Name, bool Here, string? Far)>, System.Collections.Generic.List<(uint Id, string Name)>, System.Collections.Generic.List<(uint Id, string Name)>) Read()
    {
        var c = Conn!;
        uint me = MyId();
        var mine = c.Db.MyFriends.Iter().Where(f => f.OwnerId == me).ToList();
        var theirs = c.Db.MyFriends.Iter().Where(f => f.FriendId == me).ToList();
        var pos = MyPos();
        var friends = mine.Where(f => theirs.Any(t => t.OwnerId == f.FriendId))
            .Select(f =>
            {
                var spot = c.Db.MyFriendSpots.Iter().FirstOrDefault(s => s.PlayerId == f.FriendId);
                bool here = spot is { Online: true };
                return (f.FriendId, NameOf(f.FriendId, f.FriendName), here, here ? Friends.HowFar(spot!.X - pos.X, spot.Z - pos.Z) : (string?)null);
            })
            .OrderByDescending(f => f.here).ThenBy(f => f.Item2).ToList();
        var asked = mine.Where(f => !theirs.Any(t => t.OwnerId == f.FriendId)).Select(f => (f.FriendId, NameOf(f.FriendId, f.FriendName))).ToList();
        var markedMe = theirs.Where(t => !mine.Any(f => f.FriendId == t.OwnerId)).Select(t => (t.OwnerId, NameOf(t.OwnerId, t.OwnerName))).ToList();
        return (friends, asked, markedMe);
    }

    void Fill(System.Collections.Generic.List<(uint Id, string Name, bool Here, string? Far)> friends,
        System.Collections.Generic.List<(uint Id, string Name)> asked, System.Collections.Generic.List<(uint Id, string Name)> markedMe)
    {
        foreach (var child in list.GetChildren()) { list.RemoveChild(child); child.QueueFree(); }
        if (friends.Count == 0 && asked.Count == 0 && markedMe.Count == 0)
            list.AddChild(Faint("No friends yet. Add someone by name below, or from their menu."));
        foreach (var f in friends)
            list.AddChild(Row($"{f.Name}   {(f.Here ? f.Far ?? "here" : "(away)")}", f.Here ? 0.95f : 0.5f, ("Remove", () => Conn?.Reducers.RemoveFriend(f.Id))));
        if (asked.Count > 0)
        {
            list.AddChild(Heading("Asked"));
            foreach (var a in asked)
                list.AddChild(Row($"{a.Name}   (hasn't added you yet)", 0.6f, ("Remove", () => Conn?.Reducers.RemoveFriend(a.Id))));
        }
        if (markedMe.Count > 0)
        {
            list.AddChild(Heading("Added you"));
            foreach (var m in markedMe)
                list.AddChild(Row(m.Name, 0.85f, ("Add back", () => Conn?.Reducers.AddFriend(m.Id))));
        }
    }

    void Add()
    {
        string name = nameBox.Text.Trim();
        if (Conn == null || name.Length == 0) return;
        var found = Conn.Db.Player.Iter().Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (found.Count == 0) { Note($"Nobody called {name}"); return; }
        // Several characters can share a name: the one online first.
        var who = found.OrderByDescending(p => p.Online).First();
        Conn.Reducers.AddFriend(who.PlayerId);
        nameBox.Text = "";
        shown = "";
    }

    // The current name, so a rename shows; the stored one if they aren't known here.
    string NameOf(uint playerId, string stored) => Conn?.Db.Player.PlayerId.Find(playerId)?.Name ?? stored;

    Control Row(string text, float alpha, (string Label, Action Act) button)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        var label = Hud.Text(text, 15);
        label.Modulate = new Color(1, 1, 1, alpha);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);
        row.AddChild(Small(button.Label, () => { button.Act(); shown = ""; }));
        return row;
    }

    static Label Heading(string text)
    {
        var l = Hud.Text(text, 14);
        l.Modulate = new Color(1, 1, 1, 0.6f);
        return l;
    }

    static Label Faint(string text)
    {
        var l = Hud.Text(text, 14);
        l.Modulate = new Color(1, 1, 1, 0.6f);
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
