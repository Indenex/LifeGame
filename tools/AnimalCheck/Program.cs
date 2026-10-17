// Animal check (animals-29): the server rules behind the animals, with one fresh character on a
// freshly published server. First the Def data invariants that the client and server both read
// (shared/Animals.cs), then the reducers' refusals (D-182): an animal out of reach cannot be
// struck, shot or have its eggs taken, and an animal that is gone is reported as gone. Every wait
// is state-based with a generous timeout. Needs no admin, no weapon and no walking, so it runs in a
// few seconds. Exits non-zero on failure.
//
// The second half needs an admin (to place animals and give a bow and a spear): the tool makes its
// bot an admin through the CLI (the publisher's identity), which must be on PATH as `spacetimedb-cli`.
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
Guest? guest = null;
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
                "SELECT * FROM animal_carcass",
                "SELECT * FROM terrain_edit",
                "SELECT * FROM chopped_tree",
                "SELECT * FROM structure",
                "SELECT * FROM structure_door",
                "SELECT * FROM my_inventory",
                "SELECT * FROM my_admin",
                "SELECT * FROM player_equip",
                "SELECT * FROM claim",
                "SELECT * FROM claim_member",
                "SELECT * FROM hide_rack",
                "SELECT * FROM snare_state",
                "SELECT * FROM world_item",
            });
    })
    .OnConnectError(e => Fail($"connect: {e.Message}"))
    .Build();
conn.OnUnhandledReducerError += (_, e) => lastError = e.Message;

void Pump(Func<bool> done, int seconds, string what)
{
    var until = clock.Elapsed + TimeSpan.FromSeconds(seconds);
    while (!done() && clock.Elapsed < until) { conn.FrameTick(); guest?.Conn.FrameTick(); Thread.Sleep(5); }
    if (!done()) Fail($"timed out waiting for {what}");
}

void Settle(int ms)
{
    var until = clock.Elapsed + TimeSpan.FromMilliseconds(ms);
    while (clock.Elapsed < until) { conn.FrameTick(); guest?.Conn.FrameTick(); Thread.Sleep(5); }
}

Pump(() => subscribed, 30, "subscription");
Pump(() => me is { } id && conn.Db.Player.Identity.Find(id) is not null, 20, "player row");
Player Me() => conn.Db.Player.Identity.Find(me!.Value)!;
Pump(() => conn.Db.PlayerPos.PlayerId.Find(Me().PlayerId) is not null, 20, "our position");
var here = conn.Db.PlayerPos.PlayerId.Find(Me().PlayerId)!;

// --- The animal tick stays cheap (animals-32) ---
// The server counts the compute each reducer uses (wasmtime fuel) and serves it at /v1/metrics. Watch
// the animal tick for a few seconds with this one character connected and fail if a tick costs far more
// than it did when it was last trimmed. Measured with this lone character on the animals-31 build: 47 M
// and 51 M fuel per tick in two runs; with the per-chunk lookups of that PR switched off, 110 M; before
// it, about 3x the trimmed figure. Fuel counts compute, not time, so a slow CI machine does not move it.
// The ceiling sits between: well above the run-to-run spread, below a half-lost optimisation. A server that does not serve the metrics skips the check and says so.
using var metricsHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
(double Fuel, double Ticks)? TickMetrics()
{
    try
    {
        string text = metricsHttp.GetStringAsync(uri.TrimEnd('/') + "/v1/metrics").GetAwaiter().GetResult();
        double fuel = -1, ticks = -1;
        foreach (var line in text.Split('\n'))
        {
            if (!line.Contains("reducer=\"animal_tick\"")) continue;
            double value = double.Parse(line[(line.LastIndexOf(' ') + 1)..], System.Globalization.CultureInfo.InvariantCulture);
            if (line.StartsWith("reducer_wasmtime_fuel_used{")) fuel = value;
            else if (line.StartsWith("spacetime_reducer_plus_query_duration_sec_count{")) ticks = value;
        }
        return fuel >= 0 && ticks >= 0 ? (fuel, ticks) : null;
    }
    catch (Exception e) when (e is HttpRequestException or TaskCanceledException or FormatException)
    {
        return null;
    }
}

