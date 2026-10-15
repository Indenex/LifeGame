// House building check: two players gather materials, one lays out a footprint,
// both build it to the end, stand inside it, and it is still there after a
// reconnect. Runs against a local server with the module freshly published.
// Exits non-zero if anything fails.
//
// Usage: dotnet run -- [lean-to|clay-hut|both=both] [uri=http://127.0.0.1:3000] [db=lifegame]
using System.Diagnostics;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

string which = args.Length > 0 ? args[0] : "both";
string uri = args.Length > 1 ? args[1] : "http://127.0.0.1:3000";
string db = args.Length > 2 ? args[2] : "lifegame";

var a = new Bot("A", uri, db, null);
var b = new Bot("B", uri, db, null);
var bots = new List<Bot> { a, b };
var clock = Stopwatch.StartNew();

void Pump(int ms)
{
    var until = clock.Elapsed + TimeSpan.FromMilliseconds(ms);
    while (clock.Elapsed < until)
    {
        foreach (var bot in bots) bot.Conn.FrameTick();
        Thread.Sleep(5);
    }
}

void Check(bool ok, string what)
{
    Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {what}");
    if (!ok) Environment.Exit(1);
}

void WaitFor(Func<bool> done, int seconds, string what)
{
    var until = clock.Elapsed + TimeSpan.FromSeconds(seconds);
    while (!done() && clock.Elapsed < until) Pump(20);
    Check(done(), what);
}

WaitFor(() => bots.All(x => x.Ready), 30, "both players connected");

if (which is "lean-to" or "both")
    BuildTogether(StructureKind.LeanTo,
        new() { [Item.Stick] = 5, [Item.Fiber] = 6 },
        new() { [Item.Stick] = 5, [Item.Fiber] = 5 });
if (which is "clay-hut" or "both")
    BuildTogether(StructureKind.ClayHut,
        new() { [Item.Stick] = 11, [Item.Stone] = 2, [Item.Fiber] = 9 },
        new() { [Item.Stick] = 11, [Item.Fiber] = 14, [Item.Clay] = 12 },
        // B turns the clay into daub for the walls: clean it, then mix in fiber.
        prepareB: () =>
        {
            for (int i = 0; i < 6; i++) b.Craft("clay.clean", Pump);
            for (int i = 0; i < 6; i++) b.Craft("clay.daub", Pump);
            Check(b.CountOf(Item.Daub) >= 12, $"mixed daub for the walls ({b.Carried()})");
        });

Console.WriteLine($"All building checks passed in {clock.Elapsed.TotalSeconds:F0}s");
foreach (var bot in bots) bot.Conn.Disconnect();
return 0;

void BuildTogether(StructureKind kind, Dictionary<Item, uint> forA, Dictionary<Item, uint> forB, Action? prepareB = null)
{
    string name = Structures.Shape((int)kind).Name;
    Console.WriteLine($"--- {name}");

    // Gather side by side.
    a.Wants = forA;
    b.Wants = forB;
    var until = clock.Elapsed + TimeSpan.FromSeconds(240);
    while (bots.Any(x => !x.HasWants) && clock.Elapsed < until)
    {
        foreach (var bot in bots) bot.GatherStep(0.1f);
        Pump(100);
    }
    Check(bots.All(x => x.HasWants), $"both gathered materials for the {name} ({a.Carried()} / {b.Carried()})");
    prepareB?.Invoke();

    // A lays out a footprint on dry, level ground nearby.
    var site = a.FindSite(kind);
    Check(site != null, "found level ground for a footprint");
    var (sx, sz) = site!.Value;
    a.WalkTo(sx, sz + 2.5f, 1f, Pump);
    int before = a.Conn.Db.Structure.Count;
    a.Conn.Reducers.PlaceStructure(kind, sx, sz, 0f);
    WaitFor(() => a.Conn.Db.Structure.Count > before, 10, "footprint laid out");
    var s = a.Conn.Db.Structure.Iter().OrderByDescending(r => r.PlacedAt.MicrosecondsSinceUnixEpoch).First();
    ulong id = s.Id;
    WaitFor(() => b.Conn.Db.Structure.Id.Find(id) != null, 10, "the other player sees the footprint");

    // Both stand at the open front and add what they carry until it stands.
    var front = Structures.ToWorld(sx, sz, s.Yaw, 0.6f, Structures.Shape((int)kind).Round ? 2.5f : 1.9f);
    b.WalkTo(front.x + 0.6f, front.z, 1f, Pump);
    a.WalkTo(front.x - 0.6f, front.z, 1f, Pump);
    until = clock.Elapsed + TimeSpan.FromSeconds(180);
    while (a.Conn.Db.Structure.Id.Find(id) is { Complete: false } && clock.Elapsed < until)
    {
        foreach (var bot in bots) bot.TryBuild(id);
        Pump(650);
    }
    Pump(300);
    var done = a.Conn.Db.Structure.Id.Find(id);
    Check(done is { Complete: true }, $"{name} finished (stage {done?.Stage})");
    Check(done!.Builders == 2, $"both players built it (builders: {done.Builders})");
    Check(b.Conn.Db.Structure.Id.Find(id) is { Complete: true }, "the other player sees it finished");

    // Walk in and check the spot counts as sheltered.
    a.WalkTo(sx, sz, 0.3f, Pump);
    Check(Structures.Contains((int)kind, done.X, done.Z, done.Yaw, a.X, a.Z, -0.2f), "standing inside is sheltered");

    // Reconnect and find it still there.
    a.Reconnect(Pump);
    WaitFor(() => a.Ready && a.Conn.Db.Structure.Id.Find(id) is { Complete: true }, 20, "still standing after reconnecting");
}

