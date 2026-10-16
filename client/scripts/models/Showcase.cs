using System;
using System.Collections.Generic;
using Godot;

namespace LifeGame;

// A standalone scene that lines up the model styles side by side under the game's
// light, so the Render lane can capture them for Justin (model-1). No server.
//   godot --path client res://scripts/models/Showcase.tscn -- --view=row --shot=out.png
// Views: row (everyone), people, faces, sheep, walk (people mid-stride, from the side).
// --yaw=<degrees> turns every model; --shot saves a PNG after the scene settles and quits.
public partial class Showcase : Node3D
{
    readonly Dictionary<string, string> args = new();
    readonly List<Skeleton3D> people = new();
    string? shot;
    int frames;

    public override void _Ready()
    {
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            var parts = a.TrimStart('-').Split('=', 2);
            args[parts[0]] = parts.Length > 1 ? parts[1] : "";
        }
        args.TryGetValue("shot", out shot);
        string view = args.GetValueOrDefault("view", "row");
        float yaw = Mathf.DegToRad(float.Parse(args.GetValueOrDefault("yaw", view == "walk" ? "90" : "-25"), System.Globalization.CultureInfo.InvariantCulture));

        SetupWorld();

        var wool = Look.Solid(new Color(0.74f, 0.71f, 0.63f));
        wool.SetShaderParameter("detail_scale", 3.0f);
        wool.SetShaderParameter("bump", 1.2f);
        wool.SetShaderParameter("cavity", 0.3f);

        // Today's figure first, for comparison, then each style.
        float gap = view == "faces" ? 0.75f : 2.2f;
        float x0 = -gap * 1.5f;
        var today = new Avatar("") { Position = new Vector3(x0, 0, 0), Rotation = new Vector3(0, yaw, 0), Visible = view != "sheep" };
        AddChild(today);
        Label("Today", new Vector3(x0, 0, 0));
        for (int i = 0; i < HumanFigure.All.Length; i++)
        {
            var style = HumanFigure.All[i];
            float x = x0 + gap * (i + 1);
            var person = HumanFigure.Create(style);
            person.Position = new Vector3(x, 0, 0);
            person.Rotation = new Vector3(0, yaw, 0);
            AddChild(person);
            people.Add(person);
            var sheep = SheepFigure.Create(style, wool);
            // Sheep stand beside their person in the row, and alone in the sheep view.
            sheep.Position = view == "sheep" ? new Vector3(x, 0, 0) : new Vector3(x + 0.75f, 0, -0.9f);
            sheep.Rotation = new Vector3(0, yaw + (view == "sheep" ? 1.45f : 0.9f), 0);
            sheep.Visible = view is "row" or "sheep";
            person.Visible = view != "sheep";
            AddChild(sheep);
            int tris = HumanFigure.Build(style).Triangles;
            Label(view == "faces" ? $"{style.Key}  {style.Name}" : $"{style.Key}  {style.Name}\nperson {tris:N0} tris, sheep {SheepFigure.Build(style, wool).Triangles:N0}", new Vector3(x, 0, 0));
        }
        if (view == "walk") Stride();
        // The faces view is close enough that the labels would cover the heads.
        if (view == "faces")
            foreach (var child in GetChildren())
                if (child is Label3D label) label.Position += new Vector3(0, -2.15f + 0.1f, 0.3f);

        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        var (from, at) = view switch
        {
            "people" => (new Vector3(0f, 1.3f, 8.4f), new Vector3(0f, 0.95f, 0f)),
            "faces" => (new Vector3(0f, 1.6f, 4.2f), new Vector3(0f, 1.45f, 0f)),
            "sheep" => (new Vector3(0.6f, 1.5f, 6.2f), new Vector3(0.6f, 0.5f, 0f)),
            "walk" => (new Vector3(0f, 1.2f, 8.4f), new Vector3(0f, 0.95f, 0f)),
            _ => (new Vector3(0.4f, 3.2f, 10.5f), new Vector3(0.4f, 0.7f, 0f)),
        };
        camera.LookAtFromPosition(from, at, Vector3.Up);
    }

    // Everyone mid-stride: left leg forward, right arm forward.
    void Stride()
    {
        foreach (var p in people)
        {
            Swing(p, HumanRig.HipL, -0.45f);
            Swing(p, HumanRig.KneeL, 0.15f);
            Swing(p, HumanRig.HipR, 0.35f);
            Swing(p, HumanRig.KneeR, 0.5f);
            Swing(p, HumanRig.ShoulderL, 0.35f);
            Swing(p, HumanRig.ShoulderR, -0.4f);
            Swing(p, HumanRig.ElbowR, -0.3f);
        }
    }

    static void Swing(Skeleton3D s, int bone, float angle) =>
        s.SetBonePoseRotation(bone, new Quaternion(Vector3.Right, angle));

    void Label(string text, Vector3 at) => AddChild(new Label3D
    {
        Text = text,
        FontSize = 34,
        PixelSize = 0.004f,
        Modulate = Look.Cream,
        OutlineSize = 8,
        OutlineModulate = new Color(0.1f, 0.1f, 0.08f, 0.8f),
        Position = at + new Vector3(0, 2.15f, 0),
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
    });

    void SetupWorld()
    {
        var sky = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color(0.30f, 0.45f, 0.62f),
            SkyHorizonColor = new Color(0.80f, 0.74f, 0.62f),
            SkyCurve = 0.12f,
            GroundBottomColor = new Color(0.24f, 0.28f, 0.22f),
            GroundHorizonColor = new Color(0.86f, 0.76f, 0.64f),
        };
        AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Sky,
                Sky = new Sky { SkyMaterial = sky },
                AmbientLightSource = Godot.Environment.AmbientSource.Sky,
                AmbientLightSkyContribution = 0.75f,
                AmbientLightEnergy = 0.45f,
                TonemapMode = Godot.Environment.ToneMapper.Aces,
                TonemapWhite = 6f,
                SsaoEnabled = true,
                SsaoRadius = 1.6f,
                SsaoIntensity = 1.8f,
                GlowEnabled = true,
                GlowIntensity = 0.6f,
                GlowBloom = 0.08f,
                FogEnabled = true,
                FogLightColor = new Color(0.62f, 0.68f, 0.70f),
                FogDensity = 0.006f,
                AdjustmentEnabled = true,
                AdjustmentContrast = 1.08f,
            },
        });
        AddChild(new DirectionalLight3D
        {
            LightColor = new Color(1.0f, 0.86f, 0.66f),
            LightEnergy = 2.4f,
            ShadowEnabled = true,
            ShadowBlur = 1.2f,
            DirectionalShadowMaxDistance = 40f,
            Rotation = new Vector3(Mathf.DegToRad(-30f), Mathf.DegToRad(-35f), 0),
        });
        var grass = Look.Solid(Look.GrassLow);
        grass.SetShaderParameter("detail_scale", 0.45f);
        grass.SetShaderParameter("bump", 1.0f);
        grass.SetShaderParameter("cavity", 0.18f);
        AddChild(new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(80, 80) }, MaterialOverride = grass });
    }

    public override void _Process(double delta)
    {
        if (shot == null || ++frames != 40) return;
        GetViewport().GetTexture().GetImage().SavePng(shot);
        GD.Print($"Showcase: saved {shot}");
        GetTree().Quit();
    }
}
