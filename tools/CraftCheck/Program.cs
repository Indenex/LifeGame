// End-to-end crafting check: one bot connects to a freshly published server,
// walks around gathering sticks, stones and fiber, then runs every tier 0 to 2
// process and checks the results land: tinder, a fire, a flake, a hand axe,
// cordage, a spear and a stone axe. Exits non-zero on the first failure.
//
// Usage: dotnet run -- [uri=http://127.0.0.1:3000] [db=lifegame] [fireX fireZ]
using System.Diagnostics;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

string uri = args.Length > 0 ? args[0] : "http://127.0.0.1:3000";
string db = args.Length > 1 ? args[1] : "lifegame";
// Where to light the test fire; defaults to the edge of the spawn clearing.
float fireX = args.Length > 3 ? float.Parse(args[2]) : 1.5f;
float fireZ = args.Length > 3 ? float.Parse(args[3]) : 1.5f;

Identity? me = null;
uint myId = 0;
bool subscribed = false;
string? lastError = null;
var conn = DbConnection.Builder()
    .WithUri(uri)
    .WithDatabaseName(db)
    .OnConnect((c, identity, _) =>
    {
        me = identity;
        c.SubscriptionBuilder()
            .OnApplied(_ => subscribed = true)
            .Subscribe(new[]
            {
                "SELECT * FROM player",
                "SELECT * FROM player_pos",
                "SELECT * FROM world_item",
                "SELECT * FROM fire",
                "SELECT * FROM cold_hearth",
                "SELECT * FROM my_inventory",
                "SELECT * FROM my_craft_job",
                "SELECT * FROM roast",
                "SELECT * FROM terrain_edit",
                "SELECT * FROM chopped_tree",
                "SELECT * FROM structure",
                "SELECT * FROM claim",
                "SELECT * FROM my_claim_stash",
                "SELECT * FROM craft_work",
                "SELECT * FROM my_skills",
            });
    })
    .OnConnectError(e => Fail($"connect: {e.Message}"))
    .Build();
conn.OnUnhandledReducerError += (_, e) => lastError = e.Message;

Pump(() => subscribed, 20, "subscription");
Pump(() => me is { } id && conn.Db.Player.Identity.Find(id) is not null, 10, "player row");
myId = conn.Db.Player.Identity.Find(me!.Value)!.PlayerId;
var ground = new BotGround(conn);
var start = conn.Db.PlayerPos.PlayerId.Find(myId)!;
float x = start.X, z = start.Z;

// Every run begins from the same place (bugfix-31): new players wake at a random spot in the
// spawn clearing, so walk to the origin first, at a pace the server accepts.
for (var go = Stopwatch.StartNew(); go.Elapsed.TotalSeconds < 60; )
{
    float dx = -x, dz = -z, d = MathF.Sqrt(dx * dx + dz * dz);
    if (d < 0.05f && conn.Db.PlayerPos.PlayerId.Find(myId) is { } at && MathF.Abs(at.X - x) + MathF.Abs(at.Z - z) < 0.1f) break;
    if (d >= 0.05f && conn.Db.PlayerPos.PlayerId.Find(myId) is { } seen0 && MathF.Abs(seen0.X - x) + MathF.Abs(seen0.Z - z) < 1.5f)
    {
        float step = Math.Min(d, 0.5f);
        x += dx / d * step; z += dz / d * step;
    }
    conn.Reducers.Move(x, z, MathF.Atan2(dx, dz));
    Pump(() => false, 0.1f, null);
}

// CRAFTCHECK_PART splits the check in two for CI: "tools" (the tier 0 to 2 crafts, digging, fires, chopping)
// and "stations" (claim, tables, placing, tool wear and repair). Unset runs both, as before.
string part = Environment.GetEnvironmentVariable("CRAFTCHECK_PART") ?? "";
bool runTools = part is "" or "tools", runStations = part is "" or "stations";

