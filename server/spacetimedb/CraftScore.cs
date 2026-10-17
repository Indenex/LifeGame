using System;
using LifeGame.Shared;
using SpacetimeDB;

// How far a hands-on minigame score is believed (crafting-36, D-182). The minigame is played on the
// client, which then reports a score from 0 to 1, so a modified client could always send 1. The server
// cannot see the play, but it can see how long it took and how often a player is perfect:
//   - the client says when a minigame opens (BeginMinigame); a score arriving sooner than a person could
//     play it, or with no minigame opened, counts at most QuickScoreCap;
//   - a long run of perfect scores slowly settles toward StreakFloor instead of staying at 1.
// Both only nudge (D-081): an honest player is never touched, and nobody is refused. A Master, who
// skips the minigame (Skills.SkipsMinigame), sends Skills.MasterScore and is taken at it.
public static partial class Module
{
    // The latest minigame each player opened, so the time it took can be read when the score comes in.
    [SpacetimeDB.Table(Accessor = "MinigameStart")]
    public partial struct MinigameStart
    {
        [PrimaryKey]
        public uint PlayerId;
        public string ProcessId;
        public Timestamp At;
    }

    // How many perfect scores in a row a player has sent.
    [SpacetimeDB.Table(Accessor = "ScoreStreak")]
    public partial struct ScoreStreak
    {
        [PrimaryKey]
        public uint PlayerId;
        public uint Perfect;
    }

    // The best a score can be when no real play time backs it.
    const float QuickScoreCap = 0.6f;
    // At or above this a score is perfect for the streak.
    const float PerfectScore = 0.97f;
    // Perfect scores in a row before the soft cap starts, where it ends up, and how fast it gets there.
    const uint StreakFree = 4;
    const float StreakFloor = 0.92f, StreakDecay = 0.8f;

    // The client opens a minigame (before it is played).
    [SpacetimeDB.Reducer]
    public static void BeginMinigame(ReducerContext ctx, string processId)
    {
        var player = RequirePlayer(ctx);
        if (Processes.Find(processId) is null) throw new Exception("Unknown process");
        var row = new MinigameStart { PlayerId = player.PlayerId, ProcessId = processId, At = ctx.Timestamp };
        if (ctx.Db.MinigameStart.PlayerId.Find(player.PlayerId) is null) ctx.Db.MinigameStart.Insert(row);
        else ctx.Db.MinigameStart.PlayerId.Update(row);
    }

    // The score to use for what a client reported. kind is the minigame that was played.
    static float TrustScore(ReducerContext ctx, uint playerId, string processId, Minigame kind, float score)
    {
        if (kind == Minigame.None) return score;
        score = Math.Clamp(score, 0f, 1f);
        // A Master skips the minigame and sends the fixed Master score.
        if (Skills.SkipsMinigame(SkillRank(ctx, playerId, processId)) && score <= Skills.MasterScore + 0.001f) return score;

        // A minigame counts once: the record is used up by the score that follows it.
        bool played = false, opened = false;
        if (ctx.Db.MinigameStart.PlayerId.Find(playerId) is { } start)
        {
            opened = true;
            ctx.Db.MinigameStart.PlayerId.Delete(playerId);
            played = start.ProcessId == processId && SecondsBetween(start.At, ctx.Timestamp) >= MinigamePlay.MinSeconds(kind);
        }
        if (!played)
        {
            if (score > QuickScoreCap) NoteSuspicion(ctx, playerId, Suspect.QuickScore);
            if (score > QuickScoreCap) Log.Info($"score capped: player {playerId} {processId} sent {score:F2} with no believable play ({(opened ? "too fast" : "no minigame opened")})");
            score = Math.Min(score, QuickScoreCap);
        }

        // Perfect streaks (judged on what was sent) settle toward a high-but-not-full score.
        uint perfect = ctx.Db.ScoreStreak.PlayerId.Find(playerId)?.Perfect ?? 0;
        perfect = played && score >= PerfectScore ? perfect + 1 : 0;
        var streak = new ScoreStreak { PlayerId = playerId, Perfect = perfect };
        if (ctx.Db.ScoreStreak.PlayerId.Find(playerId) is null) ctx.Db.ScoreStreak.Insert(streak);
        else ctx.Db.ScoreStreak.PlayerId.Update(streak);
        if (perfect > StreakFree)
        {
            NoteSuspicion(ctx, playerId, Suspect.PerfectStreak);
            Log.Info($"score softened: player {playerId} {processId} perfect streak {perfect}");
            score = Math.Min(score, StreakFloor + (1f - StreakFloor) * MathF.Pow(StreakDecay, perfect - StreakFree));
        }
        return score;
    }
}
