// Lets check bots walk like players (bugfix-34): the server refuses steps into tree trunks and
// boulders, so a bot that walks straight lines wedges on them. A bot slides along the edge with
// the same shared rule (Obstacles) the game client uses. Linked into each tool with
// <Compile Include="../common/*.cs" LinkBase="Common" />.
using System.Collections.Generic;
using LifeGame.Shared;
using SpacetimeDB.Types;


public sealed class BotGround : ICellGround
{
    readonly DbConnection conn;

    public BotGround(DbConnection conn) => this.conn = conn;

    public float TileHeight(int x, int z) =>
        World.Height(x, z, conn.Db.TerrainEdit.CellKey.Find(World.CellKey(x, z)) is { } edit ? edit.Depth : (short)0);

    public bool Dug(int x, int z) => conn.Db.TerrainEdit.CellKey.Find(World.CellKey(x, z)) != null;

    public bool Cleared(int x, int z)
    {
        if (conn.Db.ChoppedTree.CellKey.Find(World.CellKey(x, z)) is { Felled: true }) return true;
        foreach (var s in conn.Db.Structure.Iter())
            if (Structures.Contains((int)s.Kind, s.X, s.Z, s.Yaw, x, z, 0.4f)) return true;
        return false;
    }

    // The buildings that stop walkers, as the server sees them (PlayerCollision.cs), refreshed on each use.
    readonly List<(StructureKind Kind, ulong Id, float X, float Z, float Yaw, bool Complete, int Stage, bool Closed)> buildings = new();

    void Refresh()
    {
        buildings.Clear();
        foreach (var s in conn.Db.Structure.Iter())
            buildings.Add((s.Kind, s.Id, s.X, s.Z, s.Yaw, s.Complete, s.Stage, conn.Db.StructureDoor.StructureId.Find(s.Id) is { Closed: true }));
    }

    // Whether a step from one point to another crosses a fence, shut gate, wall, lean-to side or round solid.
    bool BuildingBlocks(float fx, float fz, float tx, float tz, float body)
    {
        foreach (var (kind, _, sx, sz, yaw, complete, stage, closed) in buildings)
        {
            if (MathF.Abs(sx - tx) > 14f || MathF.Abs(sz - tz) > 14f) continue;
            int k = (int)kind;
            var shape = Structures.Shape(k);
            if (shape.Fence)
            {
                if (!complete || (shape.Gate && !closed)) continue;
                if (Structures.StepCrossesFence(sx, sz, yaw, fx, fz, tx, tz, body)) return true;
                continue;
            }
            if (shape.Solid && shape.Round && (complete || stage >= 1))
            {
                float keep = shape.Radius + body;
                float to = MathF.Sqrt((tx - sx) * (tx - sx) + (tz - sz) * (tz - sz)), from = MathF.Sqrt((fx - sx) * (fx - sx) + (fz - sz) * (fz - sz));
                if (to < keep && to < from) return true;
                continue;
            }
            bool walls = kind == StructureKind.ClayHut ? complete || stage >= 1 : Structures.IsRoom(k) && (complete || stage >= 2);
            if (walls && Structures.StepCrossesWalls(k, sx, sz, yaw, closed, fx, fz, tx, tz)) return true;
            if (kind == StructureKind.LeanTo && Structures.StepCrossesLeanTo(k, sx, sz, yaw, fx, fz, tx, tz)) return true;
        }
        return false;
    }

    // Whether a point is inside the body of a tree trunk, boulder or round solid building.
    (float X, float Z) Push(float x, float z, float body)
    {
        (x, z) = Obstacles.PushOut(this, body, x, z, 0f);
        foreach (var (kind, _, sx, sz, _, complete, stage, _) in buildings)
        {
            var shape = Structures.Shape((int)kind);
            if (!(shape.Solid && shape.Round && (complete || stage >= 1))) continue;
            float dx = x - sx, dz = z - sz, keep = shape.Radius + body, d2 = dx * dx + dz * dz;
            if (d2 >= keep * keep) continue;
            if (d2 < 0.0001f) { dx = 1f; dz = 0f; d2 = 1f; }
            float d = MathF.Sqrt(d2);
            x = sx + dx / d * keep;
            z = sz + dz / d * keep;
        }
        return (x, z);
    }

    // The nearest spot a walker can actually stand to a wanted point: a target inside a boulder's or a
    // solid building's body is moved out to its edge, as a player standing against it would be.
    public (float X, float Z) Stand(float x, float z)
    {
        Refresh();
        return Push(x, z, BotBody + 0.02f);
    }

    // Where a bot ends up after trying to step from (fromX, fromZ) to (x, z): pushed out of any trunk or
    // boulder (as a player slides along it) and held in place by walls, fences and solids, as the server
    // would hold it. The body is just wider than the server's, so the server never refuses it.
    public (float X, float Z) Slide(float fromX, float fromZ, float x, float z)
    {
        Refresh();
        (x, z) = Push(x, z, BotBody);
        return BuildingBlocks(fromX, fromZ, x, z, 0.3f) ? (fromX, fromZ) : (x, z);
    }

