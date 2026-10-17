namespace LifeGame;

// The queries the client subscribes to. Plain C# (no Godot) so the bot checks in
// tools/ run exactly what the game runs.
public static class Subscriptions
{
    // Your own character's player row and position, wherever you are (views since survival-24:
    // a login can have several characters). Subscribed first and kept
    // for the whole session, so a player saved far from the origin, or woken on the
    // rim after dying, always gets their own row and the area can follow it.
    public static readonly string[] Self =
    {
        "SELECT * FROM my_player",
        "SELECT * FROM my_player_pos",
        "SELECT * FROM my_active_character",
        "SELECT * FROM my_characters",
        "SELECT * FROM my_character_looks",
        // Only a login's first character matches these (its key is the login). Kept so the bot
        // tools, which only ever play that one, still find their own Player row first.
        "SELECT * FROM player WHERE identity = :sender",
        "SELECT player_pos.* FROM player_pos JOIN player ON player_pos.player_id = player.player_id WHERE player.identity = :sender",
    };

    // How far, in chunks, the slow-changing world state is subscribed: dug cells, felled trees
    // and buildings. The live things (players, animals, items, fires) stay at
    // LifeGame.Shared.World.ViewRadiusChunks. Environment's view distance sets this so far chunks draw as they
    // really are; it never goes below the near radius or above FarRadiusMax.
    public const int FarRadiusMax = 10;
    static int farRadius = LifeGame.Shared.World.ViewRadiusChunks;
    // The clock, weather and seasons, and the crafting work sounds (Atmosphere). One failing query fails the
    // whole subscription, so every table here must be public: the admin table is private since admin-1,
    // which silently stopped the clock, /time and /season reaching every client (bugfix-39).
    public static readonly string[] Atmosphere =
    {
        "SELECT * FROM world_clock",
        "SELECT * FROM weather",
        "SELECT * FROM craft_work",
        "SELECT * FROM season_clock",
        "SELECT * FROM season_state",
    };

    public static int FarRadius
    {
        get => farRadius;
        set => farRadius = System.Math.Clamp(value, LifeGame.Shared.World.ViewRadiusChunks, FarRadiusMax);
    }

    public static string Range(int chunkX, int chunkZ, int r) =>
        $"chunk_x >= {chunkX - r} AND chunk_x <= {chunkX + r} AND chunk_z >= {chunkZ - r} AND chunk_z <= {chunkZ + r}";

    // Everything around one chunk, plus the per-player tables. Lanes add their own
    // chunk queries on top (see SurvivalClient.Queries).
    public static string[] Area(string range) => Area(range, range);

    // `range` for the live things; `farRange` (a wider Range) for the slow-changing world state.
    public static string[] Area(string range, string farRange) => new[]
    {
        "SELECT * FROM player WHERE online = true",
        $"SELECT * FROM player_pos WHERE {range}",
        $"SELECT * FROM world_item WHERE {range}",
        $"SELECT * FROM animal WHERE {range}",
        $"SELECT * FROM animal_carcass WHERE {range}",
        $"SELECT * FROM hide_rack WHERE {range}",
        $"SELECT * FROM terrain_edit WHERE {farRange}",
        $"SELECT * FROM fire WHERE {range}",
        $"SELECT * FROM cold_hearth WHERE {range}",
        $"SELECT * FROM chopped_tree WHERE {farRange}",
        $"SELECT * FROM roast WHERE {range}",
        $"SELECT * FROM clay_piece WHERE {range}",
        $"SELECT * FROM kiln WHERE {range}",
        $"SELECT * FROM simmer_pot WHERE {range}",
        $"SELECT * FROM lamp WHERE {range}",
        "SELECT * FROM trough_water",
        "SELECT * FROM player_mark",
        "SELECT * FROM clay_news",
        "SELECT * FROM my_inventory",
        "SELECT * FROM my_admin",
        // What you wear (animals-12, read by the equipment panel, ui-10).
        "SELECT * FROM my_worn",
        "SELECT * FROM my_hotbar",
        "SELECT * FROM my_craft_job",
        "SELECT * FROM player_sneaking",
        "SELECT * FROM player_look",
        "SELECT * FROM player_jump",
        "SELECT * FROM player_act",
        "SELECT * FROM worn_item",
        $"SELECT * FROM structure WHERE {farRange}",
    };
}
