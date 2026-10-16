// Render lane puppet: a second player that acts out a scene for the camera, and the
// cue for screenshots. tools/render/scene.sh runs it next to a real client.
//
// Usage: dotnet run -- <uri> <db> <steps file> <snap prefix>
// Steps, one per line ('#' comments):
//   name B            set this player's name
//   find              wait for the camera player (the newest other online player)
//   wait 2.5          wait seconds
//   near 3            walk to 3 m from the camera player
//   away 20           walk straight away from the camera player until 20 m off
//   goto x z          walk to a point
//   stroll dx dz      start walking dx, dz metres from here and go on to the next
//                     step at once (to snap mid-stride); "arrive" waits for it
//   say hello there   send a chat line ("/me waves" works too)
//   snap              save the screen to <snap prefix>-<n>.png (n counts from 1)
//   burst 6 0.8       snap 6 times, 0.8 s apart (the camera client lags a few
//                     seconds in software rendering; keep the best frame)
//   clip 6            record 6 s of the screen to <snap prefix>-<n>.mp4
//   give 193 8 60     the database owner puts 8 of item 193 at quality 60 in my hands
//   craft cord.spin_wool   start a recipe (as the server's own default score) and wait for it
//   drop 199          drop one of an item at my feet
//   key c             press a key in the camera client's window (xdotool)
//   keydown w / keyup w   hold and release a key
//   face 0            turn to that heading in degrees (0 = +z); fires are lit 1.2 m ahead
//   firepots          set every unfired pot I carry into the fire I lit
//   click 660 850     move there and left-click
//   live              take further steps from the file in PUPPET_LIVE (appended
//                     while running, one step per line) until a line "end"
//   mouse 660 850     move the mouse there in the camera client's window (tooltips)
//   frames 300 / frames +72   (movie scenes) wait until the camera client has
//                     written that many frames (or that many more)
//   mark walk-start   log the current movie frame; scene.sh cuts clips between
//                     "<name>-start" and "<name>-end" marks
// With PUPPET_TOKEN_OUT set, the puppet writes its login token there once it has
// joined; scene.sh hands it to the camera client for "client-as-puppet" scenes, so
// the camera shows the puppet's own inventory and work.
using System.Diagnostics;
using SpacetimeDB;
using SpacetimeDB.Types;

string uri = args[0], db = args[1], stepsFile = args[2], prefix = args[3];
var steps = File.ReadAllLines(stepsFile).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')).ToList();

Identity? me = null;
bool ready = false;
uint myId = 0;
float x = 0, z = 0;
string? lastError = null;
var conn = DbConnection.Builder()
    .WithUri(uri)
    .WithToken(Environment.GetEnvironmentVariable("PUPPET_TOKEN_IN") is { Length: > 0 } tokenIn && File.Exists(tokenIn) ? File.ReadAllText(tokenIn).Trim() : null)
    .WithDatabaseName(db)
    .OnConnect((c, identity, token) =>
    {
        me = identity;
        if (Environment.GetEnvironmentVariable("PUPPET_TOKEN_OUT") is { Length: > 0 } tokenOut)
            File.WriteAllText(tokenOut, token);
        c.SubscriptionBuilder()
            .OnApplied(ctx =>
            {
                myId = ctx.Db.Player.Identity.Find(identity)!.PlayerId;
                var pos = ctx.Db.PlayerPos.PlayerId.Find(myId)!;
                x = pos.X; z = pos.Z;
                ready = true;
            })
            .Subscribe(new[] { "SELECT * FROM player", "SELECT * FROM player_pos", "SELECT * FROM my_inventory", "SELECT * FROM my_craft_job", "SELECT * FROM fire" });
    })
    .OnConnectError(e => { Console.WriteLine($"puppet: connect error {e.Message}"); Environment.Exit(1); })
    .Build();
conn.OnUnhandledReducerError += (_, e) => { lastError = e.Message; Console.WriteLine($"puppet: server said {e.Message}"); };

