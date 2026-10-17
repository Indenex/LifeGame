using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

namespace LifeGame.Spike
{
    // Reads one Godot export (unity/Tools~/spike_export.gd) from
    // StreamingAssets/spike/<preset>/: scene.json, materials.json, shaders/,
    // textures/, meshes/mesh_NNNN.bin and instances.bin.
    //
    // Godot is right-handed (-z forward), Unity left-handed (+z forward). Everything
    // is mirrored on z on the way in (positions, normals, transforms) and triangle
    // winding is reversed, which leaves the picture the same.
    public sealed class SpikeData
    {
        public readonly string Dir;
        public readonly object Scene;
        public readonly List<object> MaterialDefs;
        readonly Dictionary<int, Mesh> meshes = new();
        readonly Dictionary<int, Material> materials = new();
        readonly Dictionary<string, Texture2D> textures = new();
        float[] instances;

        public SpikeData(string dir)
        {
            Dir = dir;
            Scene = MiniJson.Parse(File.ReadAllText(Path.Combine(dir, "scene.json")));
            MaterialDefs = MiniJson.Parse(File.ReadAllText(Path.Combine(dir, "materials.json"))) as List<object>;
        }

        public static Vector3 V(float[] a) => a == null ? Vector3.zero : new Vector3(a[0], a[1], -a[2]);
        public static Color C(float[] a) => a == null ? Color.white : new Color(a[0], a[1], a[2], a.Length > 3 ? a[3] : 1f);

        public float[] Instances => instances ??= ReadFloats(Path.Combine(Dir, "instances.bin"));

        static float[] ReadFloats(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var f = new float[bytes.Length / 4];
            System.Buffer.BlockCopy(bytes, 0, f, 0, f.Length * 4);
            return f;
        }

        // Instance i of a draw: Godot basis columns and origin, mirrored into Unity.
        public Matrix4x4 InstanceMatrix(int offset, int i)
        {
            var f = Instances;
            int b = offset + i * 20;
            var m = new Matrix4x4();
            m.SetColumn(0, new Vector4(f[b + 0], f[b + 1], -f[b + 2], 0));
            m.SetColumn(1, new Vector4(f[b + 3], f[b + 4], -f[b + 5], 0));
            m.SetColumn(2, new Vector4(-f[b + 6], -f[b + 7], f[b + 8], 0));
            m.SetColumn(3, new Vector4(f[b + 9], f[b + 10], -f[b + 11], 1));
            return m;
        }

        public Vector4 InstanceColor(int offset, int i)
        {
            int b = offset + i * 20 + 12;
            var f = Instances;
            return new Vector4(f[b], f[b + 1], f[b + 2], f[b + 3]);
        }

        public Mesh Mesh(int id)
        {
            if (meshes.TryGetValue(id, out var m)) return m;
            return meshes[id] = LoadMesh(Path.Combine(Dir, "meshes", $"mesh_{id:D4}.bin"), id);
        }

        static Mesh LoadMesh(string path, int id)
        {
            using var r = new BinaryReader(File.OpenRead(path));
            r.ReadInt32(); // "MESH"
            int surfaces = r.ReadInt32();
            var pos = new List<Vector3>();
            var nrm = new List<Vector3>();
            var col = new List<Color>();
            var uv = new List<Vector2>();
            var uv2 = new List<Vector2>();
            var subs = new List<int[]>();
            for (int s = 0; s < surfaces; s++)
            {
                int primitive = r.ReadInt32(), nv = r.ReadInt32(), ni = r.ReadInt32(), flags = r.ReadInt32();
                int baseV = pos.Count;
                for (int i = 0; i < nv; i++) pos.Add(new Vector3(r.ReadSingle(), r.ReadSingle(), -r.ReadSingle()));
                for (int i = 0; i < nv; i++) nrm.Add((flags & 1) != 0 ? new Vector3(r.ReadSingle(), r.ReadSingle(), -r.ReadSingle()) : Vector3.up);
                for (int i = 0; i < nv; i++) col.Add((flags & 2) != 0 ? new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()) : Color.white);
                for (int i = 0; i < nv; i++) uv.Add((flags & 4) != 0 ? new Vector2(r.ReadSingle(), r.ReadSingle()) : Vector2.zero);
                for (int i = 0; i < nv; i++) uv2.Add((flags & 8) != 0 ? new Vector2(r.ReadSingle(), r.ReadSingle()) : Vector2.zero);
                if ((flags & 16) != 0) r.BaseStream.Seek(nv * 16L, SeekOrigin.Current);
                int[] idx;
                if (ni > 0)
                {
                    idx = new int[ni];
                    for (int i = 0; i < ni; i++) idx[i] = r.ReadInt32() + baseV;
                }
                else
                {
                    idx = new int[nv];
                    for (int i = 0; i < nv; i++) idx[i] = baseV + i;
                }
                // Godot PRIMITIVE_TRIANGLES is 3; anything else (lines, strips) is not drawn here.
                if (primitive != 3) idx = new int[0];
                for (int t = 0; t + 2 < idx.Length; t += 3) (idx[t + 1], idx[t + 2]) = (idx[t + 2], idx[t + 1]);
                subs.Add(idx);
            }
            var mesh = new Mesh { name = $"godot_mesh_{id:D4}" };
            mesh.indexFormat = pos.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(pos);
            mesh.SetNormals(nrm);
            mesh.SetColors(col);
            mesh.SetUVs(0, uv);
            mesh.SetUVs(1, uv2);
            mesh.subMeshCount = subs.Count;
            for (int s = 0; s < subs.Count; s++) mesh.SetTriangles(subs[s], s, false);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        public Texture2D Texture(string file)
        {
            if (file == null) return null;
            if (textures.TryGetValue(file, out var t)) return t;
            // Godot's noise textures are data (no source_color hint): read them linear.
            t = new Texture2D(2, 2, TextureFormat.RGBA32, true, true) { name = file };
            t.LoadImage(File.ReadAllBytes(Path.Combine(Dir, "textures", file)));
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            return textures[file] = t;
        }

