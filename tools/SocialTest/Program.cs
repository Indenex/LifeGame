// End-to-end check of chat, claims and claim permissions against a running local
// server: two players meet outside the waking clearing, one raises a claim, and the
// other is kept from digging or taking items there until invited in.
//
// Usage: dotnet run -- [uri=http://127.0.0.1:3000] [db=lifegame] [decay=0|1]
// Run it on a fresh database: it lays out a claim marker at (35, 30).
// With decay=1 it also waits for the claim to fall; run
// `spacetime call <db> set_claim_lifetime 30` as the database owner first.
using System.Diagnostics;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

string uri = args.Length > 0 ? args[0] : "http://127.0.0.1:3000";
string db = args.Length > 1 ? args[1] : "lifegame";
bool decay = args.Length > 2 && args[2] == "1";

var a = new Tester("A", uri, db);
var b = new Tester("B", uri, db);
var all = new[] { a, b };
int failures = 0;

void Pump(Func<bool> until, double seconds, string what)
{
    var sw = Stopwatch.StartNew();
    while (!until())
    {
        if (sw.Elapsed.TotalSeconds > seconds)
        {
            Console.WriteLine($"FAIL  timed out waiting for {what} (A: {a.LastError ?? "-"}, B: {b.LastError ?? "-"})");
            Environment.Exit(1);
        }
        foreach (var t in all) t.Tick();
        Thread.Sleep(5);
    }
}

bool Wait(Func<bool> until, double seconds)
{
    var sw = Stopwatch.StartNew();
    while (!until() && sw.Elapsed.TotalSeconds < seconds)
    {
        foreach (var t in all) t.Tick();
        Thread.Sleep(5);
    }
    return until();
}

void Check(bool ok, string what)
{
    Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {what}");
    if (!ok) failures++;
}

void ExpectError(Tester t, Action call, string contains, string what)
{
    t.LastError = null;
    call();
    Wait(() => t.LastError != null, 5);
    Check(t.LastError?.Contains(contains) == true, $"{what} ({t.LastError ?? "no error"})");
}

void Walk(Tester t, float x, float z) => Pump(() => t.StepToward(x, z), 60, $"{t.Name} walking");

Pump(() => a.Ready && b.Ready, 20, "connections");
Console.WriteLine($"connected A={a.PlayerId} at {a.X:F1},{a.Z:F1}  B={b.PlayerId}");

// A gathers what a marker needs.
foreach (var (kind, count) in new[] { (Item.Stick, (int)Social.MarkerSticks), (Item.Fiber, (int)Social.MarkerFiber), (Item.Stone, 1) })
{
    while (a.Count(kind) < count)
    {
        var item = a.Conn.Db.WorldItem.Iter().Where(i => i.Item == (ushort)kind)
            .OrderBy(i => (i.X - a.X) * (i.X - a.X) + (i.Z - a.Z) * (i.Z - a.Z)).First();
        Walk(a, item.X, item.Z);
        uint before = a.Count(kind);
        a.Conn.Reducers.PickUp(item.Id);
        Pump(() => a.Count(kind) > before || a.LastError != null, 5, "pick up");
        a.LastError = null;
    }
}
Console.WriteLine($"A gathered {a.Count(Item.Stick)} sticks, {a.Count(Item.Fiber)} fiber, {a.Count(Item.Stone)} stone");

// Markers are refused in the waking clearing.
Walk(a, 2, 2);
ExpectError(a, () => a.Conn.Reducers.PlaceStructure(StructureKind.ClaimMarker, 3, 2, 0), "clearing", "no claims in the waking clearing");

