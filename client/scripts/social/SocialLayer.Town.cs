using System;
using Godot;
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
    }

    void AttachTown(DbConnection c)
    {
        town.Conn = c;
        town.MyId = () => myId;
        Action dirty = town.MarkDirty;
        c.Db.Town.OnInsert += (_, _) => dirty(); c.Db.Town.OnUpdate += (_, _, _) => dirty(); c.Db.Town.OnDelete += (_, _) => dirty();
        c.Db.TownSigner.OnInsert += (_, _) => dirty(); c.Db.TownSigner.OnUpdate += (_, _, _) => dirty(); c.Db.TownSigner.OnDelete += (_, _) => dirty();
        c.Db.TownOffice.OnInsert += (_, _) => dirty(); c.Db.TownOffice.OnUpdate += (_, _, _) => dirty(); c.Db.TownOffice.OnDelete += (_, _) => dirty();
        c.Db.TownVote.OnInsert += (_, _) => dirty(); c.Db.TownVote.OnUpdate += (_, _, _) => dirty(); c.Db.TownVote.OnDelete += (_, _) => dirty();
        c.Db.TownMerge.OnInsert += (_, _) => dirty(); c.Db.TownMerge.OnUpdate += (_, _, _) => dirty(); c.Db.TownMerge.OnDelete += (_, _) => dirty();
        c.Db.TownMergeVote.OnInsert += (_, _) => dirty(); c.Db.TownMergeVote.OnUpdate += (_, _, _) => dirty(); c.Db.TownMergeVote.OnDelete += (_, _) => dirty();
        c.Db.MyTownInvites.OnInsert += (_, invite) => { dirty(); Invited(invite); };
        c.Db.MyTownInvites.OnDelete += (_, _) => dirty();
        c.Db.Claim.OnInsert += (_, _) => dirty(); c.Db.Claim.OnUpdate += (_, _, _) => dirty(); c.Db.Claim.OnDelete += (_, _) => dirty();
    }

    // The claim panel is docked, not centred, so it closes by hand; Panels.Opening closes other centre windows.
    void OpenTown()
    {
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
