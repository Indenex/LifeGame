// End-to-end clay check: one bot connects to a freshly published server, digs
// clay, makes a stone axe, chops a log, claims land past the spawn clearing and
// builds a potter's bench there (every mode does this; pots are shaped at the
// bench, D-112). Then it cleans and tempers the clay, shapes two pinch pots at the
// bench, lights a fire beside them,
// fires one while still damp (it must crack), lets the other dry, fires it, picks
// the fired pot up and fills it with water. Then it crushes the sherds into grog
// and tempers more clay with it. Takes about ten real minutes, because drying and
// firing take real time. Exits non-zero on the first failure.
//
// Usage: dotnet run -- [uri=http://127.0.0.1:3000] [db=lifegame]
//        dotnet run -- <uri> <db> --token=path --goto=x,z   (walk that player somewhere and stop)
//        dotnet run -- <uri> <db> --kiln   (instead: bricks and daub, build an updraft kiln blueprint, load and fire it;
//        about 12 minutes; needs the Kiln structure kind from Building and Claims on main)
//        dotnet run -- <uri> <db> --kiln --jar   (the kiln run plus a storage jar: a campfire refuses it,
//        the kiln fires it, it is set down from the build menu and stores a stack; ceramics-6)
//        dotnet run -- <uri> <db> --pot   (instead: a coil pot fired in a campfire and filled, berries
//        picked, berry broth simmered by the fire, taken off and eaten, the pot comes back; ceramics-7)
using System.Diagnostics;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

string uri = args.Length > 0 ? args[0] : "http://127.0.0.1:3000";
string db = args.Length > 1 ? args[1] : "lifegame";
string? tokenPath = args.FirstOrDefault(a => a.StartsWith("--token="))?[8..];
string? goTo = args.FirstOrDefault(a => a.StartsWith("--goto="))?[7..];
bool jarRun = args.Contains("--jar");
bool potRun = args.Contains("--pot");
bool lampRun = args.Contains("--lamp");
bool tileRun = args.Contains("--tile");
bool tallowRun = Environment.GetEnvironmentVariable("CLAYCHECK_TALLOW") == "1";
bool kilnRun = jarRun || args.Contains("--kiln");

Identity? me = null;
Fire fire = null!;
Structure bench = null!, marker = null!;
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
                "SELECT * FROM basket_item",
                "SELECT * FROM simmer_pot",
                "SELECT * FROM lamp",
                "SELECT * FROM trough_water",
                "SELECT * FROM player_mark",
                "SELECT * FROM animal",
                "SELECT * FROM animal_carcass",
                "SELECT * FROM forage_bush",
                "SELECT * FROM terrain_edit",
                "SELECT * FROM chopped_tree",
                "SELECT * FROM claim",
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
var ground = new BotGround(conn);
var start = conn.Db.PlayerPos.PlayerId.Find(myId)!;
float x = start.X, z = start.Z;

// Every run begins from the same place (bugfix-31): new players wake at a random spot in the
// spawn clearing, so walk to the origin first, at a pace the server accepts.
for (var go = Stopwatch.StartNew(); go.Elapsed.TotalSeconds < 60; )
{
    float dx = -x, dz = -z, d = MathF.Sqrt(dx * dx + dz * dz);
    if (d < 0.05f && conn.Db.PlayerPos.PlayerId.Find(myId) is { } at && MathF.Abs(at.X - x) + MathF.Abs(at.Z - z) < 0.1f) break;
    if (d >= 0.05f && conn.Db.PlayerPos.PlayerId.Find(myId) is { } seen0 && MathF.Abs(seen0.X - x) + MathF.Abs(seen0.Z - z) < 1.5f)
    {
        float step = Math.Min(d, 0.5f);
        x += dx / d * step; z += dz / d * step;
    }
    conn.Reducers.Move(x, z, MathF.Atan2(dx, dz));
    Pump(() => false, 0.1f, null);
}

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

// Stones: a hammer that is never used up, one for the hand axe, two for the
// potter's bench and up to two for grit. Sticks and fiber: also the axe's haft and
// cordage, and the claim marker.
Gather(Item.Stone, jarRun ? 14u : 6u);
Gather(Item.Stick, 10 + 1 + Social.MarkerSticks);
Gather(Item.Fiber, (kilnRun ? 12u : 4u) + 3 + Social.MarkerFiber);

// Dig: every dig in a clay patch gives a lump of clay.
WalkTo(clayX, clayZ, 1.2f);
int cell = 0;
uint clayWanted = jarRun ? 70u : kilnRun ? 38u : potRun ? 12u : lampRun ? (tallowRun ? 12u : 4u) : 12u;
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

// Pots are shaped at a potter's bench inside a claim (D-112): make a stone axe, chop
// a log, claim land past the spawn clearing and build the bench there. All the clay
// work below happens round the bench.
SetUpBench();

if (kilnRun) return KilnCheck();
if (potRun) return PotCheck();
if (lampRun) return LampCheck();
if (tileRun) return TileCheck();

Craft("clay.clean", 0, Item.CleanClay);
Craft("clay.clean", 0, Item.CleanClay);
Craft("clay.clean", 0, Item.CleanClay);
Craft("clay.clean", 0, Item.CleanClay);
Craft("clay.grit", 0, Item.Grit);
Craft("clay.temper_grit", 0, Item.TemperedClay);
Craft("clay.temper_grit", 0, Item.TemperedClay);
if (CountOf(Item.TemperedClay) != 4) Fail($"expected 4 tempered clay, have {CountOf(Item.TemperedClay)}");

// Shape two pinch pots at the bench; they are set down beside it, on our side.
AtBench();
int piecesBefore = conn.Db.ClayPiece.Count;
Craft("clay.pinch_pot", 0.9f, null);
Craft("clay.pinch_pot", 0.9f, null);
Pump(() => conn.Db.ClayPiece.Count >= piecesBefore + 2, 5, "two pots set down to dry");
var pots = conn.Db.ClayPiece.Iter().Where(p => p.MadeBy == myId).OrderBy(p => p.Id).ToList();
var damp = pots[0];
var good = pots[1];

// A fire right beside the wet pots, off the bench, fed well enough for a firing.
LightFireNear(pots.Average(p => p.X) + 1.3f, pots.Average(p => p.Z));
Log($"Fire at {fire.X:F1}, {fire.Z:F1}");
KeepFireGoing();
Log($"Two wet pots at {damp.X:F1},{damp.Z:F1} and {good.X:F1},{good.Z:F1}; rate {good.DryRate * 480:F2}x");
if (DistanceTo(good.X, good.Z, fire.X, fire.Z) > ClayRules.FireReach) Fail("pot set down out of the fire's reach");

