using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LifeGame;

// Everything about other people: proximity chat, land claims and whether you can
// fight where you stand. Main hands it the connection and, every frame, where the
// player is; it keeps its own subscriptions so the rest of the client is untouched.
//
// Keys (KEYMAP.md): Enter to talk, K at a marker to open it. Markers are laid out
// from House building's B menu like any other structure.
public partial class SocialLayer : Node
{
    DbConnection? conn;
    Node world = null!;
    Terrain terrain = null!;
    Hud hud = null!;
    Func<uint, Node3D?> avatarOf = _ => null;

    readonly ChatPanel chat = new();
    readonly ClaimPanel panel = new();
    readonly Label landLabel = new();
    Label landChip = null!;
    float chipAlpha = 1f;
    readonly Dictionary<ulong, ClaimMarker> markers = new();
    readonly HashSet<ulong> heard = new();

    SubscriptionHandle? claimSub;
    Vector3 myPos;
    uint myId;
    float landTimer;

    // True while the chat box or the claim panel's name box has focus, so Main can stop walking.
    public bool Typing => chat.Typing || panel.Typing || town.Typing;

    public static long NowMicros => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;

    public void Attach(DbConnection connection, Node worldRoot, Terrain ground, Hud overlay, Func<uint, Node3D?> avatars)
    {
        conn = connection;
        world = worldRoot;
        terrain = ground;
        hud = overlay;
        avatarOf = avatars;
        panel.Conn = connection;
        panel.MyId = () => myId;

        conn.Db.MyChat.OnInsert += (_, m) => OnChat(m);
        conn.Db.Claim.OnInsert += (_, c) => OnClaim(c);
        conn.Db.Claim.OnUpdate += (_, _, c) => OnClaim(c);
        conn.Db.Claim.OnDelete += (_, c) => OnClaimGone(c);
        conn.Db.ClaimMember.OnInsert += (_, _) => RefreshClaims();
        conn.Db.ClaimMember.OnDelete += (_, _) => RefreshClaims();
        conn.Db.ClaimName.OnInsert += (_, _) => panel.MarkDirty();
        conn.Db.ClaimName.OnUpdate += (_, _, _) => panel.MarkDirty();
        conn.Db.ClaimName.OnDelete += (_, _) => panel.MarkDirty();
        conn.Db.MyClaimStashLog.OnInsert += (_, _) => panel.MarkDirty();
        conn.Db.MyClaimStashLog.OnUpdate += (_, _, _) => panel.MarkDirty();
        conn.Db.MyClaimStashLog.OnDelete += (_, _) => panel.MarkDirty();
        conn.Db.MyClaimStash.OnInsert += (_, _) => panel.MarkDirty();
        conn.Db.MyClaimStash.OnUpdate += (_, _, _) => panel.MarkDirty();
        conn.Db.MyClaimStash.OnDelete += (_, _) => panel.MarkDirty();
        conn.Db.MyInventory.OnInsert += (_, _) => panel.MarkDirty();
        conn.Db.MyInventory.OnUpdate += (_, _, _) => panel.MarkDirty();
        conn.Db.MyInventory.OnDelete += (_, _) => panel.MarkDirty();
        AttachFighting();
        AttachTrading();
        AttachTown(connection);

        // G at a marker you may tend refreshes it (KEYMAP: Use); K still opens the panel.
        Use.Register(new UseHandler
        {
            Id = "claims.tend",
            Priority = 5,
            Find = at =>
            {
                Claim? best = null;
                float bestD = Social.MarkerReach;
                foreach (var c in conn.Db.Claim.Iter())
                {
                    float d = new Vector2(c.X - at.X, c.Z - at.Z).Length();
                    if (d <= bestD && IsOwnerOrMember(c)) { best = c; bestD = d; }
                }
                return best is { } claim
                    ? new UseOffer("Tend the marker", bestD, () => conn.Reducers.TendClaim(claim.Id))
                    : null;
            },
        });
    }

    public override void _Ready()
    {
        // Chat (Enter) and the claim view (K) are bound in ui/Controls.cs.

        AddChild(chat);
        AddChild(panel);
        chat.Sent += Say;

        var layer = new CanvasLayer { Layer = 2 };
        // Boxless like the rest of the HUD (D-045).
        Hud.Soft(landLabel, 15, 0.9f);
        landChip = landLabel;
        landChip.HorizontalAlignment = HorizontalAlignment.Right;
        landChip.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        landChip.GrowHorizontal = Control.GrowDirection.Begin;
        landChip.Position = new Vector2(-20, 18);
        layer.AddChild(landChip);
        AddChild(layer);
        ReadyFighting();
        ReadyPlaces();
        ReadyTown();
    }

