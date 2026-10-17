using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Authored assets from the Blender pipeline (art-rnd-1, docs/art-pipeline.md): a .glb
// made by a tools/art/<piece>/ script, drawn with ChibiLook's banded light (shade, mid,
// lit, violet shade, a soft dark rim) over a painted texture, with optional baked normals.
// Vertex colour multiplies the texture (white leaves it as painted); its alpha is free for
// a piece's own use, such as wind sway weights on trees.
public static class PaintedAsset
{
    static readonly Dictionary<string, Mesh[]> Meshes = new();
    static readonly Dictionary<string, ShaderMaterial> Materials = new();
    static Shader? shader;

    // Every mesh in a .glb, in scene order.
    public static Mesh[] Load(string glb)
    {
        if (Meshes.TryGetValue(glb, out var done)) return done;
        var scene = GD.Load<PackedScene>(glb).Instantiate();
        var found = new List<Mesh>();
        Collect(scene, found);
        scene.Free();
        return Meshes[glb] = found.ToArray();
    }

    static void Collect(Node node, List<Mesh> into)
    {
        if (node is MeshInstance3D m && m.Mesh != null) into.Add(m.Mesh);
        foreach (var child in node.GetChildren()) Collect(child, into);
    }

    // The painted material for an albedo texture, and a normal map if there is one.
    // alphaCut > 0 cuts out texels below it (leaf cards).
    public static ShaderMaterial Material(string albedo, string? normal = null, float normalStrength = 0.5f, float alphaCut = 0f)
    {
        string key = $"{albedo}|{normal}|{normalStrength}|{alphaCut}";
        if (Materials.TryGetValue(key, out var done)) return done;
        var material = new ShaderMaterial { Shader = shader ??= new Shader { Code = Code } };
        material.SetShaderParameter("albedo_tex", GD.Load<Texture2D>(albedo));
        material.SetShaderParameter("use_normal", normal != null);
        if (normal != null) material.SetShaderParameter("normal_tex", GD.Load<Texture2D>(normal));
        material.SetShaderParameter("normal_strength", normalStrength);
        material.SetShaderParameter("alpha_cut", alphaCut);
        return Materials[key] = material;
    }

    // A node holding every mesh of a .glb with the one material.
    public static Node3D Instance(string glb, Material material)
    {
        var root = new Node3D();
        foreach (var mesh in Load(glb))
            root.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material });
        return root;
    }

    const string Code = """
        shader_type spatial;
        render_mode cull_disabled;
        uniform sampler2D albedo_tex : source_color, filter_linear_mipmap_anisotropic;
        uniform sampler2D normal_tex : hint_normal, filter_linear_mipmap;
        uniform bool use_normal = false;
        uniform float normal_strength = 0.5;
        uniform float alpha_cut = 0.0;
        uniform vec3 shade_tint : source_color = vec3(0.6, 0.56, 0.84);
        uniform float rim_dark = 0.8;
        varying vec3 world_n;
        void vertex() {
            world_n = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
        }
        void fragment() {
            vec4 t = texture(albedo_tex, UV);
            if (alpha_cut > 0.0 && t.a < alpha_cut) discard;
            vec3 c = t.rgb * COLOR.rgb;
            c *= 0.96 + 0.08 * smoothstep(0.25, 0.75, world_n.y);
            float rim = 1.0 - clamp(abs(dot(NORMAL, VIEW)), 0.0, 1.0);
            c *= mix(1.0, rim_dark, smoothstep(0.62, 0.86, rim));
            ALBEDO = c;
            if (use_normal) {
                NORMAL_MAP = texture(normal_tex, UV).rgb;
                NORMAL_MAP_DEPTH = normal_strength;
            }
            ROUGHNESS = 0.9;
            SPECULAR = 0.0;
        }
        void light() {
            float l = clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION;
            float lit = 0.35 * smoothstep(0.06, 0.1, l) + 0.65 * smoothstep(0.42, 0.47, l);
            vec3 tone = mix(shade_tint, vec3(1.0), smoothstep(0.06, 0.1, l));
            DIFFUSE_LIGHT += LIGHT_COLOR * tone * (0.2 + 0.6 * lit) / PI;
        }
        """;
}