const double TickFuelCeiling = 90_000_000;
var tick0 = TickMetrics();
Settle(8000);
var tick1 = TickMetrics();
if (tick0 is { } m0 && tick1 is { } m1 && m1.Ticks - m0.Ticks >= 5)
{
    double perTick = (m1.Fuel - m0.Fuel) / (m1.Ticks - m0.Ticks);
    Console.WriteLine($"    animal tick: {perTick / 1e6:F1} M fuel per tick over {m1.Ticks - m0.Ticks:F0} ticks");
    if (perTick > TickFuelCeiling) Fail($"the animal tick uses {perTick / 1e6:F0} M fuel per tick, over the {TickFuelCeiling / 1e6:F0} M ceiling");
    Ok($"the animal tick is cheap ({perTick / 1e6:F1} M fuel per tick, ceiling {TickFuelCeiling / 1e6:F0} M)");
}
else
    Ok("the animal tick cost was not checked (the server serves no usable metrics, or fewer than 5 ticks ran)");

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
    Ok($"a strike with nothing in hand is refused ({strike})");

    string shot = Refused("a shot out of range", () => conn.Reducers.ShootAnimal(far.Id));
    if (shot.Length == 0) Fail("a shot out of range was refused with no reason");
    Ok($"a shot without facing the animal is refused ({shot})");

    // Eggs are only laid by Defs with an Egg; either way a far animal is not collected from.
    string eggs = Refused("eggs from an animal out of reach", () => conn.Reducers.CollectEggs(far.Id));
    bool lays = AnimalDefs.Get((byte)far.Species).Egg != null;
    if (lays && !eggs.Contains("reach")) Fail($"eggs from a hen {Away(far):F0} m away were refused with \"{eggs}\"");
    if (!lays && !eggs.Contains("doesn't lay")) Fail($"eggs from a non-layer were refused with \"{eggs}\"");
    Ok($"eggs from an animal {Away(far):F0} m away are refused ({eggs})");
}


// --- Test hooks: the bot becomes an admin (the CLI is the publisher) ---
string name = "Anim" + new string(Enumerable.Range(0, 8).Select(_ => (char)('a' + Random.Shared.Next(26))).ToArray());
conn.Reducers.SetName(name);
Pump(() => Me().Name == name, 10, "the name to be set");
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
Pump(() => conn.Db.MyAdmin.Iter().Any(), 15, "the bot to become an admin");
Ok("bot is an admin (animals can be placed, items given)");

uint CountOf(Item item) => (uint)conn.Db.MyInventory.Iter().Where(r => r.Item == (ushort)item).Sum(r => (long)r.Count);

void Give(Item item, uint count, byte quality = 60)
{
    uint before = CountOf(item);
    lastError = null;
    conn.Reducers.AdminGive((ushort)item, count, quality);
    Pump(() => lastError != null || CountOf(item) >= before + count, 10, $"{item} to be given");
    if (lastError != null) Fail($"giving {item}: {lastError}");
}

// Puts an item in a hotbar slot and takes it in hand.
void Hold(byte slot, Item item)
{
    conn.Reducers.EquipSlot(255);
    Pump(() => conn.Db.PlayerEquip.Owner.Find(me!.Value) is null, 10, "the hand to be put away");
    conn.Reducers.SetHotbarSlot(slot, (ushort)item);
    conn.Reducers.EquipSlot(slot);
    Pump(() => conn.Db.PlayerEquip.Owner.Find(me!.Value) is { } e && e.Item == (ushort)item, 10, $"{item} to come to hand");
}

// Places an animal exactly where asked and returns it once it shows.
Animal Spawn(AnimalSpecies species, float x, float z, byte eggs = 0)
{
    var before = conn.Db.Animal.Iter().Select(a => a.Id).ToHashSet();
    lastError = null;
    conn.Reducers.AdminSpawnAnimal((byte)species, x, z, eggs);
    Pump(() => lastError != null || conn.Db.Animal.Iter().Any(a => !before.Contains(a.Id)), 10, $"a {species} to be placed");
    if (lastError != null) Fail($"placing a {species}: {lastError}");
    return conn.Db.Animal.Iter().First(a => !before.Contains(a.Id));
}

