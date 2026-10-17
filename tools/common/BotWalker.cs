// One way for every check bot to walk (integration-12). The tools used to carry five copies of a walk, and
// each could wedge a bot on a trunk, wall or the server's pace and fail a check that was testing something
// else. This walk routes round obstacles (BotGround), watches the SERVER's position for a stall (under
// StallMetres in StallSeconds), sidesteps and re-routes twice, and only then puts the bot at its target with
// the admin-only admin_teleport, logging a ::warning:: so the stuck spot still gets seen and fixed. A strict
// walker never teleports: a stall is a failure. Exactly one tool (SpawnCheck's water walk) is strict, because
// it is the one place walking itself is tested. Linked into each tool with
// <Compile Include="../common/*.cs" LinkBase="Common" />.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB.Types;

public sealed class BotWalker
{
    // No progress of this far in this long counts as a stall.
    // (A field, so the social check can make every walk stall once to exercise the fallback.)
    public static float StallMetres = 0.3f;
    public const double StallSeconds = 3.0;
    const int Sidesteps = 2;

    readonly DbConnection conn;
    readonly uint myId;
    readonly string tool, bot;
    readonly Action<int> pump;
    readonly Action<string> fail;
    readonly bool strict;

    public BotGround Ground { get; }
    // Times the walker had to put its bot at the target; for a summary line at the end of a run.
    public static int Fallbacks { get; private set; }

    // pump(ms) lets the tool's connection run for that long; fail(message) ends the run with the tool's own failure.
    public BotWalker(DbConnection conn, uint playerId, string tool, string bot, Action<int> pump, Action<string> fail, bool strict = false)
    {
        this.conn = conn;
        myId = playerId;
        this.tool = tool;
        this.bot = bot;
        this.pump = pump;
        this.fail = fail;
        this.strict = strict;
        Ground = new BotGround(conn);
    }

