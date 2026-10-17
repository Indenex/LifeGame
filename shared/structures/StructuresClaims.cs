// Building kinds: Claims (claims lane). New kinds: 400 and up.
// One StructureDef per kind: its fixed id (never renumbered or reused) and its StructureShape
// (footprint, rules, stage names and what each stage takes). Shared by the server module and the
// client; keep this file free of engine and SpacetimeDB types. To add a kind, add a def here and
// list it in StructureKind (server/spacetimedb/Building.cs) with the same number (docs/CONTENT.md).
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public static partial class Structures
    {
        static IEnumerable<StructureDef> ClaimsDefs() => new[]
        {
                // A land claim's marker post (Claims.cs makes the claim when it is finished).
            new StructureDef
            {
                Kind = 2,
                Shape = new StructureShape
                {
                    Name = "Claim marker",
                    Category = "Claims",
                    Round = true, Radius = 0.5f,
                    Clearance = 1.2f,
                    Solid = true,
                    Stages = new[] { "Marker post" },
                    // Claim marker: one stage, costs set by Claims in shared/Social.cs
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.Stick, Social.MarkerSticks), (Item.Fiber, Social.MarkerFiber) },
                    },
                },
            },
        };
    }
}