PlayerPos Pos() => conn.Db.PlayerPos.PlayerId.Find(Me().PlayerId)!;

float bx = here.X, bz = here.Z;
var walker = new BotWalker(conn, Me().PlayerId, "Animal check", name, ms =>
{
    for (var until = clock.Elapsed + TimeSpan.FromMilliseconds(ms); clock.Elapsed < until; Thread.Sleep(5)) conn.FrameTick();
}, msg => Fail(msg));
var ground = new BotGround(conn);

void Face(float tx, float tz)
{
    var at = Pos();
    float heading = MathF.Atan2(tx - at.X, tz - at.Z);
    conn.Reducers.Move(at.X, at.Z, heading);
    Pump(() => MathF.Abs(MathF.IEEERemainder(Pos().Heading - heading, MathF.Tau)) < 0.05f, 10, "the bot to face the target");
}

// --- Arrows stop at a trunk (bow, D-182) ---
// The server walks the line from the shooter to the animal in steps of about a metre (Bow.cs
// ArrowBlocked); this works the same steps out here with the same shared rule, to find a trunk the
// line really passes through and a clear line to compare it with.
bool LineBlocked(float fx, float fz, float tx, float tz)
{
    float dx = tx - fx, dz = tz - fz;
    int steps = Math.Max(1, (int)MathF.Ceiling(MathF.Sqrt(dx * dx + dz * dz)));
    float px = fx, pz = fz;
    for (int i = 1; i <= steps; i++)
    {
        float x = fx + dx * i / steps, z = fz + dz * i / steps;
        if (Obstacles.StepBlocked(ground, 0.05f, px, pz, x, z, 1.2f)) return true;
        (px, pz) = (x, z);
    }
    return false;
}

(float X, float Z, float R)? trunk = null;
for (int ring = 6; ring <= 40 && trunk == null; ring++)
    for (int cx = (int)here.X - ring; cx <= (int)here.X + ring && trunk == null; cx++)
        for (int cz = (int)here.Z - ring; cz <= (int)here.Z + ring && trunk == null; cz++)
        {
            if (Math.Max(Math.Abs(cx - (int)here.X), Math.Abs(cz - (int)here.Z)) != ring) continue;
            if (!Obstacles.At(ground, cx, cz, out float ox, out float oz, out float r, out float h) || h != float.MaxValue || r < 0.2f) continue;
            // Room to stand 3 m from it on one side.
            var (sx, sz) = ground.Stand(ox - 3f, oz);
            if (MathF.Abs(sx - (ox - 3f)) > 0.01f || MathF.Abs(sz - oz) > 0.01f) continue;
            trunk = (ox, oz, r);
        }
if (trunk is not { } tree) { Fail("no tree trunk within 40 m of the start to test arrows against"); return 1; }
Give(Item.Bow, 1);
Give(Item.Arrow, 6);
Hold(8, Item.Bow);
walker.To(ref bx, ref bz, tree.X - 3f, tree.Z, 0.15f, stand: true);
var at0 = Pos();
float tdx = tree.X - at0.X, tdz = tree.Z - at0.Z, tdist = MathF.Sqrt(tdx * tdx + tdz * tdz);
float ux = tdx / tdist, uz = tdz / tdist;
// A length for the line past the trunk where one of the server's steps lands on the trunk's middle.
float? line = null;
for (float len = tdist + 2f; len < 20f && line == null; len += 0.01f)
{
    int n = (int)MathF.Ceiling(len);
    for (int k = 1; k <= n; k++)
        if (MathF.Abs(k * len / n - tdist) < tree.R * 0.5f) { line = len; break; }
}
if (line is not { } len0) { Fail("no line length puts a step on the trunk"); return 1; }
float ax = at0.X + ux * len0, az = at0.Z + uz * len0;
if (!LineBlocked(at0.X, at0.Z, ax, az)) Fail($"worked out a line through the trunk at ({tree.X:F1}, {tree.Z:F1}) that the shared rule says is clear");
var behind = Spawn(AnimalSpecies.Sheep, ax, az);
Face(ax, az);
string blocked = Refused("a shot through a tree trunk", () => conn.Reducers.ShootAnimal(behind.Id));
if (!blocked.Contains("in the way")) Fail($"a shot through a trunk was refused with \"{blocked}\", expected \"Something is in the way\"");
Ok($"an arrow through a tree trunk ({len0:F1} m line) is stopped ({blocked})");

