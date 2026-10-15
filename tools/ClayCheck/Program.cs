// End-to-end clay check: one bot connects to a freshly published server, digs
// clay, cleans and tempers it, lights a fire, shapes two pinch pots beside it,
// fires one while still damp (it must crack), lets the other dry, fires it, picks
// the fired pot up and fills it with water. Then it crushes the sherds into grog
// and tempers more clay with it. Takes about ten real minutes, because drying and
// firing take real time. Exits non-zero on the first failure.
//
// Usage: dotnet run -- [uri=http://127.0.0.1:3000] [db=lifegame]
//        dotnet run -- <uri> <db> --token=path --goto=x,z   (walk that player somewhere and stop)
//        dotnet run -- <uri> <db> --kiln   (instead: bricks and daub, build an updraft kiln blueprint, load and fire it;
//        about 12 minutes; needs the Kiln structure kind from Building and Claims on main)
using System.Diagnostics;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

string uri = args.Length > 0 ? args[0] : "http://127.0.0.1:3000";
string db = args.Length > 1 ? args[1] : "lifegame";
string? tokenPath = args.FirstOrDefault(a => a.StartsWith("--token="))?[8..];
string? goTo = args.FirstOrDefault(a => a.StartsWith("--goto="))?[7..];
bool kilnRun = args.Contains("--kiln");

Identity? me = null;
Fire fire = null!;
uint myId = 0;
bool subscribed = false;
string? lastError = null;
var news = new List<string>();
var builder = DbConnection.Builder()
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
                "SELECT * FROM clay_piece",
                "SELECT * FROM clay_news",
                "SELECT * FROM kiln",
                "SELECT * FROM structure",
                "SELECT * FROM terrain_edit",
                "SELECT * FROM my_inventory",
                "SELECT * FROM my_craft_job",
            });
    })
    .OnConnectError(e => Fail($"connect: {e.Message}"));
if (tokenPath != null) builder = builder.WithToken(File.ReadAllText(tokenPath).Trim());
var conn = builder.Build();
conn.OnUnhandledReducerError += (_, e) => lastError = e.Message;
conn.Db.ClayNews.OnInsert += (_, n) =>
{
    if (n.PlayerId == myId) { news.Add(n.Text); Log($"News: {n.Text}"); }
};

Pump(() => subscribed, 20, "subscription");
Pump(() => me is { } id && conn.Db.Player.Identity.Find(id) is not null, 10, "player row");
myId = conn.Db.Player.Identity.Find(me!.Value)!.PlayerId;
var start = conn.Db.PlayerPos.PlayerId.Find(myId)!;
float x = start.X, z = start.Z;

if (goTo != null)
{
    var parts = goTo.Split(',');
    WalkTo(float.Parse(parts[0]), float.Parse(parts[1]), 0.3f);
    Log($"Walked to {x:F1}, {z:F1}");
    conn.Disconnect();
    return 0;
}

var (clayX, clayZ) = Nearest((cx, cz) => World.IsClay(cx, cz), 0, 0);
var (waterX, waterZ) = Nearest((cx, cz) => World.BaseHeight(cx, cz) < World.WaterLevel - 0.1f, 0, 0);
Log($"Clay at {clayX}, {clayZ}; water at {waterX}, {waterZ}");

Gather(Item.Stone, 3);
Gather(Item.Stick, 10);
Gather(Item.Fiber, kilnRun ? 12u : 4u);

// Dig: every dig in a clay patch gives a lump of clay.
WalkTo(clayX, clayZ, 1.2f);
int cell = 0;
uint clayWanted = kilnRun ? 38u : 12u;
while (CountOf(Item.Clay) < clayWanted)
{
    int cx = clayX + cell % 5 - 2, cz = clayZ + cell / 5 % 5 - 2;
    if (cell >= 25 * 4) Fail("ran out of clay to dig");
    if (!World.IsClay(cx, cz) || DepthAt(cx, cz) >= World.MaxDigDepth - 1) { cell++; continue; }
    lastError = null;
    uint before = CountOf(Item.Clay);
    conn.Reducers.Dig(cx, cz);
    Pump(() => lastError != null || CountOf(Item.Clay) > before, 3, "a dig");
    if (lastError is { } err && !err.Contains("Still working") && !err.Contains("reach")) Fail($"dig: {err}");
    if (lastError != null && lastError.Contains("reach")) cell++;
    Pump(() => false, 0.65f, null);
}
Log($"Dug {CountOf(Item.Clay)} clay");

