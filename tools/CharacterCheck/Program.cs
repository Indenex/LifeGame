// Character check (survival-24): several characters per login. One fresh login on a freshly
// published server: its first character is keyed by the login itself and starts in the world;
// a second and third can be made only from the character menu; each has its own
// inventory and admin status; names are unique; the fourth is refused; selecting only works
// from the menu; deleting needs the typed name and frees it. Items come from the admin give,
// so the tool makes the bot an admin through the CLI (`spacetimedb-cli` on PATH).
// Exits non-zero on failure.
//
// Usage: dotnet run -- [uri=http://127.0.0.1:3000] [db=lifegame] [server=local]
using System.Diagnostics;
using LifeGame;
using LifeGame.Shared;
using SpacetimeDB;
using SpacetimeDB.Types;

string uri = args.Length > 0 ? args[0] : "http://127.0.0.1:3000";
string db = args.Length > 1 ? args[1] : "lifegame";
string server = args.Length > 2 ? args[2] : "local";
var clock = Stopwatch.StartNew();
Identity? login = null;
bool subscribed = false;
string? lastError = null;

void Fail(string what)
{
    Console.WriteLine($"Character check FAILED: {what}");
    Environment.Exit(1);
}

var conn = DbConnection.Builder()
    .WithUri(uri)
    .WithDatabaseName(db)
    .OnConnect((c, identity, _) =>
    {
        login = identity;
        c.SubscriptionBuilder()
            .OnApplied(_ => subscribed = true)
            .Subscribe(new[]
            {
                "SELECT * FROM player",
                "SELECT * FROM my_player",
                "SELECT * FROM my_active_character",
                "SELECT * FROM my_characters",
                "SELECT * FROM my_character_looks",
                "SELECT * FROM my_inventory",
                "SELECT * FROM my_admin",
            });
    })
    .OnConnectError(e => Fail($"connect: {e.Message}"))
    .Build();
conn.OnUnhandledReducerError += (_, e) => lastError = e.Message;

void Pump(Func<bool> done, int seconds, string what)
{
    var until = clock.Elapsed + TimeSpan.FromSeconds(seconds);
    while (!done() && clock.Elapsed < until) { conn.FrameTick(); Thread.Sleep(5); }
    if (!done()) Fail($"timed out waiting for {what}");
}

void Settle(int ms)
{
    var until = clock.Elapsed + TimeSpan.FromMilliseconds(ms);
    while (clock.Elapsed < until) { conn.FrameTick(); Thread.Sleep(5); }
}

// Names are letters only (Names.Problem).
string RandName() => "Chk" + new string(Enumerable.Range(0, 8).Select(_ => (char)('a' + Random.Shared.Next(26))).ToArray());

void Ok(string what) => Console.WriteLine($"ok  {what}");

// Runs a reducer and returns the error it was refused with, or null when it went through.
string? Try(Action call)
{
    lastError = null;
    call();
    Settle(400);
    return lastError;
}

void MustWork(string what, Action call)
{
    if (Try(call) is { } err) Fail($"{what} was refused: {err}");
}

void MustRefuse(string what, string containing, Action call)
{
    var err = Try(call);
    if (err == null) Fail($"{what} went through");
    if (!err.Contains(containing, StringComparison.OrdinalIgnoreCase)) Fail($"{what} was refused with '{err}', expected something about '{containing}'");
}

uint Berries() => (uint)conn.Db.MyInventory.Iter().Where(r => r.Item == (ushort)Item.Berries).Sum(r => (long)r.Count);
bool InWorld() => Me.InWorld(conn);
Character[] Chars() => Me.Characters(conn);

Pump(() => subscribed, 20, "subscription");
Pump(() => Chars().Length == 1 && InWorld(), 10, "the first character, in the world");
var first = Chars()[0];
if (first.Key != login) Fail("a login's first character should be keyed by the login itself");
if (Me.Key(conn) != login) Fail("Me.Key should be the login before a second character exists");
Ok("a new login gets one character, keyed by the login, already in the world");

// --- Admin through the CLI, so the bot can be given berries ---
string name1 = RandName();
MustWork("SetName", () => conn.Reducers.SetName(name1));
var psi = new ProcessStartInfo("spacetimedb-cli", new[] { "call", db, "--server", server, "make_admin", $"\"{name1}\"" })
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
Pump(() => conn.Db.MyAdmin.Iter().Any(), 10, "the bot to become an admin");
MustWork("AdminGive", () => conn.Reducers.AdminGive((ushort)Item.Berries, 5, 60));
Pump(() => Berries() == 5, 5, "5 berries in the first character's bag");
Ok("the first character carries 5 berries and is an admin");

// --- Making and selecting only happen at the menu ---
string name2 = RandName();
MustRefuse("making a character in the world", "menu", () => conn.Reducers.CreateCharacter(name2, 0, 0, 0, 0));
MustRefuse("selecting a character in the world", "menu", () => conn.Reducers.SelectCharacter(first.Key));
Ok("making or selecting a character is refused while in the world");