// A lays out a marker like any structure and builds it; the claim starts when it stands.
float cx = 34, cz = 30;
Walk(a, cx, cz);
Walk(b, cx + 4, cz);
a.LastError = null;
a.Conn.Reducers.PlaceStructure(StructureKind.ClaimMarker, cx + 1, cz, 0);
Pump(() => a.Conn.Db.Structure.Iter().Any(s => s.Kind == StructureKind.ClaimMarker) || a.LastError != null, 5, "marker laid out");
if (a.LastError != null) { Console.WriteLine($"FAIL  laying out the marker: {a.LastError}"); Environment.Exit(1); }
var site = a.Conn.Db.Structure.Iter().First(s => s.Kind == StructureKind.ClaimMarker);
Check(a.MyClaim() == null, "no claim until the marker is built");
ExpectError(b, () => b.Conn.Reducers.PlaceStructure(StructureKind.ClaimMarker, cx + 4, cz + 3, 0), "", "B cannot lay out a marker next to A's");
for (int i = 0; i < 20 && a.MyClaim() == null; i++)
{
    a.Conn.Reducers.Build(site.Id);
    Wait(() => a.MyClaim() != null, 0.7);
    a.LastError = null;
}
Pump(() => a.MyClaim() != null, 5, "claim raised");
var claim = a.MyClaim()!;
Check(a.Count(Item.Stick) == 0 && a.Count(Item.Fiber) == 0, "marker used up 4 sticks and 2 fiber");
Check(claim.StructureId == site.Id && claim.X == site.X && claim.Z == site.Z, "the claim sits on the finished marker");
ExpectError(b, () => b.Conn.Reducers.PlaceStructure(StructureKind.LeanTo, b.X + 2, b.Z + 2, 0), "claimed", "B cannot build inside A's claim");

// Chat reaches the player standing nearby.
a.Conn.Reducers.SendChat("hello neighbour");
Pump(() => b.Conn.Db.ChatMessage.Iter().Any(m => m.Text == "hello neighbour"), 5, "chat");
var heard = b.Conn.Db.ChatMessage.Iter().First(m => m.Text == "hello neighbour");
Check(new Vector(heard.X - b.X, heard.Z - b.Z).Length <= Social.ChatRange, "B hears A's chat within range");

// Outsiders cannot dig or take items inside the claim.
ExpectError(b, () => b.Conn.Reducers.Dig((int)MathF.Round(b.X), (int)MathF.Round(b.Z)), "claimed", "B cannot dig inside A's claim");
a.Conn.Reducers.Drop((ushort)Item.Stone, a.Quality(Item.Stone));
Pump(() => a.Count(Item.Stone) == 0, 5, "drop");
Pump(() => b.Conn.Db.WorldItem.Iter().Any(i => Social.InClaim(claim.X, claim.Z, i.X, i.Z) && i.Item == (ushort)Item.Stone), 5, "dropped stone visible");
var stone = b.Conn.Db.WorldItem.Iter().First(i => Social.InClaim(claim.X, claim.Z, i.X, i.Z) && i.Item == (ushort)Item.Stone);
Walk(b, stone.X, stone.Z);
ExpectError(b, () => b.Conn.Reducers.PickUp(stone.Id), "claimed", "B cannot pick up inside A's claim");
ExpectError(b, () => b.Conn.Reducers.TendClaim(claim.Id), "Only the owner", "B cannot tend A's marker");

// The owner can still dig at home.
a.LastError = null;
int digX = (int)MathF.Round(a.X) + 1, digZ = (int)MathF.Round(a.Z);
short depth = a.Depth(digX, digZ);
a.Conn.Reducers.Dig(digX, digZ);
Pump(() => a.Depth(digX, digZ) > depth || a.LastError != null, 5, "owner dig");
Check(a.LastError == null, "A can dig in their own claim");