        public enum Kind { Painted, Grass, Water, Chibi, Sky, Skip }

        public string ShaderSource(object def)
        {
            var file = def.Get("shader") as string;
            return file == null ? "" : File.ReadAllText(Path.Combine(Dir, "shaders", file));
        }

        public Kind KindOf(object def)
        {
            string code = ShaderSource(def);
            if (code.Length == 0) return Kind.Skip;
            if (code.Contains("shader_type sky")) return Kind.Sky;
            if (code.Contains("#define GHOST")) return Kind.Skip;   // see-through copies of trees in the way
            if (code.Contains("water_color")) return Kind.Water;
            if (code.Contains("fade_far")) return Kind.Grass;
            if (code.Contains("rim_dark")) return Kind.Chibi;
            if (code.Contains("brush_scale")) return Kind.Painted;
            return Kind.Skip;  // weather effects (rain, mist) and the like
        }

        public object MaterialDef(int id) => id >= 0 && id < MaterialDefs.Count ? MaterialDefs[id] : null;

        // A Unity material for Godot material `id`, or null when it is not drawn.
        public Material Material(int id)
        {
            if (materials.TryGetValue(id, out var m)) return m;
            var def = MaterialDef(id);
            if (def == null) return materials[id] = null;
            var kind = KindOf(def);
            string shaderName = kind switch
            {
                Kind.Painted => "LifeGame/Painted",
                Kind.Grass => "LifeGame/Grass",
                Kind.Water => "LifeGame/Water",
                Kind.Chibi => "LifeGame/Chibi",
                Kind.Sky => "LifeGame/Sky",
                _ => null,
            };
            if (shaderName == null) return materials[id] = null;
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[spike] shader {shaderName} not found (did it fail to compile?)");
                return materials[id] = null;
            }
            m = new Material(shader) { name = $"godot_material_{id}_{kind}", enableInstancing = true };
            string code = ShaderSource(def);
            var colors = new HashSet<string>();
            foreach (Match match in Regex.Matches(code, @"uniform\s+vec[34]\s+(\w+)\s*:\s*source_color"))
                colors.Add(match.Groups[1].Value);
            if (def.Get("params") is Dictionary<string, object> ps)
                foreach (var (name, value) in ps)
                    SetParam(m, name, value, colors.Contains(name));
            if (kind == Kind.Painted)
            {
                bool foliage = code.Contains("#define PAINTED_FOLIAGE");
                m.SetFloat("_PaintedFoliage", foliage ? 1 : 0);
                if (foliage) m.EnableKeyword("_PAINTED_FOLIAGE");
                m.SetFloat("_Cull", code.Contains("cull_disabled") ? (float)CullMode.Off : (float)CullMode.Back);
            }
            return materials[id] = m;
        }

        // Godot's snake_case uniform becomes the Unity property _PascalCase.
        public static string PropertyName(string godot)
        {
            var parts = godot.Split('_');
            var s = "_";
            foreach (var p in parts) if (p.Length > 0) s += char.ToUpperInvariant(p[0]) + p.Substring(1);
            return s;
        }

        void SetParam(Material m, string name, object value, bool isColor)
        {
            string prop = PropertyName(name);
            if (!m.HasProperty(prop) || value == null) return;
            switch (value)
            {
                case string file when file.EndsWith(".png"):
                    m.SetTexture(prop, Texture(file));
                    break;
                case double d:
                    m.SetFloat(prop, (float)d);
                    break;
                case bool b:
                    m.SetFloat(prop, b ? 1 : 0);
                    break;
                case List<object> l:
                    var f = l.ConvertAll(x => x is double dd ? (float)dd : 0f);
                    var v = new Vector4(f[0], f.Count > 1 ? f[1] : 0, f.Count > 2 ? f[2] : 0, f.Count > 3 ? f[3] : 1);
                    // source_color uniforms are sRGB in Godot; SetColor converts to linear
                    // in a linear-space project, as Godot does.
                    if (isColor) m.SetColor(prop, new Color(v.x, v.y, v.z, f.Count > 3 ? v.w : 1f));
                    else m.SetVector(prop, v);
                    break;
            }
        }
    }
}