// The same distance with nothing in between is not stopped for that reason.
float clearX = 0, clearZ = 0;
bool foundClear = false;
for (int deg = 0; deg < 360 && !foundClear; deg += 15)
{
    float rad = deg * MathF.PI / 180f;
    clearX = at0.X + MathF.Sin(rad) * len0;
    clearZ = at0.Z + MathF.Cos(rad) * len0;
    foundClear = !LineBlocked(at0.X, at0.Z, clearX, clearZ);
}
if (!foundClear) Fail("no clear line found round the bot to compare with");
var open = Spawn(AnimalSpecies.Sheep, clearX, clearZ);
Face(clearX, clearZ);
lastError = null;
conn.Reducers.ShootAnimal(open.Id);
Pump(() => CountOf(Item.Arrow) < 6 || lastError != null, 10, "the clear shot to be answered");
if (lastError != null) Fail($"a shot with nothing in the way was refused: {lastError}");
Ok("a shot with nothing in the way is let fly (an arrow was used)");

// --- One swing at a time, whichever animal it lands on (D-182) ---
Give(Item.Spear, 1, 60);
Hold(9, Item.Spear);
var near = Pos();
var first = Spawn(AnimalSpecies.Sheep, near.X, near.Z + 1.5f);
var second = Spawn(AnimalSpecies.Sheep, near.X, near.Z - 1.5f);
// Let any draw timer and the arrow thump settle so nothing else interferes.
Settle(1500);
conn.Reducers.StrikeAnimal(first.Id);
conn.Reducers.StrikeAnimal(second.Id);
// The first swing hurt its target; the second came too soon after it and did nothing.
Settle(1400);
conn.Reducers.StrikeAnimal(second.Id);
Settle(1000);
if (conn.Db.Animal.Id.Find(second.Id) == null)
    Fail("the second animal died after being struck once at a normal pace: a second swing landed within the swing pause");
Ok("a swing straight after another does nothing, even on a different animal");
Settle(1400);
conn.Reducers.StrikeAnimal(second.Id);
Pump(() => conn.Db.Animal.Id.Find(second.Id) == null, 10, "the second animal to fall to a second swing");
Ok("swings at the normal pace land: two good spear strikes fell a sheep");

// --- Out of reach, now with a spear in hand and a hen with eggs ---
var distant = Spawn(AnimalSpecies.Sheep, Pos().X + 15f, Pos().Z);
lastError = null;
conn.Reducers.StrikeAnimal(distant.Id);
Pump(() => lastError != null, 10, "a strike at a sheep 15 m away to be refused");
if (!lastError!.Contains("Too far")) Fail($"a strike at a sheep 15 m away was refused with \"{lastError}\"");
Ok("a spear strike at an animal 15 m away is out of reach");
var henFar = Spawn(AnimalSpecies.Hen, Pos().X + 15f, Pos().Z + 4f, 2);
string henRefused = Refused("eggs from a hen 15 m away", () => conn.Reducers.CollectEggs(henFar.Id));
if (!henRefused.Contains("reach")) Fail($"eggs from a hen 15 m away were refused with \"{henRefused}\"");
var henNear = Spawn(AnimalSpecies.Hen, Pos().X + 1f, Pos().Z + 1f, 2);
uint eggsBefore = CountOf(Item.Egg);
conn.Reducers.CollectEggs(henNear.Id);
Pump(() => CountOf(Item.Egg) > eggsBefore, 10, "eggs from a hen at hand");
Ok("eggs come only from a hen within reach");