// Once invited, B can dig, pick up and use the stash.
a.Conn.Reducers.AddClaimMember(b.PlayerId);
Pump(() => b.Conn.Db.ClaimMember.Iter().Any(m => m.PlayerId == b.PlayerId), 5, "membership");
b.LastError = null;
b.Conn.Reducers.PickUp(stone.Id);
Pump(() => b.Count(Item.Stone) == 1 || b.LastError != null, 5, "member pick up");
Check(b.Count(Item.Stone) == 1, $"member B can pick up inside the claim ({b.LastError ?? "ok"})");
Walk(b, claim.X + 1, claim.Z);
b.LastError = null;
b.Conn.Reducers.StashItem(claim.Id, (ushort)Item.Stone, stone.Quality);
Pump(() => a.Conn.Db.MyClaimStash.Iter().Any(s => s.Item == (ushort)Item.Stone) || b.LastError != null, 5, "stash");
Check(a.Conn.Db.MyClaimStash.Iter().Any(s => s.Item == (ushort)Item.Stone), $"B's stone shows in the owner's stash ({b.LastError ?? "ok"})");
b.Conn.Reducers.TakeFromStash(claim.Id, (ushort)Item.Stone, stone.Quality);
Pump(() => b.Count(Item.Stone) == 1 || b.LastError != null, 5, "take");
Check(b.Count(Item.Stone) == 1, "B can take from the stash");
b.Conn.Reducers.StashItem(claim.Id, (ushort)Item.Stone, stone.Quality);
Pump(() => b.Count(Item.Stone) == 0, 5, "stash again");

// A member may lay out a building on the claim.
float hx = claim.X + 6, hz = claim.Z + 5;
Walk(b, hx - 2, hz);
b.LastError = null;
b.Conn.Reducers.PlaceStructure(StructureKind.LeanTo, hx, hz, 0);
Pump(() => b.Conn.Db.Structure.Iter().Any(s => s.Kind == StructureKind.LeanTo && s.Owner == b.Conn.Identity) || b.LastError != null, 5, "member lays out");
var hut = b.Conn.Db.Structure.Iter().FirstOrDefault(s => s.Kind == StructureKind.LeanTo && s.Owner == b.Conn.Identity);
Check(hut != null, $"member B can lay out a building in the claim ({b.LastError ?? "ok"})");

// PvP needs the owner's say-so inside the claim.
Check(!claim.AllowPvp, "fighting is off in a new claim");
ExpectError(b, () => b.Conn.Reducers.SetClaimPvp(claim.Id, true), "Only the owner", "a member cannot allow fighting");
a.Conn.Reducers.SetClaimPvp(claim.Id, true);
Pump(() => a.MyClaim()?.AllowPvp == true, 5, "pvp on");
Check(true, "owner can allow fighting");

// Removing B locks them out again.
a.Conn.Reducers.RemoveClaimMember(b.PlayerId);
Pump(() => !b.Conn.Db.ClaimMember.Iter().Any(m => m.PlayerId == b.PlayerId), 5, "removal");
ExpectError(b, () => b.Conn.Reducers.TakeFromStash(claim.Id, (ushort)Item.Stone, stone.Quality), "not yours", "removed member cannot open the stash");

// The land's owner can clear anything on it; nobody else can take down the owner's things.
Walk(b, site.X + 1.5f, site.Z);
ExpectError(b, () => b.Conn.Reducers.Dismantle(site.Id), "take it down", "B cannot take down A's marker");
Walk(a, hx - 2, hz);
a.LastError = null;
a.Conn.Reducers.Dismantle(hut!.Id);
Pump(() => !a.Conn.Db.Structure.Iter().Any(s => s.Id == hut.Id) || a.LastError != null, 5, "owner clears");
Check(!a.Conn.Db.Structure.Iter().Any(s => s.Id == hut.Id), $"the owner can take down a member's building ({a.LastError ?? "ok"})");

