using System;
using System.Collections.Generic;
using System.Linq;
using LifeGame.Shared;
using SpacetimeDB;

// Several characters per login (survival-24). A character's key is an Identity value, and
// every table that says "Owner", "Member" or "Holder" holds that key rather than the login.
// A login's first character uses the login's own Identity as its key, so everything that
// existed before characters was already correct and nothing was migrated. Further
// characters get a made-up key that is not any real login's. `Me(ctx)` is the key of the
// character the caller is playing; use it wherever the code used to say ctx.Sender.
public static partial class Module
{
    // One row per character, private; a login reads its own through MyCharacters.
    [SpacetimeDB.Table(Accessor = "Character")]
    public partial struct Character
    {
        [PrimaryKey]
        public Identity Key;
        [SpacetimeDB.Index.BTree]
        public Identity Login;
        public uint PlayerId;
        public Timestamp Created;
        // A copy of Player.Name so the menu can list characters that are not in the world.
        public string Name;
        // The last time this character was in the world (set on entering and leaving); Created if never.
        public Timestamp LastPlayed;
    }

    // Which character a login is playing, or last played. In the world only while that
    // character's Player is online; otherwise the login is at the character menu.
    [SpacetimeDB.Table(Accessor = "ActiveCharacter")]
    public partial struct ActiveCharacter
    {
        [PrimaryKey]
        public Identity Login;
        public Identity Key;
    }

    [SpacetimeDB.View(Accessor = "MyCharacters", Public = true)]
    public static List<Character> MyCharacters(ViewContext ctx) => ctx.Db.Character.Login.Filter(ctx.Sender).ToList();

    // The looks of your characters, for their portraits on the menu.
    [SpacetimeDB.View(Accessor = "MyCharacterLooks", Public = true)]
    public static List<PlayerLook> MyCharacterLooks(ViewContext ctx) =>
        ctx.Db.Character.Login.Filter(ctx.Sender).Select(c => ctx.Db.PlayerLook.PlayerId.Find(c.PlayerId))
            .Where(l => l.HasValue).Select(l => l!.Value).ToList();

    [SpacetimeDB.View(Accessor = "MyActiveCharacter", Public = true)]
    public static List<ActiveCharacter> MyActiveCharacter(ViewContext ctx) =>
        ctx.Db.ActiveCharacter.Login.Find(ctx.Sender) is { } a ? new List<ActiveCharacter> { a } : new List<ActiveCharacter>();

    // The Player row and position of the character the login is on, online or not (a login at
    // the character menu still sees its own name). Replaces "WHERE identity = :sender", which
    // names the login and so finds nobody once a login has a second character.
    [SpacetimeDB.View(Accessor = "MyPlayer", Public = true)]
    public static List<Player> MyPlayer(ViewContext ctx) =>
        ctx.Db.Player.Identity.Find(Me(ctx)) is { } p ? new List<Player> { p } : new List<Player>();

    [SpacetimeDB.View(Accessor = "MyPlayerPos", Public = true)]
    public static List<PlayerPos> MyPlayerPos(ViewContext ctx) =>
        ctx.Db.Player.Identity.Find(Me(ctx)) is { } p && ctx.Db.PlayerPos.PlayerId.Find(p.PlayerId) is { } pos
            ? new List<PlayerPos> { pos } : new List<PlayerPos>();

    // The key of the character the caller plays. A login with no active row is playing its
    // first character, whose key is the login itself.
    static Identity Me(ReducerContext ctx) =>
        ctx.Db.ActiveCharacter.Login.Find(ctx.Sender) is { } a ? a.Key : ctx.Sender;

    static Identity Me(ViewContext ctx) =>
        ctx.Db.ActiveCharacter.Login.Find(ctx.Sender) is { } a ? a.Key : ctx.Sender;

