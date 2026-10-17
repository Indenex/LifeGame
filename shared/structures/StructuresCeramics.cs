// Building kinds: Ceramics. The clay trough is kind 17, appended after TiledHouse.
// One StructureDef per kind: its fixed id (never renumbered or reused) and its StructureShape
// (footprint, rules, stage names and what each stage takes). Shared by the server module and the
// client; keep this file free of engine and SpacetimeDB types. List each kind in StructureKind
// (server/spacetimedb/Building.cs) with the same number (docs/CONTENT.md).
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public static partial class Structures
    {
        static IEnumerable<StructureDef> CeramicsDefs() => new[]
        {
            // Ceramics' clay trough (ceramics-13): a kiln-fired basin set down and filled by hand.
            new StructureDef
            {
                Kind = 17, // StructureKind.ClayTrough (the enum takes no explicit values, so this is its number)
                Shape = new StructureShape
                {
                    Name = "Clay trough",
                    Category = "Storage",
                    Trough = true,
                    Model = "Trough",
                    ClearExtra = 0.3f,
                    Round = true, Radius = 0.4f,
                    Clearance = 0.7f,
                    Solid = true,
                    Stages = new[] { "Set the trough" },
                    // Clay trough (Ceramics): set down one kiln-fired trough
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.ClayTrough, 1u) },
                    },
                },
            },
        };
    }
}
