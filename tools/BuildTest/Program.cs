// House building check: two players gather materials, one lays out a footprint,
// both build it to the end, stand inside it, and it is still there after a
// reconnect. Runs against a local server with the module freshly published.
// Exits non-zero if anything fails.
//
// Usage: dotnet run -- [lean-to|clay-hut|basket|fence|coop|brick-house|log-cabin|both=both] [uri=http://127.0.0.1:3000] [db=lifegame]
// brick-house and log-cabin run on their own: bricks take minutes to dry, and twenty logs take a while to chop.
using System.Diagnostics;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

// What to build: "both" (the default: lean-to, clay hut, basket, fence and gate), or one or more of
// lean-to, clay-hut, basket, fence, brick-house, log-cabin separated by commas. CI runs two parts at once.
string which = args.Length > 0 ? args[0] : "both";
bool Wants(string part) => which == "both" ? part is "lean-to" or "clay-hut" or "basket" or "fence" or "coop" : which.Split(',').Contains(part);
string uri = args.Length > 1 ? args[1] : "http://127.0.0.1:3000";
string db = args.Length > 2 ? args[2] : "lifegame";

var a = new Bot("A", uri, db, null);
var b = new Bot("B", uri, db, null);
var bots = new List<Bot> { a, b };
var clock = Stopwatch.StartNew();
// A's claim, raised past the waking clearing the first time something claim-only is built.
(float x, float z)? claimAt = null;
bool bIsMember = false;

const float Module_UnstuckRadius = 12.5f;

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
foreach (var bot in bots) bot.UsePump(Pump);

// Every run starts from the same place (bugfix-31): players wake at a random spot in the clearing.
a.WalkTo(0f, 0f, 0.3f, Pump);
b.WalkTo(1f, 0f, 0.3f, Pump);

// The tiled house (ceramics-10) is the brick house with a roof of 12 fired tiles.
{
    var tiled = Structures.Shape((int)StructureKind.TiledHouse);
    var brick = Structures.Shape((int)StructureKind.BrickHouse);
    Check(tiled.Room && tiled.BrickWalls && tiled.TileRoof && !brick.TileRoof, "the tiled house is a brick-walled home with a tile roof");
    Check(tiled.Stages.Length == brick.Stages.Length && tiled.Costs.Length == tiled.Stages.Length, "the tiled house has the brick house's stages");
    Check(tiled.Costs[3].Length == 1 && tiled.Costs[3][0] == (Item.RoofTile, 12u), "its roof takes 12 roof tiles");
    for (int i = 0; i < 3; i++) Check(tiled.Costs[i].SequenceEqual(brick.Costs[i]), $"its stage {i + 1} costs what the brick house's does");
    Check(Structures.StageProcess((int)StructureKind.TiledHouse, 1) == Structures.DaubProcess && Structures.StageProcess((int)StructureKind.TiledHouse, 3) == Structures.RoofProcess, "its walls are daub work and its roof is roofing");
}

// The hen coop (building-16): claim-only, with the entry and roost points Animals target.
{
    var coop = Structures.Shape((int)StructureKind.HenCoop);
    Check(!coop.Wild, "the hen coop has to stand inside a claim");
    Check(coop.Stages.Length == 3 && coop.Costs.Length == 3, "the hen coop goes up in three stages");
    Check(coop.Entry.Z > coop.HalfDepth && coop.Entry.Z < coop.HalfDepth + 2f, "the coop's entry is just outside its front");
    Check(MathF.Abs(coop.Roost.X) < coop.HalfWidth && MathF.Abs(coop.Roost.Z) < coop.HalfDepth, "the roost is inside the coop");
    Check(Structures.StageProcess((int)StructureKind.HenCoop, 1) == Structures.WattleProcess && Structures.StageProcess((int)StructureKind.HenCoop, 2) == Structures.RoofProcess, "its walls are wattle work and its roof is roofing");
}

// Building size and doorways (building-14), checked on the shared numbers both sides use.
foreach (var kind in new[] { StructureKind.ClayHut, StructureKind.BrickHouse, StructureKind.LogCabin, StructureKind.TiledHouse })
{
    int k = (int)kind;
    var shape = Structures.Shape(k);
    Check(MathF.Abs(shape.Scale - 1.25f) < 0.001f, $"{shape.Name} is built 1.25x");
    // Walking in along the middle of the doorway crosses no wall when the door is open; a side wall does.
    float wall = kind == StructureKind.ClayHut ? Structures.HutWallRadiusFor(k) : Structures.HouseWallZFor(k);
    Check(!Structures.StepCrossesWalls(k, 0, 0, 0f, false, 0, wall + 0.5f, 0, wall - 0.5f), $"{shape.Name}: the open doorway lets you in");
    Check(Structures.StepCrossesWalls(k, 0, 0, 0f, true, 0, wall + 0.5f, 0, wall - 0.5f), $"{shape.Name}: a shut door keeps you out");
    float side = kind == StructureKind.ClayHut ? Structures.HutWallRadiusFor(k) : Structures.HouseWallXFor(k);
    Check(Structures.StepCrossesWalls(k, 0, 0, 0f, false, -side - 0.5f, 0, -side + 0.5f, 0), $"{shape.Name}: the side wall holds");
    Check(shape.DoorwayZ >= wall - 0.2f, $"{shape.Name}: the door sits in the front wall");
}

if (Wants("lean-to"))
    BuildTogether(StructureKind.LeanTo,
        new() { [Item.Stick] = 5, [Item.Fiber] = 6 },
        new() { [Item.Stick] = 5, [Item.Fiber] = 5 });
if (Wants("clay-hut"))
{
    ulong hut = BuildTogether(StructureKind.ClayHut,
        new() { [Item.Stick] = 11, [Item.Stone] = 2, [Item.Fiber] = 9 },
        new() { [Item.Stick] = 11, [Item.Fiber] = 14, [Item.Clay] = 12 },
        // B turns the clay into daub for the walls: clean it, then mix in fiber.
        prepareB: () =>
        {
            for (int i = 0; i < 6; i++) b.Craft("clay.clean", Pump);
            for (int i = 0; i < 6; i++) b.Craft("clay.daub", Pump);
            Check(b.CountOf(Item.Daub) >= 12, $"mixed daub for the walls ({b.Carried()})");
        });
    HangDoor(hut);
    LayMat(hut);
    UnstuckInHut(hut);
    BuildHearth(hut);
}
if (Wants("basket"))
    StoreInBasket(BuildTogether(StructureKind.Basket,
        new() { [Item.Fiber] = 3, [Item.Stick] = 2 },
        new() { [Item.Fiber] = 9 },
        // B twists two cordage for the basket's stitching.
        prepareB: () =>
        {
            for (int i = 0; i < 2; i++) b.Craft("cord.twist", Pump);
            Check(b.CountOf(Item.Cordage) >= 2, $"twisted cordage for the basket ({b.Carried()})");
        }));