if (runTools)
{
    // Needs for every process once, with a spare stone to strike with.
    Gather(Item.Stick, 5);
    Gather(Item.Stone, 3);
    Gather(Item.Fiber, 10);

    Craft("gather.tinder", 0, Item.Tinder);
    Craft("cord.twist", 0, Item.Cordage);
    Craft("cord.twist", 0, Item.Cordage);
    Craft("knap.flake", 0.95f, Item.Flake);
    Craft("knap.hand_axe", 0.95f, Item.HandAxe);
    Craft("lash.spear", 0, Item.Spear);
    Craft("lash.stone_axe", 0, Item.StoneAxe);

    // Crafting-2 tools: a knife and a digging stick (the basket is made at a table, below).
    Gather(Item.Stick, 5);
    Gather(Item.Stone, 3);
    Gather(Item.Fiber, 13);
    Craft("knap.flake", 0.95f, Item.Flake);
    Craft("cord.twist", 0, Item.Cordage);
    Craft("haft.knife", 0, Item.StoneKnife);
    Craft("carve.digging_stick", 0, Item.DiggingStick);

    // A digging stick reaches one level below bare hands, where the clay is finer.
    var (clayX, clayZ) = Enumerable.Range(1, 40)
        .SelectMany(r => Enumerable.Range(-r, 2 * r + 1).SelectMany(o => new[] { (o, -r), (o, r), (-r, o), (r, o) }))
        .Select(c => (cx: World.CellOf(x) + c.Item1, cz: World.CellOf(z) + c.Item2))
        .First(c => World.IsClay(c.cx, c.cz) && World.Height(c.cx, c.cz, 0) > Needs.WaterLevel + 0.4f
            && conn.Db.TerrainEdit.CellKey.Find(World.CellKey(c.cx, c.cz)) is null);
    WalkTo(clayX * World.CellSize, clayZ * World.CellSize);
    for (int dig = 0; dig <= World.MaxDigDepth; dig++)
    {
        uint clayBefore = CountOf(Item.Clay);
        lastError = null;
        conn.Reducers.Dig(clayX, clayZ);
        Pump(() => lastError != null || CountOf(Item.Clay) > clayBefore, 5, "clay from digging");
        if (lastError != null) Fail($"dig {dig + 1}: {lastError}");
        Pump(() => false, World.DigCooldownMicros / 1_000_000f + 0.1f, null);
    }
    var deepClay = conn.Db.MyInventory.Iter().Where(r => r.Item == (ushort)Item.Clay).Max(r => r.Quality);
    if (deepClay < 80) Fail($"the deepest clay came up at quality {deepClay}");
    lastError = null;
    conn.Reducers.Dig(clayX, clayZ);
    Pump(() => lastError != null, 3, "a dig past the stick's reach to be refused");
    Console.WriteLine($"Digging stick reached level {World.MaxDigDepth + 1} (clay quality {deepClay}); below that: {lastError}");

    Gather(Item.Stick, 2);
    WalkTo(fireX, fireZ);
    int firesBefore = conn.Db.Fire.Count;
    FireCraft("fire.hand_drill", 1f);
    Pump(() => conn.Db.Fire.Count > firesBefore, 5, "a fire to be lit");
    var fire = conn.Db.Fire.Iter().First(f => f.LitBy == myId);
    Console.WriteLine($"Fire lit at ({fire.X:F1}, {fire.Z:F1})");
    OffRocks(fire);

    // A failed drill loses its inputs and lights nothing.
    Gather(Item.Fiber, 2);
    Gather(Item.Stick, 3);
    Craft("gather.tinder", 0, Item.Tinder);
    int firesNow = conn.Db.Fire.Count;
    FireCraft("fire.hand_drill", 0.1f);
    Pump(() => false, 1, null);
    if (conn.Db.Fire.Count != firesNow) Fail("a failed drill still lit a fire");
    if (CountOf(Item.Tinder) != 0) Fail("a failed drill kept its tinder");
    Console.WriteLine("Failed drill lit nothing and used up its tinder");

    // Crafting-3: a bow drill lights a fire on a score that fails a hand drill.
    Gather(Item.Fiber, 5);
    Gather(Item.Stick, 4);
    Craft("cord.twist", 0, Item.Cordage);
    Craft("lash.bow_drill", 0, Item.BowDrill);
    Craft("gather.tinder", 0, Item.Tinder);
    int firesBeforeBow = conn.Db.Fire.Count;
    FireCraft("fire.bow_drill", 0.25f);
    Pump(() => conn.Db.Fire.Count > firesBeforeBow, 5, "a bow-drill fire to be lit");
    if (CountOf(Item.BowDrill) != 1) Fail("the bow drill was used up");
    Console.WriteLine("Bow drill lit a fire at score 0.25 and was kept");
    foreach (var f in conn.Db.Fire.Iter()) OffRocks(f);
    Gather(Item.Stick, 1);

    // A fire burns no higher than the cap (D-090): once it is full, feeding is refused and the stick is
    // kept. The cap grows with Firecraft (the shared progression curve), so a freshly lit fire may still
    // have room for a stick; top it up first instead of assuming rank 0.
    var bowFire = conn.Db.Fire.Iter().OrderByDescending(f => f.LitAt.MicrosecondsSinceUnixEpoch).First();
    WalkTo(bowFire.X, bowFire.Z);
    for (int top = 0; top < 12; top++)
    {
        long fuelNow = conn.Db.Fire.Id.Find(bowFire.Id)!.FuelUntil.MicrosecondsSinceUnixEpoch;
        lastError = null;
        conn.Reducers.FeedFire(bowFire.Id, (ushort)Item.Stick);
        Pump(() => lastError != null || conn.Db.Fire.Id.Find(bowFire.Id)?.FuelUntil.MicrosecondsSinceUnixEpoch > fuelNow, 5, "the bow-drill fire to take a stick or refuse it");
        if (lastError == "The fire is burning as high as it will") break;
        if (lastError != null) Fail($"topping up the bow-drill fire: {lastError}");
        Gather(Item.Stick, 1);
        WalkTo(bowFire.X, bowFire.Z);
    }
    RefusedFeed(bowFire, Item.Stick);

    // Chopping: the stone axe takes logs from the nearest standing tree until it falls.
    var (treeX, treeZ) = NearestTree();
    var trunk = Trees.Trunk(treeX, treeZ);
    WalkTo(trunk.X, trunk.Z);
    int chops = Trees.Felling ? Trees.LogsPerTree : 1;
    for (int i = 0; i < chops; i++) Craft("chop.log", 0, Item.Log);
    if (Trees.Felling)
    {
        Pump(() => conn.Db.ChoppedTree.CellKey.Find(World.CellKey(treeX, treeZ)) is { Felled: true }, 3, "the tree to fall");
        Console.WriteLine($"Felled the tree at {treeX}, {treeZ} after {chops} logs");
    }
    WalkTo(bowFire.X, bowFire.Z);
    RefusedFeed(bowFire, Item.Log);

    // The first fire has burned a while; once a whole stick fits under the cap it is taken (D-121).
    // Firecraft only raises the cap, so the rank-0 cap is a safe bound. By here the fire has had the bow drill, the
    // chopping and the refused logs to burn down, so this rarely waits (it used to stand still for a minute; integration-10).
    WalkTo(fire.X, fire.Z);
    float FirstFireLeft() => (conn.Db.Fire.Id.Find(fire.Id)!.FuelUntil.MicrosecondsSinceUnixEpoch - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000) / 1_000_000f;
    float roomNeeded = Items.Def(Item.Stick).FuelSeconds + 2;
    Pump(() => FirstFireLeft() < Fires.MaxSeconds - roomNeeded, 150, "room for a stick on the first fire");
    long fuelBefore = conn.Db.Fire.Id.Find(fire.Id)!.FuelUntil.MicrosecondsSinceUnixEpoch;
    lastError = null;
    conn.Reducers.FeedFire(fire.Id, (ushort)Item.Stick);
    Pump(() => lastError != null || conn.Db.Fire.Id.Find(fire.Id)?.FuelUntil.MicrosecondsSinceUnixEpoch > fuelBefore, 5, "fuel to be added");
    if (lastError != null) Fail($"feeding the fire: {lastError}");
    Console.WriteLine("Fed the fire a stick");

}