var clock = Stopwatch.StartNew();
(float X, float Z)? stroll = null;
double lastStep = 0;
void Pump(double seconds, Func<bool>? until = null)
{
    var end = clock.Elapsed.TotalSeconds + seconds;
    while (clock.Elapsed.TotalSeconds < end && !(until?.Invoke() ?? false))
    {
        conn.FrameTick();
        if (stroll is { } to && clock.Elapsed.TotalSeconds - lastStep >= 0.1)
        {
            lastStep = clock.Elapsed.TotalSeconds;
            float dx = to.X - x, dz = to.Z - z, d = MathF.Sqrt(dx * dx + dz * dz);
            if (d < 0.05f) stroll = null;
            else
            {
                float step = MathF.Min(d, 4f * 0.1f);
                x += dx / d * step; z += dz / d * step;
                conn.Reducers.Move(x, z, MathF.Atan2(dx, dz));
            }
        }
        Thread.Sleep(5);
    }
}

Pump(30, () => ready);
if (!ready) { Console.WriteLine("puppet: never joined"); return 1; }
Console.WriteLine($"puppet: joined as player {myId} at {x:F1},{z:F1}");

uint target = 0;
(float X, float Z) Target()
{
    var p = conn.Db.PlayerPos.PlayerId.Find(target);
    return p == null ? (x, z) : (p.X, p.Z);
}

// 10 Hz steps at walking pace, as a client sends them.
void WalkTo(float tx, float tz)
{
    var sw = Stopwatch.StartNew();
    while (sw.Elapsed.TotalSeconds < 60)
    {
        float dx = tx - x, dz = tz - z, d = MathF.Sqrt(dx * dx + dz * dz);
        if (d < 0.05f) break;
        float step = MathF.Min(d, 4f * 0.1f);
        x += dx / d * step; z += dz / d * step;
        conn.Reducers.Move(x, z, MathF.Atan2(dx, dz));
        Pump(0.1);
    }
    // Face the camera player when done, so a bubble reads toward them.
    var (ax, az) = Target();
    conn.Reducers.Move(x, z, MathF.Atan2(ax - x, az - z));
    Pump(0.3);
}

int shots = 0;
string? movieDir = Environment.GetEnvironmentVariable("PUPPET_MOVIE_DIR");
int Frames() => movieDir != null && Directory.Exists(movieDir) ? Directory.GetFiles(movieDir, "*.png").Length : 0;
string Display() => Environment.GetEnvironmentVariable("DISPLAY") ?? ":99";
void Run(string file, string arguments)
{
    using var p = Process.Start(new ProcessStartInfo(file, arguments) { RedirectStandardError = true, RedirectStandardOutput = true })!;
    while (!p.HasExited) Pump(0.05);
    if (p.ExitCode != 0) Console.WriteLine($"puppet: {file} failed: {p.StandardError.ReadToEnd()}");
}