if (Wants("fence"))
{
    WallsStopWalkers(BuildTogether(StructureKind.Fence,
        new() { [Item.Stick] = 2 },
        new() { [Item.Stick] = 2, [Item.Fiber] = 1 }));
    UseGate(BuildTogether(StructureKind.Gate,
        new() { [Item.Stick] = 3 },
        new() { [Item.Stick] = 2, [Item.Fiber] = 2 }));
}

if (Wants("coop"))
    BuildTogether(StructureKind.HenCoop,
        new() { [Item.Stick] = 5, [Item.Fiber] = 5 },
        new() { [Item.Stick] = 4, [Item.Fiber] = 5 });

// Not in "both": sun-dried bricks take minutes to dry, so this runs on its own (brick-house).
if (Wants("brick-house"))
{
    ulong house = BuildTogether(StructureKind.BrickHouse,
        new() { [Item.Stone] = 6, [Item.Stick] = 16, [Item.Fiber] = 20 },
        new() { [Item.Fiber] = 19, [Item.Clay] = 70 },
        prepareA: () =>
        {
            for (int i = 0; i < 2; i++) a.Craft("cord.twist", Pump);
            Check(a.CountOf(Item.Cordage) >= 2, $"twisted cordage for the rafters ({a.Carried()})");
        },
        // B cleans the clay, mixes daub, and molds and dries sixteen bricks.
        prepareB: () =>
        {
            for (int i = 0; i < 35; i++) b.Craft("clay.clean", Pump);
            for (int i = 0; i < 3; i++) b.Craft("clay.daub", Pump);
            for (int i = 0; i < 16; i++) b.Craft("clay.brick", Pump);
            WaitFor(() => b.Conn.Db.ClayPiece.Iter().Count(p => p.Form == (byte)ClayForm.Brick) >= 16, 10, "sixteen bricks set down to dry");
            var until = clock.Elapsed + TimeSpan.FromMinutes(12);
            while (b.CountOf(Item.Brick) < 16 && clock.Elapsed < until)
            {
                foreach (var piece in b.Conn.Db.ClayPiece.Iter().Where(p => p.Form == (byte)ClayForm.Brick).ToList())
                    b.Conn.Reducers.TendClay(piece.Id, false);
                Pump(20000);
            }
            Check(b.CountOf(Item.Brick) >= 16 && b.CountOf(Item.Daub) >= 6, $"dried bricks and mixed daub for the walls ({b.Carried()})");
        });
    HangDoor(house);
}

// Not in "both": each player makes a stone axe and chops ten logs, which takes minutes (log-cabin).
if (Wants("log-cabin"))
{
    ulong cabin = BuildTogether(StructureKind.LogCabin,
        new() { [Item.Stone] = 7, [Item.Stick] = 7, [Item.Fiber] = 3 },
        new() { [Item.Stone] = 3, [Item.Stick] = 7, [Item.Fiber] = 23 },
        prepareA: () => ChopLogs(a, 10),
        // B also twists the two cordage for the rafters.
        prepareB: () =>
        {
            ChopLogs(b, 10);
            for (int i = 0; i < 2; i++) b.Craft("cord.twist", Pump);
            Check(b.CountOf(Item.Cordage) >= 2, $"twisted cordage for the rafters ({b.Carried()})");
        });
    HangDoor(cabin);
}

Console.WriteLine($"All building checks passed in {clock.Elapsed.TotalSeconds:F0}s");
foreach (var bot in bots) bot.Conn.Disconnect();
return 0;

// The server refuses a step straight through a finished fence, even from a hacked client (bugfix-27),
// and lets a jump hop it (a low fence on nobody's claim).
void WallsStopWalkers(ulong id)
{
    Console.WriteLine("--- Walls stop walkers");
    var s = a.Conn.Db.Structure.Id.Find(id)!;
    // Stand 0.9 m from the fence with the same distance on its far side as the target: a step that is
    // short enough to be allowed in one go and free of trunks and boulders, which the server refuses
    // (bugfix-45), however the bot happened to be standing after the last stage.
    var og = new BotGround(a.Conn);
    (float x, float z)? start = null, target = null;
    foreach (var off in new[] { 0f, 0.5f, -0.5f, 0.9f, -0.9f })
        foreach (var side in new[] { 1f, -1f })
        {
            var f = Structures.ToWorld(s.X, s.Z, s.Yaw, off, side * 0.9f);
            var t = Structures.ToWorld(s.X, s.Z, s.Yaw, off, -side * 0.9f);
            if (start != null || Obstacles.StepBlocked(og, 0.4f, f.x, f.z, t.x, t.z, 0f)
                || Obstacles.PushOut(og, 0.4f, f.x, f.z, 0f) != (f.x, f.z) || Obstacles.PushOut(og, 0.4f, t.x, t.z, 0f) != (t.x, t.z)) continue;
            start = f; target = t;
        }
    Check(start != null, "found clear ground either side of the fence");
    a.WalkTo(start!.Value.x, start.Value.z, 0.2f, Pump);
    Pump(1200);
    var (lx, lz) = Structures.ToLocal(s.X, s.Z, s.Yaw, a.X, a.Z);
    var across = target!.Value;
    a.Conn.Reducers.Move(across.x, across.z, 0f);
    Pump(1000);
    var srv = a.Conn.Db.PlayerPos.PlayerId.Find(a.MyId)!;
    var (_, sz) = Structures.ToLocal(s.X, s.Z, s.Yaw, srv.X, srv.Z);
    Check(MathF.Sign(sz) == MathF.Sign(lz), "a step straight through the fence is refused");
    a.X = srv.X; a.Z = srv.Z;
    Pump(1200);
    a.Conn.Reducers.Jump();
    Pump(100);
    a.Conn.Reducers.Move(across.x, across.z, 0f);
    Pump(1000);
    srv = a.Conn.Db.PlayerPos.PlayerId.Find(a.MyId)!;
    (_, sz) = Structures.ToLocal(s.X, s.Z, s.Yaw, srv.X, srv.Z);
    Check(MathF.Sign(sz) == -MathF.Sign(lz), "a jump hops the fence");
    a.X = srv.X; a.Z = srv.Z;
}