// --- Claims: eggs and hide-rack leather stay with the claim's owner and members (animals-28) ---
// A raises a claim marker well outside the waking clearing; B, a second character who is no
// member, comes to it. Hens and hung hides are placed with the admin hooks.
Give(Item.Stick, Social.MarkerSticks);
Give(Item.Fiber, Social.MarkerFiber);
float H(float x, float z) => Structures.GroundAt(x, z, (i, j) => conn.Db.TerrainEdit.CellKey.Find(World.CellKey(i, j))?.Depth ?? 0);
const float keepOut = Social.SpawnClearRadius + Social.ClaimRadius + 0.5f;
(float X, float Z)? site = null;
for (int r = 42; r < 90 && site == null; r += 2)
    for (int k = 0; k < 16 && site == null; k++)
    {
        float angle = k * MathF.Tau / 16, sx0 = here.X + MathF.Cos(angle) * r, sz0 = here.Z + MathF.Sin(angle) * r;
        if (sx0 * sx0 + sz0 * sz0 < keepOut * keepOut) continue;
        if (Structures.GroundProblem((int)StructureKind.ClaimMarker, sx0, sz0, 0f, H) != null) continue;
        if (conn.Db.Structure.Iter().Any(st => Structures.TooClose((int)StructureKind.ClaimMarker, sx0, sz0, (int)st.Kind, st.X, st.Z))) continue;
        // Open ground round it for the hens and racks.
        if (Enumerable.Range(0, 8).Any(i => ground.Stand(sx0 + 4f * MathF.Cos(i * MathF.Tau / 8), sz0 + 4f * MathF.Sin(i * MathF.Tau / 8)) is var (qx, qz)
                && (MathF.Abs(qx - (sx0 + 4f * MathF.Cos(i * MathF.Tau / 8))) > 0.01f || MathF.Abs(qz - (sz0 + 4f * MathF.Sin(i * MathF.Tau / 8))) > 0.01f))) continue;
        site = (sx0, sz0);
    }
if (site is not { } mark) { Fail("no open site for a claim marker 42 m or more from the start"); return 1; }
walker.To(ref bx, ref bz, mark.X - 1.5f, mark.Z, 0.3f, stand: true);
lastError = null;
conn.Reducers.PlaceStructure(StructureKind.ClaimMarker, mark.X, mark.Z, 0);
Pump(() => lastError != null || conn.Db.Structure.Iter().Any(st => st.Kind == StructureKind.ClaimMarker && st.Owner == me!.Value), 10, "the claim marker to be laid out");
if (lastError != null) Fail($"laying out the claim marker at ({mark.X:F1}, {mark.Z:F1}): {lastError}");
var markerSite = conn.Db.Structure.Iter().First(st => st.Kind == StructureKind.ClaimMarker && st.Owner == me!.Value);
Claim? MyClaim() => conn.Db.Claim.Iter().FirstOrDefault(c => c.OwnerPlayerId == Me().PlayerId);
for (int i = 0; i < 25 && MyClaim() == null; i++)
{
    lastError = null;
    conn.Reducers.Build(markerSite.Id);
    var until = clock.Elapsed + TimeSpan.FromMilliseconds(700);
    while (MyClaim() == null && clock.Elapsed < until) { conn.FrameTick(); Thread.Sleep(5); }
}
if (MyClaim() == null) Fail($"the claim marker never completed ({lastError ?? "no error"})");
Ok("A holds a claim");

// B: a second character with no part in the claim.
string gname = "Gest" + new string(Enumerable.Range(0, 8).Select(_ => (char)('a' + Random.Shared.Next(26))).ToArray());
var g = new Guest();
g.Conn = DbConnection.Builder()
    .WithUri(uri)
    .WithDatabaseName(db)
    .OnConnect((c, identity, _) =>
    {
        g.Id = identity;
        c.SubscriptionBuilder()
            .OnApplied(_ => g.Ready = true)
            .Subscribe(new[]
            {
                "SELECT * FROM player", "SELECT * FROM player_pos", "SELECT * FROM animal", "SELECT * FROM terrain_edit",
                "SELECT * FROM chopped_tree", "SELECT * FROM structure", "SELECT * FROM structure_door",
                "SELECT * FROM my_inventory", "SELECT * FROM claim", "SELECT * FROM hide_rack", "SELECT * FROM snare_state", "SELECT * FROM world_item",
            });
    })
    .OnConnectError(e => Fail($"second character connect: {e.Message}"))
    .Build();
