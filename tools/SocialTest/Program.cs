// End-to-end check of chat, claims and claim permissions against a running local
// server: two players meet outside the waking clearing, one raises a claim, and the
// other is kept from digging or taking items there until invited in.
//
// Usage: dotnet run -- [uri=http://127.0.0.1:3000] [db=lifegame] [decay=0|1]
// Run it on a fresh database: it lays out a claim marker at (35, 30).
// With decay=1 it also waits for the claim to fall.
// It sets the newcomer grace to 0 (and with decay=1 the claim lifetime to 30 s) itself,
// through spacetimedb-cli as the database owner, so a local run matches CI.
using System.Diagnostics;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

string uri = args.Length > 0 ? args[0] : "http://127.0.0.1:3000";
string db = args.Length > 1 ? args[1] : "lifegame";
bool decay = args.Length > 2 && args[2] == "1";

// Admin settings the checks below rely on. Grace is fixed when a player joins, so
// this runs before A and B connect.
AsOwner("set_newcomer_grace", "0");
if (decay) AsOwner("set_claim_lifetime", "30");

var a = new Tester("A", uri, db);
var b = new Tester("B", uri, db);
var all = new[] { a, b };
int failures = 0;

// Calls an admin reducer as the CLI's identity, which published the database. Runs
// from the temp folder: inside server/ the CLI reads spacetime.json and misparses.
void AsOwner(string reducer, string value)
{
    var run = Process.Start(new ProcessStartInfo("spacetimedb-cli", new[] { "call", "-s", uri, db, reducer, value })
    {
        WorkingDirectory = Path.GetTempPath(),
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    })!;
    string output = run.StandardOutput.ReadToEnd() + run.StandardError.ReadToEnd();
    run.WaitForExit();
    if (run.ExitCode != 0)
    {
        Console.WriteLine($"FAIL  {reducer} {value} as the database owner: {output.Trim()}");
        Environment.Exit(1);
    }
    Console.WriteLine($"Set {reducer} {value}");
}

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

// A gathers what a marker needs (and a second marker's worth for the town check at the end, claims-19); B gathers one marker's worth.
foreach (var gatherer in new[] { a, b })
foreach (var (kind, count) in new[] { (Item.Stick, (int)Social.MarkerSticks * (gatherer == a ? 2 : 1)), (Item.Fiber, (int)Social.MarkerFiber * (gatherer == a ? 2 : 1)), (Item.Stone, gatherer == a ? 1 : 0) })
{
    while (gatherer.Count(kind) < count)
    {
        var item = gatherer.Conn.Db.WorldItem.Iter().Where(i => i.Item == (ushort)kind)
            .OrderBy(i => (i.X - gatherer.X) * (i.X - gatherer.X) + (i.Z - gatherer.Z) * (i.Z - gatherer.Z)).First();
        Walk(gatherer, item.X, item.Z);
        uint before = gatherer.Count(kind);
        gatherer.Conn.Reducers.PickUp(item.Id);
        Pump(() => gatherer.Count(kind) > before || gatherer.LastError != null, 5, "pick up");
        gatherer.LastError = null;
    }
}
Console.WriteLine($"A gathered {a.Count(Item.Stick)} sticks, {a.Count(Item.Fiber)} fiber, {a.Count(Item.Stone)} stone");

// Markers are refused in the waking clearing.
Walk(a, 2, 2);
ExpectError(a, () => a.Conn.Reducers.PlaceStructure(StructureKind.ClaimMarker, 3, 2, 0), "clearing", "no claims in the waking clearing");

// A lays out a marker like any structure and builds it; the claim starts when it stands.
// Other checks share this database and may have built near here, so take the first
// spot near (34, 30) clear of every building and every other claim.
bool ClearSpot(float x, float z)
{
    float keepOut = Social.SpawnClearRadius + Social.ClaimRadius + 4f;
    if (x * x + z * z < keepOut * keepOut) return false;
    foreach (var s in a.Conn.Db.Structure.Iter())
    {
        float dx = s.X - x, dz = s.Z - z, min = s.Kind == StructureKind.ClaimMarker ? Social.ClaimRadius * 2f + 2f : 14f;
        if (dx * dx + dz * dz < min * min) return false;
    }
    return true;
}
float cx = 34, cz = 30;
for (int ring = 0; ring <= 20 && !ClearSpot(cx + 1, cz); ring++)
    for (int step = 0; step < Math.Max(1, ring * 6); step++)
    {
        float angle = step * MathF.Tau / Math.Max(1, ring * 6), r = ring * 4f;
        float x = 34 + r * MathF.Cos(angle), z = 30 + r * MathF.Sin(angle);
        if (ClearSpot(x + 1, z)) { cx = x; cz = z; break; }
    }
Console.WriteLine($"claim marker spot {cx + 1:F1},{cz:F1}");
Walk(a, cx, cz);
Walk(b, cx + 4, cz);
a.LastError = null;
a.Conn.Reducers.PlaceStructure(StructureKind.ClaimMarker, cx + 1, cz, 0);
Pump(() => a.Conn.Db.Structure.Iter().Any(s => s.Kind == StructureKind.ClaimMarker && s.Owner == a.Conn.Identity) || a.LastError != null, 5, "marker laid out");
if (a.LastError != null) { Console.WriteLine($"FAIL  laying out the marker: {a.LastError}"); Environment.Exit(1); }
var site = a.Conn.Db.Structure.Iter().First(s => s.Kind == StructureKind.ClaimMarker && s.Owner == a.Conn.Identity);
Check(a.MyClaim() == null, "no claim until the marker is built");
ExpectError(b, () => b.Conn.Reducers.PlaceStructure(StructureKind.ClaimMarker, cx + 4, cz + 3, 0), "m further", "B cannot lay out a marker next to A's, and is told how far to move");
for (int i = 0; i < 20 && a.MyClaim() == null; i++)
{
    a.Conn.Reducers.Build(site.Id);
    Wait(() => a.MyClaim() != null, 0.7);
    a.LastError = null;
}
Pump(() => a.MyClaim() != null, 5, "claim raised");
var claim = a.MyClaim()!;
Check(a.Count(Item.Stick) == Social.MarkerSticks && a.Count(Item.Fiber) == Social.MarkerFiber, "marker used up 4 sticks and 2 fiber (a second marker's worth is kept for the town check)");
Check(claim.StructureId == site.Id && claim.X == site.X && claim.Z == site.Z, "the claim sits on the finished marker");
ExpectError(b, () => b.Conn.Reducers.PlaceStructure(StructureKind.LeanTo, b.X + 2, b.Z + 2, 0), "claimed", "B cannot build inside A's claim");