ulong BuildTogether(StructureKind kind, Dictionary<Item, uint> forA, Dictionary<Item, uint> forB, Action? prepareB = null, Action? prepareA = null)
{
    string name = Structures.Shape((int)kind).Name;
    // Only basic survival buildings stand outside a claim (building-13): the rest go up inside A's.
    if (!Structures.IsWild((int)kind)) EnsureClaim();
    Console.WriteLine($"--- {name}");

    // Gather side by side.
    a.Wants = forA;
    b.Wants = forB;
    var until = clock.Elapsed + TimeSpan.FromSeconds(420);
    while (bots.Any(x => !x.HasWants) && clock.Elapsed < until)
    {
        foreach (var bot in bots) bot.GatherStep(0.1f);
        Pump(100);
    }
    Check(bots.All(x => x.HasWants), $"both gathered materials for the {name} ({a.Carried()} / {b.Carried()})");
    prepareA?.Invoke();
    prepareB?.Invoke();

    // A lays out a footprint on dry, level ground nearby.
    var site = a.FindSite(kind, Structures.IsWild((int)kind) ? null : claimAt);
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
    var shape = Structures.Shape((int)kind);
    // A fence is thin: stand close, since a walk can stop a meter short and reach is 2.2 m past its line.
    float stand = shape.Round ? shape.Radius + 0.6f : shape.Fence ? 0.9f : Math.Max(1.9f, shape.HalfDepth + 0.6f);
    var front = Structures.ToWorld(sx, sz, s.Yaw, 0.6f, stand);
    // Walk right up to the spot, not "within a meter": stopping 1 m short on the far side of a round hut
    // left B 0.1 m outside the work reach (hut radius + 2.2 m) and every Build was refused.
    b.WalkTo(front.x + 0.6f, front.z, 0.3f, Pump);
    a.WalkTo(front.x - 0.6f, front.z, 0.3f, Pump);
    until = clock.Elapsed + TimeSpan.FromSeconds(180);
    while (a.Conn.Db.Structure.Id.Find(id) is { Complete: false } && clock.Elapsed < until)
    {
        foreach (var bot in bots) bot.TryBuild(id);
        Pump(650);
    }
    Pump(300);
    var done = a.Conn.Db.Structure.Id.Find(id);
    if (done is not { Complete: true })
        // Say where everyone stood, so a reach failure can be told apart from a blocked walk.
        foreach (var (bot, who) in new[] { (a, "A"), (b, "B") })
        {
            var seen = bot.Seen();
            float sd = seen is { } sp ? MathF.Sqrt((sp.X - sx) * (sp.X - sx) + (sp.Z - sz) * (sp.Z - sz)) : -1f;
            Console.WriteLine($"  {who}: bot ({bot.X:F1}, {bot.Z:F1}), server ({seen?.X:F1}, {seen?.Z:F1}), {sd:F1} m from the {name}'s middle at ({sx:F1}, {sz:F1}); reach {Structures.WorkReach} m past its edge, round={shape.Round} radius={shape.Radius}");
        }
    Check(done is { Complete: true }, $"{name} finished (stage {done?.Stage})");
    Check(done!.Builders == 2, $"both players built it (builders: {done.Builders})");
    Check(b.Conn.Db.Structure.Id.Find(id) is { Complete: true }, "the other player sees it finished");

    // Walk in and check the spot counts as sheltered.
    if (Structures.Shape((int)kind).RainCover > 0)
    {
        if (kind == StructureKind.LeanTo)
        {
            // Its sides and back stop walkers (bugfix-34): come in by the open front.
            var lean = Structures.Shape((int)kind);
            var leanFront = Structures.ToWorld(done.X, done.Z, done.Yaw, 0f, lean.HalfDepth + 1.2f);
            a.WalkTo(leanFront.x, leanFront.z, 0.3f, Pump);
        }
        a.WalkTo(sx, sz, 0.3f, Pump);
        Check(Structures.Contains((int)kind, done.X, done.Z, done.Yaw, a.X, a.Z, -0.2f), "standing inside is sheltered");
    }

    // Reconnect and find it still there.
    a.Reconnect(Pump);
    WaitFor(() => a.Ready && a.Conn.Db.Structure.Id.Find(id) is { Complete: true }, 20, "still standing after reconnecting");
    return id;
}

// A hangs a door in the hut's doorway; B, who didn't make it, opens and shuts it.
void HangDoor(ulong hut)
{
    Console.WriteLine("--- Door");
    LeaveHut(hut); // both are still inside after the "standing inside" check; its walls pen them in
    a.Wants = new() { [Item.Stick] = Structures.DoorSticks, [Item.Fiber] = Structures.DoorFiber };
    b.Wants = new();
    var until = clock.Elapsed + TimeSpan.FromSeconds(120);
    while (!a.HasWants && clock.Elapsed < until)
    {
        a.GatherStep(0.1f);
        Pump(100);
    }
    Check(a.HasWants, $"gathered sticks and fiber for a door ({a.Carried()})");
    var s = a.Conn.Db.Structure.Id.Find(hut)!;
    var door = Structures.ToWorld(s.X, s.Z, s.Yaw, 0f, Structures.Shape((int)s.Kind).DoorwayZ + 0.6f);
    a.WalkTo(door.x - 0.4f, door.z, 0.3f, Pump);
    b.WalkTo(door.x + 0.4f, door.z, 0.3f, Pump);
    a.Conn.Reducers.HangDoor(hut);
    WaitFor(() => b.Conn.Db.StructureDoor.StructureId.Find(hut) is { Closed: true }, 10, "door hung, shut");
    b.Conn.Reducers.ToggleDoor(hut);
    WaitFor(() => a.Conn.Db.StructureDoor.StructureId.Find(hut) is { Closed: false }, 10, "the other player opened it");
    b.Conn.Reducers.ToggleDoor(hut);
    WaitFor(() => a.Conn.Db.StructureDoor.StructureId.Find(hut) is { Closed: true }, 10, "and shut it again");

    // A, who hung it, bars it; B can't open it then; A unbars it and B can again. (B is no member of A's claim here.)
    StrangerB();
    a.Conn.Reducers.ToggleLock(hut);
    WaitFor(() => b.Conn.Db.StructureDoor.StructureId.Find(hut) is { Locked: true, Closed: true }, 10, "A barred the door");
    b.Conn.Reducers.ToggleLock(hut);
    b.Conn.Reducers.ToggleDoor(hut);
    Pump(1500);
    Check(a.Conn.Db.StructureDoor.StructureId.Find(hut) is { Locked: true, Closed: true }, "B can't unbar or open it");
    a.Conn.Reducers.ToggleLock(hut);
    WaitFor(() => b.Conn.Db.StructureDoor.StructureId.Find(hut) is { Locked: false }, 10, "A unbarred it");
    b.Conn.Reducers.ToggleDoor(hut);
    WaitFor(() => a.Conn.Db.StructureDoor.StructureId.Find(hut) is { Closed: false }, 10, "and B can open it again");
}

