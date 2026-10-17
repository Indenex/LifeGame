using LifeGame.Shared;
using SpacetimeDB;

// Emotes (Animation lane, anim-16): a wave, cheer, dance or bow on request. The server
// checks the emote and a short cooldown, then notes it as a PlayerAct so every client
// near enough plays it on that player.
public static partial class Module
{
    // When each player last emoted (private; only for the cooldown).
    [SpacetimeDB.Table(Accessor = "EmoteLast")]
    public partial struct EmoteLast
    {
        [PrimaryKey]
        public uint PlayerId;
        public Timestamp At;
    }

    [SpacetimeDB.Reducer]
    public static void Emote(ReducerContext ctx, byte motion)
    {
        if (!Emotes.IsEmote((WorkMotion)motion)) throw new Exception("No such emote");
        var player = RequirePlayer(ctx);
        var last = ctx.Db.EmoteLast.PlayerId.Find(player.PlayerId);
        if (last is { } l && ctx.Timestamp.MicrosecondsSinceUnixEpoch - l.At.MicrosecondsSinceUnixEpoch < Emotes.CooldownMicros)
            throw new Exception("Too soon");
        var row = new EmoteLast { PlayerId = player.PlayerId, At = ctx.Timestamp };
        if (last is null) ctx.Db.EmoteLast.Insert(row);
        else ctx.Db.EmoteLast.PlayerId.Update(row);
        NoteAct(ctx, player.PlayerId, (WorkMotion)motion);
    }
}
