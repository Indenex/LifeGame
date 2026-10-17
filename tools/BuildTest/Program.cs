// House building check: two players gather materials, one lays out a footprint,
// both build it to the end, stand inside it, and it is still there after a
// reconnect. Runs against a local server with the module freshly published.
// Exits non-zero if anything fails.
//
// Usage: dotnet run -- [lean-to|clay-hut|basket|fence|brick-house|log-cabin|both=both] [uri=http://127.0.0.1:3000] [db=lifegame]
// brick-house and log-cabin run on their own: bricks take minutes to dry, and twenty logs take a while to chop.
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
    BuildHearth(hut);
}
if (which is "basket" or "both")
    StoreInBasket(BuildTogether(StructureKind.Basket,
        new() { [Item.Fiber] = 3, [Item.Stick] = 2 },
        new() { [Item.Fiber] = 9 },
        // B twists two cordage for the basket's stitching.
        prepareB: () =>
        {
            for (int i = 0; i < 2; i++) b.Craft("cord.twist", Pump);
            Check(b.CountOf(Item.Cordage) >= 2, $"twisted cordage for the basket ({b.Carried()})");
        }));

if (which is "fence" or "both")
{
    BuildTogether(StructureKind.Fence,
        new() { [Item.Stick] = 2 },
        new() { [Item.Stick] = 2, [Item.Fiber] = 1 });
    UseGate(BuildTogether(StructureKind.Gate,
        new() { [Item.Stick] = 3 },
        new() { [Item.Stick] = 2, [Item.Fiber] = 2 }));
}

// Not in "both": sun-dried bricks take minutes to dry, so this runs on its own (brick-house).
if (which is "brick-house")
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
if (which is "log-cabin")
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

