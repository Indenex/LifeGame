using System;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Drives each avatar's work pose from its running craft job, other players' sneaking
// from player_sneaking, and everyone's clothes from worn_item. Your own job comes from
// my_craft_job; other players' from the public craft_work table (D-072), which
// Atmosphere already subscribes to.
public static class AvatarWork
{
    public static void Attach(DbConnection conn, Avatar me, Func<uint, Avatar?> other)
    {
        conn.Db.MyCraftJob.OnInsert += (_, j) =>
        {
            Start(me, j.ProcessId, conn);
            if (WorkMotions.For(j.ProcessId) == WorkMotion.Chop) FaceTree(me);
            // Station crafts turn you to the station you work at (D-098).
            else if (Processes.Find(j.ProcessId) is { Station: not StationKind.None } def
                && Stations.NearestOf(conn, def.Station, me.Position) is { } st)
                me.Face(st.X, st.Z);
        };
        conn.Db.MyCraftJob.OnDelete += (_, _) => Start(me, null);
        conn.Db.CraftWork.OnInsert += (_, w) => { if (other(w.PlayerId) is { } a) Start(a, w.ProcessId, tool: w.Tool); };
        conn.Db.CraftWork.OnDelete += (_, w) => { if (other(w.PlayerId) is { } a) Start(a, null); };

        me.SneakChanged += sneaking => conn.Reducers.SetSneaking(sneaking);
        conn.Db.PlayerSneaking.OnInsert += (_, s) => { if (other(s.PlayerId) is { } a) a.Sneaking = true; };
        conn.Db.PlayerSneaking.OnDelete += (_, s) => { if (other(s.PlayerId) is { } a) a.Sneaking = false; };
        // Main creates an avatar on its first position (its handler runs before this one),
        // which can arrive after that player's sneaking or work row.
        conn.Db.PlayerPos.OnInsert += (_, p) =>
        {
            if (other(p.PlayerId) is not { } a) return;
            a.Sneaking = conn.Db.PlayerSneaking.PlayerId.Find(p.PlayerId) is not null;
            var work = conn.Db.CraftWork.PlayerId.Find(p.PlayerId);
            Start(a, work?.ProcessId, tool: work?.Tool ?? 0);
            Dress(conn, a, p.PlayerId);
            if (conn.Db.Player.PlayerId.Find(p.PlayerId) is { } who)
                a.Equip(Equipped.ItemOf(conn.Db.PlayerEquip.Owner.Find(who.Identity)), instant: true);
        };

        // What everyone wears (worn_item, D-074). Wearing a new piece deletes the old
        // row and inserts the new one, so each change re-reads the player's whole set.
        void Changed(WornItem w)
        {
            if (w.Owner == Me.Key(conn)) Dress(conn, me, w.PlayerId);
            else if (other(w.PlayerId) is { } a) Dress(conn, a, w.PlayerId);
        }
        conn.Db.WornItem.OnInsert += (_, w) => Changed(w);

        // What everyone holds (crafting-29 PlayerEquip: the item in hand, one row per player,
        // none when the hand is empty).
        void Holding(PlayerEquip row, bool gone)
        {
            var avatar = row.Owner == Me.Key(conn) ? me
                : conn.Db.Player.Identity.Find(row.Owner) is { } p ? other(p.PlayerId) : null;
            avatar?.Equip(gone ? null : Equipped.ItemOf(row));
        }
        conn.Db.PlayerEquip.OnInsert += (_, row) => Holding(row, false);
        conn.Db.PlayerEquip.OnUpdate += (_, _, row) => Holding(row, false);
        conn.Db.PlayerEquip.OnDelete += (_, row) => Holding(row, true);

        // Quick actions anyone did (player_act, anim-4), your own included: the server
        // notes them only once they succeed.
        conn.Db.PlayerAct.OnInsert += (_, act) =>
        {
            AvatarOf(conn, me, other, act.PlayerId)?.Act((WorkMotion)act.Motion, act.Item == 0 ? null : (Item)act.Item);
        };
        // A bow shot (ArrowShot, hit or miss) and a struck player's flinch come from
        // events other lanes already send.
        conn.Db.ArrowShot.OnInsert += (_, shot) => AvatarOf(conn, me, other, shot.PlayerId)?.Act(WorkMotion.Shoot, Item.Bow);
        conn.Db.StrikeEvent.OnInsert += (_, hit) => AvatarOf(conn, me, other, hit.VictimId)?.Act(WorkMotion.Flinch, null);
        conn.Db.WornItem.OnDelete += (_, w) => Changed(w);
    }

    // Felling: face the nearest standing trunk within reach (shared/Trees.cs cells).
    static void FaceTree(Avatar avatar)
    {
        var at = avatar.Position;
        int cx = (int)Godot.Mathf.Floor(at.X), cz = (int)Godot.Mathf.Floor(at.Z);
        float best = 3f * 3f, bx = 0, bz = 0;
        for (int x = cx - 3; x <= cx + 3; x++)
            for (int z = cz - 3; z <= cz + 3; z++)
            {
                if (!Trees.GrowsAt(x, z)) continue;
                var (tx, tz) = Trees.Trunk(x, z);
                float d = (tx - at.X) * (tx - at.X) + (tz - at.Z) * (tz - at.Z);
                if (d < best) (best, bx, bz) = (d, tx, tz);
            }
        if (best < 3f * 3f) avatar.Face(bx, bz);
    }

    static Avatar? AvatarOf(DbConnection conn, Avatar me, Func<uint, Avatar?> other, uint playerId) =>
        playerId == MyPlayerId(conn) ? me : other(playerId);

    static uint? MyPlayerId(DbConnection conn) =>
        Me.Key(conn) is { } id ? conn.Db.Player.Identity.Find(id)?.PlayerId : null;

    static void Dress(DbConnection conn, Avatar avatar, uint playerId)
    {
        ushort body = 0, feet = 0;
        foreach (var w in conn.Db.WornItem.Iter())
        {
            if (w.PlayerId != playerId) continue;
            if (w.Slot == PlayerModel.Body) body = w.Item;
            else if (w.Slot == PlayerModel.Feet) feet = w.Item;
        }
        avatar.Wear(PlayerModel.Body, body);
        avatar.Wear(PlayerModel.Feet, feet);
    }

    // conn only for your own avatar: your inventory says which tool you work with.
    // Other players' craft_work says which tool they hold (tool, 0 if not known).
    static void Start(Avatar avatar, string? processId, DbConnection? conn = null, ushort tool = 0)
    {
        avatar.Work = WorkMotions.For(processId);
        var (right, left) = HeldTools.For(processId, conn?.Db.MyInventory.Iter().Select(i => (i.Item, i.Quality)));
        if (tool != 0 && Items.IsKnown(tool)) right = (Item)tool;
        avatar.Hold(right, left);
    }
}