// Firing a damp pot is refused without confirmation, then cracks.
WalkTo(damp.X, damp.Z, 1.2f);
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
WalkTo(good.X, good.Z, 1.2f);
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
    while (CountOf(Item.CleanClay) < (jarRun ? 33 : 19)) Craft("clay.clean", 0, Item.CleanClay);
    Craft("clay.grit", 0, Item.Grit);
    Craft("clay.temper_grit", 0, Item.TemperedClay);
    if (jarRun)
    {
        Craft("clay.grit", 0, Item.Grit);
        Craft("clay.temper_grit", 0, Item.TemperedClay);
        Craft("clay.temper_grit", 0, Item.TemperedClay);
        Craft("clay.temper_grit", 0, Item.TemperedClay); // two more tempered clay, for a roof tile
        // Six more tempered clay for a clay trough (ceramics-13): three batches, two more grit.
        Craft("clay.grit", 0, Item.Grit);
        Craft("clay.grit", 0, Item.Grit);
        Craft("clay.temper_grit", 0, Item.TemperedClay);
        Craft("clay.temper_grit", 0, Item.TemperedClay);
        Craft("clay.temper_grit", 0, Item.TemperedClay);
    }

    // Two pots shaped at the bench, away from any fire, so picking them up doesn't fire them.
    AtBench();
    Craft("clay.pinch_pot", 0.9f, null);
    Craft("clay.pinch_pot", 0.9f, null);
    Craft("clay.daub", 0, Item.Daub);
    var kilnPots = conn.Db.ClayPiece.Iter().Where(p => p.MadeBy == myId && p.Form == (byte)ClayForm.PinchPot).Select(p => p.Id).ToList();
    if (kilnPots.Count != 2) Fail($"expected 2 pots drying, have {kilnPots.Count}");
    ulong jarPiece = 0, tilePiece = 0, troughPiece = 0;
    if (jarRun)
    {
        AtBench();
        Craft("clay.storage_jar", 0.9f, null);
        jarPiece = conn.Db.ClayPiece.Iter().FirstOrDefault(p => p.MadeBy == myId && p.Form == (byte)ClayForm.StorageJar)?.Id ?? 0;
        if (jarPiece == 0) Fail("no storage jar drying");
        Log("STAGE jar-drying");
        // A roof tile too (ceramics-10): kiln-fired only, like the jar.
        Craft("clay.tile", 0.9f, null);
        tilePiece = conn.Db.ClayPiece.Iter().FirstOrDefault(p => p.MadeBy == myId && p.Form == (byte)ClayForm.Tile)?.Id ?? 0;
        if (tilePiece == 0) Fail("no roof tile drying");
        Log("STAGE tile-drying");
        // And a clay trough (ceramics-13), kiln-fired only too.
        Craft("clay.trough", 0.9f, null);
        troughPiece = conn.Db.ClayPiece.Iter().FirstOrDefault(p => p.MadeBy == myId && p.Form == (byte)ClayForm.Trough)?.Id ?? 0;
        if (troughPiece == 0) Fail("no clay trough drying");
        Log("STAGE trough-drying");
    }

    // Eight bricks round a fire, where they dry twice as fast. Bricks need no bench;
    // the fire is well clear of the pots by the bench.
    LightFireNear(bench.X + 4f, bench.Z + 1.3f);
    foreach (var id in kilnPots.Append(jarPiece).Append(tilePiece).Append(troughPiece).Where(id => id != 0))
        if (conn.Db.ClayPiece.Id.Find(id) is { } p && DistanceTo(p.X, p.Z, fire.X, fire.Z) <= ClayRules.FireReach + 0.5f)
            Fail("the brick fire is lit too close to the drying pots");
    KeepFireGoing();
    WalkTo(fire.X, fire.Z + 1.2f, 0.2f);
    Face(fire.X, fire.Z);
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
    var kilnSite = FindSite(StructureKind.Kiln, fire.X + 3.5f, fire.Z, -MathF.PI / 2, (sx, sz) =>
        DistanceTo(sx, sz, fire.X, fire.Z) >= 3f
        && conn.Db.ClayPiece.Iter().All(p => DistanceTo(sx, sz, p.X, p.Z) >= 2.5f)
        && InMyClaim(sx, sz, 2f) && ClearGround(sx, sz, -2, 2, -2, 2)) ?? FailAt("no ground for the kiln");
    WalkTo(kilnSite.X - 1.8f, kilnSite.Z, 0.2f);
    float kx = kilnSite.X, kz = kilnSite.Z;
    lastError = null;
    conn.Reducers.PlaceStructure(StructureKind.Kiln, kx, kz, MathF.Atan2(x - kx, z - kz));
    Pump(() => lastError != null || conn.Db.Structure.Iter().Any(s => s.Kind == StructureKind.Kiln && s.Owner == me), 5, "the kiln blueprint");
    if (lastError != null) Fail($"placing the kiln: {lastError}");
    var site = conn.Db.Structure.Iter().First(s => s.Kind == StructureKind.Kiln && s.Owner == me);
    Log("STAGE kiln-blueprint");
    for (int i = 0; i < 80 && conn.Db.Structure.Id.Find(site.Id) is { Complete: false }; i++)
    {
        lastError = null;
        var before = conn.Db.Structure.Id.Find(site.Id)!;
        conn.Reducers.Build(site.Id);
        Pump(() => lastError != null || !ReferenceEquals(conn.Db.Structure.Id.Find(site.Id), before), 3, "a brick to go on");
        // Building paces each addition like any other action; wait and add again.
        if (lastError == "Still working") { Pump(() => false, 0.5f, null); continue; }
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
    while (kilnPots.Append(jarPiece).Append(tilePiece).Append(troughPiece).Where(id => id != 0).Any(id => MoistureOf(id) > ClayRules.SafeToFire))
    {
        if (watch.Elapsed > TimeSpan.FromMinutes(12)) Fail("pots never dried");
        Pump(() => false, 5, null);
    }
    if (jarRun)
    {
        // The jar first, then a campfire must refuse it.
        var jp = conn.Db.ClayPiece.Id.Find(jarPiece)!;
        WalkTo(jp.X, jp.Z, 1.2f);
        lastError = null;
        conn.Reducers.TendClay(jarPiece, false);
        Pump(() => lastError != null || conn.Db.ClayPiece.Id.Find(jarPiece) == null, 3, "picking up the dry jar");
        if (lastError != null) Fail($"picking up the jar: {lastError}");
        if (CountOf(Item.UnfiredStorageJar) != 1) Fail("expected an unfired storage jar");
        KeepFireGoing();
        WalkTo(fire.X, fire.Z, 1.2f);
        lastError = null;
        conn.Reducers.FirePots(fire.Id);
        Pump(() => lastError != null, 3, "the campfire to refuse the jar");
        if (lastError == null || !lastError.Contains("kiln")) Fail($"campfire took the jar: {lastError}");
        Log($"Campfire: {lastError}");
        // The tile is picked up too, and also refused by a campfire.
        var tp = conn.Db.ClayPiece.Id.Find(tilePiece)!;
        WalkTo(tp.X, tp.Z, 1.2f);
        lastError = null;
        conn.Reducers.TendClay(tilePiece, false);
        Pump(() => lastError != null || conn.Db.ClayPiece.Id.Find(tilePiece) == null, 3, "picking up the dry tile");
        if (lastError != null) Fail($"picking up the tile: {lastError}");
        if (CountOf(Item.UnfiredRoofTile) != 1) Fail("expected an unfired roof tile");
        // And the trough.
        var trp = conn.Db.ClayPiece.Id.Find(troughPiece)!;
        WalkTo(trp.X, trp.Z, 1.2f);
        lastError = null;
        conn.Reducers.TendClay(troughPiece, false);
        Pump(() => lastError != null || conn.Db.ClayPiece.Id.Find(troughPiece) == null, 3, "picking up the dry trough");
        if (lastError != null) Fail($"picking up the trough: {lastError}");
        if (CountOf(Item.UnfiredClayTrough) != 1) Fail("expected an unfired clay trough");
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
    // The two pots can differ by a point and sit in separate stacks.
    var loaded = conn.Db.MyInventory.Iter().Where(r => r.Item == (ushort)Item.UnfiredPinchPot)
        .SelectMany(r => Enumerable.Repeat(ClayRules.KilnFired(r.Quality), (int)r.Count)).OrderBy(q => q).ToList();

    WalkTo(kiln.X, kiln.Z, 1.6f);
    lastError = null;
    conn.Reducers.UseKiln(kiln.Id);
    int loadWanted = jarRun ? 5 : 2;
    Pump(() => lastError != null || (conn.Db.Kiln.StructureId.Find(kiln.Id)?.Load?.Count ?? 0) == loadWanted, 3, "the kiln to be loaded");
    if (lastError != null) Fail($"loading: {lastError}");
    Log($"Loaded {loadWanted} pieces");
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
    var got = fired.Select(w => w.Quality).OrderBy(q => q).ToList();
    if (!got.SequenceEqual(loaded)) Fail($"kiln pot qualities {string.Join(",", got)}, expected {string.Join(",", loaded)}");
    Log($"{fired.Count} kiln-fired pots at quality {string.Join(",", got)}");
    Log("STAGE kiln-done");
    if (jarRun)
    {
        // The roof tile comes out by the door as a fired roof tile (or sherds, 2% of the time).
        var tile = conn.Db.WorldItem.Iter().FirstOrDefault(w => w.Item == (ushort)Item.RoofTile && DistanceTo(w.X, w.Z, kiln.X, kiln.Z) < 2.5f);
        if (tile == null) Log("The roof tile cracked in the kiln (2% chance); tile not checked this run");
        else Log($"Kiln-fired roof tile at quality {tile.Quality}");
        if (tile != null) Log("STAGE tile-fired");
    }
    if (jarRun) JarCheck(kiln);
    if (jarRun) TroughCheck(kiln);
    Log("Kiln check passed: bricks to a kiln to kiln-fired pots");
    conn.Disconnect();
    return 0;
}

// Cooking in a pot (ceramics-7): a coil pot dried by a fire and fired in it,
// filled at the water, berries picked, broth simmered, taken off and eaten.
// Clay oil lamps (ceramics-9): two lamps pinched at the bench and pit-fired; setting
// one down with no tallow is refused. Tallow comes from raw mutton, which only Animals
// hands out: with CLAYCHECK_TALLOW=1, put 2 raw mutton in this player's bag (the
// identity is printed) and the run renders tallow in a pot, lights a lamp and feeds it.
int LampCheck()
{
    // The tallow run also needs a coil pot to render in; it dries and fires with the lamps.
    int cleanWanted = tallowRun ? 6 : 2;
    while (CountOf(Item.CleanClay) < cleanWanted) Craft("clay.clean", 0, Item.CleanClay);
    Craft("clay.grit", 0, Item.Grit);
    Craft("clay.temper_grit", 0, Item.TemperedClay);
    if (tallowRun)
    {
        Craft("clay.grit", 0, Item.Grit);
        Craft("clay.temper_grit", 0, Item.TemperedClay);
        Craft("clay.temper_grit", 0, Item.TemperedClay);
    }

    AtBench();
    Craft("clay.lamp", 0.9f, null);
    Craft("clay.lamp", 0.9f, null);
    if (tallowRun) Craft("clay.coil_pot", 0.9f, null);
    var lampPieces = conn.Db.ClayPiece.Iter().Where(p => p.MadeBy == myId && p.Form == (byte)ClayForm.Lamp).ToList();
    if (lampPieces.Count != 2) Fail($"expected 2 lamps drying, have {lampPieces.Count}");
    var potPieces = conn.Db.ClayPiece.Iter().Where(p => p.MadeBy == myId && p.Form == (byte)ClayForm.CoilPot).ToList();
    Log("STAGE lamps-drying");
    LightFireNear(lampPieces.Average(p => p.X) + 1.3f, lampPieces.Average(p => p.Z));
    KeepFireGoing();
    var ids = lampPieces.Concat(potPieces).Select(p => p.Id).ToList();
    var watch = Stopwatch.StartNew();
    while (ids.Any(id => MoistureOf(id) > ClayRules.SafeToFire))
    {
        if (watch.Elapsed > TimeSpan.FromMinutes(10)) Fail("lamps never dried");
        KeepFireGoing();
        Pump(() => false, 5, null);
    }
    int newsBefore = news.Count;
    foreach (var id in ids)
    {
        KeepFireGoing();
        var piece = conn.Db.ClayPiece.Id.Find(id)!;
        WalkTo(piece.X, piece.Z, 1.2f);
        lastError = null;
        conn.Reducers.TendClay(id, false);
        Pump(() => lastError != null || conn.Db.ClayPiece.Id.Find(id)?.Stage == (byte)ClayStage.Firing, 3, "a lamp to go in the fire");
        if (lastError != null) Fail($"firing a lamp: {lastError}");
    }
    watch.Restart();
    while (ids.Any(id => conn.Db.ClayPiece.Id.Find(id) != null))
    {
        if (watch.Elapsed > TimeSpan.FromMinutes(6)) Fail("lamp firing never finished");
        Pump(() => false, 2, null);
    }
    Pump(() => news.Count >= newsBefore + ids.Count, 5, "firing news");
    int WorldCount(Item it) => conn.Db.WorldItem.Iter().Count(w => w.Item == (ushort)it);
    if (WorldCount(Item.ClayLamp) == 0)
    {
        Log("Both lamps cracked in the fire (a small chance); lamp lighting not checked this run");
        conn.Disconnect();
        return 0;
    }
    if (tallowRun && WorldCount(Item.CoilPot) == 0)
    {
        Log("The coil pot cracked in the fire (a small chance); tallow not checked this run");
        tallowRun = false;
    }
    Gather(Item.ClayLamp, 1);
    if (tallowRun) Gather(Item.CoilPot, 1);
    Log("STAGE lamp-fired");

    KeepFireGoing();
    WalkTo(fire.X + 2.5f, fire.Z, 0.3f);
    lastError = null;
    conn.Reducers.SetLamp(x + 1f, z);
    Pump(() => lastError != null, 3, "a lamp with no tallow to be refused");
    if (lastError?.Contains("tallow") != true) Fail($"a lamp with no tallow: {lastError}");
    Log($"No tallow: {lastError}");

    if (!tallowRun)
    {
        Log("Lamp check passed: a lamp pinched at the bench, fired, and refused without tallow (set CLAYCHECK_TALLOW=1 to light one)");
        conn.Disconnect();
        return 0;
    }
    // Hunt a sheep for its mutton (Animals: a spear and a quiet approach), unless a
    // debug hook already put some in the bag.
    if (CountOf(Item.RawMutton) < 2) HuntMutton();
    WalkTo(waterX, waterZ, 1.5f);
    if (CountOf(Item.CoilPotOfWater) == 0)
    {
        lastError = null;
        conn.Reducers.FillPots();
        Pump(() => lastError != null || CountOf(Item.CoilPotOfWater) > 0, 3, "a coil pot of water");
        if (CountOf(Item.CoilPotOfWater) == 0) Fail("the tallow run needs a coil pot in the bag (put one there too)");
    }
    fire = conn.Db.Fire.Id.Find(fire.Id) ?? fire;
    KeepFireGoing();
    WalkTo(fire.X + 1.2f, fire.Z, 0.3f);
    Craft("cook.tallow", 0, null);
    Pump(() => conn.Db.SimmerPot.Iter().Any(p => p.PlayerId == myId), 5, "the tallow to go on");
    var simmer = conn.Db.SimmerPot.Iter().First(p => p.PlayerId == myId);
    Pump(() => false, ClayRules.SimmerSeconds(Item.Tallow) + 1, null);
    lastError = null;
    conn.Reducers.TakeOffPot(simmer.Id);
    Pump(() => lastError != null || CountOf(Item.Tallow) >= 1, 3, "tallow");
    if (lastError != null) Fail($"taking the tallow off: {lastError}");
    Log($"Rendered tallow; the coil pot came back empty ({CountOf(Item.CoilPot)} coil pots)");
    Gather(Item.Tallow, 1);

    WalkTo(fire.X + 2.5f, fire.Z, 0.3f);
    lastError = null;
    conn.Reducers.SetLamp(x + 1f, z);
    Pump(() => lastError != null || conn.Db.Lamp.Iter().Any(l => l.PlayerId == myId), 3, "a lit lamp");
    if (lastError != null) Fail($"setting a lamp: {lastError}");
    Log("STAGE lamp-lit");
    Log("Lamp check passed: tallow rendered, and a fired lamp set down and lit");
    conn.Disconnect();
    return 0;
}

// Raw mutton the way a player gets it (ceramics-9): lash a spear, sneak up on the
// nearest sheep, strike until it falls, butcher it with the stone axe and pick up the
// cuts. Sheep never bolt from a player who moves in short steps.
void HuntMutton()
{
    if (CountOf(Item.Spear) == 0)
    {
        if (CountOf(Item.Stone) < 2) Gather(Item.Stone, 2); // one to strike with, one to knap
        if (CountOf(Item.Stick) < 1) Gather(Item.Stick, 1);
        if (CountOf(Item.Fiber) < 3 && CountOf(Item.Cordage) < 1) Gather(Item.Fiber, 3);
        Craft("knap.flake", 0.95f, Item.Flake);
        if (CountOf(Item.Cordage) < 1) Craft("cord.twist", 0, Item.Cordage);
        Craft("lash.spear", 0, Item.Spear);
    }
    var sheep = conn.Db.Animal.Iter().OrderBy(a => DistanceTo(a.X, a.Z, x, z)).FirstOrDefault();
    if (sheep == null) Fail("no sheep to hunt in this world");
    ulong id = sheep!.Id;
    Log($"Hunting sheep {id}, {DistanceTo(sheep.X, sheep.Z, x, z):0} m away");
    var clock = Stopwatch.StartNew();
    while (conn.Db.AnimalCarcass.Iter().All(c => c.AnimalId != id))
    {
        if (clock.Elapsed > TimeSpan.FromMinutes(4)) Fail("could not bring the sheep down");
        var a = conn.Db.Animal.Id.Find(id);
        if (a == null) Fail("the sheep vanished without a carcass");
        while (DistanceTo(a!.X, a.Z, x, z) > 2.0f)
        {
            // Short steps, so the sheep isn't startled.
            float dx = a.X - x, dz = a.Z - z, d = MathF.Sqrt(dx * dx + dz * dz);
            float px = x, pz = z;
            x += dx / d * 0.15f;
            z += dz / d * 0.15f;
            (x, z) = ground.Slide(px, pz, x, z);
            conn.Reducers.Move(x, z, MathF.Atan2(dx, dz));
            Pump(() => false, 0.1f, null);
            a = conn.Db.Animal.Id.Find(id);
            if (a == null) break;
        }
        if (a == null) continue;
        lastError = null;
        conn.Reducers.StrikeAnimal(id);
        Pump(() => false, 1.1f, null);
        if (lastError != null && !lastError.Contains("Too far")) Fail($"striking the sheep: {lastError}");
    }
    var carcass = conn.Db.AnimalCarcass.Iter().First(c => c.AnimalId == id);
    WalkTo(carcass.X, carcass.Z, 1.5f);
    lastError = null;
    conn.Reducers.ButcherCarcass(carcass.Id);
    Pump(() => lastError != null || CountOf(Item.RawMutton) >= 2 || conn.Db.WorldItem.Iter().Count(w => w.Item == (ushort)Item.RawMutton) >= 3, 5, "the butchering");
    if (lastError != null) Fail($"butchering: {lastError}");
    Gather(Item.RawMutton, 2);
    Log("STAGE mutton-hunted");
}

// Roof tiles (ceramics-10): pressed at the bench, dried by a fire; a campfire refuses
// to fire them (kiln only, like the jar). Kiln firing is the same path as the jar's,
// which --kiln --jar covers.
int TileCheck()
{
    while (CountOf(Item.CleanClay) < 2) Craft("clay.clean", 0, Item.CleanClay);
    Craft("clay.grit", 0, Item.Grit);
    Craft("clay.temper_grit", 0, Item.TemperedClay);

    // Shaping a tile away from the bench is refused.
    WalkTo(bench.X, bench.Z - 15f, 0.5f);
    lastError = null;
    conn.Reducers.StartCraft("clay.tile", 0.9f);
    Pump(() => lastError != null || conn.Db.MyCraftJob.Count > 0, 3, "a tile with no bench to be refused");
    if (lastError == null) Fail("a tile was shaped away from the bench");
    Log($"No bench: {lastError}");

    AtBench();
    Craft("clay.tile", 0.9f, null);
    Craft("clay.tile", 0.9f, null);
    var tiles = conn.Db.ClayPiece.Iter().Where(p => p.MadeBy == myId && p.Form == (byte)ClayForm.Tile).ToList();
    if (tiles.Count != 2) Fail($"expected 2 tiles drying, have {tiles.Count}");
    Log("STAGE tiles-drying");
    LightFireNear(tiles.Average(p => p.X) + 1.3f, tiles.Average(p => p.Z));
    KeepFireGoing();
    var watch = Stopwatch.StartNew();
    while (tiles.Any(t => MoistureOf(t.Id) > ClayRules.SafeToFire))
    {
        if (watch.Elapsed > TimeSpan.FromMinutes(10)) Fail("tiles never dried");
        KeepFireGoing();
        Pump(() => false, 5, null);
    }
    foreach (var t in tiles)
    {
        var piece = conn.Db.ClayPiece.Id.Find(t.Id)!;
        WalkTo(piece.X, piece.Z, 1.2f);
        lastError = null;
        conn.Reducers.TendClay(t.Id, false);
        Pump(() => lastError != null || conn.Db.ClayPiece.Id.Find(t.Id) == null, 3, "picking up a dry tile");
        if (lastError != null) Fail($"picking up a tile: {lastError}");
    }
    if (CountOf(Item.UnfiredRoofTile) != 2) Fail($"expected 2 unfired tiles, have {CountOf(Item.UnfiredRoofTile)}");
    KeepFireGoing();
    WalkTo(fire.X, fire.Z, 1.2f);
    lastError = null;
    conn.Reducers.FirePots(fire.Id);
    Pump(() => lastError != null, 3, "the campfire to refuse the tiles");
    if (lastError == null || !lastError.Contains("kiln")) Fail($"campfire took the tiles: {lastError}");
    Log($"Campfire: {lastError}");
    Log("Tile check passed: tiles pressed at the bench, dried, and refused by a campfire (they need a kiln)");
    conn.Disconnect();
    return 0;
}

// The fired clay trough comes out of the kiln; set it down, pour a pot of water in, scoop
// a pot back out, and have a drink (ceramics-13).
void TroughCheck(Structure kiln)
{
    var trough = conn.Db.WorldItem.Iter().FirstOrDefault(w => w.Item == (ushort)Item.ClayTrough && DistanceTo(w.X, w.Z, kiln.X, kiln.Z) < 2.5f);
    if (trough == null)
    {
        Log("The trough cracked in the kiln (2% chance); trough not checked this run");
        return;
    }
    WalkTo(trough.X, trough.Z, 1.0f);
    lastError = null;
    conn.Reducers.PickUp(trough.Id);
    Pump(() => lastError != null || CountOf(Item.ClayTrough) == 1, 3, "picking up the trough");
    if (lastError != null) Fail($"picking up the fired trough: {lastError}");
    Log($"Fired trough at quality {trough.Quality}");
    Log("STAGE trough-fired");

    var site = FindSite(StructureKind.ClayTrough, kiln.X + 3.5f, kiln.Z, 0f, (sx, sz) =>
        DistanceTo(sx, sz, kiln.X, kiln.Z) >= 2.5f && DistanceTo(sx, sz, fire.X, fire.Z) >= 2f
        && conn.Db.Structure.Iter().All(o => DistanceTo(sx, sz, o.X, o.Z) >= 2.5f)
        && InMyClaim(sx, sz, 1f) && ClearGround(sx, sz, -1, 1, -1, 1)) ?? FailAt("no ground for the trough");
    WalkTo(site.X - 1.4f, site.Z, 0.2f);
    float tx = site.X, tz = site.Z;
    lastError = null;
    conn.Reducers.PlaceStructure(StructureKind.ClayTrough, tx, tz, 0f);
    Pump(() => lastError != null || conn.Db.Structure.Iter().Any(s => s.Kind == StructureKind.ClayTrough && s.Owner == me), 5, "the trough site");
    if (lastError != null) Fail($"placing the trough: {lastError}");
    var placed = conn.Db.Structure.Iter().First(s => s.Kind == StructureKind.ClayTrough && s.Owner == me);
    for (int i = 0; i < 10 && conn.Db.Structure.Id.Find(placed.Id) is { Complete: false }; i++)
    {
        lastError = null;
        conn.Reducers.Build(placed.Id);
        Pump(() => lastError != null || conn.Db.Structure.Id.Find(placed.Id) is { Complete: true }, 3, "the trough to be set down");
        if (lastError == "Still working") { Pump(() => false, 0.5f, null); continue; }
        if (lastError != null) Fail($"setting the trough down: {lastError}");
    }
    if (conn.Db.Structure.Id.Find(placed.Id) is not { Complete: true }) Fail("trough never set down");
    if (CountOf(Item.ClayTrough) != 0) Fail("the trough is still in the bag");
    int capacity = Structures.TroughLitres(conn.Db.StructureProgress.StructureId.Find(placed.Id)?.Quality ?? Items.RawQuality);
    Log($"Trough set down; it holds {capacity} L");
    Log("STAGE trough-placed");

    // Pouring with no water pot is refused.
    WalkTo(placed.X - 1.2f, placed.Z, 0.2f);
    lastError = null;
    conn.Reducers.PourIntoTrough(placed.Id);
    Pump(() => lastError != null, 3, "pouring nothing to be refused");
    if (lastError?.Contains("pot of water") != true) Fail($"pouring with no water: {lastError}");

    // Fill a fired pot at the water, bring it back and pour it in.
    var pinch = conn.Db.WorldItem.Iter().Where(w => w.Item == (ushort)Item.PinchPot && DistanceTo(w.X, w.Z, kiln.X, kiln.Z) < 3f).FirstOrDefault();
    if (pinch == null) { Log("No fired pot by the kiln to carry water; pouring not checked this run"); return; }
    Gather(Item.PinchPot, 1);
    WalkTo(waterX, waterZ, 1.5f);
    lastError = null;
    conn.Reducers.FillPots();
    Pump(() => lastError != null || CountOf(Item.PinchPotOfWater) == 1, 3, "a pot of water");
    if (lastError != null) Fail($"filling a pot: {lastError}");
    WalkTo(placed.X - 1.2f, placed.Z, 0.2f);
    lastError = null;
    conn.Reducers.PourIntoTrough(placed.Id);
    Pump(() => lastError != null || conn.Db.TroughWater.StructureId.Find(placed.Id) is { Litres: 2 }, 3, "the trough to fill");
    if (lastError != null) Fail($"pouring: {lastError}");
    if (CountOf(Item.PinchPot) != 1 || CountOf(Item.PinchPotOfWater) != 0) Fail("the emptied pot did not come back");
    Log("Poured a pot in: the trough holds 2 L");
    Log("STAGE trough-poured");

    // A drink: either a sip, or "not thirsty" if the bot is not.
    // Actions share a short cooldown: wait it out, as a player would.
    for (int tries = 0; tries < 6; tries++)
    {
        Pump(() => false, 1.0f, null);
        lastError = null;
        conn.Reducers.DrinkFromTrough(placed.Id);
        Pump(() => lastError != null || conn.Db.TroughWater.StructureId.Find(placed.Id) is { Litres: 1 }, 3, "a drink");
        if (lastError != "Still working") break;
    }
    if (lastError != null && !lastError.Contains("not thirsty")) Fail($"drinking: {lastError}");
    int litres = (int)(conn.Db.TroughWater.StructureId.Find(placed.Id)?.Litres ?? 0);
    Log(lastError != null ? $"Drink: {lastError}" : $"Drank a sip; {litres} L left");

    // Scoop the empty pot full again from what's left (a pinch pot holds 2 L).
    if (litres >= 2)
    {
        for (int tries = 0; tries < 6; tries++)
        {
            Pump(() => false, 1.0f, null);
            lastError = null;
            conn.Reducers.ScoopFromTrough(placed.Id);
            Pump(() => lastError != null || CountOf(Item.PinchPotOfWater) == 1, 3, "a pot scooped full");
            if (lastError != "Still working") break;
        }
        if (lastError != null) Fail($"scooping: {lastError}");
        if ((conn.Db.TroughWater.StructureId.Find(placed.Id)?.Litres ?? 0) != litres - 2) Fail("the trough level did not drop by a pot");
        Log("Scooped a pot back out");
    }
    Log("STAGE trough-done");
}

int PotCheck()
{
    // Two coil pots, so one cracking in the pit (a chance) doesn't void the run.
    while (CountOf(Item.CleanClay) < 6) Craft("clay.clean", 0, Item.CleanClay);
    Craft("clay.grit", 0, Item.Grit);
    Craft("clay.grit", 0, Item.Grit);
    for (int i = 0; i < 3; i++) Craft("clay.temper_grit", 0, Item.TemperedClay);

    // Shaped at the bench, then a fire lit right beside them so they dry by it.
    AtBench();
    // A first pot with no mark chosen carries no maker; then pick a mark (a bad glyph is refused).
    lastError = null;
    conn.Reducers.SetMark(9999, 0xFFFFFF);
    Pump(() => lastError != null, 3, "a bad glyph to be refused");
    if (lastError?.Contains("no such mark") != true) Fail($"a bad glyph: {lastError}");
    lastError = null;
    conn.Reducers.SetMark(Marks.All[17].Id, 0xFF00FF);   // neon magenta: clamped
    Pump(() => lastError != null || conn.Db.PlayerMark.PlayerId.Find(myId) != null, 3, "the mark to be set");
    if (lastError != null) Fail($"setting the mark: {lastError}");
    var mine = conn.Db.PlayerMark.PlayerId.Find(myId)!;
    if (mine.Color != Marks.ClampColor(0xFF00FF) || mine.Color == 0xFF00FF) Fail($"colour not clamped: {mine.Color:X}");
    Log($"Mark: {Marks.Find(mine.Glyph)!.Name}, colour {mine.Color:X6} (clamped from FF00FF)");
    Craft("clay.coil_pot", 0.9f, null);
    Craft("clay.coil_pot", 0.9f, null);
    var coilPieces = conn.Db.ClayPiece.Iter().Where(p => p.MadeBy == myId && p.Form == (byte)ClayForm.CoilPot).ToList();
    var coils = coilPieces.Select(p => p.Id).ToList();
    if (coils.Count != 2) Fail($"expected 2 coil pots drying, have {coils.Count}");
    // Each pot carries its maker's mark (ceramics-15): this player's id and the glyph worked out from it.
    uint expectedMark = Marks.Pack(myId);
    foreach (var cp in coilPieces)
        if (cp.Mark != expectedMark) Fail($"coil pot mark {cp.Mark:X}, expected {expectedMark:X}");
    Log($"Pots carry their maker, player {Marks.Maker(expectedMark)}");
    LightFireNear(coilPieces.Average(p => p.X) + 1.3f, coilPieces.Average(p => p.Z));
    if (coilPieces.Any(p => DistanceTo(p.X, p.Z, fire.X, fire.Z) > ClayRules.FireReach)) Fail("coil pot set down out of the fire's reach");
    KeepFireGoing();
    var watch = Stopwatch.StartNew();
    while (coils.Any(id => MoistureOf(id) > ClayRules.SafeToFire))
    {
        if (watch.Elapsed > TimeSpan.FromMinutes(10)) Fail("coil pots never dried");
        KeepFireGoing();
        Pump(() => false, 5, null);
    }
    int newsBefore = news.Count;
    foreach (var id in coils)
    {
        KeepFireGoing();
        var piece = conn.Db.ClayPiece.Id.Find(id)!;
        WalkTo(piece.X, piece.Z, 1.2f);
        lastError = null;
        conn.Reducers.TendClay(id, false);
        Pump(() => lastError != null || conn.Db.ClayPiece.Id.Find(id)?.Stage == (byte)ClayStage.Firing, 3, "a coil pot to go in the fire");
        if (lastError != null) Fail($"firing: {lastError}");
    }
    watch.Restart();
    while (coils.Any(id => conn.Db.ClayPiece.Id.Find(id) != null))
    {
        if (watch.Elapsed > TimeSpan.FromMinutes(6)) Fail("firing never finished");
        Pump(() => false, 2, null);
    }
    Pump(() => news.Count >= newsBefore + 2, 5, "firing news");
    if (!news.Skip(newsBefore).Any(n => n.Contains("whole")))
    {
        Log("Both coil pots cracked in the fire (a small chance); pot cooking not checked this run");
        conn.Disconnect();
        return 0;
    }
    Gather(Item.CoilPot, 1);
    var firedRow = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.CoilPot);
    if (firedRow.Wear != expectedMark) Fail($"the fired pot lost its mark: {firedRow.Wear:X}, expected {expectedMark:X}");
    Log("The fired pot kept its mark through the pit fire and the pick-up");
    WalkTo(waterX, waterZ, 1.5f);
    lastError = null;
    conn.Reducers.FillPots();
    Pump(() => lastError != null || CountOf(Item.CoilPotOfWater) > 0, 3, "a coil pot of water");
    if (lastError != null) Fail($"filling: {lastError}");
    var filledRow = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.CoilPotOfWater);
    if (filledRow.Wear != expectedMark) Fail($"the filled pot lost its mark: {filledRow.Wear:X}");
    Log("STAGE pot-of-water");

    // Changing the mark later: the maker stays, so older pots follow the new glyph.
    lastError = null;
    conn.Reducers.SetMark(Marks.All[30].Id, Marks.Presets[5]);
    Pump(() => lastError != null || conn.Db.PlayerMark.PlayerId.Find(myId) is { Glyph: 30 }, 3, "the mark to change");
    if (lastError != null) Fail($"changing the mark: {lastError}");
    Log($"Changed the mark to {Marks.Find(30)!.Name}; the pot's maker is unchanged");
    Log("STAGE mark-changed");

    // Berries from the nearest ripe bushes.
    watch.Restart();
    while (CountOf(Item.Berries) < 3)
    {
        if (watch.Elapsed > TimeSpan.FromMinutes(3)) Fail("could not pick 3 berries");
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        var bush = conn.Db.ForageBush.Iter().Where(b => b.RipeAt.MicrosecondsSinceUnixEpoch <= now)
            .OrderBy(b => DistanceTo(b.X, b.Z, x, z)).FirstOrDefault();
        if (bush == null) { Pump(() => false, 2, null); continue; }
        WalkTo(bush.X, bush.Z, 1.5f);
        lastError = null;
        uint before = CountOf(Item.Berries);
        conn.Reducers.Forage(bush.Id);
        Pump(() => lastError != null || CountOf(Item.Berries) > before, 3, "berries");
        if (lastError == "Still working") Pump(() => false, 0.7f, null);
    }
    Log($"Picked {CountOf(Item.Berries)} berries");

    // A fresh fire if this one died while we were away.
    fire = conn.Db.Fire.Id.Find(fire.Id) ?? fire;
    KeepFireGoing();
    WalkTo(fire.X + 1.2f, fire.Z, 0.3f);
    Craft("cook.broth", 0, null);
    Pump(() => conn.Db.SimmerPot.Iter().Any(p => p.PlayerId == myId), 5, "the broth to go on");
    var simmer = conn.Db.SimmerPot.Iter().First(p => p.PlayerId == myId);
    Log($"Broth simmering by the fire at quality {simmer.Quality}");
    Log("STAGE broth-simmering");

    lastError = null;
    conn.Reducers.TakeOffPot(simmer.Id);
    Pump(() => lastError != null, 3, "an early take-off to be refused");
    Log($"Too early: {lastError}");
    Pump(() => false, ClayRules.BrothSeconds + 1, null);
    lastError = null;
    conn.Reducers.TakeOffPot(simmer.Id);
    Pump(() => lastError != null || CountOf(Item.PotOfBroth) == 1, 3, "the broth to come off");
    if (lastError != null) Fail($"taking the pot off: {lastError}");
    Log("STAGE broth-done");

    uint pots = CountOf(Item.CoilPot);
    lastError = null;
    conn.Reducers.Eat((ushort)Item.PotOfBroth);
    Pump(() => lastError != null || CountOf(Item.PotOfBroth) == 0, 3, "eating the broth");
    if (lastError != null) Fail($"eating: {lastError}");
    Pump(() => CountOf(Item.CoilPot) == pots + 1, 3, "the empty pot back");
    Log("Ate the broth; the coil pot came back");
    Log("STAGE broth-eaten");
    Log("Pot check passed: a fired pot cooks broth that feeds you");
    conn.Disconnect();
    return 0;
}