    // A route round trunks and boulders (grid A* over the same shared rule): the waypoints to walk
    // through, in order, ending at the target. A bot that only slides along one obstacle at a time
    // gets stuck in a thicket; this finds the way out. The straight line is returned when nothing
    // is in the way or when no route exists within the search box (the walk then falls back to sliding).
    const float Cell = 0.5f, Clear = 0.42f;

    // A bot's body when it slides: just wider than the server's (0.25), so the server never refuses it,
    // yet narrow enough to reach a target that sits by a trunk.
    const float BotBody = 0.27f;

    public List<(float X, float Z)> Route(float fx, float fz, float tx, float tz)
    {
        Refresh();
        var straight = new List<(float X, float Z)> { (tx, tz) };
        if (LineFree(fx, fz, tx, tz)) return straight;

        float x0 = MathF.Min(fx, tx) - 10f, x1 = MathF.Max(fx, tx) + 10f, z0 = MathF.Min(fz, tz) - 10f, z1 = MathF.Max(fz, tz) + 10f;
        int w = (int)((x1 - x0) / Cell) + 1, h = (int)((z1 - z0) / Cell) + 1;
        if ((long)w * h > 160_000) return straight;
        (float X, float Z) At(int i, int j) => (x0 + i * Cell, z0 + j * Cell);
        var blocked = new Dictionary<int, bool>();
        bool Blocked(int i, int j)
        {
            int key = j * w + i;
            if (blocked.TryGetValue(key, out bool b)) return b;
            var (px, pz) = At(i, j);
            var (qx, qz) = Push(px, pz, Clear);
            return blocked[key] = qx != px || qz != pz;
        }
        int si = Math.Clamp((int)MathF.Round((fx - x0) / Cell), 0, w - 1), sj = Math.Clamp((int)MathF.Round((fz - z0) / Cell), 0, h - 1);
        int gi = Math.Clamp((int)MathF.Round((tx - x0) / Cell), 0, w - 1), gj = Math.Clamp((int)MathF.Round((tz - z0) / Cell), 0, h - 1);
        var cost = new Dictionary<int, float> { [sj * w + si] = 0f };
        var from = new Dictionary<int, int>();
        var open = new PriorityQueue<int, float>();
        open.Enqueue(sj * w + si, 0f);
        int goal = gj * w + gi, found = -1;
        for (int expanded = 0; open.Count > 0 && expanded < 60_000; expanded++)
        {
            int cur = open.Dequeue();
            if (cur == goal) { found = cur; break; }
            int ci = cur % w, cj = cur / w;
            for (int di = -1; di <= 1; di++)
                for (int dj = -1; dj <= 1; dj++)
                {
                    if (di == 0 && dj == 0) continue;
                    int ni = ci + di, nj = cj + dj;
                    if (ni < 0 || nj < 0 || ni >= w || nj >= h) continue;
                    int n = nj * w + ni;
                    if (n != goal && Blocked(ni, nj)) continue;
                    var (ax, az) = At(ci, cj);
                    var (bx, bz) = At(ni, nj);
                    if (BuildingBlocks(ax, az, bx, bz, 0.35f)) continue;
                    float step = cost[cur] + (di != 0 && dj != 0 ? 1.4142f : 1f);
                    if (cost.TryGetValue(n, out float old) && old <= step) continue;
                    cost[n] = step;
                    from[n] = cur;
                    float hx = ni - gi, hz = nj - gj;
                    open.Enqueue(n, step + MathF.Sqrt(hx * hx + hz * hz));
                }
        }
        if (found < 0) return straight;

        var cells = new List<(float X, float Z)>();
        for (int c = found; ; c = from[c])
        {
            cells.Add(At(c % w, c / w));
            if (c == sj * w + si) break;
        }
        cells.Reverse();
        // Pull the path tight: from each point jump to the farthest later point that can be reached in a straight line.
        var route = new List<(float X, float Z)>();
        var at = (X: fx, Z: fz);
        int k = 0;
        while (k < cells.Count)
        {
            int far = k;
            for (int m = cells.Count - 1; m > k; m--)
                if (LineFree(at.X, at.Z, cells[m].X, cells[m].Z)) { far = m; break; }
            at = cells[far];
            route.Add(at);
            k = far + 1;
        }
        route[^1] = (tx, tz);
        return route;
    }

    // Whether a straight walk between two points keeps clear of every trunk and boulder.
    bool LineFree(float ax, float az, float bx, float bz)
    {
        float dx = bx - ax, dz = bz - az, len = MathF.Sqrt(dx * dx + dz * dz);
        int steps = Math.Max(1, (int)(len / 0.25f));
        for (int i = 1; i <= steps; i++)
        {
            float px = ax + dx * i / steps, pz = az + dz * i / steps;
            var (qx, qz) = Push(px, pz, Clear);
            if (qx != px || qz != pz) return false;
            if (BuildingBlocks(ax + dx * (i - 1) / steps, az + dz * (i - 1) / steps, px, pz, 0.35f)) return false;
        }
        return true;
    }
}
