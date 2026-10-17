using System.Collections.Generic;
using Godot;

namespace LifeGame;

// Bramble's and Pebble's heads authored in Blender (art-rnd-1): a soft round head with
// a mop of chunky hair clumps (Bramble), a smooth, slightly beaked turtle head with a
// daisy (Pebble), built, baked and painted by tools/art/heads/ into client/assets/art/heads/.
// Each head is two meshes on the Head bone, modelled in the rig's rest space:
//
//   "Head": the painted head (skin, hair, daisy). Vertex colour red marks skin, green
//           hair; the shader tints them from the painted colours to the player's look.
//   "Face": a thin patch 2 mm off the front of the skin, showing one cell of the face
//           atlas (eyes, brows, mouth; <race>_face.png, 2 x 2 cells).
//
// For Animation, the interface is one per-instance shader parameter on the Face mesh:
//
//   ChibiFace.SetExpression(node, ChibiFace.Expression.Closed);   // or 0..3
//   // which does: faceMesh.SetInstanceShaderParameter("expression", 1);
//
//   0 Open, 1 Closed (blink), 2 Happy (^ ^ eyes, open smile), 3 Sleepy (half-lidded).
//
// A blink is Closed for about 0.12 s, then back to the expression before it. The
// parameter is per instance, so every figure shares one material and one mesh, and a
// change costs nothing (no material copy, no mesh rebuild). `node` can be the figure's
// Skeleton3D, the head's BoneAttachment3D or anything above the Face mesh; Face(node)
// returns the mesh if Animation wants to keep it.
public static class ChibiFace
{
    public enum Expression { Open, Closed, Happy, Sleepy }

    const string Dir = "res://assets/art/heads/";
    const string NodeName = "AuthoredHead";
    static Shader? headShader, faceShader;
    static readonly Dictionary<string, ShaderMaterial> Materials = new();

    // The colours the textures are painted in (ChibiFigure's default looks).
    static Color PaintedSkin(ChibiFigure.Race race) => race == ChibiFigure.Race.Pebble ? ChibiFigure.PebbleSkins[0] : ChibiFigure.Skins[0];
    static readonly Color PaintedHair = ChibiFigure.Hairs[2];

    public static bool Has(ChibiFigure.Race race) => race is ChibiFigure.Race.Bramble or ChibiFigure.Race.Pebble;

    static string Name(ChibiFigure.Race race) => race == ChibiFigure.Race.Pebble ? "pebble" : "bramble";

