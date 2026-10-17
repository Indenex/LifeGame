// The server's own stamina bar (D-182). The client still predicts running, but the server
// tracks the bar from how far the player actually moved, so a client that sprints forever
// is held to the same rhythm of run and walk the bar allows. Running dry only caps pace at
// walking; nothing else is taken (D-081).
using System;
using SpacetimeDB;
using LifeGame.Shared;

public static partial class Module
{
    // Private. The bar, and a recent average of how fast the player is moving: meters and
    // seconds, both fading with time, so a lag spike that delivers several steps at once is
    // measured against the time they cover and never counts as sprinting.
    [SpacetimeDB.Table(Accessor = "StaminaBar")]
    public partial struct StaminaBar
    {
        [PrimaryKey]
        public uint PlayerId;
        public float Stamina;
        public bool Winded;
        public float Rested;
        public long LastMicros;
        public float Meters;
        public float Seconds;
    }

    // Called from Move with the step already clamped to the walking pace. Returns the end
    // point, pulled back toward the start if the player is out of breath and over walking pace.
    static (float x, float z) LimitByStamina(ReducerContext ctx, uint playerId, float fromX, float fromZ, float x, float z)
    {
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        var found = ctx.Db.StaminaBar.PlayerId.Find(playerId);
        var bar = found ?? new StaminaBar { PlayerId = playerId, Stamina = 1f, Rested = Stamina.RefillDelay, LastMicros = now, Seconds = 1f };

        float gap = Math.Clamp((now - bar.LastMicros) / 1_000_000f, 0f, 600f);
        float dt = Math.Min(gap, 1f);
        float fade = MathF.Exp(-dt / Stamina.ServerAverageSeconds);
        float meters = bar.Meters * fade, seconds = bar.Seconds * fade;

        float dx = x - fromX, dz = z - fromZ;
        float dist = MathF.Sqrt(dx * dx + dz * dz);
        if (bar.Winded)
        {
            float room = Math.Max(Stamina.WalkPace * (seconds + dt) - meters, 0f);
            if (dist > room)
            {
                float k = dist > 0f ? room / dist : 0f;
                x = fromX + dx * k;
                z = fromZ + dz * k;
                dist = room;
            }
        }
        meters += dist;
        seconds += dt;

        // Share of the time spent at run pace, from the recent average.
        float pace = meters / Math.Max(seconds, 0.5f);
        float share = Math.Clamp((pace - Stamina.WalkPace) / (Stamina.RunSpeed - Stamina.WalkPace), 0f, 1f);
        int rank = ctx.Db.Player.PlayerId.Find(playerId) is { } p && FindSkill(ctx, p.Identity, Stamina.Skill) is { } sk
            ? Skills.Rank(sk.Points) : 0;
        bool wellFed = ctx.Db.Vitals.PlayerId.Find(playerId) is { } v && v.WellFedUntil > now;
        float s = bar.Stamina;
        // Drain slower than the client does, so honest play is never caught short (the margin).
        s = Stamina.Step(s, true, 0f, rank, wellFed, dt * share * Stamina.ServerDrainFactor);
        float rested = share > 0.05f ? 0f : bar.Rested + gap;
        s = Stamina.Step(s, false, rested, rank, wellFed, dt * (1f - share) + (gap - dt));
        bar.Winded = s <= 0f || (bar.Winded && s < Stamina.MinToRun);
        bar.Stamina = s;
        bar.Rested = rested;
        bar.LastMicros = now;
        bar.Meters = meters;
        bar.Seconds = seconds;
        if (found is not null) ctx.Db.StaminaBar.PlayerId.Update(bar);
        else ctx.Db.StaminaBar.Insert(bar);
        return (x, z);
    }
}