    static void Bind(string action, params Key[] keys)
    {
        if (InputMap.HasAction(action)) return;
        InputMap.AddAction(action);
        foreach (var key in keys) InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    // Called by Main every frame with the local player's position.
    public void Track(Vector3 position, uint playerId)
    {
        myPos = position;
        myId = playerId;
        if (conn is not { IsActive: true } || myId == 0) return;

        if (claimSub == null)
            claimSub = conn.SubscriptionBuilder()
                .OnError((_, e) => ServerCheck.Failed(e.Message, hud.Note))
                .Subscribe(new[] { "SELECT * FROM claim", "SELECT * FROM claim_member", "SELECT * FROM my_claim_stash", "SELECT * FROM player_health", "SELECT * FROM strike_event", "SELECT * FROM my_trade", "SELECT * FROM claim_name", "SELECT * FROM my_claim_stash_log", "SELECT * FROM my_trade_items", "SELECT * FROM trade_ended", "SELECT * FROM my_chat", "SELECT * FROM town", "SELECT * FROM town_signer", "SELECT * FROM town_office", "SELECT * FROM town_vote", "SELECT * FROM my_town_invites", "SELECT * FROM town_merge", "SELECT * FROM town_merge_vote", "SELECT * FROM town_ban", "SELECT * FROM town_log", "SELECT * FROM my_group", "SELECT * FROM my_group_invites" });
    }

    public Func<bool> HideChip = () => false;

    public override void _Process(double delta)
    {
        foreach (var marker in markers.Values) marker.Near(myPos);
        // Step aside while a full panel (journal, claim panel, menu) or the death card is up.
        chipAlpha = Mathf.MoveToward(chipAlpha, Panels.Open() || HideChip() ? 0f : 1f, (float)delta * 6f);
        landChip.Modulate = new Color(1, 1, 1, chipAlpha);
        ProcessFighting((float)delta);
        landTimer -= (float)delta;
        if (landTimer <= 0f)
        {
            landTimer = 0.25f;
            UpdateLandChip();
            if (conn != null && myId != 0) RefreshTradeCue();
            if (panel.IsOpen && (panel.Claim is not { } open || Flat(open).DistanceTo(Flat(myPos)) > Social.MarkerReach + 1f))
                panel.Close();
        }
    }

    // Opens the chat line already started, for the player menu's Whisper (ui-35).
    public void OpenChat(string prefill) => chat.Open(prefill);

    public override void _UnhandledInput(InputEvent e)
    {
        if (conn == null || Typing) return;
        if (e.IsActionPressed("chat"))
        {
            chat.Open();
            GetViewport().SetInputAsHandled();
        }
        else if (e.IsActionPressed("claim_open"))
        {
            if (panel.IsOpen || town.IsOpen) { panel.Close(); town.Close(); return; }
            var marker = NearestMarker();
            if (marker == null) hud.Note("Walk up to a claim marker first");
            else if (!IsOwnerOrMember(marker)) hud.Note($"This marker belongs to {marker.OwnerName}");
            else panel.Open(marker, IsMine(marker));
        }
        else if (e.IsActionPressed("ui_cancel") && (panel.IsOpen || town.IsOpen))
        {
            panel.Close();
            town.Close();
            GetViewport().SetInputAsHandled();
        }
    }

    // --- Chat --------------------------------------------------------------

    void Say(string text)
    {
        if (conn == null) return;
        if (text.StartsWith("/name "))
        {
            conn.Reducers.SetName(text[6..].Trim());
            hud.Note("Name changed");
            return;
        }
        if (text.StartsWith('/') && (TimeCommands.Try(conn, text, hud.Note) || AdminCommands.Try(conn, text, hud.Note) || StuckCommand.Try(text, hud.Note) || BugReportPanel.Try(conn, text, hud.Note))) return;
        conn.Reducers.SendChat(text);
    }

    void OnChat(ChatHeard m)
    {
        if (!heard.Add(m.Id)) return;
        long age = NowMicros - m.SentAt.MicrosecondsSinceUnixEpoch;
        if (age > Social.ChatKeepMicros) return;
        var voice = Social.VoiceOf(m.Text);
        float range = Social.RangeOf(voice);
        float away = new Vector2(m.X - myPos.X, m.Z - myPos.Z).Length();
        if (away > range) return;

        if (m.SenderId == 0)
        {
            // A line from the world (someone was struck down nearby): no speaker, no bubble.
            chat.Add("", m.Text, false);
            return;
        }
        bool mine = m.SenderId == myId;
        bool emote = Social.IsEmote(m.Text);
        // ui-38: "/g hi" came from your group, wherever they stand.
        if (Groups.IsGroupLine(m.Text))
        {
            chat.Add($"{(mine ? "You" : m.SenderName)} to the group", Groups.GroupBody(m.Text), mine);
            return;
        }
        string said = emote ? Social.EmoteBody(m.Text) : Social.VoiceBody(m.Text);
        float dim = mine ? 1f : Social.ChatDim(away, range);
        // claims-10: "Mira whispers" / "You shout" in the log.
        string who = mine && !emote ? "You" : m.SenderName;
        if (voice == Social.Voice.Whisper) who += mine ? " whisper" : " whispers";
        else if (voice == Social.Voice.Shout) who += mine ? " shout" : " shouts";
        chat.Add(who, said, mine, dim, emote);
        // Only fresh words get a bubble, so joining doesn't replay the last few lines at once.
        if (age < 3_000_000) Bubble(m, said, emote, voice);
    }

    // The words float above the speaker for a few seconds, then drift up and fade.
    const int BubbleStack = 3;
    // In the bubble's own pixels (Label3D offset), not metres.
    const float BubbleLineHeight = 44f, BubbleGap = 16f;
    static FontVariation? slanted;
    static FontVariation Slanted => slanted ??= new FontVariation { VariationTransform = new Transform2D(1f, 0f, 0.22f, 1f, 0f, 0f) };

    static void FadeOut(Label3D bubble)
    {
        bubble.RemoveMeta("bubble");
        var fade = bubble.CreateTween();
        fade.TweenProperty(bubble, "transparency", 1f, 0.8f).SetTrans(Tween.TransitionType.Sine);
        fade.TweenCallback(Callable.From(bubble.QueueFree));
    }

    // claims-6: a soft line above the speaker's head. If they say more, older lines
    // drift up and dim (up to three), and an action ("/me waves") is slanted.
    void Bubble(ChatHeard m, string said, bool emote, Social.Voice voice = Social.Voice.Say)
    {
        string text = Wrap(said, 34);
        int lineCount = text.Split('\n').Length;
        var bubble = new Label3D
        {
            Text = text,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            // A shout is bigger, a whisper smaller and fainter (claims-10).
            FontSize = voice == Social.Voice.Shout ? 40 : voice == Social.Voice.Whisper ? 24 : 30,
            PixelSize = 0.0075f,
            Modulate = new Color(Look.Cream, 0f),
            OutlineSize = 8,
            OutlineModulate = new Color(0.08f, 0.09f, 0.07f, 0.6f),
            NoDepthTest = true,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        if (emote) bubble.Font = Slanted;
        if (avatarOf(m.SenderId) is { } avatar)
        {
            // Older lines drift up by the new one's height and dim; the oldest beyond three go.
            var older = avatar.GetChildren().OfType<Label3D>().Where(l => l.HasMeta("bubble") && !l.IsQueuedForDeletion()).ToList();
            float rise = lineCount * BubbleLineHeight * bubble.FontSize / 30f + BubbleGap;
            for (int i = 0; i < older.Count; i++)
            {
                var old = older[i];
                // Every older line rises (the one leaving too, so nothing overlaps while it fades).
                // Lift from where it is headed, not where it is, in case it is still rising.
                float to = (old.HasMeta("lift_to") ? (float)old.GetMeta("lift_to") : old.Offset.Y) + rise;
                old.SetMeta("lift_to", to);
                var lift = old.CreateTween().SetParallel();
                lift.TweenProperty(old, "offset:y", to, 0.6f)
                    .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
                if (older.Count - i >= BubbleStack) FadeOut(old);
                else lift.TweenProperty(old, "transparency", 0.45f, 0.6f).SetTrans(Tween.TransitionType.Sine);
            }
            bubble.SetMeta("bubble", true);
            bubble.Position = new Vector3(0, 2.35f, 0);
            avatar.AddChild(bubble);
        }
        else
        {
            bubble.Position = new Vector3(m.X, terrain.HeightAt(m.X, m.Z) + 2.35f, m.Z);
            world.AddChild(bubble);
        }
        float hold = Mathf.Clamp(3f + m.Text.Length * 0.06f, 3f, 9f);
        var tween = bubble.CreateTween();
        tween.TweenProperty(bubble, "modulate:a", voice == Social.Voice.Whisper ? 0.7f : 1f, 0.35f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenInterval(hold);
        tween.Parallel().TweenProperty(bubble, "position:y", bubble.Position.Y + 0.25f, hold + 1.2f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(bubble, "modulate:a", 0f, 1.2f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenCallback(Callable.From(bubble.QueueFree));
    }

    static string Wrap(string text, int width)
    {
        var lines = new List<string>();
        var line = "";
        foreach (var word in text.Split(' '))
        {
            if (line.Length > 0 && line.Length + word.Length + 1 > width)
            {
                lines.Add(line);
                line = "";
            }
            line = line.Length == 0 ? word : $"{line} {word}";
        }
        if (line.Length > 0) lines.Add(line);
        return string.Join("\n", lines);
    }

    // --- Claims ------------------------------------------------------------

    void OnClaim(Claim c)
    {
        if (!markers.TryGetValue(c.Id, out var marker))
        {
            marker = new ClaimMarker(terrain);
            markers[c.Id] = marker;
            world.AddChild(marker);
        }
        marker.Set(c, IsOwnerOrMember(c));
        if (panel.Claim?.Id == c.Id) panel.Open(c, IsMine(c));
    }

    void OnClaimGone(Claim c)
    {
        if (markers.Remove(c.Id, out var marker)) marker.Fall();
        if (panel.Claim?.Id == c.Id) panel.Close();
        if (IsMine(c) && NearMe(c)) hud.Note("Your marker has fallen");
    }

    bool NearMe(Claim c) => Flat(c).DistanceTo(Flat(myPos)) < 60f;

    void RefreshClaims()
    {
        if (conn == null) return;
        foreach (var c in conn.Db.Claim.Iter())
            if (markers.TryGetValue(c.Id, out var marker)) marker.Set(c, IsOwnerOrMember(c));
        panel.MarkDirty();
    }

    Claim? ClaimAt(float x, float z)
    {
        if (conn == null) return null;
        long now = NowMicros;
        return conn.Db.Claim.Iter().FirstOrDefault(c => c.ExpiresAt.MicrosecondsSinceUnixEpoch > now && Social.InClaim(c.X, c.Z, x, z));
    }

    Claim? NearestMarker()
    {
        if (conn == null) return null;
        return conn.Db.Claim.Iter()
            .Where(c => Flat(c).DistanceTo(Flat(myPos)) <= Social.MarkerReach)
            .OrderBy(c => Flat(c).DistanceTo(Flat(myPos)))
            .FirstOrDefault();
    }

    bool IsMine(Claim c) => conn?.Db.Player.Identity.Find(c.Owner) is { } p ? p.PlayerId == myId : c.OwnerPlayerId == myId;

    // Whether you own or belong to the claim over this point. Main gives this to Building for door bars.
    public bool HasRightsAt(float x, float z) => ClaimAt(x, z) is { } c && HasRights(c);

    // Whether a standing claim covers this point; claim fences stop non-members even mid-jump (bugfix-25).
    public bool ClaimStandingAt(float x, float z) => ClaimAt(x, z) != null;

    // claims-12: the centre of a claim you belong to whose border is near this point (fences snap to it).
    public (float x, float z)? ClaimBorderNear(float x, float z)
    {
        if (conn == null) return null;
        long now = NowMicros;
        foreach (var c in conn.Db.Claim.Iter())
            if (c.ExpiresAt.MicrosecondsSinceUnixEpoch > now && Social.NearClaimBorder(c.X, c.Z, x, z) && HasRights(c))
                return (c.X, c.Z);
        return null;
    }

    // Builder rights, as the server decides them: the owner, its members, and anyone who signed
    // the same town roster as this claim (claims-19). Stashes and the marker's upkeep are the
    // owner's and members' alone (IsOwnerOrMember).
    bool HasRights(Claim c) => IsOwnerOrMember(c) || IsTownmate(c);

    bool IsOwnerOrMember(Claim c) =>
        IsMine(c) || (conn?.Db.ClaimMember.Iter().Any(m => m.ClaimId == c.Id && m.PlayerId == myId) ?? false);

    bool IsTownmate(Claim c)
    {
        if (conn == null) return false;
        var there = conn.Db.TownSigner.ClaimId.Find(c.Id);
        return there != null && conn.Db.TownSigner.Iter().Any(t => t.PlayerId == myId && t.TownId == there.TownId);
    }

    void UpdateLandChip()
    {
        if (conn == null || myId == 0)
        {
            landChip.Visible = false;
            return;
        }
        landChip.Visible = true;
        var here = ClaimAt(myPos.X, myPos.Z);
        ArriveAt(here);
        if (here == null)
        {
            float keepOut = Social.SpawnClearRadius + Social.ClaimRadius;
            bool spawn = myPos.X * myPos.X + myPos.Z * myPos.Z < keepOut * keepOut;
            landLabel.Text = spawn ? "Waking clearing   ·   no claims here" : "Wild land   ·   fighting allowed";
            return;
        }
        string whose = IsMine(here) ? "Your land" : HasRights(here) ? $"{here.OwnerName}'s land (you are welcome)" : $"{here.OwnerName}'s land";
        string fighting = here.AllowPvp ? "fighting allowed" : "peaceful";
        landLabel.Text = PlaceName(here.Id) is { } place ? $"{place}   ·   {whose}   ·   {fighting}" : $"{whose}   ·   {fighting}";
    }

    static Vector2 Flat(Claim c) => new(c.X, c.Z);
    static Vector2 Flat(Vector3 v) => new(v.X, v.Z);
}
