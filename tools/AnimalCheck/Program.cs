// Animal check (animals-29): the server rules behind the animals, with one fresh character on a
// freshly published server. First the Def data invariants that the client and server both read
// (shared/Animals.cs), then the reducers' refusals (D-182): an animal out of reach cannot be
// struck, shot or have its eggs taken, and an animal that is gone is reported as gone. Every wait
// is state-based with a generous timeout. Needs no admin, no weapon and no walking, so it runs in a
// few seconds. Exits non-zero on failure.
//
// Usage: dotnet run -- [uri=http://127.0.0.1:3000] [db=lifegame]
using System.Diagnostics;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

string uri = args.Length > 0 ? args[0] : "http://127.0.0.1:3000";
string db = args.Length > 1 ? args[1] : "lifegame";
var clock = Stopwatch.StartNew();
Identity? me = null;
bool subscribed = false;
string? lastError = null;

void Fail(string what)
{
    Console.WriteLine($"Animal check FAILED: {what}");
    Environment.Exit(1);
}

void Ok(string what) => Console.WriteLine($"ok  {what}");

// --- The Defs: what the server and client both read ---
var defs = AnimalDefs.All;
if (defs.Length == 0) Fail("no animal Defs");
for (int i = 0; i < defs.Length; i++)
{
    var d = defs[i];
    if (d.Id != i) Fail($"{d.Name}: Id {d.Id} is not its place {i} in AnimalDefs.All (ids are append-only)");
    if (d.Key.Length == 0 || d.Name.Length == 0) Fail($"animal {i} has no key or name");
    if (!(d.Walk > 0f && d.Run >= d.Walk)) Fail($"{d.Name}: speeds walk {d.Walk}, run {d.Run} are not ordered");
    if (d.DamageTaken <= 0f) Fail($"{d.Name}: DamageTaken {d.DamageTaken} is not positive");
    if (d.Voice.Length == 0) Fail($"{d.Name}: no Voice key for the Audio lane");
    if (d.Model.Length == 0) Fail($"{d.Name}: no model plan");
    if ((d.Egg != null) != (d.EggCap > 0 && d.EggDays > 0f))
        Fail($"{d.Name}: Egg, EggDays and EggCap must be set together (Egg {d.Egg}, {d.EggDays} days, cap {d.EggCap})");
}
if (defs.Select(d => d.Key).Distinct().Count() != defs.Length) Fail("two animals share a key");
Ok($"{defs.Length} animal Defs are consistent (ids, speeds, damage, voice, model, egg fields)");

// --- A fresh character ---
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
                "SELECT * FROM animal",
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

Pump(() => subscribed, 30, "subscription");
Pump(() => me is { } id && conn.Db.Player.Identity.Find(id) is not null, 20, "player row");
Player Me() => conn.Db.Player.Identity.Find(me!.Value)!;
Pump(() => conn.Db.PlayerPos.PlayerId.Find(Me().PlayerId) is not null, 20, "our position");
var here = conn.Db.PlayerPos.PlayerId.Find(Me().PlayerId)!;

// Calls a reducer that must be refused and returns the refusal text.
string Refused(string what, Action call)
{
    lastError = null;
    call();
    Pump(() => lastError != null, 15, $"{what} to be refused");
    return lastError!;
}

// --- An animal that is gone ---
const ulong NoSuchAnimal = ulong.MaxValue - 7;
Refused("striking an animal that is not there", () => conn.Reducers.StrikeAnimal(NoSuchAnimal));
Refused("shooting an animal that is not there", () => conn.Reducers.ShootAnimal(NoSuchAnimal));
string gone = Refused("collecting eggs from a hen that is not there", () => conn.Reducers.CollectEggs(NoSuchAnimal));
if (!gone.Contains("wandered off")) Fail($"a missing hen was refused with \"{gone}\"");
Ok("an animal that is not there is refused");

// --- An animal out of reach: pick one well away from us, if the area holds one ---
float Away(Animal a) => MathF.Sqrt((a.X - here.X) * (a.X - here.X) + (a.Z - here.Z) * (a.Z - here.Z));
var far = conn.Db.Animal.Iter().Where(a => Away(a) > 25f).OrderBy(Away).FirstOrDefault();
if (far == null)
{
    Ok("no animal in view to test reach on (the Defs and missing-animal checks stand)");
}
else
{
    string strike = Refused("a strike out of reach", () => conn.Reducers.StrikeAnimal(far.Id));
    if (strike.Length == 0) Fail("a strike out of reach was refused with no reason");
    Ok($"a strike at an animal {Away(far):F0} m away is refused ({strike})");

    string shot = Refused("a shot out of range", () => conn.Reducers.ShootAnimal(far.Id));
    if (shot.Length == 0) Fail("a shot out of range was refused with no reason");
    Ok($"a shot at an animal {Away(far):F0} m away is refused ({shot})");

    // Eggs are only laid by Defs with an Egg; either way a far animal is not collected from.
    string eggs = Refused("eggs from an animal out of reach", () => conn.Reducers.CollectEggs(far.Id));
    bool lays = AnimalDefs.Get((byte)far.Species).Egg != null;
    if (lays && !eggs.Contains("reach")) Fail($"eggs from a hen {Away(far):F0} m away were refused with \"{eggs}\"");
    if (!lays && !eggs.Contains("doesn't lay")) Fail($"eggs from a non-layer were refused with \"{eggs}\"");
    Ok($"eggs from an animal {Away(far):F0} m away are refused ({eggs})");
}

Console.WriteLine("Animal check passed");
conn.Disconnect();
return 0;