    // Gives a login that predates characters its row for character #1, and an active row.
    static void BackfillCharacter(ReducerContext ctx, Identity login)
    {
        if (ctx.Db.Character.Login.Filter(login).Any()) return;
        if (ctx.Db.Player.Identity.Find(login) is not { } old) return;
        ctx.Db.Character.Insert(new Character { Key = login, Login = login, PlayerId = old.PlayerId, Created = ctx.Timestamp, LastPlayed = ctx.Timestamp, Name = old.Name });
        if (ctx.Db.ActiveCharacter.Login.Find(login) == null)
            ctx.Db.ActiveCharacter.Insert(new ActiveCharacter { Login = login, Key = login });
    }

    static void SetActive(ReducerContext ctx, Identity login, Identity key)
    {
        if (ctx.Db.ActiveCharacter.Login.Find(login) is { } a) ctx.Db.ActiveCharacter.Login.Update(a with { Key = key });
        else ctx.Db.ActiveCharacter.Insert(new ActiveCharacter { Login = login, Key = key });
    }

    // A key no real login has: the front of a login's own Identity is a hash, this is random.
    static Identity NewCharacterKey(ReducerContext ctx)
    {
        for (;;)
        {
            var bytes = new byte[32];
            ctx.Rng.NextBytes(bytes);
            bytes[0] = 0xCC; bytes[1] = 0x01;
            var key = Identity.FromHexString(Convert.ToHexString(bytes).ToLowerInvariant());
            if (ctx.Db.Player.Identity.Find(key) == null && ctx.Db.Character.Key.Find(key) == null) return key;
        }
    }

    // A new character: the Player row and the rows every player has, waking naked near the
    // middle of the seeded area. Not yet in the world (Online false).
    static Player MakeCharacter(ReducerContext ctx, Identity login, Identity key, string name)
    {
        var player = ctx.Db.Player.Insert(new Player { Identity = key, PlayerId = 0, Name = name, Online = false });
        ctx.Db.Character.Insert(new Character { Key = key, Login = login, PlayerId = player.PlayerId, Created = ctx.Timestamp, LastPlayed = ctx.Timestamp, Name = name });
        float x = (float)(ctx.Rng.NextDouble() * 16 - 8);
        float z = (float)(ctx.Rng.NextDouble() * 16 - 8);
        ctx.Db.PlayerPos.Insert(new PlayerPos
        {
            PlayerId = player.PlayerId, X = x, Z = z, Heading = 0,
            ChunkX = (short)World.ChunkOf(x), ChunkZ = (short)World.ChunkOf(z),
        });
        ctx.Db.PlayerState.Insert(new PlayerState { PlayerId = player.PlayerId, LastMoveAt = ctx.Timestamp, LastActionAt = ctx.Timestamp });
        return player;
    }

    // The character steps into the world: marked online, with the survival and health rows.
    static void EnterWorld(ReducerContext ctx, Player player)
    {
        player = player with { Online = true };
        ctx.Db.Player.Identity.Update(player);
        TouchLastPlayed(ctx, player.Identity);
        ForgetSeen(ctx, player.PlayerId);
        if (ctx.Db.PlayerState.PlayerId.Find(player.PlayerId) is { } state)
            ctx.Db.PlayerState.PlayerId.Update(state with { LastMoveAt = ctx.Timestamp });
        SurvivalConnected(ctx, player);
        EnsureHealth(ctx, player.PlayerId);
    }

    static void TouchLastPlayed(ReducerContext ctx, Identity key)
    {
        if (ctx.Db.Character.Key.Find(key) is { } c) ctx.Db.Character.Key.Update(c with { LastPlayed = ctx.Timestamp });
    }

    // The character leaves the world, by disconnecting or going back to the menu.
    static void LeaveWorld(ReducerContext ctx, Player player)
    {
        if (!player.Online) return;
        ctx.Db.Player.Identity.Update(player with { Online = false });
        TouchLastPlayed(ctx, player.Identity);
        ForgetSeen(ctx, player.PlayerId);
        SurvivalDisconnected(ctx, player);
        StanceDisconnected(ctx, player);
    }

    static Player? ActivePlayer(ReducerContext ctx, Identity login) =>
        ctx.Db.ActiveCharacter.Login.Find(login) is { } a ? ctx.Db.Player.Identity.Find(a.Key)
        : ctx.Db.Player.Identity.Find(login);