g.Conn.OnUnhandledReducerError += (_, e) => g.LastError = e.Message;
guest = g;
Pump(() => g.Ready && g.Conn.Db.Player.Identity.Find(g.Id!.Value) is not null, 30, "the second character");
Pump(() => g.Conn.Db.PlayerPos.PlayerId.Find(g.Conn.Db.Player.Identity.Find(g.Id!.Value)!.PlayerId) is not null, 20, "the second character's position");
g.Conn.Reducers.SetName(gname);
Pump(() => g.Conn.Db.Player.Identity.Find(g.Id!.Value)!.Name == gname, 10, "the second character's name");
uint gid = g.Conn.Db.Player.Identity.Find(g.Id!.Value)!.PlayerId;
var gstart = g.Conn.Db.PlayerPos.PlayerId.Find(gid)!;
float gx = gstart.X, gz = gstart.Z;
var gwalker = new BotWalker(g.Conn, gid, "Animal check", gname, ms =>
{
    for (var until = clock.Elapsed + TimeSpan.FromMilliseconds(ms); clock.Elapsed < until; Thread.Sleep(5)) { conn.FrameTick(); g.Conn.FrameTick(); }
}, msg => Fail(msg));
uint GCount(Item item) => (uint)g.Conn.Db.MyInventory.Iter().Where(r => r.Item == (ushort)item).Sum(r => (long)r.Count);
string GRefused(string what, Action call)
{
    g.LastError = null;
    call();
    Pump(() => g.LastError != null, 15, $"{what} to be refused");
    return g.LastError!;
}
void GWalkTo(float tx, float tz) => gwalker.To(ref gx, ref gz, tx, tz, 0.6f, stand: true);
void AWalkTo(float tx, float tz) => walker.To(ref bx, ref bz, tx, tz, 0.6f, stand: true);

HideRack Hang(float x, float z, uint madeBy)
{
    var before = conn.Db.HideRack.Iter().Select(r => r.Id).ToHashSet();
    lastError = null;
    conn.Reducers.AdminHangHide(x, z, madeBy);
    Pump(() => lastError != null || conn.Db.HideRack.Iter().Any(r => !before.Contains(r.Id)), 10, "a hide to be hung");
    if (lastError != null) Fail($"hanging a hide: {lastError}");
    return conn.Db.HideRack.Iter().First(r => !before.Contains(r.Id));
}

// Eggs inside the claim: refused to B, taken by A.
var yardHen = Spawn(AnimalSpecies.Hen, mark.X + 4f, mark.Z, 2);
GWalkTo(yardHen.X, yardHen.Z);
string yard = GRefused("a stranger taking eggs inside a claim", () => g.Conn.Reducers.CollectEggs(yardHen.Id));
if (!yard.Contains("belong to") || !yard.Contains(Me().Name)) Fail($"a non-member taking eggs inside A's claim was refused with \"{yard}\"");
Ok($"a non-member cannot take eggs from hens in a claim ({yard})");
AWalkTo(yardHen.X, yardHen.Z);
uint eggsA = CountOf(Item.Egg);
lastError = null;
conn.Reducers.CollectEggs(yardHen.Id);
Pump(() => CountOf(Item.Egg) > eggsA || lastError != null, 10, "the owner's eggs");
if (CountOf(Item.Egg) <= eggsA) Fail($"the claim's owner could not take eggs: {lastError}");
Ok("the owner takes eggs from hens in their claim");

// Leather on a rack inside the claim: kept for its maker or the claim's people.
var rackA = Hang(mark.X, mark.Z + 4f, 0);
var rackB = Hang(mark.X - 3f, mark.Z + 4f, gid);
GWalkTo(rackA.X, rackA.Z);
string leather = GRefused("a stranger taking leather inside a claim", () => g.Conn.Reducers.TakeLeather(rackA.Id));
if (!leather.Contains("still drying")) Fail($"a non-member taking someone else's leather in A's claim was refused with \"{leather}\"");
Ok($"a non-member cannot take leather someone else hung ({leather})");
GWalkTo(rackB.X, rackB.Z);
uint leatherB = GCount(Item.Leather);
g.LastError = null;
g.Conn.Reducers.TakeLeather(rackB.Id);
Pump(() => GCount(Item.Leather) > leatherB || g.LastError != null, 10, "the maker's leather");
if (GCount(Item.Leather) <= leatherB) Fail($"B could not take the leather B hung, inside A's claim: {g.LastError}");
Ok("whoever hung a hide takes its leather, even inside another's claim");
AWalkTo(rackA.X, rackA.Z);
uint leatherA = CountOf(Item.Leather);
lastError = null;
conn.Reducers.TakeLeather(rackA.Id);
Pump(() => CountOf(Item.Leather) > leatherA || lastError != null, 10, "the owner's leather");
if (CountOf(Item.Leather) <= leatherA) Fail($"the claim's owner could not take leather hung in their claim: {lastError}");
Ok("the owner takes leather hung in their claim");

