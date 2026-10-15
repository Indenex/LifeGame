// Phase 0 load test: N simulated players connect to a local SpacetimeDB,
// subscribe to the chunks around them like the real client, walk around at
// 10 Hz, dig, and pick up items. Prints server CPU, memory, bandwidth and
// reducer round-trip latency.
//
// Usage: dotnet run -c Release -- [bots=50] [seconds=60] [compression=brotli|gzip|none] [uri=http://127.0.0.1:3000] [db=lifegame]
using System.Diagnostics;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

int botCount = args.Length > 0 ? int.Parse(args[0]) : 50;
int seconds = args.Length > 1 ? int.Parse(args[1]) : 60;
var compression = (args.Length > 2 ? args[2] : "brotli") switch
{
    "gzip" => Compression.Gzip,
    "none" => Compression.None,
    _ => Compression.Brotli,
};
string uri = args.Length > 3 ? args[3] : "http://127.0.0.1:3000";
string db = args.Length > 4 ? args[4] : "lifegame";

var bots = new List<Bot>();
for (int i = 0; i < botCount; i++)
    bots.Add(new Bot(i, uri, db, compression));

// Wait for every bot to connect and receive its first subscription.
var connectDeadline = Stopwatch.StartNew();
while (bots.Any(b => !b.Ready) && connectDeadline.Elapsed < TimeSpan.FromSeconds(30))
{
    foreach (var b in bots) b.Conn.FrameTick();
    Thread.Sleep(5);
}
int ready = bots.Count(b => b.Ready);
Console.WriteLine($"{ready}/{botCount} bots connected in {connectDeadline.Elapsed.TotalSeconds:F1}s");
if (ready == 0) return 1;

var server = ServerStats.Find();
var startStats = server?.Sample();
long startLo = LoopbackBytes();
var run = Stopwatch.StartNew();
var tick = TimeSpan.FromMilliseconds(100);
var nextTick = TimeSpan.Zero;

while (run.Elapsed < TimeSpan.FromSeconds(seconds))
{
    if (run.Elapsed >= nextTick)
    {
        nextTick += tick;
        foreach (var b in bots.Where(b => b.Ready)) b.Step(0.1f);
    }
    foreach (var b in bots) b.Conn.FrameTick();
    Thread.Sleep(2);
}

double elapsed = run.Elapsed.TotalSeconds;
var endStats = server?.Sample();
long loBytes = LoopbackBytes() - startLo;

var latencies = bots.SelectMany(b => b.MoveLatenciesMs).OrderBy(x => x).ToArray();
double P(double q) => latencies.Length == 0 ? double.NaN : latencies[(int)Math.Min(latencies.Length - 1, q * latencies.Length)];

Console.WriteLine();
Console.WriteLine($"Bots: {ready}, duration: {elapsed:F0}s, tick: 10 Hz, compression: {compression}");
Console.WriteLine($"Moves sent: {bots.Sum(b => b.MovesSent)}, digs: {bots.Sum(b => b.Digs)}, pickups: {bots.Sum(b => b.Pickups)}, reducer errors: {bots.Sum(b => b.Errors)}");
Console.WriteLine($"Move round trip ms (until others see it): p50 {P(0.5):F1}, p95 {P(0.95):F1}, p99 {P(0.99):F1}, max {(latencies.Length > 0 ? latencies[^1] : double.NaN):F1}");
if (startStats is { } s0 && endStats is { } s1)
{
    double cpuCores = (s1.CpuSeconds - s0.CpuSeconds) / elapsed;
    Console.WriteLine($"Server CPU: {cpuCores * 100:F1}% of one core ({cpuCores / ready * 100:F2}% per player)");
    Console.WriteLine($"Server memory (RSS): {s1.RssMb:F0} MB");
}
else
{
    Console.WriteLine("Server process not found; skipped CPU and memory.");
}
if (loBytes > 0)
    Console.WriteLine($"Loopback traffic, both directions: {loBytes / elapsed / 1024:F0} KB/s total, {loBytes / elapsed / ready / 1024:F1} KB/s per player");

foreach (var b in bots) b.Conn.Disconnect();
return 0;

static long LoopbackBytes()
{
    // Counts each loopback packet once (transmit side), which covers both directions.
    foreach (var line in File.ReadLines("/proc/net/dev"))
    {
        var parts = line.Trim().Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 9 && parts[0] == "lo")
            return long.Parse(parts[9]);
    }
    return 0;
}

sealed class Bot
{
    public readonly DbConnection Conn;
    public bool Ready;
    public int MovesSent, Digs, Pickups, Errors;
    public readonly List<double> MoveLatenciesMs = new();

    readonly Random rng;
    readonly Queue<(long At, float X, float Z)> pendingMoves = new();
    Identity? me;
    uint myId;
    float x, z, heading, targetX, targetZ;
    int subChunkX = int.MinValue, subChunkZ = int.MinValue;
    SubscriptionHandle? sub;
    float digTimer, pickupTimer;

