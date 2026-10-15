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
