using System;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Tells a server that doesn't match this game apart from a real error (bugfix-7).
// A server older than the client is missing tables or reducers the client asks
// for; rather than raw "no such table" errors, the player sees one calm line.
public static class ServerCheck
{
    public const string Older = "Your server is older than this game. Publish the new server, then restart.";

    // Set by Main to a line that stays on screen.
    public static Action<string>? Warn;
    static bool warned;

    public static bool Mismatch(string message) =>
        message.Contains("no such table", StringComparison.OrdinalIgnoreCase)
        || message.Contains("no such reducer", StringComparison.OrdinalIgnoreCase);

    // Shows the calm line once for a mismatch; anything else goes to note as before.
    public static void Failed(string message, Action<string> note, string prefix = "Subscription error: ")
    {
        if (!Mismatch(message)) { note(prefix + message); return; }
        GD.PushWarning($"Server mismatch (game build {BuildStamp.Commit}): {message}");
        if (warned) return;
        warned = true;
        Warn?.Invoke(Older);
    }

    // The build the server says it runs. A different build is fine as long as the
    // tables match, so it is only logged; missing tables and reducers are what warn.
    public static void Compare(string serverBuild)
    {
        if (serverBuild != BuildStamp.Commit)
            GD.Print($"Server build {serverBuild}, game build {BuildStamp.Commit}");
    }
}