sealed class Bot
{
    public DbConnection Conn;
    public bool Ready;
    public float X, Z;
    public Dictionary<Item, uint> Wants = new();

    readonly string name, uri, db;
    string? token;
    Identity? me;
    uint myId;
    ulong targetItem;
    (int x, int z)? digCell;
    float actionTimer;

    public Bot(string name, string uri, string db, string? token)
    {
        this.name = name;
        this.uri = uri;
        this.db = db;
        this.token = token;
        Conn = Connect();
    }

    DbConnection Connect()
    {
        Ready = false;
        var builder = DbConnection.Builder()
            .WithUri(uri)
            .WithDatabaseName(db)
            .OnConnect((conn, identity, t) =>
            {
                me = identity;
                token = t;
                conn.SubscriptionBuilder()
                    .OnApplied(ctx =>
                    {
                        if (ctx.Db.Player.Identity.Find(identity) is not { } mine) return;
                        myId = mine.PlayerId;
                        if (ctx.Db.PlayerPos.PlayerId.Find(myId) is { } pos) { X = pos.X; Z = pos.Z; }
                        Ready = true;
                    })
                    .OnError((_, e) => Console.WriteLine($"{name} subscription error: {e.Message}"))
                    .Subscribe(new[]
                    {
                        "SELECT * FROM player",
                        "SELECT * FROM player_pos",
                        "SELECT * FROM world_item",
                        "SELECT * FROM terrain_edit",
                        "SELECT * FROM structure",
                        "SELECT * FROM my_inventory",
                        "SELECT * FROM my_craft_job",
                    });
            })
            .OnConnectError(e => Console.WriteLine($"{name} connect error: {e.Message}"));
        if (token != null) builder = builder.WithToken(token);
        var conn = builder.Build();
        conn.OnUnhandledReducerError += (_, e) =>
        {
            if (!e.Message.Contains("Still working")) Console.WriteLine($"  {name}: {e.Message}");
        };
        return conn;
    }

    public void Reconnect(Action<int> pump)
    {
        Conn.Disconnect();
        pump(500);
        Conn = Connect();
    }

    public uint CountOf(Item item) => Conn.Db.MyInventory.Iter().Where(i => i.Item == (ushort)item).Aggregate(0u, (n, i) => n + i.Count);

    public bool HasWants => Wants.All(w => CountOf(w.Key) >= w.Value);

    public string Carried() => string.Join(" ", Conn.Db.MyInventory.Iter().Select(i => $"{(Item)i.Item}:{i.Count}"));