MustWork("ReturnToMenu", () => conn.Reducers.ReturnToMenu());
Pump(() => !InWorld(), 5, "the character to leave the world");
MustRefuse("walking from the menu", "player", () => conn.Reducers.Move(1, 1, 0));
Ok("at the menu: out of the world and unable to act");

// --- A second character ---
MustRefuse("a taken name", "taken", () => conn.Reducers.CreateCharacter(name1, 0, 0, 0, 0));
MustRefuse("a bad name", "", () => conn.Reducers.CreateCharacter("x", 0, 0, 0, 0));
MustWork("CreateCharacter", () => conn.Reducers.CreateCharacter(name2, 0, 0, 0, 0));
Pump(() => Chars().Length == 2 && InWorld(), 10, "the second character, in the world");
var second = Chars().First(c => c.Key != first.Key);
if (Me.Key(conn) != second.Key) Fail("the new character should be the one being played");
if (second.Key == login) Fail("a second character must not be keyed by the login");
if (Me.Player(conn)?.Name != name2) Fail($"the new character is called {Me.Player(conn)?.Name}, expected {name2}");
Pump(() => Berries() == 0, 5, "the new character's bag to be empty");
if (conn.Db.MyAdmin.Iter().Any()) Fail("the new character should not be an admin");
Ok("the second character is its own: new key, own name, empty bag, not an admin");
if (Chars().First(c => c.Key == second.Key).Name != name2) Fail("the menu list should carry the new character's name");
if (Chars().First(c => c.Key == first.Key).Name != name1) Fail("the menu list should carry the renamed first character's name");
Pump(() => conn.Db.MyCharacterLooks.Iter().Any(l => l.PlayerId == second.PlayerId), 5, "the new character's look on the menu list");
Ok("the menu list carries every character's name and look");

MustWork("a move as the second character", () => conn.Reducers.Move(0, 0, 0));
Ok("the second character can act");

// --- Third and fourth ---
MustWork("ReturnToMenu", () => conn.Reducers.ReturnToMenu());
Pump(() => !InWorld(), 5, "the second character to leave the world");
string name3 = RandName();
MustWork("CreateCharacter (third)", () => conn.Reducers.CreateCharacter(name3, 0, 0, 0, 0));
Pump(() => Chars().Length == 3 && InWorld(), 10, "the third character");
MustWork("ReturnToMenu", () => conn.Reducers.ReturnToMenu());
Pump(() => !InWorld(), 5, "the third character to leave the world");
MustRefuse("a fourth character", $"{Characters.MaxPerLogin}", () => conn.Reducers.CreateCharacter("Fourth One", 0, 0, 0, 0));
Ok($"{Characters.MaxPerLogin} characters is the limit");

// --- Back to the first ---
MustWork("SelectCharacter", () => conn.Reducers.SelectCharacter(first.Key));
Pump(() => InWorld() && Me.Key(conn) == first.Key, 10, "the first character back in the world");
Pump(() => Berries() == 5, 5, "the first character's berries");
Pump(() => conn.Db.MyAdmin.Iter().Any(), 5, "the first character's admin status");
Ok("selecting the first character brings its bag and admin status back");

// --- Delete ---
MustWork("ReturnToMenu", () => conn.Reducers.ReturnToMenu());
Pump(() => !InWorld(), 5, "the first character to leave the world");
MustRefuse("a delete with the wrong name", "name", () => conn.Reducers.DeleteCharacter(second.Key, "Somebody Else"));
if (Chars().Length != 3) Fail("a refused delete removed a character");
MustWork("DeleteCharacter", () => conn.Reducers.DeleteCharacter(second.Key, name2));
Pump(() => Chars().Length == 2, 5, "the deleted character to leave the list");
MustRefuse("deleting another login's or a missing character", "not one of your", () => conn.Reducers.DeleteCharacter(second.Key, name2));
Ok("delete needs the typed name and removes the character");
MustWork("CreateCharacter with the freed name", () => conn.Reducers.CreateCharacter(name2, 0, 0, 0, 0));
Pump(() => Chars().Length == 3 && InWorld(), 10, "a character with the freed name");
Ok("a deleted character's name is free again");

// --- The first character can go too; the login is then at an empty menu ---
MustWork("ReturnToMenu", () => conn.Reducers.ReturnToMenu());
Pump(() => !InWorld(), 5, "to leave the world");
MustWork("DeleteCharacter (first)", () => conn.Reducers.DeleteCharacter(first.Key, name1));
Pump(() => Chars().Length == 2, 5, "the first character to be deleted");
var spare = Chars().First();
MustWork("SelectCharacter", () => conn.Reducers.SelectCharacter(spare.Key));
Pump(() => InWorld() && Me.Key(conn) == spare.Key, 10, "a remaining character");
Ok("a login can delete its first character and play another");

Console.WriteLine($"Character check passed in {clock.Elapsed.TotalSeconds:F0} s");