// Chat reaches the player standing nearby.
a.Conn.Reducers.SendChat("hello neighbour");
Pump(() => b.Conn.Db.MyChat.Iter().Any(m => m.Text == "hello neighbour"), 5, "chat");
var heard = b.Conn.Db.MyChat.Iter().First(m => m.Text == "hello neighbour");
Check(new Vector(heard.X - b.X, heard.Z - b.Z).Length <= Social.ChatRange, "B hears A's chat within range");

// claims-6: "/me" actions are stored the same way whatever the case, and an empty one says nothing.
var afterHello = DateTime.UtcNow.AddSeconds(0.5);
Pump(() => DateTime.UtcNow > afterHello, 2, "chat cooldown");
a.Conn.Reducers.SendChat("/ME waves");
Pump(() => b.Conn.Db.MyChat.Iter().Any(m => m.Text == "/me waves"), 5, "emote");
Check(Social.IsEmote("/me waves") && Social.EmoteBody("/me waves") == "waves", "an action reads as \"waves\"");
var afterWave = DateTime.UtcNow.AddSeconds(0.5);
Pump(() => DateTime.UtcNow > afterWave, 2, "chat cooldown");
int chatCount = b.Conn.Db.MyChat.Iter().Count();
a.LastError = null;
a.Conn.Reducers.SendChat("/me");
Wait(() => b.Conn.Db.MyChat.Iter().Count() > chatCount || a.LastError != null, 1.5);
Check(b.Conn.Db.MyChat.Iter().Count() == chatCount && a.LastError == null, "an empty /me says nothing");
Check(Social.TownNear(0, 0, 41, 0) && !Social.TownNear(0, 0, 61, 0), "claims within 60 m can join the same town");
Check(Social.StripControl("a\nb\u001bc") == "a b c" && Social.CleanClaimName("Wil\nlow\u0000 Hollow") == "Wil low Hollow", "control characters read as spaces in chat and names");
Check(Social.ChatDim(2f) == 1f && MathF.Abs(Social.ChatDim(Social.ChatRange) - 0.55f) < 0.01f && Social.ChatDim(16f) < 1f, "far chat lines are dimmer");

// claims-12: a fence near the border snaps onto it, both ends on the circle.
{
    bool ok = true;
    foreach (var (px, pz) in new[] { (19.4f, 0.6f), (-3f, 20.8f), (13f, -14.5f) })
    {
        var (fx, fz, yaw) = Social.SnapToClaimBorder(100f, 50f, 100f + px, 50f + pz);
        var (e1, e2) = Structures.FenceEnds(fx, fz, yaw);
        foreach (var (ex, ez) in new[] { e1, e2 })
            ok &= MathF.Abs(MathF.Sqrt((ex - 100f) * (ex - 100f) + (ez - 50f) * (ez - 50f)) - Social.ClaimRadius) < 0.05f;
    }
    Check(ok && Social.NearClaimBorder(0, 0, 19f, 0) && !Social.NearClaimBorder(0, 0, 10f, 0), "a fence near the border snaps onto it");
}

// claims-10: whisper and shout.
Check(Social.VoiceOf("/w hi") == Social.Voice.Whisper && Social.VoiceBody("/W  hi ") == "hi" && Social.VoiceOf("/s hey") == Social.Voice.Shout
    && Social.VoiceOf("/sky") == Social.Voice.Say && Social.RangeOf(Social.Voice.Shout) == Social.ShoutRange, "/w whispers and /s shouts");
Walk(b, a.X + 2f, a.Z);
Thread.Sleep(1500);
a.Conn.Reducers.SendChat("/W  psst ");
Check(Wait(() => b.Conn.Db.MyChat.Iter().Any(m => m.Text == "/w psst"), 5), "a whisper is stored as \"/w psst\"");
int voiceCount = b.Conn.Db.MyChat.Iter().Count();
Thread.Sleep(500);
a.Conn.Reducers.SendChat("/s");
Wait(() => false, 1);
Check(b.Conn.Db.MyChat.Iter().Count() == voiceCount, "an empty /s says nothing");
Thread.Sleep(500);

// claims-13 (D-182): the server decides who hears. Out of earshot, B receives nothing at all.
Walk(b, a.X + Social.WhisperRange + 6f, a.Z);
Thread.Sleep(1500);
a.Conn.Reducers.SendChat("/w secret");
Check(Wait(() => a.Conn.Db.MyChat.Iter().Any(m => m.Text == "/w secret"), 5), "A hears their own whisper");
Check(!Wait(() => b.Conn.Db.MyChat.Iter().Any(m => m.Text == "/w secret"), 1.5), "a whisper never reaches someone out of earshot");

