using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Far chunks share their batches (env-24). Every chunk is its own handful of MultiMeshes,
// about twenty draw calls, so a far view cost draw calls by the thousand. Past NearChunks
// from the player, a chunk draws only its ground, and the trees, rocks and plants of all
// the far chunks in a RegionChunks-square region are drawn as one batch per mesh and
// material. A far region holds only the painted trees' coarse version (it is always past
// their near range) and no see-through copies (nothing that far can be in the camera's
// way). A region is rebuilt only when one of its chunks lands, leaves, or crosses the near
// edge; its chunks' build data is kept, so crossing never rebuilds a chunk.
public partial class Terrain
{
    // Chunks this close (in chunks, either axis) keep their own plants: the grass tufts
    // and the painted trees' near version reach this far.
    const int NearChunks = ViewDistance.GrassChunks;
    const int RegionChunks = 4;

    // Every placed chunk's build, for moving its plants between it and its region.
    readonly Dictionary<(int, int), ChunkData> placed = new();
    // The chunks whose plants are in their region now.
    readonly HashSet<(int, int)> inRegion = new();
    readonly Dictionary<(int, int), Node3D> regions = new();
    readonly HashSet<(int, int)> dirtyRegions = new();

    bool IsFar((int, int) key) =>
        Math.Max(Math.Abs(key.Item1 - center.Item1), Math.Abs(key.Item2 - center.Item2)) > NearChunks;

    static (int, int) RegionOf((int, int) key) => (FloorDiv(key.Item1, RegionChunks), FloorDiv(key.Item2, RegionChunks));

    static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

    // A chunk was put in place: its plants are already on it (near) or go to its region (far).
    void Placed((int, int) key, ChunkData data, bool far)
    {
        placed[key] = data;
        if (far) inRegion.Add(key);
        if (far || inRegion.Remove(key)) dirtyRegions.Add(RegionOf(key));
    }

    // A chunk left the view.
    void Dropped((int, int) key)
    {
        placed.Remove(key);
        if (inRegion.Remove(key)) dirtyRegions.Add(RegionOf(key));
    }

    // The player moved: chunks that crossed the near edge swap where their plants are drawn.
    void Regroup()
    {
        foreach (var (key, data) in placed)
        {
            if (!chunks.TryGetValue(key, out var node)) continue;
            bool far = IsFar(key);
            if (far == inRegion.Contains(key)) continue;
            if (far)
            {
                node.GetNodeOrNull<Node3D>(PlantsName)?.QueueFree();
                ghostHolders.Remove(key);
                inRegion.Add(key);
            }
            else
            {
                var plants = AddPlants(node, data);
                if (plants.GetNodeOrNull<Node3D>(GhostsName) is { } ghosts) ghostHolders[key] = ghosts;
                inRegion.Remove(key);
            }
            dirtyRegions.Add(RegionOf(key));
        }
    }

    // Rebuilds changed regions, a few milliseconds' worth a frame.
    void PumpRegions()
    {
        if (dirtyRegions.Count == 0) return;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        foreach (var region in new List<(int, int)>(dirtyRegions))
        {
            if (clock.Elapsed.TotalMilliseconds > 2) break;
            dirtyRegions.Remove(region);
            if (regions.Remove(region, out var old)) old.QueueFree();
            var node = BuildRegion(region);
            if (node == null) continue;
            AddChild(node);
            regions[region] = node;
        }
    }

    Node3D? BuildRegion((int, int) region)
    {
        var merged = new Dictionary<(Mesh, Material, bool), List<(Transform3D, Color, Color)>>();
        for (int i = 0; i < RegionChunks; i++)
            for (int j = 0; j < RegionChunks; j++)
            {
                var key = (region.Item1 * RegionChunks + i, region.Item2 * RegionChunks + j);
                if (!inRegion.Contains(key) || !placed.TryGetValue(key, out var data)) continue;
                foreach (var (mesh, material, items, shadow, range) in data.Batches)
                {
                    // Only the painted trees' far version: a far region is past their near range.
                    if (range.Item2 > 0f) continue;
                    if (!merged.TryGetValue((mesh, material, shadow), out var all)) merged[(mesh, material, shadow)] = all = new();
                    all.AddRange(items);
                }
            }
        if (merged.Count == 0) return null;
        var root = new Node3D { Name = $"Region_{region.Item1}_{region.Item2}" };
        foreach (var ((mesh, material, shadow), items) in merged)
            AddMulti(root, mesh, items, material, shadow);
        // Nothing this far can stand between the camera and the player.
        root.GetNodeOrNull<Node3D>(GhostsName)?.Free();
        return root;
    }
}