    static void RequireNameFree(ReducerContext ctx, string name, uint exceptPlayerId)
    {
        string key = Names.Key(name);
        foreach (var other in ctx.Db.Player.Iter())
            if (other.PlayerId != exceptPlayerId && Names.Key(other.Name) == key)
                throw new Exception("That name is taken");
    }

    static void RequireAtMenu(ReducerContext ctx)
    {
        if (ActivePlayer(ctx, ctx.Sender) is { Online: true })
            throw new Exception("Go back to the character menu first");
    }

    // Makes a new character, with a name and a race and look (as SetRaceLook), and steps into the world as them.
    [SpacetimeDB.Reducer]
    public static void CreateCharacter(ReducerContext ctx, string name, byte race, byte a, byte b, byte c, byte d)
    {
        MeterCall(ctx);
        RequireAtMenu(ctx);
        BackfillCharacter(ctx, ctx.Sender);
        if (ctx.Db.Character.Login.Filter(ctx.Sender).Count() >= Characters.MaxPerLogin)
            throw new Exception($"You can have {Characters.MaxPerLogin} characters");
        name = Names.Normalize(name);
        if (Names.Problem(name) is { } problem) throw new Exception(problem);
        if (!LifeGame.Shared.Looks.Valid(race, a, b, c, d)) throw new Exception("That look is not one of the choices.");
        RequireNameFree(ctx, name, 0);
        var player = MakeCharacter(ctx, ctx.Sender, NewCharacterKey(ctx), name);
        SaveLook(ctx, new PlayerLook { PlayerId = player.PlayerId, Skin = a, Hair = b, Cut = c, Beard = d, Race = race });
        SetActive(ctx, ctx.Sender, player.Identity);
        EnterWorld(ctx, player);
    }

    // Plays one of your characters. Only from the menu, so there is no swapping in the world.
    [SpacetimeDB.Reducer]
    public static void SelectCharacter(ReducerContext ctx, Identity key)
    {
        MeterCall(ctx);
        RequireAtMenu(ctx);
        BackfillCharacter(ctx, ctx.Sender);
        var row = ctx.Db.Character.Key.Find(key);
        if (row is not { } c || c.Login != ctx.Sender) throw new Exception("That is not one of your characters");
        var player = ctx.Db.Player.Identity.Find(key) ?? throw new Exception("That character is gone");
        SetActive(ctx, ctx.Sender, key);
        EnterWorld(ctx, player);
    }

    // Back to the character menu.
    [SpacetimeDB.Reducer]
    public static void ReturnToMenu(ReducerContext ctx)
    {
        MeterCall(ctx);
        if (ActivePlayer(ctx, ctx.Sender) is { } p) LeaveWorld(ctx, p);
    }

    // Deletes one of your characters; their name is typed to confirm. What they carried is
    // gone. Claims and what is built weather away by the usual rules, so nothing in the
    // world is erased at once. The town roster, offices and mats go with them.
    [SpacetimeDB.Reducer]
    public static void DeleteCharacter(ReducerContext ctx, Identity key, string typedName)
    {
        MeterCall(ctx);
        RequireAtMenu(ctx);
        BackfillCharacter(ctx, ctx.Sender);
        var row = ctx.Db.Character.Key.Find(key);
        if (row is not { } c || c.Login != ctx.Sender) throw new Exception("That is not one of your characters");
        var player = ctx.Db.Player.Identity.Find(key) ?? throw new Exception("That character is gone");
        if (Names.Key(typedName) != Names.Key(player.Name)) throw new Exception("Type the character's name to delete them");
        RemoveCharacterRows(ctx, player);
        ctx.Db.Character.Key.Delete(key);
        // Left pointing at the deleted character, the login does nothing until it picks another.
    }