// Out in the open anyone takes eggs.
var wildHen = Spawn(AnimalSpecies.Hen, mark.X + Social.ClaimRadius + 8f, mark.Z, 2);
GWalkTo(wildHen.X, wildHen.Z);
uint eggsB = GCount(Item.Egg);
g.LastError = null;
g.Conn.Reducers.CollectEggs(wildHen.Id);
Pump(() => GCount(Item.Egg) > eggsB || g.LastError != null, 10, "eggs from a hen outside every claim");
if (GCount(Item.Egg) <= eggsB) Fail($"B could not take eggs from a hen outside every claim: {g.LastError}");
Ok("anyone takes eggs from hens outside a claim");

// A member of the claim does take eggs there.
conn.Reducers.AddClaimMemberByName(gname);
Pump(() => conn.Db.ClaimMember.Iter().Any(), 10, "B to be added as a member");
var memberHen = Spawn(AnimalSpecies.Hen, mark.X + 4f, mark.Z - 3f, 2);
GWalkTo(memberHen.X, memberHen.Z);
eggsB = GCount(Item.Egg);
g.LastError = null;
g.Conn.Reducers.CollectEggs(memberHen.Id);
Pump(() => GCount(Item.Egg) > eggsB || g.LastError != null, 10, "a member's eggs");
if (GCount(Item.Egg) <= eggsB) Fail($"a member could not take eggs inside the claim: {g.LastError}");
Ok("a claim member takes eggs from hens in the claim");
var rackM = Hang(mark.X + 3f, mark.Z + 4f, 0);
GWalkTo(rackM.X, rackM.Z);
leatherB = GCount(Item.Leather);
g.LastError = null;
g.Conn.Reducers.TakeLeather(rackM.Id);
Pump(() => GCount(Item.Leather) > leatherB || g.LastError != null, 10, "a member's leather");
if (GCount(Item.Leather) <= leatherB) Fail($"a member could not take leather hung in the claim: {g.LastError}");
Ok("a claim member takes leather hung in the claim");

// --- Rabbit snares (animals-34) ---
// A snare set inside A's claim: it takes a rabbit that comes within reach, keeps the catch for its owner
// and the claim's people only, re-sets itself, and falls apart after its last catch.
TrapDef snareDef = Traps.Find(Item.RabbitSnare)!;
float snx = mark.X + 4f, snz = mark.Z + 3f;
AWalkTo(snx - 1f, snz);
Give(Item.RabbitSnare, 1, 60);
Hold(7, Item.RabbitSnare);
lastError = null;
conn.Reducers.PlaceItem((ushort)Item.RabbitSnare, 60, 0u, snx, snz, 0f);
Pump(() => lastError != null || conn.Db.SnareState.Iter().Any(), 10, "the snare to be set down");
if (lastError != null) Fail($"setting the snare down: {lastError}");
var snareRow = conn.Db.SnareState.Iter().First();
Ok("a rabbit snare is set down and the server keeps a row for it");

