using System;

namespace LifeGame;

// /stuck in chat (bugfix-44). Main sets Request once connected.
public static class StuckCommand
{
    public static Action? Request;

    public static bool Try(string text, Action<string> note)
    {
        if (text.Trim().ToLowerInvariant() != "/stuck") return false;
        if (Request == null) note("You aren't connected to a world");
        else Request();
        return true;
    }
}