if (kilnRun) return KilnCheck();

Craft("clay.clean", 0, Item.CleanClay);
Craft("clay.clean", 0, Item.CleanClay);
Craft("clay.clean", 0, Item.CleanClay);
Craft("clay.clean", 0, Item.CleanClay);
Craft("clay.grit", 0, Item.Grit);
Craft("clay.temper_grit", 0, Item.TemperedClay);
Craft("clay.temper_grit", 0, Item.TemperedClay);
if (CountOf(Item.TemperedClay) != 4) Fail($"expected 4 tempered clay, have {CountOf(Item.TemperedClay)}");

// A fire near the spawn clearing, fed well enough for a firing.
float fireX = 2f, fireZ = 2f;
WalkTo(fireX, fireZ, 1.2f);
Craft("gather.tinder", 0, Item.Tinder);
int firesBefore = conn.Db.Fire.Count;
Craft("fire.hand_drill", 1f, null);
Pump(() => conn.Db.Fire.Count > firesBefore, 5, "a fire");
fire = conn.Db.Fire.Iter().First(f => f.LitBy == myId);
Log($"Fire at {fire.X:F1}, {fire.Z:F1}");
KeepFireGoing();

// Shape two pinch pots. They are set down at the fire's edge if shaped onto it.
int piecesBefore = conn.Db.ClayPiece.Count;
Craft("clay.pinch_pot", 0.9f, null);
Craft("clay.pinch_pot", 0.9f, null);
Pump(() => conn.Db.ClayPiece.Count >= piecesBefore + 2, 5, "two pots set down to dry");
var pots = conn.Db.ClayPiece.Iter().Where(p => p.MadeBy == myId).OrderBy(p => p.Id).ToList();
var damp = pots[0];
var good = pots[1];
Log($"Two wet pots at {damp.X:F1},{damp.Z:F1} and {good.X:F1},{good.Z:F1}; rate {good.DryRate * 480:F2}x");
if (DistanceTo(good.X, good.Z, fire.X, fire.Z) > ClayRules.FireReach) Fail("pot set down out of the fire's reach");

// Firing a damp pot is refused without confirmation, then cracks.
lastError = null;
conn.Reducers.TendClay(damp.Id, false);
Pump(() => lastError != null, 3, "a warning about a damp pot");
Log($"Warned: {lastError}");
lastError = null;
conn.Reducers.TendClay(damp.Id, true);
Pump(() => lastError != null || conn.Db.ClayPiece.Id.Find(damp.Id)?.Stage == (byte)ClayStage.Firing, 3, "the damp pot to go in the fire");
if (lastError != null) Fail($"firing damp pot: {lastError}");
Log("STAGE wet-pots-and-damp-firing");

// Wait for the other pot to dry, keeping the fire going.
var dryWatch = Stopwatch.StartNew();
while (MoistureOf(good.Id) > ClayRules.SafeToFire)
{
    if (dryWatch.Elapsed > TimeSpan.FromMinutes(10)) Fail("pot never dried");
    KeepFireGoing();
    Pump(() => false, 5, null);
    if ((int)dryWatch.Elapsed.TotalSeconds % 30 < 5) Log($"Drying: moisture {MoistureOf(good.Id):F2}");
}
Log($"Pot is bone dry after {dryWatch.Elapsed.TotalSeconds:F0} s");
Log("STAGE dry-pot");

// The damp pot must have cracked into sherds by now.
Pump(() => news.Any(n => n.Contains("Crack")), 60, "the damp pot to crack");
Log("Damp pot cracked into sherds, as it should");

KeepFireGoing();
lastError = null;
conn.Reducers.TendClay(good.Id, false);
Pump(() => lastError != null || conn.Db.ClayPiece.Id.Find(good.Id)?.Stage == (byte)ClayStage.Firing, 3, "the dry pot to go in the fire");
if (lastError != null) Fail($"firing: {lastError}");
Log("STAGE firing");
var fireWatch = Stopwatch.StartNew();
while (conn.Db.ClayPiece.Id.Find(good.Id) != null)
{
    if (fireWatch.Elapsed > TimeSpan.FromMinutes(6)) Fail("firing never finished");
    Pump(() => false, 2, null);
}
string result = news.Last();
Log($"Firing done: {result}");
Log("STAGE fired");