// A player makes a stone axe (a hand axe knapped from a stone, hafted on a stick with
// cordage), then fells trees until they carry this many logs.
void ChopLogs(Bot bot, int logs)
{
    bot.Craft("cord.twist", Pump);
    bot.Craft("knap.hand_axe", Pump, 0.95f);
    bot.Craft("lash.stone_axe", Pump);
    Check(bot.CountOf(Item.StoneAxe) >= 1, $"{bot.Name} made a stone axe ({bot.Carried()})");
    var until = clock.Elapsed + TimeSpan.FromMinutes(12);
    while (bot.CountOf(Item.Log) < logs && clock.Elapsed < until)
    {
        var (tx, tz) = bot.NearestTree();
        var trunk = Trees.Trunk(tx, tz);
        bot.WalkTo(trunk.X, trunk.Z, 1.2f, Pump);
        uint before = bot.CountOf(Item.Log);
        bot.Craft("chop.log", Pump);
        // A tree that fell or was taken by the other player: move on to the next one.
        if (bot.CountOf(Item.Log) == before) Pump(500);
    }
    Check(bot.CountOf(Item.Log) >= logs, $"{bot.Name} chopped {logs} logs ({bot.Carried()})");
}

// A finished gate hangs shut by itself. B opens and shuts it; A, who laid it out, bars it.
void EnsureClaim()
{
    if (claimAt != null) return;
    Console.WriteLine("--- Claim");
    // Outside a claim the brick house is refused (building-13).
    // On clear ground in reach, so the claim rule is what refuses it (not distance or a neighbour).
    var probe = a.FindSite(StructureKind.BrickHouse);
    Check(probe != null, "found clear ground for the claim-only probe");
    a.WalkTo(probe!.Value.Item1, probe.Value.Item2 + 2.5f, 1f, Pump);
    a.LastError = null;
    a.Conn.Reducers.PlaceStructure(StructureKind.BrickHouse, probe.Value.Item1, probe.Value.Item2, 0f);
    WaitFor(() => a.LastError != null, 5, "a claim-only building to be refused outside a claim");
    Check(a.LastError!.Contains("inside your claim"), $"it says it needs a claim ({a.LastError})");
    ulong marker = BuildTogether(StructureKind.ClaimMarker,
        new() { [Item.Stick] = Social.MarkerSticks },
        new() { [Item.Fiber] = Social.MarkerFiber });
    WaitFor(() => a.Conn.Db.Claim.Iter().Any(c => c.StructureId == marker), 10, "the claim is raised");
    var claim = a.Conn.Db.Claim.Iter().First(c => c.StructureId == marker);
    claimAt = (claim.X, claim.Z);
    // B helps build and gather inside it, so A lets B in; tests that need B to be a stranger take B out (StrangerB).
    a.Conn.Reducers.AddClaimMember(b.MyId);
    WaitFor(() => b.Conn.Db.ClaimMember.Iter().Any(m => m.PlayerId == b.MyId), 10, "A let B into the claim");
    bIsMember = true;
}

void StrangerB()
{
    if (!bIsMember) return;
    a.Conn.Reducers.RemoveClaimMember(b.MyId);
    WaitFor(() => !b.Conn.Db.ClaimMember.Iter().Any(m => m.PlayerId == b.MyId), 10, "A took B out of the claim");
    bIsMember = false;
}

void UseGate(ulong gate)
{
    Console.WriteLine("--- Gate");
    // Gates inside a claim open for its members only (D-160): B is a member for the first part,
    // then A takes B out again, so B is a stranger when the gate is barred.
    WaitFor(() => b.Conn.Db.StructureDoor.StructureId.Find(gate) is { Closed: true }, 10, "the finished gate hangs shut");
    var s = a.Conn.Db.Structure.Id.Find(gate)!;
    var side = Structures.ToWorld(s.X, s.Z, s.Yaw, 0f, 0.9f);
    a.WalkTo(side.x - 0.4f, side.z, 0.3f, Pump);
    b.WalkTo(side.x + 0.4f, side.z, 0.3f, Pump);
    b.Conn.Reducers.ToggleDoor(gate);
    WaitFor(() => a.Conn.Db.StructureDoor.StructureId.Find(gate) is { Closed: false }, 10, "the other player opened it");
    b.Conn.Reducers.ToggleDoor(gate);
    WaitFor(() => a.Conn.Db.StructureDoor.StructureId.Find(gate) is { Closed: true }, 10, "and shut it again");
    // A member of the claim bars and unbars its gate too (claims-21: owner, members and the town's Gatekeeper).
    b.Conn.Reducers.ToggleLock(gate);
    WaitFor(() => a.Conn.Db.StructureDoor.StructureId.Find(gate) is { Locked: true }, 10, "B, a member, barred the gate");
    b.Conn.Reducers.ToggleLock(gate);
    WaitFor(() => a.Conn.Db.StructureDoor.StructureId.Find(gate) is { Locked: false }, 10, "and unbarred it");
    StrangerB();
    a.Conn.Reducers.ToggleLock(gate);
    WaitFor(() => b.Conn.Db.StructureDoor.StructureId.Find(gate) is { Locked: true }, 10, "A barred the gate");
    b.Conn.Reducers.ToggleDoor(gate);
    Pump(1500);
    Check(a.Conn.Db.StructureDoor.StructureId.Find(gate) is { Locked: true, Closed: true }, "B can't open it barred");
    a.Conn.Reducers.ToggleLock(gate);
    WaitFor(() => b.Conn.Db.StructureDoor.StructureId.Find(gate) is { Locked: false }, 10, "A unbarred it");
}

