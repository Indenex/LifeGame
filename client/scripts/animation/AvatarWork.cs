using System;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Drives each avatar's work pose from its running craft job, and other players'
// sneaking from player_sneaking. Your own job comes from my_craft_job; other players'
// will come from the public craft_work table once atmosphere-12 merges (D-072).
public static class AvatarWork
{
    public static void Attach(DbConnection conn, Avatar me, Func<uint, Avatar?> other)
    {
        conn.Db.MyCraftJob.OnInsert += (_, j) =>
        {
            Start(me, j.ProcessId);
            // Station crafts turn you to face the station you are working at (D-098).
            if (Processes.Find(j.ProcessId) is { Station: not StationKind.None } def
                && Stations.NearestOf(conn, def.Station, me.Position) is { } st)
                me.Face(st.X, st.Z);
        };
        conn.Db.MyCraftJob.OnDelete += (_, _) => Start(me, null);

        me.SneakChanged += sneaking => conn.Reducers.SetSneaking(sneaking);
        conn.Db.PlayerSneaking.OnInsert += (_, s) => { if (other(s.PlayerId) is { } a) a.Sneaking = true; };
        conn.Db.PlayerSneaking.OnDelete += (_, s) => { if (other(s.PlayerId) is { } a) a.Sneaking = false; };
        // Main creates an avatar on its first position (its handler runs before this one),
        // which can arrive after that player's sneaking row.
        conn.Db.PlayerPos.OnInsert += (_, p) =>
        {
            if (other(p.PlayerId) is { } a) a.Sneaking = conn.Db.PlayerSneaking.PlayerId.Find(p.PlayerId) is not null;
        };
    }

    static void Start(Avatar avatar, string? processId)
    {
        avatar.Work = WorkMotions.For(processId);
    }
}
