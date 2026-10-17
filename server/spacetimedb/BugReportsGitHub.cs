#pragma warning disable STDB_UNSTABLE
using System.Text;
using System.Text.RegularExpressions;
using SpacetimeDB;

// F8 reports go to GitHub on their own (bugfix-64). Justin's token lives only in the private
// BugReportGitHub table, set once by the publisher with set_bug_report_token from the server
// machine; it never reaches a client, the repo or any table a client can read. SubmitBugReport
// queues each report here, the client then sends its log tail and screenshot in parts, and the
// SendBugReports procedure (the only code that may make HTTP calls) uploads the screenshot to
// the bug-reports branch and opens an issue. A player reads how their own reports went through
// my_bug_report_sends. Rate limits keep anyone from spamming issues with the token.
public static partial class Module
{
    const string BugRepo = "Indenex/LifeGame";
    const string BugShotBranch = "bug-reports";
    const int BugLogMax = 30_000;
    const int BugShotMax = 1_500_000;
    const int BugSendsPerHour = 3;
    const int BugSendsPerDay = 10;
    const int BugSendsWorldPerDay = 40;
    const int BugSendAttempts = 3;
    // A queued report waits this long for its screenshot before it goes without one.
    const long BugAttachWaitMicros = 60_000_000;
    // How long one send may hold a report before another run may try it again.
    const long BugSendLeaseMicros = 300_000_000;
    const long HourMicros = 3_600_000_000;

    [SpacetimeDB.Table(Accessor = "BugReportGitHub")]
    public partial struct BugReportGitHub
    {
        [PrimaryKey]
        public uint Id;
        public string Token;
    }

    // A report waiting to go to GitHub, with what the client attached.
    [SpacetimeDB.Table(Accessor = "BugReportOutbox")]
    public partial struct BugReportOutbox
    {
        [PrimaryKey]
        public ulong ReportId;
        [SpacetimeDB.Index.BTree]
        public Identity Reporter;
        // True once the last screenshot part is in; until then attachments may still come.
        public bool Complete;
        public string Log;
        public List<byte> Shot;
        public byte Attempts;
        public Timestamp SendAfter;
    }

    // How each report went: "waiting", "sent" (Issue is its number) or "kept" (Note says why).
    [SpacetimeDB.Table(Accessor = "BugReportSend")]
    public partial struct BugReportSend
    {
        [PrimaryKey]
        public ulong ReportId;
        [SpacetimeDB.Index.BTree]
        public Identity Reporter;
        public string State;
        public uint Issue;
        public string Note;
    }