// The fired jar (or its sherds, 2% of the time) lies by the kiln door. Pick it
// up, set it down from the build menu, put a stack in and take it out again.
void JarCheck(Structure kiln)
{
    var jar = conn.Db.WorldItem.Iter().FirstOrDefault(w => w.Item == (ushort)Item.StorageJar && DistanceTo(w.X, w.Z, kiln.X, kiln.Z) < 2.5f);
    if (jar == null)
    {
        Log("The jar cracked in the kiln (2% chance); jar placement not checked this run");
        return;
    }
    WalkTo(jar.X, jar.Z, 1.0f);
    lastError = null;
    conn.Reducers.PickUp(jar.Id);
    Pump(() => lastError != null || CountOf(Item.StorageJar) == 1, 3, "picking up the jar");
    if (lastError != null) Fail($"picking up the fired jar: {lastError}");
    Log($"Fired jar at quality {jar.Quality}");
    Log("STAGE jar-fired");

    var jarSite = FindSite(StructureKind.StorageJar, kiln.X, kiln.Z - 3f, 0f, (sx, sz) =>
        DistanceTo(sx, sz, kiln.X, kiln.Z) >= 2.5f && DistanceTo(sx, sz, fire.X, fire.Z) >= 2f
        && InMyClaim(sx, sz, 1f) && ClearGround(sx, sz, -1, 1, -1, 1)) ?? FailAt("no ground for the jar");
    WalkTo(jarSite.X - 1.2f, jarSite.Z, 0.2f);
    float jx = jarSite.X, jz = jarSite.Z;
    lastError = null;
    conn.Reducers.PlaceStructure(StructureKind.StorageJar, jx, jz, 0f);
    Pump(() => lastError != null || conn.Db.Structure.Iter().Any(s => s.Kind == StructureKind.StorageJar && s.Owner == me), 5, "the jar site");
    if (lastError != null) Fail($"placing the jar: {lastError}");
    var site = conn.Db.Structure.Iter().First(s => s.Kind == StructureKind.StorageJar && s.Owner == me);
    for (int i = 0; i < 10 && conn.Db.Structure.Id.Find(site.Id) is { Complete: false }; i++)
    {
        lastError = null;
        conn.Reducers.Build(site.Id);
        Pump(() => lastError != null || conn.Db.Structure.Id.Find(site.Id) is { Complete: true }, 3, "the jar to be set down");
        if (lastError == "Still working") { Pump(() => false, 0.5f, null); continue; }
        if (lastError != null) Fail($"setting the jar down: {lastError}");
    }
    if (conn.Db.Structure.Id.Find(site.Id) is not { Complete: true }) Fail("jar never set down");
    if (CountOf(Item.StorageJar) != 0) Fail("the jar is still in the bag");
    Log("STAGE jar-placed");

    if (CountOf(Item.Stick) < 1) Gather(Item.Stick, 2);
    WalkTo(site.X, site.Z, 1.0f);
    uint sticks = CountOf(Item.Stick);
    var stick = conn.Db.MyInventory.Iter().First(r => r.Item == (ushort)Item.Stick);
    lastError = null;
    conn.Reducers.BasketPut(site.Id, stick.Item, stick.Quality, 1);
    Pump(() => lastError != null || conn.Db.BasketItem.Iter().Any(b => b.StructureId == site.Id), 3, "a stick in the jar");
    if (lastError != null) Fail($"putting a stick in the jar: {lastError}");
    var stored = conn.Db.BasketItem.Iter().First(b => b.StructureId == site.Id);
    lastError = null;
    conn.Reducers.BasketTake(stored.Id, 1);
    Pump(() => lastError != null || CountOf(Item.Stick) == sticks, 3, "the stick back out");
    if (lastError != null) Fail($"taking the stick out: {lastError}");
    Log($"Jar stores and gives back; at quality {jar.Quality} it holds {Structures.JarStacks(jar.Quality)} stacks");
    Log("STAGE jar-store");
}

