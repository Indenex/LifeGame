using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// env-9 measurement: --view-bench builds every chunk out to each radius in turn,
// all at once, and prints what it costs: build time, instances, triangles and draw
// calls (before culling; the camera sees roughly a third of them), with grass tufts
// only where WantsTufts puts them. Then quits. With --view-hash it instead prints one
// hash of every chunk out to 4 from the player (ground and instances), to prove a
// change to chunk building leaves the world bit for bit the same.
public partial class Terrain
{
    static readonly bool BenchWanted = OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--view-bench");

    readonly Dictionary<Mesh, long> benchTris = new();

    void RunViewBench(int chunkX, int chunkZ)
    {
        center = (chunkX, chunkZ);
        if (OS.GetCmdlineUserArgs().Concat(OS.GetCmdlineArgs()).Contains("--view-hash"))
        {
            ulong all = 14695981039346656037UL;
            for (int cx = chunkX - 4; cx <= chunkX + 4; cx++)
                for (int cz = chunkZ - 4; cz <= chunkZ + 4; cz++)
                {
                    var node = BuildChunk(cx, cz);
                    foreach (var child in node.GetChildren())
                    {
                        if (child is MultiMeshInstance3D mmi && mmi.Multimesh is { } mm)
                        {
                            Mix(ref all, mm.InstanceCount);
                            Mix(ref all, mm.Mesh.GetSurfaceCount());
                            foreach (float f in mm.Buffer) Mix(ref all, BitConverter.SingleToInt32Bits(f));
                        }
                        else if (child is MeshInstance3D mi)
                        {
                            var a = mi.Mesh.SurfaceGetArrays(0);
                            foreach (var v in a[(int)Mesh.ArrayType.Vertex].AsVector3Array()) { Mix(ref all, BitConverter.SingleToInt32Bits(v.X)); Mix(ref all, BitConverter.SingleToInt32Bits(v.Y)); }
                            foreach (var c in a[(int)Mesh.ArrayType.Color].AsColorArray()) Mix(ref all, BitConverter.SingleToInt32Bits(c.R + c.G * 3 + c.B * 7));
                        }
                    }
                    node.Free();
                }
            GD.Print($"[hash] {all:X16}");
            GetTree().Quit();
            return;
        }
        foreach (int r in new[] { 3, 4, 5, 6, 8, 10, 12 })
        {
            long tris = 0, grassTris = 0, instances = 0, draws = 0, shadowDraws = 0;
            double ms = 0;
            int count = 0;
            for (int cx = chunkX - r; cx <= chunkX + r; cx++)
                for (int cz = chunkZ - r; cz <= chunkZ + r; cz++)
                {
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    var node = BuildChunk(cx, cz);
                    ms += clock.Elapsed.TotalMilliseconds;
                    count++;
                    foreach (var child in node.GetChildren())
                    {
                        Mesh? mesh = null;
                        long n = 1;
                        bool grass = false, shadow = false;
                        if (child is MultiMeshInstance3D mmi && mmi.Multimesh is { } mm)
                        {
                            mesh = mm.Mesh;
                            n = mm.InstanceCount;
                            grass = mmi.MaterialOverride == Look.Grass || mmi.MaterialOverride == Look.GrassTufts;
                            shadow = mmi.CastShadow != GeometryInstance3D.ShadowCastingSetting.Off;
                        }
                        else if (child is MeshInstance3D mi)
                        {
                            mesh = mi.Mesh;
                            shadow = mi.CastShadow != GeometryInstance3D.ShadowCastingSetting.Off;
                        }
                        if (mesh == null) continue;
                        long t = Tris(mesh) * n;
                        tris += t;
                        if (grass) grassTris += t;
                        instances += n;
                        draws += mesh.GetSurfaceCount();
                        if (shadow) shadowDraws += mesh.GetSurfaceCount();
                    }
                    node.Free();
                }
            GD.Print($"[bench] radius {r} ({count} chunks, edge {r * World.ChunkSize} m): build {ms:F0} ms ({ms / count:F1} per chunk), "
                + $"instances {instances}, triangles {tris / 1e6:F2} M (grass {grassTris / 1e6:F2} M), draw calls {draws} (+{shadowDraws} shadow-casting)");
        }
        GetTree().Quit();
    }

    static void Mix(ref ulong h, long v) { h ^= (ulong)v; h *= 1099511628211UL; }

    long Tris(Mesh mesh)
    {
        if (benchTris.TryGetValue(mesh, out long t)) return t;
        t = 0;
        for (int s = 0; s < mesh.GetSurfaceCount(); s++)
        {
            var arrays = mesh.SurfaceGetArrays(s);
            var index = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            t += index.Length > 0 ? index.Length / 3 : arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array().Length / 3;
        }
        return benchTris[mesh] = t;
    }
}
