// Shapes and rules for buildings, shared by the server module and the client so
// placement checks, "am I inside?" and what each stage costs agree on both sides.
// Keep this file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    public sealed class StructureShape
    {
        public string Name = "";
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
        // A crafting station of this kind once finished (D-098); None for anything else.
        public StationKind Station;
        // A rectangular room with four walls (HouseWallX/Z) and its doorway on +Z: the
        // mud-brick house and the log cabin share the walls, door and fire-warms-the-room rule.
        public bool Room;
        public string[] Stages = Array.Empty<string>();
    }

    public static class Structures
    {
        // Indexed by StructureKind; append new kinds at the end, in the same order.
        public static readonly StructureShape[] Shapes =
        {
            new StructureShape
            {
                Name = "Lean-to",
                HalfWidth = 1.5f, HalfDepth = 1.25f,
                Clearance = 1.9f,
                RainCover = 0.6f, Warmth = 0.2f,
                Stages = new[] { "Frame", "Lashing", "Ribs", "Thatch" },
            },
            new StructureShape
            {
                Name = "Clay hut",
                Round = true, Radius = 1.9f,
                Clearance = 2.2f,
                RainCover = 0.9f, Warmth = 0.4f, ShutWarmth = 0.5f,
                DoorwayZ = 1.7f,
                SmokeY = 2.9f,
                Stages = new[] { "Posts", "Wattle", "Daub", "Roof" },
            },
            // A land claim's marker post (Claims.cs makes the claim when it is finished).
            new StructureShape
            {
                Name = "Claim marker",
                Category = "Claims",
                Round = true, Radius = 0.5f,
                Clearance = 1.2f,
                Solid = true,
                Stages = new[] { "Marker post" },
            },
            new StructureShape
            {
                // Ceramics' updraft kiln: pots are loaded and fired in it (server/spacetimedb/Kiln.cs).
                Name = "Updraft kiln",
                Category = "Crafting",
                Round = true, Radius = 0.85f,
                Clearance = 1.2f,
                Solid = true,
                Stages = new[] { "Base", "Dome" },
            },
            new StructureShape
            {
                Name = "Storage basket",
                Category = "Storage",
                Round = true, Radius = 0.35f,
                Clearance = 0.6f,
                Solid = true,
                StoreStacks = BasketStacks,
                Stages = new[] { "Weave" },
            },
            // A mud-brick house: a bigger room with a hearth. A fire lit inside warms
            // the whole room, and it weathers slower than wattle and daub.
            new StructureShape
            {
                Name = "Mud-brick house",
                HalfWidth = 2.4f, HalfDepth = 2.0f,
                Clearance = 3.2f,
                RainCover = 1f, Warmth = 0.5f, ShutWarmth = 0.55f,
                DoorwayZ = 1.8f,
                WeatherAfter = 168,
                Room = true,
                SmokeY = 3.3f,
                Stages = new[] { "Footings", "Brick walls", "Rafters", "Thatch" },
            },
            // Ceramics' storage jar (ceramics-6): a kiln-fired jar set down as a store.
            // Fired clay doesn't rot, so it weathers only after two weeks untouched.
            new StructureShape
            {
                Name = "Storage jar",
                Category = "Storage",
                Round = true, Radius = 0.32f,
                Clearance = 0.6f,
                Solid = true,
                // The most a jar can hold; a jar's own quality sets its size (StoreStacksFor).
                StoreStacks = JarStacksMax,
                WeatherAfter = 336,
                Stages = new[] { "Set the jar" },
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
            },
            // A crafting table (Crafting, D-098): general crafts are made at it, inside a claim.
            new StructureShape
            {
                Name = "Crafting table",
                Category = "Crafting",
                HalfWidth = 0.8f, HalfDepth = 0.45f,
                Clearance = 1.0f,
                Station = StationKind.Table,
                Stages = new[] { "Table" },
            },
            // A raised hearth inside a hut or house: a ring of stones with a clay surround.
            // A fire on it burns longer per stick and warms a whole hut (building-9, D-132).
            new StructureShape
            {
                Name = "Raised hearth",
                Category = "Fire",
                Round = true, Radius = HearthRadius, Solid = true,
                Clearance = 0.6f,
                Indoor = true,
                Stages = new[] { "Stone ring", "Clay surround" },
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
            },
            // A log cabin (building-10, D-151): the forest's way to the same home as the
            // mud-brick house, with the same room, shelter and weathering, built of logs.
            new StructureShape
            {
                Name = "Log cabin",
                HalfWidth = 2.4f, HalfDepth = 2.0f,
                Clearance = 3.2f,
                RainCover = 1f, Warmth = 0.5f, ShutWarmth = 0.55f,
                DoorwayZ = 1.8f,
                WeatherAfter = 168,
                Room = true,
                SmokeY = 3.3f,
                Stages = new[] { "Sill", "Log walls", "Rafters", "Thatch" },
            },
        };

        // What each stage takes, by kind and stage, indexed like Shapes. Fiber stands in
        // for thatch; the hut's walls are daub (clean clay mixed with fiber, from Ceramics).
        // The server builds each stage's needs from this; the client shows it before you lay out.
        public static readonly (Item Item, uint Count)[][][] StageCosts =
        {
            // Lean-to: Frame, Lashing, Ribs, Thatch
            new[]
            {
                new[] { (Item.Stick, 4u) },
                new[] { (Item.Fiber, 3u) },
                new[] { (Item.Stick, 6u) },
                new[] { (Item.Fiber, 8u) },
            },
            // Clay hut (wattle and daub): Posts, Wattle, Daub, Roof
            new[]
            {
                new[] { (Item.Stick, 6u), (Item.Stone, 2u) },
                new[] { (Item.Stick, 10u), (Item.Fiber, 4u) },
                new[] { (Item.Daub, 12u) },
                new[] { (Item.Stick, 6u), (Item.Fiber, 10u) },
            },
            // Claim marker: one stage, costs set by Claims in shared/Social.cs
            new[]
            {
                new[] { (Item.Stick, Social.MarkerSticks), (Item.Fiber, Social.MarkerFiber) },
            },
            // Updraft kiln (Ceramics): Base, Dome
            new[]
            {
                new[] { (Item.Brick, 4u) },
                new[] { (Item.Brick, 4u), (Item.Daub, 2u) },
            },
            // Storage basket: Weave
            new[]
            {
                new[] { (Item.Fiber, 6u), (Item.Cordage, 2u) },
            },
            // Brick house: Footings, Brick walls, Rafters, Thatch
            new[]
            {
                new[] { (Item.Stone, 6u), (Item.Stick, 4u) },
                new[] { (Item.Brick, 16u), (Item.Daub, 6u) },
                new[] { (Item.Stick, 12u), (Item.Cordage, 2u) },
                new[] { (Item.Fiber, 14u) },
            },
            // Storage jar (Ceramics): set down one kiln-fired jar
            new[]
            {
                new[] { (Item.StorageJar, 1u) },
            },
            // Fence: Posts, Rails
            new[]
            {
                new[] { (Item.Stick, 2u) },
                new[] { (Item.Stick, 2u), (Item.Fiber, 1u) },
            },
            // Gate: Posts, Gate (a hurdle like a hut door)
            new[]
            {
                new[] { (Item.Stick, 2u) },
                new[] { (Item.Stick, 3u), (Item.Fiber, 2u) },
            },
            // Crafting table (D-112): Table
            new[]
            {
                new[] { (Item.Stick, 4u), (Item.Cordage, 2u), (Item.Log, 1u) },
            },
            // Raised hearth: Stone ring, Clay surround
            new[]
            {
                new[] { (Item.Stone, 4u) },
                new[] { (Item.Daub, 3u) },
            },
            // Spindle station: your drop spindle becomes part of it (D-137)
            new[]
            {
                new[] { (Item.DropSpindle, 1u), (Item.Stick, 4u), (Item.Cordage, 2u), (Item.Stone, 1u) },
            },
            // Leather drying rack
            new[]
            {
                new[] { (Item.Stick, 6u), (Item.Cordage, 2u) },
            },
            // Potter's bench (Ceramics, D-127): a split log on two stones
            new[]
            {
                new[] { (Item.Log, 1u), (Item.Stone, 2u) },
            },
            // Log cabin: Sill, Log walls, Rafters, Thatch (D-151)
            new[]
            {
                new[] { (Item.Stone, 4u), (Item.Log, 4u) },
                new[] { (Item.Log, 16u) },
                new[] { (Item.Stick, 12u), (Item.Cordage, 2u) },
                new[] { (Item.Fiber, 14u) },
            },
        };

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
            kind == ClayRules.JarStructureKind ? JarStacks(quality) : Shape(kind).StoreStacks;

        // Weathering (D-046): outside claims, anything players made that nobody has
        // touched for WeatherAfterHours starts to weather, and is gone WeatherForHours later.
        public const double WeatherAfterHours = 72, WeatherForHours = 24;

        public static bool IsRoom(int kind) => Shape(kind).Room;

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
            ClayRules.KilnStructureKind => ClayRules.KilnProcess,
            // Claim marker (claims-9): one stage, reported as claim.mark for the journal.
            2 => Social.MarkProcess,
            4 => WeaveProcess,
            // Brick house: footings and rafters are framing, walls are daub work, thatch is roofing.
            5 => stage switch { 1 => DaubProcess, 3 => RoofProcess, _ => FrameProcess },
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
                float r2 = HutWallRadius * HutWallRadius;
                if (ax * ax + az * az < r2 == bx * bx + bz * bz < r2) return false;
                // Through the doorway: the crossing is within the doorway's angle around +Z.
                float mx = (ax + bx) * 0.5f, mz = (az + bz) * 0.5f;
                return doorShut || MathF.Abs(MathF.Atan2(mx, mz)) > HutDoorHalfAngle;
            }
            if (Shape(kind).Room)
            {
                bool inA = MathF.Abs(ax) < HouseWallX && MathF.Abs(az) < HouseWallZ;
                bool inB = MathF.Abs(bx) < HouseWallX && MathF.Abs(bz) < HouseWallZ;
                if (inA == inB) return false;
                float mx = (ax + bx) * 0.5f, mz = (az + bz) * 0.5f;
                return doorShut || mz < 0f || MathF.Abs(mx) > HouseDoorHalfWidth;
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