if (part == "stations")
{
    // Part two starts from nothing: it makes the tools the stations and repairs need, and one log.
    // Just what these need (a spare stone to strike with, a flake to cut with): a heavy bag walks slowly.
    Gather(Item.Stick, 2);
    Gather(Item.Stone, 4);
    Gather(Item.Fiber, 3);
    Craft("cord.twist", 0, Item.Cordage);
    Craft("knap.flake", 0.95f, Item.Flake);
    Craft("knap.hand_axe", 0.95f, Item.HandAxe);
    Craft("lash.stone_axe", 0, Item.StoneAxe);
    Craft("knap.flake", 0.95f, Item.Flake);
    Craft("carve.digging_stick", 0, Item.DiggingStick);
    var (stX, stZ) = NearestTree();
    var stTrunk = Trees.Trunk(stX, stZ);
    WalkTo(stTrunk.X, stTrunk.Z);
    Craft("chop.log", 0, Item.Log);
}

if (runStations)
{
    // Crafting-15: stations (D-098, D-112). A basket is woven at a crafting table, which
    // stands inside a claim, and part of its fiber comes from the claim's stash.
    lastError = null;
    conn.Reducers.StartCraft("weave.basket", 0);
    Pump(() => lastError != null, 5, "a basket away from a table to be refused");
    Console.WriteLine($"Basket with no table: {lastError}");
    lastError = null;
    // Only basic survival buildings stand outside a claim (building-13): a station that isn't the table is refused.
    conn.Reducers.PlaceStructure(StructureKind.SpindleStation, x + 2f, z, 0f);
    Pump(() => lastError != null, 5, "a spindle station outside a claim to be refused");
    if (lastError?.Contains("inside your claim") != true) Fail($"a spindle station outside a claim: {lastError}");
    Console.WriteLine($"Spindle station outside a claim: {lastError}");
    Gather(Item.Fiber, 6);
    Craft("cord.twist", 0, Item.Cordage);
    Craft("cord.twist", 0, Item.Cordage);
    Gather(Item.Stick, 11);
    Gather(Item.Fiber, 12);
    if (CountOf(Item.Log) < 1) Fail("no log left for the table");

    // Claim land out past the spawn clearing, then build the table inside it.
    float claimDist = Social.SpawnClearRadius + Social.ClaimRadius + 5f;
    float ang = MathF.Atan2(z, x);
    WalkTo(MathF.Cos(ang) * claimDist, MathF.Sin(ang) * claimDist);
    var marker = BuildHere(StructureKind.ClaimMarker);
    Pump(() => conn.Db.Claim.Iter().Any(c => c.StructureId == marker.Id), 5, "the claim");
    var claim = conn.Db.Claim.Iter().First(c => c.StructureId == marker.Id);
    Console.WriteLine($"Claimed land at ({claim.X:F0}, {claim.Z:F0})");
    var table = BuildHere(StructureKind.CraftingTable);
    Console.WriteLine($"Built a crafting table at ({table.X:F1}, {table.Z:F1})");
    WalkTo(table.X, table.Z);

    // Stash all but 6 fiber; the basket needs 10, so 4 must come from the stash.
    WalkTo(marker.X, marker.Z);
    while (CountOf(Item.Fiber) > 6)
    {
        var fiber = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.Fiber);
        uint had = CountOf(Item.Fiber);
        lastError = null;
        conn.Reducers.StashItem(claim.Id, fiber.Item, fiber.Quality, 1);
        Pump(() => lastError != null || CountOf(Item.Fiber) < had, 5, "fiber into the stash");
        if (lastError != null) Fail($"stashing fiber: {lastError}");
    }
    uint StashFiber() => conn.Db.MyClaimStash.Iter().Where(r => r.ClaimId == claim.Id && r.Item == (ushort)Item.Fiber).Aggregate(0u, (n, r) => n + r.Count);
    uint stashed = StashFiber();
    WalkTo(table.X, table.Z);
    Craft("weave.basket", 0, Item.Basket);
    Pump(() => StashFiber() == stashed - 4, 3, "4 fiber to leave the stash");
    Console.WriteLine($"Wove a basket at the table; 4 fiber came from the stash ({stashed} -> {StashFiber()})");

    // Crafting-21: a made thing is set down where the player chooses. The basket just woven
    // is placeable; a stick is not; the spot has to be within reach.
    {
        Gather(Item.Stick, 1);
        WalkTo(table.X, table.Z);
        var basket = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.Basket);
        var stickRow = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.Stick);
        lastError = null;
        conn.Reducers.PlaceItem(stickRow.Item, stickRow.Quality, stickRow.Wear, x + 1f, z, 0f);
        Pump(() => lastError != null, 5, "placing a stick to be refused");
        if (lastError != "That can't be set down") Fail($"placing a stick: {lastError}");
        lastError = null;
        conn.Reducers.PlaceItem(basket.Item, basket.Quality, basket.Wear, x + 30f, z, 0f);
        Pump(() => lastError != null, 5, "placing out of reach to be refused");
        Console.WriteLine($"Placing out of reach: {lastError}");

        bool placedOk = false;
        for (int k = 0; k < 12 && !placedOk; k++)
        {
            float a = k * MathF.Tau / 12, px = x + MathF.Cos(a) * 1.8f, pz = z + MathF.Sin(a) * 1.8f;
            int before = conn.Db.WorldItem.Count;
            lastError = null;
            conn.Reducers.PlaceItem(basket.Item, basket.Quality, basket.Wear, px, pz, 1.25f);
            Pump(() => lastError != null || conn.Db.WorldItem.Count > before, 5, "the basket to be placed");
            if (lastError == null) placedOk = true;
            else if (!lastError.Contains("steep") && !lastError.Contains("way") && !lastError.Contains("water")) Fail($"placing the basket: {lastError}");
        }
        if (!placedOk) Fail("no spot beside the table took a basket");
        var placed = conn.Db.WorldItem.Iter().First(w => w.Item == (ushort)Item.Basket && w.Placed);
        if (MathF.Abs(placed.Yaw - 1.25f) > 0.001f) Fail($"the placed basket lost its turn ({placed.Yaw})");
        if (CountOf(Item.Basket) != 0) Fail("the basket stayed in the bag");
        WalkTo(placed.X, placed.Z);
        conn.Reducers.PickUp(placed.Id);
        Pump(() => CountOf(Item.Basket) == 1, 5, "the placed basket to be picked back up");
        Console.WriteLine($"Placed the basket at ({placed.X:F1}, {placed.Z:F1}) turned {placed.Yaw:F2} and picked it up again");
    }

    // Crafting-19: tool wear and repair (D-155). The server counts each use as one use of
    // the tool's life; the admin raises that so a single use spends a tool.
    {
        // A repair table, built like the crafting table: 4 sticks, 2 cordage, a log and 2 stones.
        Gather(Item.Stick, 6);
        Gather(Item.Stone, 3);
        Gather(Item.Fiber, 12);
        while (CountOf(Item.Cordage) < 3) Craft("cord.twist", 0, null);
        if (CountOf(Item.Log) < 1)
        {
            var (lx, lz) = NearestTree();
            var tr = Trees.Trunk(lx, lz);
            WalkTo(tr.X, tr.Z);
            Craft("chop.log", 0, Item.Log);
            WalkTo(table.X, table.Z);
        }
        WalkTo(table.X, table.Z);
        var repairTable = BuildHere(StructureKind.RepairTable);
        Console.WriteLine($"Built a repair table at ({repairTable.X:F1}, {repairTable.Z:F1})");

        WalkTo(repairTable.X, repairTable.Z);

        // One use spends the axe.
        AdminCall("set_tool_wear_step", "1000");
        var (wx, wz) = NearestTree();
        var wtrunk = Trees.Trunk(wx, wz);
        WalkTo(wtrunk.X, wtrunk.Z);
        Craft("chop.log", 0, Item.Log);
        var spentAxe = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.StoneAxe);
        if (!Durability.IsSpent(spentAxe.Quality, spentAxe.Wear)) Fail($"one hard use did not spend the axe (wear {spentAxe.Wear})");
        Console.WriteLine($"The axe is {Durability.Condition(spentAxe.Quality, spentAxe.Wear)} after one use");
        lastError = null;
        conn.Reducers.StartCraft("chop.log", 0);
        Pump(() => lastError != null, 5, "a spent axe to be refused");
        if (lastError?.Contains("spent") != true) Fail($"chopping with a spent axe: {lastError}");
        Console.WriteLine($"Spent axe: {lastError}");

        // Away from the table, mending is refused.
        if (MathF.Sqrt((x - repairTable.X) * (x - repairTable.X) + (z - repairTable.Z) * (z - repairTable.Z)) > 8f)
        {
            lastError = null;
            conn.Reducers.RepairTool(spentAxe.Item, spentAxe.Quality, spentAxe.Wear);
            Pump(() => lastError != null, 5, "mending away from the table to be refused");
            if (lastError?.Contains("repair table") != true) Fail($"mending away from the table: {lastError}");
            Console.WriteLine($"Mending away from the table: {lastError}");
        }

        // Wear travels with the tool into the claim's stash and back.
        WalkTo(marker.X, marker.Z);
        lastError = null;
        conn.Reducers.StashItem(claim.Id, spentAxe.Item, spentAxe.Quality, 1);
        Pump(() => lastError != null || CountOf(Item.StoneAxe) == 0, 5, "the axe into the stash");
        if (lastError != null) Fail($"stashing the axe: {lastError}");
        var stashedAxe = conn.Db.MyClaimStash.Iter().First(r => r.ClaimId == claim.Id && r.Item == (ushort)Item.StoneAxe);
        if (stashedAxe.Wear != spentAxe.Wear) Fail($"the stash lost the axe's wear ({spentAxe.Wear} -> {stashedAxe.Wear})");
        conn.Reducers.TakeFromStash(claim.Id, stashedAxe.Item, stashedAxe.Quality, 1);
        Pump(() => CountOf(Item.StoneAxe) == 1, 5, "the axe back out of the stash");
        var backAxe = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.StoneAxe);
        if (backAxe.Wear != spentAxe.Wear) Fail($"the axe came back with different wear ({spentAxe.Wear} -> {backAxe.Wear})");
        Console.WriteLine("The axe kept its wear through the stash");

        // Spend the digging stick too, then show a spent stick no longer reaches the deeper level.
        var stick = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.DiggingStick);
        var (dx, dz) = (World.CellOf(x) + 1, World.CellOf(z) + 1);
        WalkTo(dx * World.CellSize, dz * World.CellSize);
        for (int dig = 0; dig <= World.MaxDigDepth; dig++)
        {
            lastError = null;
            conn.Reducers.Dig(dx, dz);
            Pump(() => lastError != null || (conn.Db.TerrainEdit.CellKey.Find(World.CellKey(dx, dz))?.Depth ?? 0) > dig, 5, "a dig");
            if (lastError != null && dig < World.MaxDigDepth) Fail($"dig {dig + 1}: {lastError}");
            Pump(() => false, World.DigCooldownMicros / 1_000_000f + 0.1f, null);
        }
        var spentStick = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.DiggingStick);
        if (!Durability.IsSpent(spentStick.Quality, spentStick.Wear)) Fail("the digging stick did not wear out");
        lastError = null;
        conn.Reducers.Dig(dx, dz);
        Pump(() => lastError != null, 5, "a spent stick to be left out of a deep dig");
        if (lastError != "Too deep to dig by hand") Fail($"a deep dig with a spent stick: {lastError}");
        Console.WriteLine($"Spent digging stick: {lastError}");

        // Mend both at the table. The axe takes a length of cordage; the stick just a cutting tool.
        AdminCall("set_tool_wear_step", "1");
        WalkTo(repairTable.X, repairTable.Z);
        uint cordageBefore = CountOf(Item.Cordage);
        lastError = null;
        var oldAxe = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.StoneAxe);
        int toolmakingBefore = conn.Db.MySkills.Iter().Where(r => r.Spec == "toolmaking").Sum(r => (int)r.Points);
        conn.Reducers.RepairTool(oldAxe.Item, oldAxe.Quality, oldAxe.Wear);
        Pump(() => lastError != null || conn.Db.MyInventory.Iter().Any(r => r.Item == (ushort)Item.StoneAxe && r.Wear != oldAxe.Wear), 5, "the axe to be mended");
        if (lastError != null) Fail($"mending the axe: {lastError}");
        var mendedAxe = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.StoneAxe);
        byte expectQ = Durability.RepairedQuality(oldAxe.Quality, oldAxe.Wear);
        if (Durability.IsSpent(mendedAxe.Quality, mendedAxe.Wear) || Durability.Used(mendedAxe.Wear) != 0) Fail("the mended axe still shows wear");
        if (mendedAxe.Quality != expectQ || mendedAxe.Quality > oldAxe.Quality) Fail($"mended axe quality {mendedAxe.Quality}, expected {expectQ} (was {oldAxe.Quality})");
        if (Durability.Origin(mendedAxe.Wear) != oldAxe.Quality) Fail($"the mended axe forgot its original quality ({Durability.Origin(mendedAxe.Wear)} vs {oldAxe.Quality})");
        if (CountOf(Item.Cordage) != cordageBefore - 1) Fail($"mending the axe took {cordageBefore - CountOf(Item.Cordage)} cordage, not 1");
        Console.WriteLine($"Mended the axe: quality {oldAxe.Quality} -> {mendedAxe.Quality}, 1 cordage");
        Pump(() => conn.Db.MySkills.Iter().Where(r => r.Spec == "toolmaking").Sum(r => (int)r.Points) > toolmakingBefore, 5, "mending to train Toolmaking");
        Console.WriteLine("Mending trained Toolmaking");

        uint cordageNow = CountOf(Item.Cordage);
        lastError = null;
        conn.Reducers.RepairTool(spentStick.Item, spentStick.Quality, spentStick.Wear);
        Pump(() => lastError != null || conn.Db.MyInventory.Iter().Any(r => r.Item == (ushort)Item.DiggingStick && r.Wear != spentStick.Wear), 5, "the digging stick to be mended");
        if (lastError != null) Fail($"mending the digging stick: {lastError}");
        if (CountOf(Item.Cordage) != cordageNow) Fail("sharpening a digging stick used cordage");
        Console.WriteLine("Sharpened the digging stick (no materials)");

        // As good as it can be, and the mended axe chops again.
        var freshStick = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.DiggingStick);
        lastError = null;
        conn.Reducers.RepairTool(freshStick.Item, freshStick.Quality, freshStick.Wear);
        Pump(() => lastError != null, 5, "mending a fresh stick to be refused");
        var (cx2, cz2) = NearestTree();
        var ctrunk = Trees.Trunk(cx2, cz2);
        WalkTo(ctrunk.X, ctrunk.Z);
        Craft("chop.log", 0, Item.Log);
        Console.WriteLine("The mended axe chops again");
    }

    // Crafting-20: a bow and arrows (animals-15) are made at the bench by hand: the bow from
    // sticks and cordage, three arrows from sticks and fiber, both with a cutting tool.
    {
        Gather(Item.Stick, 5);
        Gather(Item.Fiber, 8);
        while (CountOf(Item.Cordage) < 2) Craft("cord.twist", 0, null);
        Craft("lash.bow", 0, Item.Bow);
        uint arrowsBefore = CountOf(Item.Arrow);
        Craft("carve.arrows", 0, Item.Arrow);
        if (CountOf(Item.Arrow) != arrowsBefore + 3) Fail($"one arrow craft made {CountOf(Item.Arrow) - arrowsBefore} arrows, not 3");
        Console.WriteLine("Made a bow and three arrows");
    }

    // Crafting-26: a spade (a log, a stone and cordage, carved with a cutting tool) digs
