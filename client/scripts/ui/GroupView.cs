using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Your group on screen (ui-38, the shell): a quiet list of members down the left edge, no box
// (D-045), with "Leave group" under it; "Mara asks you to join her group    E joins" above the
// E prompt while an invitation waits, and a soft "wants you in their group" over the
// inviter's head, like the trade cue. The server decides everything (Groups.cs); this reads
// my_group and my_group_invites.
public partial class GroupView : CanvasLayer
{
    public Func<uint, Node3D?> AvatarOf = _ => null;
    public Func<uint> MyId = () => 0;

    DbConnection? conn;
    VBoxContainer list = null!;
    Label asked = null!;
    Label3D? cue;
    uint cueFor;
    string shown = "";

    public void Attach(DbConnection connection)
    {
        conn = connection;
        Use.Register(new UseHandler { Id = "ui.group_invite", Priority = 99, Find = FindUse });
    }

    // The invitation waiting for you, if any (the newest).
    public GroupInvite? Incoming => conn == null ? null
        : conn.Db.MyGroupInvites.Iter().Where(i => i.InviteeId == MyId()).OrderByDescending(i => i.At.MicrosecondsSinceUnixEpoch).Cast<GroupInvite?>().FirstOrDefault();

    public bool InMyGroup(uint playerId) => conn != null && conn.Db.MyGroup.Iter().Any(m => m.PlayerId == playerId);
    public bool InGroup => conn != null && conn.Db.MyGroup.Count > 0;
    public bool Full => conn != null && conn.Db.MyGroup.Count >= Groups.MaxMembers;

    public void Answer(GroupInvite invite, bool yes) => conn?.Reducers.AnswerGroup(invite.Id, yes);

    UseOffer? FindUse(Vector3 at) => Incoming is { } invite
        ? new UseOffer($"Join {invite.FromName}'s group", 0f, () => Answer(invite, true), Target: at)
        : null;

    public override void _Ready()
    {
        Layer = 2;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        list = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        list.AddThemeConstantOverride("separation", 2);
        list.SetAnchorsPreset(Control.LayoutPreset.CenterLeft);
        list.GrowVertical = Control.GrowDirection.Both;
        list.Position = new Vector2(18, -60);
        root.AddChild(list);

        asked = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        Hud.Soft(asked, 16, 0.92f);
        asked.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        asked.GrowHorizontal = Control.GrowDirection.Both;
        asked.GrowVertical = Control.GrowDirection.Begin;
        asked.Position = new Vector2(0, -276);
        root.AddChild(asked);
    }

    public override void _Process(double delta)
    {
        if (conn == null) return;
        var invite = Incoming;
        asked.Text = invite is { } i ? $"{i.FromName} asks you to join their group    E joins" : "";
        Cue(invite?.FromId ?? 0);

        // Rebuild the list only when it changes.
        var members = conn.Db.MyGroup.Iter().OrderBy(m => m.JoinedAt.MicrosecondsSinceUnixEpoch).ToList();
        string key = string.Join("|", members.Select(m => $"{m.PlayerId}:{Online(m.PlayerId)}"));
        if (key == shown) return;
        shown = key;
        foreach (var child in list.GetChildren()) { list.RemoveChild(child); child.QueueFree(); }
        if (members.Count == 0) return;

        var title = Hud.Text("Your group", 14);
        title.Modulate = new Color(1, 1, 1, 0.6f);
        list.AddChild(title);
        foreach (var m in members)
        {
            bool here = Online(m.PlayerId);
            var name = Hud.Text((m.PlayerId == MyId() ? "You" : m.Name) + (here ? "" : "  (away)"), 15);
            Hud.Soft(name, 15, here ? 0.9f : 0.45f);
            list.AddChild(name);
        }
        var leave = ControlsPage.TextButton("Leave group", () => conn?.Reducers.LeaveGroup());
        leave.AddThemeFontSizeOverride("font_size", 13);
        leave.Modulate = new Color(1, 1, 1, 0.6f);
        leave.MouseFilter = Control.MouseFilterEnum.Stop;
        list.AddChild(leave);
    }

    bool Online(uint playerId) => conn?.Db.Player.PlayerId.Find(playerId) is { Online: true };

    // "wants you in their group" over the inviter's head while you are asked; only you see it.
    void Cue(uint from)
    {
        if (from == cueFor && (from == 0 || cue != null)) return;
        if (cue is { } old)
        {
            var fade = old.CreateTween();
            fade.TweenProperty(old, "modulate:a", 0f, 0.6f).SetTrans(Tween.TransitionType.Sine);
            fade.TweenCallback(Callable.From(old.QueueFree));
            cue = null;
        }
        cueFor = from;
        if (from == 0 || AvatarOf(from) is not { } avatar) return;
        cue = new Label3D
        {
            Text = "wants you in their group",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 26,
            PixelSize = 0.0075f,
            Modulate = new Color(Look.Cream, 0f),
            OutlineSize = 8,
            OutlineModulate = new Color(0.08f, 0.09f, 0.07f, 0.6f),
            NoDepthTest = true,
            VerticalAlignment = VerticalAlignment.Bottom,
            Font = new FontVariation { VariationTransform = new Transform2D(1f, 0f, 0.22f, 1f, 0f, 0f) },
            Position = new Vector3(0, 2.35f, 0),
        };
        avatar.AddChild(cue);
        cue.CreateTween().TweenProperty(cue, "modulate:a", 0.9f, 0.35f).SetTrans(Tween.TransitionType.Sine);
    }
}