    public Bot(int index, string uri, string db, Compression compression)
    {
        rng = new Random(index);
        digTimer = (float)rng.NextDouble() * 3;
        pickupTimer = (float)rng.NextDouble() * 2;
        Conn = DbConnection.Builder()
            .WithUri(uri)
            .WithDatabaseName(db)
            .WithCompression(compression)
            .WithLightMode(Environment.GetEnvironmentVariable("LIGHT") == "1")
            .OnConnect((conn, identity, token) =>
            {
                me = identity;
                Subscribe(0, 0);
            })
            .OnConnectError(e => Console.WriteLine($"bot {index} connect error: {e.Message}"))
            .Build();

        // Round trip = from sending a Move until our own position comes back in a tick batch,
        // which is what other players see.
        Conn.Db.PlayerPos.OnUpdate += (ctx, _, row) =>
        {
            if (row.PlayerId != myId) return;
            while (pendingMoves.TryDequeue(out var sent))
            {
                if (MathF.Abs(sent.X - row.X) < 1e-3f && MathF.Abs(sent.Z - row.Z) < 1e-3f)
                {
                    MoveLatenciesMs.Add(Stopwatch.GetElapsedTime(sent.At).TotalMilliseconds);
                    break;
                }
            }
        };
        Conn.OnUnhandledReducerError += (ctx, e) => Errors++;
        Conn.Db.PlayerPos.OnInsert += (ctx, row) =>
        {
            if (Ready || me is not { } id || ctx.Db.Player.Identity.Find(id) is not { } mine) return;
            if (row.PlayerId != mine.PlayerId) return;
            myId = mine.PlayerId;
            x = row.X; z = row.Z;
            PickTarget();
            Ready = true;
        };
    }

    void Subscribe(int chunkX, int chunkZ)
    {
        if (chunkX == subChunkX && chunkZ == subChunkZ) return;
        subChunkX = chunkX; subChunkZ = chunkZ;
        int r = World.ViewRadiusChunks;
        string range = $"chunk_x >= {chunkX - r} AND chunk_x <= {chunkX + r} AND chunk_z >= {chunkZ - r} AND chunk_z <= {chunkZ + r}";
        var old = sub;
        // Subscribe to the new area before dropping the old one so nothing flickers.
        sub = Conn.SubscriptionBuilder()
            .OnApplied(_ => old?.Unsubscribe())
            .OnError((_, e) => Console.WriteLine($"subscription error: {e.Message}"))
            .Subscribe(new[]
            {
                "SELECT * FROM player WHERE online = true",
                $"SELECT * FROM player_pos WHERE {range}",
                $"SELECT * FROM world_item WHERE {range}",
                $"SELECT * FROM terrain_edit WHERE {range}",
                "SELECT * FROM my_inventory",
            });
    }

    void PickTarget()
    {
        float span = World.SpawnRadiusChunks * World.ChunkSize * World.CellSize;
        targetX = (float)(rng.NextDouble() * 2 - 1) * span;
        targetZ = (float)(rng.NextDouble() * 2 - 1) * span;
    }

    public void Step(float dt)
    {
        float dx = targetX - x, dz = targetZ - z;
        float dist = MathF.Sqrt(dx * dx + dz * dz);
        if (dist < 1f) PickTarget();
        else
        {
            float step = MathF.Min(dist, 4f * dt);
            x += dx / dist * step;
            z += dz / dist * step;
            heading = MathF.Atan2(dx, dz);
        }

        pendingMoves.Enqueue((Stopwatch.GetTimestamp(), x, z));
        Conn.Reducers.Move(x, z, heading);
        MovesSent++;
        Subscribe(World.ChunkOf(x), World.ChunkOf(z));

        digTimer -= dt;
        if (digTimer <= 0)
        {
            digTimer = 3f;
            Conn.Reducers.Dig(World.CellOf(x), World.CellOf(z));
            Digs++;
        }

        pickupTimer -= dt;
        if (pickupTimer <= 0)
        {
            pickupTimer = 2f;
            foreach (var item in Conn.Db.WorldItem.Iter())
            {
                float ix = item.X - x, iz = item.Z - z;
                if (ix * ix + iz * iz < World.Reach * World.Reach * 0.8f)
                {
                    Conn.Reducers.PickUp(item.Id);
                    Pickups++;
                    break;
                }
            }
        }
    }
}

sealed class ServerStats
{
    readonly int pid;
    ServerStats(int pid) => this.pid = pid;

    public static ServerStats? Find()
    {
        foreach (var dir in Directory.GetDirectories("/proc"))
        {
            if (!int.TryParse(Path.GetFileName(dir), out int pid)) continue;
            try
            {
                var cmd = File.ReadAllText(Path.Combine(dir, "cmdline"));
                if (cmd.Contains("spacetimedb-standalone")) return new ServerStats(pid);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return null;
    }

    public (double CpuSeconds, double RssMb) Sample()
    {
        var stat = File.ReadAllText($"/proc/{pid}/stat");
        var fields = stat[(stat.LastIndexOf(')') + 2)..].Split(' ');
        double ticks = double.Parse(fields[11]) + double.Parse(fields[12]);
        double rssKb = File.ReadLines($"/proc/{pid}/status")
            .Where(l => l.StartsWith("VmRSS:"))
            .Select(l => double.Parse(l.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]))
            .FirstOrDefault();
        return (ticks / 100.0, rssKb / 1024.0);
    }
}
