using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using SpacetimeDB.Types;

namespace LifeGame;

// The in-game bug report (bugfix-42). F8 (or the Esc menu) takes a screenshot of what you
// were looking at, then asks what went wrong. Send writes a zip to user://bug-reports/
// (report.md, screenshot.png, log.txt), copies a one-line summary to the clipboard, and
// sends the summary and report text to the server (submit_bug_report) so the admin can read
// it with /bugs. Then it sends the log tail and a smaller JPEG of the screenshot in parts
// (attach_bug_report), and the server opens a GitHub issue with them (bugfix-64); the window
// shows how that went from my_bug_report_sends. The auth token is never part of a report.
// Main sets Conn and Facts; UX styles the window.
public partial class BugReportPanel : CanvasLayer
{
    public Func<DbConnection?> Conn = () => null;
    // The game state lines (player, position, time, bag ...) Main knows; one "Key: value" per line.
    public Func<IEnumerable<string>> Facts = () => Array.Empty<string>();

    public bool IsOpen => panel.Visible;

    const int LogLines = 200;
    // The GitHub copy of the screenshot: at most this wide, sent in parts of this many bytes.
    const int ShotWidth = 1280;
    const int ShotPart = 60_000;

    Control panel = null!;
    LineEdit what = null!;
    Label result = null!, thanks = null!;
    TextureRect picture = null!;
    Control asking = null!;
    Image? shot;
    // Reports with a higher id than this are the one just sent; 0 while nothing is being watched.
    ulong watchAbove;
    bool watching;
    double watchClock;
    Label sending = null!;

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

    public override void _Process(double delta)
    {
        if (!watching || !panel.Visible) return;
        watchClock -= delta;
        if (watchClock > 0) return;
        watchClock = 0.5;
        if (Conn() is not { } conn) return;
        var row = conn.Db.MyBugReportSends.Iter().Where(r => r.ReportId > watchAbove).OrderByDescending(r => r.ReportId).FirstOrDefault();
        if (row == null) return;
        sending.Text = row.State switch
        {
            "sent" => $"It's on GitHub as issue #{row.Issue}.",
            "waiting" => "Sending it to GitHub...",
            _ => $"{row.Note}.",
        };
        if (row.State != "waiting") watching = false;
    }

    public void Open()
    {
        // The picture is of the world as it was, before this window covers it.
        shot = DisplayServer.GetName() == "headless" ? null : GetViewport().GetTexture()?.GetImage();
        Panels.Opening(this);
        what.Text = "";
        result.Text = "";
        thanks.Visible = false;
        sending.Text = "";
        watching = false;
        asking.Visible = true;
        // The picture that goes with the report, small, so you can see what is sent.
        picture.Texture = shot == null ? null : ImageTexture.CreateFromImage(shot);
        picture.Visible = shot != null;
        panel.Visible = true;
        what.GrabFocus();
    }

    public void Close()
    {
        panel.Visible = false;
        what.ReleaseFocus();
    }

    // The look (UX, ui-29): the shared window, a clear field, Send as the main button, the
    // screenshot as a small picture, and a plain thank-you once it's sent.
    Control MakePanel()
    {
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(520, 0) };
        box.AddThemeConstantOverride("separation", 12);
        box.AddChild(Ui.Title("Report a bug"));