// Outsiders cannot dig or take items inside the claim.
ExpectError(b, () => b.Conn.Reducers.Dig((int)MathF.Round(b.X), (int)MathF.Round(b.Z)), "claimed", "B cannot dig inside A's claim");
a.Conn.Reducers.Drop((ushort)Item.Stone, a.Quality(Item.Stone));
Pump(() => a.Count(Item.Stone) == 0, 5, "drop");
Pump(() => b.Conn.Db.WorldItem.Iter().Any(i => Social.InClaim(claim.X, claim.Z, i.X, i.Z) && i.Item == (ushort)Item.Stone), 5, "dropped stone visible");
var stone = b.Conn.Db.WorldItem.Iter().First(i => Social.InClaim(claim.X, claim.Z, i.X, i.Z) && i.Item == (ushort)Item.Stone);
Walk(b, stone.X, stone.Z);
ExpectError(b, () => b.Conn.Reducers.PickUp(stone.Id), "claimed", "B cannot pick up inside A's claim");
ExpectError(b, () => b.Conn.Reducers.TendClaim(claim.Id), "Only the owner", "B cannot tend A's marker");

// The owner can still dig at home, and working the land tends the claim (claims-5).
var beforeWork = a.MyClaim()!;
long workEvery = Math.Min(Social.WorkTendEverySeconds, (beforeWork.ExpiresAt.MicrosecondsSinceUnixEpoch - beforeWork.TendedAt.MicrosecondsSinceUnixEpoch) / 4_000_000) * 1_000_000;
var workAfter = DateTime.UtcNow.AddMilliseconds((beforeWork.TendedAt.MicrosecondsSinceUnixEpoch + workEvery) / 1000 - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 500);
Pump(() => DateTime.UtcNow > workAfter, 90, "wait to tend by work");
a.LastError = null;
int digX = (int)MathF.Round(a.X) + 1, digZ = (int)MathF.Round(a.Z);
short depth = a.Depth(digX, digZ);
a.Conn.Reducers.Dig(digX, digZ);
Pump(() => a.Depth(digX, digZ) > depth || a.LastError != null, 5, "owner dig");
Check(a.LastError == null, "A can dig in their own claim");
Pump(() => a.MyClaim()!.TendedAt.MicrosecondsSinceUnixEpoch > beforeWork.TendedAt.MicrosecondsSinceUnixEpoch, 5, "tended by work");
Check(a.MyClaim()!.ExpiresAt.MicrosecondsSinceUnixEpoch > beforeWork.ExpiresAt.MicrosecondsSinceUnixEpoch, "digging in your own claim tends it");
long day = 86_400_000_000L, t0 = 1_000L * day;
Check(Social.TendedLine(t0 - 2 * day - 5_000_000, t0 + day + 7_000_000, t0) == "Tended 2 days ago · stands 1 more day", $"the marker line reads well ({Social.TendedLine(t0 - 2 * day - 5_000_000, t0 + day + 7_000_000, t0)})");

// claims-4: the owner adds B by name (any case), and a calm line says so.
string bName = $"Tobin{Random.Shared.Next(10000, 99999)}";
b.Conn.Reducers.SetName(bName);
Pump(() => a.Conn.Db.Player.PlayerId.Find(b.PlayerId)?.Name == bName, 5, "rename");
ExpectError(a, () => a.Conn.Reducers.AddClaimMemberByName("Nobody" + bName), "Nobody called", "adding an unknown name is refused");
ulong chatBeforeAdd = b.Conn.Db.MyChat.Iter().Select(m => m.Id).DefaultIfEmpty(0UL).Max();
a.LastError = null;
a.Conn.Reducers.AddClaimMemberByName(" " + bName.ToUpperInvariant() + " ");
Pump(() => b.Conn.Db.ClaimMember.Iter().Any(m => m.PlayerId == b.PlayerId) || a.LastError != null, 5, "membership");
Check(b.Conn.Db.ClaimMember.Iter().Any(m => m.PlayerId == b.PlayerId), $"the owner can add someone by name ({a.LastError ?? "ok"})");

// claims-9: the owner names the place; members see the name; an empty name clears it.
ExpectError(b, () => b.Conn.Reducers.NameClaim("Not Yours"), "do not hold a claim", "only the owner can name the land");
a.Conn.Reducers.NameClaim("  Willow   Hollow  ");
Pump(() => b.Conn.Db.ClaimName.ClaimId.Find(claim.Id) != null || a.LastError != null, 5, "name");
Check(b.Conn.Db.ClaimName.ClaimId.Find(claim.Id)?.Name == "Willow Hollow", $"the owner names the land, tidied ({b.Conn.Db.ClaimName.ClaimId.Find(claim.Id)?.Name ?? a.LastError ?? "-"})");
Check(Social.CleanClaimName(new string('x', 40)).Length == Social.ClaimNameMax, "long names are cut short");
a.Conn.Reducers.NameClaim(" ");
Pump(() => b.Conn.Db.ClaimName.ClaimId.Find(claim.Id) == null, 5, "unname");
Check(true, "an empty name clears it");
a.Conn.Reducers.NameClaim("Willow Hollow");
Pump(() => b.Conn.Db.ClaimName.ClaimId.Find(claim.Id) != null, 5, "rename");
Pump(() => b.Conn.Db.MyChat.Iter().Any(m => m.Id > chatBeforeAdd && m.SenderId == 0 && m.Text.StartsWith(bName + " now shares")), 5, "added line");
Check(true, "a chat line says who now shares the land");
ExpectError(a, () => a.Conn.Reducers.AddClaimMemberByName(bName), "already a member", "adding a member twice is refused");