// Pick up what came out of the fire, and the sherds.
Gather(Item.Potsherd, 2);
bool whole = result.Contains("whole");
if (whole)
{
    Gather(Item.PinchPot, 1);
    WalkTo(waterX, waterZ, 1.5f);
    lastError = null;
    conn.Reducers.FillPots();
    Pump(() => lastError != null || CountOf(Item.PinchPotOfWater) > 0, 3, "a pot of water");
    if (lastError != null) Fail($"filling: {lastError}");
    var full = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.PinchPotOfWater);
    Log($"Filled the pot with water (quality {full.Quality}, {Processes.Grade(full.Quality)})");
    WalkTo(fire.X + 1.5f, fire.Z + 0.5f, 0.3f);
    conn.Reducers.Drop((ushort)Item.PinchPotOfWater, full.Quality);
    Log("STAGE pot-of-water");
}
else Log("The good pot cracked too (bad luck). Not a failure: cracking is a chance.");

Craft("clay.grog", 0, Item.Grog);
Craft("clay.clean", 0, Item.CleanClay);
Craft("clay.clean", 0, Item.CleanClay);
uint temperedBefore = CountOf(Item.TemperedClay);
Craft("clay.temper_grog", 0, Item.TemperedClay);
if (CountOf(Item.TemperedClay) - temperedBefore != 3) Fail("grog temper should give 3 tempered clay");
Log("Grog from the sherds tempered 3 more clay");

Log("Inventory:");
foreach (var row in conn.Db.MyInventory.Iter().OrderBy(r => r.Item))
    Log($"  {Items.Def((Item)row.Item).Count(row.Count)} x{row.Count} (quality {row.Quality})");
Log(whole ? "Clay check passed: dug clay to a fired pot of water" : "Clay check passed (pot cracked; rerun to see the water)");
conn.Disconnect();
return 0;

