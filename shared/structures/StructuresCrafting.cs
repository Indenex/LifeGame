// Building kinds, shapes and stage costs: Crafting stations (Crafting and Ceramics). New kinds: 200 and up.
// One StructureDef per kind: its fixed id (never renumbered or reused), its footprint and rules,
// and what each stage takes. Shared by the server module and the client; keep this file free of
// engine and SpacetimeDB types. To add a kind, add a def here and list it in StructureKind
// (server/spacetimedb/Building.cs) with the same number (docs/CONTENT.md).
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public static partial class Structures
    {
        static IEnumerable<StructureDef> CraftingDefs() => new[]
        {
                // A crafting table (Crafting, D-098): general crafts are made at it, inside a claim.
                // Crafting table (D-112): Table
            new StructureDef
            {
                Kind = 9,
                Shape = new StructureShape
                {
                    Name = "Crafting table",
                    Category = "Crafting",
                    HalfWidth = 0.8f, HalfDepth = 0.45f,
                    Clearance = 1.0f,
                    Station = StationKind.Table,
                    Stages = new[] { "Table" },
                },
                Stages = new[]
                {
                    new[] { (Item.Stick, 4u), (Item.Cordage, 2u), (Item.Log, 1u) },
                },
            },
                // Ceramics' potter's bench (ceramics-8, D-112): pots are shaped at it, inside a claim.
                // Potter's bench (Ceramics, D-127): a split log on two stones
            new StructureDef
            {
                Kind = 13,
                Shape = new StructureShape
                {
                    Name = "Potter's bench",
                    Category = "Crafting",
                    // Modeling's bench (wedging stone, stool, pots) is about 1.3 x 1.0 m, +Z where the potter sits.
                    HalfWidth = 0.65f, HalfDepth = 0.5f,
                    Clearance = 1.0f,
                    Station = StationKind.PottersBench,
                    Stages = new[] { "Bench" },
                },
                Stages = new[]
                {
                    new[] { (Item.Log, 1u), (Item.Stone, 2u) },
                },
            },
                // A repair table (Crafting, D-155): worn tools are mended here, inside a claim.
                // Repair table (D-155): Table, the crafting table's parts plus a pair of stones
            new StructureDef
            {
                Kind = 15,
                Shape = new StructureShape
                {
                    Name = "Repair table",
                    Category = "Crafting",
                    HalfWidth = 0.8f, HalfDepth = 0.45f,
                    Clearance = 1.0f,
                    Station = StationKind.Repair,
                    Stages = new[] { "Table" },
                },
                Stages = new[]
                {
                    new[] { (Item.Stick, 4u), (Item.Cordage, 2u), (Item.Log, 1u), (Item.Stone, 2u) },
                },
            },
        };
    }
}