// faster than a digging stick. A second dig half a stick-cooldown after the first lands
// with the spade in the bag; it would be refused with the stick alone.
{
    Gather(Item.Stone, 1);
    while (CountOf(Item.Cordage) < 1) Craft("cord.twist", 0, null);
    if (CountOf(Item.Log) < 1)
    {
        var (lx, lz) = NearestTree();
        var tr = Trees.Trunk(lx, lz);
        WalkTo(tr.X, tr.Z);
        Craft("chop.log", 0, Item.Log);
    }
    Gather(Item.Fiber, 2);
    Craft("carve.spade", 0, Item.Spade);
    var (sx, sz) = (World.CellOf(x) + 3, World.CellOf(z) + 3);
    WalkTo(sx * World.CellSize, sz * World.CellSize);
    Pump(() => false, World.DigCooldownMicros / 1_000_000f + 0.1f, null);
    lastError = null;
    conn.Reducers.Dig(sx, sz);
    Pump(() => (conn.Db.TerrainEdit.CellKey.Find(World.CellKey(sx, sz))?.Depth ?? 0) >= 1 || lastError != null, 5, "the first spade dig");
    if (lastError != null) Fail($"first spade dig: {lastError}");
    Pump(() => false, World.DigCooldownMicros / 1_000_000f * 0.85f, null);
    conn.Reducers.Dig(sx, sz);
    Pump(() => (conn.Db.TerrainEdit.CellKey.Find(World.CellKey(sx, sz))?.Depth ?? 0) >= 2 || lastError != null, 5, "the quick second dig");
    if (lastError != null || (conn.Db.TerrainEdit.CellKey.Find(World.CellKey(sx, sz))?.Depth ?? 0) < 2)
        Fail($"the spade did not dig faster than a stick ({lastError})");
    Console.WriteLine("A spade digs faster than a stick");
}

