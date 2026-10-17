using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Pebble's shell authored in Blender (art-rnd-1 pilot, docs/art-pipeline.md): one carved
// carapace, its wall and the plastron, built and baked by tools/art/pebble_shell/ and
// painted by script in the Golden Skull way. It rides the Spine bone, as the shell
// built in ChibiFigure did, so every pose and animation carries it.
//
// The material is ChibiLook's banded light (shade, mid, lit, violet shade, dark rim)
// over the painted texture, with the baked normals at part strength so the light bands
// follow the carved seams. The texture is painted in ChibiFigure.Shells[0]; the other
// shell colours tint the carapace from it (vertex colour red marks what is tinted).
public static class ChibiShell
{
    const string Dir = "res://assets/models/pebble_shell/";

    // Draw Pebble with the authored shell; false keeps ChibiFigure's built shell. It also
    // reads false when the asset can't be loaded (not imported yet), so Pebble always
    // keeps a shell.
    public static bool Enabled { get => enabled && Mesh != null; set => enabled = value; }
    static bool enabled = true;

    static Mesh? mesh;
    static bool loaded;
    static readonly Dictionary<string, ShaderMaterial> Materials = new();

    public static Mesh? Mesh
    {
        get
        {
            if (!loaded) { loaded = true; mesh = Load(); }
            return mesh;
        }
    }

    static Mesh? Load()
    {
        if (!ResourceLoader.Exists(Dir + "pebble_shell.glb") || GD.Load<PackedScene>(Dir + "pebble_shell.glb") is not { } packed)
        {
            GD.PushWarning("ChibiShell: pebble_shell.glb could not be loaded; Pebble keeps the built shell.");
            return null;
        }
        var scene = packed.Instantiate();
        var found = Find(scene);
        scene.Free();
        return found;
    }

    static Mesh? Find(Node node)
    {
        if (node is MeshInstance3D m && m.Mesh != null) return m.Mesh;
        foreach (var child in node.GetChildren())
            if (Find(child) is { } found) return found;
        return null;
    }

    // Style 1 shows the scutes; style 0 is an older shell worn smooth.
    public static ShaderMaterial Material(int shell, int style)
    {
        string key = $"{shell}:{style % 2}";
        if (Materials.TryGetValue(key, out var done)) return done;
        var material = new ShaderMaterial { Shader = shader ??= new Shader { Code = Code } };
        material.SetShaderParameter("albedo_tex", GD.Load<Texture2D>(Dir + (style % 2 == 1 ? "shell_scutes.png" : "shell_worn.png")));
        material.SetShaderParameter("normal_tex", GD.Load<Texture2D>(Dir + "shell_normal.png"));
        // The look's shell colour against the one the texture is painted in, in linear light.
        Color want = ChibiFigure.Shells[shell % ChibiFigure.Shells.Length], painted = ChibiFigure.Shells[0];
        material.SetShaderParameter("tint", new Vector3(
            Mathf.Pow(want.R / painted.R, 2.2f), Mathf.Pow(want.G / painted.G, 2.2f), Mathf.Pow(want.B / painted.B, 2.2f)));
        Materials[key] = material;
        return material;
    }

    // Hangs the shell on a figure's Spine bone, or takes it off for other races (a
    // figure can change race in place, PlayerModel.SetLook). The mesh is modelled in the
    // rig's rest space, so it sits under the bone at the inverse of the bone's rest.
    public static void Sync(Skeleton3D skeleton, ChibiFigure.Look look)
    {
        skeleton.GetNodeOrNull("Shell")?.Free();
        if (look.Race != ChibiFigure.Race.Pebble || !Enabled) return;
        var bone = new BoneAttachment3D { Name = "Shell", BoneIdx = HumanRig.Spine };
        skeleton.AddChild(bone);
        bone.AddChild(new MeshInstance3D
        {
            Mesh = Mesh,
            MaterialOverride = Material(look.Second, look.Style),
            Transform = skeleton.GetBoneGlobalRest(HumanRig.Spine).AffineInverse(),
        });
    }

    static Shader? shader;

    const string Code = """
        shader_type spatial;
        render_mode cull_disabled;
        uniform sampler2D albedo_tex : source_color, filter_linear_mipmap_anisotropic, repeat_disable;
        uniform sampler2D normal_tex : hint_normal, filter_linear_mipmap, repeat_disable;
        uniform float normal_strength = 0.5;
        uniform vec3 tint = vec3(1.0);
        uniform vec3 shade_tint : source_color = vec3(0.6, 0.56, 0.84);
        uniform float rim_dark = 0.8;
        varying vec3 world_n;
        void vertex() {
            world_n = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
        }
        void fragment() {
            vec3 c = texture(albedo_tex, UV).rgb * mix(vec3(1.0), tint, COLOR.r);
            c *= 0.96 + 0.08 * smoothstep(0.25, 0.75, world_n.y);
            float rim = 1.0 - clamp(abs(dot(NORMAL, VIEW)), 0.0, 1.0);
            c *= mix(1.0, rim_dark, smoothstep(0.62, 0.86, rim));
            ALBEDO = c;
            NORMAL_MAP = texture(normal_tex, UV).rgb;
            NORMAL_MAP_DEPTH = normal_strength;
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
