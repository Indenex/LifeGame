using SpacetimeDB.Types;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Other players' jumps (bugfix-25): the server's player_jump row says someone left the
// ground, and we play the arc on their avatar: pose, lift and sound.
public sealed class JumpView
{
    sealed class Flight
    {
        public Avatar Avatar = null!;
        public float Seconds;
    }

    readonly Dictionary<uint, Flight> flying = new();

    // `avatarOf` finds a remote player's avatar; `groundAt` names the ground under a spot for the landing sound.
    public void Attach(DbConnection conn, System.Func<uint, Avatar?> avatarOf, System.Func<uint> myId)
    {
        void Jumped(PlayerJump row)
        {
            if (row.PlayerId == myId()) return;
            if (avatarOf(row.PlayerId) is not { } avatar || !avatar.Visible) return;
            flying[row.PlayerId] = new Flight { Avatar = avatar };
            avatar.SetJump(true, true, 0f);
            Sfx.PlayAt(Cue.Jump, avatar.GlobalPosition);
        }
        conn.Db.PlayerJump.OnInsert += (_, row) => Jumped(row);
        conn.Db.PlayerJump.OnUpdate += (_, _, row) => Jumped(row);
    }

    public void Process(float dt, System.Func<Vector3, Ground> groundAt)
    {
        if (flying.Count == 0) return;
        List<uint>? done = null;
        foreach (var (id, f) in flying)
        {
            if (!GodotObject.IsInstanceValid(f.Avatar)) { (done ??= new()).Add(id); continue; }
            f.Seconds += dt;
            if (f.Seconds >= Jumping.AirSeconds)
            {
                f.Avatar.SetAirHeight(0f);
                f.Avatar.SetJump(false, false, Jumping.LandingStrength);
                Sfx.LandAt(Jumping.LandingStrength, groundAt(f.Avatar.GlobalPosition), f.Avatar.GlobalPosition);
                (done ??= new()).Add(id);
            }
            else
            {
                f.Avatar.SetAirHeight(Jumping.Height(f.Seconds));
                f.Avatar.SetJump(true, Jumping.Rising(f.Seconds), 0f);
            }
        }
        if (done != null) foreach (var id in done) flying.Remove(id);
    }
}
