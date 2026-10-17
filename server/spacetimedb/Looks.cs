using SpacetimeDB;

public static partial class Module
{
    // What each player chose to look like (model-3, D-233). A player with no row
    // wears the default look from their player id (LifeGame.Shared.Looks.Default).
    [SpacetimeDB.Table(Accessor = "PlayerLook", Public = true)]
    public partial struct PlayerLook
    {
        [PrimaryKey]
        public uint PlayerId;
        public byte Skin, Hair, Cut, Beard;
    }

    [SpacetimeDB.Reducer]
    public static void SetLook(ReducerContext ctx, byte skin, byte hair, byte cut, byte beard)
    {
        var player = RequirePlayer(ctx);
        if (!LifeGame.Shared.Looks.Valid(skin, hair, cut, beard))
            throw new System.Exception("That look is not one of the choices.");
        var row = new PlayerLook { PlayerId = player.PlayerId, Skin = skin, Hair = hair, Cut = cut, Beard = beard };
        if (ctx.Db.PlayerLook.PlayerId.Find(player.PlayerId) != null)
            ctx.Db.PlayerLook.PlayerId.Update(row);
        else
            ctx.Db.PlayerLook.Insert(row);
    }
}