// The updraft kiln: bricks dry by a fire, the kiln is built from them, two pots
// dried in the open are loaded and fired in it.
int KilnCheck()
{
    while (CountOf(Item.CleanClay) < 19) Craft("clay.clean", 0, Item.CleanClay);
    Craft("clay.grit", 0, Item.Grit);
    Craft("clay.temper_grit", 0, Item.TemperedClay);

    // Two pots shaped in the open, away from any fire, so picking them up doesn't fire them.
    WalkTo(-4f, 6f, 0.5f);
    Craft("clay.pinch_pot", 0.9f, null);
    Craft("clay.pinch_pot", 0.9f, null);
    Craft("clay.daub", 0, Item.Daub);
    var kilnPots = conn.Db.ClayPiece.Iter().Where(p => p.MadeBy == myId && p.Form == (byte)ClayForm.PinchPot).Select(p => p.Id).ToList();
    if (kilnPots.Count != 2) Fail($"expected 2 pots drying, have {kilnPots.Count}");

    // Eight bricks round a fire, where they dry twice as fast.
    float fx = 2f, fz = 2f;
    WalkTo(fx, fz, 1.2f);
    Craft("gather.tinder", 0, Item.Tinder);
    int firesBefore = conn.Db.Fire.Count;
    Craft("fire.hand_drill", 1f, null);
    Pump(() => conn.Db.Fire.Count > firesBefore, 5, "a fire");
    fire = conn.Db.Fire.Iter().First(f => f.LitBy == myId);
    KeepFireGoing();
    for (int i = 0; i < 8; i++) Craft("clay.brick", 0, null);
    Log($"8 bricks drying round the fire; {CountOf(Item.Daub)} daub");
    Log("STAGE bricks");

    var watch = Stopwatch.StartNew();
    while (conn.Db.ClayPiece.Iter().Any(p => p.MadeBy == myId && p.Form == (byte)ClayForm.Brick))
    {
        if (watch.Elapsed > TimeSpan.FromMinutes(10)) Fail("bricks never dried");
        KeepFireGoing();
        Pump(() => false, 5, null);
    }
    Log($"Bricks dry after {watch.Elapsed.TotalSeconds:F0} s");
    Gather(Item.Brick, 8);

    // Lay out a kiln blueprint a few steps from the fire, door toward us, and
    // build it brick by brick.
    WalkTo(fx + 4f, fz + 1f, 0.3f);
    float kx = x + 1.8f, kz = z;
    lastError = null;
    conn.Reducers.PlaceStructure(StructureKind.Kiln, kx, kz, MathF.Atan2(x - kx, z - kz));
    Pump(() => lastError != null || conn.Db.Structure.Iter().Any(s => s.Kind == StructureKind.Kiln && s.Owner == me), 5, "the kiln blueprint");
    if (lastError != null) Fail($"placing the kiln: {lastError}");
    var site = conn.Db.Structure.Iter().First(s => s.Kind == StructureKind.Kiln && s.Owner == me);
    Log("STAGE kiln-blueprint");
    for (int i = 0; i < 20 && conn.Db.Structure.Id.Find(site.Id) is { Complete: false }; i++)
    {
        lastError = null;
        var before = conn.Db.Structure.Id.Find(site.Id)!;
        conn.Reducers.Build(site.Id);
        Pump(() => lastError != null || !ReferenceEquals(conn.Db.Structure.Id.Find(site.Id), before), 3, "a brick to go on");
        if (lastError != null) Fail($"building: {lastError}");
        Pump(() => false, 0.3f, null);
    }
    if (conn.Db.Structure.Id.Find(site.Id) is not { Complete: true }) Fail("kiln never finished");
    var kiln = conn.Db.Structure.Id.Find(site.Id)!;
    Log($"Kiln built at {kiln.X:F1}, {kiln.Z:F1}; {CountOf(Item.Brick)} bricks and {CountOf(Item.Daub)} daub left");
    Log("STAGE kiln-built");

    // Lighting an empty kiln is refused.
    lastError = null;
    conn.Reducers.UseKiln(kiln.Id);
    Pump(() => lastError != null, 3, "an empty kiln to be refused");
    Log($"Empty kiln: {lastError}");

    // Wait for the pots, pick them up, load them.
    watch.Restart();
    while (kilnPots.Any(id => MoistureOf(id) > ClayRules.SafeToFire))
    {
        if (watch.Elapsed > TimeSpan.FromMinutes(12)) Fail("pots never dried");
        Pump(() => false, 5, null);
    }
    foreach (var id in kilnPots)
    {
        var p = conn.Db.ClayPiece.Id.Find(id)!;
        WalkTo(p.X, p.Z, 1.2f);
        lastError = null;
        conn.Reducers.TendClay(id, false);
        Pump(() => lastError != null || conn.Db.ClayPiece.Id.Find(id) == null, 3, "picking up a dry pot");
        if (lastError != null) Fail($"picking up: {lastError}");
    }
    if (CountOf(Item.UnfiredPinchPot) != 2) Fail($"expected 2 unfired pots, have {CountOf(Item.UnfiredPinchPot)}");
    byte quality = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.UnfiredPinchPot).Quality;

    WalkTo(kiln.X, kiln.Z, 1.6f);
    lastError = null;
    conn.Reducers.UseKiln(kiln.Id);
    Pump(() => lastError != null || (conn.Db.Kiln.StructureId.Find(kiln.Id)?.Load?.Count ?? 0) == 2, 3, "the kiln to be loaded");
    if (lastError != null) Fail($"loading: {lastError}");
    Log("Loaded 2 pots");
    Log("STAGE kiln-loaded");

    if (CountOf(Item.Stick) < 2) Gather(Item.Stick, 2);
    WalkTo(kiln.X, kiln.Z, 1.6f);
    uint sticks = CountOf(Item.Stick);
    lastError = null;
    conn.Reducers.UseKiln(kiln.Id);
    Pump(() => lastError != null || conn.Db.Kiln.StructureId.Find(kiln.Id)?.State == 1, 3, "the kiln to light");
    if (lastError != null) Fail($"lighting: {lastError}");
    Log($"Kiln lit, burned {sticks - CountOf(Item.Stick)} sticks");
    Log("STAGE kiln-firing");

    watch.Restart();
    while (conn.Db.Kiln.StructureId.Find(kiln.Id)?.State == 1)
    {
        if (watch.Elapsed > TimeSpan.FromMinutes(5)) Fail("kiln never finished");
        Pump(() => false, 2, null);
    }
    Pump(() => news.Any(n => n.Contains("kiln has cooled")), 5, "kiln news");
    Log($"Kiln done after {watch.Elapsed.TotalSeconds:F0} s: {news.Last()}");
    Pump(() => false, 1, null);
    var fired = conn.Db.WorldItem.Iter().Where(w => w.Item == (ushort)Item.PinchPot && DistanceTo(w.X, w.Z, kiln.X, kiln.Z) < 2f).ToList();
    foreach (var w in fired)
        if (w.Quality != ClayRules.KilnFired(quality)) Fail($"kiln pot quality {w.Quality}, expected {ClayRules.KilnFired(quality)}");
    Log($"{fired.Count} kiln-fired pots at quality {ClayRules.KilnFired(quality)} (shaped at {quality})");
    Log("STAGE kiln-done");
    Log("Kiln check passed: bricks to a kiln to kiln-fired pots");
    conn.Disconnect();
    return 0;
}