// Once added, B can dig, pick up and use the stash.
b.LastError = null;
b.Conn.Reducers.PickUp(stone.Id);
Pump(() => b.Count(Item.Stone) == 1 || b.LastError != null, 5, "member pick up");
Check(b.Count(Item.Stone) == 1, $"member B can pick up inside the claim ({b.LastError ?? "ok"})");
Walk(b, claim.X + 1, claim.Z);
b.LastError = null;
b.Conn.Reducers.StashItem(claim.Id, (ushort)Item.Stone, stone.Quality, 1);
Pump(() => a.Conn.Db.MyClaimStash.Iter().Any(s => s.Item == (ushort)Item.Stone) || b.LastError != null, 5, "stash");
Check(a.Conn.Db.MyClaimStash.Iter().Any(s => s.Item == (ushort)Item.Stone), $"B's stone shows in the owner's stash ({b.LastError ?? "ok"})");
ExpectError(b, () => b.Conn.Reducers.TakeFromStash(claim.Id, (ushort)Item.Stone, stone.Quality, 999), "have that many", "you can't take more than the stash holds");
ExpectError(b, () => b.Conn.Reducers.TakeFromStash(claim.Id, (ushort)Item.Stone, stone.Quality, 0), "Nothing", "a stash move needs a count");
b.LastError = null;
b.Conn.Reducers.TakeFromStash(claim.Id, (ushort)Item.Stone, stone.Quality, 1);
Pump(() => b.Count(Item.Stone) == 1 || b.LastError != null, 5, "take");
Check(b.Count(Item.Stone) == 1, "B can take from the stash");
ExpectError(b, () => b.Conn.Reducers.StashItem(claim.Id, (ushort)Item.Stone, stone.Quality, 999), "that many", "you can't stash more than you carry");
Check(Wait(() => a.Conn.Db.MyClaimStashLog.Iter().Any(e => e.PlayerId == b.PlayerId && e.Count > 0)
    && a.Conn.Db.MyClaimStashLog.Iter().Any(e => e.PlayerId == b.PlayerId && e.Count < 0), 5), "the owner sees who left and took the stone");
Check(Social.StashLine("Tam", Items.Def(Item.Stick), -2) == "Tam took 2 sticks", "a stash line reads well");

// claims-7: trading (D-069). B gives A the stone through a trade, then A trades it back.
Check(Social.GiftPhrase(Items.Def(Item.Stone), 1) == "a stone" && Social.GiftPhrase(Items.Def(Item.Clay), 1) == "some clay"
    && Social.GiftPhrase(Items.Def(Item.Stick), 3) == "3 sticks", "a gift reads well (a stone, some clay, 3 sticks)");
Trade? TradeFor(Tester t) => t.Conn.Db.MyTrade.Iter().FirstOrDefault();
Walk(a, b.X + 6, b.Z);
ExpectError(b, () => b.Conn.Reducers.AskTrade(a.PlayerId), "Step closer", "you can't trade across a distance");
Walk(a, b.X + 1.5f, b.Z);
b.LastError = null;
b.Conn.Reducers.AskTrade(a.PlayerId);
Pump(() => TradeFor(a) != null || b.LastError != null, 5, "ask");
Check(TradeFor(a)?.State == Social.TradeAsked, $"A is asked to trade ({b.LastError ?? "ok"})");
ExpectError(b, () => b.Conn.Reducers.AskTrade(a.PlayerId), "already trading", "one trade at a time");
a.Conn.Reducers.AnswerTrade(TradeFor(a)!.Id, false);
Pump(() => TradeFor(b) == null, 5, "decline");
Check(Wait(() => b.LastTradeEnd == "declined", 2), $"declining ends it ({b.LastTradeEnd ?? "-"})");

b.Conn.Reducers.AskTrade(a.PlayerId);
Pump(() => TradeFor(a) != null, 5, "ask again");
ulong tradeId = TradeFor(a)!.Id;
a.Conn.Reducers.AnswerTrade(tradeId, true);
Pump(() => TradeFor(b)?.State == Social.TradeOpen, 5, "trade open");
ExpectError(a, () => a.Conn.Reducers.AcceptTrade(tradeId, 0, true), "Nothing to trade", "an empty trade can't be accepted");
ExpectError(b, () => b.Conn.Reducers.SetTradeItem(tradeId, (ushort)Item.Stone, stone.Quality, 2), "not carrying that many", "you can only offer what you carry");
b.LastError = null;
b.Conn.Reducers.SetTradeItem(tradeId, (ushort)Item.Stone, stone.Quality, 1);
Pump(() => a.Conn.Db.MyTradeItems.Iter().Any() && TradeFor(a)!.Version == 1, 5, "offer shows");
ExpectError(a, () => a.Conn.Reducers.AcceptTrade(tradeId, 0, true), "changed", "accepting an offer that changed is refused");
uint aStones = a.Count(Item.Stone);
a.Conn.Reducers.AcceptTrade(tradeId, 1, true);
Pump(() => TradeFor(b)?.BAccepted == true, 5, "A accepts");
a.Conn.Reducers.AcceptTrade(tradeId, 1, false);
Pump(() => TradeFor(b)?.BAccepted == false, 5, "A takes it back");
Check(TradeFor(b)?.Version == 1, "taking back an Accept leaves the offer as it was");
a.Conn.Reducers.AcceptTrade(tradeId, 1, true);
Pump(() => TradeFor(b)?.BAccepted == true, 5, "A accepts again");
Check(b.Count(Item.Stone) == 1, "nothing moves until both accept");
b.LastError = null;
b.LastTradeEnd = null;
b.Conn.Reducers.AcceptTrade(tradeId, 1, true);
Pump(() => a.Count(Item.Stone) > aStones || b.LastError != null, 5, "swap");
Check(a.Count(Item.Stone) == aStones + 1 && b.Count(Item.Stone) == 0 && TradeFor(a) == null, $"both accepting swaps the items ({b.LastError ?? "ok"})");
Check(Wait(() => b.LastTradeEnd == "done", 2) && !Wait(() => b.Conn.Db.MyChat.Iter().Any(m => m.SenderId == 0 && (m.Text.Contains(" trade") || m.Text.Contains(" hands "))), 1),
    "both traders hear it is done, and bystanders hear nothing (D-080)");