    // The head and its face as one node, in the rig's rest space.
    public static Node3D Head(ChibiFigure.Look look)
    {
        string name = Name(look.Race);
        var root = new Node3D { Name = NodeName };
        root.AddChild(new MeshInstance3D { Name = "Head", Mesh = PaintedAsset.Load(Dir + name + "_head.glb")[0], MaterialOverride = HeadMaterial(look) });
        var face = new MeshInstance3D
        {
            Name = "Face", Mesh = PaintedAsset.Load(Dir + name + "_face.glb")[0], MaterialOverride = FaceMaterial(name),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        root.AddChild(face);
        return root;
    }

    // Hangs the authored head on a figure's Head bone (under the inverse of the bone's
    // rest, as ChibiShell does on Spine), or takes it off for other races.
    public static void Sync(Skeleton3D skeleton, ChibiFigure.Look look)
    {
        skeleton.GetNodeOrNull("HeadArt")?.Free();
        if (!Has(look.Race)) return;
        var bone = new BoneAttachment3D { Name = "HeadArt", BoneIdx = HumanRig.Head };
        skeleton.AddChild(bone);
        var head = Head(look);
        head.Transform = skeleton.GetBoneGlobalRest(HumanRig.Head).AffineInverse();
        bone.AddChild(head);
    }

    public static MeshInstance3D? Face(Node node)
    {
        if (node is MeshInstance3D { Name: var n } m && n == "Face") return m;
        foreach (var child in node.GetChildren())
            if (Face(child) is { } found) return found;
        return null;
    }

    public static void SetExpression(Node node, Expression expression) => SetExpression(node, (int)expression);

    // The uniform is a float: an int Variant would not reach it.
    // A head not yet in the scene gets it again as it enters.
    public static void SetExpression(Node node, int expression)
    {
        if (Face(node) is not { } face) return;
        face.SetInstanceShaderParameter("expression", (float)expression);
        if (!face.IsInsideTree())
            face.Connect(Node.SignalName.TreeEntered, Callable.From(() => face.SetInstanceShaderParameter("expression", (float)expression)), (uint)GodotObject.ConnectFlags.OneShot);
    }

    static Vector3 Ratio(Color want, Color painted) => new(
        Mathf.Pow(want.R / painted.R, 2.2f), Mathf.Pow(want.G / painted.G, 2.2f), Mathf.Pow(want.B / painted.B, 2.2f));

    static ShaderMaterial HeadMaterial(ChibiFigure.Look look)
    {
        bool pebble = look.Race == ChibiFigure.Race.Pebble;
        string key = pebble ? $"p{look.Main}" : $"b{look.Main}:{look.Second}";
        if (Materials.TryGetValue(key, out var done)) return done;
        var m = new ShaderMaterial { Shader = headShader ??= new Shader { Code = HeadCode } };
        m.SetShaderParameter("albedo_tex", GD.Load<Texture2D>(Dir + Name(look.Race) + "_head.png"));
        var skin = pebble ? ChibiFigure.PebbleSkins[look.Main % ChibiFigure.PebbleSkins.Length] : ChibiFigure.Skins[look.Main % ChibiFigure.Skins.Length];
        m.SetShaderParameter("skin_tint", Ratio(skin, PaintedSkin(look.Race)));
        m.SetShaderParameter("hair_tint", pebble ? Vector3.One : Ratio(ChibiFigure.Hairs[look.Second % ChibiFigure.Hairs.Length], PaintedHair));
        return Materials[key] = m;
    }

    static ShaderMaterial FaceMaterial(string name)
    {
        string key = "face:" + name;
        if (Materials.TryGetValue(key, out var done)) return done;
        var m = new ShaderMaterial { Shader = faceShader ??= new Shader { Code = FaceCode } };
        m.SetShaderParameter("face_tex", GD.Load<Texture2D>(Dir + name + "_face.png"));
        return Materials[key] = m;
    }

    // ChibiLook's banded light, shared by the head and the face.
    const string Light = """
        void light() {
            float l = clamp(dot(NORMAL, LIGHT), 0.0, 1.0) * ATTENUATION;
            float lit = 0.35 * smoothstep(0.06, 0.1, l) + 0.65 * smoothstep(0.42, 0.47, l);
            vec3 tone = mix(shade_tint, vec3(1.0), smoothstep(0.06, 0.1, l));
            DIFFUSE_LIGHT += LIGHT_COLOR * tone * (0.2 + 0.6 * lit) / PI;
        }
        """;

    const string HeadCode = """
        shader_type spatial;
        render_mode cull_back;
        uniform sampler2D albedo_tex : source_color, filter_linear_mipmap_anisotropic;
        uniform vec3 skin_tint = vec3(1.0);
        uniform vec3 hair_tint = vec3(1.0);
        uniform vec3 shade_tint : source_color = vec3(0.6, 0.56, 0.84);
        uniform float rim_dark = 0.8;
        varying vec3 world_n;
        void vertex() {
            world_n = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
        }
        void fragment() {
            vec3 c = texture(albedo_tex, UV).rgb;
            c *= mix(vec3(1.0), skin_tint, COLOR.r) * mix(vec3(1.0), hair_tint, COLOR.g);
            c *= 0.96 + 0.08 * smoothstep(0.25, 0.75, world_n.y);
            float rim = 1.0 - clamp(abs(dot(NORMAL, VIEW)), 0.0, 1.0);
            c *= mix(1.0, rim_dark, smoothstep(0.62, 0.86, rim));
            ALBEDO = c;
            ROUGHNESS = 0.9;
            SPECULAR = 0.0;
        }
        """ + Light;

    // The face: one atlas cell, picked by the per-instance `expression` (0..3).
    const string FaceCode = """
        shader_type spatial;
        render_mode cull_back, blend_mix, depth_draw_opaque;
        uniform sampler2D face_tex : source_color, filter_linear_mipmap, repeat_disable;
        instance uniform float expression = 0.0;
        uniform vec3 shade_tint : source_color = vec3(0.6, 0.56, 0.84);
        void fragment() {
            int e = clamp(int(expression + 0.5), 0, 3);
            vec2 cell = vec2(float(e % 2), float(e / 2)) * 0.5;
            vec4 t = texture(face_tex, UV + cell);
            ALBEDO = t.rgb;
            ALPHA = t.a;
            ROUGHNESS = 0.9;
            SPECULAR = 0.0;
        }
        """ + Light;
}