bool Step(string line)
{
    var parts = line.Split(' ', 2);
    string verb = parts[0], rest = parts.Length > 1 ? parts[1].Trim() : "";
    Console.WriteLine($"puppet: {line}");
    switch (verb)
    {
        case "name":
            conn.Reducers.SetName(rest);
            Pump(0.5);
            break;
        case "find":
            Pump(300, () => conn.Db.Player.Iter().Any(p => p.Online && p.PlayerId != myId && conn.Db.PlayerPos.PlayerId.Find(p.PlayerId) != null));
            target = conn.Db.Player.Iter().Where(p => p.Online && p.PlayerId != myId).Select(p => p.PlayerId).DefaultIfEmpty(0u).Max();
            Console.WriteLine(target == 0 ? "puppet: nobody to find" : $"puppet: camera player is {target}");
            break;
        case "wait":
            Pump(double.Parse(rest, System.Globalization.CultureInfo.InvariantCulture));
            break;
        case "near":
        case "away":
        {
            float want = float.Parse(rest, System.Globalization.CultureInfo.InvariantCulture);
            var (ax, az) = Target();
            float dx = x - ax, dz = z - az, d = MathF.Sqrt(dx * dx + dz * dz);
            if (d < 0.1f) { dx = 1; dz = 0; d = 1; }
            WalkTo(ax + dx / d * want, az + dz / d * want);
            break;
        }
        case "goto":
        {
            var xz = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            WalkTo(xz[0], xz[1]);
            break;
        }
        case "stroll":
        {
            var d = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(v => float.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            stroll = (x + d[0], z + d[1]);
            break;
        }
        case "arrive":
            Pump(60, () => stroll == null);
            break;
        case "say":
            conn.Reducers.SendChat(rest);
            Pump(0.1);
            break;
        case "snap":
            shots++;
            Run("import", $"-display {Display()} -window root {prefix}-{shots}.png");
            Console.WriteLine($"puppet: saved {prefix}-{shots}.png");
            break;
        case "burst":
        {
            var b = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int count = int.Parse(b[0]);
            double gap = double.Parse(b[1], System.Globalization.CultureInfo.InvariantCulture);
            for (int i = 0; i < count; i++)
            {
                if (i > 0) Pump(gap);
                shots++;
                Run("import", $"-display {Display()} -window root {prefix}-{shots}.png");
                Console.WriteLine($"puppet: saved {prefix}-{shots}.png");
            }
            break;
        }
        case "clip":
            shots++;
            Run("ffmpeg", $"-loglevel error -y -f x11grab -video_size 1600x900 -framerate 10 -t {rest} -i {Display()} -pix_fmt yuv420p {prefix}-{shots}.mp4");
            Console.WriteLine($"puppet: saved {prefix}-{shots}.mp4");
            break;
        case "give":
        {
            var n = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string quality = n.Length > 2 ? n[2] : "50";
            Run("spacetimedb-cli", $"sql --server {uri} {db} \"INSERT INTO inventory_item (id, owner, item, quality, count) VALUES (0, 0x{me}, {n[0]}, {quality}, {n[1]})\"");
            Pump(1.0);
            break;
        }
        case "craft":
        {
            lastError = null;
            conn.Reducers.StartCraft(rest, 0.8f);
            Pump(3, () => conn.Db.MyCraftJob.Iter().Any() || lastError != null);
            Pump(120, () => !conn.Db.MyCraftJob.Iter().Any() || lastError != null);
            Pump(0.5);
            break;
        }
        case "drop":
        {
            ushort item = ushort.Parse(rest);
            var held = conn.Db.MyInventory.Iter().FirstOrDefault(i => i.Item == item);
            if (held == null) { Console.WriteLine($"puppet: server said nothing to drop ({item})"); break; }
            conn.Reducers.Drop(item, held.Quality);
            Pump(0.8);
            break;
        }
        case "frames":
        {
            int now = Frames();
            int want = rest.StartsWith('+') ? now + int.Parse(rest[1..]) : int.Parse(rest);
            Pump(3600, () => Frames() >= want);
            break;
        }
        case "mark":
            Console.WriteLine($"puppet: mark {rest} frame={Frames()}");
            break;
        case "face":
            conn.Reducers.Move(x, z, float.Parse(rest, System.Globalization.CultureInfo.InvariantCulture) * MathF.PI / 180f);
            Pump(0.5);
            break;
        case "firepots":
        {
            var fire = conn.Db.Fire.Iter().LastOrDefault(f => f.LitBy == myId);
            if (fire == null) { Console.WriteLine("puppet: server said no fire of mine"); break; }
            conn.Reducers.FirePots(fire.Id);
            Pump(1.0);
            break;
        }
        case "click":
            Run("xdotool", $"search --name LifeGame windowfocus mousemove --window %1 {rest} click 1");
            Pump(0.3);
            break;
        case "mouse":
            Run("xdotool", $"search --name LifeGame windowfocus mousemove --window %1 {rest}");
            Pump(0.3);
            break;
        case "keydown":
        case "keyup":
            Run("xdotool", $"search --name LifeGame windowfocus {verb} {rest}");
            Pump(0.1);
            break;
        case "key":
            // Bare Xvfb has no window manager: find the game window by name and focus it.
            Run("xdotool", $"search --name LifeGame windowfocus key --clearmodifiers {rest}");
            Pump(0.3);
            break;
        default:
            Console.WriteLine($"puppet: unknown step '{line}'");
            return false;
    }
    return true;
}

foreach (var line in steps)
{
    if (line == "live")
    {
        string live = Environment.GetEnvironmentVariable("PUPPET_LIVE") ?? "live.txt";
        int done = 0;
        Console.WriteLine($"puppet: live, reading {live}");
        while (true)
        {
            var lines = File.Exists(live) ? File.ReadAllLines(live) : Array.Empty<string>();
            if (done < lines.Length)
            {
                var next = lines[done++].Trim();
                if (next == "end") break;
                if (next.Length > 0 && !next.StartsWith('#')) Step(next);
            }
            else Pump(0.3);
        }
        continue;
    }
    if (!Step(line)) return 2;
}
conn.Disconnect();
return 0;
