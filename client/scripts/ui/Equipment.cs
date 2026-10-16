using System;
using System.Collections.Generic;
using System.Linq;
using LifeGame.Shared;

namespace LifeGame;

// Where a piece of clothing goes on the body (D-074). Body and feet to start; more
// slots (head, hands, back) can follow without changing the panel's shape much.
public enum WearSlot : byte { Body = 0, Feet = 1 }

// A piece of clothing, worn or carried. Effect is a short plain line for the panel
// ("A little warmer"); numbers stay off it (D-075 keeps them small and they change).
public record Wearable(Item Item, byte Quality, string Name, WearSlot Slot, string Effect);

// What the equipment panel talks to. The server side is animals-9 (Animals lane, which
// sends the worn-slots contract); until it lands, MockEquipLink stands in, and the real
// link implements this over the generated bindings.
public interface IEquipLink
{
    // What you wear in a slot, or null.
    Wearable? Worn(WearSlot slot);
    // What you carry that could go in a slot.
    IEnumerable<Wearable> Carried(WearSlot slot);
    void Wear(Wearable piece);
    void TakeOff(WearSlot slot);
    // A server refusal ("Your hands are full") as one line for the HUD.
    event Action<string>? Said;
}

// Pretend clothes for screenshots and for building the panel before the server lands.
// Leather and wool stand in for the real items' pictures until Animals adds them.
public sealed class MockEquipLink : IEquipLink
{
    readonly Dictionary<WearSlot, Wearable> worn = new();
    readonly List<Wearable> carried = new()
    {
        new(Item.Leather, 55, "Leather wrap", WearSlot.Body, "A little warmer"),
        new(Item.Wool, 40, "Wool tunic", WearSlot.Body, "A little warmer, and dries faster"),
        new(Item.SheepHide, 50, "Hide shoes", WearSlot.Feet, "Warmer feet; wading chills you less"),
    };

    public event Action<string>? Said;

    public Wearable? Worn(WearSlot slot) => worn.GetValueOrDefault(slot);
    public IEnumerable<Wearable> Carried(WearSlot slot) => carried.Where(w => w.Slot == slot);

    public void Wear(Wearable piece)
    {
        if (!carried.Remove(piece)) return;
        if (worn.Remove(piece.Slot, out var old)) carried.Add(old);
        worn[piece.Slot] = piece;
        Said?.Invoke($"You put on the {piece.Name.ToLowerInvariant()}");
    }

    public void TakeOff(WearSlot slot)
    {
        if (!worn.Remove(slot, out var old)) return;
        carried.Add(old);
        Said?.Invoke($"You take off the {old.Name.ToLowerInvariant()}");
    }
}