// --- Fighting (claims-3). CI sets the newcomer grace to 0 before this runs.
float Hp(Tester t, uint id) => t.Conn.Db.PlayerHealth.PlayerId.Find(id)?.Health ?? -1f;
// Not on claimed land with fighting off.
a.Conn.Reducers.SetClaimPvp(claim.Id, false);
Pump(() => a.MyClaim()?.AllowPvp == false, 5, "pvp off");
Walk(a, claim.X + 4, claim.Z);
Walk(b, claim.X + 5, claim.Z);
ExpectError(b, () => b.Conn.Reducers.StrikePlayer(a.PlayerId), "does not allow fighting", "no fighting on claimed land unless the owner allows it");
// Open in the wild.
float wx = claim.X + Social.ClaimRadius + 8, wz = claim.Z;
Walk(a, wx, wz);
Walk(b, wx + 4, wz);
ExpectError(b, () => b.Conn.Reducers.StrikePlayer(a.PlayerId), "Too far", "a strike out of reach misses");
Walk(b, wx + 1, wz);
float full = Hp(b, a.PlayerId);
b.LastError = null;
b.Conn.Reducers.StrikePlayer(a.PlayerId);
Pump(() => Hp(b, a.PlayerId) < full || b.LastError != null, 5, "strike lands");
Check(Hp(b, a.PlayerId) < full, $"a strike in the wild lands ({full:F1} -> {Hp(b, a.PlayerId):F1}, {b.LastError ?? "ok"})");
ExpectError(b, () => b.Conn.Reducers.StrikePlayer(a.PlayerId), "Too soon", "strikes have a short cooldown");
// Strike until A falls: A's things drop and A wakes elsewhere.
uint carried = (uint)a.Conn.Db.MyInventory.Iter().Sum(i => i.Count);
for (int i = 0; i < 40 && !b.Conn.Db.ChatMessage.Iter().Any(m => m.SenderId == 0 && m.Text.Contains("struck down")); i++)
{
    var until = DateTime.UtcNow.AddSeconds(0.85);
    Pump(() => DateTime.UtcNow > until, 2, "cooldown");
    b.LastError = null;
    b.Conn.Reducers.StrikePlayer(a.PlayerId);
    Pump(() => true, 1, "tick");
}
Pump(() => b.Conn.Db.ChatMessage.Iter().Any(m => m.SenderId == 0 && m.Text.Contains("struck down")), 5, "struck down");
Check(true, "a player can be struck down in the wild, and nearby players hear of it");
Check(b.Conn.Db.PlayerHealth.PlayerId.Find(b.PlayerId)?.MarkedUntil.MicrosecondsSinceUnixEpoch > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000,
    "whoever struck them down is marked for a while");
Pump(() => a.Conn.Db.MyInventory.Iter().Sum(i => i.Count) == 0, 5, "drop on death");
Check(carried == 0 || a.Conn.Db.MyInventory.Iter().Sum(i => i.Count) == 0, $"being struck down drops what you carry ({carried} things)");
var woke = a.Conn.Db.PlayerPos.PlayerId.Find(a.PlayerId)!;
Check(MathF.Abs(woke.X - wx) + MathF.Abs(woke.Z - wz) > 5, "the fallen player wakes somewhere else");
Check(Hp(b, a.PlayerId) >= Social.MaxHealth, "and wakes with full health");
a.X = woke.X; a.Z = woke.Z;

if (decay)
{
    var expires = a.MyClaim()!.ExpiresAt.MicrosecondsSinceUnixEpoch;
    Console.WriteLine($"Waiting for the marker to fall ({(expires - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000) / 1_000_000}s left plus up to 20s for the decay tick)...");
    Pump(() => a.MyClaim() == null, 120, "decay");
    Check(true, "untended claim fell");
    Pump(() => !a.Conn.Db.Structure.Iter().Any(s => s.Id == site.Id), 5, "marker gone");
    Check(true, "the marker post went with it");
    Pump(() => a.Conn.Db.WorldItem.Iter().Any(i => i.Item == (ushort)Item.Stone && Social.InClaim(claim.X, claim.Z, i.X, i.Z)), 5, "spill");
    Check(true, "the stash spilled onto the ground");
    b.LastError = null;
    digX = (int)MathF.Round(b.X); digZ = (int)MathF.Round(b.Z) + 1;
    depth = b.Depth(digX, digZ);
    b.Conn.Reducers.Dig(digX, digZ);
    Pump(() => b.Depth(digX, digZ) > depth || b.LastError != null, 5, "dig after decay");
    Check(b.LastError == null, $"anyone can dig once it has fallen ({b.LastError ?? "ok"})");
}
else
{
    Walk(a, site.X + 1, site.Z);
    a.Conn.Reducers.Dismantle(site.Id);
    Pump(() => a.MyClaim() == null, 5, "dismantle");
    Check(true, "dismantling the marker ends the claim");
}

