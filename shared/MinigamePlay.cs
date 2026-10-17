// How long the hands-on minigames take at the least, shared so the server's plausibility check
// (server/spacetimedb/CraftScore.cs) and the check bots (which open a minigame and wait this long
// before reporting a score, like a player) agree. Lenient on purpose.
namespace LifeGame.Shared
{
    public static class MinigamePlay
    {
        public static float MinSeconds(Minigame kind) => kind switch
        {
            Minigame.Drill => 2.0f,
            Minigame.Strike => 1.2f,
            Minigame.Shape => 1.5f,
            Minigame.Coil => 2.0f,
            Minigame.Spin => 1.5f,
            _ => 1.0f,
        };
    }
}
