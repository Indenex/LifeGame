using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-28: the town's store in the Town panel. It works when the panel was opened at the
// town's main marker (the claim on the roster longest). Anyone on the roster drags things in
// (or clicks them in your bag); the Treasurer and the Mayor click or drag them back out, and
// the Treasurer keeps up the town's claims from it. The server checks the roster, the reach
// and the office, and writes every move in the log.
public partial class TownPanel
{
    // The claim whose marker opened the panel (SocialLayer sets it); 0 when none.
    public ulong AtClaim;
    readonly VBoxContainer storeBox = new();
    BagGrid? storeGrid, storeBag;

    // Kept across rebuilds like the name boxes, so a drag in progress isn't cut short.
    void ReadyStore()
    {
        storeBox.AddThemeConstantOverride("separation", 6);
        storeGrid = new BagGrid
        {
            Kind = "townstore", Width = 7, Empty = "Empty",
            Source = () => Conn != null && Mine() is { } me
                ? Conn.Db.MyTownStore.Iter().Where(s => s.TownId == me.TownId).OrderBy(s => s.Item).ThenByDescending(s => s.Quality)
                    .Select(s => new BagStack((Item)s.Item, s.Quality, s.Count, 0, -1, Marks.MakerOf((Item)s.Item, s.Wear)))
                : Enumerable.Empty<BagStack>(),
            Accepts = d => d["from"].AsString() == "bag",
            Receive = d => Conn?.Reducers.PutInTownStore(AtClaim, (ushort)d["item"].AsInt32(), (byte)d["quality"].AsInt32(), (uint)d["count"].AsInt64()),
            // A click takes one into the bag, Shift+click the whole stack (as the claim stash does).
            Clicked = (s, all) => Conn?.Reducers.TakeFromTownStore(AtClaim, (ushort)s.Item, s.Quality, all ? s.Count : 1),
        };
        storeBag = new BagGrid
        {
            Kind = "bag", Width = 7, Empty = "Nothing",
            Source = () => Conn != null
                ? Conn.Db.MyInventory.Iter().Where(i => i.Count > 0 && !i.OnBelt).Select(i => new BagStack((Item)i.Item, i.Quality, i.Count, 0, -1, Marks.MakerOf((Item)i.Item, i.Wear)))
                : Enumerable.Empty<BagStack>(),
            Accepts = d => d["from"].AsString() == "townstore",
            Receive = d => Conn?.Reducers.TakeFromTownStore(AtClaim, (ushort)d["item"].AsInt32(), (byte)d["quality"].AsInt32(), (uint)d["count"].AsInt64()),
            Clicked = (s, all) => Conn?.Reducers.PutInTownStore(AtClaim, (ushort)s.Item, s.Quality, all ? s.Count : 1),
        };
        storeBox.AddChild(Note("In the store"));
        storeBox.AddChild(storeGrid);
        storeBox.AddChild(Note("You carry"));
        storeBox.AddChild(storeBag);
    }

    public void RefreshStore()
    {
        if (!IsOpen) return;
        storeGrid?.Refresh();
        storeBag?.Refresh();
    }

    void BuildStore(Snapshot s)
    {
        if (Conn == null || s.Me == null) return;
        side.AddChild(Heading("The town's store", 16));
        var main = MainClaim(s);
        if (main == null || main.Id != AtClaim)
        {
            side.AddChild(Note(main == null ? "The town has no standing marker for its store."
                : $"The store is kept at {main.OwnerName}'s marker. Open the Town panel there to reach it."));
            return;
        }
        bool mayTake = HasPower(s.Seats, MyId(), TownPower.Store);
        side.AddChild(Note(mayTake
            ? "Click to move one, Shift+click a whole stack, or drag. Every put and take goes in the log."
            : "Anyone on the roster can put things in. Only the Treasurer and the Mayor take them out. Every move goes in the log."));
        side.AddChild(storeBox);
        RefreshStore();
        if (HasPower(s.Seats, MyId(), TownPower.Upkeep)) BuildUpkeep(s);
    }

    // The town's main marker: the standing claim on the roster longest, as the server decides.
    Claim? MainClaim(Snapshot s)
    {
        long now = SocialLayer.NowMicros;
        foreach (var signer in s.Signers.OrderBy(r => r.SignedAt.MicrosecondsSinceUnixEpoch).ThenBy(r => r.Id))
            if (Conn!.Db.Claim.Id.Find(signer.ClaimId) is { } claim && claim.ExpiresAt.MicrosecondsSinceUnixEpoch > now) return claim;
        return null;
    }

    // The Treasurer renews any claim on the roster from here, for a stick and some fiber from the store.
    void BuildUpkeep(Snapshot s)
    {
        side.AddChild(Note($"Keeping up a claim renews its marker as if its owner had tended it, for {Towns.UpkeepSticks} {Items.Def(Item.Stick).Count(Towns.UpkeepSticks).ToLowerInvariant()} and {Towns.UpkeepFiber} {Items.Def(Item.Fiber).Count(Towns.UpkeepFiber).ToLowerInvariant()} from the store."));
        long now = SocialLayer.NowMicros;
        foreach (var signer in s.Signers)
        {
            if (Conn!.Db.Claim.Id.Find(signer.ClaimId) is not { } claim) continue;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            var label = new Label { Text = $"{claim.OwnerName}  ·  marker falls {Until(claim.ExpiresAt.MicrosecondsSinceUnixEpoch - now)}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            Hud.Style(label, 14, 0.85f);
            row.AddChild(label);
            ulong id = claim.Id;
            row.AddChild(Small("Keep up", () => Conn?.Reducers.KeepUpTownClaim(AtClaim, id)));
            side.AddChild(row);
        }
    }
}