a.Conn.Reducers.AskTrade(b.PlayerId);
Pump(() => TradeFor(b) != null, 5, "ask back");
tradeId = TradeFor(b)!.Id;
b.Conn.Reducers.AnswerTrade(tradeId, true);
Pump(() => TradeFor(a)?.State == Social.TradeOpen, 5, "open back");
a.Conn.Reducers.SetTradeItem(tradeId, (ushort)Item.Stone, stone.Quality, 1);
Pump(() => TradeFor(b)?.Version == 1, 5, "offer back");
b.Conn.Reducers.AcceptTrade(tradeId, 1, true);
Pump(() => TradeFor(a)?.BAccepted == true, 5, "B accepts");
a.Conn.Reducers.SetTradeItem(tradeId, (ushort)Item.Stone, stone.Quality, 0);
a.Conn.Reducers.SetTradeItem(tradeId, (ushort)Item.Stone, stone.Quality, 1);
Pump(() => TradeFor(b)?.Version == 3, 5, "offer changed");
Check(TradeFor(b) is { AAccepted: false, BAccepted: false }, "changing an offer clears both accepts");
a.LastError = b.LastError = null;
a.Conn.Reducers.AcceptTrade(tradeId, 3, true);
b.Conn.Reducers.AcceptTrade(tradeId, 3, true);
Pump(() => b.Count(Item.Stone) == 1 || a.LastError != null || b.LastError != null, 5, "trade back");
Check(b.Count(Item.Stone) == 1, $"the stone goes back ({a.LastError ?? b.LastError ?? "ok"})");

b.Conn.Reducers.AskTrade(a.PlayerId);
Pump(() => TradeFor(a) != null, 5, "ask to walk away");
a.Conn.Reducers.AnswerTrade(TradeFor(a)!.Id, true);
Pump(() => TradeFor(b)?.State == Social.TradeOpen, 5, "open to walk away");
a.LastTradeEnd = null;
Walk(a, b.X + Social.TradeBreakDistance + 2, b.Z);
Check(Wait(() => a.LastTradeEnd == "apart" && TradeFor(a) == null, 5), $"walking apart ends a trade ({a.LastTradeEnd ?? "-"})");
b.Conn.Reducers.StashItem(claim.Id, (ushort)Item.Stone, stone.Quality, 1);
Pump(() => b.Count(Item.Stone) == 0, 5, "stash again");

// A member may lay out a building on the claim.
// The land is hilly now, so the first spot may be too steep or wet: try spots round the claim's
// middle until the server accepts one. Any other refusal (rights, reach) still fails the check.
float hx = claim.X + 6, hz = claim.Z + 5;
for (int ring = 0, tries = 0; ring < 3 && tries < 20; ring++)
    for (int k = 0; k < 8; k++, tries++)
    {
        float r = 6f + 4f * ring, ang = MathF.Tau * k / 8f + 0.6f;
        hx = claim.X + MathF.Cos(ang) * r;
        hz = claim.Z + MathF.Sin(ang) * r;
        Walk(b, hx - 2, hz);
        b.LastError = null;
        b.Conn.Reducers.PlaceStructure(StructureKind.LeanTo, hx, hz, 0);
        Pump(() => b.Conn.Db.Structure.Iter().Any(s => s.Kind == StructureKind.LeanTo && s.Owner == b.Conn.Identity) || b.LastError != null, 5, "member lays out");
        if (b.LastError == null || !(b.LastError.Contains("steep") || b.LastError.Contains("wet"))) { ring = 99; break; }
    }
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
ExpectError(b, () => b.Conn.Reducers.TakeFromStash(claim.Id, (ushort)Item.Stone, stone.Quality, 1), "not yours", "removed member cannot open the stash");

// The land's owner can clear anything on it; nobody else can take down the owner's things.
Walk(b, site.X + 1.5f, site.Z);
ExpectError(b, () => b.Conn.Reducers.Dismantle(site.Id), "take it down", "B cannot take down A's marker");
Walk(a, hx - 2, hz);
a.LastError = null;
a.Conn.Reducers.Dismantle(hut!.Id);
Pump(() => !a.Conn.Db.Structure.Iter().Any(s => s.Id == hut.Id) || a.LastError != null, 5, "owner clears");
Check(!a.Conn.Db.Structure.Iter().Any(s => s.Id == hut.Id), $"the owner can take down a member's building ({a.LastError ?? "ok"})");

// --- Fighting (claims-3). The grace was set to 0 at the top.
float Hp(Tester t, uint id) => t.Conn.Db.PlayerHealth.PlayerId.Find(id)?.Health ?? -1f;
// Not on claimed land with fighting off. Tend first: CI gives claims only 30 s.
Walk(a, claim.X + 1.5f, claim.Z);
long tendedBefore = a.MyClaim()?.ExpiresAt.MicrosecondsSinceUnixEpoch ?? 0;
a.Conn.Reducers.TendClaim(claim.Id);
Pump(() => (a.MyClaim()?.ExpiresAt.MicrosecondsSinceUnixEpoch ?? 0) > tendedBefore, 5, "tend before fighting");
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
// Moves reach the server in batches: wait until it agrees B is within reach of A.
bool ServerInReach() =>
    b.Conn.Db.PlayerPos.PlayerId.Find(b.PlayerId) is { } pb && b.Conn.Db.PlayerPos.PlayerId.Find(a.PlayerId) is { } pa
    && new Vector(pb.X - pa.X, pb.Z - pa.Z).Length <= Social.StrikeReach;
