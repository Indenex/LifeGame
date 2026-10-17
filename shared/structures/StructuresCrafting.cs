// Building kinds: Crafting stations (Crafting and Ceramics). New kinds: 200 and up.
// One StructureDef per kind: its id, the StructureKind enum ordinal (append-only, never renumbered or reused) and its StructureShape
// (footprint, rules, stage names and what each stage takes). Shared by the server module and the
// client; keep this file free of engine and SpacetimeDB types. To add a kind, add a def here and
// list it in StructureKind (server/spacetimedb/Building.cs) with the same number (docs/CONTENT.md).
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public static partial class Structures
    {
        static IEnumerable<StructureDef> CraftingDefs() => new[]
        {
                // A crafting table (Crafting, D-098): general crafts are made at it, inside a claim.
            new StructureDef
            {
                Kind = 9,
                Shape = new StructureShape
                {
                    Name = "Crafting table",
                    Scale = 1.25f,
                    Wild = true,
                    Category = "Crafting",
                    HalfWidth = 0.8f, HalfDepth = 0.45f,
                    Clearance = 1.0f,
                    Station = StationKind.Table,
                    Stages = new[] { "Table" },
                    // Crafting table (D-112): Table
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.Stick, 4u), (Item.Cordage, 2u), (Item.Log, 1u) },
                    },
                },
            },
                // Ceramics' potter's bench (ceramics-8, D-112): pots are shaped at it, inside a claim.
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
                    // Potter's bench (Ceramics, D-127): a split log on two stones
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.Log, 1u), (Item.Stone, 2u) },
                    },
                },
            },
                // A repair table (Crafting, D-155): worn tools are mended here, inside a claim.
            new StructureDef
            {
                Kind = 15,
                Shape = new StructureShape
                {
                    Name = "Repair table",
                    Scale = 1.25f,
                    Category = "Crafting",
                    HalfWidth = 0.8f, HalfDepth = 0.45f,
                    Clearance = 1.0f,
                    Station = StationKind.Repair,
                    Stages = new[] { "Table" },
                    // Repair table (D-155): Table, the crafting table's parts plus a pair of stones
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.Stick, 4u), (Item.Cordage, 2u), (Item.Log, 1u), (Item.Stone, 2u) },
                    },
                },
            },
                // Batch devices (crafting-23, D-195): load one and it works through a batch on its own.
            new StructureDef
            {
                Kind = 21,
                Shape = new StructureShape
                {
                    Name = "Settling tub",
                    Category = "Crafting",
                    Round = true, Radius = 0.55f,
                    Clearance = 1.0f,
                    Solid = true,
                    Station = StationKind.ClayTub,
                    Stages = new[] { "Tub" },
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.Stick, 6u), (Item.Cordage, 2u), (Item.Log, 1u) },
                    },
                },
            },
            new StructureDef
            {
                Kind = 22,
                Shape = new StructureShape
                {
                    Name = "Crushing block",
                    Category = "Crafting",
                    HalfWidth = 0.45f, HalfDepth = 0.45f,
                    Clearance = 0.9f,
                    Station = StationKind.CrushBlock,
                    Stages = new[] { "Block" },
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.Log, 1u), (Item.Stone, 3u) },
                    },
                },
            },
            new StructureDef
            {
                Kind = 23,
                Shape = new StructureShape
                {
                    Name = "Tempering trough",
                    Category = "Crafting",
                    HalfWidth = 0.85f, HalfDepth = 0.4f,
                    Clearance = 1.0f,
                    Station = StationKind.TemperTrough,
                    Stages = new[] { "Trough" },
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.Stick, 4u), (Item.Cordage, 2u), (Item.Log, 1u) },
                    },
                },
            },
        };
    }
}