// Roasting needs raw mutton, which only Animals hands out. Set CRAFTCHECK_ROAST=1
    // and put some in this player's inventory (the identity is printed) to check it.
    if (Environment.GetEnvironmentVariable("CRAFTCHECK_ROAST") == "1")
    {
        Console.WriteLine($"Waiting for raw mutton in the inventory of {me}");
        Pump(() => CountOf(Item.RawMutton) > 0, 60, "raw mutton");
        Craft("cook.meat", 0, null);
        Pump(() => conn.Db.Roast.Iter().Any(r => r.Owner == me), 3, "the roast to go on the fire");
        var roast = conn.Db.Roast.Iter().First(r => r.Owner == me);
        lastError = null;
        conn.Reducers.TakeOffFire(roast.Id);
        Pump(() => lastError != null, 3, "taking it off too early to be refused");
        Console.WriteLine($"Too early: {lastError}");
        Pump(() => false, Roasting.BestSeconds, null);
        lastError = null;
        conn.Reducers.TakeOffFire(roast.Id);
        Pump(() => lastError != null || CountOf(Item.CookedMutton) > 0, 5, "roast mutton");
        if (lastError != null) Fail($"taking the roast off: {lastError}");
        Console.WriteLine($"Roasted mutton (quality {conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.CookedMutton).Quality})");
    }

}