Wait(ServerInReach, 5);
float full = Hp(b, a.PlayerId);
b.LastError = null;
b.Conn.Reducers.StrikePlayer(a.PlayerId);
Pump(() => Hp(b, a.PlayerId) < full || b.LastError != null, 5, "strike lands");
Check(Hp(b, a.PlayerId) < full, $"a strike in the wild lands ({full:F1} -> {Hp(b, a.PlayerId):F1}, {b.LastError ?? "ok"})");
ExpectError(b, () => b.Conn.Reducers.StrikePlayer(a.PlayerId), "Too soon", "strikes have a short cooldown");
// Strike until A falls: A's things drop and A wakes elsewhere.
uint carried = (uint)a.Conn.Db.MyInventory.Iter().Sum(i => i.Count);
ulong chatBefore = b.Conn.Db.MyChat.Iter().Select(m => m.Id).DefaultIfEmpty(0UL).Max();
bool StruckDown() => b.Conn.Db.MyChat.Iter().Any(m => m.Id > chatBefore && m.SenderId == 0 && m.Text.Contains("struck down"));
for (int i = 0; i < 40 && !StruckDown(); i++)
{
    var until = DateTime.UtcNow.AddSeconds(0.85);
    Pump(() => DateTime.UtcNow > until, 2, "cooldown");
    b.LastError = null;
    b.Conn.Reducers.StrikePlayer(a.PlayerId);
    Pump(() => true, 1, "tick");
}
Pump(StruckDown, 5, "struck down");
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
    // CI gives the claim 30 s, which the fight above can already use up.
    var expires = a.MyClaim()?.ExpiresAt.MicrosecondsSinceUnixEpoch;
    Console.WriteLine(expires is { } e
        ? $"Waiting for the marker to fall ({(e - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000) / 1_000_000}s left plus up to 20s for the decay tick)..."
        : "The marker already fell");
    Pump(() => a.MyClaim() == null, 120, "decay");
    Check(true, "untended claim fell");
    Pump(() => !a.Conn.Db.Structure.Iter().Any(s => s.Id == site.Id), 5, "marker gone");
    Check(true, "the marker post went with it");
    Check(Wait(() => a.Conn.Db.ClaimName.ClaimId.Find(claim.Id) == null, 5), "its name went with it");
    Check(Wait(() => !a.Conn.Db.MyClaimStashLog.Iter().Any(e => e.ClaimId == claim.Id), 5), "and its stash log");
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

