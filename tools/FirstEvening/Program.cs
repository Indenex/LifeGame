// First evening (survival-10): how far a new player is from what they need on
// their first evening, and how long their warmth lasts while they fetch it.
// Reads the live world (berry bushes, loose sticks and fiber) from a local server
// and the terrain from shared/World.cs, then prints a table per spawn kind:
// first spawns (the middle of the seeded area) and respawns (the wilderness rim).
// It changes nothing in the world.
//
// Usage: dotnet run -- [uri=http://127.0.0.1:3000] [db=lifegame]
using System.Diagnostics;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

string uri = args.Length > 0 ? args[0] : "http://127.0.0.1:3000";
string db = args.Length > 1 ? args[1] : "lifegame";
var clock = Stopwatch.StartNew();
bool ready = false, applied = false;
var conn = DbConnection.Builder().WithUri(uri).WithDatabaseName(db)
    .OnConnect((c, _, _) => ready = true)
    .OnConnectError(e => Console.WriteLine($"connect error: {e.Message}"))
    .Build();
void Pump(Func<bool> until, int seconds)
{
    var end = clock.Elapsed + TimeSpan.FromSeconds(seconds);
    while (!until() && clock.Elapsed < end) { conn.FrameTick(); Thread.Sleep(5); }
}
Pump(() => ready, 20);
if (!ready) { Console.WriteLine("FAIL could not connect"); return 1; }
conn.SubscriptionBuilder().OnApplied(_ => applied = true)
    .Subscribe(new[] { "SELECT * FROM forage_bush", "SELECT * FROM world_item" });
Pump(() => applied, 30);

var bushes = conn.Db.ForageBush.Iter().Where(b => b.Kind == Forages.BerryBushKind).Select(b => (b.X, b.Z)).ToList();
var items = conn.Db.WorldItem.Iter().ToList();
var sticks = items.Where(i => i.Item == (ushort)Item.Stick).Select(i => (i.X, i.Z)).ToList();
var fiber = items.Where(i => i.Item == (ushort)Item.Fiber).Select(i => (i.X, i.Z)).ToList();
Console.WriteLine($"world: {bushes.Count} bushes, {sticks.Count} sticks, {fiber.Count} fiber lying about");

const float Walk = 4.2f, PickSeconds = 1f;
float Dist((float x, float z) a, (float x, float z) b) => MathF.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
bool Wet(float x, float z) => World.BaseHeight(World.CellOf(x), World.CellOf(z)) < Needs.WaterLevel;

float NearestWater((float x, float z) p)
{
    for (int r = 0; r <= 120; r++)
        for (int i = 0; i < Math.Max(8, r * 8); i++)
        {
            float a = i * MathF.PI * 2 / Math.Max(8, r * 8);
            if (Wet(p.x + MathF.Cos(a) * r, p.z + MathF.Sin(a) * r)) return MathF.Max(0, r - World.Reach);
        }
    return float.PositiveInfinity;
}

float Nearest((float x, float z) p, List<(float, float)> list) =>
    list.Count == 0 ? float.PositiveInfinity : list.Min(q => Dist(p, q));

// Greedy route picking up `n` of each kind, nearest first; returns metres walked.
float Gather((float x, float z) start, params (List<(float, float)> list, int n)[] wants)
{
    var at = start;
    float walked = 0;
    var left = wants.Select(w => (pool: new List<(float, float)>(w.list), n: w.n)).ToList();
    while (left.Any(l => l.n > 0))
    {
        int best = -1; (float, float) target = default; float bestD = float.PositiveInfinity;
        for (int k = 0; k < left.Count; k++)
            if (left[k].n > 0)
                foreach (var q in left[k].pool)
                    if (Dist(at, q) < bestD) { bestD = Dist(at, q); target = q; best = k; }
        if (best < 0) return float.PositiveInfinity;
        walked += bestD; at = target;
        left[best].pool.Remove(target);
        left[best] = (left[best].pool, left[best].n - 1);
    }
    return walked;
}

// Minutes from full warmth to frozen away from fire, for a chill multiplier.
float Freeze(float chill, bool arrival) =>
    1f / (Needs.ColdDrain * Math.Min(chill, Needs.MaxChill) * (arrival ? Needs.ArrivalChill : 1f)) / 60f;

void Report(string kind, List<(float x, float z)> spawns, bool arrival)
{
    var rows = spawns.Select(p =>
    {
        float fireMetres = Gather(p, (sticks, 2), (fiber, 2));
        float fireSeconds = fireMetres / Walk + 4 * PickSeconds + 3 + 4 / 0.65f; // shred, drill with ~35% fails
        return (wet: Wet(p.x, p.z), bush: Nearest(p, bushes), water: NearestWater(p), fireMetres, fireSeconds);
    }).ToList();
    string Stat(Func<(bool wet, float bush, float water, float fireMetres, float fireSeconds), float> f) =>
        $"median {Median(rows.Select(f)):F0}, worst {rows.Select(f).Max():F0}";
    Console.WriteLine($"\n{kind} ({spawns.Count} spots)");
    Console.WriteLine($"  spawned in water: {rows.Count(r => r.wet)}");
    Console.WriteLine($"  nearest bush (m): {Stat(r => r.bush)}");
    Console.WriteLine($"  nearest drinkable water (m): {Stat(r => r.water)}");
    Console.WriteLine($"  2 sticks + 2 fiber walk (m): {Stat(r => r.fireMetres)}");
    Console.WriteLine($"  first fire (s): {Stat(r => r.fireSeconds)}");
    float worstFire = rows.Max(r => r.fireSeconds) / 60f;
    foreach (var (label, chill) in new[] { ("mild day 15 C", 1f), ("cool 7.5 C", 1.5f), ("freezing night", 2f), ("freezing + soaked (capped)", 3f) })
        Console.WriteLine($"  {label}: frozen in {Freeze(chill, arrival):F1} min; worst first fire uses {100 * worstFire / Freeze(chill, arrival):F0}% of that");
}

static float Median(IEnumerable<float> xs) { var s = xs.OrderBy(x => x).ToList(); return s.Count == 0 ? 0 : s[s.Count / 2]; }

var first = new List<(float, float)>();
for (float x = -8; x <= 8; x += 2) for (float z = -8; z <= 8; z += 2) first.Add((x, z));
float rim = World.SpawnRadiusChunks * World.ChunkSize * World.CellSize - 6f;
var edge = Enumerable.Range(0, 24).Select(i => (MathF.Cos(i * MathF.PI / 12) * rim, MathF.Sin(i * MathF.PI / 12) * rim))
    .Where(p => !Wet(p.Item1, p.Item2)).ToList();

Report("First spawn (new character, half-speed cold for 30 min)", first, arrival: true);
Report("Respawn at the wilderness edge (after a death)", edge, arrival: false);
conn.Disconnect();
return 0;