Console.WriteLine("Inventory:");
foreach (var row in conn.Db.MyInventory.Iter().OrderBy(r => r.Item))
    Console.WriteLine($"  {Items.Def((Item)row.Item).Count(row.Count)} x{row.Count} (quality {row.Quality})");
Console.WriteLine("Crafting check passed");
conn.Disconnect();
return 0;

// Lights a fire (or tries to). The server sets a fire only on clear, level ground
// near the crafter, away from other fires, hearths, boulders and trunks (Hearths.cs);
// when it finds none here, step a few metres along and try again.
void FireCraft(string processId, float score)
{
    for (int attempt = 0; ; attempt++)
    {
        lastError = null;
        conn.Reducers.StartCraft(processId, score);
        Pump(() => lastError != null || conn.Db.MyCraftJob.Count > 0, 5, $"{processId} to start");
        if (lastError == null) break;
        if (!lastError.Contains("clear, level ground") || attempt >= 8)
            Fail($"{processId} at ({x:F1}, {z:F1}), try {attempt + 1}: {lastError}");
        float a = attempt * MathF.PI / 4f;
        Console.WriteLine($"No ground for a fire at ({x:F1}, {z:F1}); stepping along");
        WalkTo(x + MathF.Cos(a) * 4f, z + MathF.Sin(a) * 4f);
    }
    var def = Processes.Find(processId)!;
    Pump(() => conn.Db.MyCraftJob.Count == 0, def.Seconds * 1.2f + 5, $"{processId} to finish");
}

// Lays out a structure on clear, level ground near the bot and builds it from the bag.
// Just past the server's keep-out for claims (Claims.CheckStructureSite).
const float ClaimKeepOut = Social.SpawnClearRadius + Social.ClaimRadius + 0.5f;

Structure BuildHere(StructureKind kind)
{
    float H(float px, float pz) => Structures.GroundAt(px, pz, (i, j) => conn.Db.TerrainEdit.CellKey.Find(World.CellKey(i, j))?.Depth ?? 0);
    (float X, float Z)? site = null;
    for (int r = 3; r < 30 && site == null; r++)
        for (int k = 0; k < 16 && site == null; k++)
        {
            float a = k * MathF.Tau / 16, sx = x + MathF.Cos(a) * r, sz = z + MathF.Sin(a) * r;
            if (Structures.GroundProblem((int)kind, sx, sz, 0f, H) != null) continue;
            // The server refuses claims in the spawn clearing; the search must not drift back into it.
            if (kind == StructureKind.ClaimMarker && sx * sx + sz * sz < ClaimKeepOut * ClaimKeepOut) continue;
            if (conn.Db.Structure.Iter().Any(o => Structures.TooClose((int)kind, sx, sz, (int)o.Kind, o.X, o.Z))) continue;
            if (Trees.GrowsAt(World.CellOf(sx), World.CellOf(sz)) || Rocks.LiesAt(World.CellOf(sx), World.CellOf(sz))) continue;
            site = (sx, sz);
        }
    if (site is not { } at) { Fail($"no ground for a {kind}"); return null!; }
    WalkTo(at.X, at.Z + 2f);
    int before = conn.Db.Structure.Count;
    lastError = null;
    conn.Reducers.PlaceStructure(kind, at.X, at.Z, 0f);
    Pump(() => lastError != null || conn.Db.Structure.Count > before, 5, $"the {kind} to be laid out");
    if (lastError != null) Fail($"laying out a {kind}: {lastError}");
    var s = conn.Db.Structure.Iter().Where(o => o.Owner == me).OrderByDescending(o => o.PlacedAt.MicrosecondsSinceUnixEpoch).First();
    var sw = Stopwatch.StartNew();
    while (conn.Db.Structure.Id.Find(s.Id) is { Complete: false })
    {
        if (sw.Elapsed.TotalSeconds > 60) Fail($"building the {kind}: {lastError}");
        lastError = null;
        conn.Reducers.Build(s.Id);
        Pump(() => false, 0.35f, null);
        // WalkTo stops up to 1.5 m short of its target, so approaching the site from the far
        // side can leave the bot just outside the work reach: step up to the building and go on.
        if (lastError != null && lastError.Contains("Out of reach")) WalkTo(s.X, s.Z);
    }
    return conn.Db.Structure.Id.Find(s.Id)!;
}

