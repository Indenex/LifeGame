using System;

namespace LifeGame;

// Whether a full panel (journal, claim panel, Esc menu) covers the world right now.
// While it does, world HUD such as hints, the G prompt, the inventory row, the land
// chip and the needs rings fade away, so nothing shows through or collides with it.
// Main sets Open; anything on the HUD reads it.
public static class Panels
{
    public static Func<bool> Open = () => false;
}
