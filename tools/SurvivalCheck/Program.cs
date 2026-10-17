// Survival check (survival-22): one fresh character on a freshly published server. Checks
// the first-hour thirst ease (survival-14), that foods and water containers work through
// the data in shared/Foods.cs (survival-17/20: eating, the pot or skin coming back, the
// good-meal buff), and that inedible things are refused. Items come from the admin give,
// so the tool makes its bot an admin through the CLI (the publisher's identity), which
// must be on PATH as `spacetimedb-cli`. Runs in a few seconds. Exits non-zero on failure.
//
// Usage: dotnet run -- [uri=http://127.0.0.1:3000] [db=lifegame] [server=local]
using System.Diagnostics;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

string uri = args.Length > 0 ? args[0] : "http://127.0.0.1:3000";
string db = args.Length > 1 ? args[1] : "lifegame";
string server = args.Length > 2 ? args[2] : "local";
var clock = Stopwatch.StartNew();
Identity? me = null;
bool subscribed = false;
string? lastError = null;

void Fail(string what)
{
    Console.WriteLine($"Survival check FAILED: {what}");
    Environment.Exit(1);
}

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
                "SELECT * FROM my_vitals",
                "SELECT * FROM my_inventory",
                "SELECT * FROM my_admin",
            });
    })
    .OnConnectError(e => Fail($"connect: {e.Message}"))
    .Build();
conn.OnUnhandledReducerError += (_, e) => lastError = e.Message;

void Pump(Func<bool> done, int seconds, string what)
{
    var until = clock.Elapsed + TimeSpan.FromSeconds(seconds);
    while (!done() && clock.Elapsed < until) { conn.FrameTick(); Thread.Sleep(5); }
    if (!done()) Fail($"timed out waiting for {what}");
}

void Settle(int ms)
{
    var until = clock.Elapsed + TimeSpan.FromMilliseconds(ms);
    while (clock.Elapsed < until) { conn.FrameTick(); Thread.Sleep(5); }
}

void Ok(string what) => Console.WriteLine($"ok  {what}");

Pump(() => subscribed, 20, "subscription");
Pump(() => me is { } id && conn.Db.Player.Identity.Find(id) is not null, 10, "player row");
Pump(() => conn.Db.MyVitals.Iter().Any(), 10, "vitals row");
Pump(() => conn.Db.MyVitals.Iter().First().ThirstEaseLeft > 0, 5, "the new character's vitals to settle");

Vitals V() => conn.Db.MyVitals.Iter().First();
uint CountOf(Item item) => (uint)conn.Db.MyInventory.Iter().Where(r => r.Item == (ushort)item).Sum(r => (long)r.Count);

// --- First hour: thirst falls 20% slower (survival-14) ---
var v0 = V();
float expectedRate = -Needs.WaterDrain * Needs.ThirstEaseFactor;
if (MathF.Abs(v0.WaterRate - expectedRate) > MathF.Abs(expectedRate) * 0.02f)
    Fail($"a new character's thirst rate is {v0.WaterRate * 3600f:F3}/h, expected {expectedRate * 3600f:F3}/h");
Ok($"a new character's thirst falls {-v0.WaterRate * 3600f:F2} of the bar an hour ({Needs.ThirstEaseFactor:P0} of the usual)");
if (v0.ThirstEaseLeft < Needs.ThirstEaseSeconds - 120f || v0.ThirstEaseLeft > Needs.ThirstEaseSeconds + 1f)
    Fail($"thirst ease has {v0.ThirstEaseLeft:F0} s left, expected about {Needs.ThirstEaseSeconds:F0}");
Ok($"thirst ease runs for the first {v0.ThirstEaseLeft / 60f:F0} minutes online");
if (MathF.Abs(v0.FoodRate + Needs.FoodDrain) > Needs.FoodDrain * 0.02f)
    Fail($"hunger rate {v0.FoodRate * 5400f:F3} per 90 min is not the usual");
Ok("hunger is not eased");