void Craft(string processId, float score, Item? expect)
{
    uint before = expect is { } e ? CountOf(e) : 0;
    lastError = null;
    conn.Reducers.StartCraft(processId, score);
    Pump(() => lastError != null || conn.Db.MyCraftJob.Count > 0, 5, $"{processId} to start");
    if (lastError != null) Fail($"{processId}: {lastError}");
    var def = Processes.Find(processId)!;
    // Other players see the tool in hand (CraftWork.Tool, for Animation): it is the one a tool craft used.
    if (def.NeedsTool != ItemTags.None)
    {
        Pump(() => conn.Db.CraftWork.PlayerId.Find(myId) != null || conn.Db.MyCraftJob.Count == 0, 3, null);
        if (conn.Db.CraftWork.PlayerId.Find(myId) is { Tool: 0 })
            Fail($"{processId} needs a tool but its work row names none");
    }
    Pump(() => conn.Db.MyCraftJob.Count == 0, def.Seconds * 1.2f + 5, $"{processId} to finish");
    if (expect is { } item)
    {
        Pump(() => CountOf(item) > before, 3, $"{processId} output");
        var best = conn.Db.MyInventory.Iter().Where(r => r.Item == (ushort)item).Max(r => r.Quality);
        Console.WriteLine($"{processId}: made {Items.Def(item).Name} (quality {best})");
    }
}

// A fire never sits on a boulder: its stone ring stays clear of every one nearby.
void OffRocks(Fire f)
{
    for (int i = World.CellOf(f.X) - 2; i <= World.CellOf(f.X) + 2; i++)
        for (int j = World.CellOf(f.Z) - 2; j <= World.CellOf(f.Z) + 2; j++)
            // A building clears the ground under it, so a hut's hearth can stand where a boulder was.
            if (Rocks.LiesAt(i, j) && conn.Db.TerrainEdit.CellKey.Find(World.CellKey(i, j)) is null
                && !conn.Db.Structure.Iter().Any(o => Structures.Contains((int)o.Kind, o.X, o.Z, o.Yaw, i, j, 0.4f)))
            {
                var (rx, rz) = Rocks.Center(i, j);
                float d = MathF.Sqrt((rx - f.X) * (rx - f.X) + (rz - f.Z) * (rz - f.Z));
                if (d < Rocks.Radius(i, j) + Fires.RingRadius) Fail($"a fire at ({f.X:F1}, {f.Z:F1}) sits on the boulder at ({rx:F1}, {rz:F1})");
            }
}

// Feeding a full fire is refused with D-090's message, and the fuel stays in the bag.
void RefusedFeed(Fire target, Item fuel)
{
    var before = CountOf(fuel);
    lastError = null;
    conn.Reducers.FeedFire(target.Id, (ushort)fuel);
    Pump(() => lastError != null, 5, $"a full fire to refuse {fuel}");
    if (lastError != "The fire is burning as high as it will") Fail($"feeding a full fire {fuel}: {lastError}");
    Pump(() => false, 0.5f, null);
    if (CountOf(fuel) != before) Fail($"a full fire ate {fuel} ({before} -> {CountOf(fuel)})");
    Console.WriteLine($"A full fire refused {fuel} and it was kept");
}

// The standing tree nearest the bot, by the same rules the server uses.
(int X, int Z) NearestTree()
{
    int cx = (int)MathF.Round(x), cz = (int)MathF.Round(z);
    for (int r = 0; r < 60; r++)
        for (int i = cx - r; i <= cx + r; i++)
            for (int j = cz - r; j <= cz + r; j++)
            {
                if (Math.Max(Math.Abs(i - cx), Math.Abs(j - cz)) != r || !Trees.GrowsAt(i, j)) continue;
                long key = World.CellKey(i, j);
                if (conn.Db.TerrainEdit.CellKey.Find(key) != null || conn.Db.ChoppedTree.CellKey.Find(key) != null) continue;
                return (i, j);
            }
    Fail("no tree within 60 m");
    return (0, 0);
}

uint CountOf(Item item) =>
    conn.Db.MyInventory.Iter().Where(r => r.Item == (ushort)item).Aggregate(0u, (n, r) => n + r.Count);