    // One row per report queued for GitHub in the last day, for the rate limits.
    [SpacetimeDB.Table(Accessor = "BugReportSendLog")]
    public partial struct BugReportSendLog
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public Identity Reporter;
        public Timestamp At;
    }

    [SpacetimeDB.Table(Accessor = "BugReportSendTimer", Scheduled = nameof(SendBugReports), ScheduledAt = nameof(ScheduledAt))]
    public partial struct BugReportSendTimer
    {
        [PrimaryKey]
        [AutoInc]
        public ulong ScheduledId;
        public ScheduleAt ScheduledAt;
    }

    [SpacetimeDB.View(Accessor = "MyBugReportSends", Public = true)]
    public static List<BugReportSend> MyBugReportSends(ViewContext ctx) =>
        ctx.Db.BugReportSend.Reporter.Filter(Me(ctx)).ToList();

    // The publisher sets (or, with "", clears) the token: spacetime call <db> set_bug_report_token '"..."'.
    [SpacetimeDB.Reducer]
    public static void SetBugReportToken(ReducerContext ctx, string token)
    {
        if (ctx.Db.ClaimConfig.Id.Find(0)?.Admin != Me(ctx)) throw new Exception("Only the server's publisher can set the GitHub token");
        token = token.Trim();
        ctx.Db.BugReportGitHub.Id.Delete(0);
        if (token.Length > 0) ctx.Db.BugReportGitHub.Insert(new BugReportGitHub { Id = 0, Token = token });
        Log.Info(token.Length > 0 ? "bug_report: GitHub token set" : "bug_report: GitHub token cleared");
    }

    // Called by SubmitBugReport for each new report.
    static void QueueBugReportForGitHub(ReducerContext ctx, ulong reportId)
    {
        var me = Me(ctx);
        // Old reports' rows go with them, so the status table stays as small as BugReport.
        foreach (var s in ctx.Db.BugReportSend.Reporter.Filter(me).ToList())
            if (ctx.Db.BugReport.Id.Find(s.ReportId) is null) ctx.Db.BugReportSend.ReportId.Delete(s.ReportId);
        string? kept = BugSendRefusal(ctx, me);
        if (kept != null)
        {
            ctx.Db.BugReportSend.Insert(new BugReportSend { ReportId = reportId, Reporter = me, State = "kept", Note = kept });
            return;
        }
        ctx.Db.BugReportSendLog.Insert(new BugReportSendLog { Reporter = me, At = ctx.Timestamp });
        // A report still waiting for its parts is replaced by the newer one.
        foreach (var o in ctx.Db.BugReportOutbox.Reporter.Filter(me).Where(o => !o.Complete).ToList())
        {
            ctx.Db.BugReportOutbox.ReportId.Delete(o.ReportId);
            SetBugSend(ctx, o.ReportId, o.Reporter, "kept", 0, "A newer report replaced this one before it was sent");
        }
        var after = ctx.Timestamp + new TimeDuration(BugAttachWaitMicros);
        ctx.Db.BugReportOutbox.Insert(new BugReportOutbox
        {
            ReportId = reportId,
            Reporter = me,
            Complete = false,
            Log = "",
            Shot = new List<byte>(),
            SendAfter = after,
        });
        ctx.Db.BugReportSend.Insert(new BugReportSend { ReportId = reportId, Reporter = me, State = "waiting", Note = "" });
        ctx.Db.BugReportSendTimer.Insert(new BugReportSendTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Time(after) });
    }

    // Why this report stays in game only, or null when it may go to GitHub.
    static string? BugSendRefusal(ReducerContext ctx, Identity me)
    {
        if (ctx.Db.BugReportGitHub.Id.Find(0) is null) return "Sending reports to GitHub isn't set up on this server, so it's kept here for the admin";
        long now = ctx.Timestamp.MicrosecondsSinceUnixEpoch;
        foreach (var old in ctx.Db.BugReportSendLog.Iter().Where(l => l.At.MicrosecondsSinceUnixEpoch < now - 24 * HourMicros).ToList())
            ctx.Db.BugReportSendLog.Id.Delete(old.Id);
        var mine = ctx.Db.BugReportSendLog.Reporter.Filter(me).ToList();
        if (mine.Count(l => l.At.MicrosecondsSinceUnixEpoch >= now - HourMicros) >= BugSendsPerHour) return $"You've sent {BugSendsPerHour} reports to GitHub this hour, so this one is kept here for the admin";
        if (mine.Count >= BugSendsPerDay) return $"You've sent {BugSendsPerDay} reports to GitHub today, so this one is kept here for the admin";
        if (ctx.Db.BugReportSendLog.Count >= BugSendsWorldPerDay) return "This server has sent its reports to GitHub for today, so this one is kept here for the admin";
        return null;
    }

    static void SetBugSend(ReducerContext ctx, ulong reportId, Identity reporter, string state, uint issue, string note)
    {
        ctx.Db.BugReportSend.ReportId.Delete(reportId);
        ctx.Db.BugReportSend.Insert(new BugReportSend { ReportId = reportId, Reporter = reporter, State = state, Issue = issue, Note = note });
    }

    // The client sends its log tail with the first part and the screenshot (a JPEG) in parts;
    // last marks the final part, and the report goes right away.
    [SpacetimeDB.Reducer]
    public static void AttachBugReport(ReducerContext ctx, string log, List<byte> shotPart, bool last)
    {
        var me = Me(ctx);
        var open = ctx.Db.BugReportOutbox.Reporter.Filter(me).Where(o => !o.Complete).OrderByDescending(o => o.ReportId).ToList();
        if (open.Count == 0) return;
        var job = open[0];
        if (log.Length > 0 && job.Log.Length == 0) job.Log = log.Length > BugLogMax ? log[^BugLogMax..] : log;
        // A picture too big to send whole goes without one.
        if (job.Shot.Count + shotPart.Count <= BugShotMax) job.Shot.AddRange(shotPart);
        else job.Shot = new List<byte>();
        if (last)
        {
            job.Complete = true;
            job.SendAfter = ctx.Timestamp;
            ctx.Db.BugReportSendTimer.Insert(new BugReportSendTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Time(ctx.Timestamp) });
        }
        ctx.Db.BugReportOutbox.ReportId.Update(job);
    }

    // Sends one report per run, then schedules the next if one is due. HTTP happens only
    // between the transactions.
    [SpacetimeDB.Procedure]
    public static void SendBugReports(ProcedureContext ctx, BugReportSendTimer timer)
    {
        string token = "";
        BugReportOutbox? picked = null;
        BugReport? pickedReport = null;
        ctx.WithTx(tx =>
        {
            picked = null;
            pickedReport = null;
            token = tx.Db.BugReportGitHub.Id.Find(0)?.Token ?? "";
            foreach (var o in tx.Db.BugReportOutbox.Iter())
                if (o.SendAfter.MicrosecondsSinceUnixEpoch <= tx.Timestamp.MicrosecondsSinceUnixEpoch && (picked is not { } p || o.ReportId < p.ReportId)) picked = o;
            if (picked is not { } job) return 0;
            // Held while this run sends it, so a second run doesn't open the same issue.
            tx.Db.BugReportOutbox.ReportId.Update(job with { Complete = true, SendAfter = tx.Timestamp + new TimeDuration(BugSendLeaseMicros) });
            pickedReport = tx.Db.BugReport.Id.Find(job.ReportId);
            return 0;
        });
        if (picked is not { } sending) return;

        string state, note = "";
        uint issue = 0;
        if (pickedReport is not { } report) { state = "gone"; }
        else if (token.Length == 0) { state = "kept"; note = "Sending reports to GitHub isn't set up on this server, so it's kept here for the admin"; }
        else
        {
            string? shotUrl = sending.Shot.Count > 0 ? UploadBugShot(ctx, token, report, sending.Shot) : null;
            var (status, body) = GitHub(ctx, token, SpacetimeDB.HttpMethod.Post, $"/repos/{BugRepo}/issues",
                $"{{\"title\":{Json(BugIssueTitle(report))},\"body\":{Json(BugIssueBody(report, sending.Log, shotUrl))}}}");
            if (status == 201 && Regex.Match(body, "\"number\"\\s*:\\s*(\\d+)") is { Success: true } m)
            {
                state = "sent";
                issue = uint.Parse(m.Groups[1].Value);
            }
            else
            {
                state = sending.Attempts + 1 >= BugSendAttempts ? "kept" : "retry";
                note = status == 0 ? "GitHub couldn't be reached" : $"GitHub said {status}";
                Log.Warn($"bug_report: issue for report {sending.ReportId} failed: {status} {Short(body, 300)}");
            }
        }

        bool more = false;
        ctx.WithTx(tx =>
        {
            if (state == "retry")
            {
                // Waits a little longer after each failure: 1, then 4 minutes.
                long wait = 60_000_000L << (2 * sending.Attempts);
                var at = tx.Timestamp + new TimeDuration(wait);
                if (tx.Db.BugReportOutbox.ReportId.Find(sending.ReportId) is { } still)
                    tx.Db.BugReportOutbox.ReportId.Update(still with { Attempts = (byte)(still.Attempts + 1), SendAfter = at });
                tx.Db.BugReportSendTimer.Insert(new BugReportSendTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Time(at) });
            }
            else
            {
                tx.Db.BugReportOutbox.ReportId.Delete(sending.ReportId);
                tx.Db.BugReportSend.ReportId.Delete(sending.ReportId);
                if (state != "gone")
                    tx.Db.BugReportSend.Insert(new BugReportSend { ReportId = sending.ReportId, Reporter = sending.Reporter, State = state, Issue = issue, Note = note });
            }
            more = false;
            foreach (var o in tx.Db.BugReportOutbox.Iter())
                if (o.SendAfter.MicrosecondsSinceUnixEpoch <= tx.Timestamp.MicrosecondsSinceUnixEpoch) { more = true; break; }
            if (more) tx.Db.BugReportSendTimer.Insert(new BugReportSendTimer { ScheduledId = 0, ScheduledAt = new ScheduleAt.Time(tx.Timestamp) });
            return 0;
        });
        if (state == "sent") Log.Info($"bug_report: report {sending.ReportId} is issue #{issue}");
    }

    // Puts the screenshot on the bug-reports branch (made from main the first time) and returns
    // a link that shows it, or null when that fails; the issue then goes without the picture.
    static string? UploadBugShot(ProcedureContext ctx, string token, BugReport report, List<byte> shot)
    {
        var (status, _) = GitHub(ctx, token, SpacetimeDB.HttpMethod.Get, $"/repos/{BugRepo}/git/ref/heads/{BugShotBranch}", null);
        if (status == 404)
        {
            var (mainStatus, mainBody) = GitHub(ctx, token, SpacetimeDB.HttpMethod.Get, $"/repos/{BugRepo}/git/ref/heads/main", null);
            var sha = Regex.Match(mainBody, "\"sha\"\\s*:\\s*\"([0-9a-f]{40})\"");
            if (mainStatus != 200 || !sha.Success) return null;
            (status, _) = GitHub(ctx, token, SpacetimeDB.HttpMethod.Post, $"/repos/{BugRepo}/git/refs",
                $"{{\"ref\":\"refs/heads/{BugShotBranch}\",\"sha\":\"{sha.Groups[1].Value}\"}}");
            if (status != 201) return null;
        }
        else if (status != 200) return null;
        var at = DateTimeOffset.FromUnixTimeMilliseconds(report.At.MicrosecondsSinceUnixEpoch / 1000).UtcDateTime;
        string path = $"bug-reports/{at:yyyyMMdd-HHmmss}-{report.Id}.jpg";
        (status, _) = GitHub(ctx, token, SpacetimeDB.HttpMethod.Put, $"/repos/{BugRepo}/contents/{path}",
            $"{{\"message\":{Json($"Screenshot for bug report {report.Id}")},\"branch\":\"{BugShotBranch}\",\"content\":\"{Convert.ToBase64String(shot.ToArray())}\"}}");
        return status is 200 or 201 ? $"https://github.com/{BugRepo}/blob/{BugShotBranch}/{path}?raw=true" : null;
    }

    static string BugIssueTitle(BugReport report)
    {
        string said = report.Summary.Split(" | ")[0].Trim();
        if (said.Length > 90) said = said[..90] + "...";
        return $"F8: {NoMentions(said)}";
    }

    static string BugIssueBody(BugReport report, string log, string? shotUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Sent from the game by **{NoMentions(report.PlayerName)}** (player {report.PlayerId}), server report {report.Id}.").AppendLine();
        sb.AppendLine($"> {NoMentions(report.Summary)}").AppendLine();
        if (shotUrl != null) sb.AppendLine($"![screenshot]({shotUrl})").AppendLine();
        sb.AppendLine(NoMentions(report.Text)).AppendLine();
        if (log.Length > 0)
        {
            // The issue body holds 65536 characters; the report text is at most 24000.
            string tail = log.Replace("```", "'''");
            int room = 60_000 - sb.Length;
            if (tail.Length > room) tail = tail[^Math.Max(0, room)..];
            sb.AppendLine("<details><summary>Log (last lines)</summary>").AppendLine().AppendLine("```");
            sb.AppendLine(NoMentions(tail)).AppendLine("```").AppendLine("</details>");
        }
        return sb.ToString();
    }

    // Player text must not ping anyone on GitHub.
    static string NoMentions(string s) => s.Replace("@", "@​");

    static (ushort status, string body) GitHub(ProcedureContext ctx, string token, SpacetimeDB.HttpMethod method, string path, string? json)
    {
        var headers = new List<HttpHeader>
        {
            new("User-Agent", "LifeGame-bug-reports"),
            new("Accept", "application/vnd.github+json"),
            new("X-GitHub-Api-Version", "2022-11-28"),
            new("Authorization", Encoding.UTF8.GetBytes($"Bearer {token}"), true),
        };
        if (json != null) headers.Add(new("Content-Type", "application/json"));
        var result = ctx.Http.Send(new HttpRequest
        {
            Uri = "https://api.github.com" + path,
            Method = method,
            Headers = headers,
            Body = json == null ? HttpBody.Empty : HttpBody.FromString(json),
            Timeout = TimeSpan.FromSeconds(60),
        });
        return result.Match(
            response => (response.StatusCode, response.Body.ToStringUtf8Lossy()),
            error => ((ushort)0, error.Message));
    }

    static string Json(string s)
    {
        var sb = new StringBuilder(s.Length + 16).Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < ' ') sb.Append($"\\u{(int)c:x4}");
                    else sb.Append(c);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }

    static string Short(string s, int max) => s.Length <= max ? s : s[..max];
}
