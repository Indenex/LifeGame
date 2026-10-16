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
//   say hello there   send a chat line ("/me waves" works too)
//   snap              save the screen to <snap prefix>-<n>.png (n counts from 1)
//   clip 6            record 6 s of the screen to <snap prefix>-<n>.mp4
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
    .WithDatabaseName(db)
    .OnConnect((c, identity, _) =>
    {
        me = identity;
        c.SubscriptionBuilder()
            .OnApplied(ctx =>
            {
                myId = ctx.Db.Player.Identity.Find(identity)!.PlayerId;
                var pos = ctx.Db.PlayerPos.PlayerId.Find(myId)!;
                x = pos.X; z = pos.Z;
                ready = true;
            })
            .Subscribe(new[] { "SELECT * FROM player", "SELECT * FROM player_pos" });
    })
    .OnConnectError(e => { Console.WriteLine($"puppet: connect error {e.Message}"); Environment.Exit(1); })
    .Build();
conn.OnUnhandledReducerError += (_, e) => { lastError = e.Message; Console.WriteLine($"puppet: server said {e.Message}"); };

var clock = Stopwatch.StartNew();
void Pump(double seconds, Func<bool>? until = null)
{
    var end = clock.Elapsed.TotalSeconds + seconds;
    while (clock.Elapsed.TotalSeconds < end && !(until?.Invoke() ?? false))
    {
        conn.FrameTick();
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
string Display() => Environment.GetEnvironmentVariable("DISPLAY") ?? ":99";
void Run(string file, string arguments)
{
    using var p = Process.Start(new ProcessStartInfo(file, arguments) { RedirectStandardError = true, RedirectStandardOutput = true })!;
    while (!p.HasExited) Pump(0.05);
    if (p.ExitCode != 0) Console.WriteLine($"puppet: {file} failed: {p.StandardError.ReadToEnd()}");
}

foreach (var line in steps)
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
        case "say":
            conn.Reducers.SendChat(rest);
            Pump(0.1);
            break;
        case "snap":
            shots++;
            Run("import", $"-display {Display()} -window root {prefix}-{shots}.png");
            Console.WriteLine($"puppet: saved {prefix}-{shots}.png");
            break;
        case "clip":
            shots++;
            Run("ffmpeg", $"-loglevel error -y -f x11grab -video_size 1600x900 -framerate 10 -t {rest} -i {Display()} -pix_fmt yuv420p {prefix}-{shots}.mp4");
            Console.WriteLine($"puppet: saved {prefix}-{shots}.mp4");
            break;
        default:
            Console.WriteLine($"puppet: unknown step '{line}'");
            return 2;
    }
}
conn.Disconnect();
return 0;
