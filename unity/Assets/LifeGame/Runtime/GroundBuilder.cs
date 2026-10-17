using System;
using System.Collections.Generic;
using LifeGame.Shared;
using UnityEngine;
using UnityEngine.Rendering;

namespace LifeGame.Spike
{
    // The ground chunks, built from shared/ (LifeGame.Shared.dll) exactly as the Godot
    // client's Terrain.BuildGround / TileColor / UnderGrass do: one vertex per
    // one-metre cell, heights from World.BaseHeight, colours from the same noise and
    // forest, heath, wetland and trail maps. Nothing dug or cleared (no server).
    // Godot's z is flipped on the way into Unity (see SpikeData).
    public static class GroundBuilder
    {
        // Look.cs palette (sRGB, as authored).
        static readonly Color GrassLow = new(0.30f, 0.46f, 0.13f);
        static readonly Color GrassHigh = new(0.48f, 0.58f, 0.17f);
        static readonly Color Clay = new(0.64f, 0.43f, 0.31f);
        static readonly Color Earth = new(0.32f, 0.25f, 0.19f);
        static readonly Color Shore = new(0.80f, 0.74f, 0.52f);
        static readonly Color LeafWarm = new(0.66f, 0.50f, 0.18f);
        static readonly Color HeathGround = new(0.42f, 0.37f, 0.24f);
        static readonly Color Moss = new(0.20f, 0.31f, 0.12f);
        static readonly Color DampShore = new(0.50f, 0.45f, 0.34f);
        static readonly Color LeafLitter = Lerp(Earth, LeafWarm, 0.25f);
        const float WaterLevel = -1.0f;
        const float UnderGrassMatch = 0.7f;

        // Godot's Color and Mathf semantics (Unity's Mathf.SmoothStep is a different function).
        static Color Lerp(Color a, Color b, float t) => new(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        static Color Lightened(Color c, float amount) => new(c.r + (1f - c.r) * amount, c.g + (1f - c.g) * amount, c.b + (1f - c.b) * amount, c.a);
        static float Clamp01(float x) => x < 0f ? 0f : x > 1f ? 1f : x;
        static float SmoothStep(float from, float to, float x)
        {
            if (from == to) return x < from ? 0f : 1f;
            float s = Clamp01((x - from) / (to - from));
            return s * s * (3f - 2f * s);
        }
        static float ToLinear(float c) => c < 0.04045f ? c * (1f / 12.92f) : MathF.Pow((c + 0.055f) * (1f / 1.055f), 2.4f);
        static float ToSrgb(float c) => c < 0.0031308f ? 12.92f * c : 1.055f * MathF.Pow(c, 1f / 2.4f) - 0.055f;

        static float TileHeight(int x, int z) => World.BaseHeight(x, z);

        static Vector3 NormalAt(int x, int z) =>
            new Vector3(TileHeight(x - 1, z) - TileHeight(x + 1, z), 2f, TileHeight(x, z - 1) - TileHeight(x, z + 1)).normalized;

        static Color TileColor(int x, int z, float top)
        {
            float lush = World.Noise(x, z, 14f, 31);
            var color = Lerp(GrassLow, GrassHigh, Clamp01(lush * 1.1f - 0.1f));
            color = Lerp(color, LeafWarm, Clamp01(World.Noise(x, z, 40f, 32) - 0.62f) * 0.5f);
            color = Lerp(color, LeafLitter, Forest.Density(x, z) * 0.4f);
            color = Lerp(color, HeathGround, Forest.Heath(x, z) * 0.45f);
            color = Lerp(color, Moss, Forest.Wetland(x, z) * 0.4f);
            color = Lerp(color, Earth, Landmarks.Trail(x, z) * (0.25f + 0.35f * World.Noise(x + 733, z - 1291, 20f, 93)));
            float clay = SmoothStep(0.64f, 0.85f, World.ClayScore(x, z));
            color = Lerp(color, Clay, clay * 0.35f);
            float sand = 1f - SmoothStep(WaterLevel + 0.08f, WaterLevel + 0.34f, top);
            color = Lerp(color, Shore, sand * 0.8f);
            float damp = 1f - SmoothStep(WaterLevel - 0.02f, WaterLevel + 0.12f, top);
            color = Lerp(color, DampShore, damp * 0.7f);
            return Lightened(color, (Trees.Hash01(x, z, 7) - 0.5f) * 0.08f);
        }

        static Color UnderGrass(int x, int z, float top, Color color)
        {
            if (top < WaterLevel + 0.2f || World.ClayScore(x, z) >= 0.7f) return color;
            float cover = Clamp01(0.93f - 0.5f * Forest.Density(x, z) - 0.5f * Forest.Wetland(x, z) - 0.6f * Landmarks.Trail(x, z));
            var lin = new Color(ToLinear(color.r), ToLinear(color.g), ToLinear(color.b), color.a);
            float lum = lin.r * 0.3f + lin.g * 0.59f + lin.b * 0.11f;
            var olive = new Color(
                (lin.r + (lum * 1.1f - lin.r) * 0.3f) * 0.9f,
                (lin.g + (lum * 1.05f - lin.g) * 0.3f) * 0.9f,
                (lin.b + (lum * 0.55f - lin.b) * 0.3f) * 0.9f,
                lin.a);
            var mixed = Lerp(lin, olive, cover * UnderGrassMatch);
            return new Color(ToSrgb(mixed.r), ToSrgb(mixed.g), ToSrgb(mixed.b), mixed.a);
        }

        // One chunk (Godot's chunk cx, cz) as a Unity mesh, in Unity coordinates.
        public static Mesh BuildChunk(int cx, int cz)
        {
            int n = World.ChunkSize;
            var heights = new float[n + 1, n + 1];
            var normals = new Vector3[n + 1, n + 1];
            var colors = new Color[n + 1, n + 1];
            for (int i = 0; i <= n; i++)
                for (int j = 0; j <= n; j++)
                {
                    int x = cx * n + i, z = cz * n + j;
                    float h = TileHeight(x, z);
                    heights[i, j] = h;
                    var nG = NormalAt(x, z);
                    normals[i, j] = new Vector3(nG.x, nG.y, -nG.z);
                    colors[i, j] = UnderGrass(x, z, h, TileColor(x, z, h));
                }

            var vertices = new List<Vector3>(n * n * 6);
            var vertexNormals = new List<Vector3>(n * n * 6);
            var vertexColors = new List<Color>(n * n * 6);
            void Corner(int i, int j)
            {
                vertices.Add(new Vector3(cx * n + i, heights[i, j], -(cz * n + j)));
                vertexNormals.Add(normals[i, j]);
                vertexColors.Add(colors[i, j]);
            }
            // Godot's order per cell, with each triangle's last two corners swapped
            // for Unity's winding once z is mirrored.
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    Corner(i, j); Corner(i + 1, j + 1); Corner(i + 1, j);
                    Corner(i, j); Corner(i, j + 1); Corner(i + 1, j + 1);
                }
            var tris = new int[vertices.Count];
            for (int k = 0; k < tris.Length; k++) tris[k] = k;
            var mesh = new Mesh { name = $"ground_{cx}_{cz}", indexFormat = IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(vertexNormals);
            mesh.SetColors(vertexColors);
            mesh.SetUVs(0, new Vector2[vertices.Count]);
            mesh.SetUVs(1, new Vector2[vertices.Count]);
            mesh.SetTriangles(tris, 0, true);
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
