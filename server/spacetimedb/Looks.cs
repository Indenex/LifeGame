using SpacetimeDB;

public static partial class Module
{
    // What each player chose to look like (model-3, D-233). A player with no row
    // wears the default look from their player id (LifeGame.Shared.Looks.Default).
    // Race (model-6) picks the figure; Skin, Hair, Cut and Beard are then that
    // race's four option slots (LifeGame.Shared.Looks.Races).
    [SpacetimeDB.Table(Accessor = "PlayerLook", Public = true)]
    public partial struct PlayerLook
    {
        [PrimaryKey]
        public uint PlayerId;
        public byte Skin, Hair, Cut, Beard;
        [Default((byte)0)]
        public byte Race;
    }

    [SpacetimeDB.Reducer]
    public static void SetLook(ReducerContext ctx, byte skin, byte hair, byte cut, byte beard)
    {
        var player = RequirePlayer(ctx);
        if (!LifeGame.Shared.Looks.Valid(skin, hair, cut, beard))
            throw new System.Exception("That look is not one of the choices.");
        SaveLook(ctx, new PlayerLook { PlayerId = player.PlayerId, Skin = skin, Hair = hair, Cut = cut, Beard = beard, Race = LifeGame.Shared.Looks.Bramble });
    }

    // A race and its four option bytes, checked against the roster.
    [SpacetimeDB.Reducer]
    public static void SetRaceLook(ReducerContext ctx, byte race, byte a, byte b, byte c, byte d)
    {
        var player = RequirePlayer(ctx);
        if (!LifeGame.Shared.Looks.Valid(race, a, b, c, d))
            throw new System.Exception("That look is not one of the choices.");
        SaveLook(ctx, new PlayerLook { PlayerId = player.PlayerId, Skin = a, Hair = b, Cut = c, Beard = d, Race = race });
    }

    static void SaveLook(ReducerContext ctx, PlayerLook row)
    {
        if (ctx.Db.PlayerLook.PlayerId.Find(row.PlayerId) != null)
            ctx.Db.PlayerLook.PlayerId.Update(row);
        else
            ctx.Db.PlayerLook.Insert(row);
    }
}