// --- Towns (claims-19). Run last, with two fresh claims, because a claim lasts only 30 s here:
// B raises a claim, A raises one 41 m away, A starts a roster, B signs it, and A gets builder rights
// on B's land (but not the stash, or tending) until B leaves the roster.
{
    float H(float px, float pz) => Structures.GroundAt(px, pz, (i, j) => b.Conn.Db.TerrainEdit.CellKey.Find(World.CellKey(i, j))?.Depth ?? 0);
    bool GoodMarkerSpot(float x, float z) => ClearSpot(x, z)
        && Structures.GroundProblem((int)StructureKind.ClaimMarker, x, z, 0f, H) == null
        && !Trees.GrowsAt(World.CellOf(x), World.CellOf(z)) && !Rocks.LiesAt(World.CellOf(x), World.CellOf(z));
    (float x, float z, float x2, float z2)? pair = null;
    for (int ring = 0; ring <= 24 && pair == null; ring++)
        for (int step = 0; step < Math.Max(1, ring * 6) && pair == null; step++)
        {
            float angle = step * MathF.Tau / Math.Max(1, ring * 6), r = ring * 4f;
            float x1 = 34 + r * MathF.Cos(angle), z1 = 30 + r * MathF.Sin(angle);
            if (!GoodMarkerSpot(x1, z1)) continue;
            for (int d = 0; d < 8 && pair == null; d++)
            {
                float da = MathF.Tau * d / 8f, x2 = x1 + 41f * MathF.Cos(da), z2 = z1 + 41f * MathF.Sin(da);
                if (GoodMarkerSpot(x2, z2)) pair = (x1, z1, x2, z2);
            }
        }
    Check(pair != null, "there is room for two neighbouring claims for the town check");
    if (pair is { } sites)
    {
        void Raise(Tester t, float x, float z)
        {
            Walk(t, x - 1.5f, z);
            t.LastError = null;
            t.Conn.Reducers.PlaceStructure(StructureKind.ClaimMarker, x, z, 0);
            Pump(() => t.Conn.Db.Structure.Iter().Any(st => st.Kind == StructureKind.ClaimMarker && st.Owner == t.Conn.Identity && !st.Complete) || t.LastError != null, 5, $"{t.Name} lays out a marker");
            if (t.LastError != null) Console.WriteLine($"  {t.Name} could not lay out the town-check marker: {t.LastError}");
            var mine = t.Conn.Db.Structure.Iter().FirstOrDefault(st => st.Kind == StructureKind.ClaimMarker && st.Owner == t.Conn.Identity && !st.Complete);
            for (int i = 0; i < 20 && t.MyClaim() == null && mine.Id != 0; i++)
            {
                t.Conn.Reducers.Build(mine.Id);
                Wait(() => t.MyClaim() != null, 0.7);
                t.LastError = null;
            }
        }
        // The earlier checks gave materials away and used some up: top both up first.
        foreach (var gatherer in new[] { a, b })
            foreach (var (kind, count) in new[] { (Item.Stick, (int)Social.MarkerSticks), (Item.Fiber, (int)Social.MarkerFiber) })
                while (gatherer.Count(kind) < count)
                {
                    var item = gatherer.Conn.Db.WorldItem.Iter().Where(i => i.Item == (ushort)kind)
                        .OrderBy(i => (i.X - gatherer.X) * (i.X - gatherer.X) + (i.Z - gatherer.Z) * (i.Z - gatherer.Z)).First();
                    Walk(gatherer, item.X, item.Z);
                    uint before = gatherer.Count(kind);
                    gatherer.Conn.Reducers.PickUp(item.Id);
                    Pump(() => gatherer.Count(kind) > before || gatherer.LastError != null, 5, "pick up");
                    gatherer.LastError = null;
                }
        Raise(b, sites.x2, sites.z2);
        Raise(a, sites.x, sites.z);
        var aClaim = a.MyClaim();
        var bClaim = b.MyClaim();
        Check(aClaim != null && bClaim != null, $"A and B each hold a claim for the town check (A {(aClaim != null ? "ok" : a.LastError ?? "none")}, B {(bClaim != null ? "ok" : b.LastError ?? "none")})");
        if (aClaim != null && bClaim != null)
        {
            // B stays at their marker and tends it, so both claims are fresh for the next 30 s.
            b.Conn.Reducers.TendClaim(bClaim.Id);

            // Two one-claim towns merge by a majority of each: A's "Alder" and B's "Birch".
            a.Conn.Reducers.StartTown("Alder");
            b.Conn.Reducers.StartTown("Birch");
            Check(Wait(() => a.Conn.Db.Town.Iter().Any(t => t.Name == "Alder") && a.Conn.Db.Town.Iter().Any(t => t.Name == "Birch"), 5), "A and B each start a town");
            var alderId = a.Conn.Db.Town.Iter().First(t => t.Name == "Alder").Id;
            var birchId = a.Conn.Db.Town.Iter().First(t => t.Name == "Birch").Id;
            a.Conn.Reducers.ProposeTownMerge(birchId);
            Check(Wait(() => a.Conn.Db.TownMerge.Iter().Any(m => m.FromTownId == alderId && m.ToTownId == birchId), 5), "A, the Mayor, proposes joining Birch");
            var mergeId = a.Conn.Db.TownMerge.Iter().First(m => m.FromTownId == alderId).Id;
            a.Conn.Reducers.VoteTownMerge(mergeId, true);
            Wait(() => a.Conn.Db.TownMergeVote.Iter().Any(v => v.MergeId == mergeId), 5);
            Check(a.Conn.Db.Town.Iter().Count(t => t.Name == "Alder" || t.Name == "Birch") == 2, "one town's yes is not enough");
            b.Conn.Reducers.VoteTownMerge(mergeId, true);
            // The older town keeps its name (which started first is a race between the two calls).
            Check(Wait(() => a.Conn.Db.Town.Iter().Count(t => t.Name == "Alder" || t.Name == "Birch") == 1 && a.Conn.Db.TownSigner.Iter().Count(t => t.TownId == alderId || t.TownId == birchId) == 2, 5)
                && a.Conn.Db.TownSigner.Iter().Select(t => t.TownId).Distinct().Count() == 1, "a majority of both towns merges them into one");
            var townId = a.Conn.Db.TownSigner.Iter().First(t => t.PlayerId == a.PlayerId).TownId;
            Check(a.Conn.Db.TownOffice.Iter().FirstOrDefault(o => o.TownId == townId && o.Office == Social.OfficeMayor)?.HolderId == 0, "the merged town starts with an empty Mayor's seat");
            // B steps out again, A elects themselves Mayor (one vote of one is a majority) and renames the town.
            b.Conn.Reducers.LeaveTown();
            Check(Wait(() => a.Conn.Db.TownSigner.Iter().Count(t => t.TownId == townId) == 1, 5), "B leaves the merged town");
            a.Conn.Reducers.VoteTownOffice(Social.OfficeMayor, a.PlayerId);
            Check(Wait(() => a.Conn.Db.TownOffice.Iter().Any(o => o.TownId == townId && o.Office == Social.OfficeMayor && o.HolderId == a.PlayerId), 5), "a lone signer's vote makes them Mayor");
            a.Conn.Reducers.RenameTown("Willow Run");
            Check(Wait(() => a.Conn.Db.Town.Iter().Any(t => t.Name == "Willow Run"), 5), "the Mayor renames the town");
            ExpectError(b, () => b.Conn.Reducers.SignTown(townId), "invited", "B cannot sign without an invitation");
            a.Conn.Reducers.InviteToTown(b.PlayerId);
            Check(Wait(() => b.Conn.Db.MyTownInvites.Iter().Any(i => i.TownName == "Willow Run"), 5) && !a.Conn.Db.MyTownInvites.Iter().Any(), "B sees the invitation, and A does not");
            // Before signing, A has no rights on B's land.
            Walk(a, bClaim.X + 4f, bClaim.Z + 4f);
            float tx = bClaim.X + 7f, tz = bClaim.Z, tx2 = bClaim.X, tz2 = bClaim.Z + 7f;
            ExpectError(a, () => a.Conn.Reducers.PlaceStructure(StructureKind.LeanTo, tx, tz, 0), "claimed", "before B signs, A cannot build on B's land");
            b.Conn.Reducers.SignTown(townId);
            Check(Wait(() => a.Conn.Db.TownSigner.Iter().Count(t => t.TownId == townId) == 2, 5), "B signs, and the roster holds both claims");

            // Builder rights now; the stash and the marker's upkeep stay B's.
            a.LastError = null;
            a.Conn.Reducers.PlaceStructure(StructureKind.LeanTo, tx, tz, 0);
            Wait(() => a.LastError != null || a.Conn.Db.Structure.Iter().Any(st => st.Kind == StructureKind.LeanTo && st.Owner == a.Conn.Identity && Math.Abs(st.X - tx) < 0.1f), 5);
            Check(a.LastError?.Contains("claimed") != true, $"a signer can lay out a building on another signed claim ({a.LastError ?? "ok"})");
            ExpectError(a, () => a.Conn.Reducers.TakeFromStash(bClaim.Id, (ushort)Item.Stone, 50, 1), "not yours", "a signer cannot open another claim's stash");
            ExpectError(a, () => a.Conn.Reducers.TendClaim(bClaim.Id), "owner and members", "a signer cannot tend another claim's marker");
            ExpectError(a, () => a.Conn.Reducers.RemoveFromTown(b.PlayerId + 1000), "not on this roster", "the founder cannot remove someone who is not on the roster");

            ExpectError(b, () => b.Conn.Reducers.RenameTown("Nope"), "Mayor", "only the Mayor renames the town");
            a.LastError = null;
            a.Conn.Reducers.AnnounceToTown("Market day on the green");
            Wait(() => a.LastError != null, 1);
            Check(a.LastError == null, $"the Mayor can speak for the town ({a.LastError ?? "ok"})");
            ExpectError(a, () => a.Conn.Reducers.AnnounceToTown("And again"), "moment", "the town's word is rate limited");
            ExpectError(b, () => b.Conn.Reducers.AnnounceToTown("Hello"), "Herald", "only the Herald or Mayor speaks for the town");
            // A second office, as data: both signers vote A in as Deputy.
            a.Conn.Reducers.VoteTownOffice(1, a.PlayerId);
            b.Conn.Reducers.VoteTownOffice(1, a.PlayerId);
            Check(Wait(() => a.Conn.Db.TownOffice.Iter().Any(o => o.TownId == townId && o.Office == 1 && o.HolderId == a.PlayerId), 5), "the signers elect a Deputy");
            ExpectError(a, () => a.Conn.Reducers.VoteTownOffice(77, a.PlayerId), "no such office", "an unknown office cannot be voted on");
            // The Mayor: A, the founder, starts as Mayor. Only the Mayor removes; the signers vote.
            var mayorNow = a.Conn.Db.TownOffice.Iter().FirstOrDefault(o => o.TownId == townId && o.Office == Social.OfficeMayor);
            Check(mayorNow.HolderId == a.PlayerId, "A is Mayor of the signed town");
            ExpectError(b, () => b.Conn.Reducers.RemoveFromTown(a.PlayerId), "Mayor", "a signer who is not Mayor cannot remove anyone");
            b.Conn.Reducers.VoteTownOffice(Social.OfficeMayor, b.PlayerId);
            Check(Wait(() => b.Conn.Db.TownVote.Iter().Any(v => v.TownId == townId && v.VoterId == b.PlayerId), 5), "B votes for B");
            Check(a.Conn.Db.TownOffice.Iter().First(o => o.TownId == townId && o.Office == Social.OfficeMayor).HolderId == a.PlayerId, "one vote of two is not a majority, so A stays Mayor");
            a.Conn.Reducers.VoteTownOffice(Social.OfficeMayor, b.PlayerId);
            Check(Wait(() => a.Conn.Db.TownOffice.Iter().Any(o => o.TownId == townId && o.Office == Social.OfficeMayor && o.HolderId == b.PlayerId), 5), "two votes of two make B the Mayor");
            Check(!a.Conn.Db.TownVote.Iter().Any(v => v.TownId == townId), "the ballot clears once the seat is filled");
            ExpectError(a, () => a.Conn.Reducers.RemoveFromTown(b.PlayerId), "Mayor", "A is no longer Mayor and cannot remove B");
            ExpectError(b, () => b.Conn.Reducers.VoteTownOffice(Social.OfficeMayor, 999999), "not on this roster", "a vote must name a signer");

            // The Mayor removes a signer; the rights go with them.
            b.Conn.Reducers.RemoveFromTown(a.PlayerId);
            Check(Wait(() => a.Conn.Db.TownSigner.Iter().Count(t => t.TownId == townId) == 1, 5), "the Mayor removes A from the roster");
            ExpectError(a, () => a.Conn.Reducers.PlaceStructure(StructureKind.LeanTo, tx2, tz2, 0), "claimed", "after being removed, A cannot build on B's land");
            // The last signer leaving closes the town.
            b.Conn.Reducers.LeaveTown();
            Check(Wait(() => !a.Conn.Db.Town.Iter().Any(t => t.Id == townId) && !a.Conn.Db.TownOffice.Iter().Any(o => o.TownId == townId), 5), "the town and its offices close when the last claim leaves");
        }
    }
}

Console.WriteLine(failures == 0 ? "All social checks passed" : $"{failures} checks failed");
// The SDK keeps background threads alive, so leave explicitly.
Environment.Exit(failures == 0 ? 0 : 1);

sealed record Site(ulong Id, float X, float Z);

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
    public string? LastTradeEnd;
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
                        "SELECT * FROM claim_member", "SELECT * FROM town", "SELECT * FROM town_signer", "SELECT * FROM town_office", "SELECT * FROM town_vote", "SELECT * FROM town_merge", "SELECT * FROM town_merge_vote", "SELECT * FROM my_town_invites", "SELECT * FROM my_claim_stash", "SELECT * FROM my_chat", "SELECT * FROM structure", "SELECT * FROM player_health", "SELECT * FROM claim_name", "SELECT * FROM my_claim_stash_log", "SELECT * FROM my_trade", "SELECT * FROM my_trade_items", "SELECT * FROM trade_ended",
                    });
            })
            .OnConnectError(e => Console.WriteLine($"{name} connect error: {e.Message}"))
            .Build();
        Conn.OnUnhandledReducerError += (_, e) => LastError = e.Message;
        Conn.Db.TradeEnded.OnInsert += (_, e) => LastTradeEnd = e.Reason;
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
