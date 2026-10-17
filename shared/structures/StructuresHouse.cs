// Building kinds: House building. New kinds: 100 and up.
// One StructureDef per kind: its fixed id (never renumbered or reused) and its StructureShape
// (footprint, rules, stage names and what each stage takes). Shared by the server module and the
// client; keep this file free of engine and SpacetimeDB types. To add a kind, add a def here and
// list it in StructureKind (server/spacetimedb/Building.cs) with the same number (docs/CONTENT.md).
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public static partial class Structures
    {
        static IEnumerable<StructureDef> HouseDefs() => new[]
        {
                // A mud-brick house: a bigger room with a hearth. A fire lit inside warms
                // the whole room, and it weathers slower than wattle and daub.
            new StructureDef
            {
                Kind = 5,
                Shape = new StructureShape
                {
                    Name = "Mud-brick house",
                    Scale = 1.25f,
                    HalfWidth = 2.4f, HalfDepth = 2.0f,
                    Clearance = 3.2f,
                    RainCover = 1f, Warmth = 0.5f, ShutWarmth = 0.55f,
                    DoorwayZ = 1.8f,
                    Room = true,
                    BrickWalls = true,
                    SmokeY = 3.3f,
                    Stages = new[] { "Footings", "Brick walls", "Rafters", "Thatch" },
                    // Brick house: Footings, Brick walls, Rafters, Thatch
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.Stone, 6u), (Item.Stick, 4u) },
                        new[] { (Item.Brick, 16u), (Item.Daub, 6u) },
                        new[] { (Item.Stick, 12u), (Item.Cordage, 2u) },
                        new[] { (Item.Fiber, 14u) },
                    },
                },
            },
                // A raised hearth inside a hut or house: a ring of stones with a clay surround.
                // A fire on it burns longer per stick and warms a whole hut (building-9, D-132).
            new StructureDef
            {
                Kind = 10,
                Shape = new StructureShape
                {
                    Name = "Raised hearth",
                    Wild = true,
                    Category = "Fire",
                    Round = true, Radius = HearthRadius, Solid = true,
                    Clearance = 0.6f,
                    Indoor = true,
                    Stages = new[] { "Stone ring", "Clay surround" },
                    // Raised hearth: Stone ring, Clay surround
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.Stone, 4u) },
                        new[] { (Item.Daub, 3u) },
                    },
                },
            },
                // A log cabin (building-10, D-151): the forest's way to the same home as the
                // mud-brick house, with the same room, shelter and weathering, built of logs.
            new StructureDef
            {
                Kind = 14,
                Shape = new StructureShape
                {
                    Name = "Log cabin",
                    Scale = 1.25f,
                    HalfWidth = 2.4f, HalfDepth = 2.0f,
                    Clearance = 3.2f,
                    RainCover = 1f, Warmth = 0.5f, ShutWarmth = 0.55f,
                    DoorwayZ = 1.8f,
                    Room = true,
                    SmokeY = 3.3f,
                    Stages = new[] { "Sill", "Log walls", "Rafters", "Thatch" },
                    // Log cabin: Sill, Log walls, Rafters, Thatch (D-151)
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.Stone, 4u), (Item.Log, 4u) },
                        new[] { (Item.Log, 16u) },
                        new[] { (Item.Stick, 12u), (Item.Cordage, 2u) },
                        new[] { (Item.Fiber, 14u) },
                    },
                },
            },
                // A tiled house (ceramics-10, Justin): the mud-brick house with a roof of fired clay tiles
                // instead of thatch. Same room, door and shelter; claim-only like the brick house.
            new StructureDef
            {
                Kind = 16,
                Shape = new StructureShape
                {
                    Name = "Tiled house",
                    Category = "Shelter",
                    Scale = 1.25f,
                    HalfWidth = 2.4f, HalfDepth = 2.0f,
                    Clearance = 3.2f,
                    RainCover = 1f, Warmth = 0.5f, ShutWarmth = 0.55f,
                    DoorwayZ = 1.8f,
                    Room = true,
                    BrickWalls = true,
                    TileRoof = true,
                    SmokeY = 3.3f,
                    Stages = new[] { "Footings", "Brick walls", "Rafters", "Roof tiles" },
                    Costs = new (Item Item, uint Count)[][]
                    {
                        new[] { (Item.Stone, 6u), (Item.Stick, 4u) },
                        new[] { (Item.Brick, 16u), (Item.Daub, 6u) },
                        new[] { (Item.Stick, 12u), (Item.Cordage, 2u) },
                        new[] { (Item.RoofTile, 12u) },
                    },
                },
            },
        };
    }
}