// A stone axe, one log, a claim past the spawn clearing and a potter's bench in it.
void SetUpBench()
{
    Craft("knap.hand_axe", 0.95f, Item.HandAxe);
    Craft("cord.twist", 0, Item.Cordage);
    Craft("lash.stone_axe", 0, Item.StoneAxe);
    var (treeX, treeZ) = NearestTree();
    var trunk = Trees.Trunk(treeX, treeZ);
    WalkTo(trunk.X, trunk.Z, 1.2f);
    Craft("chop.log", 0, Item.Log);

    // Claims stay out of the spawn clearing: walk out past it, away from the origin.
    float keepOut = Social.SpawnClearRadius + Social.ClaimRadius;
    float claimDist = keepOut + 5f;
    float ang = clayX == 0 && clayZ == 0 ? 0f : MathF.Atan2(clayZ, clayX);
    WalkTo(MathF.Cos(ang) * claimDist, MathF.Sin(ang) * claimDist, 1f);
    marker = BuildHere(StructureKind.ClaimMarker, (sx, sz) => sx * sx + sz * sz > (keepOut + 1f) * (keepOut + 1f) && ClearGround(sx, sz, -1, 1, -1, 1));
    Pump(() => conn.Db.Claim.Iter().Any(c => c.StructureId == marker.Id), 5, "the claim");
    Log($"Claimed land at {marker.X:F1}, {marker.Z:F1}");

    // With no bench, shaping is refused.
    lastError = null;
    conn.Reducers.StartCraft("clay.pinch_pot", 0.9f);
    Pump(() => lastError != null || conn.Db.MyCraftJob.Count > 0, 5, "a pot away from a bench to be refused");
    if (lastError?.Contains("potter's bench") != true) Fail($"a pot shaped away from a bench: {lastError ?? "it started"}");
    Log($"No bench: {lastError}");

    // The bench, on open level ground with room to the east for fires and a kiln, and
    // well away from the marker.
    bench = BuildHere(StructureKind.PottersBench, (sx, sz) =>
        InMyClaim(sx, sz, 7f) && DistanceTo(sx, sz, marker.X, marker.Z) >= 10f && ClearGround(sx, sz, -3, 6, -2, 4));
    Log($"Built a potter's bench at {bench.X:F1}, {bench.Z:F1}; {CountOf(Item.Stone)} stones left");
    Log("STAGE bench");
}

