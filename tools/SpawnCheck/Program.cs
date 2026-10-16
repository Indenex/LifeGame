// Spawn check: a player walks well past the area around the origin, logs off, and
// logs back in. With the game's own subscriptions they must get their own position
// back right away and the area around it, instead of waiting at spawn forever
// (bugfix-1). Runs against a local server with the module freshly published.
// Exits non-zero if anything fails.
//
// Usage: dotnet run -- [uri=http://127.0.0.1:3000] [db=lifegame]
using System.Diagnostics;
using LifeGame;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

string uri = args.Length > 0 ? args[0] : "http://127.0.0.1:3000";
string db = args.Length > 1 ? args[1] : "lifegame";
var clock = Stopwatch.StartNew();
DbConnection conn = null!;
Identity? me = null;
string? token = null;

void Pump(int ms)
{
    var until = clock.Elapsed + TimeSpan.FromMilliseconds(ms);
    while (clock.Elapsed < until) { conn.FrameTick(); Thread.Sleep(5); }
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

void Connect()
{
    me = null;
    var builder = DbConnection.Builder()
        .WithUri(uri)
        .WithDatabaseName(db)
        .OnConnect((_, identity, t) => { me = identity; token = t; })
        .OnConnectError(e => Console.WriteLine($"connect error: {e.Message}"));
    if (token != null) builder = builder.WithToken(token);
    conn = builder.Build();
    conn.OnUnhandledReducerError += (_, e) => Console.WriteLine($"  reducer error: {e.Message}");
    WaitFor(() => me != null, 20, "connected");
}

bool Subscribe(string[] queries, int seconds, string what)
{
    bool applied = false;
    conn.SubscriptionBuilder()
        .OnApplied(_ => applied = true)
        .OnError((_, e) => Console.WriteLine($"  subscription error: {e.Message}"))
        .Subscribe(queries);
    WaitFor(() => applied, seconds, what);
    return applied;
}

PlayerPos? MyPos() =>
    conn.Db.Player.Identity.Find(me!.Value) is { } p ? conn.Db.PlayerPos.PlayerId.Find(p.PlayerId) : null;

string AreaAround(float x, float z) =>
    Subscriptions.Range(World.ChunkOf(x), World.ChunkOf(z), World.ViewRadiusChunks);

// A fresh player joins the way the game does: self first, then the area around it.
Connect();
Subscribe(Subscriptions.Self, 20, "own player subscribed");
Check(MyPos() != null, "own position arrives with the self subscription");
var start = MyPos()!;
Subscribe(Subscriptions.Area(AreaAround(start.X, start.Z)), 20, "area around spawn subscribed");

// Walk west until the old origin area (3 chunks either side of chunk 0) is far behind.
float targetX = -(World.ViewRadiusChunks + 2) * World.ChunkSize * World.CellSize, x = start.X, z = start.Z;
while (x > targetX)
{
    x = MathF.Max(targetX, x - 0.5f);
    conn.Reducers.Move(x, z, -MathF.PI / 2);
    Pump(100);
}
WaitFor(() => MyPos() is { } p && p.X < targetX + 1, 20, $"server has us out at x={targetX:F0}");
var far = MyPos()!;
Check(World.ChunkOf(far.X) < -World.ViewRadiusChunks, $"chunk {far.ChunkX} is outside the old area around the origin");

// Others see us sneak (anim-1); the row is the whole signal.
uint myId = conn.Db.Player.Identity.Find(me!.Value)!.PlayerId;
conn.Reducers.SetSneaking(true);
WaitFor(() => conn.Db.PlayerSneaking.PlayerId.Find(myId) != null, 10, "sneaking shows for others");
conn.Reducers.SetSneaking(true);
Pump(300);
Check(conn.Db.PlayerSneaking.Iter().Count(r => r.PlayerId == myId) == 1, "sneaking twice is still one row");

conn.Disconnect();
Pump(500);
Connect();

// The old client subscribed around the origin first and waited for its own row there.
Subscribe(Subscriptions.Area(Subscriptions.Range(0, 0, World.ViewRadiusChunks)), 20, "origin area subscribed (old behaviour)");
Check(MyPos() == null, "the origin area alone does not include us (this was the bug)");

// The game now subscribes to its own rows first, then the area around them.
Subscribe(Subscriptions.Self, 20, "own player subscribed after reconnect");
var back = MyPos();
Check(back != null && MathF.Abs(back.X - far.X) < 1.5f && MathF.Abs(back.Z - far.Z) < 1.5f,
    $"own position is where we logged off ({back?.X:F1}, {back?.Z:F1})");
Subscribe(Subscriptions.Area(AreaAround(back!.X, back.Z)), 20, "area around the saved position subscribed");
Check(conn.Db.PlayerPos.Iter().Any(p => p.PlayerId == back.PlayerId), "the area includes us too");
Check(conn.Db.PlayerSneaking.PlayerId.Find(myId) == null, "logging off stood us up again");
conn.Reducers.SetSneaking(true);
WaitFor(() => conn.Db.PlayerSneaking.PlayerId.Find(myId) != null, 10, "can sneak again");
conn.Reducers.SetSneaking(false);
WaitFor(() => conn.Db.PlayerSneaking.PlayerId.Find(myId) == null, 10, "standing up clears it");

// Walking still works from there.
float stepX = back.X + 3;
for (int i = 0; i < 10; i++) { conn.Reducers.Move(back.X + 0.3f * (i + 1), back.Z, MathF.PI / 2); Pump(100); }
WaitFor(() => MyPos() is { } p && p.X > stepX - 0.5f, 10, "can walk on after reconnecting");

Console.WriteLine($"All spawn checks passed in {clock.Elapsed.TotalSeconds:F0}s");
conn.Disconnect();
return 0;