// A raised hearth goes inside the finished hut, not outside it. A lays the stones, B the daub.
void BuildHearth(ulong hut)
{
    Console.WriteLine("--- Raised hearth");
    LeaveHut(hut);
    a.Wants = new() { [Item.Stone] = 4, [Item.Stick] = 2, [Item.Fiber] = 2 };
    b.Wants = new() { [Item.Clay] = 4, [Item.Fiber] = 2 };
    var until = clock.Elapsed + TimeSpan.FromSeconds(300);
    while (bots.Any(x => !x.HasWants) && clock.Elapsed < until)
    {
        foreach (var bot in bots) bot.GatherStep(0.1f);
        Pump(100);
    }
    Check(bots.All(x => x.HasWants), $"gathered stones and clay for a hearth ({a.Carried()} / {b.Carried()})");
    for (int i = 0; i < 2; i++) b.Craft("clay.clean", Pump);
    for (int i = 0; i < 2; i++) b.Craft("clay.daub", Pump);
    Check(b.CountOf(Item.Daub) >= 3, $"mixed daub for the surround ({b.Carried()})");

    var s = a.Conn.Db.Structure.Id.Find(hut)!;
    var door = Structures.ToWorld(s.X, s.Z, s.Yaw, 0f, Structures.Shape((int)s.Kind).DoorwayZ + 0.6f);
    a.WalkTo(door.x, door.z, 0.3f, Pump);
    int before = a.Conn.Db.Structure.Count;
    var outside = Structures.ToWorld(s.X, s.Z, s.Yaw, 0f, Structures.Shape((int)s.Kind).Radius + 2f);
    a.Conn.Reducers.PlaceStructure(StructureKind.Hearth, outside.x, outside.z, 0f);
    Pump(1500);
    Check(a.Conn.Db.Structure.Count == before, "a hearth can't go outside a hut");

    var spot = Structures.ToWorld(s.X, s.Z, s.Yaw, -0.2f, -0.7f);
    var inside = Structures.ToWorld(s.X, s.Z, s.Yaw, 0f, 0.4f);
    a.WalkTo(inside.x, inside.z, 0.3f, Pump);
    b.WalkTo(inside.x + 0.3f, inside.z, 0.3f, Pump);
    a.Conn.Reducers.PlaceStructure(StructureKind.Hearth, spot.x, spot.z, s.Yaw);
    WaitFor(() => a.Conn.Db.Structure.Count > before, 10, "hearth laid out inside the hut");
    ulong id = a.Conn.Db.Structure.Iter().First(r => r.Kind == StructureKind.Hearth && !r.Complete).Id;
    until = clock.Elapsed + TimeSpan.FromSeconds(60);
    while (a.Conn.Db.Structure.Id.Find(id) is { Complete: false } && clock.Elapsed < until)
    {
        foreach (var bot in bots) bot.TryBuild(id);
        Pump(650);
    }
    Pump(300);
    var done = b.Conn.Db.Structure.Id.Find(id);
    Check(done is { Complete: true, Builders: 2 }, $"both built the hearth (stage {done?.Stage}, builders {done?.Builders})");

    // A fire lit beside it goes onto the hearth and starts with half as much fuel again (D-132).
    a.Craft("gather.tinder", Pump);
    int fires = a.Conn.Db.Fire.Count;
    a.Craft("fire.hand_drill", Pump);
    WaitFor(() => a.Conn.Db.Fire.Count > fires, 10, "lit a fire beside the hearth");
    var fire = a.Conn.Db.Fire.Iter().OrderByDescending(f => f.LitAt.MicrosecondsSinceUnixEpoch).First();
    Check(MathF.Abs(fire.X - done!.X) < 0.01f && MathF.Abs(fire.Z - done.Z) < 0.01f, "the fire sits on the hearth");
    double seconds = (fire.FuelUntil.MicrosecondsSinceUnixEpoch - fire.LitAt.MicrosecondsSinceUnixEpoch) / 1e6;
    Check(Math.Abs(seconds - Fires.StartSeconds * Structures.HearthFuelScale) < 1, $"and burns {Structures.HearthFuelScale}x as long ({seconds:F0} s)");

    // Walls stop the server's players (bugfix-27): the next stages gather from outside, so go out
    // through the doorway to the yard, as a player would.
    LeaveHut(hut);
}

// The hut's walls (and its shut door) stop the server's players (bugfix-27), so bots that finished
// something inside must go out through the doorway before they gather: the door is opened, then
// both walk out to the yard in front of it.
void LeaveHut(ulong hut)
{
    var hutRow = a.Conn.Db.Structure.Id.Find(hut)!;
    var doorway = Structures.ToWorld(hutRow.X, hutRow.Z, hutRow.Yaw, 0f, Structures.Shape((int)hutRow.Kind).DoorwayZ + 0.6f);
    var yard = Structures.ToWorld(hutRow.X, hutRow.Z, hutRow.Yaw, 0f, Structures.Shape((int)hutRow.Kind).Radius + 2.5f);
    a.WalkTo(doorway.x, doorway.z, 0.3f, Pump);
    if (a.Conn.Db.StructureDoor.StructureId.Find(hut) is { Closed: true })
    {
        a.Conn.Reducers.ToggleDoor(hut);
        WaitFor(() => a.Conn.Db.StructureDoor.StructureId.Find(hut) is { Closed: false }, 10, "the hut door opens for the way out");
    }
    foreach (var bot in bots)
    {
        bot.WalkTo(doorway.x, doorway.z, 0.3f, Pump);
        bot.WalkTo(yard.x, yard.z, 0.5f, Pump);
    }
}

// A weaves a sleeping mat, lays it out inside the hut, and rolls it up again.
void LayMat(ulong hut)
{
    Console.WriteLine("--- Sleeping mat");
    LeaveHut(hut);
    a.Wants = new() { [Item.Fiber] = 8 };
    var until = clock.Elapsed + TimeSpan.FromSeconds(120);
    while (!a.HasWants && clock.Elapsed < until)
    {
        a.GatherStep(0.1f);
        Pump(100);
    }
    Check(a.HasWants, $"gathered fiber for a mat ({a.Carried()})");
    a.Craft("weave.mat", Pump);
    Check(a.CountOf(Item.SleepingMat) == 1, $"wove a sleeping mat ({a.Carried()})");
    var s = a.Conn.Db.Structure.Id.Find(hut)!;
    var door = Structures.ToWorld(s.X, s.Z, s.Yaw, 0f, Structures.Shape((int)s.Kind).DoorwayZ + 0.6f);
    a.WalkTo(door.x, door.z, 0.3f, Pump);
    var bed = Structures.ToWorld(s.X, s.Z, s.Yaw, 0.2f, -0.3f);
    a.WalkTo(bed.x, bed.z, 0.2f, Pump);
    var me = a.MyId;
    a.Conn.Reducers.LayMat(hut);
    WaitFor(() => b.Conn.Db.SleepingMat.PlayerId.Find(me) is { } m && m.StructureId == hut, 10, "mat laid out in the hut, seen by the other player");
    Check(a.CountOf(Item.SleepingMat) == 0, "it left A's hands");
    a.Conn.Reducers.RollUpMat();
    WaitFor(() => a.CountOf(Item.SleepingMat) == 1 && a.Conn.Db.SleepingMat.PlayerId.Find(me) is null, 10, "rolled it up again");
    a.Conn.Reducers.LayMat(hut);
    WaitFor(() => b.Conn.Db.SleepingMat.PlayerId.Find(me) is not null, 10, "and laid it out");

    // Laying a mat does not choose your waking place; A is asked, and the server checks the answer (survival-23).
    Check(!a.Conn.Db.MyRespawnPoint.Iter().Any(), "laying a mat does not set the waking place by itself");
    var mat = RestSpots.OfItem(Item.SleepingMat)!;
    b.LastError = null;
    b.Conn.Reducers.SetRespawn(mat.Id, hut);
    WaitFor(() => b.LastError != null, 5, "B choosing a mat that is not theirs to be refused");
    Check(b.LastError!.Contains("no sleeping mat"), $"it says there is no mat for B ({b.LastError})");
    a.Conn.Reducers.SetRespawn(mat.Id, hut);
    WaitFor(() => a.Conn.Db.MyRespawnPoint.Iter().Any(), 10, "A set the mat as their waking place");
    var point = a.Conn.Db.MyRespawnPoint.Iter().First();
    var laid = a.Conn.Db.SleepingMat.PlayerId.Find(me)!;
    Check(point.StructureId == hut && point.Spot == mat.Id && MathF.Abs(point.X - laid.X) < 0.01f && MathF.Abs(point.Z - laid.Z) < 0.01f,
        $"the waking place is the mat in the hut ({point.X:F1}, {point.Z:F1})");
}

