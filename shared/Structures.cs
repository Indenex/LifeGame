// Shapes and rules for buildings, shared by the server module and the client so
// placement checks and "am I inside?" agree on both sides. Material costs live
// on the server (Building.cs) because they name item kinds; the client reads
// them from each structure's row. Keep this file free of engine and SpacetimeDB types.
using System;

namespace LifeGame.Shared
{
    public sealed class StructureShape
    {
        public string Name = "";
        // Footprint in the structure's own frame: +Z is the open front or door.
        public bool Round;
        public float HalfWidth, HalfDepth, Radius;
        // How far apart two structures' centers must be, as the sum of both clearances.
        public float Clearance;
        // Shelter given to someone standing inside once it is finished, 0 to 1.
        public float RainCover, Warmth;
        // Round shapes only: players can't walk into the footprint once the first stage is done.
        public bool Solid;
        // Where a hung door goes: the doorway's center on the local +Z axis, 0 when it has none.
        public float DoorwayZ;
        // Hours untouched before it starts to weather (D-046); 0 uses WeatherAfterHours.
        public double WeatherAfter;
        // Stacks it stores when finished, 0 for none. Any kind with stacks opens like the basket (BuildingStorage.cs).
        public int StoreStacks;
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
                RainCover = 0.8f, Warmth = 0.3f,
                Stages = new[] { "Frame", "Lashing", "Ribs", "Thatch" },
            },
            new StructureShape
            {
                Name = "Clay hut",
                Round = true, Radius = 1.9f,
                Clearance = 2.2f,
                RainCover = 1f, Warmth = 0.75f,
                DoorwayZ = 1.7f,
                Stages = new[] { "Posts", "Wattle", "Daub", "Roof" },
            },
            // A land claim's marker post (Claims.cs makes the claim when it is finished).
            new StructureShape
            {
                Name = "Claim marker",
                Round = true, Radius = 0.5f,
                Clearance = 1.2f,
                Stages = new[] { "Marker post" },
            },
            new StructureShape
            {
                // Ceramics' updraft kiln: pots are loaded and fired in it (server/spacetimedb/Kiln.cs).
                Name = "Updraft kiln",
                Round = true, Radius = 0.85f,
                Clearance = 1.2f,
                Solid = true,
                Stages = new[] { "Base", "Dome" },
            },
            new StructureShape
            {
                Name = "Storage basket",
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
                RainCover = 1f, Warmth = 0.85f,
                DoorwayZ = 1.8f,
                WeatherAfter = 168,
                Stages = new[] { "Footings", "Brick walls", "Rafters", "Thatch" },
            },
            // Ceramics' storage jar (ceramics-6): a kiln-fired jar set down as a store.
            // Fired clay doesn't rot, so it weathers only after two weeks untouched.
            new StructureShape
            {
                Name = "Storage jar",
                Round = true, Radius = 0.32f,
                Clearance = 0.6f,
                Solid = true,
                // The most a jar can hold; a jar's own quality sets its size (StoreStacksFor).
                StoreStacks = JarStacksMax,
                WeatherAfter = 336,
                Stages = new[] { "Set the jar" },
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

        // A hurdle door on a finished clay hut: what hanging it takes, and the warmth it keeps in when shut.
        public const int DoorSticks = 4, DoorFiber = 2;
        public const float ClosedDoorWarmth = 0.9f;
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

        // Whether a finished building of this kind takes a door, and where its doorway is.
        public static bool HasDoorway(int kind) => Shape(kind).DoorwayZ > 0;
        public static bool IsStore(int kind) => Shape(kind).StoreStacks > 0;

        public static double WeatherAfterFor(int kind) =>
            Shape(kind).WeatherAfter > 0 ? Shape(kind).WeatherAfter : WeatherAfterHours;

        // Inside a brick house's walls, where a fire warms the whole room.
        public const float HouseWallX = 2.2f, HouseWallZ = 1.8f;

        public static string StageProcess(int kind, int stage) => kind switch
        {
            // Lean-to: frame, lashing and ribs are all framing; thatch is roofing.
            0 => stage >= 3 ? RoofProcess : FrameProcess,
            ClayRules.KilnStructureKind => ClayRules.KilnProcess,
            4 => WeaveProcess,
            // Brick house: footings and rafters are framing, walls are daub work, thatch is roofing.
            5 => stage switch { 1 => DaubProcess, 3 => RoofProcess, _ => FrameProcess },
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

        public static bool TooClose(int kindA, float ax, float az, int kindB, float bx, float bz)
        {
            float min = Shape(kindA).Clearance + Shape(kindB).Clearance;
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