// Stands where pots are shaped: just on the player's (south) side of the bench.
void AtBench() => WalkTo(bench.X, bench.Z + 1.6f, 0.1f);

// Lights a hand-drill fire at a spot, standing 1.2 m south of it and facing it.
void LightFireNear(float tx, float tz)
{
    WalkTo(tx, tz + 1.2f, 0.1f);
    Face(tx, tz);
    Craft("gather.tinder", 0, Item.Tinder);
    int firesBefore = conn.Db.Fire.Count;
    Craft("fire.hand_drill", 1f, null);
    Pump(() => conn.Db.Fire.Count > firesBefore, 5, "a fire");
    fire = conn.Db.Fire.Iter().Where(f => f.LitBy == myId).OrderBy(f => DistanceTo(f.X, f.Z, tx, tz)).First();
    if (DistanceTo(fire.X, fire.Z, tx, tz) > 0.5f) Log($"Fire landed at {fire.X:F1}, {fire.Z:F1}, not {tx:F1}, {tz:F1}");
}

// Turns on the spot to face a point.
void Face(float tx, float tz)
{
    conn.Reducers.Move(x, z, MathF.Atan2(tx - x, tz - z));
    Pump(() => false, 0.4f, null);
}

bool InMyClaim(float px, float pz, float margin) => DistanceTo(px, pz, marker.X, marker.Z) <= Social.ClaimRadius - margin;

