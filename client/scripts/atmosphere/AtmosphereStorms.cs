using Godot;

namespace LifeGame;

// Thunderstorms (atmosphere-21). Rare and dramatic, so shelter feels cosy: the
// sky goes dark slate, the light drops, and lightning flickers inside the clouds
// over there. Now and then a strike is close: a jagged white bolt stands from the
// cloud to the ground, the land flashes blue-white with hard shadows thrown away
// from it, and the thunder cracks right after (Audio's SoundMix plays it).
//
// Command line: --lightning=seconds throws a close strike that often, whatever
// the weather, for screenshots; combine with --rain=1 --wind=0.9 --cloud=1.
public partial class Atmosphere
{
    static readonly StringName NStorm = "storm";
    static readonly StringName NFlash = "flash";
    static readonly StringName NFlashDir = "flash_dir";
    static readonly StringName NBoltStrength = "strength";

    // How much of a thunderstorm is overhead, 0 to 1.
    float storminess;
    float strikeAge = 10f, strikePeak;
    float? argLightning;
    // Where the last strike was, seen from the player (flat, unit length).
    Vector3 strikeDir = new(0f, 0f, -1f);
    bool boltShown;

    // The strike's own light: a second sun from the bolt's direction, on only
    // while the flash lasts, so its shadows cost nothing the rest of the time.
    readonly DirectionalLight3D boltLight = new()
    {
        Visible = false,
        ShadowEnabled = true,
        LightColor = new Color(0.78f, 0.84f, 1f),
        LightVolumetricFogEnergy = 0.6f,
        SkyMode = DirectionalLight3D.SkyModeEnum.LightOnly,
    };

    readonly ImmediateMesh boltMesh = new();
    readonly MeshInstance3D boltView = new() { TopLevel = true, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Visible = false };
    readonly ShaderMaterial boltMaterial = new() { Shader = BoltShader };

    void ReadyStorms()
    {
        AddChild(boltLight);
        boltView.Mesh = boltMesh;
        boltView.MaterialOverride = boltMaterial;
        AddChild(boltView);
        argLightning = ArgFloat("lightning");
        if (argLightning != null) nextStrike = 3f;
    }

    // Storms throw lightning every 10 to 30 seconds at their height. Most of it is
    // far off: a glow inside the clouds on one side and a late, low rumble. About
    // one strike in four is close enough to see the bolt and hear the crack.
    void Lightning(float dt, Vector3 focus)
    {
        float stormy = Mathf.SmoothStep(0.75f, 0.95f, rainAmount);
        storminess = Mathf.Lerp(storminess, stormy, 1f - Mathf.Exp(-dt / 4f));
        float pace = argLightning != null ? 1f : stormy;
        nextStrike -= dt * pace;
        if (pace > 0.05f && nextStrike <= 0f)
        {
            nextStrike = argLightning ?? (float)GD.RandRange(10.0, 30.0);
            float near = argLightning != null ? 1f : Mathf.Pow(GD.Randf(), 2f);
            float strength = argLightning != null ? 1f : stormy;
            float angle = GD.Randf() * Mathf.Tau;
            strikeDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            strikePeak = (0.2f + 0.8f * near) * strength;
            strikeAge = 0f;
            // Sound covers about 340 m a second; distant strikes are 2 to 6 km off.
            sound.Thunder(0.3f + (1f - near) * 5f, (0.35f + 0.65f * near) * strength, near);
            boltShown = near > 0.5f && GetViewport()?.GetCamera3D() is { } camera && DrawBolt(focus, camera.GlobalPosition, near);
        }
        strikeAge += dt;
        float a = strikeAge;
        // A bright stroke, a second return stroke a moment later, then gone.
        flash = strikePeak * (Mathf.Exp(-a / 0.07f) + (a > 0.16f ? 0.7f * Mathf.Exp(-(a - 0.16f) / 0.12f) : 0f));

        // The land lit from the strike: low over that horizon, harder the closer.
        bool lit = flash > 0.02f && strikePeak > 0.45f;
        boltLight.Visible = lit;
        if (lit)
        {
            var from = (strikeDir + Vector3.Up * 0.5f).Normalized();
            boltLight.GlobalBasis = Basis.LookingAt(-from, Vector3.Up);
            boltLight.LightEnergy = 3f * flash;
        }
        boltView.Visible = boltShown && flash > 0.01f;
        if (boltView.Visible) boltMaterial.SetShaderParameter(NBoltStrength, Mathf.Clamp(flash * 1.6f, 0f, 1.5f));

        sky.SetShaderParameter(NStorm, storminess);
        sky.SetShaderParameter(NFlash, flash);
        sky.SetShaderParameter(NFlashDir, (strikeDir + Vector3.Up * 0.25f).Normalized());
    }

