using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// The panel you get at a claim marker you belong to: tend it, see how long it will
// stand, share the stash, and (for the owner) choose who belongs and whether
// fighting is allowed.
public partial class ClaimPanel : CanvasLayer
{
    public DbConnection? Conn;
    public Func<uint> MyId = () => 0;
    public Claim? Claim { get; private set; }
    public bool IsOpen => box.Visible;
    // True while the owner is typing a name, so Main stops walking.
    public bool Typing => nameBox.HasFocus() || placeBox.HasFocus();

    readonly PanelContainer box = new();
    readonly VBoxContainer content = new();
    readonly Label standing = new();
    readonly HBoxContainer addRow = new();
    readonly LineEdit nameBox = new();
    // claims-9: the owner names the place.
    readonly LineEdit placeBox = new();
    readonly HBoxContainer placeRow = new();
    // ui-18: the stash and your bag as round-slot grids, dragged between (a whole stack
    // per drag). Kept across rebuilds like the rows above, so a drag isn't cut short.
    readonly HBoxContainer stashGrids = new();
    BagGrid stashGrid = null!, stashBag = null!;
    bool owner, dirty, confirmAbandon;
    float clock;

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
        box.CustomMinimumSize = new Vector2(340, 0);
        content.AddThemeConstantOverride("separation", 8);
        box.AddChild(content);
        box.Visible = false;
        root.AddChild(box);
        Hud.Style(standing, 14, 0.75f);
        ReadyNameBox();
        ReadyPlaceBox();
    }

    // claims-4: the owner adds someone by name, near or far, online or not.
    void ReadyNameBox()
    {
        nameBox.PlaceholderText = "Add someone by name";
        nameBox.MaxLength = 24;
        nameBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        nameBox.AddThemeFontSizeOverride("font_size", 14);
        nameBox.AddThemeColorOverride("font_color", Look.Cream);
        nameBox.AddThemeColorOverride("font_placeholder_color", new Color(Look.Cream, 0.45f));
        nameBox.AddThemeColorOverride("caret_color", Look.Cream);
        var field = new StyleBoxFlat
        {
            BgColor = new Color(Look.Cream, 0.08f),
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        };
        field.SetCornerRadiusAll(10);
        nameBox.AddThemeStyleboxOverride("normal", field);
        nameBox.AddThemeStyleboxOverride("focus", field);
        nameBox.TextSubmitted += _ => AddByName();
        nameBox.GuiInput += e =>
        {
            // Esc while typing just stops typing; a second Esc closes the panel.
            if (e.IsActionPressed("ui_cancel"))
            {
                nameBox.ReleaseFocus();
                GetViewport().SetInputAsHandled();
            }
        };
        addRow.AddThemeConstantOverride("separation", 6);
        addRow.AddChild(nameBox);
        addRow.AddChild(Small("Add", AddByName));
    }

    void ReadyPlaceBox()
    {
        placeBox.PlaceholderText = "Name this place";
        placeBox.MaxLength = Social.ClaimNameMax;
        placeBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        placeBox.AddThemeFontSizeOverride("font_size", 14);
        placeBox.AddThemeColorOverride("font_color", Look.Cream);
        placeBox.AddThemeColorOverride("font_placeholder_color", new Color(Look.Cream, 0.45f));
        placeBox.AddThemeColorOverride("caret_color", Look.Cream);
        foreach (var state in new[] { "normal", "focus" })
            placeBox.AddThemeStyleboxOverride(state, nameBox.GetThemeStylebox(state));
        placeBox.TextSubmitted += _ => SaveName();
        placeBox.GuiInput += e =>
        {
            if (e.IsActionPressed("ui_cancel"))
            {
                placeBox.ReleaseFocus();
                GetViewport().SetInputAsHandled();
            }
        };
        placeRow.AddThemeConstantOverride("separation", 6);
        placeRow.AddChild(placeBox);
        placeRow.AddChild(Small("Name", SaveName));
    }

    void SaveName()
    {
        if (Conn == null) return;
        Conn.Reducers.NameClaim(placeBox.Text);
        placeBox.ReleaseFocus();
    }

    void AddByName()
    {
        string name = nameBox.Text.Trim();
        if (name.Length == 0 || Conn == null) return;
        Conn.Reducers.AddClaimMemberByName(name);
        nameBox.Text = "";
        nameBox.ReleaseFocus();
    }

    public void Open(Claim claim, bool isOwner)
    {
        if (Claim?.Id != claim.Id) confirmAbandon = false;
        Claim = claim;
        owner = isOwner;
        box.Visible = true;
        box.Modulate = new Color(1, 1, 1, box.Modulate.A);
        dirty = true;
    }

    public void Close()
    {
        box.Visible = false;
        Claim = null;
        confirmAbandon = false;
        nameBox.Text = "";
        nameBox.ReleaseFocus();
        placeBox.ReleaseFocus();
    }

    public void MarkDirty() => dirty = true;

    public override void _Process(double delta)
    {
        if (!box.Visible) return;
        if (dirty) Rebuild();
        clock -= (float)delta;
        if (clock <= 0f && Claim != null)
        {
            clock = 1f;
            standing.Text = StandsFor(Claim);
        }
    }

    // Built once; each rebuild refreshes the slots in place (ui-18).
    void StashGrids()
    {
        if (stashGrid == null)
        {
            stashGrids.AddThemeConstantOverride("separation", 24);
            stashGrid = new BagGrid
            {
                Kind = "stash", Width = 5, Empty = "Empty",
                Source = () => Conn != null && Claim is { } c
                    ? Conn.Db.MyClaimStash.Iter().Where(s => s.ClaimId == c.Id).Select(s => new BagStack((Item)s.Item, s.Quality, s.Count))
                    : Enumerable.Empty<BagStack>(),
                Accepts = d => d["from"].AsString() == "bag",
                Receive = d => { if (Claim is { } c) Conn?.Reducers.StashItem(c.Id, (ushort)d["item"].AsInt32(), (byte)d["quality"].AsInt32(), (uint)d["count"].AsInt64()); },
            };
            stashBag = new BagGrid
            {
                Kind = "bag", Width = 5, Empty = "Nothing",
                Source = () => Conn != null
                    ? Conn.Db.MyInventory.Iter().Where(i => i.Count > 0 && !i.OnBelt).Select(i => new BagStack((Item)i.Item, i.Quality, i.Count))
                    : Enumerable.Empty<BagStack>(),
                Accepts = d => d["from"].AsString() == "stash",
                Receive = d => { if (Claim is { } c) Conn?.Reducers.TakeFromStash(c.Id, (ushort)d["item"].AsInt32(), (byte)d["quality"].AsInt32(), (uint)d["count"].AsInt64()); },
            };
            foreach (var (title, grid) in new[] { ("In the stash", stashGrid), ("You carry", stashBag) })
            {
                var column = new VBoxContainer();
                column.AddChild(Note(title));
                column.AddChild(grid);
                stashGrids.AddChild(column);
            }
        }
        stashGrid.Refresh();
        stashBag.Refresh();
    }

    void Rebuild()
    {
        dirty = false;
        if (Conn == null || Claim is not { } claim) return;
        bool typing = nameBox.HasFocus();
        foreach (var child in content.GetChildren())
        {
            content.RemoveChild(child);
            if (child != standing && child != addRow && child != placeRow && child != stashGrids) child.QueueFree();
        }

        string? place = Conn.Db.ClaimName.ClaimId.Find(claim.Id)?.Name;
        content.AddChild(Heading(place ?? (owner ? "Your claim" : $"{claim.OwnerName}'s claim"), 20));
        if (place != null) content.AddChild(Note(owner ? "Your claim" : $"{claim.OwnerName}'s claim"));
        standing.Text = StandsFor(claim);
        content.AddChild(standing);
        content.AddChild(Button("Tend the marker", () => Conn.Reducers.TendClaim(claim.Id)));
        content.AddChild(Note("Working on the land keeps it tended too."));

        if (owner)
        {
            bool naming = placeBox.HasFocus();
            if (!naming) placeBox.Text = place ?? "";
            content.AddChild(placeRow);
            if (naming) placeBox.CallDeferred(Control.MethodName.GrabFocus);
            content.AddChild(Button(claim.AllowPvp ? "Fighting: allowed   (turn off)" : "Fighting: not allowed   (allow)",
                () => Conn.Reducers.SetClaimPvp(claim.Id, !claim.AllowPvp)));
        }
        else
        {
            content.AddChild(Note(claim.AllowPvp ? "The owner allows fighting here." : "The owner keeps this land peaceful."));
        }

        // Members, and people close enough to invite.
        content.AddChild(Heading("People", 16));
        var members = Conn.Db.ClaimMember.Iter().Where(m => m.ClaimId == claim.Id).ToList();
        if (members.Count == 0) content.AddChild(Note("Nobody else belongs here yet."));
        foreach (var m in members)
        {
            var row = Row(m.Name);
            if (owner) row.AddChild(Small("Remove", () => Conn.Reducers.RemoveClaimMember(m.PlayerId)));
            content.AddChild(row);
        }
        if (owner)
        {
            var nearby = Conn.Db.Player.Iter()
                .Where(p => p.Online && p.Identity != claim.Owner && members.All(m => m.PlayerId != p.PlayerId))
                .Where(p => Conn.Db.PlayerPos.PlayerId.Find(p.PlayerId) is { } pos && new Vector2(pos.X - claim.X, pos.Z - claim.Z).Length() < Social.ChatRange)
                .ToList();
            foreach (var p in nearby)
            {
                var row = Row(p.Name);
                row.AddChild(Small("Invite", () => Conn.Reducers.AddClaimMember(p.PlayerId)));
                content.AddChild(row);
            }
            content.AddChild(addRow);
            if (typing) nameBox.CallDeferred(Control.MethodName.GrabFocus);
        }

        // The shared stash at the foot of the marker.
        content.AddChild(Heading("Stash", 16));
        StashGrids();
        content.AddChild(stashGrids);
        var stash = Conn.Db.MyClaimStash.Iter().Where(s => s.ClaimId == claim.Id).OrderBy(s => s.Item).ThenByDescending(s => s.Quality).ToList();
        if (stash.Count == 0) content.AddChild(Note("Empty. Anything you leave here is safe from outsiders."));
        foreach (var s in stash)
        {
            var row = Row($"{Name(s.Item, s.Quality, s.Wear)}  {s.Count}");
            row.AddChild(Small("Take", () => Conn.Reducers.TakeFromStash(claim.Id, s.Item, s.Quality, 1)));
            content.AddChild(row);
        }
        // claims-11: the last few comings and goings, newest first.
        long now = SocialLayer.NowMicros;
        var recent = Conn.Db.MyClaimStashLog.Iter().Where(e => e.ClaimId == claim.Id)
            .OrderByDescending(e => e.At.MicrosecondsSinceUnixEpoch).Take(5).ToList();
        foreach (var e in recent)
        {
            if (!Items.IsKnown(e.Item)) continue;
            string who = e.PlayerId == MyId() ? "You" : e.PlayerName;
            content.AddChild(Note($"{Social.StashLine(who, Items.Def((Item)e.Item), e.Count)}   ·   {Ago(now - e.At.MicrosecondsSinceUnixEpoch)}"));
        }

        var carried = Conn.Db.MyInventory.Iter().OrderBy(i => i.Item).ThenByDescending(i => i.Quality).ToList();
        if (carried.Count > 0)
        {
            var row = new HFlowContainer();
            row.AddThemeConstantOverride("h_separation", 6);
            row.AddThemeConstantOverride("v_separation", 6);
            foreach (var item in carried)
                row.AddChild(Small($"Leave {Name(item.Item, item.Quality, item.Wear).ToLowerInvariant()}", () => Conn.Reducers.StashItem(claim.Id, item.Item, item.Quality, 1)));
            content.AddChild(row);
        }

        content.AddChild(new Control { CustomMinimumSize = new Vector2(0, 6) });
        if (owner)
            content.AddChild(Button(confirmAbandon ? "Really take the marker down?" : "Take the marker down", () =>
            {
                if (!confirmAbandon) { confirmAbandon = true; dirty = true; return; }
                Conn.Reducers.AbandonClaim(claim.Id);
                Close();
            }));
        else
            content.AddChild(Button("Leave this claim", () =>
            {
                Conn.Reducers.LeaveClaim(claim.Id);
                Close();
            }));
        content.AddChild(Note("K or Esc to close"));
    }

    // "Hand axe (fine)" for made things, just "Stick" for raw materials.
    static string Name(ushort item, byte quality, uint wear = 0)
    {
        if (!Items.IsKnown(item)) return "Something";
        var def = Items.Def((Item)item);
        string worn = ItemLook.Condition(item, quality, wear);
        return def.HasQuality ? $"{def.Name} ({Processes.Grade(quality).ToLowerInvariant()}{(worn == "" ? "" : ", " + worn)})" : def.Name;
    }

    // "just now", "4 min ago", "2 h ago", "3 days ago".
    static string Ago(long micros)
    {
        long sec = micros / 1_000_000;
        if (sec < 60) return "just now";
        if (sec < 3600) return $"{sec / 60} min ago";
        if (sec < 86400) return $"{sec / 3600} h ago";
        long d = sec / 86400;
        return d == 1 ? "a day ago" : $"{d} days ago";
    }

    // claims-5: "Tended 2 days ago · stands 1 more day".
    static string StandsFor(Claim claim) =>
        Social.TendedLine(claim.TendedAt.MicrosecondsSinceUnixEpoch, claim.ExpiresAt.MicrosecondsSinceUnixEpoch, SocialLayer.NowMicros);

    static Label Heading(string text, int size)
    {
        var label = new Label { Text = text };
        Hud.Style(label, size, 1f);
        return label;
    }

    static Label Note(string text)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(300, 0) };
        Hud.Style(label, 14, 0.65f);
        return label;
    }

    static HBoxContainer Row(string text)
    {
        var row = new HBoxContainer();
        var label = new Label { Text = text, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        Hud.Style(label, 15, 0.9f);
        row.AddChild(label);
        return row;
    }

    static Button Button(string text, Action onPress) => MakeButton(text, onPress, 15, 10);

    static Button Small(string text, Action onPress) => MakeButton(text, onPress, 13, 6);

    static Button MakeButton(string text, Action onPress, int size, int pad)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", size);
        button.AddThemeColorOverride("font_color", Look.Cream);
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.95f, 0.8f));
        foreach (var (state, alpha) in new[] { ("normal", 0.10f), ("hover", 0.22f), ("pressed", 0.32f) })
        {
            var style = new StyleBoxFlat
            {
                BgColor = new Color(Look.Cream, alpha),
                ContentMarginLeft = pad + 4,
                ContentMarginRight = pad + 4,
                ContentMarginTop = pad / 2 + 2,
                ContentMarginBottom = pad / 2 + 2,
            };
            style.SetCornerRadiusAll(10);
            button.AddThemeStyleboxOverride(state, style);
        }
        button.Pressed += onPress;
        return button;
    }
}
