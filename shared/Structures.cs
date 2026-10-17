// Shapes and rules for buildings, shared by the server module and the client so
// placement checks, "am I inside?" and what each stage costs agree on both sides.
// Keep this file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    public sealed class StructureShape
    {
        public string Name = "";
        // May stand outside a claim (building-13, Justin): basic survival only. Everything else has
        // to be built inside a claim the builder belongs to. Outside a claim, everything weathers fast.
        public bool Wild;
        // Building size relative to the design size the model and the numbers below are written in
        // (building-14, Justin: houses felt too small). Applied once when the shapes are made, so
        // every footprint, clearance and doorway number is already the real, scaled size; the model
        // is drawn at the design size and scaled by this at the view. 1 for everything but homes.
        public float Scale = 1f;
        // Where the build menu files it: Shelter, Storage, Crafting, Fire, "Fences and gates" or Claims (ui-20).
        public string Category = "Shelter";
        // Footprint in the structure's own frame: +Z is the open front or door.
        public bool Round;
        public float HalfWidth, HalfDepth, Radius;
        // How far apart two structures' centers must be, as the sum of both clearances.
        public float Clearance;
        // Shelter given to someone standing inside once it is finished, 0 to 1 (D-116).
        // Warmth is the share of the cold it keeps off; ShutWarmth applies with the door shut.
        public float RainCover, Warmth, ShutWarmth;
        // Round shapes only: players can't walk into the footprint once the first stage is done.
        public bool Solid;
        // Where a hung door goes: the doorway's center on the local +Z axis, 0 when it has none.
        public float DoorwayZ;
        // Where smoke from a fire inside leaves the building: metres above the ground at the
        // centre, 0 for none (Atmosphere draws the plume; atmosphere-19, D-206).
        public float SmokeY;
        // Hours untouched before it starts to weather (D-046); 0 uses WeatherAfterHours.
        public double WeatherAfter;
        // Stacks it stores when finished, 0 for none. Any kind with stacks opens like the basket (BuildingStorage.cs).
        public int StoreStacks;
        // A straight run along the local X axis (FenceHalfLength each side) that people
        // and animals can't step across. A gate is a fence whose middle opens like a door.
        public bool Fence, Gate;
        // Built inside a finished hut or house (a raised hearth), so it sits within their clearance.
        public bool Indoor;
        // Ceramics' updraft kiln and storage jar: found by shape, never by enum number (docs/CONTENT.md).
        public bool Kiln, Jar;
        // A crafting station of this kind once finished (D-098); None for anything else.
        public StationKind Station;
        // A rectangular room with four walls (HouseWallX/Z) and its doorway on +Z: the
        // mud-brick house and the log cabin share the walls, door and fire-warms-the-room rule.
        public bool Room;
        // Sun-dried brick walls (a Daub-work stage) and, if TileRoof, a roof of fired clay tiles, not thatch.
        public bool BrickWalls, TileRoof;
        public string[] Stages = Array.Empty<string>();
        // What each stage takes, in the same order as Stages (the server builds each stage's needs from
        // this; the client shows it before you lay out). Fiber stands in for thatch; the hut's walls are daub.
        public (Item Item, uint Count)[][] Costs = Array.Empty<(Item Item, uint Count)[]>();
    }

    public static class Structures
    {
        // Indexed by StructureKind; append new kinds at the end, in the same order.
        public static readonly StructureShape[] Shapes = Scaled(new StructureShape[]
        {
            new StructureShape
            {
                Name = "Lean-to",
                Wild = true,
                Scale = 1.25f,
                HalfWidth = 1.5f, HalfDepth = 1.25f,
                Clearance = 1.9f,
                RainCover = 0.6f, Warmth = 0.2f,
                Stages = new[] { "Frame", "Lashing", "Ribs", "Thatch" },
                // Lean-to: Frame, Lashing, Ribs, Thatch
                Costs = new (Item Item, uint Count)[][]
                {
                    new[] { (Item.Stick, 4u) },
                    new[] { (Item.Fiber, 3u) },
                    new[] { (Item.Stick, 6u) },
                    new[] { (Item.Fiber, 8u) },
                },
            },
            new StructureShape
            {
                Name = "Clay hut",
                Wild = true,
                Scale = 1.25f,
                Round = true, Radius = 1.9f,
                Clearance = 2.2f,
                RainCover = 0.9f, Warmth = 0.4f, ShutWarmth = 0.5f,
                DoorwayZ = 1.7f,
                SmokeY = 2.9f,
                Stages = new[] { "Posts", "Wattle", "Daub", "Roof" },
                // Clay hut (wattle and daub): Posts, Wattle, Daub, Roof
                Costs = new (Item Item, uint Count)[][]
                {
                    new[] { (Item.Stick, 6u), (Item.Stone, 2u) },
                    new[] { (Item.Stick, 10u), (Item.Fiber, 4u) },
                    new[] { (Item.Daub, 12u) },
                    new[] { (Item.Stick, 6u), (Item.Fiber, 10u) },
                },
            },
            // A land claim's marker post (Claims.cs makes the claim when it is finished).
            new StructureShape
            {
                Name = "Claim marker",
                Wild = true,
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
            new StructureShape
            {
                // Ceramics' updraft kiln: pots are loaded and fired in it (server/spacetimedb/Kiln.cs).
                Kiln = true,
                Name = "Updraft kiln",
                Category = "Crafting",
                Round = true, Radius = 0.85f,
                Clearance = 1.2f,
                Solid = true,
                Stages = new[] { "Base", "Dome" },
                // Updraft kiln (Ceramics): Base, Dome
                Costs = new (Item Item, uint Count)[][]
                {
                    new[] { (Item.Brick, 4u) },
                    new[] { (Item.Brick, 4u), (Item.Daub, 2u) },
                },
            },
            new StructureShape
            {
                Name = "Storage basket",
                Wild = true,
                Category = "Storage",
                Round = true, Radius = 0.35f,
                Clearance = 0.6f,
                Solid = true,
                StoreStacks = BasketStacks,
                Stages = new[] { "Weave" },
                // Storage basket: Weave
                Costs = new (Item Item, uint Count)[][]
                {
                    new[] { (Item.Fiber, 6u), (Item.Cordage, 2u) },
                },
            },
            // A mud-brick house: a bigger room with a hearth. A fire lit inside warms
            // the whole room, and it weathers slower than wattle and daub.
            new StructureShape
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
            // Ceramics' storage jar (ceramics-6): a kiln-fired jar set down as a store.
            // Outside a claim it weathers like everything else.
            new StructureShape
            {
                Name = "Storage jar",
                Wild = true,
                Category = "Storage",
                Jar = true,
                Round = true, Radius = 0.32f,
                Clearance = 0.6f,
                Solid = true,
                // The most a jar can hold; a jar's own quality sets its size (StoreStacksFor).
                StoreStacks = JarStacksMax,
                Stages = new[] { "Set the jar" },
                // Storage jar (Ceramics): set down one kiln-fired jar
                Costs = new (Item Item, uint Count)[][]
                {
                    new[] { (Item.StorageJar, 1u) },
                },
            },
            // A stick fence: two posts and two lashed rails, 2 m long. Runs of them pen
            // sheep and mark out a yard (building-8).
            new StructureShape
            {
                Name = "Fence",
                Category = "Fences and gates",
                HalfWidth = FenceHalfLength, HalfDepth = 0.15f,
                Clearance = 0.25f,
                Fence = true,
                Stages = new[] { "Posts", "Rails" },
                // Fence: Posts, Rails
                Costs = new (Item Item, uint Count)[][]
                {
                    new[] { (Item.Stick, 2u) },
                    new[] { (Item.Stick, 2u), (Item.Fiber, 1u) },
                },
            },
            // A fence length with a woven hurdle gate in it that opens and bars like a door.
            new StructureShape
            {
                Name = "Gate",
                Category = "Fences and gates",
                HalfWidth = FenceHalfLength, HalfDepth = 0.15f,
                Clearance = 0.25f,
                Fence = true, Gate = true,
                Stages = new[] { "Posts", "Gate" },
                // Gate: Posts, Gate (a hurdle like a hut door)
                Costs = new (Item Item, uint Count)[][]
                {
                    new[] { (Item.Stick, 2u) },
                    new[] { (Item.Stick, 3u), (Item.Fiber, 2u) },
                },
            },
            // A crafting table (Crafting, D-098): general crafts are made at it, inside a claim.
            new StructureShape
            {
                Name = "Crafting table",
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
            // A raised hearth inside a hut or house: a ring of stones with a clay surround.
            // A fire on it burns longer per stick and warms a whole hut (building-9, D-132).
            new StructureShape
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
            // Spindle station (Animals, D-098): a distaff stand with a stool and a weaving frame.
            new StructureShape
            {
                Name = "Spindle station",
                Category = "Crafting",
                HalfWidth = 0.6f, HalfDepth = 0.4f,
                Clearance = 1.0f,
                Station = StationKind.Spindle,
                Stages = new[] { "Distaff stand" },
                // Spindle station: your drop spindle becomes part of it (D-137)
                Costs = new (Item Item, uint Count)[][]
                {
                    new[] { (Item.DropSpindle, 1u), (Item.Stick, 4u), (Item.Cordage, 2u), (Item.Stone, 1u) },
                },
            },
            // Leather drying rack (Animals, D-098): forked posts and a bar for up to three hides.
            new StructureShape
            {
                Name = "Leather drying rack",
                Category = "Crafting",
                HalfWidth = 1.2f, HalfDepth = 0.35f,
                Clearance = 1.0f,
                Station = StationKind.DryingRack,
                Stages = new[] { "Posts and bar" },
                // Leather drying rack
                Costs = new (Item Item, uint Count)[][]
                {
                    new[] { (Item.Stick, 6u), (Item.Cordage, 2u) },
                },
            },
            // Ceramics' potter's bench (ceramics-8, D-112): pots are shaped at it, inside a claim.
            new StructureShape
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
            // A log cabin (building-10, D-151): the forest's way to the same home as the
            // mud-brick house, with the same room, shelter and weathering, built of logs.
            new StructureShape
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
            // A repair table (Crafting, D-155): worn tools are mended here, inside a claim.
            new StructureShape
            {
                Name = "Repair table",
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
            // A hen coop (building-16, Justin: claim-only, hens roost in it at night): a small shed on legs
            // with a pop-hole and ramp at the front. Animals walk to Entry and settle at Roost.
            new StructureShape
            {
                Name = "Hen coop",
                Model = "Coop",
                Category = "Animals",
                HalfWidth = 0.9f, HalfDepth = 0.7f,
                Clearance = 1.5f,
                RainCover = 1f, Warmth = 0.2f,
                Entry = (0f, 1.5f), Roost = (0f, -0.1f),
                Stages = new[] { "Frame", "Wattle walls", "Thatch roof" },
                // Hen coop: Frame, Wattle walls, Thatch roof (a little less than a lean-to, D-075)
                Costs = new (Item Item, uint Count)[][]
                {
                    new[] { (Item.Stick, 5u) },
                    new[] { (Item.Stick, 4u), (Item.Fiber, 4u) },
                    new[] { (Item.Fiber, 6u) },
                },
            },
            // A tiled house (ceramics-10, Justin): the mud-brick house with a roof of fired clay tiles
            // instead of thatch. Same room, door and shelter; claim-only like the brick house.
            new StructureShape
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
        });

        // Multiplies each shape's lengths by its Scale, once, so nothing else has to know about it.
        static StructureShape[] Scaled(StructureShape[] shapes)
        {
            foreach (var shape in shapes)
            {
                float k = shape.Scale;
                if (k == 1f) continue;
                shape.HalfWidth *= k;
                shape.HalfDepth *= k;
                shape.Radius *= k;
                shape.Clearance *= k;
                shape.DoorwayZ *= k;
                shape.SmokeY *= k;
                shape.Entry = (shape.Entry.X * k, shape.Entry.Z * k);
                shape.Roost = (shape.Roost.X * k, shape.Roost.Z * k);
            }
            return shapes;
        }

        public static float ScaleOf(int kind) => Shape(kind).Scale;

        // What each stage takes, by kind and stage, indexed like Shapes. Fiber stands in
        // for thatch; the hut's walls are daub (clean clay mixed with fiber, from Ceramics).
        // The server builds each stage's needs from this; the client shows it before you lay out.
        // Derived from the shapes, so there is one list to extend; a shape whose stages and costs
        // don't line up fails here at start-up, not at the first build.
        public static readonly (Item Item, uint Count)[][][] StageCosts = Array.ConvertAll(Shapes, shape =>
            shape.Costs.Length == shape.Stages.Length ? shape.Costs
            : throw new InvalidOperationException($"{shape.Name}: {shape.Stages.Length} stages but {shape.Costs.Length} costs"));

        // How far from the player a footprint may be laid out.
        public const float PlaceRange = 6f;
        // Ground under a footprint may rise or fall at most this much.
        public const float MaxGroundStep = 0.7f;
        // Unfinished footprints one player may have open at a time.
        public const int MaxOpenSites = 3;
        // A builder may work on a structure from this far outside its footprint.
        public const float WorkReach = 2.2f;
        // Standing water; matches the client's Look.WaterLevel.
        public const float WaterLevel = -1.0f;

        // The skill each stage practices, for the journal and skills (build.* process ids).
        public const string FrameProcess = "build.frame", WattleProcess = "build.wattle",
            DaubProcess = "build.daub", RoofProcess = "build.roof", WeaveProcess = "build.weave", DoorProcess = "build.door";

        // A hurdle door on a finished clay hut: what hanging it takes (its warmth is the shape's ShutWarmth).
        public const int DoorSticks = 4, DoorFiber = 2;
        // Stacks a storage basket holds.
        public const int BasketStacks = 12;
        // Stacks a storage jar holds (ceramics-6, D-093): 4 for a poor jar up to 8 for
        // a fine one. The first 15% of quality gives 40% of the gain, so an early,
        // better-made jar is quickly worth it; then it climbs slowly (D-075).
        public const int JarStacksMin = 4, JarStacksMax = 8;

        public static int JarStacks(byte quality)
        {
            float t = Math.Clamp(quality / 100f, 0f, 1f);
            float curve = t < 0.15f ? t / 0.15f * 0.4f : 0.4f + (t - 0.15f) / 0.85f * 0.6f;
            return JarStacksMin + (int)MathF.Round((JarStacksMax - JarStacksMin) * curve);
        }

        // Stacks a finished store of this kind and quality holds.
        public static int StoreStacksFor(int kind, byte quality) =>
            Shape(kind).Jar ? JarStacks(quality) : Shape(kind).StoreStacks;

        // Weathering (D-046, claims-15): outside claims, anything players made that nobody
        // has touched for WeatherAfterHours starts to fall apart, and is gone WeatherForHours
        // later. Justin: "starts falling apart after 2 days". One raisable constant each (D-197);
        // a shape may still set its own WeatherAfter, but none does now.
        public const double WeatherAfterHours = 48, WeatherForHours = 6;

        // Whether this kind may be built outside a claim (building-13).
        public static bool IsWild(int kind) => Shape(kind).Wild;

        public static bool IsRoom(int kind) => Shape(kind).Room;

        // The real (scaled) wall numbers of a hut or a room-shaped home.
        public static float HouseWallXFor(int kind) => HouseWallX * ScaleOf(kind);
        public static float HouseWallZFor(int kind) => HouseWallZ * ScaleOf(kind);
        public static float HutWallRadiusFor(int kind) => HutWallRadius * ScaleOf(kind);
        public static float HouseDoorHalfWidthFor(int kind) => HouseDoorHalfWidth * ScaleOf(kind);

        // Whether a finished building of this kind takes a door, and where its doorway is.
        public static bool HasDoorway(int kind) => Shape(kind).DoorwayZ > 0;
        public static bool IsStore(int kind) => Shape(kind).StoreStacks > 0;

        public static double WeatherAfterFor(int kind) =>
            Shape(kind).WeatherAfter > 0 ? Shape(kind).WeatherAfter : WeatherAfterHours;

        // Inside a brick house's walls, where a fire warms the whole room.
        public const float HouseWallX = 2.2f, HouseWallZ = 1.8f;
        // A clay hut's wall circle, and the half-angle of its doorway around +Z.
        public const float HutWallRadius = 1.7f, HutDoorHalfAngle = 0.42f;
        // Half the width of a brick house's doorway.
        public const float HouseDoorHalfWidth = 0.48f;
        // Half a fence length; fences meet end to end every 2 m.
        public const float FenceHalfLength = 1.0f;

        // A raised hearth's ring, and how much longer fuel lasts on it (D-132).
        public const float HearthRadius = 0.45f, HearthFuelScale = 1.5f;
        // How far inside a home's walls a hearth's middle must be.
        public const float HearthWallGap = 0.6f;

        public static bool IsFence(int kind) => Shape(kind).Fence;
        public static bool IsIndoor(int kind) => Shape(kind).Indoor;
        // Huts and houses (the kinds with a doorway): where indoor things like a hearth may go.
        public static bool IsHome(int kind) => HasDoorway(kind);
        public static bool IsGate(int kind) => Shape(kind).Gate;

        public static string StageProcess(int kind, int stage) => kind switch
        {
            // Lean-to: frame, lashing and ribs are all framing; thatch is roofing.
            0 => stage >= 3 ? RoofProcess : FrameProcess,
            _ when Shape(kind).Kiln => ClayRules.KilnProcess,
            // Claim marker (claims-9): one stage, reported as claim.mark for the journal.
            2 => Social.MarkProcess,
            4 => WeaveProcess,
            // Brick and tiled houses: footings and rafters are framing, walls are daub work, the roof is roofing.
            _ when Shape(kind).BrickWalls => stage switch { 1 => DaubProcess, 3 => RoofProcess, _ => FrameProcess },
            // Log cabin: sill, log walls and rafters are all framing; thatch is roofing.
            _ when Shape(kind).Room => stage >= 3 ? RoofProcess : FrameProcess,
            // Fences: posts are framing; rails are wattle work; a gate's hurdle is door work.
            // Hearth: laying the stone ring is framing; the clay surround is daub work.
            _ when IsIndoor(kind) => stage == 0 ? FrameProcess : DaubProcess,
            _ when IsFence(kind) => stage == 0 ? FrameProcess : IsGate(kind) ? DoorProcess : WattleProcess,
            // Each crafting station reports its own id, so the journal knows it is built.
            _ when Shape(kind).Station != StationKind.None => StationInfo.BuildProcess(Shape(kind).Station),
            _ => stage switch { 0 => FrameProcess, 1 => WattleProcess, 2 => DaubProcess, _ => RoofProcess },
        };

        public static StructureShape Shape(int kind) => Shapes[Math.Clamp(kind, 0, Shapes.Length - 1)];

        // A world point in the structure's own frame (yaw in radians about +Y).
        public static (float x, float z) ToLocal(float sx, float sz, float yaw, float px, float pz)
        {
            float dx = px - sx, dz = pz - sz;
            float c = MathF.Cos(yaw), s = MathF.Sin(yaw);
            return (dx * c - dz * s, dx * s + dz * c);
        }

        public static (float x, float z) ToWorld(float sx, float sz, float yaw, float lx, float lz)
        {
            float c = MathF.Cos(yaw), s = MathF.Sin(yaw);
            return (sx + lx * c + lz * s, sz - lx * s + lz * c);
        }

        // Whether a world point is inside the footprint, shrunk or grown by margin.
        public static bool Contains(int kind, float sx, float sz, float yaw, float px, float pz, float margin = 0f)
        {
            var shape = Shape(kind);
            var (lx, lz) = ToLocal(sx, sz, yaw, px, pz);
            if (shape.Round)
            {
                float r = shape.Radius + margin;
                return r > 0 && lx * lx + lz * lz <= r * r;
            }
            return MathF.Abs(lx) <= shape.HalfWidth + margin && MathF.Abs(lz) <= shape.HalfDepth + margin;
        }

        // The two ends of a fence in world space.
        public static ((float x, float z) a, (float x, float z) b) FenceEnds(float sx, float sz, float yaw) =>
            (ToWorld(sx, sz, yaw, -FenceHalfLength, 0f), ToWorld(sx, sz, yaw, FenceHalfLength, 0f));

        // Whether a step from one point to another goes through a fence line, counting
        // a body of this radius: it crosses the line within the fence's length, or it
        // ends closer to the fence than body while moving nearer to it.
        public static bool StepCrossesFence(float sx, float sz, float yaw, float fromX, float fromZ, float toX, float toZ, float body)
        {
            var (ax, az) = ToLocal(sx, sz, yaw, fromX, fromZ);
            var (bx, bz) = ToLocal(sx, sz, yaw, toX, toZ);
            if (az * bz < 0f)
            {
                float x = ax + (bx - ax) * (az / (az - bz));
                if (MathF.Abs(x) <= FenceHalfLength + body) return true;
            }
            float to = FenceDistance(bx, bz), from = FenceDistance(ax, az);
            return to < body && to < from;
        }

        // Distance from a point in a fence's own frame to its line.
        static float FenceDistance(float lx, float lz)
        {
            float dx = MathF.Max(MathF.Abs(lx) - FenceHalfLength, 0f);
            return MathF.Sqrt(dx * dx + lz * lz);
        }

        // Whether a step goes in or out through a hut's or brick house's walls rather
        // than its doorway (open unless doorShut). Other kinds have no walls here.
        public static bool StepCrossesWalls(int kind, float sx, float sz, float yaw, bool doorShut, float fromX, float fromZ, float toX, float toZ)
        {
            var (ax, az) = ToLocal(sx, sz, yaw, fromX, fromZ);
            var (bx, bz) = ToLocal(sx, sz, yaw, toX, toZ);
            if (kind == 1)
            {
                float wall = HutWallRadiusFor(kind), r2 = wall * wall;
                if (ax * ax + az * az < r2 == bx * bx + bz * bz < r2) return false;
                // Through the doorway: the crossing is within the doorway's angle around +Z.
                float mx = (ax + bx) * 0.5f, mz = (az + bz) * 0.5f;
                return doorShut || MathF.Abs(MathF.Atan2(mx, mz)) > HutDoorHalfAngle;
            }
            if (Shape(kind).Room)
            {
                float wx = HouseWallXFor(kind), wz = HouseWallZFor(kind);
                bool inA = MathF.Abs(ax) < wx && MathF.Abs(az) < wz;
                bool inB = MathF.Abs(bx) < wx && MathF.Abs(bz) < wz;
                if (inA == inB) return false;
                float mx = (ax + bx) * 0.5f, mz = (az + bz) * 0.5f;
                return doorShut || mz < 0f || MathF.Abs(mx) > HouseDoorHalfWidthFor(kind);
            }
            return false;
        }

        // Whether a step crosses a lean-to's two sides or its back (the front is open).
        // Other kinds have no such walls.
        public static bool StepCrossesLeanTo(int kind, float sx, float sz, float yaw, float fromX, float fromZ, float toX, float toZ)
        {
            var shape = Shape(kind);
            if (shape.Round || shape.Fence || shape.Room || kind != 0) return false;
            float wx = shape.HalfWidth, wz = shape.HalfDepth;
            var (ax, az) = ToLocal(sx, sz, yaw, fromX, fromZ);
            var (bx, bz) = ToLocal(sx, sz, yaw, toX, toZ);
            // The back wall at z = -wz.
            if ((az > -wz) != (bz > -wz))
            {
                float x = ax + (bx - ax) * ((-wz - az) / (bz - az));
                if (MathF.Abs(x) <= wx) return true;
            }
            // The two sides at x = +-wx.
            foreach (float side in new[] { -wx, wx })
                if ((ax > side) != (bx > side))
                {
                    float z = az + (bz - az) * ((side - ax) / (bx - ax));
                    if (z >= -wz && z <= wz) return true;
                }
            return false;
        }

        public static bool TooClose(int kindA, float ax, float az, int kindB, float bx, float bz)
        {
            float min = Shape(kindA).Clearance + Shape(kindB).Clearance;
            // Fences join end to end and at corners; only stop two on the same spot.
            if (IsFence(kindA) && IsFence(kindB)) min = FenceHalfLength * 0.5f;
            // A hearth goes inside a home; PlaceStructure keeps it within the walls instead.
            if (IsIndoor(kindA) && IsHome(kindB) || IsIndoor(kindB) && IsHome(kindA)) return false;
            float dx = ax - bx, dz = az - bz;
            return dx * dx + dz * dz < min * min;
        }

        // Points around the footprint edge and center, for checking the ground under it.
        public static (float x, float z)[] Samples(int kind, float sx, float sz, float yaw)
        {
            var shape = Shape(kind);
            var local = shape.Round
                ? new[]
                {
                    (0f, 0f), (shape.Radius, 0f), (-shape.Radius, 0f), (0f, shape.Radius), (0f, -shape.Radius),
                    (shape.Radius * 0.7f, shape.Radius * 0.7f), (-shape.Radius * 0.7f, shape.Radius * 0.7f),
                    (shape.Radius * 0.7f, -shape.Radius * 0.7f), (-shape.Radius * 0.7f, -shape.Radius * 0.7f),
                }
                : new[]
                {
                    (0f, 0f), (shape.HalfWidth, shape.HalfDepth), (-shape.HalfWidth, shape.HalfDepth),
                    (shape.HalfWidth, -shape.HalfDepth), (-shape.HalfWidth, -shape.HalfDepth),
                    (0f, shape.HalfDepth), (0f, -shape.HalfDepth),
                };
            var result = new (float, float)[local.Length];
            for (int i = 0; i < local.Length; i++)
                result[i] = ToWorld(sx, sz, yaw, local[i].Item1, local[i].Item2);
            return result;
        }

        // Ground height at a world position, blended between the four nearest
        // height points the way the client draws it. depthAt gives each point's dug depth.
        public static float GroundAt(float x, float z, Func<int, int, short> depthAt)
        {
            int x0 = (int)MathF.Floor(x / World.CellSize), z0 = (int)MathF.Floor(z / World.CellSize);
            float fx = x / World.CellSize - x0, fz = z / World.CellSize - z0;
            float H(int i, int j) => World.Height(i, j, depthAt(i, j));
            float a = H(x0, z0) + (H(x0 + 1, z0) - H(x0, z0)) * fx;
            float b = H(x0, z0 + 1) + (H(x0 + 1, z0 + 1) - H(x0, z0 + 1)) * fx;
            return a + (b - a) * fz;
        }

        // Why a footprint can't go here, or null if it can. Water and steep ground are refused.
        public static string? GroundProblem(int kind, float sx, float sz, float yaw, Func<float, float, float> groundAt)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var (x, z) in Samples(kind, sx, sz, yaw))
            {
                float h = groundAt(x, z);
                lo = MathF.Min(lo, h);
                hi = MathF.Max(hi, h);
            }
            if (lo < WaterLevel + 0.15f) return "Too wet to build here";
            if (hi - lo > MaxGroundStep) return "The ground is too steep here";
            return null;
        }
    }
}