// No tree, boulder or uneven ground anywhere in a box of cells round a spot.
bool ClearGround(float px, float pz, int x0, int x1, int z0, int z1)
{
    int ci = World.CellOf(px), cj = World.CellOf(pz);
    float lo = float.MaxValue, hi = float.MinValue;
    for (int i = ci + x0; i <= ci + x1; i++)
        for (int j = cj + z0; j <= cj + z1; j++)
        {
            if (Trees.GrowsAt(i, j) || Rocks.LiesAt(i, j)) return false;
            float h = World.Height(i, j, DepthAt(i, j));
            lo = Math.Min(lo, h);
            hi = Math.Max(hi, h);
        }
    return lo > World.WaterLevel + 0.1f && hi - lo < 0.35f;
}

// The nearest spot to a point where a structure fits and `ok` holds.
(float X, float Z)? FindSite(StructureKind kind, float ox, float oz, float yaw, Func<float, float, bool> ok)
{
    float H(float px, float pz) => Structures.GroundAt(px, pz, (i, j) => DepthAt(i, j));
    for (int r = 0; r < 30; r++)
    {
        int steps = r == 0 ? 1 : 8 + 4 * r;
        for (int k = 0; k < steps; k++)
        {
            float a = k * MathF.Tau / steps, sx = ox + MathF.Cos(a) * r, sz = oz + MathF.Sin(a) * r;
            if (Structures.GroundProblem((int)kind, sx, sz, yaw, H) != null) continue;
            if (conn.Db.Structure.Iter().Any(o => Structures.TooClose((int)kind, sx, sz, (int)o.Kind, o.X, o.Z))) continue;
            if (Trees.GrowsAt(World.CellOf(sx), World.CellOf(sz)) || Rocks.LiesAt(World.CellOf(sx), World.CellOf(sz))) continue;
            if (!ok(sx, sz)) continue;
            return (sx, sz);
        }
    }
    return null;
}

