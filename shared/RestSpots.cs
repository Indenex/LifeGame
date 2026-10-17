// Rest spots (survival-23, D-206): the things you can sleep on, as data. Setting one as
// your waking place is the player's choice (Justin: sleeping mats and beds "should offer to
// set your respawn point"). A new bed, hammock or bedroll is one entry here, not new code.
// Shared by the server module and the Godot client.
using System.Collections.Generic;
using System.Linq;

namespace LifeGame.Shared
{
    // What a rest spot is: a laid-out item (the sleeping mat, which House building keeps in its own
    // table) or a built structure of a kind (a future bed). Exactly one of Item and StructureKind is set.
    public sealed class RestSpotDef
    {
        // Stable number stored with a player's waking place. Never renumber.
        public ushort Id;
        public string Name = "";
        public Item? Item;
        public int StructureKind = -1;
    }

    public static class RestSpots
    {
        public static readonly IReadOnlyList<RestSpotDef> All = new[]
        {
            new RestSpotDef { Id = 1, Name = "sleeping mat", Item = Shared.Item.SleepingMat },
        };

        static readonly Dictionary<ushort, RestSpotDef> ById = All.ToDictionary(r => r.Id);

        public static RestSpotDef? Find(ushort id) => ById.TryGetValue(id, out var r) ? r : null;

        public static RestSpotDef? OfItem(Item item) => All.FirstOrDefault(r => r.Item == item);

        public static RestSpotDef? OfStructure(int kind) => All.FirstOrDefault(r => r.StructureKind == kind);

        // How a player woke after dying (Vitals.WakeKind): the wilderness edge, on their waking
        // place, or at the edge because their waking place was gone.
        public const byte WokeAtEdge = 0, WokeAtRestSpot = 1, WokeBedGone = 2;
    }
}
