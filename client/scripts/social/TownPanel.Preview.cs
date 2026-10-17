using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-30: --screenshot-town opens the Town panel on a pretend town with no server, for
// Render's stills (asked by UX for ui-72): five signers around a green, a Mayor, a Herald and a
// Treasurer, an open ballot, a few log lines and an invitation pinned up as a parchment note.
public partial class TownPanel
{
    bool preview;
    public bool Previewing => preview;
    const ulong PreviewTownId = 1;
    const uint PreviewMe = 2;

    void ReadyPreview()
    {
        if (!OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--screenshot-town"))) return;
        preview = true;
        MyId = () => PreviewMe;
        CallDeferred(MethodName.Open);
    }

    static Timestamp In(long seconds) => new(SocialLayer.NowMicros + seconds * 1_000_000);

    static readonly (uint id, string name, float x, float z)[] PreviewPeople =
    {
        (1, "Mira", 0, 0), (2, "Tam", 34, 8), (3, "Oren", -28, 22), (4, "Lise", 10, -36), (5, "Bram", -20, -24),
    };

    Snapshot PreviewSnapshot()
    {
        var s = new Snapshot { Town = new Town { Id = PreviewTownId, Name = "Willow Run", StartedAt = In(-3 * 86400) } };
        s.Signers = PreviewPeople.Select((p, i) => new TownSigner
        {
            Id = (ulong)(i + 1), TownId = PreviewTownId, ClaimId = 100 + p.id, PlayerId = p.id, OwnerName = p.name,
            SignedAt = In(-3 * 86400 + i * 3600),
        }).ToList();
        s.Me = s.Signers.First(r => r.PlayerId == PreviewMe);
        s.Seats = new List<TownOffice>
        {
            new() { Id = 1, TownId = PreviewTownId, Office = Social.OfficeMayor, HolderId = 1, HolderName = "Mira", TermEndsAt = In(2 * 86400) },
            new() { Id = 2, TownId = PreviewTownId, Office = 3, HolderId = 3, HolderName = "Oren", TermEndsAt = In(5 * 3600) },
            new() { Id = 3, TownId = PreviewTownId, Office = 6, HolderId = 2, HolderName = "Tam", TermEndsAt = In(86400) },
        };
        s.Votes = new List<TownVote>
        {
            new() { Id = 1, TownId = PreviewTownId, Office = 5, VoterId = 2, CandidateId = 4 },
            new() { Id = 2, TownId = PreviewTownId, Office = 5, VoterId = 3, CandidateId = 4 },
            new() { Id = 3, TownId = PreviewTownId, Office = 5, VoterId = 5, CandidateId = 5 },
        };
        return s;
    }

    List<TownMap.Spot> PreviewSpots(Snapshot s)
    {
        var spots = PreviewPeople.Select(p => new TownMap.Spot(100 + p.id, p.x, p.z, p.name, p.id, TownMap.Kind.Signed, p.id == PreviewMe,
            s.OfficesOf(p.id).Select(d => TownMap.Mark(d.Id)).ToList())).ToList();
        spots.Add(new TownMap.Spot(200, 52, -30, "Wren", 9, TownMap.Kind.Free, false, new List<string>()));
        spots.Add(new TownMap.Spot(201, -50, 40, "Hollis", 10, TownMap.Kind.OtherTown, false, new List<string>()));
        return spots;
    }

    static List<TownInvite> PreviewInvites() => new()
    {
        new() { Id = 1, TownId = 7, TownName = "Birch Hollow", InvitedBy = "Hollis", At = In(-600) },
    };

    void BuildPreviewLog()
    {
        side.AddChild(Heading("The town's log", 16));
        foreach (var line in new[]
        {
            "Tam put 6 sticks in the store", "Tam kept up Bram's claim from the store", "Oren: Market day on the green",
            "Lise signed the roster", "Oren became Herald", "Mira started the town",
        })
            side.AddChild(Note(line));
    }
}