        asking = new VBoxContainer();
        asking.AddThemeConstantOverride("separation", 12);
        box.AddChild(asking);
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 14);
        asking.AddChild(top);
        var ask = Hud.Text("What went wrong? A few words is plenty. This picture and your game details go with it.", 14);
        ask.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        ask.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        ask.Modulate = new Color(1, 1, 1, 0.8f);
        top.AddChild(ask);
        picture = new TextureRect
        {
            CustomMinimumSize = new Vector2(160, 90),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        top.AddChild(picture);

        what = new LineEdit { PlaceholderText = "For example: my sleeping mat vanished", MaxLength = 300, CustomMinimumSize = new Vector2(0, 40) };
        what.AddThemeFontSizeOverride("font_size", 16);
        var field = new StyleBoxFlat { BgColor = new Color(0.05f, 0.05f, 0.04f, 0.9f), BorderColor = new Color(Look.Cream, 0.35f) };
        field.SetBorderWidthAll(1);
        field.SetCornerRadiusAll(6);
        field.ContentMarginLeft = field.ContentMarginRight = 10;
        var focused = (StyleBoxFlat)field.Duplicate();
        focused.BorderColor = new Color(Look.Cream, 0.8f);
        what.AddThemeStyleboxOverride("normal", field);
        what.AddThemeStyleboxOverride("focus", focused);
        what.TextSubmitted += _ => Send();
        asking.AddChild(what);

        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        row.AddThemeConstantOverride("separation", 10);
        row.AddChild(ControlsPage.TextButton("Cancel", Close));
        row.AddChild(MainButton("Send report", Send));
        asking.AddChild(row);

        // After Send: a plain thank-you, with where the copy went in smaller print.
        thanks = Hud.Text("Thanks, it's sent.", 18);
        thanks.Modulate = new Color(0.8f, 1f, 0.75f);
        thanks.Visible = false;
        box.AddChild(thanks);
        sending = Hud.Text("", 14);
        sending.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        sending.Modulate = new Color(1, 1, 1, 0.85f);
        box.AddChild(sending);
        result = Hud.Text("", 13);
        result.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        result.Modulate = new Color(1, 1, 1, 0.75f);
        box.AddChild(result);

        var hint = Hud.Text("Enter sends    Esc closes    F8 opens this any time", 13);
        hint.Modulate = new Color(1, 1, 1, 0.55f);
        box.AddChild(hint);
        return Ui.Window(box);
    }

    // The one filled button: the thing to do here.
    static Button MainButton(string text, Action pressed)
    {
        var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(150, 38) };
        b.AddThemeFontSizeOverride("font_size", 16);
        var style = new StyleBoxFlat { BgColor = new Color(0.55f, 0.42f, 0.24f, 0.95f) };
        style.SetCornerRadiusAll(8);
        var hover = (StyleBoxFlat)style.Duplicate();
        hover.BgColor = new Color(0.64f, 0.5f, 0.3f, 1f);
        b.AddThemeStyleboxOverride("normal", style);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", hover);
        b.AddThemeColorOverride("font_color", Look.Cream);
        b.AddThemeColorOverride("font_hover_color", Look.Cream);
        b.Pressed += pressed;
        return b;
    }

    void Send()
    {
        string said = what.Text.Trim();
        if (said.Length == 0) { result.Text = "Say what went wrong first."; return; }
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string report = Report(said);
        string summary = Summary(said);
        string zip = $"user://bug-reports/{stamp}.zip";
        string log = Tail(LogLines);
        string? error = WriteZip(zip, report, shot, log);
        DisplayServer.ClipboardSet(summary);
        sending.Text = "";
        watching = false;
        if (Conn() is { } conn)
        {
            try
            {
                watchAbove = conn.Db.MyBugReportSends.Iter().Select(r => r.ReportId).DefaultIfEmpty(0UL).Max();
                watchAbove = Math.Max(watchAbove, conn.Db.MyBugReports.Iter().Select(r => r.Id).DefaultIfEmpty(0UL).Max());
                conn.Reducers.SubmitBugReport(summary, report);
                Attach(conn, log, GitHubShot(shot));
                watching = true;
                watchClock = 0.5;
            }
            catch (Exception e) { GD.PushWarning($"bug report not sent: {e.Message}"); }
        }
        string folder = ProjectSettings.GlobalizePath("user://bug-reports");
        result.Text = error == null
            ? $"A copy is saved in {folder}, and a one-line summary is on your clipboard."
            : $"Saving a copy on this computer failed ({error}), but the server has it.";
        thanks.Visible = true;
        asking.Visible = false;
        what.Text = "";
    }

    // The log tail goes with the first part, and the last part says the report may go.
    static void Attach(DbConnection conn, string log, byte[] jpg)
    {
        int parts = Math.Max(1, (jpg.Length + ShotPart - 1) / ShotPart);
        for (int i = 0; i < parts; i++)
        {
            int from = i * ShotPart, to = Math.Min(jpg.Length, from + ShotPart);
            conn.Reducers.AttachBugReport(i == 0 ? log : "", jpg[from..to].ToList(), i == parts - 1);
        }
    }

    // A smaller JPEG of the screenshot for the GitHub issue; the zip keeps the full PNG.
    static byte[] GitHubShot(Image? shot)
    {
        if (shot == null || shot.IsEmpty()) return Array.Empty<byte>();
        var small = (Image)shot.Duplicate();
        if (small.GetWidth() > ShotWidth)
            small.Resize(ShotWidth, Math.Max(1, small.GetHeight() * ShotWidth / small.GetWidth()), Image.Interpolation.Lanczos);
        return small.SaveJpgToBuffer(0.8f);
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