    // Walks toward the nearest wanted item and picks it up; digs clay where the ground has it.
    public void GatherStep(float dt)
    {
        actionTimer -= dt;
        var missing = Wants.Where(w => CountOf(w.Key) < w.Value).Select(w => w.Key).ToHashSet();
        if (missing.Count == 0) return;

        if (missing.Contains(Item.Clay) && missing.Count == 1)
        {
            digCell ??= NearestClay();
            var (cx, cz) = digCell.Value;
            if (Step(cx * World.CellSize - 0.8f, cz * World.CellSize, dt) < 0.3f && actionTimer <= 0)
            {
                actionTimer = 0.7f;
                var depth = Conn.Db.TerrainEdit.CellKey.Find(World.CellKey(cx, cz))?.Depth ?? 0;
                if (depth >= World.MaxDigDepth - 1) digCell = null;
                else Conn.Reducers.Dig(cx, cz);
            }
            return;
        }

        var item = Conn.Db.WorldItem.Id.Find(targetItem);
        if (item == null || !missing.Contains((Item)item.Item))
        {
            item = Conn.Db.WorldItem.Iter()
                .Where(i => missing.Contains((Item)i.Item) && i.Item != (ushort)Item.Clay)
                .OrderBy(i => (i.X - X) * (i.X - X) + (i.Z - Z) * (i.Z - Z))
                .FirstOrDefault();
            targetItem = item?.Id ?? 0;
            if (item == null) return;
        }
        if (Step(item.X, item.Z, dt) < 1.2f && actionTimer <= 0)
        {
            actionTimer = 0.3f;
            Conn.Reducers.PickUp(item.Id);
        }
    }

    (int, int) NearestClay()
    {
        int x0 = World.CellOf(X), z0 = World.CellOf(Z);
        for (int r = 0; r < 80; r++)
            for (int x = x0 - r; x <= x0 + r; x++)
                for (int z = z0 - r; z <= z0 + r; z++)
                    if (Math.Max(Math.Abs(x - x0), Math.Abs(z - z0)) == r && World.IsClay(x, z)
                        && World.Height(x, z, 0) > Structures.WaterLevel + 0.4f)
                        return (x, z);
        throw new Exception("No clay nearby");
    }

    // Moves up to one tick's walk toward a point and returns the distance left.
    float Step(float tx, float tz, float dt)
    {
        float dx = tx - X, dz = tz - Z, dist = MathF.Sqrt(dx * dx + dz * dz);
        if (dist > 0.05f)
        {
            float step = MathF.Min(dist, 5f * dt);
            X += dx / dist * step;
            Z += dz / dist * step;
            Conn.Reducers.Move(X, Z, MathF.Atan2(dx, dz));
        }
        return MathF.Max(0, dist - 5f * dt);
    }

    public void WalkTo(float tx, float tz, float within, Action<int> pump)
    {
        while (Step(tx, tz, 0.1f) > within) pump(100);
        pump(300);
    }

    public (float, float)? FindSite(StructureKind kind)
    {
        float H(float x, float z) => Structures.GroundAt(x, z, (i, j) => Conn.Db.TerrainEdit.CellKey.Find(World.CellKey(i, j))?.Depth ?? 0);
        for (int r = 4; r < 40; r++)
            for (int k = 0; k < 16; k++)
            {
                float angle = k * MathF.Tau / 16, x = X + MathF.Cos(angle) * r, z = Z + MathF.Sin(angle) * r;
                if (Structures.GroundProblem((int)kind, x, z, 0f, H) != null) continue;
                if (Conn.Db.Structure.Iter().Any(s => Structures.TooClose((int)kind, x, z, (int)s.Kind, s.X, s.Z))) continue;
                return (x, z);
            }
        return null;
    }

    // Starts a craft and waits for it to finish.
    public void Craft(string processId, Action<int> pump)
    {
        Conn.Reducers.StartCraft(processId, 0.6f);
        var def = Processes.Find(processId)!;
        pump(200);
        var until = DateTime.UtcNow.AddSeconds(def.Seconds + 5);
        while (Conn.Db.MyCraftJob.Count > 0 && DateTime.UtcNow < until) pump(100);
        pump(200);
    }

    public void TryBuild(ulong id)
    {
        if (Conn.Db.Structure.Id.Find(id) is not { Complete: false } s) return;
        var carried = Conn.Db.MyInventory.Iter().Where(i => i.Count > 0).Select(i => i.Item).ToHashSet();
        if (s.Needs.Any(n => n.Have < n.Need && carried.Contains(n.Item)))
            Conn.Reducers.Build(id);
    }
}