(float X, float Z) FailAt(string message)
{
    Fail(message);
    return default;
}

// Lays out a structure on clear, level ground near the bot and builds it from the bag.
Structure BuildHere(StructureKind kind, Func<float, float, bool> ok)
{
    float fromX = x, fromZ = z;
    var at = FindSite(kind, x, z, 0f, (sx, sz) => DistanceTo(sx, sz, fromX, fromZ) >= 3f && ok(sx, sz)) ?? FailAt($"no ground for a {kind}");
    WalkTo(at.X, at.Z + 2f, 0.2f);
    int before = conn.Db.Structure.Count;
    lastError = null;
    conn.Reducers.PlaceStructure(kind, at.X, at.Z, 0f);
    Pump(() => lastError != null || conn.Db.Structure.Count > before, 5, $"the {kind} to be laid out");
    if (lastError != null) Fail($"laying out a {kind}: {lastError}");
    var s = conn.Db.Structure.Iter().Where(o => o.Owner == me).OrderByDescending(o => o.PlacedAt.MicrosecondsSinceUnixEpoch).First();
    var sw = Stopwatch.StartNew();
    while (conn.Db.Structure.Id.Find(s.Id) is { Complete: false })
    {
        if (sw.Elapsed.TotalSeconds > 60) Fail($"building the {kind}: {lastError}");
        lastError = null;
        conn.Reducers.Build(s.Id);
        Pump(() => false, 0.35f, null);
    }
    return conn.Db.Structure.Id.Find(s.Id)!;
}

