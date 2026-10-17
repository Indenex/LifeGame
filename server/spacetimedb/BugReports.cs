using SpacetimeDB;

// In-game bug reports (bugfix-42). The F8 panel on the client gathers a short summary and
// the full report text and sends them here. The table is private: a player reads only
// their own reports and an admin reads all of them, both through the my_bug_reports view.
// The client never sends the auth token, and the server stores only what it is given.
public static partial class Module
{
    const int BugSummaryMax = 400;
    const int BugTextMax = 24_000;
    const int BugReportsPerPlayer = 20;

    [SpacetimeDB.Table(Accessor = "BugReport")]
    public partial struct BugReport
    {
        [PrimaryKey]
        [AutoInc]
        public ulong Id;
        [SpacetimeDB.Index.BTree]
        public Identity Reporter;
        // Always true while the report stands; views may only read by index, so admins read by this.
        [SpacetimeDB.Index.BTree]
        public bool Open;
        public uint PlayerId;
        public string PlayerName;
        public Timestamp At;
        // The line of facts the player can copy: build, position, time, and what they said.
        public string Summary;
        // The whole report.md text (no screenshot, no log; those stay in the player's zip).
        public string Text;
    }

    // Own reports, or every report for an admin.
    [SpacetimeDB.View(Accessor = "MyBugReports", Public = true)]
    public static List<BugReport> MyBugReports(ViewContext ctx)
    {
        if (ctx.Db.WorldAdmin.Identity.Find(ctx.Sender) is not null) return ctx.Db.BugReport.Open.Filter(true).ToList();
        return ctx.Db.BugReport.Reporter.Filter(ctx.Sender).ToList();
    }

    [SpacetimeDB.Reducer]
    public static void SubmitBugReport(ReducerContext ctx, string summary, string text)
    {
        var player = RequirePlayer(ctx);
        if (string.IsNullOrWhiteSpace(summary)) throw new Exception("Say what went wrong first");
        if (summary.Length > BugSummaryMax) summary = summary[..BugSummaryMax];
        if (text.Length > BugTextMax) text = text[..BugTextMax];
        // Keep the newest few: older ones drop off so nobody can fill the table.
        var mine = ctx.Db.BugReport.Reporter.Filter(ctx.Sender).OrderBy(r => r.Id).ToList();
        for (int i = 0; i <= mine.Count - BugReportsPerPlayer; i++) ctx.Db.BugReport.Id.Delete(mine[i].Id);
        ctx.Db.BugReport.Insert(new BugReport
        {
            Reporter = ctx.Sender,
            Open = true,
            PlayerId = player.PlayerId,
            PlayerName = player.Name,
            At = ctx.Timestamp,
            Summary = summary,
            Text = text,
        });
        Log.Info($"bug_report: {player.Name} ({player.PlayerId}): {summary}");
    }

    // Admins clear a report once it is dealt with.
    [SpacetimeDB.Reducer]
    public static void AdminClearBugReport(ReducerContext ctx, ulong id)
    {
        if (!IsWorldAdmin(ctx)) throw new Exception("Only an admin can clear bug reports");
        ctx.Db.BugReport.Id.Delete(id);
    }
}