// Walks to items of a kind and picks them up until carrying enough.
void Gather(Item item, uint want)
{
    var deadline = Stopwatch.StartNew();
    while (CountOf(item) < want)
    {
        if (deadline.Elapsed > TimeSpan.FromSeconds(90)) Fail($"could not gather {want} {item}");
        var target = conn.Db.WorldItem.Iter()
            .Where(w => w.Item == (ushort)item)
            .OrderBy(w => (w.X - x) * (w.X - x) + (w.Z - z) * (w.Z - z))
            .FirstOrDefault();
        if (target == null) { Pump(() => false, 1, null); continue; }

        WalkTo(target.X, target.Z);
        uint before = CountOf(item);
        conn.Reducers.PickUp(target.Id);
        Pump(() => CountOf(item) > before || conn.Db.WorldItem.Id.Find(target.Id) == null, 3, "pickup");
    }
}

// Walks at a safe pace until within reach of a spot.
void WalkTo(float tx, float tz)
{
    // Round trunks and boulders by a route, then straight in (bugfix-34).
    var route = ground.Route(x, z, tx, tz);
    for (int i = 0; i < route.Count - 1; i++) Leg(route[i].X, route[i].Z, 0.4f);
    Leg(tx, tz, 1.5f);
}

void Leg(float tx, float tz, float tol)
{
    int lagged = 0;
    for (int guard = 0; ; guard++)
    {
        float dx = tx - x, dz = tz - z, d = MathF.Sqrt(dx * dx + dz * dz);
        if (d < tol) break;
        if (guard > 900)
        {
            var srv = conn.Db.PlayerPos.PlayerId.Find(myId);
            var near = new List<string>();
            for (int ix = (int)x - 3; ix <= (int)x + 3; ix++)
                for (int iz = (int)z - 3; iz <= (int)z + 3; iz++)
                    if (Obstacles.At(ground, ix, iz, out float ox, out float oz, out float r, out float h)) near.Add($"{(h == float.MaxValue ? "trunk" : "rock")}@({ox:F1},{oz:F1}) r{r:F2}");
            foreach (var st in conn.Db.Structure.Iter())
                if (MathF.Abs(st.X - x) < 6 && MathF.Abs(st.Z - z) < 6) near.Add($"{st.Kind}@({st.X:F1},{st.Z:F1}) done={st.Complete} stage={st.Stage}");
            Console.WriteLine("  near: " + string.Join("; ", near));
            Fail($"stuck walking to ({tx:F1}, {tz:F1}): bot at ({x:F1}, {z:F1}), server at ({srv?.X:F1}, {srv?.Z:F1}), {d:F1} m left");
        }
        float step = Math.Min(tol >= 1.5f ? d - 1f : d, 0.5f);
        // The server limits the pace (World.MoveAllowance, Carry.WalkFactor): only walk on once it
        // has caught up, and keep reporting where we are, as a real client does (bugfix-24).
        var behind = conn.Db.PlayerPos.PlayerId.Find(myId);
        if (behind is { } held && MathF.Abs(held.X - x) + MathF.Abs(held.Z - z) >= 1.5f)
        {
            // The server has not followed for a second: it refused the last step (something solid in the
            // way). Take its word for where we are and carry on from there, like a player would.
            if (++lagged > 10) { x = held.X; z = held.Z; lagged = 0; }
        }
        else lagged = 0;
        if (behind is not { } seen || MathF.Abs(seen.X - x) + MathF.Abs(seen.Z - z) < 1.5f)
        {
            float px = x, pz = z;
            x += dx / d * step;
            z += dz / d * step;
            (x, z) = ground.Slide(px, pz, x, z); // round things and walls, as a player meets them
        }
        conn.Reducers.Move(x, z, MathF.Atan2(dx, dz));
        Pump(() => false, 0.1f, null);
    }
    for (var settle = Stopwatch.StartNew(); ; )
    {
        if (conn.Db.PlayerPos.PlayerId.Find(myId) is { } p && MathF.Abs(p.X - x) < 0.1f && MathF.Abs(p.Z - z) < 0.1f) break;
        if (settle.Elapsed.TotalSeconds > 15)
        {
            // Say where everyone is and what is near, so a blocked or too-slow walk can be told apart.
            var at = conn.Db.PlayerPos.PlayerId.Find(myId);
            var near = string.Join(", ", conn.Db.Structure.Iter()
                .Select(st => $"{st.Kind}#{st.Id} d={MathF.Sqrt((st.X - x) * (st.X - x) + (st.Z - z) * (st.Z - z)):F1}")
                .OrderBy(t => t).Take(6));
            Fail($"timed out waiting for position to settle (target {x:F1},{z:F1}; server {at?.X:F1},{at?.Z:F1}; last error {lastError ?? "none"}; structures: {near})");
        }
        conn.Reducers.Move(x, z, 0f);
        Pump(() => false, 0.1f, null);
    }
}

void Pump(Func<bool> until, float seconds, string? what)
{
    var sw = Stopwatch.StartNew();
    while (!until())
    {
        if (sw.Elapsed.TotalSeconds > seconds)
        {
            if (what != null) Fail($"timed out waiting for {what}");
            return;
        }
        conn.FrameTick();
        Thread.Sleep(10);
    }
}

// Runs a reducer as the database owner through the CLI (the bot is not the owner).
void AdminCall(string reducer, string arg)
{
    var info = new ProcessStartInfo("spacetimedb-cli", new[] { "call", "--no-config", "--server", uri, db, reducer, arg })
    {
        WorkingDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "server"),
        RedirectStandardError = true,
        RedirectStandardOutput = true,
    };
    using var p = Process.Start(info) ?? throw new Exception("could not start spacetimedb-cli");
    string err = p.StandardError.ReadToEnd();
    p.WaitForExit();
    if (p.ExitCode != 0) Fail($"{reducer} {arg}: {err}");
    Pump(() => false, 0.5f, null);
}

static void Fail(string message)
{
    Console.Error.WriteLine($"Crafting check FAILED: {message}");
    Environment.Exit(1);
}
