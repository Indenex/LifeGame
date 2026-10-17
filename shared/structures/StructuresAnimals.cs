// Building kinds, shapes and stage costs: Animals stations. New kinds: 300 and up.
// One StructureDef per kind: its fixed id (never renumbered or reused), its footprint and rules,
// and what each stage takes. Shared by the server module and the client; keep this file free of
// engine and SpacetimeDB types. To add a kind, add a def here and list it in StructureKind
// (server/spacetimedb/Building.cs) with the same number (docs/CONTENT.md).
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public static partial class Structures
    {
        static IEnumerable<StructureDef> AnimalsDefs() => new[]
        {
                // Spindle station (Animals, D-098): a distaff stand with a stool and a weaving frame.
                // Spindle station: your drop spindle becomes part of it (D-137)
            new StructureDef
            {
                Kind = 11,
                Shape = new StructureShape
                {
                    Name = "Spindle station",
                    Category = "Crafting",
                    HalfWidth = 0.6f, HalfDepth = 0.4f,
                    Clearance = 1.0f,
                    Station = StationKind.Spindle,
                    Stages = new[] { "Distaff stand" },
                },
                Stages = new[]
                {
                    new[] { (Item.DropSpindle, 1u), (Item.Stick, 4u), (Item.Cordage, 2u), (Item.Stone, 1u) },
                },
            },
                // Leather drying rack (Animals, D-098): forked posts and a bar for up to three hides.
                // Leather drying rack
            new StructureDef
            {
                Kind = 12,
                Shape = new StructureShape
                {
                    Name = "Leather drying rack",
                    Category = "Crafting",
                    HalfWidth = 1.2f, HalfDepth = 0.35f,
                    Clearance = 1.0f,
                    Station = StationKind.DryingRack,
                    Stages = new[] { "Posts and bar" },
                },
                Stages = new[]
                {
                    new[] { (Item.Stick, 6u), (Item.Cordage, 2u) },
                },
            },
        };
    }
}
