// Shapes and rules for buildings, shared by the server module and the client so
// placement checks, "am I inside?" and what each stage costs agree on both sides.
// Keep this file free of engine and SpacetimeDB types.
using System;
using System.Collections.Generic;
using System.Linq;

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
        // A fire lit within reach goes onto it and its fuel burns longer (the raised and stone-ringed hearths).
        public bool Hearth;
        // How far a fire on this hearth warms, in meters; 0 means the plain fire's Fires.WarmthRadius.
        public float HeatRadius;
        // Ceramics' updraft kiln and storage jar: found by shape, never by enum number (docs/CONTENT.md).
        public bool Kiln, Jar, Trough;
        // Extra metres of ground cleared of grass and trees round the footprint, beyond the usual
        // margin: a low basin needs room so tall grass doesn't hide its rim (ceramics-13).
        public float ClearExtra;
        // A round walled hut: walls on a circle of HutWallRadius with a doorway on +Z (the clay hut).
        public bool Hut;
        // Which hand-built client model draws this kind (StructureModel); empty falls back on the flags above.
        public string Model = "";
        // For a structure animals use (the hen coop): where they walk to go in, and where they settle
        // inside, as offsets in the structure's own frame (+Z is its front). Scaled with the shape.
        public (float X, float Z) Entry, Roost;
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

    // One building kind: its fixed id and its shape (which also carries the stage costs).
    public sealed class StructureDef
    {
        public int Kind;
        public StructureShape Shape = new();
    }

    public static partial class Structures
    {
        // Every kind, keyed by its fixed id. The defs live in one file per lane (shared/structures/);
        // an id is never renumbered or reused, and a duplicate, or a shape whose stages and costs don't
        // line up, fails the first time this class loads. Id ranges: 0-99 the original kinds, House
        // building 100+, Crafting stations 200+, Animals 300+, Claims 400+ (PM, integration-9).
        static readonly Dictionary<int, StructureDef> defs = BuildDefs();
        // Every registered kind, in id order.
        public static readonly int[] Kinds = defs.Keys.OrderBy(k => k).ToArray();

        static Dictionary<int, StructureDef> BuildDefs()
        {
            var all = new Dictionary<int, StructureDef>();
            foreach (var def in CoreDefs().Concat(HouseDefs()).Concat(CraftingDefs()).Concat(AnimalsDefs()).Concat(ClaimsDefs()).Concat(CeramicsDefs()))
            {
                if (!all.TryAdd(def.Kind, def))
                    throw new InvalidOperationException($"Structure kind {def.Kind} is defined twice");
                if (def.Shape.Costs.Length != def.Shape.Stages.Length)
                    throw new InvalidOperationException($"{def.Shape.Name}: {def.Shape.Stages.Length} stages but {def.Shape.Costs.Length} costs");
                Scale(def.Shape);
            }
            return all;
        }

        public static bool Exists(int kind) => defs.ContainsKey(kind);

        // What each stage takes, in order: the server builds each stage's needs from this and the
        // client shows it before you lay out. Fiber stands in for thatch; the hut's walls are daub.
        public static (Item Item, uint Count)[][] Costs(int kind) => defs[kind].Shape.Costs;


        // Multiplies a shape's lengths by its Scale, once, so nothing else has to know about it.
        static void Scale(StructureShape shape)
        {
            float k = shape.Scale;
            if (k == 1f) return;
            shape.HalfWidth *= k;
            shape.HalfDepth *= k;
            shape.Radius *= k;
            shape.Clearance *= k;
            shape.DoorwayZ *= k;
            shape.SmokeY *= k;
            shape.Entry = (shape.Entry.X * k, shape.Entry.Z * k);
            shape.Roost = (shape.Roost.X * k, shape.Roost.Z * k);
        }

        public static float ScaleOf(int kind) => Shape(kind).Scale;

        // How far from the player a footprint may be laid out.
        public const float PlaceRange = 6f;
        // Ground under a footprint may rise or fall at most this much.
        public const float MaxGroundStep = 0.7f;
        // Unfinished footprints one player may have open at a time.
        public const int MaxOpenSites = 3;
        // A builder may work on a structure from this far outside its footprint.
        public const float WorkReach = 2.2f;

        // Litres a trough holds: its own quality sets the size, quicker at first then
        // slowly (D-075), as a jar's stacks do.
        public static int TroughLitres(byte quality)
        {
            float t = Math.Clamp((int)quality, 1, 100) / 100f;
            float curve = 1f - (1f - t) * (1f - t);
            return ClayRules.TroughMin + (int)MathF.Round((ClayRules.TroughMax - ClayRules.TroughMin) * curve);
        }

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
        public static bool IsHearth(int kind) => Shape(kind).Hearth;
        // Huts and houses (the kinds with a doorway): where indoor things like a hearth may go.
        public static bool IsHome(int kind) => HasDoorway(kind);
        public static bool IsGate(int kind) => Shape(kind).Gate;

        public static string StageProcess(int kind, int stage) => kind switch
        {
            // Lean-to: frame, lashing and ribs are all framing; thatch is roofing.
            _ when Shape(kind).Model == "LeanTo" => stage >= 3 ? RoofProcess : FrameProcess,
            _ when Shape(kind).Kiln => ClayRules.KilnProcess,
            // Claim marker (claims-9): one stage, reported as claim.mark for the journal.
            _ when Shape(kind).Model == "ClaimMarker" => Social.MarkProcess,
            _ when Shape(kind).Model == "Basket" => WeaveProcess,
            // Hen coop: the frame is framing, the wattle walls wattle work, the thatch roofing.
            _ when Shape(kind).Model == "Coop" => stage switch { 0 => FrameProcess, 1 => WattleProcess, _ => RoofProcess },
            // Brick and tiled houses: footings and rafters are framing, walls are daub work, the roof is roofing.
            _ when Shape(kind).BrickWalls => stage switch { 1 => DaubProcess, 3 => RoofProcess, _ => FrameProcess },
            // Log cabin: sill, log walls and rafters are all framing; thatch is roofing.
            _ when Shape(kind).Room => stage >= 3 ? RoofProcess : FrameProcess,
            // Fences: posts are framing; rails are wattle work; a gate's hurdle is door work.
            // Hearth: laying the stone ring is framing; the clay surround is daub work.
            _ when IsIndoor(kind) => stage == 0 ? FrameProcess : DaubProcess,
            // A stone-ringed hearth: ring and kerb are both stone laid up like a frame.
            _ when Shape(kind).Hearth => FrameProcess,
            _ when IsFence(kind) => stage == 0 ? FrameProcess : IsGate(kind) ? DoorProcess : WattleProcess,
            // Each crafting station reports its own id, so the journal knows it is built.
            _ when Shape(kind).Station != StationKind.None => StationInfo.BuildProcess(Shape(kind).Station),
            _ => stage switch { 0 => FrameProcess, 1 => WattleProcess, 2 => DaubProcess, _ => RoofProcess },
        };

        // An unknown kind reads as the first one, as before.
        public static StructureShape Shape(int kind) => (defs.TryGetValue(kind, out var def) ? def : defs[0]).Shape;

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
            if (Shape(kind).Hut)
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
            if (shape.Round || shape.Fence || shape.Room || shape.Model != "LeanTo") return false;
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
