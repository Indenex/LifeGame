using System;
using System.Linq;
using Godot;
using LifeGame.Shared;
using SpacetimeDB.Types;

namespace LifeGame;

// M2, "the first evening": one-time tips in the hint corner (Hints.Tip, once per
// install) at the moments a brand-new player most needs a nudge. It only watches
// what the client already has (your bag, fires, claims), never changes anything, and
// checks twice a second. Main sets Conn and MyId.
public partial class FirstEvening : Node
{
    public Func<DbConnection?> Conn = () => null;
    public Func<uint> MyId = () => 0;
    // 0 midnight, 0.25 sunrise, 0.5 noon, 0.75 sunset (Atmosphere); set by Main.
    public Func<float> TimeOfDay = () => 0.5f;

    const float Every = 0.5f;
    float wait = Every;
    // Tips already queued this run; Hints remembers the ones shown across runs.
    readonly System.Collections.Generic.HashSet<string> sent = new();

    public override void _Process(double delta)
    {
        if ((wait -= (float)delta) > 0f) return;
        wait = Every;
        if (Conn() is not { } c || MyId() is var me && me == 0) return;

        bool litOne = c.Db.Fire.Iter().Any(f => f.LitBy == me);

        // The first and clearest tip (Survival's first-evening test: not knowing how to
        // make fire is the real risk). It shows once, whichever comes first: you carry
        // sticks and fiber, or dusk falls and you have no fire.
        float t = TimeOfDay();
        bool dusk = t > 0.68f || t < 0.2f;
        bool makings = Carried(c, Item.Stick) >= 2 && (Carried(c, Item.Fiber) >= 2 || Carried(c, Item.Tinder) >= 1);
        if (!litOne && (makings || dusk))
            Tip("fire_how", (!makings && t > 0.68f && t < 0.8f ? "Night is coming. " : "")
                + $"A fire needs two sticks and some tinder. Plant fiber shreds into tinder. Make both with {Key("craft", "C")}.");

        // You carry what a fire needs, and haven't lit one yet.
        if (!litOne && Carried(c, Item.Stick) >= 2 && Carried(c, Item.Tinder) >= 1)
            Tip("fire_ready", $"You have what a fire needs. Press {Key("craft", "C")} and pick the hand-drill fire.");

        // Your first fire is burning.
        if (litOne)
            Tip("fire_fed", $"A fire burns down unless you feed it. Stand by it and press {Key("use", "E")} to add a stick.");

        // Your claim marker is up.
        if (c.Db.Claim.Iter().Any(k => k.OwnerPlayerId == me))
            Tip("claim_up", $"This land is yours now. What you build inside it stays yours. Press {Key("build_mode", "B")} to build.");
    }

    void Tip(string id, string text)
    {
        if (sent.Add(id)) Hints.Tip(id, text);
    }

    static string Key(string action, string fallback) => Controls.KeyFor(action) ?? fallback;

    static uint Carried(DbConnection c, Item item) =>
        c.Db.MyInventory.Iter().Where(i => i.Item == (ushort)item).Aggregate(0u, (n, i) => n + i.Count);
}
