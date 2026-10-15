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
                "SELECT * FROM my_inventory",
                "SELECT * FROM my_craft_job",
                "SELECT * FROM roast",
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

WalkTo(fireX, fireZ);
int firesBefore = conn.Db.Fire.Count;
Craft("fire.hand_drill", 1f, null);
Pump(() => conn.Db.Fire.Count > firesBefore, 5, "a fire to be lit");
var fire = conn.Db.Fire.Iter().First(f => f.LitBy == myId);
Console.WriteLine($"Fire lit at ({fire.X:F1}, {fire.Z:F1})");

// A failed drill loses its inputs and lights nothing.
Gather(Item.Fiber, 2);
Gather(Item.Stick, 3);
Craft("gather.tinder", 0, Item.Tinder);
int firesNow = conn.Db.Fire.Count;
Craft("fire.hand_drill", 0.1f, null);
Pump(() => false, 1, null);
if (conn.Db.Fire.Count != firesNow) Fail("a failed drill still lit a fire");
if (CountOf(Item.Tinder) != 0) Fail("a failed drill kept its tinder");
Console.WriteLine("Failed drill lit nothing and used up its tinder");

WalkTo(fire.X, fire.Z);
long fuelBefore = fire.FuelUntil.MicrosecondsSinceUnixEpoch;
lastError = null;
conn.Reducers.FeedFire(fire.Id, (ushort)Item.Stick);
Pump(() => lastError != null || conn.Db.Fire.Id.Find(fire.Id)?.FuelUntil.MicrosecondsSinceUnixEpoch > fuelBefore, 5, "fuel to be added");
if (lastError != null) Fail($"feeding the fire: {lastError}");
Console.WriteLine("Fed the fire a stick");

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

void Craft(string processId, float score, Item? expect)
{
    uint before = expect is { } e ? CountOf(e) : 0;
    lastError = null;
    conn.Reducers.StartCraft(processId, score);
    Pump(() => lastError != null || conn.Db.MyCraftJob.Count > 0, 5, $"{processId} to start");
    if (lastError != null) Fail($"{processId}: {lastError}");
    var def = Processes.Find(processId)!;
    Pump(() => conn.Db.MyCraftJob.Count == 0, def.Seconds + 5, $"{processId} to finish");
    if (expect is { } item)
    {
        Pump(() => CountOf(item) > before, 3, $"{processId} output");
        var best = conn.Db.MyInventory.Iter().Where(r => r.Item == (ushort)item).Max(r => r.Quality);
        Console.WriteLine($"{processId}: made {Items.Def(item).Name} (quality {best})");
    }
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
