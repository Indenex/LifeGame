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
            });
    })
    .OnConnectError(e => Fail($"connect: {e.Message}"))
    .Build();
conn.OnUnhandledReducerError += (_, e) => lastError = e.Message;

Pump(() => subscribed, 20, "subscription");
Pump(() => me is { } id && conn.Db.Player.Identity.Find(id) is not null, 10, "player row");
myId = conn.Db.Player.Identity.Find(me!.Value)!.PlayerId;
var start = conn.Db.PlayerPos.PlayerId.Find(myId)!;
float x = start.X, z = start.Z;

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

// The first fire has burned a while; once a whole stick fits under the cap it is taken (D-121).
// Firecraft only raises the cap, so the rank-0 cap is a safe bound.
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

// Crafting-15: stations (D-098, D-112). A basket is woven at a crafting table, which
// stands inside a claim, and part of its fiber comes from the claim's stash.
lastError = null;
conn.Reducers.StartCraft("weave.basket", 0);
Pump(() => lastError != null, 5, "a basket away from a table to be refused");
Console.WriteLine($"Basket with no table: {lastError}");
lastError = null;
conn.Reducers.PlaceStructure(StructureKind.CraftingTable, x + 2f, z, 0f);
Pump(() => lastError != null, 5, "a table outside a claim to be refused");
if (lastError?.Contains("inside your claim") != true) Fail($"a table outside a claim: {lastError}");
Console.WriteLine($"Table outside a claim: {lastError}");
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
    conn.Reducers.StashItem(claim.Id, fiber.Item, fiber.Quality);
    Pump(() => lastError != null || CountOf(Item.Fiber) < had, 5, "fiber into the stash");
    if (lastError != null) Fail($"stashing fiber: {lastError}");
}
uint StashFiber() => conn.Db.MyClaimStash.Iter().Where(r => r.ClaimId == claim.Id && r.Item == (ushort)Item.Fiber).Aggregate(0u, (n, r) => n + r.Count);
uint stashed = StashFiber();
WalkTo(table.X, table.Z);
Craft("weave.basket", 0, Item.Basket);
Pump(() => StashFiber() == stashed - 4, 3, "4 fiber to leave the stash");
Console.WriteLine($"Wove a basket at the table; 4 fiber came from the stash ({stashed} -> {StashFiber()})");

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
            if (Rocks.LiesAt(i, j) && conn.Db.TerrainEdit.CellKey.Find(World.CellKey(i, j)) is null)
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
    while (true)
    {
        float dx = tx - x, dz = tz - z, d = MathF.Sqrt(dx * dx + dz * dz);
        if (d < 1.5f) break;
        float step = Math.Min(d - 1f, 0.5f);
        x += dx / d * step;
        z += dz / d * step;
        conn.Reducers.Move(x, z, MathF.Atan2(dx, dz));
        Pump(() => false, 0.1f, null);
    }
    Pump(() => conn.Db.PlayerPos.PlayerId.Find(myId) is { } p && MathF.Abs(p.X - x) < 0.1f && MathF.Abs(p.Z - z) < 0.1f, 3, "position to settle");
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

static void Fail(string message)
{
    Console.Error.WriteLine($"Crafting check FAILED: {message}");
    Environment.Exit(1);
}
