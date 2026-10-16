using System;
using SpacetimeDB.Types;

namespace LifeGame;

// Drives each avatar's work pose from its running craft job. Your own job comes from
// my_craft_job; other players' will come from the public craft_work table once
// atmosphere-12 merges (D-072).
public static class AvatarWork
{
    public static void Attach(DbConnection conn, Avatar me, Func<uint, Avatar?> other)
    {
        conn.Db.MyCraftJob.OnInsert += (_, j) => Start(me, j.ProcessId);
        conn.Db.MyCraftJob.OnDelete += (_, _) => Start(me, null);
    }

    static void Start(Avatar avatar, string? processId)
    {
        avatar.Work = WorkMotions.For(processId);
        var (right, left) = HeldItems.For(processId);
        avatar.Hold(right, left);
    }
}
