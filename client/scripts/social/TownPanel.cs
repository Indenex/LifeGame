using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-19: the Town panel. Neighbouring claim owners sign one roster and become a town
// (Justin: "Add neighbors together via a roster everyone signs"). Map first: the town's claims
// drawn from above (TownMap), and beside it the roster, the ten offices and their open ballots,
// invitations to sign, merge proposals and a few actions. Every change is a reducer call; the
// server decides (D-182) and a refusal comes back as a note through Main's
// OnUnhandledReducerError. The client only hides buttons you plainly cannot use.
// Opened from the claim panel's "Town" button; Esc closes it (SocialLayer).
public partial class TownPanel : CanvasLayer
{
    public DbConnection? Conn;
    public Func<uint> MyId = () => 0;
    public bool IsOpen => window?.Visible == true;
    // True while a name or announcement is being typed, so Main stops walking.
    public bool Typing => nameBox.HasFocus() || sayBox.HasFocus() || logBox.HasFocus();

    // Window sizes, readable at 1280x720.
    const float MapSize = 380f, SideWidth = 430f, SideHeight = 470f;
    // Parchment notes (invitations, merge proposals) in the map's ink-on-paper look.
    static readonly Color NotePaper = new(0.87f, 0.80f, 0.64f, 0.95f);
    static readonly Color NoteInk = new(0.25f, 0.17f, 0.11f);
    static readonly Color Chosen = new(0.91f, 0.72f, 0.28f);

    PanelContainer? window;
    readonly Label title = new();
    readonly TownMap map = new();
    readonly Label mapLegend = new();
    readonly VBoxContainer card = new();
    readonly VBoxContainer side = new();
    // Kept across rebuilds, like ClaimPanel's name boxes, so typing isn't cut short.
    readonly LineEdit nameBox = new();
    readonly HBoxContainer nameRow = new();
    readonly Button nameButton = new();
    readonly LineEdit sayBox = new();
    readonly HBoxContainer sayRow = new();
    readonly LineEdit logBox = new();
    readonly HBoxContainer logRow = new();

    bool dirty, confirmLeave;
    byte? votingFor;     // an office chosen to vote for; the next signer clicked gets the vote
    ulong selected;      // the claim whose card shows under the map

    // --- Window: UX's shared look (Ui.Window) and the one-centre-window rule (Panels, D-283). ---

    public override void _Ready()
    {
        Layer = 3;
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        window = Ui.Window(Body());
        window.MouseFilter = Control.MouseFilterEnum.Stop;
        window.Visible = false;
        Panels.Centre(this, Close);
        root.AddChild(window);
        ReadyBoxes();
        map.Picked += Pick;
    }

    public void Open()
    {
        if (window == null) return;
        Panels.Opening(this);
        window.Visible = true;
        confirmLeave = false;
        dirty = true;
    }

    public void Close()
    {
        if (window == null) return;
        window.Visible = false;
        votingFor = null;
        selected = 0;
        nameBox.ReleaseFocus();
        sayBox.ReleaseFocus();
    }

    public void MarkDirty() => dirty = true;

    public override void _Process(double delta)
    {
        if (IsOpen && dirty) Rebuild();
    }