    // Makes this bot an admin through the CLI as the database owner, so the teleport fallback is allowed.
    // The bot's name must be unique among players (make_admin finds it by name); a bot that shares one is
    // renamed. Returns false when it could not be done, and the walker then fails with the stall instead.
    public bool MakeAdmin(string uri, string db)
    {
        string name = conn.Db.Player.PlayerId.Find(myId)?.Name ?? "";
        if (name.Length == 0 || conn.Db.Player.Iter().Count(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) != 1)
        {
            name = $"{tool.Replace(" ", "")}{Random.Shared.Next(10000, 99999)}";
            conn.Reducers.SetName(name);
            for (int i = 0; i < 50 && conn.Db.Player.PlayerId.Find(myId)?.Name != name; i++) pump(100);
        }
        // Runs from the temp folder: inside server/ the CLI reads spacetime.json and misparses.
        var info = new ProcessStartInfo("spacetimedb-cli", new[] { "call", "--no-config", "--server", uri, db, "make_admin", "\"" + name + "\"" })
        {
            WorkingDirectory = System.IO.Path.GetTempPath(),
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        try
        {
            using var p = Process.Start(info);
            if (p == null) return false;
            p.StandardOutput.ReadToEnd();
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0) Console.WriteLine($"::warning::{tool} {bot}: make_admin failed ({err.Trim()}); a stuck walk will fail instead of repositioning");
            admin = p.ExitCode == 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"::warning::{tool} {bot}: could not run spacetimedb-cli ({e.Message}); a stuck walk will fail instead of repositioning");
        }
        return admin;
    }

    bool admin;

    // Walks to within `near` of a spot and waits until the server agrees where the bot is. x and z are the
    // bot's own idea of where it stands (the tool's variables), kept in step with the server.
    // stand: first move a target that lies inside a boulder or solid out to where a player could stand.
    public void To(ref float x, ref float z, float tx, float tz, float near, bool stand = false)
    {
        if (stand) (tx, tz) = Ground.Stand(tx, tz);
        float startX = x, startZ = z;
        for (int attempt = 0; ; attempt++)
        {
            // Round trunks and boulders by a route, then straight in (bugfix-34).
            var route = Ground.Route(x, z, tx, tz);
            bool ok = true;
            for (int i = 0; i < route.Count - 1 && ok; i++) ok = Leg(ref x, ref z, route[i].X, route[i].Z, 0.4f);
            ok = ok && Leg(ref x, ref z, tx, tz, near) && Settle(ref x, ref z);
            if (ok) return;

            if (attempt < Sidesteps)
            {
                // Take the server's word for where we are, step aside (alternating sides, wider each time), re-route.
                AdoptServer(ref x, ref z);
                float dx = tx - x, dz = tz - z, d = MathF.Sqrt(dx * dx + dz * dz);
                if (d > 0.1f)
                {
                    float side = (attempt % 2 == 0 ? 1f : -1f) * (2f + attempt);
                    Leg(ref x, ref z, x - dz / d * side, z + dx / d * side, 0.5f, giveUpAfter: 40);
                }
                continue;
            }

            string where = $"{tool} {bot} stuck walking from ({startX:F1}, {startZ:F1}) to ({tx:F1}, {tz:F1}): bot ({x:F1}, {z:F1}), server {ServerAt()}";
            if (strict || !admin) fail(where + (strict ? " (strict walk: no repositioning)" : " (bot is not an admin, so it cannot be repositioned)") + "\n  near: " + Describe(x, z));
            Teleport(ref x, ref z, tx, tz, where);
            return;
        }
    }

    // One straight stretch at a safe pace. False when the server's position stopped following (a stall).
    bool Leg(ref float x, ref float z, float tx, float tz, float near, int giveUpAfter = int.MaxValue)
    {
        int lagged = 0;
        var progress = Stopwatch.StartNew();
        float markX = x, markZ = z;
        if (conn.Db.PlayerPos.PlayerId.Find(myId) is { } first) { markX = first.X; markZ = first.Z; }
        for (int n = 0; ; n++)
        {
            float dx = tx - x, dz = tz - z, d = MathF.Sqrt(dx * dx + dz * dz);
            if (d <= near) return true;
            if (n >= giveUpAfter) return true;
            var seen = conn.Db.PlayerPos.PlayerId.Find(myId);
            if (seen is { } s)
            {
                // The server has moved on from where we last measured: that is progress.
                if (MathF.Abs(s.X - markX) + MathF.Abs(s.Z - markZ) >= StallMetres) { markX = s.X; markZ = s.Z; progress.Restart(); }
                else if (progress.Elapsed.TotalSeconds > StallSeconds) return false;
            }
            float step = MathF.Min(near < 0.5f ? d : d - near * 0.9f, 0.5f);
            // The server limits the pace (World.MoveAllowance, Carry.WalkFactor): only walk on once it has
            // caught up, and keep reporting where we are, as a real client does (bugfix-24).
            if (seen is { } held && MathF.Abs(held.X - x) + MathF.Abs(held.Z - z) >= 1.5f)
            {
                // The server has not followed for a second: it refused the last step (something solid in the
                // way). Take its word for where we are and carry on from there, like a player would.
                if (++lagged > 10) { x = held.X; z = held.Z; lagged = 0; }
            }
            else lagged = 0;
            if (seen is not { } at || MathF.Abs(at.X - x) + MathF.Abs(at.Z - z) < 1.5f)
            {
                float px = x, pz = z;
                x += dx / d * step;
                z += dz / d * step;
                (x, z) = Ground.Slide(px, pz, x, z); // round things and walls, as a player meets them
            }
            conn.Reducers.Move(x, z, MathF.Atan2(dx, dz));
            pump(100);
        }
    }

    // Keeps reporting until the server has the bot where the bot thinks it is.
    bool Settle(ref float x, ref float z)
    {
        for (var settle = Stopwatch.StartNew(); ; )
        {
            if (conn.Db.PlayerPos.PlayerId.Find(myId) is { } p && MathF.Abs(p.X - x) < 0.1f && MathF.Abs(p.Z - z) < 0.1f) return true;
            if (settle.Elapsed.TotalSeconds > 15) return false;
            conn.Reducers.Move(x, z, 0f);
            pump(100);
        }
    }

    void AdoptServer(ref float x, ref float z)
    {
        if (conn.Db.PlayerPos.PlayerId.Find(myId) is { } p) { x = p.X; z = p.Z; }
    }

    void Teleport(ref float x, ref float z, float tx, float tz, string where)
    {
        Fallbacks++;
        Console.WriteLine($"::warning::bot walk fallback: {where}; repositioned to the target. Near: {Describe(x, z)}");
        conn.Reducers.AdminTeleport(tx, tz);
        for (var wait = Stopwatch.StartNew(); wait.Elapsed.TotalSeconds < 10; pump(100))
            if (conn.Db.PlayerPos.PlayerId.Find(myId) is { } p && MathF.Abs(p.X - tx) < 0.5f && MathF.Abs(p.Z - tz) < 0.5f)
            {
                x = p.X;
                z = p.Z;
                return;
            }
        fail(where + "; and admin_teleport did not move the bot (is it an admin?)");
    }

    string ServerAt() => conn.Db.PlayerPos.PlayerId.Find(myId) is { } p ? $"({p.X:F1}, {p.Z:F1})" : "(unknown)";

    // What stands near a spot, so a blocked walk can be told apart from a slow one in the log.
    string Describe(float x, float z)
    {
        var near = new List<string>();
        for (int ix = (int)x - 3; ix <= (int)x + 3; ix++)
            for (int iz = (int)z - 3; iz <= (int)z + 3; iz++)
                if (Obstacles.At(Ground, ix, iz, out float ox, out float oz, out float r, out float h)) near.Add($"{(h == float.MaxValue ? "trunk" : "rock")}@({ox:F1},{oz:F1}) r{r:F2}");
        foreach (var st in conn.Db.Structure.Iter())
            if (MathF.Abs(st.X - x) < 6 && MathF.Abs(st.Z - z) < 6) near.Add($"{st.Kind}@({st.X:F1},{st.Z:F1}) done={st.Complete} stage={st.Stage}");
        return near.Count == 0 ? "nothing solid" : string.Join("; ", near);
    }
}