ulong BuildTogether(StructureKind kind, Dictionary<Item, uint> forA, Dictionary<Item, uint> forB, Action? prepareB = null, Action? prepareA = null)
{
    string name = Structures.Shape((int)kind).Name;
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
    var shape = Structures.Shape((int)kind);
    // A fence is thin: stand close, since a walk can stop a meter short and reach is 2.2 m past its line.
    float stand = shape.Round ? shape.Radius + 0.6f : shape.Fence ? 0.9f : Math.Max(1.9f, shape.HalfDepth + 0.6f);
    var front = Structures.ToWorld(sx, sz, s.Yaw, 0.6f, stand);
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

    // A, who hung it, bars it; B can't open it then; A unbars it and B can again.
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
void UseGate(ulong gate)
{
    Console.WriteLine("--- Gate");
    WaitFor(() => b.Conn.Db.StructureDoor.StructureId.Find(gate) is { Closed: true }, 10, "the finished gate hangs shut");
    var s = a.Conn.Db.Structure.Id.Find(gate)!;
    var side = Structures.ToWorld(s.X, s.Z, s.Yaw, 0f, 0.9f);
    a.WalkTo(side.x - 0.4f, side.z, 0.3f, Pump);
    b.WalkTo(side.x + 0.4f, side.z, 0.3f, Pump);
    b.Conn.Reducers.ToggleDoor(gate);
    WaitFor(() => a.Conn.Db.StructureDoor.StructureId.Find(gate) is { Closed: false }, 10, "the other player opened it");
    b.Conn.Reducers.ToggleDoor(gate);
    WaitFor(() => a.Conn.Db.StructureDoor.StructureId.Find(gate) is { Closed: true }, 10, "and shut it again");
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
}

// A weaves a sleeping mat, lays it out inside the hut, and rolls it up again.
void LayMat(ulong hut)
{
    Console.WriteLine("--- Sleeping mat");
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
    WaitFor(() => b.Conn.Db.SleepingMat.PlayerId.Find(me) is not null, 10, "and laid it out to wake on");
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

    readonly string name, uri, db;
    public string Name => name;
    string? token;
    Identity? me;
    public uint MyId => myId;
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
                        "SELECT * FROM structure_door",
                        "SELECT * FROM basket_item",
                        "SELECT * FROM sleeping_mat",
                        "SELECT * FROM fire",
                        "SELECT * FROM chopped_tree",
                        "SELECT * FROM clay_piece",
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
            if (Step(cx * World.CellSize - 0.8f, cz * World.CellSize, dt) < 0.3f && Lag() < 0.8f && actionTimer <= 0)
            {
                actionTimer = 0.7f;
                if (DugOut(cx, cz)) digCell = null;
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
        if (Step(item.X, item.Z, dt) < 1.2f && Lag() < 0.8f && actionTimer <= 0)
        {
            actionTimer = 0.3f;
            Conn.Reducers.PickUp(item.Id);
        }
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
    public float Lag() => Conn.Db.PlayerPos.PlayerId.Find(myId) is { } seen ? MathF.Abs(seen.X - X) + MathF.Abs(seen.Z - Z) : 0f;

    // Moves up to one tick's walk toward a point and returns the distance left.
    float Step(float tx, float tz, float dt)
    {
        float dx = tx - X, dz = tz - Z, dist = MathF.Sqrt(dx * dx + dz * dz);
        if (dist > 0.05f)
        {
            float step = MathF.Min(dist, 5f * dt);
            // The server limits the pace (World.MoveAllowance, Carry.WalkFactor): only walk on once
            // it has caught up, and keep reporting where we are, as a real client does (bugfix-24).
            if (Conn.Db.PlayerPos.PlayerId.Find(myId) is not { } seen || MathF.Abs(seen.X - X) + MathF.Abs(seen.Z - Z) < 0.8f)
            {
                X += dx / dist * step;
                Z += dz / dist * step;
            }
            Conn.Reducers.Move(X, Z, MathF.Atan2(dx, dz));
        }
        else if (Lag() > 0.05f)
            Conn.Reducers.Move(X, Z, 0f); // there already: keep reporting until the server agrees
        return MathF.Max(0, dist - 5f * dt);
    }

    // Walks to a point and checks the SERVER has the bot there. The walk is a straight line, so a tree
    // trunk or boulder on the way (they block movement now) can hold the server's position back while
    // the bot believes it arrived, and every Build after that is "Out of reach". When the server is
    // still short, adopt where it has us and go round the side, a little wider each time.
    public void WalkTo(float tx, float tz, float within, Action<int> pump)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            WalkStraight(tx, tz, within, pump);
            if (Conn.Db.PlayerPos.PlayerId.Find(myId) is not { } seen) return;
            float left = MathF.Sqrt((seen.X - tx) * (seen.X - tx) + (seen.Z - tz) * (seen.Z - tz));
            if (left <= within + 1f) return;
            X = seen.X;
            Z = seen.Z;
            float dx = tx - X, dz = tz - Z, d = MathF.Sqrt(dx * dx + dz * dz);
            float side = (attempt % 2 == 0 ? 1f : -1f) * (3f + attempt);
            if (d > 0.1f) WalkStraight(X - dz / d * side, Z + dx / d * side, 0.5f, pump);
        }
    }

    void WalkStraight(float tx, float tz, float within, Action<int> pump)
    {
        while (Step(tx, tz, 0.1f) > within) pump(100);
        // The server limits the pace (bugfix-24): keep reporting until it has us here too.
        for (int i = 0; i < 300 && Lag() > 0.3f; i++)
        {
            Conn.Reducers.Move(X, Z, 0f);
            pump(100);
        }
        pump(300);
    }

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

    // Starts a craft and waits for it to finish. The job row can reach us after the
    // inputs leave the inventory, so an empty job table alone doesn't mean done: wait
    // until the job has come and gone, or the made item has turned up.
    public void Craft(string processId, Action<int> pump, float score = 0.6f)
    {
        var def = Processes.Find(processId)!;
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
