using System;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// Drives each avatar's work pose from its running craft job, and other players'
// sneaking from player_sneaking. Your own job comes from my_craft_job; other players'
// from the public craft_work table (D-072), which Atmosphere already subscribes to.
public static class AvatarWork
{
    public static void Attach(DbConnection conn, Avatar me, Func<uint, Avatar?> other)
    {
        conn.Db.MyCraftJob.OnInsert += (_, j) =>
        {
            Start(me, j.ProcessId);
            if (WorkMotions.For(j.ProcessId) == WorkMotion.Chop) FaceTree(me);
            // Station crafts turn you to the station you work at (D-098).
            else if (Processes.Find(j.ProcessId) is { Station: not StationKind.None } def
                && Stations.NearestOf(conn, def.Station, me.Position) is { } st)
                me.Face(st.X, st.Z);
        };
        conn.Db.MyCraftJob.OnDelete += (_, _) => Start(me, null);
        conn.Db.CraftWork.OnInsert += (_, w) => { if (other(w.PlayerId) is { } a) Start(a, w.ProcessId); };
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
            Start(a, conn.Db.CraftWork.PlayerId.Find(p.PlayerId)?.ProcessId);
        };
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

    static void Start(Avatar avatar, string? processId)
    {
        avatar.Work = WorkMotions.For(processId);
    }
}