void Feed(int sticks)
{
    for (int i = 0; i < sticks && CountOf(Item.Stick) > 0; i++)
    {
        lastError = null;
        conn.Reducers.FeedFire(fire.Id, (ushort)Item.Stick);
        Pump(() => false, 0.3f, null);
        if (lastError != null) Fail($"feeding the fire: {lastError}");
    }
}

void KeepFireGoing()
{
    var f = conn.Db.Fire.Id.Find(fire.Id) ?? throw new Exception("fire went out");
    double left = (f.FuelUntil.MicrosecondsSinceUnixEpoch - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000) / 1e6;
    while (left < ClayRules.PitFireSeconds + 30)
    {
        if (CountOf(Item.Stick) < 1) Gather(Item.Stick, 4);
        WalkTo(fire.X, fire.Z, 1.2f);
        Feed(1);
        left += Items.Def(Item.Stick).FuelSeconds;
    }
}

float MoistureOf(ulong id)
{
    var p = conn.Db.ClayPiece.Id.Find(id) ?? throw new Exception("pot vanished");
    double since = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 - p.MoistureAt.MicrosecondsSinceUnixEpoch) / 1e6;
    return ClayRules.MoistureAt(p.Moisture, p.DryRate, (float)since);
}

short DepthAt(int cx, int cz) => conn.Db.TerrainEdit.CellKey.Find(World.CellKey(cx, cz))?.Depth ?? 0;

(int, int) Nearest(Func<int, int, bool> match, int ox, int oz)
{
    for (int r = 0; r < 200; r++)
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
                if (Math.Max(Math.Abs(dx), Math.Abs(dz)) == r && match(ox + dx, oz + dz))
                    return (ox + dx, oz + dz);
    Fail("nothing found nearby");
    return (0, 0);
}

static float DistanceTo(float ax, float az, float bx, float bz) => MathF.Sqrt((ax - bx) * (ax - bx) + (az - bz) * (az - bz));

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
        Log($"{processId}: made {Items.Def(item).Name} x{CountOf(item) - before}");
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
        if (deadline.Elapsed > TimeSpan.FromSeconds(120)) Fail($"could not gather {want} {item}");
        var target = conn.Db.WorldItem.Iter()
            .Where(w => w.Item == (ushort)item)
            .OrderBy(w => (w.X - x) * (w.X - x) + (w.Z - z) * (w.Z - z))
            .FirstOrDefault();
        if (target == null) { Pump(() => false, 1, null); continue; }

        WalkTo(target.X, target.Z, 1.5f);
        uint before = CountOf(item);
        conn.Reducers.PickUp(target.Id);
        Pump(() => CountOf(item) > before || conn.Db.WorldItem.Id.Find(target.Id) == null, 3, "pickup");
    }
}

// Walks at a safe pace until within `near` of a spot.
void WalkTo(float tx, float tz, float near)
{
    while (true)
    {
        float dx = tx - x, dz = tz - z, d = MathF.Sqrt(dx * dx + dz * dz);
        if (d <= near) break;
        float step = Math.Min(d - near * 0.9f, 0.5f);
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

static void Log(string message) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");

static void Fail(string message)
{
    Console.Error.WriteLine($"Clay check FAILED: {message}");
    Environment.Exit(1);
}