// Rabbits stand where they are placed (no flock): three on the snare make a catch within a few seconds.
int RabbitsAtSnare() => conn.Db.Animal.Iter().Count(a => a.Species == AnimalSpecies.Rabbit && MathF.Abs(a.X - snx) < 2f && MathF.Abs(a.Z - snz) < 2f);
void WaitForCatch()
{
    // Counted before the spawns: the snare can take the first rabbit while the third is still
    // being placed, so a count taken after them may already be one short (bugfix-59).
    int before = RabbitsAtSnare() + 3;
    for (int i = 0; i < 3; i++) Spawn(AnimalSpecies.Rabbit, snx + 0.1f * i, snz);
    Pump(() => conn.Db.SnareState.ItemId.Find(snareRow.ItemId) is { Caught: not 0 }, 60, "the snare to catch a rabbit");
    Pump(() => RabbitsAtSnare() < before, 10, "the caught rabbit to leave the field");
    Settle(300);
    if (RabbitsAtSnare() != before - 1) Fail($"a catch took {before - RabbitsAtSnare()} rabbits, expected 1");
}
WaitForCatch();
Ok("a rabbit within reach is caught, and only one");

// The catch is kept for the owner and the claim's people.
conn.Reducers.RemoveClaimMember(gid);
Pump(() => !conn.Db.ClaimMember.Iter().Any(), 10, "B to be taken off the claim");
GWalkTo(snx, snz);
string stranger = GRefused("a stranger taking the catch", () => g.Conn.Reducers.CollectSnare(snareRow.ItemId));
if (!stranger.Contains("not yours")) Fail($"a non-member taking the catch was refused with \"{stranger}\"");
string stolen = GRefused("a stranger lifting the snare", () => g.Conn.Reducers.PickUp(snareRow.ItemId));
if (stolen.Length == 0) Fail("a non-member lifted a snare that holds a catch");
Ok($"a non-member cannot take the catch or lift the snare ({stranger}; {stolen})");
lastError = null;
conn.Reducers.PickUp(snareRow.ItemId);
Pump(() => lastError != null, 10, "lifting a snare that holds a catch to be refused");
if (!lastError!.Contains("catch")) Fail($"the owner lifting a snare that holds a catch was refused with \"{lastError}\"");
Ok("a snare holding a catch is not lifted, even by its owner, until the catch is taken");

// A member takes it: the rabbit's meat and fur land beside the snare, and the snare re-sets itself.
conn.Reducers.AddClaimMemberByName(gname);
Pump(() => conn.Db.ClaimMember.Iter().Any(), 10, "B to rejoin the claim");
uint meatBefore = (uint)conn.Db.WorldItem.Iter().Count(i => i.Item == (ushort)Item.RawRabbit);
g.LastError = null;
g.Conn.Reducers.CollectSnare(snareRow.ItemId);
Pump(() => g.LastError != null || conn.Db.SnareState.ItemId.Find(snareRow.ItemId) is { Caught: 0 }, 10, "a member to take the catch");
if (g.LastError != null) Fail($"a claim member could not take the catch: {g.LastError}");
Pump(() => (uint)conn.Db.WorldItem.Iter().Count(i => i.Item == (ushort)Item.RawRabbit) > meatBefore, 10, "the rabbit's meat to appear");
if (!conn.Db.WorldItem.Iter().Any(i => i.Item == (ushort)Item.RabbitFur)) Fail("no rabbit fur beside the snare");
if (Durability.Used(conn.Db.WorldItem.Id.Find(snareRow.ItemId)!.Wear) != 1) Fail("the snare did not count its first catch");
Ok("a claim member takes the catch (meat and fur beside the snare); the snare re-sets itself and counts the catch");

// It lasts a set number of catches, then falls apart.
for (int n = 2; n <= snareDef.Catches; n++)
{
    WaitForCatch();
    lastError = null;
    conn.Reducers.CollectSnare(snareRow.ItemId);
    Pump(() => lastError != null || conn.Db.SnareState.ItemId.Find(snareRow.ItemId) is not { Caught: not 0 }, 10, $"catch {n} to be taken");
    if (lastError != null) Fail($"taking catch {n}: {lastError}");
}
Pump(() => conn.Db.WorldItem.Id.Find(snareRow.ItemId) == null && conn.Db.SnareState.ItemId.Find(snareRow.ItemId) == null, 10, "the snare to fall apart");
Ok($"the snare falls apart after {snareDef.Catches} catches");
g.Conn.Disconnect();

Console.WriteLine("Animal check passed");
conn.Disconnect();
return 0;

sealed class Guest
{
    public DbConnection Conn = null!;
    public Identity? Id;
    public bool Ready;
    public string? LastError;
}