    Control Body()
    {
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 10);
        Hud.Style(title, Ui.TitleSize, 1f);
        body.AddChild(title);

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 20);
        body.AddChild(columns);

        var left = new VBoxContainer { CustomMinimumSize = new Vector2(MapSize, 0) };
        left.AddThemeConstantOverride("separation", 6);
        map.CustomMinimumSize = new Vector2(MapSize, MapSize);
        left.AddChild(map);
        Hud.Style(mapLegend, 12, 0.6f);
        mapLegend.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        mapLegend.CustomMinimumSize = new Vector2(MapSize, 0);
        mapLegend.Text = "Filled: signed the roster.   Dashed: a neighbour on no roster.   Blue: another town.";
        left.AddChild(mapLegend);
        card.AddThemeConstantOverride("separation", 6);
        left.AddChild(card);
        columns.AddChild(left);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(SideWidth, SideHeight),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        side.AddThemeConstantOverride("separation", 8);
        side.CustomMinimumSize = new Vector2(SideWidth - 16, 0);
        scroll.AddChild(side);
        columns.AddChild(scroll);

        body.AddChild(Note("Esc to close"));
        return body;
    }

    void ReadyBoxes()
    {
        StyleField(nameBox, "Name the town", Social.ClaimNameMax);
        nameBox.TextSubmitted += _ => SubmitName();
        nameRow.AddThemeConstantOverride("separation", 6);
        nameRow.AddChild(nameBox);
        StyleButton(nameButton, 13, 6);
        nameButton.Pressed += SubmitName;
        nameRow.AddChild(nameButton);

        StyleField(sayBox, "A word to the whole town", Towns.AnnounceMax);
        sayBox.TextSubmitted += _ => Announce();
        sayRow.AddThemeConstantOverride("separation", 6);
        sayRow.AddChild(sayBox);
        sayRow.AddChild(Small("Speak", Announce));

        StyleField(logBox, "A note for the town's log", Towns.LogNoteMax);
        logBox.TextSubmitted += _ => WriteLog();
        logRow.AddThemeConstantOverride("separation", 6);
        logRow.AddChild(logBox);
        logRow.AddChild(Small("Write", WriteLog));
    }

    void WriteLog()
    {
        string text = logBox.Text.Trim();
        if (Conn == null || text.Length == 0) return;
        Conn.Reducers.WriteTownLog(text);
        logBox.Text = "";
        logBox.ReleaseFocus();
    }

    // Starts a town when you are on no roster, renames it when you are its Mayor.
    void SubmitName()
    {
        string name = nameBox.Text.Trim();
        if (Conn == null || name.Length == 0) return;
        if (Mine() == null) Conn.Reducers.StartTown(name);
        else Conn.Reducers.RenameTown(name);
        nameBox.Text = "";
        nameBox.ReleaseFocus();
    }

    void Announce()
    {
        string text = sayBox.Text.Trim();
        if (Conn == null || text.Length == 0) return;
        Conn.Reducers.AnnounceToTown(text);
        sayBox.Text = "";
        sayBox.ReleaseFocus();
    }

    // --- What the client knows -------------------------------------------------------------

    TownSigner? Mine() => Conn?.Db.TownSigner.Iter().FirstOrDefault(s => s.PlayerId == MyId());

    Claim? MyClaim()
    {
        long now = SocialLayer.NowMicros;
        return Conn?.Db.Claim.Iter().FirstOrDefault(c => c.OwnerPlayerId == MyId() && c.ExpiresAt.MicrosecondsSinceUnixEpoch > now);
    }

    // Mirrors the server's HasTownPower, only to hide buttons; the server still checks.
    static bool HasPower(List<TownOffice> seats, uint who, TownPower power)
    {
        foreach (var seat in seats)
        {
            if (seat.HolderId == 0 || seat.HolderId != who || Towns.Get(seat.Office) is not { } def) continue;
            if ((def.Powers & power) != 0) return true;
            if (def.StandsInFor != TownOfficeDef.NoOffice && Towns.Get(def.StandsInFor) is { } covered
                && (covered.Powers & power) != 0 && !seats.Any(o => o.Office == def.StandsInFor && o.HolderId != 0))
                return true;
        }
        return false;
    }

    static int Majority(int signers) => signers / 2 + 1;

    // The town as it stands, read once per rebuild.
    sealed class Snapshot
    {
        public Town? Town;
        public TownSigner? Me;
        public Claim? MyClaim;
        public List<TownSigner> Signers = new();
        public List<TownOffice> Seats = new();
        public List<TownVote> Votes = new();
        public string NameOf(uint playerId) => Signers.FirstOrDefault(s => s.PlayerId == playerId)?.OwnerName ?? "someone";
        public IEnumerable<TownOfficeDef> OfficesOf(uint playerId) =>
            Seats.Where(o => o.HolderId == playerId && o.HolderId != 0).Select(o => Towns.Get(o.Office)).OfType<TownOfficeDef>().OrderBy(d => d.Id);
    }

    Snapshot Read()
    {
        var s = new Snapshot { Me = Mine(), MyClaim = MyClaim() };
        if (Conn == null || s.Me == null) return s;
        ulong townId = s.Me.TownId;
        s.Town = Conn.Db.Town.Id.Find(townId);
        s.Signers = Conn.Db.TownSigner.ByTown.Filter(townId).OrderBy(r => r.SignedAt.MicrosecondsSinceUnixEpoch).ToList();
        s.Seats = Conn.Db.TownOffice.ByTown.Filter(townId).ToList();
        s.Votes = Conn.Db.TownVote.ByTown.Filter(townId).ToList();
        return s;
    }

    // The map's circles: the town's signed claims (or just yours, before you are on a roster),
    // and every standing claim within Social.TownNearMetres of one of them.
    List<TownMap.Spot> Spots(Snapshot s)
    {
        var spots = new List<TownMap.Spot>();
        if (Conn == null) return spots;
        long now = SocialLayer.NowMicros;
        uint me = MyId();
        var anchors = new List<Claim>();
        foreach (var signer in s.Signers)
            if (Conn.Db.Claim.Id.Find(signer.ClaimId) is { } c) anchors.Add(c);
        if (anchors.Count == 0 && s.MyClaim != null) anchors.Add(s.MyClaim);

        foreach (var c in anchors)
        {
            bool signed = s.Me != null;
            var badges = signed ? s.OfficesOf(c.OwnerPlayerId).Select(d => TownMap.Mark(d.Id)).ToList() : new List<string>();
            spots.Add(new TownMap.Spot(c.Id, c.X, c.Z, c.OwnerName, c.OwnerPlayerId, signed ? TownMap.Kind.Signed : TownMap.Kind.Free, c.OwnerPlayerId == me, badges));
        }
        foreach (var c in Conn.Db.Claim.Iter())
        {
            if (c.ExpiresAt.MicrosecondsSinceUnixEpoch <= now || anchors.Any(a => a.Id == c.Id)) continue;
            if (!anchors.Any(a => Social.TownNear(a.X, a.Z, c.X, c.Z))) continue;
            var kind = Conn.Db.TownSigner.ClaimId.Find(c.Id) != null ? TownMap.Kind.OtherTown : TownMap.Kind.Free;
            spots.Add(new TownMap.Spot(c.Id, c.X, c.Z, c.OwnerName, c.OwnerPlayerId, kind, false, Array.Empty<string>()));
        }
        return spots;
    }

    // --- Building the panel --------------------------------------------------------------

    void Rebuild()
    {
        dirty = false;
        if (Conn == null) return;
        foreach (var child in side.GetChildren())
        {
            side.RemoveChild(child);
            if (child != nameRow && child != sayRow && child != logRow) child.QueueFree();
        }
        var s = Read();
        if (s.Me == null) votingFor = null;
        var invites = Conn.Db.MyTownInvites.Iter().ToList();

        title.Text = s.Town?.Name ?? "Town";
        var spots = Spots(s);
        bool mapShown = spots.Count > 0;
        map.Visible = mapLegend.Visible = card.Visible = mapShown;
        if (spots.All(p => p.ClaimId != selected)) selected = 0;
        map.Selected = selected;
        map.Prompt = votingFor is { } office && Towns.Get(office) is { } def ? $"Click a signer to vote for {def.Name}" : "";
        map.SetSpots(spots);
        BuildCard(s, spots.FirstOrDefault(p => p.ClaimId == selected));

        if (s.Me == null && s.MyClaim == null && invites.Count == 0)
        {
            side.AddChild(Note("A town starts from a claim marker. Set one down, then come back here to start a roster or to sign a neighbour's."));
            return;
        }

        if (invites.Count > 0) BuildInvites(invites, s);
        if (s.Me == null)
        {
            if (s.MyClaim != null)
            {
                side.AddChild(Heading("Start a town", 16));
                side.AddChild(Note("Name it and you sign first, as its Mayor. Then invite the neighbours whose markers stand near yours."));
                nameBox.PlaceholderText = "Name the town";
                nameButton.Text = "Start";
                KeepFocus(nameRow, nameBox);
            }
            else side.AddChild(Note("You need a claim of your own to sign with."));
            return;
        }

        BuildRoster(s);
        BuildOffices(s);
        BuildMerges(s);
        BuildActions(s);
    }

    // Adds a kept row back, handing focus back if it had it.
    void KeepFocus(Control row, LineEdit box)
    {
        bool typing = box.HasFocus();
        side.AddChild(row);
        if (typing) box.CallDeferred(Control.MethodName.GrabFocus);
    }

    void BuildInvites(List<TownInvite> invites, Snapshot s)
    {
        side.AddChild(Heading("Invitations", 16));
        long now = SocialLayer.NowMicros;
        foreach (var invite in invites.OrderByDescending(i => i.At.MicrosecondsSinceUnixEpoch))
        {
            var pin = PinnedNote();
            var inner = (VBoxContainer)pin.GetChild(0);
            bool expired = now - invite.At.MicrosecondsSinceUnixEpoch > Social.TownInviteMicros;
            inner.AddChild(InkLabel($"{invite.InvitedBy} asks you to sign the roster of {invite.TownName}.", 14));
            inner.AddChild(InkLabel(expired ? "This invitation has run out." : "Signers may build on each other's claims and vote for the town's offices.", 12, 0.7f));
            var buttons = new HBoxContainer();
            buttons.AddThemeConstantOverride("separation", 8);
            if (!expired && s.Me == null) buttons.AddChild(InkButton("Sign", () => Conn?.Reducers.SignTown(invite.TownId)));
            buttons.AddChild(InkButton("Decline", () => Conn?.Reducers.DeclineTown(invite.TownId)));
            inner.AddChild(buttons);
            side.AddChild(pin);
        }
    }

    void BuildRoster(Snapshot s)
    {
        side.AddChild(Heading($"Roster  ·  {s.Signers.Count} signed", 16));
        uint me = MyId();
        foreach (var signer in s.Signers)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            string offices = string.Join(", ", s.OfficesOf(signer.PlayerId).Select(d => d.Name));
            string text = signer.PlayerId == me ? $"{signer.OwnerName} (you)" : signer.OwnerName;
            var name = Flat(offices == "" ? text : $"{text}   ·   {offices}", () => SelectPlayer(signer.PlayerId));
            name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(name);
            if (votingFor is { } office) row.AddChild(Small("Vote", () => Vote(office, signer.PlayerId)));
            side.AddChild(row);
        }
    }

    void BuildOffices(Snapshot s)
    {
        side.AddChild(Heading("Offices", 16));
        side.AddChild(Note($"Open ballot. {Majority(s.Signers.Count)} of {s.Signers.Count} votes seat someone at once; otherwise the most votes win when the term ends."));
        long now = SocialLayer.NowMicros;
        uint me = MyId();
        foreach (var def in Towns.Offices)
        {
            var seat = s.Seats.FirstOrDefault(o => o.Office == def.Id);
            string holder = seat == null || seat.HolderId == 0 ? "vacant" : seat.HolderName;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            var label = new Label { Text = $"{def.Name}  ·  {holder}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            Hud.Style(label, 15, 0.9f);
            if (votingFor == def.Id) label.AddThemeColorOverride("font_color", Chosen);
            row.AddChild(label);
            row.AddChild(Small(votingFor == def.Id ? "Cancel" : "Vote", () =>
            {
                votingFor = votingFor == def.Id ? null : def.Id;
                dirty = true;
            }));
            side.AddChild(row);
            side.AddChild(Note(def.Job));

            var ballot = s.Votes.Where(v => v.Office == def.Id).ToList();
            if (ballot.Count > 0)
            {
                var tally = ballot.GroupBy(v => v.CandidateId).OrderByDescending(g => g.Count())
                    .Select(g => $"{s.NameOf(g.Key)} {g.Count()} ({string.Join(", ", g.Select(v => v.VoterId == me ? "you" : s.NameOf(v.VoterId)))})");
                side.AddChild(Note($"Votes: {string.Join(";  ", tally)}   ·   {Majority(s.Signers.Count)} needed"));
            }
            if (seat != null)
                side.AddChild(Note($"Term ends {Until(seat.TermEndsAt.MicrosecondsSinceUnixEpoch - now)}"));
            if (votingFor == def.Id) side.AddChild(Note($"Click a signer on the map or a name in the roster to vote for {def.Name}."));
        }
    }

    void BuildMerges(Snapshot s)
    {
        if (Conn == null || s.Me == null) return;
        ulong mine = s.Me.TownId;
        var merges = Conn.Db.TownMerge.Iter().Where(m => m.FromTownId == mine || m.ToTownId == mine).ToList();
        if (merges.Count == 0) return;
        side.AddChild(Heading("Joining towns", 16));
        long now = SocialLayer.NowMicros;
        foreach (var merge in merges)
        {
            var votes = Conn.Db.TownMergeVote.ByMerge.Filter(merge.Id).ToList();
            string Tally(ulong townId, string name)
            {
                int signers = Conn.Db.TownSigner.ByTown.Filter(townId).Count();
                int yes = votes.Count(v => v.TownId == townId && v.Yes);
                return $"{name}: {yes} yes, {Majority(signers)} of {signers} needed";
            }
            var pin = PinnedNote();
            var inner = (VBoxContainer)pin.GetChild(0);
            inner.AddChild(InkLabel($"Join {merge.FromName} and {merge.ToName} into one town?", 14));
            inner.AddChild(InkLabel($"{Tally(merge.FromTownId, merge.FromName)}\n{Tally(merge.ToTownId, merge.ToName)}", 12, 0.8f));
            inner.AddChild(InkLabel($"The smaller town joins the larger, keeping its name; the offices start afresh. Open {Until(merge.ExpiresAt.MicrosecondsSinceUnixEpoch - now)} more.", 12, 0.7f));
            var myVote = votes.FirstOrDefault(v => v.Voter == s.Me.Owner);
            var buttons = new HBoxContainer();
            buttons.AddThemeConstantOverride("separation", 8);
            buttons.AddChild(InkButton(myVote is { Yes: true } ? "Yes (your vote)" : "Yes", () => Conn?.Reducers.VoteTownMerge(merge.Id, true)));
            buttons.AddChild(InkButton(myVote is { Yes: false } ? "No (your vote)" : "No", () => Conn?.Reducers.VoteTownMerge(merge.Id, false)));
            inner.AddChild(buttons);
            side.AddChild(pin);
        }
    }

    void BuildActions(Snapshot s)
    {
        side.AddChild(Heading("The town", 16));
        uint me = MyId();
        if (HasPower(s.Seats, me, TownPower.Rename))
        {
            nameBox.PlaceholderText = "A new name for the town";
            nameButton.Text = "Rename";
            KeepFocus(nameRow, nameBox);
        }
        if (HasPower(s.Seats, me, TownPower.Announce)) KeepFocus(sayRow, sayBox);
        if (s.Town != null) BuildLog(s, me);
        side.AddChild(Button(confirmLeave ? "Really leave the roster?" : "Leave the town", () =>
        {
            if (!confirmLeave) { confirmLeave = true; dirty = true; return; }
            confirmLeave = false;
            Conn?.Reducers.LeaveTown();
        }));
        side.AddChild(Note("Leaving takes away your building rights on the others' claims, and theirs on yours."));
    }

    // The latest lines of the town's log; the Scribe can add a note.
    void BuildLog(Snapshot s, uint me)
    {
        side.AddChild(Heading("The town's log", 16));
        var lines = Conn!.Db.TownLog.ByTown.Filter(s.Town!.Id).OrderByDescending(l => l.Id).Take(LogShown).ToList();
        if (lines.Count == 0) side.AddChild(Note("Nothing written yet."));
        foreach (var line in lines) side.AddChild(Note(line.Text));
        if (HasPower(s.Seats, me, TownPower.Log)) KeepFocus(logRow, logBox);
        var bans = Conn.Db.TownBan.ByTown.Filter(s.Town.Id).Where(b => b.Until.MicrosecondsSinceUnixEpoch > SocialLayer.NowMicros).ToList();
        foreach (var ban in bans)
            side.AddChild(Note($"{ban.BannedName} is kept off the town's claims {Until(ban.Until.MicrosecondsSinceUnixEpoch - SocialLayer.NowMicros)}."));
    }

    const int LogShown = 6;

    // The card under the map for the claim last clicked.
    void BuildCard(Snapshot s, TownMap.Spot? spot)
    {
        foreach (var child in card.GetChildren())
        {
            card.RemoveChild(child);
            child.QueueFree();
        }
        if (spot == null)
        {
            card.AddChild(Note(s.Me != null ? "Click a claim on the map." : "Your claim, on no roster yet."));
            return;
        }
        uint me = MyId();
        card.AddChild(Heading(spot.Mine ? $"{spot.Owner} (you)" : spot.Owner, 16));
        switch (spot.Kind)
        {
            case TownMap.Kind.Signed:
                string offices = string.Join(", ", s.OfficesOf(spot.OwnerId).Select(d => d.Name));
                card.AddChild(Note(offices == "" ? "Signed the roster." : $"Signed the roster. Holds: {offices}."));
                var actions = new HBoxContainer();
                actions.AddThemeConstantOverride("separation", 6);
                if (votingFor is { } office && Towns.Get(office) is { } def)
                    actions.AddChild(Small($"Vote for {def.Name}", () => Vote(office, spot.OwnerId)));
                if (!spot.Mine && HasPower(s.Seats, me, TownPower.Remove))
                    actions.AddChild(Small("Remove from town", () => Conn?.Reducers.RemoveFromTown(spot.OwnerId)));
                if (!spot.Mine && HasPower(s.Seats, me, TownPower.Ban))
                {
                    bool banned = Conn?.Db.TownBan.ByTown.Filter(s.Me!.TownId).Any(b => b.BannedId == spot.OwnerId && b.Until.MicrosecondsSinceUnixEpoch > SocialLayer.NowMicros) == true;
                    actions.AddChild(banned
                        ? Small("Lift the ban", () => Conn?.Reducers.LiftTownBan(spot.OwnerId))
                        : Small("Ban for a day", () => Conn?.Reducers.BanFromTown(spot.OwnerId, Towns.BanDaysDefault)));
                }
                if (actions.GetChildCount() > 0) card.AddChild(actions);
                else actions.Free();
                break;
            case TownMap.Kind.Free:
                card.AddChild(Note(spot.Mine ? "Your claim." : "A neighbour on no roster."));
                if (s.Me != null && !spot.Mine)
                    card.AddChild(Small($"Invite {spot.Owner}", () => Conn?.Reducers.InviteToTown(spot.OwnerId)));
                break;
            case TownMap.Kind.OtherTown:
                ulong? other = Conn?.Db.TownSigner.ClaimId.Find(spot.ClaimId)?.TownId;
                string name = other is { } id ? Conn?.Db.Town.Id.Find(id)?.Name ?? "another town" : "another town";
                card.AddChild(Note($"On the roster of {name}."));
                if (s.Me == null || other is not { } otherId) break;
                if (HasPower(s.Seats, me, TownPower.Merge))
                    card.AddChild(Small($"Propose to join {name}", () => Conn?.Reducers.ProposeTownMerge(otherId)));
                else
                    card.AddChild(Note("Your Mayor can propose joining the two towns."));
                break;
        }
    }

    void Pick(TownMap.Spot spot)
    {
        // While choosing for an office, a signer's circle is a vote; anything else just selects.
        if (votingFor is { } office && spot.Kind == TownMap.Kind.Signed)
        {
            Vote(office, spot.OwnerId);
            return;
        }
        selected = selected == spot.ClaimId ? 0 : spot.ClaimId;
        dirty = true;
    }

    void SelectPlayer(uint playerId)
    {
        if (votingFor is { } office) { Vote(office, playerId); return; }
        if (Conn?.Db.TownSigner.Iter().FirstOrDefault(r => r.PlayerId == playerId) is { } row) selected = row.ClaimId;
        dirty = true;
    }

    void Vote(byte office, uint candidate)
    {
        Conn?.Reducers.VoteTownOffice(office, candidate);
        votingFor = null;
        dirty = true;
    }

    // --- Small helpers, in ClaimPanel's style -------------------------------------------

    // "in 3 days", "in 5 h", "in 12 min", "any moment".
    static string Until(long micros)
    {
        long sec = micros / 1_000_000;
        if (sec < 60) return "any moment";
        if (sec < 3600) return $"in {sec / 60} min";
        if (sec < 86400) return $"in {sec / 3600} h";
        long d = sec / 86400;
        return d == 1 ? "in a day" : $"in {d} days";
    }

    static Label Heading(string text, int size)
    {
        var label = new Label { Text = text };
        Hud.Style(label, size, 1f);
        return label;
    }

    static Label Note(string text)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(300, 0) };
        Hud.Style(label, 13, 0.65f);
        return label;
    }

    // A small parchment slip, like a note pinned to a board.
    static PanelContainer PinnedNote()
    {
        var pin = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = NotePaper,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 10, ContentMarginBottom = 10,
            ShadowColor = new Color(0, 0, 0, 0.3f), ShadowSize = 4, ShadowOffset = new Vector2(1, 2),
        };
        style.SetCornerRadiusAll(6);
        pin.AddThemeStyleboxOverride("panel", style);
        var inner = new VBoxContainer();
        inner.AddThemeConstantOverride("separation", 6);
        pin.AddChild(inner);
        return pin;
    }

    static Label InkLabel(string text, int size, float alpha = 1f)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(300, 0) };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color(NoteInk, alpha));
        return label;
    }

    static Button InkButton(string text, Action onPress)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeColorOverride("font_color", NoteInk);
        button.AddThemeColorOverride("font_hover_color", NoteInk);
        foreach (var (state, alpha) in new[] { ("normal", 0.12f), ("hover", 0.22f), ("pressed", 0.32f) })
        {
            var style = new StyleBoxFlat { BgColor = new Color(NoteInk, alpha), ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 4, ContentMarginBottom = 4 };
            style.SetCornerRadiusAll(8);
            button.AddThemeStyleboxOverride(state, style);
        }
        button.Pressed += onPress;
        return button;
    }

    // A roster name you can click: plain text that lights on hover.
    static Button Flat(string text, Action onPress)
    {
        var button = new Button { Text = text, Flat = true, FocusMode = Control.FocusModeEnum.None, Alignment = HorizontalAlignment.Left };
        button.AddThemeFontSizeOverride("font_size", 15);
        button.AddThemeColorOverride("font_color", new Color(Look.Cream, 0.9f));
        button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.95f, 0.8f));
        button.Pressed += onPress;
        return button;
    }

    static Button Button(string text, Action onPress)
    {
        var button = new Button { Text = text };
        StyleButton(button, 15, 10);
        button.Pressed += onPress;
        return button;
    }

    static Button Small(string text, Action onPress)
    {
        var button = new Button { Text = text };
        StyleButton(button, 13, 6);
        button.Pressed += onPress;
        return button;
    }

    // ClaimPanel's button look.
    static void StyleButton(Button button, int size, int pad)
    {
        button.FocusMode = Control.FocusModeEnum.None;
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
    }

    // ClaimPanel's text field look; Esc while typing only stops typing.
    void StyleField(LineEdit box, string placeholder, int max)
    {
        box.PlaceholderText = placeholder;
        box.MaxLength = max;
        box.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        box.AddThemeFontSizeOverride("font_size", 14);
        box.AddThemeColorOverride("font_color", Look.Cream);
        box.AddThemeColorOverride("font_placeholder_color", new Color(Look.Cream, 0.45f));
        box.AddThemeColorOverride("caret_color", Look.Cream);
        var field = new StyleBoxFlat { BgColor = new Color(Look.Cream, 0.08f), ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 4, ContentMarginBottom = 4 };
        field.SetCornerRadiusAll(10);
        box.AddThemeStyleboxOverride("normal", field);
        box.AddThemeStyleboxOverride("focus", field);
        box.GuiInput += e =>
        {
            if (e.IsActionPressed("ui_cancel"))
            {
                box.ReleaseFocus();
                GetViewport().SetInputAsHandled();
            }
        };
    }
}