Console.WriteLine(failures == 0 ? "All social checks passed" : $"{failures} checks failed");
// The SDK keeps background threads alive, so leave explicitly.
Environment.Exit(failures == 0 ? 0 : 1);

readonly record struct Vector(float X, float Z)
{
    public float Length => MathF.Sqrt(X * X + Z * Z);
}

sealed class Tester
{
    public readonly string Name;
    public readonly DbConnection Conn;
    public bool Ready;
    public string? LastError;
    public uint PlayerId;
    public float X, Z;
    Identity? me;
    readonly Stopwatch clock = Stopwatch.StartNew();
    double lastMove;

    public Tester(string name, string uri, string db)
    {
        Name = name;
        Conn = DbConnection.Builder()
            .WithUri(uri)
            .WithDatabaseName(db)
            .OnConnect((conn, identity, _) =>
            {
                me = identity;
                conn.SubscriptionBuilder()
                    .OnApplied(ctx =>
                    {
                        var mine = ctx.Db.Player.Identity.Find(identity)!;
                        PlayerId = mine.PlayerId;
                        var pos = ctx.Db.PlayerPos.PlayerId.Find(PlayerId)!;
                        X = pos.X; Z = pos.Z;
                        Ready = true;
                    })
                    .Subscribe(new[]
                    {
                        "SELECT * FROM player", "SELECT * FROM player_pos", "SELECT * FROM world_item",
                        "SELECT * FROM terrain_edit", "SELECT * FROM my_inventory", "SELECT * FROM claim",
                        "SELECT * FROM claim_member", "SELECT * FROM my_claim_stash", "SELECT * FROM chat_message", "SELECT * FROM structure", "SELECT * FROM player_health",
                    });
            })
            .OnConnectError(e => Console.WriteLine($"{name} connect error: {e.Message}"))
            .Build();
        Conn.OnUnhandledReducerError += (_, e) => LastError = e.Message;
    }

    public void Tick() => Conn.FrameTick();

    public uint Count(Item item) => (uint)Conn.Db.MyInventory.Iter().Where(i => i.Item == (ushort)item).Sum(i => i.Count);

    public byte Quality(Item item) => Conn.Db.MyInventory.Iter().First(i => i.Item == (ushort)item).Quality;

    public short Depth(int x, int z) => Conn.Db.TerrainEdit.CellKey.Find(World.CellKey(x, z))?.Depth ?? 0;

    public Claim? MyClaim() => Conn.Db.Claim.Iter().FirstOrDefault(c => c.OwnerPlayerId == PlayerId);

    // One 10 Hz step toward a point; true once the server has us close enough.
    public bool StepToward(float tx, float tz)
    {
        var pos = Conn.Db.PlayerPos.PlayerId.Find(PlayerId);
        if (pos != null && MathF.Abs(pos.X - tx) < 1f && MathF.Abs(pos.Z - tz) < 1f && MathF.Abs(X - tx) < 0.5f && MathF.Abs(Z - tz) < 0.5f)
            return true;
        if (clock.Elapsed.TotalSeconds - lastMove < 0.1) return false;
        lastMove = clock.Elapsed.TotalSeconds;
        float dx = tx - X, dz = tz - Z, d = MathF.Sqrt(dx * dx + dz * dz);
        float step = MathF.Min(d, 5f * 0.1f);
        if (d > 0.001f) { X += dx / d * step; Z += dz / d * step; }
        Conn.Reducers.Move(X, Z, 0);
        return false;
    }
}
