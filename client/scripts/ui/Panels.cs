using System;
using System.Collections.Generic;

namespace LifeGame;

// Whether a full panel (journal, claim panel, Esc menu) covers the world right now.
// While it does, world HUD such as hints, the G prompt, the inventory row, the land
// chip and the needs rings fade away, so nothing shows through or collides with it.
// Main sets Open; anything on the HUD reads it.
public static class Panels
{
    public static Func<bool> Open = () => false;

    // Whether a side panel (crafting) is up. The world still shows beside it, and the
    // inventory row stays since you make things from it, but the G prompt steps aside
    // so it never sits half under the panel.
    public static Func<bool> Side = () => false;

    // One centre window at a time (D-283): each registers how it closes, and opening one
    // closes the rest (bag, build menu, crafting, a station, the journal). Trade and the
    // look and mark pickers stay out of this: closing those would cancel or lose something.
    static readonly List<(object who, Action close)> centre = new();

    public static void Centre(object who, Action close) => centre.Add((who, close));

    public static void Opening(object who)
    {
        foreach (var (other, close) in centre)
            if (other != who) close();
    }
}