// The standing tree nearest the bot, by the same rules the server uses.
(int X, int Z) NearestTree()
{
    int cx = (int)MathF.Round(x), cz = (int)MathF.Round(z);
    for (int r = 0; r < 60; r++)
        for (int i = cx - r; i <= cx + r; i++)
            for (int j = cz - r; j <= cz + r; j++)
            {
                if (Math.Max(Math.Abs(i - cx), Math.Abs(j - cz)) != r || !Trees.GrowsAt(i, j)) continue;
                long key = World.CellKey(i, j);
                if (conn.Db.TerrainEdit.CellKey.Find(key) != null || conn.Db.ChoppedTree.CellKey.Find(key) != null) continue;
                return (i, j);
            }
    Fail("no tree within 60 m");
    return (0, 0);
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
    Pump(() => conn.Db.MyCraftJob.Count == 0, def.Seconds * 1.2f + 5, $"{processId} to finish");
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
    // Round trunks and boulders by a route, then straight in (bugfix-34).
    var route = ground.Route(x, z, tx, tz);
    for (int i = 0; i < route.Count - 1; i++) Leg(route[i].X, route[i].Z, 0.4f);
    Leg(tx, tz, near);
}

void Leg(float tx, float tz, float near)
{
    int lagged = 0;
    while (true)
    {
        float dx = tx - x, dz = tz - z, d = MathF.Sqrt(dx * dx + dz * dz);
        if (d <= near) break;
        float step = Math.Min(near < 0.5f ? d : d - near * 0.9f, 0.5f);
        // The server limits the pace (World.MoveAllowance, Carry.WalkFactor): only walk on once it
        // has caught up, and keep reporting where we are, as a real client does (bugfix-24).
        var behind = conn.Db.PlayerPos.PlayerId.Find(myId);
        if (behind is { } held && MathF.Abs(held.X - x) + MathF.Abs(held.Z - z) >= 1.5f)
        {
            // The server has not followed for a second: it refused the last step (something solid in the
            // way). Take its word for where we are and carry on from there, like a player would.
            if (++lagged > 10) { x = held.X; z = held.Z; lagged = 0; }
        }
        else lagged = 0;
        if (behind is not { } seen || MathF.Abs(seen.X - x) + MathF.Abs(seen.Z - z) < 1.5f)
        {
            float px = x, pz = z;
            x += dx / d * step;
            z += dz / d * step;
            (x, z) = ground.Slide(px, pz, x, z); // round things and walls, as a player meets them
        }
        conn.Reducers.Move(x, z, MathF.Atan2(dx, dz));
        Pump(() => false, 0.1f, null);
    }
    for (var settle = Stopwatch.StartNew(); ; )
    {
        if (conn.Db.PlayerPos.PlayerId.Find(myId) is { } p && MathF.Abs(p.X - x) < 0.1f && MathF.Abs(p.Z - z) < 0.1f) break;
        if (settle.Elapsed.TotalSeconds > 15) Fail("timed out waiting for position to settle");
        conn.Reducers.Move(x, z, 0f);
        Pump(() => false, 0.1f, null);
    }
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
