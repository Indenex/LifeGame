using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// The in-game bug report (bugfix-42). F8 (or the Esc menu) takes a screenshot of what you
// were looking at, then asks what went wrong. Send writes a zip to user://bug-reports/
// (report.md, screenshot.png, log.txt), opens that folder, copies a one-line summary to the
// clipboard, and sends the summary and report text to the server (submit_bug_report) so
// the admin can read it with /bugs. The auth token is never part of a report.
// Main sets Conn and Facts; UX styles the window.
public partial class BugReportPanel : CanvasLayer
{
    public Func<DbConnection?> Conn = () => null;
    // The game state lines (player, position, time, bag ...) Main knows; one "Key: value" per line.
    public Func<IEnumerable<string>> Facts = () => Array.Empty<string>();

    public bool IsOpen => panel.Visible;

    const int LogLines = 200;

    Control panel = null!;
    LineEdit what = null!;
    Label result = null!;
    Image? shot;

    public override void _Ready()
    {
        Layer = 6;
        if (!InputMap.HasAction("bug_report"))
        {
            InputMap.AddAction("bug_report");
            InputMap.ActionAddEvent("bug_report", new InputEventKey { PhysicalKeycode = Key.F8 });
        }
        panel = MakePanel();
        panel.Visible = false;
        AddChild(panel);
        Panels.Centre(this, Close);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false }) return;
        if (e.IsActionPressed("bug_report") && !IsOpen) Open();
        else if (IsOpen && e.IsActionPressed("ui_cancel")) Close();
        else return;
        GetViewport().SetInputAsHandled();
    }

    public void Open()
    {
        // The picture is of the world as it was, before this window covers it.
        shot = DisplayServer.GetName() == "headless" ? null : GetViewport().GetTexture()?.GetImage();
        Panels.Opening(this);
        what.Text = "";
        result.Text = "";
        panel.Visible = true;
        what.GrabFocus();
    }

    public void Close()
    {
        panel.Visible = false;
        what.ReleaseFocus();
    }

    Control MakePanel()
    {
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(460, 0) };
        box.AddThemeConstantOverride("separation", 10);
        box.AddChild(Ui.Title("Report a bug"));
        box.AddChild(Hud.Text("What went wrong? A few words is plenty. A picture of the game and your details are added for you.", 14));
        what = new LineEdit { PlaceholderText = "For example: my sleeping mat vanished", MaxLength = 300 };
        what.TextSubmitted += _ => Send();
        box.AddChild(what);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        row.AddChild(ControlsPage.TextButton("Send report", Send));
        row.AddChild(ControlsPage.TextButton("Cancel", Close));
        box.AddChild(row);
        result = Hud.Text("", 13);
        result.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(result);
        return Ui.Window(box);
    }

    void Send()
    {
        string said = what.Text.Trim();
        if (said.Length == 0) { result.Text = "Say what went wrong first."; return; }
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string report = Report(said);
        string summary = Summary(said);
        string zip = $"user://bug-reports/{stamp}.zip";
        string? error = WriteZip(zip, report, shot, Tail(LogLines));
        DisplayServer.ClipboardSet(summary);
        try { Conn()?.Reducers.SubmitBugReport(summary, report); }
        catch (Exception e) { GD.PushWarning($"bug report not sent: {e.Message}"); }
        string folder = ProjectSettings.GlobalizePath("user://bug-reports");
        if (error == null) OS.ShellOpen(folder);
        result.Text = error == null
            ? $"Sent. A copy is saved in {folder}. The summary is on your clipboard."
            : $"Sent to the server. Saving the copy failed: {error}";
        what.Text = "";
    }

    // --- Contents ----------------------------------------------------------

    string Summary(string said) =>
        $"{said} | {Fact("Player")} | {Fact("Position")} | {Fact("Build")} | {Fact("Time")}";

    string Fact(string key) =>
        Facts().FirstOrDefault(l => l.StartsWith(key + ":"))?[(key.Length + 1)..].Trim() ?? "?";

    string Report(string said)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Bug report").AppendLine().AppendLine(said).AppendLine();
        sb.AppendLine("## Game");
        foreach (var l in Facts()) sb.AppendLine($"- {l}");
        sb.AppendLine($"- Os: {OS.GetName()} {OS.GetVersion()}");
        sb.AppendLine($"- Gpu: {RenderingServer.GetVideoAdapterName()} ({RenderingServer.GetVideoAdapterVendor()})");
        sb.AppendLine($"- Window: {DisplayServer.WindowGetSize().X}x{DisplayServer.WindowGetSize().Y}");
        sb.AppendLine($"- Fps: {Engine.GetFramesPerSecond()}");
        sb.AppendLine($"- Launcher: {Env("LIFEGAME_LAUNCHER") ?? "none"}");
        sb.AppendLine($"- Server: {Env("LIFEGAME_SERVER") ?? "default"} {(Conn() == null ? "(not connected)" : "(connected)")}");
        sb.AppendLine().AppendLine("## Last notes shown");
        foreach (var n in BugLog.Notes) sb.AppendLine($"- {n}");
        sb.AppendLine().AppendLine("## Last server errors");
        foreach (var n in BugLog.Errors) sb.AppendLine($"- {n}");
        return sb.ToString();
    }

    static string? Env(string name) => OS.GetEnvironment(name) is { Length: > 0 } v ? v : null;

    // The last lines of this run's log. Godot writes it to user://logs/godot.log.
    static string Tail(int lines)
    {
        const string path = "user://logs/godot.log";
        if (!FileAccess.FileExists(path)) return "(no log file)";
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (f == null) return "(log not readable)";
        var all = f.GetAsText().Split('\n');
        return string.Join('\n', all.Skip(Math.Max(0, all.Length - lines)));
    }

    static string? WriteZip(string path, string report, Image? shot, string log)
    {
        try
        {
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("user://bug-reports"));
            var zip = new ZipPacker();
            var err = zip.Open(ProjectSettings.GlobalizePath(path));
            if (err != Error.Ok) return err.ToString();
            Add(zip, "report.md", Encoding.UTF8.GetBytes(report));
            if (shot != null) Add(zip, "screenshot.png", shot.SavePngToBuffer());
            Add(zip, "log.txt", Encoding.UTF8.GetBytes(log));
            zip.Close();
            return null;
        }
        catch (Exception e) { return e.Message; }
    }

    static void Add(ZipPacker zip, string name, byte[] data)
    {
        zip.StartFile(name);
        zip.WriteFile(data);
        zip.CloseFile();
    }

    // Admin /bugs: the newest reports the server holds, one line each.
    public static bool Try(DbConnection conn, string text, Action<string> note)
    {
        if (text.Trim().ToLowerInvariant() != "/bugs") return false;
        if (!TimeCommands.IsAdmin(conn))
        {
            note("Only the world's admin can list bug reports");
            return true;
        }
        var all = conn.Db.MyBugReports.Iter().OrderByDescending(r => r.Id).ToList();
        if (all.Count == 0) { note("No bug reports"); return true; }
        note($"{all.Count} bug report(s), newest first");
        foreach (var r in all.Take(5)) note($"#{r.Id} {r.PlayerName}: {r.Summary}");
        return true;
    }
}
