using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// claims-28: the town's store in the Town panel. It works at the marker the panel was opened
// from, when that claim is on your town's roster. Anyone on the roster drags things in (or
// clicks them in your bag); the Treasurer and the Mayor click or drag them back out. The
// server checks the roster, the reach and the office, and writes every move in the log.
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
        bool here = Conn.Db.TownSigner.ClaimId.Find(AtClaim) is { } there && there.TownId == s.Me.TownId;
        if (!here)
        {
            side.AddChild(Note("Open the Town panel at a marker on the town's roster to reach the store."));
            return;
        }
        bool mayTake = HasPower(s.Seats, MyId(), TownPower.Store);
        side.AddChild(Note(mayTake
            ? "Click to move one, Shift+click a whole stack, or drag. Every put and take goes in the log."
            : "Anyone on the roster can put things in. Only the Treasurer and the Mayor take them out. Every move goes in the log."));
        side.AddChild(storeBox);
        RefreshStore();
    }
}