    // A jagged bolt from the cloud base (160 m) to the ground, 130 to 230 m away in the
    // strike's direction, with a couple of thinner forks. Drawn as ribbons facing
    // the camera, unaffected by fog so the rain doesn't swallow it.
    bool DrawBolt(Vector3 focus, Vector3 eye, float near)
    {
        float distance = Mathf.Lerp(230f, 130f, near);
        var foot = focus + strikeDir * distance;
        foot.Y = focus.Y - 4f;
        var top = foot + new Vector3((GD.Randf() - 0.5f) * 60f, 160f, (GD.Randf() - 0.5f) * 60f);

        boltMesh.ClearSurfaces();
        boltMesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        var trunk = Jagged(top, foot, 6, 0.22f);
        Ribbon(trunk, eye, 2.2f, 1f);
        Ribbon(trunk, eye, 14f, 0.18f);
        int forks = GD.RandRange(1, 3);
        for (int f = 0; f < forks; f++)
        {
            int at = GD.RandRange(trunk.Length / 6, trunk.Length / 2);
            var start = trunk[at];
            float drop = (start.Y - foot.Y) * (float)GD.RandRange(0.25, 0.6);
            var end = start + new Vector3((GD.Randf() - 0.5f) * drop * 1.4f, -drop, (GD.Randf() - 0.5f) * drop * 1.4f);
            var fork = Jagged(start, end, 4, 0.3f);
            Ribbon(fork, eye, 1.1f, 0.55f);
            Ribbon(fork, eye, 7f, 0.1f);
        }
        boltMesh.SurfaceEnd();
        return true;
    }

    // A path from a to b, bent at random by halving it this many times; each
    // bend moves sideways by up to `rough` of the piece's length.
    static Vector3[] Jagged(Vector3 a, Vector3 b, int levels, float rough)
    {
        var points = new System.Collections.Generic.List<Vector3> { a, b };
        for (int level = 0; level < levels; level++)
        {
            var next = new System.Collections.Generic.List<Vector3>(points.Count * 2) { points[0] };
            for (int i = 1; i < points.Count; i++)
            {
                var p = points[i - 1];
                var q = points[i];
                float len = p.DistanceTo(q);
                var mid = (p + q) * 0.5f + new Vector3(GD.Randf() - 0.5f, (GD.Randf() - 0.5f) * 0.3f, GD.Randf() - 0.5f) * len * rough * 2f;
                next.Add(mid);
                next.Add(q);
            }
            points = next;
        }
        return points.ToArray();
    }

    // One camera-facing strip along the path; alpha is how bright it adds.
    void Ribbon(Vector3[] path, Vector3 eye, float width, float alpha)
    {
        boltMesh.SurfaceSetColor(new Color(1f, 1f, 1f, alpha));
        for (int i = 1; i < path.Length; i++)
        {
            var a = path[i - 1];
            var b = path[i];
            var side = (b - a).Cross(eye - (a + b) * 0.5f).Normalized() * width * 0.5f;
            boltMesh.SurfaceAddVertex(a - side);
            boltMesh.SurfaceAddVertex(a + side);
            boltMesh.SurfaceAddVertex(b + side);
            boltMesh.SurfaceAddVertex(a - side);
            boltMesh.SurfaceAddVertex(b + side);
            boltMesh.SurfaceAddVertex(b - side);
        }
    }

    static readonly Shader BoltShader = new()
    {
        Code = """
            shader_type spatial;
            render_mode unshaded, blend_add, cull_disabled, fog_disabled, shadows_disabled, depth_draw_never;

            uniform float strength = 1.0;

            void fragment() {
                // Brighter than white, so the glow spreads it into the rain.
                ALBEDO = vec3(0.82, 0.87, 1.0) * COLOR.a * strength * 3.0;
            }
            """,
    };
}
