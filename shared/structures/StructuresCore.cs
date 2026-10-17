// Building kinds, shapes and stage costs: The first shelters, stores and fences (ids 0-8 except the claim marker, brick house).
// One StructureDef per kind: its fixed id (never renumbered or reused), its footprint and rules,
// and what each stage takes. Shared by the server module and the client; keep this file free of
// engine and SpacetimeDB types. To add a kind, add a def here and list it in StructureKind
// (server/spacetimedb/Building.cs) with the same number (docs/CONTENT.md).
using System.Collections.Generic;

namespace LifeGame.Shared
{
    public static partial class Structures
    {
        static IEnumerable<StructureDef> CoreDefs() => new[]
        {
                // Lean-to: Frame, Lashing, Ribs, Thatch
            new StructureDef
            {
                Kind = 0,
                Shape = new StructureShape
                {
                    Name = "Lean-to",
                    Scale = 1.25f,
                    HalfWidth = 1.5f, HalfDepth = 1.25f,
                    Clearance = 1.9f,
                    RainCover = 0.6f, Warmth = 0.2f,
                    Stages = new[] { "Frame", "Lashing", "Ribs", "Thatch" },
                },
                Stages = new[]
                {
                    new[] { (Item.Stick, 4u) },
                    new[] { (Item.Fiber, 3u) },
                    new[] { (Item.Stick, 6u) },
                    new[] { (Item.Fiber, 8u) },
                },
            },
                // Clay hut (wattle and daub): Posts, Wattle, Daub, Roof
            new StructureDef
            {
                Kind = 1,
                Shape = new StructureShape
                {
                    Name = "Clay hut",
                    Scale = 1.25f,
                    Round = true, Radius = 1.9f,
                    Clearance = 2.2f,
                    RainCover = 0.9f, Warmth = 0.4f, ShutWarmth = 0.5f,
                    DoorwayZ = 1.7f,
                    SmokeY = 2.9f,
                    Stages = new[] { "Posts", "Wattle", "Daub", "Roof" },
                },
                Stages = new[]
                {
                    new[] { (Item.Stick, 6u), (Item.Stone, 2u) },
                    new[] { (Item.Stick, 10u), (Item.Fiber, 4u) },
                    new[] { (Item.Daub, 12u) },
                    new[] { (Item.Stick, 6u), (Item.Fiber, 10u) },
                },
            },
                // Updraft kiln (Ceramics): Base, Dome
            new StructureDef
            {
                Kind = 3,
                Shape = new StructureShape
                {
                    // Ceramics' updraft kiln: pots are loaded and fired in it (server/spacetimedb/Kiln.cs).
                    Kiln = true,
                    Name = "Updraft kiln",
                    Category = "Crafting",
                    Round = true, Radius = 0.85f,
                    Clearance = 1.2f,
                    Solid = true,
                    Stages = new[] { "Base", "Dome" },
                },
                Stages = new[]
                {
                    new[] { (Item.Brick, 4u) },
                    new[] { (Item.Brick, 4u), (Item.Daub, 2u) },
                },
            },
                // Storage basket: Weave
            new StructureDef
            {
                Kind = 4,
                Shape = new StructureShape
                {
                    Name = "Storage basket",
                    Category = "Storage",
                    Round = true, Radius = 0.35f,
                    Clearance = 0.6f,
                    Solid = true,
                    StoreStacks = BasketStacks,
                    Stages = new[] { "Weave" },
                },
                Stages = new[]
                {
                    new[] { (Item.Fiber, 6u), (Item.Cordage, 2u) },
                },
            },
                // Ceramics' storage jar (ceramics-6): a kiln-fired jar set down as a store.
                // Outside a claim it weathers like everything else.
                // Storage jar (Ceramics): set down one kiln-fired jar
            new StructureDef
            {
                Kind = 6,
                Shape = new StructureShape
                {
                    Name = "Storage jar",
                    Category = "Storage",
                    Jar = true,
                    Round = true, Radius = 0.32f,
                    Clearance = 0.6f,
                    Solid = true,
                    // The most a jar can hold; a jar's own quality sets its size (StoreStacksFor).
                    StoreStacks = JarStacksMax,
                    Stages = new[] { "Set the jar" },
                },
                Stages = new[]
                {
                    new[] { (Item.StorageJar, 1u) },
                },
            },
                // A stick fence: two posts and two lashed rails, 2 m long. Runs of them pen
                // sheep and mark out a yard (building-8).
                // Fence: Posts, Rails
            new StructureDef
            {
                Kind = 7,
                Shape = new StructureShape
                {
                    Name = "Fence",
                    Category = "Fences and gates",
                    HalfWidth = FenceHalfLength, HalfDepth = 0.15f,
                    Clearance = 0.25f,
                    Fence = true,
                    Stages = new[] { "Posts", "Rails" },
                },
                Stages = new[]
                {
                    new[] { (Item.Stick, 2u) },
                    new[] { (Item.Stick, 2u), (Item.Fiber, 1u) },
                },
            },
                // A fence length with a woven hurdle gate in it that opens and bars like a door.
                // Gate: Posts, Gate (a hurdle like a hut door)
            new StructureDef
            {
                Kind = 8,
                Shape = new StructureShape
                {
                    Name = "Gate",
                    Category = "Fences and gates",
                    HalfWidth = FenceHalfLength, HalfDepth = 0.15f,
                    Clearance = 0.25f,
                    Fence = true, Gate = true,
                    Stages = new[] { "Posts", "Gate" },
                },
                Stages = new[]
                {
                    new[] { (Item.Stick, 2u) },
                    new[] { (Item.Stick, 3u), (Item.Fiber, 2u) },
                },
            },
        };
    }
}
