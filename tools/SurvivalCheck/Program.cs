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
                "SELECT * FROM my_respawn_point",
                "SELECT * FROM my_skills",
                "SELECT * FROM forage_bush",
                "SELECT * FROM player_pos",
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
// Names are letters only (Names.Problem, ui-31).
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

// --- Hazelnuts (survival-30): a nibble from the data, from about one bush in three ---
Give(Item.Hazelnuts);
float foodH = V().Food;
conn.Reducers.Eat((ushort)Item.Hazelnuts);
Pump(() => CountOf(Item.Hazelnuts) == 0, 5, "the hazelnut to be eaten");
Settle(200);
if (MathF.Abs((V().Food - foodH) - Needs.HazelnutFood) > 0.01f)
    Fail($"a hazelnut gave {V().Food - foodH:F3} food, expected {Needs.HazelnutFood:F3}");
Ok($"a hazelnut gives {Needs.HazelnutFood:P0} food (from Foods)");
var allBushes = conn.Db.ForageBush.Iter().ToList();
int hazels = allBushes.Count(b => Needs.IsHazelBush(b.Id));
if (allBushes.Count < 6 || hazels * 5 < allBushes.Count || hazels * 2 > allBushes.Count)
    Fail($"{hazels} of {allBushes.Count} bushes are hazel; expected about a third");
Ok($"{hazels} of {allBushes.Count} bushes are hazel, the rest bear berries");

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

// --- Waking places (survival-23): none to start, and the server refuses nonsense ---
if (conn.Db.MyRespawnPoint.Iter().Any()) Fail("a new character should have no waking place");
if (V().WakeKind != RestSpots.WokeAtEdge) Fail("a new character has not woken anywhere yet");
Ok("a new character has no waking place");
var matSpot = RestSpots.OfItem(Item.SleepingMat) ?? throw new Exception("the sleeping mat is a rest spot");
lastError = null;
conn.Reducers.SetRespawn(60000, 1);
Pump(() => lastError != null, 5, "an unknown rest spot to be refused");
if (!lastError!.Contains("not somewhere to sleep")) Fail($"unknown rest spot said: {lastError}");
Ok($"an unknown rest spot is refused ({lastError})");
lastError = null;
conn.Reducers.SetRespawn(matSpot.Id, 9_999_999);
Pump(() => lastError != null, 5, "a missing building to be refused");
if (!lastError!.Contains("gone")) Fail($"a missing building said: {lastError}");
Ok($"a building that is not there is refused ({lastError})");
if (conn.Db.MyRespawnPoint.Iter().Any()) Fail("a refused choice must not set a waking place");
Ok("a refused choice sets nothing (the accepted choice and waking after a death are checked in BuildTest)");
// --- Endurance is trained by running, which the server checks it saw (D-182) ---
int Endurance() => conn.Db.MySkills.Iter().Where(r => r.Spec == Stamina.Skill).Sum(r => (int)r.Points);
uint myId = conn.Db.Player.Identity.Find(me!.Value)!.PlayerId;
var here = conn.Db.PlayerPos.PlayerId.Find(myId)!;
float px = here.X, pz = here.Z;
void Walk(float meters)
{
    for (int i = 0; i < (int)(meters / 0.4f); i++)
    {
        px += 0.4f;
        conn.Reducers.Move(px, pz, MathF.PI / 2);
        Settle(100);
    }
}
int e0 = Endurance();
Settle((int)(Stamina.RunReportMoveWindow * 1000) + 1000);   // joining counts as having just moved
conn.Reducers.Ran();
Settle(500);
if (Endurance() != e0) Fail("Endurance rose for a run reported by a player who was standing still");
Ok("a run reported while standing still trains nothing");
Walk(30);
conn.Reducers.Ran();
Pump(() => Endurance() > e0, 5, "Endurance to rise after a real stretch of walking");
int e1 = Endurance();
Ok("a run reported after real movement trains Endurance");
Settle((int)(Stamina.RunPerUse * 0.95f * 1000));
Walk(5);
conn.Reducers.Ran();
Settle(500);
if (Endurance() != e1) Fail("Endurance rose for a second report from almost the same place");
Ok("a second report from almost the same place trains nothing");
Walk(30);
conn.Reducers.Ran();
Pump(() => Endurance() > e1, 5, "Endurance to rise after moving on");
Ok("a report after moving on trains Endurance again");

Console.WriteLine("Survival check passed");
conn.Disconnect();
return 0;
