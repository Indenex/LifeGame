using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-19: the Town panel's wiring. It opens from the claim panel's "Town" button and
// redraws whenever a town, roster, office, vote, invitation, merge or claim row changes.
public partial class SocialLayer
{
    readonly TownPanel town = new();

    void ReadyTown()
    {
        AddChild(town);
        panel.OpenTown = OpenTown;
        if (OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--screenshot-sign")))
            GetTree().CreateTimer(1.5).Timeout += SignPreview;
    }

    // claims-33: --screenshot-sign stands a pretend town marker with its signboard a few steps ahead of
    // the player, with no server rows, for Render.
    void SignPreview()
    {
        var marker = new ClaimMarker(terrain);
        world.AddChild(marker);
        var spot = myPos + new Vector3(0, 0, -5f);
        var now = NowMicros;
        marker.Set(new Claim
        {
            Id = 9001, OwnerName = "Mira", X = spot.X, Z = spot.Z,
            TendedAt = new Timestamp(now), ExpiresAt = new Timestamp(now + 86_400_000_000L),
        }, true);
        marker.Rotation = Vector3.Zero;
        marker.SetSign("Willow Run");
    }

    void AttachTown(DbConnection c)
    {
        // The --screenshot-town preview keeps its pretend town, whatever the server holds.
        if (town.Previewing) return;
        town.Conn = c;
        town.MyId = () => myId;
        Action dirty = town.MarkDirty;
        c.Db.Town.OnInsert += (_, _) => dirty(); c.Db.Town.OnUpdate += (_, _, _) => dirty(); c.Db.Town.OnDelete += (_, _) => dirty();
        c.Db.TownSigner.OnInsert += (_, _) => dirty(); c.Db.TownSigner.OnUpdate += (_, _, _) => dirty(); c.Db.TownSigner.OnDelete += (_, _) => dirty();
        c.Db.TownOffice.OnInsert += (_, _) => dirty(); c.Db.TownOffice.OnUpdate += (_, _, _) => dirty(); c.Db.TownOffice.OnDelete += (_, _) => dirty();
        c.Db.TownVote.OnInsert += (_, _) => dirty(); c.Db.TownVote.OnUpdate += (_, _, _) => dirty(); c.Db.TownVote.OnDelete += (_, _) => dirty();
        c.Db.TownMerge.OnInsert += (_, _) => dirty(); c.Db.TownMerge.OnUpdate += (_, _, _) => dirty(); c.Db.TownMerge.OnDelete += (_, _) => dirty();
        c.Db.TownMergeVote.OnInsert += (_, _) => dirty(); c.Db.TownMergeVote.OnUpdate += (_, _, _) => dirty(); c.Db.TownMergeVote.OnDelete += (_, _) => dirty();
        c.Db.TownBan.OnInsert += (_, _) => dirty(); c.Db.TownBan.OnUpdate += (_, _, _) => dirty(); c.Db.TownBan.OnDelete += (_, _) => dirty();
        c.Db.TownLog.OnInsert += (_, _) => dirty(); c.Db.TownLog.OnUpdate += (_, _, _) => dirty(); c.Db.TownLog.OnDelete += (_, _) => dirty();
        c.Db.MyTownInvites.OnInsert += (_, invite) => { dirty(); Invited(invite); };
        c.Db.MyTownInvites.OnDelete += (_, _) => dirty();
        c.Db.Claim.OnInsert += (_, _) => dirty(); c.Db.Claim.OnUpdate += (_, _, _) => dirty(); c.Db.Claim.OnDelete += (_, _) => dirty();
        // claims-33: each town's main marker carries a signboard with the town's name.
        Action signs = RefreshSigns;
        c.Db.Town.OnInsert += (_, _) => signs(); c.Db.Town.OnUpdate += (_, _, _) => signs(); c.Db.Town.OnDelete += (_, _) => signs();
        c.Db.TownSigner.OnInsert += (_, _) => signs(); c.Db.TownSigner.OnDelete += (_, _) => signs();
        c.Db.Claim.OnInsert += (_, _) => signs(); c.Db.Claim.OnDelete += (_, _) => signs();
        RefreshSigns();
        // claims-28: the store's grids follow the store and your bag without a full redraw.
        Action store = town.RefreshStore;
        c.Db.MyTownStore.OnInsert += (_, _) => store(); c.Db.MyTownStore.OnUpdate += (_, _, _) => store(); c.Db.MyTownStore.OnDelete += (_, _) => store();
        c.Db.MyInventory.OnInsert += (_, _) => store(); c.Db.MyInventory.OnUpdate += (_, _, _) => store(); c.Db.MyInventory.OnDelete += (_, _) => store();
    }

    readonly Dictionary<ulong, string> signNames = new();

    // The main marker of a town is the standing claim on its roster longest, as the server and the
    // Town panel decide. Only that marker carries the town's name.
    void RefreshSigns()
    {
        if (conn == null) return;
        signNames.Clear();
        long now = NowMicros;
        foreach (var t in conn.Db.Town.Iter())
        {
            var roster = conn.Db.TownSigner.ByTown.Filter(t.Id).OrderBy(r => r.SignedAt.MicrosecondsSinceUnixEpoch).ThenBy(r => r.Id);
            foreach (var signer in roster)
                if (conn.Db.Claim.Id.Find(signer.ClaimId) is { } claim && claim.ExpiresAt.MicrosecondsSinceUnixEpoch > now)
                {
                    signNames[claim.Id] = t.Name;
                    break;
                }
        }
        foreach (var (id, marker) in markers) marker.SetSign(signNames.GetValueOrDefault(id));
    }

    // The claim panel is docked, not centred, so it closes by hand; Panels.Opening closes other centre windows.
    void OpenTown()
    {
        // The town's store works at the marker the panel was opened from (claims-28).
        town.AtClaim = panel.Claim?.Id ?? 0;
        panel.Close();
        town.Open();
    }

    // A fresh invitation gets a line on screen; it waits in the Town panel to be answered.
    void Invited(TownInvite invite)
    {
        if (NowMicros - invite.At.MicrosecondsSinceUnixEpoch > 60_000_000) return;
        hud.Note($"{invite.InvitedBy} asks you to sign the roster of {invite.TownName}. Answer from your claim marker's Town button");
    }
}