// A stands inside the finished hut (A is at the bed after LayMat) and asks to be moved out: the server
// puts A on free ground within its search radius; a second try inside the cooldown is refused (bugfix-44).
void UnstuckInHut(ulong hut)
{
    Console.WriteLine("--- Unstuck");
    var s = a.Conn.Db.Structure.Id.Find(hut)!;
    var from = a.Conn.Db.PlayerPos.PlayerId.Find(a.MyId)!;
    Check(Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, from.X, from.Z), "A stands inside the hut");
    a.LastError = null;
    a.Conn.Reducers.Unstuck();
    WaitFor(() => a.Conn.Db.PlayerPos.PlayerId.Find(a.MyId) is { } p && (p.X != from.X || p.Z != from.Z) || a.LastError != null, 10, "the server answered unstuck");
    Check(a.LastError == null, $"unstuck was allowed ({a.LastError ?? "-"})");
    var to = a.Conn.Db.PlayerPos.PlayerId.Find(a.MyId)!;
    float moved = MathF.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Z - from.Z) * (to.Z - from.Z));
    Check(!Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, to.X, to.Z), $"A now stands outside the hut ({to.X:F1}, {to.Z:F1})");
    Check(moved <= Module_UnstuckRadius, $"A moved {moved:F1} m, within the search radius");
    a.X = to.X; a.Z = to.Z;
    a.LastError = null;
    a.Conn.Reducers.Unstuck();
    WaitFor(() => a.LastError != null, 10, "a second unstuck inside the cooldown is answered");
    Check(a.LastError!.Contains("again in"), $"the second try is refused for the cooldown ({a.LastError})");

    // B stands on free ground in the yard: unstuck still helps, moving B at least 1.5 m (and within the radius).
    var bFrom = b.Conn.Db.PlayerPos.PlayerId.Find(b.MyId)!;
    Check(!Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, bFrom.X, bFrom.Z, 0.25f), "B stands on free ground");
    b.LastError = null;
    b.Conn.Reducers.Unstuck();
    WaitFor(() => b.Conn.Db.PlayerPos.PlayerId.Find(b.MyId) is { } p && (p.X != bFrom.X || p.Z != bFrom.Z) || b.LastError != null, 10, "the server answered B's unstuck");
    Check(b.LastError == null, $"unstuck from free ground was allowed ({b.LastError ?? "-"})");
    var bTo = b.Conn.Db.PlayerPos.PlayerId.Find(b.MyId)!;
    float bMoved = MathF.Sqrt((bTo.X - bFrom.X) * (bTo.X - bFrom.X) + (bTo.Z - bFrom.Z) * (bTo.Z - bFrom.Z));
    Check(bMoved >= 1.4f && bMoved <= Module_UnstuckRadius, $"B was nudged {bMoved:F1} m");
    b.X = bTo.X; b.Z = bTo.Z;
}

// A puts sticks in the basket; B takes one out.
void StoreInBasket(ulong basket)
{
    Console.WriteLine("--- Storing");
    var s = a.Conn.Db.Structure.Id.Find(basket)!;
    a.WalkTo(s.X + 0.9f, s.Z, 0.3f, Pump);
    b.WalkTo(s.X - 0.9f, s.Z, 0.3f, Pump);
    uint sticks = a.CountOf(Item.Stick);
    Check(sticks >= 2, $"A carries sticks to store ({a.Carried()})");
    var stack = a.Conn.Db.MyInventory.Iter().First(i => i.Item == (ushort)Item.Stick);
    a.Conn.Reducers.BasketPut(basket, stack.Item, stack.Quality, 2);
    WaitFor(() => b.Conn.Db.BasketItem.Iter().Any(i => i.StructureId == basket && i.Count == 2), 10, "two sticks in the basket, seen by the other player");
    Check(a.CountOf(Item.Stick) == sticks - 2, "they left A's hands");
    uint bSticks = b.CountOf(Item.Stick);
    var row = b.Conn.Db.BasketItem.Iter().First(i => i.StructureId == basket);
    b.Conn.Reducers.BasketTake(row.Id, 1);
    WaitFor(() => b.CountOf(Item.Stick) == bSticks + 1, 10, "B took one out");
    WaitFor(() => a.Conn.Db.BasketItem.Iter().Any(i => i.StructureId == basket && i.Count == 1), 10, "one left inside");
}

sealed class Bot
{
    public DbConnection Conn;
    public bool Ready;
    public float X, Z;
    public Dictionary<Item, uint> Wants = new();
    public string? LastError;