// --- Items come from the admin give; make this bot an admin through the CLI ---
string name = "Surv" + new string(Enumerable.Range(0, 8).Select(_ => (char)('a' + Random.Shared.Next(26))).ToArray());
conn.Reducers.SetName(name);
Settle(400);
var psi = new ProcessStartInfo("spacetimedb-cli", new[] { "call", db, "--server", server, "make_admin", $"\"{name}\"" })
{
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};
try
{
    using var p = Process.Start(psi)!;
    p.WaitForExit(60_000);
    if (p.ExitCode != 0) Fail($"make_admin through the CLI failed: {p.StandardError.ReadToEnd()}");
}
catch (Exception e) when (e is not OperationCanceledException)
{
    Fail($"could not run spacetimedb-cli to make the bot an admin: {e.Message}");
}
Pump(() => conn.Db.MyAdmin.Iter().Any(), 10, "the bot to become an admin");
Ok("bot is an admin (items can be given)");

void Give(Item item, byte quality = 60)
{
    uint before = CountOf(item);
    lastError = null;
    conn.Reducers.AdminGive((ushort)item, 1, quality);
    Pump(() => lastError != null || CountOf(item) > before, 5, $"{item} to be given");
    if (lastError != null) Fail($"giving {item}: {lastError}");
}

// --- Berries: food from the data, a pinch of water with it ---
Give(Item.Berries);
float food0 = V().Food, water0 = V().Water;
conn.Reducers.Eat((ushort)Item.Berries);
Pump(() => CountOf(Item.Berries) == 0, 5, "the berries to be eaten");
Settle(200);
var v1 = V();
if (MathF.Abs((v1.Food - food0) - Needs.BerryFood) > 0.01f)
    Fail($"berries gave {v1.Food - food0:F3} food, expected {Needs.BerryFood:F3}");
if (MathF.Abs((v1.Water - water0) - Needs.BerryWater) > 0.01f)
    Fail($"berries gave {v1.Water - water0:F3} water, expected {Needs.BerryWater:F3}");
Ok($"berries give {Needs.BerryFood:P0} food and {Needs.BerryWater:P0} water (from Foods)");

// --- Stew: a good meal, the pot comes back at its quality ---
Give(Item.PotOfStew, 77);
conn.Reducers.Eat((ushort)Item.PotOfStew);
Pump(() => CountOf(Item.PotOfStew) == 0 && CountOf(Item.CoilPot) == 1, 5, "the stew to be eaten and the pot to come back");
Settle(200);
var pot = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.CoilPot);
if (pot.Quality != 77) Fail($"the pot came back at quality {pot.Quality}, expected 77");
long nowMicros = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000L;
if (V().WellFedUntil < nowMicros + 5 * 60 * 1_000_000L) Fail("a stew should leave you well fed for a while");
Ok("stew is a good meal and the coil pot comes back at its quality");

// --- Waterskin and pot of water: drunk, the empty container comes back ---
foreach (var (full, empty) in new[] { (Item.WaterskinOfWater, Item.Waterskin), (Item.PinchPotOfWater, Item.PinchPot) })
{
    Give(full, 70);
    // Let thirst show first, so the drink is not clamped away.
    float waterBefore = V().Water;
    conn.Reducers.DrinkFrom((ushort)full);
    Pump(() => CountOf(full) == 0 && CountOf(empty) == 1, 5, $"{full} to be drunk and {empty} to come back");
    Settle(200);
    var back = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)empty);
    if (back.Quality != 70) Fail($"{empty} came back at quality {back.Quality}, expected 70");
    if (V().Water < waterBefore - 0.001f) Fail($"{full} did not quench thirst");
    Ok($"{full} is drunk and the empty {empty} comes back at its quality");
}

// --- Refusals are the server's ---
Give(Item.Stick, 50);
lastError = null;
conn.Reducers.Eat((ushort)Item.Stick);
Pump(() => lastError != null, 5, "eating a stick to be refused");
Ok($"eating a stick is refused ({lastError})");
lastError = null;
conn.Reducers.DrinkFrom((ushort)Item.Stick);
Pump(() => lastError != null, 5, "drinking from a stick to be refused");
Ok($"drinking from a stick is refused ({lastError})");

Console.WriteLine("Survival check passed");
