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

    SubscriptionHandle? claimSub, chatSub;
    (int, int) chatChunk = (int.MinValue, int.MinValue);
    Vector3 myPos;
    uint myId;
    float landTimer;

    // True while the chat box has focus, so Main can stop walking.
    public bool Typing => chat.Typing;

    public static long NowMicros => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;

    public void Attach(DbConnection connection, Node worldRoot, Terrain ground, Hud overlay, Func<uint, Node3D?> avatars)
    {
        conn = connection;
        world = worldRoot;
        terrain = ground;
        hud = overlay;
        avatarOf = avatars;
        panel.Conn = connection;

        conn.Db.ChatMessage.OnInsert += (_, m) => OnChat(m);
        conn.Db.Claim.OnInsert += (_, c) => OnClaim(c);
        conn.Db.Claim.OnUpdate += (_, _, c) => OnClaim(c);
        conn.Db.Claim.OnDelete += (_, c) => OnClaimGone(c);
        conn.Db.ClaimMember.OnInsert += (_, _) => RefreshClaims();
        conn.Db.ClaimMember.OnDelete += (_, _) => RefreshClaims();
        conn.Db.MyClaimStash.OnInsert += (_, _) => panel.MarkDirty();
        conn.Db.MyClaimStash.OnUpdate += (_, _, _) => panel.MarkDirty();
        conn.Db.MyClaimStash.OnDelete += (_, _) => panel.MarkDirty();
        conn.Db.MyInventory.OnInsert += (_, _) => panel.MarkDirty();
        conn.Db.MyInventory.OnUpdate += (_, _, _) => panel.MarkDirty();
        conn.Db.MyInventory.OnDelete += (_, _) => panel.MarkDirty();
        AttachFighting();

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
                    if (d <= bestD && HasRights(c)) { best = c; bestD = d; }
                }
                return best is { } claim
                    ? new UseOffer("Tend the marker", bestD, () => conn.Reducers.TendClaim(claim.Id))
                    : null;
            },
        });
    }

    public override void _Ready()
    {
        Bind("chat", Key.Enter, Key.KpEnter);
        Bind("claim_open", Key.K);

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
                .OnError((_, e) => hud.Note($"Subscription error: {e.Message}"))
                .Subscribe(new[] { "SELECT * FROM claim", "SELECT * FROM claim_member", "SELECT * FROM my_claim_stash", "SELECT * FROM player_health", "SELECT * FROM strike_event" });

        // Chat carries a little further than a chunk, so listen two chunks out.
        var chunk = (World.ChunkOf(myPos.X), World.ChunkOf(myPos.Z));
        if (chunk != chatChunk)
        {
            chatChunk = chunk;
            int r = 2;
            var old = chatSub;
            chatSub = conn.SubscriptionBuilder()
                .OnApplied(_ => old?.Unsubscribe())
                .Subscribe(new[] { $"SELECT * FROM chat_message WHERE chunk_x >= {chunk.Item1 - r} AND chunk_x <= {chunk.Item1 + r} AND chunk_z >= {chunk.Item2 - r} AND chunk_z <= {chunk.Item2 + r}" });
        }
    }

    public override void _Process(double delta)
    {
        foreach (var marker in markers.Values) marker.Near(myPos);
        // Step aside while a full panel (journal, claim panel, menu) is open.
        chipAlpha = Mathf.MoveToward(chipAlpha, Panels.Open() ? 0f : 1f, (float)delta * 6f);
        landChip.Modulate = new Color(1, 1, 1, chipAlpha);
        ProcessFighting((float)delta);
        landTimer -= (float)delta;
        if (landTimer <= 0f)
        {
            landTimer = 0.25f;
            UpdateLandChip();
            if (panel.IsOpen && (panel.Claim is not { } open || Flat(open).DistanceTo(Flat(myPos)) > Social.MarkerReach + 1f))
                panel.Close();
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (conn == null || chat.Typing) return;
        if (e.IsActionPressed("chat"))
        {
            chat.Open();
            GetViewport().SetInputAsHandled();
        }
        else if (e.IsActionPressed("claim_open"))
        {
            if (panel.IsOpen) { panel.Close(); return; }
            var marker = NearestMarker();
            if (marker == null) hud.Note("Walk up to a claim marker first");
            else if (!HasRights(marker)) hud.Note($"This marker belongs to {marker.OwnerName}");
            else panel.Open(marker, IsMine(marker));
        }
        else if (e.IsActionPressed("ui_cancel") && panel.IsOpen)
        {
            panel.Close();
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
        conn.Reducers.SendChat(text);
    }

    void OnChat(ChatMessage m)
    {
        if (!heard.Add(m.Id)) return;
        long age = NowMicros - m.SentAt.MicrosecondsSinceUnixEpoch;
        if (age > Social.ChatKeepMicros) return;
        if (new Vector2(m.X - myPos.X, m.Z - myPos.Z).Length() > Social.ChatRange) return;

        if (m.SenderId == 0)
        {
            // A line from the world (someone was struck down nearby): no speaker, no bubble.
            chat.Add("", m.Text, false);
            return;
        }
        chat.Add(m.SenderId == myId ? "You" : m.SenderName, m.Text, m.SenderId == myId);
        if (age < 8_000_000) Bubble(m);
    }

    // The words float above the speaker for a few seconds, then drift up and fade.
    void Bubble(ChatMessage m)
    {
        var bubble = new Label3D
        {
            Text = Wrap(m.Text, 34),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 30,
            PixelSize = 0.0075f,
            Modulate = new Color(Look.Cream, 0f),
            OutlineSize = 8,
            OutlineModulate = new Color(0.08f, 0.09f, 0.07f, 0.6f),
            NoDepthTest = true,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        if (avatarOf(m.SenderId) is { } avatar)
        {
            // Only the newest line stays above a head.
            foreach (var child in avatar.GetChildren())
                if (child is Label3D old && old.HasMeta("bubble")) old.QueueFree();
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
        tween.TweenProperty(bubble, "modulate:a", 1f, 0.35f).SetTrans(Tween.TransitionType.Sine);
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
        marker.Set(c, HasRights(c));
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
            if (markers.TryGetValue(c.Id, out var marker)) marker.Set(c, HasRights(c));
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

    bool HasRights(Claim c) =>
        IsMine(c) || (conn?.Db.ClaimMember.Iter().Any(m => m.ClaimId == c.Id && m.PlayerId == myId) ?? false);

    void UpdateLandChip()
    {
        if (conn == null || myId == 0)
        {
            landChip.Visible = false;
            return;
        }
        landChip.Visible = true;
        var here = ClaimAt(myPos.X, myPos.Z);
        if (here == null)
        {
            float keepOut = Social.SpawnClearRadius + Social.ClaimRadius;
            bool spawn = myPos.X * myPos.X + myPos.Z * myPos.Z < keepOut * keepOut;
            landLabel.Text = spawn ? "Waking clearing   ·   no claims here" : "Wild land   ·   fighting allowed";
            return;
        }
        string whose = IsMine(here) ? "Your land" : HasRights(here) ? $"{here.OwnerName}'s land (you are welcome)" : $"{here.OwnerName}'s land";
        landLabel.Text = $"{whose}   ·   {(here.AllowPvp ? "fighting allowed" : "peaceful")}";
    }

    static Vector2 Flat(Claim c) => new(c.X, c.Z);
    static Vector2 Flat(Vector3 v) => new(v.X, v.Z);
}