    readonly string name, uri, db;
    public string Name => name;
    string? token;
    Identity? me;
    public uint MyId => myId;
    uint myId;
    ulong targetItem;
    readonly HashSet<ulong> gone = new();
    // What each bot is heading for, so two bots do not race for the same ground item.
    static readonly Dictionary<string, ulong> gatherTargets = new();
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
                        "SELECT * FROM structure_door",
                        "SELECT * FROM basket_item",
                        "SELECT * FROM sleeping_mat",
                        "SELECT * FROM my_respawn_point",
                        "SELECT * FROM fire",
                        "SELECT * FROM chopped_tree",
                        "SELECT * FROM clay_piece",
                        "SELECT * FROM my_inventory",
                        "SELECT * FROM my_craft_job",
                        "SELECT * FROM claim",
                        "SELECT * FROM claim_member",
                    });
            })
            .OnConnectError(e => Console.WriteLine($"{name} connect error: {e.Message}"));
        if (token != null) builder = builder.WithToken(token);
        var conn = builder.Build();
        conn.OnUnhandledReducerError += (_, e) =>
        {
            LastError = e.Message;
            if (!e.Message.Contains("Still working")) Console.WriteLine($"  {name}: {e.Message}");
        };
        return conn;
    }

    public void Reconnect(Action<int> pump)
    {
        Conn.Disconnect();
        pump(500);
        Ready = false;
        Conn = Connect();
        // A login that already has a character waits at the character menu (survival-24): play it.
        for (int i = 0; i < 400 && !Ready; i++) pump(50);
        if (Conn.Identity is { } login) Conn.Reducers.SelectCharacter(login);
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
            float clayLeft = StepRouted(cx * World.CellSize - 0.8f, cz * World.CellSize, dt);
            if (GatherStalled(clayLeft, 0.3f)) WalkTo(cx * World.CellSize - 0.8f, cz * World.CellSize, 0.3f, gatherPump!);
            else if (clayLeft < 0.3f && Lag() < 0.8f && actionTimer <= 0)
            {
                actionTimer = 0.7f;
                if (DugOut(cx, cz)) digCell = null;
                else Conn.Reducers.Dig(cx, cz);
            }
            return;
        }

        // The other bot got there first ("That item is gone"): never go for that one again, and pick another.
        if (targetItem != 0 && LastError?.Contains("That item is gone") == true)
        {
            gone.Add(targetItem);
            targetItem = 0;
            LastError = null;
        }
        var item = Conn.Db.WorldItem.Id.Find(targetItem);
        if (item == null || !missing.Contains((Item)item.Item) || gone.Contains(item.Id))
        {
            // Nearest item neither of us has lost, and not the one the other bot is already heading for.
            var candidates = Conn.Db.WorldItem.Iter()
                .Where(i => missing.Contains((Item)i.Item) && i.Item != (ushort)Item.Clay && !gone.Contains(i.Id))
                .OrderBy(i => (i.X - X) * (i.X - X) + (i.Z - Z) * (i.Z - Z)).ToList();
            item = candidates.FirstOrDefault(i => !gatherTargets.Any(t => t.Key != name && t.Value == i.Id)) ?? candidates.FirstOrDefault();
            targetItem = item?.Id ?? 0;
            gatherTargets[name] = targetItem;
            if (item == null)
            {
                // Nothing left in the world to pick up: say so once, so a timeout reads as scarcity, not a stall.
                if (!noneLogged)
                {
                    noneLogged = true;
                    Console.WriteLine($"::warning::BuildTest {name}: no world item left for {string.Join(", ", missing)} (carrying {Carried()})");
                }
                return;
            }
            noneLogged = false;
        }
        float itemLeft = StepRouted(item.X, item.Z, dt);
        if (GatherStalled(itemLeft, 1.2f)) WalkTo(item.X, item.Z, 1.0f, gatherPump!);
        else if (itemLeft < 1.2f && Lag() < 0.8f && actionTimer <= 0)
        {
            actionTimer = 0.3f;
            Conn.Reducers.PickUp(item.Id);
        }
    }

    // Gathering is walked a tick at a time, so it cannot use the blocking walk until it is stuck: when the
    // server has not moved the bot 0.3 m in 3 s and the bot is still out of reach of its target, hand the
    // walk to the shared walker (sidesteps, then a logged reposition) so a wedged bot cannot fail the check.
    // Standing at the target (digging, picking up) is not a stall.
    bool noneLogged;
    readonly Stopwatch gatherStill = Stopwatch.StartNew();
    float gatherMarkX, gatherMarkZ;
    Action<int>? gatherPump;

    public void UsePump(Action<int> pump) => gatherPump = pump;

    bool GatherStalled(float left, float reach)
    {
        if (gatherPump == null || Seen() is not { } srv) return false;
        if (left < reach || MathF.Abs(srv.X - gatherMarkX) + MathF.Abs(srv.Z - gatherMarkZ) >= BotWalker.StallMetres)
        {
            gatherMarkX = srv.X; gatherMarkZ = srv.Z;
            gatherStill.Restart();
            return false;
        }
        if (gatherStill.Elapsed.TotalSeconds < BotWalker.StallSeconds) return false;
        gatherStill.Restart();
        return true;
    }

    // A cell the load test or an earlier run already dug to the bottom gives no more clay.
    bool DugOut(int x, int z) =>
        (Conn.Db.TerrainEdit.CellKey.Find(World.CellKey(x, z))?.Depth ?? 0) >= World.MaxDigDepth;

    (int, int) NearestClay()
    {
        int x0 = World.CellOf(X), z0 = World.CellOf(Z);
        for (int r = 0; r < 80; r++)
            for (int x = x0 - r; x <= x0 + r; x++)
                for (int z = z0 - r; z <= z0 + r; z++)
                    if (Math.Max(Math.Abs(x - x0), Math.Abs(z - z0)) == r && World.IsClay(x, z)
                        && World.Height(x, z, 0) > Structures.WaterLevel + 0.4f && !DugOut(x, z))
                        return (x, z);
        throw new Exception("No clay nearby");
    }

    // Where the server has this player.
    public PlayerPos? Seen() => Conn.Db.PlayerPos.PlayerId.Find(myId);

    // How far the server still has this player from where the bot thinks it stands.
    float lastSx, lastSz, stuckFor, detourLeft, detourSign = 1f;
    BotGround? ground;
    BotGround Ground => ground ??= new BotGround(Conn);

    public float Lag() => Conn.Db.PlayerPos.PlayerId.Find(myId) is { } seen ? MathF.Abs(seen.X - X) + MathF.Abs(seen.Z - Z) : 0f;

    // Like Step, but round trunks, boulders and buildings by a route (bugfix-34): follows waypoints
    // and works the route out again when the target changes or every few seconds. Returns the distance to the target.
    List<(float X, float Z)> route = new();
    float routeTx = float.NaN, routeTz, routeAge;

    float StepRouted(float tx, float tz, float dt)
    {
        routeAge += dt;
        if (route.Count == 0 || MathF.Abs(tx - routeTx) > 0.5f || MathF.Abs(tz - routeTz) > 0.5f || routeAge > 3f)
        {
            route = Ground.Route(X, Z, tx, tz);
            routeTx = tx; routeTz = tz; routeAge = 0f;
        }
        while (route.Count > 1 && (route[0].X - X) * (route[0].X - X) + (route[0].Z - Z) * (route[0].Z - Z) < 0.16f) route.RemoveAt(0);
        Step(route[0].X, route[0].Z, dt);
        return MathF.Sqrt((tx - X) * (tx - X) + (tz - Z) * (tz - Z));
    }

    // Moves up to one tick's walk toward a point and returns the distance left.
    float Step(float tx, float tz, float dt)
    {
        float dx = tx - X, dz = tz - Z, dist = MathF.Sqrt(dx * dx + dz * dz);
        // Walls bind the server's player now (bugfix-27): when the server keeps us put while we
        // push on, step aside like a player would instead of walking into the wall for ever.
        if (Conn.Db.PlayerPos.PlayerId.Find(myId) is { } srv)
        {
            bool lagging = MathF.Abs(srv.X - X) + MathF.Abs(srv.Z - Z) > 1.0f;
            bool still = MathF.Abs(srv.X - lastSx) + MathF.Abs(srv.Z - lastSz) < 0.05f;
            lastSx = srv.X; lastSz = srv.Z;
            stuckFor = lagging && still ? stuckFor + dt : 0f;
            if (stuckFor > 1.2f)
            {
                X = srv.X; Z = srv.Z;
                detourLeft = 1.6f; detourSign = -detourSign; stuckFor = 0f;
                dx = tx - X; dz = tz - Z; dist = MathF.Sqrt(dx * dx + dz * dz);
            }
        }
        if (detourLeft > 0f && dist > 0.05f)
        {
            detourLeft -= dt;
            float px = -dz / dist * detourSign, pz = dx / dist * detourSign;
            X += px * 5f * dt; Z += pz * 5f * dt;
            Conn.Reducers.Move(X, Z, MathF.Atan2(px, pz));
            return dist;
        }
        float moved = 0f; // how far this call really walked: waiting for the server is not progress
        if (dist > 0.05f)
        {
            float step = MathF.Min(dist, 5f * dt);
            // The server limits the pace (World.MoveAllowance, Carry.WalkFactor): only walk on once
            // it has caught up, and keep reporting where we are, as a real client does (bugfix-24).
            if (Conn.Db.PlayerPos.PlayerId.Find(myId) is not { } seen || MathF.Abs(seen.X - X) + MathF.Abs(seen.Z - Z) < 0.8f)
            {
                float px = X, pz = Z;
                X += dx / dist * step;
                Z += dz / dist * step;
                (X, Z) = Ground.Slide(px, pz, X, Z); // round things and walls, as a player meets them
                moved = step;
            }
            Conn.Reducers.Move(X, Z, MathF.Atan2(dx, dz));
        }
        else if (Lag() > 0.05f)
            Conn.Reducers.Move(X, Z, 0f); // there already: keep reporting until the server agrees
        return moved > 0f ? MathF.Sqrt((tx - X) * (tx - X) + (tz - Z) * (tz - Z)) : dist;
    }

    // Walks to a point and checks the SERVER has the bot there (the shared walk, tools/common/BotWalker.cs:
    // routes round trunks and walls, sidesteps a stall, and as a last resort puts the bot at the target).
    public void WalkTo(float tx, float tz, float within, Action<int> pump)
    {
        if (walker == null || walkerConn != Conn)
        {
            walker = new BotWalker(Conn, myId, "Building check", name, pump, msg => { Console.WriteLine("FAIL  " + msg); Environment.Exit(1); });
            walkerConn = Conn;
        }
        walker.To(ref X, ref Z, tx, tz, within, stand: true);
    }

    BotWalker? walker;
    DbConnection? walkerConn;

    // The nearest standing tree nobody has started on.
    public (int X, int Z) NearestTree()
    {
        int cx = World.CellOf(X), cz = World.CellOf(Z);
        for (int r = 0; r < 60; r++)
            for (int i = cx - r; i <= cx + r; i++)
                for (int j = cz - r; j <= cz + r; j++)
                {
                    if (Math.Max(Math.Abs(i - cx), Math.Abs(j - cz)) != r || !Trees.GrowsAt(i, j)) continue;
                    long key = World.CellKey(i, j);
                    if (Conn.Db.TerrainEdit.CellKey.Find(key) != null || Conn.Db.ChoppedTree.CellKey.Find(key) != null) continue;
                    return (i, j);
                }
        throw new Exception("no tree within 60 m");
    }

    // inClaim: look round that claim's centre and keep to its inner ground (claim-only kinds, building-13).
    public (float, float)? FindSite(StructureKind kind, (float x, float z)? inClaim = null)
    {
        float H(float x, float z) => Structures.GroundAt(x, z, (i, j) => Conn.Db.TerrainEdit.CellKey.Find(World.CellKey(i, j))?.Depth ?? 0);
        // The server refuses claims in the waking clearing; start the search past it.
        const float keepOut = Social.SpawnClearRadius + Social.ClaimRadius + 0.5f;
        float cx = inClaim?.x ?? X, cz = inClaim?.z ?? Z;
        for (int r = 4; r < 80; r++)
            for (int k = 0; k < 16; k++)
            {
                float angle = k * MathF.Tau / 16, x = cx + MathF.Cos(angle) * r, z = cz + MathF.Sin(angle) * r;
                if (kind == StructureKind.ClaimMarker && x * x + z * z < keepOut * keepOut) continue;
                if (inClaim is { } c && (x - c.x) * (x - c.x) + (z - c.z) * (z - c.z) > (Social.ClaimRadius - 5f) * (Social.ClaimRadius - 5f)) continue;
                if (Structures.GroundProblem((int)kind, x, z, 0f, H) != null) continue;
                if (Conn.Db.Structure.Iter().Any(s => Structures.TooClose((int)kind, x, z, (int)s.Kind, s.X, s.Z))) continue;
                return (x, z);
            }
        return null;
    }

    // Starts a craft and waits for it to finish. The job row can reach us after the
    // inputs leave the inventory, so an empty job table alone doesn't mean done: wait
    // until the job has come and gone, or the made item has turned up.
    // World tool actions need the tool on the bar and equipped (crafting-29). Equipping toggles, so only once.
    bool axeEquipped;

    public void Craft(string processId, Action<int> pump, float score = 0.6f)
    {
        var def = Processes.Find(processId)!;
        if (def.Outcome == Outcome.Chop)
        {
            Conn.Reducers.SetHotbarSlot(9, (ushort)Item.StoneAxe);
            if (!axeEquipped) { Conn.Reducers.EquipSlot(9); axeEquipped = true; }
        }
        uint before = CountOf(def.Makes);
        Conn.Reducers.StartCraft(processId, score);
        var until = DateTime.UtcNow.AddSeconds(def.Seconds * 1.2f + 5);
        bool seen = false;
        while (DateTime.UtcNow < until)
        {
            pump(100);
            if (Conn.Db.MyCraftJob.Count > 0) seen = true;
            else if (seen || CountOf(def.Makes) > before) break;
        }
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