    static void RemoveCharacterRows(ReducerContext ctx, Player player)
    {
        var key = player.Identity;
        uint id = player.PlayerId;
        foreach (var row in ctx.Db.TownSigner.Owner.Filter(key).ToList()) DropSigner(ctx, row);
        foreach (var row in ctx.Db.TownInvite.Invitee.Filter(key).ToList()) ctx.Db.TownInvite.Id.Delete(row.Id);
        foreach (var row in ctx.Db.InventoryItem.Owner.Filter(key).ToList()) ctx.Db.InventoryItem.Id.Delete(row.Id);
        foreach (var row in ctx.Db.SkillLevel.Owner.Filter(key).ToList()) ctx.Db.SkillLevel.Id.Delete(row.Id);
        foreach (var row in ctx.Db.HotbarSlot.Owner.Filter(key).ToList()) ctx.Db.HotbarSlot.Id.Delete(row.Id);
        foreach (var row in ctx.Db.JournalEntry.Owner.Filter(key).ToList()) ctx.Db.JournalEntry.Id.Delete(row.Id);
        foreach (var row in ctx.Db.Taught.Teacher.Filter(key).ToList()) ctx.Db.Taught.Id.Delete(row.Id);
        foreach (var row in ctx.Db.WornItem.Owner.Filter(key).ToList()) ctx.Db.WornItem.Id.Delete(row.Id);
        foreach (var row in ctx.Db.ClaimMember.Member.Filter(key).ToList()) ctx.Db.ClaimMember.Id.Delete(row.Id);
        if (ctx.Db.PlayerRest.PlayerId.Find(id) != null) ctx.Db.PlayerRest.PlayerId.Delete(id);
        if (ctx.Db.PlayerEquip.Owner.Find(key) != null) ctx.Db.PlayerEquip.Owner.Delete(key);
        if (ctx.Db.CraftJob.Owner.Find(key) != null) ctx.Db.CraftJob.Owner.Delete(key);
        if (ctx.Db.SleepingMat.PlayerId.Find(id) != null) ctx.Db.SleepingMat.PlayerId.Delete(id);
        if (ctx.Db.Vitals.PlayerId.Find(id) != null) ctx.Db.Vitals.PlayerId.Delete(id);
        if (ctx.Db.Drenched.PlayerId.Find(id) != null) ctx.Db.Drenched.PlayerId.Delete(id);
        if (ctx.Db.PlayerLook.PlayerId.Find(id) != null) ctx.Db.PlayerLook.PlayerId.Delete(id);
        if (ctx.Db.PlayerMark.PlayerId.Find(id) != null) ctx.Db.PlayerMark.PlayerId.Delete(id);
        if (ctx.Db.PlayerHealth.PlayerId.Find(id) != null) ctx.Db.PlayerHealth.PlayerId.Delete(id);
        if (ctx.Db.PlayerStanding.PlayerId.Find(id) != null) ctx.Db.PlayerStanding.PlayerId.Delete(id);
        if (ctx.Db.PlayerJump.PlayerId.Find(id) != null) ctx.Db.PlayerJump.PlayerId.Delete(id);
        if (ctx.Db.PlayerSneaking.PlayerId.Find(id) != null) ctx.Db.PlayerSneaking.PlayerId.Delete(id);
        if (ctx.Db.PlayerSeen.PlayerId.Find(id) != null) ctx.Db.PlayerSeen.PlayerId.Delete(id);
        if (ctx.Db.ChatState.PlayerId.Find(id) != null) ctx.Db.ChatState.PlayerId.Delete(id);
        if (ctx.Db.MoveInput.PlayerId.Find(id) != null) ctx.Db.MoveInput.PlayerId.Delete(id);
        if (ctx.Db.MoveBank.PlayerId.Find(id) != null) ctx.Db.MoveBank.PlayerId.Delete(id);
        if (ctx.Db.StaminaBar.PlayerId.Find(id) != null) ctx.Db.StaminaBar.PlayerId.Delete(id);
        if (ctx.Db.PlayerState.PlayerId.Find(id) != null) ctx.Db.PlayerState.PlayerId.Delete(id);
        if (ctx.Db.PlayerPos.PlayerId.Find(id) != null) ctx.Db.PlayerPos.PlayerId.Delete(id);
        if (ctx.Db.WorldAdmin.Identity.Find(key) != null) ctx.Db.WorldAdmin.Identity.Delete(key);
        ctx.Db.Player.Identity.Delete(key);
    }
}
