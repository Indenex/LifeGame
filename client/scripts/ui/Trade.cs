using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// One stack in a trade offer, or one you carry: what it is, how good, how many.
public record TradeStack(Item Item, byte Quality, uint Count);

// A trade in progress as the window shows it (D-069). Version goes up whenever either
// offer changes; the server clears both accepts at the same moment.
public sealed record TradeView(
    uint PartnerId,
    string Partner,
    IReadOnlyList<TradeStack> Mine,
    IReadOnlyList<TradeStack> Theirs,
    bool MineAccepted,
    bool TheirsAccepted,
    int Version);

// What the trade window talks to. The server side is claims-7 (Claims lane); until its
// contract lands, MockTradeLink stands in, and the real link implements this over the
// generated bindings.
public interface ITradeLink
{
    // The open trade, or null.
    TradeView? Current { get; }
    // Someone nearby asked you to trade and waits for an answer.
    (uint id, string name)? Incoming { get; }
    // You asked someone and wait for their answer.
    (uint id, string name)? Outgoing { get; }
    // Everything you carry, offered or not; the window takes the offer away itself.
    IEnumerable<TradeStack> Carried();

    void Ask(uint playerId, string name);
    void Answer(bool yes);
    void Offer(TradeStack stack);
    void Withdraw(TradeStack stack);
    void Accept(bool on);
    void Cancel();

    // A finished trade, a refusal or a cancel, as one line for the HUD.
    event Action<string>? Said;
}

// A pretend partner for screenshots and for building the window before the server
// lands. Mara offers berries and a hand axe, and accepts a moment after your offer
// stops changing; when both accept, the items swap.
public sealed class MockTradeLink : ITradeLink
{
    readonly List<TradeStack> carried = new()
    {
        new(Item.Stick, 0, 8),
        new(Item.Stone, 0, 3),
        new(Item.Fiber, 0, 12),
        new(Item.Flake, 60, 2),
        new(Item.Clay, 0, 4),
    };
    readonly List<TradeStack> mine = new(), theirs = new();
    bool mineAccepted, theirsAccepted, open;
    int version;
    double theirAcceptAt = double.MaxValue;

    public TradeView? Current => open
        ? new TradeView(7, "Mara", mine.ToList(), theirs.ToList(), mineAccepted, theirsAccepted, version)
        : null;
    public (uint id, string name)? Incoming { get; private set; }
    public (uint id, string name)? Outgoing { get; private set; }
    public event Action<string>? Said;

    public IEnumerable<TradeStack> Carried() => carried;

    // Opens straight into a trade, as if Mara had asked and you said yes.
    public void OpenNow()
    {
        open = true;
        theirs.Clear();
        theirs.Add(new(Item.Berries, 0, 5));
        theirs.Add(new(Item.HandAxe, 72, 1));
        Changed();
    }

    // Mara asks you, as if she had pressed G on you.
    public void AskMe() => Incoming = (7, "Mara");

    public void Ask(uint playerId, string name)
    {
        Outgoing = (playerId, name);
        Said?.Invoke($"You ask {name} to trade");
    }

    public void Answer(bool yes)
    {
        if (Incoming is not { } who) return;
        Incoming = null;
        if (yes) OpenNow();
        else Said?.Invoke($"You turn down {who.name}");
    }

    // You can offer up to what you carry; offered things stay in your hands until the swap.
    public void Offer(TradeStack stack)
    {
        var have = carried.FirstOrDefault(s => Same(s, stack));
        uint already = mine.FirstOrDefault(s => Same(s, stack))?.Count ?? 0;
        if (have == null || already >= have.Count) return;
        Add(mine, stack with { Count = Math.Min(stack.Count, have.Count - already) });
        Changed();
    }

    public void Withdraw(TradeStack stack)
    {
        Take(mine, stack);
        Changed();
    }

    public void Accept(bool on)
    {
        mineAccepted = on;
        if (mineAccepted && theirsAccepted) Finish();
    }

    public void Cancel()
    {
        if (!open && Outgoing == null) return;
        mine.Clear();
        theirs.Clear();
        open = false;
        Outgoing = null;
        Said?.Invoke("The trade is off");
    }

    // Called each frame by the window so Mara can make up her mind.
    public void Tick(double now)
    {
        if (open && !theirsAccepted && now >= theirAcceptAt)
        {
            theirsAccepted = true;
            if (mineAccepted) Finish();
        }
    }

    static bool Same(TradeStack a, TradeStack b) => a.Item == b.Item && a.Quality == b.Quality;

    static void Add(List<TradeStack> list, TradeStack stack)
    {
        int i = list.FindIndex(s => Same(s, stack));
        if (i >= 0) list[i] = list[i] with { Count = list[i].Count + stack.Count };
        else list.Add(stack);
    }

    static void Take(List<TradeStack> list, TradeStack stack)
    {
        int i = list.FindIndex(s => Same(s, stack));
        if (i < 0) return;
        if (list[i].Count > stack.Count) list[i] = list[i] with { Count = list[i].Count - stack.Count };
        else list.RemoveAt(i);
    }

    void Changed()
    {
        version++;
        mineAccepted = theirsAccepted = false;
        theirAcceptAt = Time.GetTicksMsec() / 1000.0 + 2.5;
    }

    void Finish()
    {
        foreach (var s in mine) Take(carried, s);
        foreach (var s in theirs) Add(carried, s);
        mine.Clear();
        theirs.Clear();
        open = false;
        Said?.Invoke("You trade with Mara");
    }
}
