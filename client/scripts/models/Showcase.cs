using System;
using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// A standalone scene that lines up the model styles side by side under the game's
// light, so the Render lane can capture them for Justin (model-1). No server.
//   godot --path client res://scripts/models/Showcase.tscn -- --view=row --shot=out.png
// Views: row (everyone), people, faces, sheep, walk, items (--old for today's), held, clothes, stations, props, env, hearth, (people mid-stride, from the side).
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
        if (view is "items" or "held" or "clothes" or "hearth" or "stations" or "props" or "env" or "cooking" or "deer")
        {
            if (view == "hearth") Hearth();
            else if (view == "stations") Stations();
            else if (view == "props") Props();
            else if (view == "env") EnvProps();
            else if (view == "cooking") Cooking();
            else if (view == "deer") Deer();
            else if (view == "items") ItemGrid(args.ContainsKey("old"));
            else if (view == "held") HeldTools();
            else Clothes();
            return;
        }

        var wool = Look.Solid(new Color(0.74f, 0.71f, 0.63f));
        // As Sheep.WoolLook: only a touch of relief.
        wool.SetShaderParameter("detail_scale", 1.5f);
        wool.SetShaderParameter("bump", 0.15f);
        wool.SetShaderParameter("cavity", 0.08f);

        // Today's figure first, for comparison, then each style.
        float gap = view == "faces" ? 0.75f : 2.2f;
        float x0 = -gap * 1.5f;
        var today = new Avatar("") { Position = new Vector3(x0, 0, 0), Rotation = new Vector3(0, yaw, 0), Visible = view != "sheep" };
        AddChild(today);
        if (view == "sheep")
        {
            // In the sheep view a style-A person stands in for scale.
            var scale = PlayerModel.Build();
            scale.Position = new Vector3(x0 + 0.6f, 0, -0.2f);
            scale.Rotation = new Vector3(0, yaw, 0);
            AddChild(scale);
        }
        else Label("Today", new Vector3(x0, 0, 0));
        for (int i = 0; i < HumanFigure.All.Length; i++)
        {
            var style = HumanFigure.All[i];
            float x = x0 + gap * (i + 1);
            var person = HumanFigure.Create(style);
            person.Position = new Vector3(x, 0, 0);
            person.Rotation = new Vector3(0, yaw, 0);
            AddChild(person);
            people.Add(person);
            // The sheep view compares the fleece variants; elsewhere the game's one.
            var fleece = view == "sheep" ? (SheepFigure.Fleece)i : SheepFigure.Variant;
            var sheep = SheepFigure.Create(fleece, wool);
            // Sheep stand beside their person in the row, and alone in the sheep view.
            sheep.Position = view == "sheep" ? new Vector3(x, 0, 0) : new Vector3(x + 0.75f, 0, -0.9f);
            sheep.Rotation = new Vector3(0, yaw + (view == "sheep" ? 1.45f : 0.9f), 0);
            sheep.Visible = view is "row" or "sheep";
            person.Visible = view != "sheep";
            AddChild(sheep);
            int tris = HumanFigure.Build(style).Triangles;
            Label(view == "faces" ? $"{style.Key}  {style.Name}"
                : view == "sheep" ? $"{style.Key}  {fleece}\n{SheepFigure.Build(fleece, wool).Triangles:N0} tris"
                : $"{style.Key}  {style.Name}\nperson {tris:N0} tris, sheep {SheepFigure.Build(fleece, wool).Triangles:N0}", new Vector3(x, view == "sheep" ? -1.1f : 0, 0));
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

    // Every item lying on the grass in rows, the new model or (--old) today's.
    void ItemGrid(bool old)
    {
        var items = new List<LifeGame.Shared.Item>(LifeGame.Shared.Items.All.Keys);
        // Items other lanes are still adding (D-074, animals-8), shown by number.
        var pending = new Dictionary<ushort, string> { [202] = "Raw venison", [203] = "Deer hide", [204] = "Antler", [197] = "Drop spindle", [198] = "Wool yarn", [199] = "Wool cloth", [200] = "Wool tunic", [201] = "Foot wraps" };
        foreach (var id in pending.Keys)
            if (!items.Contains((LifeGame.Shared.Item)id)) items.Add((LifeGame.Shared.Item)id);
        items.Sort();
        const int columns = 7;
        const float step = 0.62f;
        int rowCount = (items.Count + columns - 1) / columns;
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var at = new Vector3((i % columns - (columns - 1) / 2f) * step, 0, (i / columns - (rowCount - 1) / 2f) * step * 1.1f);
            var mesh = old ? ItemLook.OldMeshFor(item) : ItemLook.MeshFor(item);
            // Long things are scaled down to fit their cell; the label says so.
            float size = mesh.GetAabb().Size.Length();
            float fit = Mathf.Min(1f, 0.6f / size);
            AddChild(new MeshInstance3D { Mesh = mesh, Position = at, Scale = Vector3.One * fit, Rotation = new Vector3(0, -0.5f, 0) });
            AddChild(new Label3D
            {
                Text = (LifeGame.Shared.Items.All.TryGetValue(item, out var def) ? def.Name : pending[(ushort)item]) + (fit < 0.99f ? $" (x{fit:0.0})" : ""),
                FontSize = 22,
                PixelSize = 0.0022f,
                Modulate = Look.Cream,
                OutlineSize = 6,
                OutlineModulate = new Color(0.1f, 0.1f, 0.08f, 0.8f),
                Position = at + new Vector3(0, 0.02f, 0.24f),
                Rotation = new Vector3(-Mathf.Pi / 2f + 0.9f, 0, 0),
            });
        }
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 4.3f, 3.6f), new Vector3(0, 0, 0.35f), Vector3.Up);
    }

    // The player figure holding tools in its right hand, arm raised a little.
    void HeldTools()
    {
        // Stand-in ids for the bow and arrow until Crafting's reach main.
        ItemModels.BowItem ??= (LifeGame.Shared.Item)9001;
        ItemModels.ArrowItem ??= (LifeGame.Shared.Item)9002;
        var tools = new[] { LifeGame.Shared.Item.StoneAxe, LifeGame.Shared.Item.Spear, ItemModels.BowItem.Value, ItemModels.ArrowItem.Value, LifeGame.Shared.Item.Basket };
        for (int i = 0; i < tools.Length; i++)
        {
            var figure = PlayerModel.Build();
            figure.Position = new Vector3((i - (tools.Length - 1) / 2f) * 1.1f, 0, 0);
            figure.Rotation = new Vector3(0, -0.5f, 0);
            AddChild(figure);
            // Forearm forward so the tool shows.
            figure.SetBonePoseRotation(HumanRig.ElbowR, new Quaternion(Vector3.Right, -0.9f));
            var hand = figure.FindChild("ToolR", true, false);
            hand.AddChild(new MeshInstance3D { Mesh = ItemModels.HeldMesh(tools[i]) });
        }
        // An arrow in flight, and the bow and arrow lying on the grass.
        AddChild(new MeshInstance3D { Mesh = ItemModels.ArrowFlight, Position = new Vector3(0.6f, 1.6f, 0.8f), Rotation = new Vector3(0, -1.2f, 0) });
        AddChild(new MeshInstance3D { Mesh = ItemModels.GroundMesh(ItemModels.BowItem.Value)!, Position = new Vector3(-0.6f, 0, 1.3f), Rotation = new Vector3(0, 0.3f, 0) });
        AddChild(new MeshInstance3D { Mesh = ItemModels.GroundMesh(ItemModels.ArrowItem.Value)!, Position = new Vector3(0.6f, 0, 1.4f), Rotation = new Vector3(0, -0.4f, 0) });
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 1.4f, 5.4f), new Vector3(0, 0.9f, 0), Vector3.Up);
    }

    // World props for Environment (model-2), tinted the way Terrain tints them.
    void Props()
    {
        ShaderMaterial Tinted(ShaderMaterial source, Color color)
        {
            var material = (ShaderMaterial)source.Duplicate();
            material.SetShaderParameter("albedo", color);
            return material;
        }
        var bark = Tinted(Look.BarkSurface, Look.Bark.Lightened(0.15f));
        var stone = Tinted(Look.RockSurface, Look.Rock);
        var props = new (Mesh, Material, Vector3)[]
        {
            (PropMeshes.Trunk, bark, new Vector3(-2.4f, 0, -0.6f)),
            (PropMeshes.Rock(0), stone, new Vector3(-0.8f, 0, 0.2f)),
            (PropMeshes.FallenLog(0), bark, new Vector3(1.2f, 0, -0.4f)),
            (PropMeshes.Stump, bark, new Vector3(2.8f, 0, 0.6f)),
        };
        foreach (var (mesh, material, at) in props)
            AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = at, Rotation = new Vector3(0, 0.4f, 0) });
        var figure = PlayerModel.Build();
        figure.Position = new Vector3(0.2f, 0, 1.0f);
        AddChild(figure);
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 2.4f, 6.5f), new Vector3(0, 0.8f, 0), Vector3.Up);
    }

    // Red deer for Animals: a hind and a stag, with a person and a sheep for scale.
    void Deer()
    {
        var coat = Look.Solid(new Color(0.55f, 0.30f, 0.18f));
        var hind = DeerFigure.Create(false, coat);
        hind.Position = new Vector3(-0.9f, 0, 0);
        hind.Rotation = new Vector3(0, 1.3f, 0);
        AddChild(hind);
        var stag = DeerFigure.Create(true, coat);
        stag.Position = new Vector3(1.0f, 0, -0.4f);
        stag.Rotation = new Vector3(0, 1.0f, 0);
        AddChild(stag);
        var wool = Look.Solid(new Color(0.74f, 0.71f, 0.63f));
        var sheep = SheepFigure.Create(SheepFigure.Variant, wool);
        sheep.Position = new Vector3(-2.3f, 0, 0.6f);
        sheep.Rotation = new Vector3(0, 1.3f, 0);
        AddChild(sheep);
        var figure = PlayerModel.Build();
        figure.Position = new Vector3(2.6f, 0, 0.5f);
        figure.Rotation = new Vector3(0, -0.4f, 0);
        AddChild(figure);
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 1.8f, 6.0f), new Vector3(0, 0.9f, 0), Vector3.Up);
    }

    // Ceramics' cooking pot at a fire's edge, on its stones, with each dish
    // floating in it, roughly as SimmerView draws it.
    void Cooking()
    {
        AddChild(new MeshInstance3D { Mesh = HearthModel.Ring, Position = new Vector3(0, 0, -0.55f) });
        var dishes = new[] { (CookingMeshes.Dish.Broth, new Color(0.35f, 0.08f, 0.10f)), (CookingMeshes.Dish.Stew, new Color(0.38f, 0.24f, 0.12f)), (CookingMeshes.Dish.Tallow, new Color(0.80f, 0.74f, 0.58f)) };
        var line = PotMesh.WaterLine(ClayForm.CoilPot) * PotView.Size;
        for (int i = 0; i < dishes.Length; i++)
        {
            var root = new Node3D { Position = new Vector3((i - 1) * 0.75f, -0.02f, 0) };
            AddChild(root);
            root.AddChild(new MeshInstance3D { Mesh = PotMesh.For(ClayForm.CoilPot, 0), Scale = Vector3.One * PotView.Size, MaterialOverride = ClayLook.For(ClayStage.Fired, FireKind.Pit, 0f, 0) });
            root.AddChild(new MeshInstance3D { Mesh = CookingMeshes.Under });
            var liquid = new StandardMaterial3D { AlbedoColor = dishes[i].Item2, Roughness = 0.2f };
            root.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = line.X, BottomRadius = line.X, Height = 0.002f, RadialSegments = 20 }, MaterialOverride = liquid, Position = new Vector3(0, line.Y, 0) });
            root.AddChild(new MeshInstance3D { Mesh = CookingMeshes.Floaters(dishes[i].Item1, line.X), Position = new Vector3(0, line.Y + 0.002f, 0) });
        }
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 1.9f, 1.9f), new Vector3(0, 0.15f, 0), Vector3.Up);
    }

    // Environment's env-4 props, drawn the way Terrain draws them: a MultiMesh
    // instance with a color, under the world materials.
    void EnvProps()
    {
        void Place(Mesh mesh, Material material, Vector3 at, Color tint, float scale = 1f)
        {
            var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = mesh, InstanceCount = 1 };
            multi.SetInstanceTransform(0, new Transform3D(new Basis(Vector3.Up, 0.5f).Scaled(Vector3.One * scale), at));
            multi.SetInstanceColor(0, tint);
            AddChild(new MultiMeshInstance3D { Multimesh = multi, MaterialOverride = material });
        }
        if (args.ContainsKey("variants"))
        {
            // Every seeded variant of the landmark props, in rows.
            for (int v = 0; v < PropMeshes.OutcropVariants; v++)
                Place(PropMeshes.Outcrop(v), Look.RockSurface, new Vector3(-7.5f + v * 3.0f, 0, -4.5f), Look.Rock);
            for (int v = 0; v < PropMeshes.BirchVariants; v++)
                Place(PropMeshes.Birch(v), Look.BarkSurface, new Vector3(-6.5f + v * 0.8f, 0, -1.2f), new Color(0.86f, 0.84f, 0.80f));
            for (int v = 0; v < PropMeshes.HeatherVariants; v++)
                Place(PropMeshes.Heather(v), Look.Foliage, new Vector3(-3.0f + v * 0.8f, 0, -0.8f), Colors.White);
            for (int v = 0; v < PropMeshes.GorseVariants; v++)
                Place(PropMeshes.Gorse(v), Look.Foliage, new Vector3(0.0f + v * 1.3f, 0, -1.0f), Colors.White);
            for (int v = 0; v < PropMeshes.RockVariants; v++)
                Place(PropMeshes.Rock(v), Look.RockSurface, new Vector3(-6.0f + v * 1.5f, 0, 1.6f), Look.Rock);
            for (int v = 0; v < PropMeshes.FallenLogVariants; v++)
                Place(PropMeshes.FallenLog(v), Look.BarkSurface, new Vector3(1.0f + v * 4.0f, 0, 1.8f), Look.Bark.Lightened(0.15f));
            for (int v = 0; v < PropMeshes.RiverStoneVariants; v++)
                Place(PropMeshes.RiverStone(v), Look.RockSurface, new Vector3(4.2f + v * 0.45f, 0, -0.6f), Look.Rock.Lightened(0.1f));
            var wide = new Camera3D { Fov = 45f, Current = true };
            AddChild(wide);
            wide.LookAtFromPosition(new Vector3(0, 6.0f, 9.5f), new Vector3(0, 0.4f, -1.2f), Vector3.Up);
            return;
        }
        Place(PropMeshes.Birch(0), Look.BarkSurface, new Vector3(-2.6f, 0, -1.2f), new Color(0.86f, 0.84f, 0.80f));
        Place(PropMeshes.Outcrop(0), Look.RockSurface, new Vector3(1.6f, 0, -1.8f), Look.Rock);
        Place(PropMeshes.Heather(0), Look.Foliage, new Vector3(-1.0f, 0, 0.4f), Colors.White);
        Place(PropMeshes.Heather(1), Look.Foliage, new Vector3(-0.5f, 0, 0.9f), Colors.White, 0.8f);
        Place(PropMeshes.Gorse(0), Look.Foliage, new Vector3(0.2f, 0, -0.2f), Colors.White);
        Place(PropMeshes.Cattails, Look.Grass, new Vector3(2.4f, 0, 0.8f), Look.GrassBlade);
        Place(PropMeshes.Cattails, Look.Grass, new Vector3(2.9f, 0, 0.5f), Look.GrassBlade, 0.85f);
        for (int i = 0; i < 6; i++)
            Place(PropMeshes.RiverStone(i), Look.RockSurface, new Vector3(0.9f + i * 0.35f, 0, 1.4f + (i % 2) * 0.25f), Look.Rock.Lightened(0.1f), 0.8f + (i % 3) * 0.2f);
        var figure = PlayerModel.Build();
        figure.Position = new Vector3(-1.8f, 0, 0.6f);
        AddChild(figure);
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        if (args.ContainsKey("close")) camera.LookAtFromPosition(new Vector3(-2.2f, 1.4f, 0.2f), new Vector3(-2.6f, 1.3f, -1.2f), Vector3.Up);
        else camera.LookAtFromPosition(new Vector3(0, 2.6f, 6.4f), new Vector3(0, 0.7f, -0.3f), Vector3.Up);
    }

    // Work stations side by side (model-2), with a player for scale.
    void Stations()
    {
        var stations = new[]
        {
            ("Crafting table", StationMeshes.CraftingTable()), ("Potter's bench", StationMeshes.PottersBench()),
            ("Distaff stand", StationMeshes.Distaff()), ("Drying rack", StationMeshes.DryingRack()),
        };
        for (int i = 0; i < stations.Length; i++)
        {
            var root = new Node3D { Position = new Vector3((i % 2 - 0.5f) * 2.6f, 0, (i / 2) * -2.4f), Rotation = new Vector3(0, 0.35f, 0) };
            AddChild(root);
            foreach (var piece in stations[i].Item2)
                if (piece.Stage >= 0) root.AddChild(new MeshInstance3D { Mesh = piece.Mesh, MaterialOverride = piece.Material });
            Label(stations[i].Item1, root.Position + new Vector3(0, -0.9f, 0));
        }
        var figure = PlayerModel.Build();
        figure.Position = new Vector3(-2.6f, 0, 0.6f);
        AddChild(figure);
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 3.2f, 5.2f), new Vector3(0, 0.4f, -1.2f), Vector3.Up);
    }

    // The cold hearth: the stone ring with ash and burnt stubs.
    void Hearth()
    {
        AddChild(new MeshInstance3D { Mesh = HearthModel.Ring });
        AddChild(new MeshInstance3D { Mesh = HearthModel.ColdAsh });
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 1.3f, 1.7f), new Vector3(0, 0, 0), Vector3.Up);
    }

    // Leaf wrap, then wool tunic and foot wraps standing, walking, and from behind.
    void Clothes()
    {
        for (int i = 0; i < 4; i++)
        {
            var figure = PlayerModel.Build();
            figure.Position = new Vector3((i - 1.5f) * 1.1f, 0, 0);
            figure.Rotation = new Vector3(0, i == 2 ? 1.2f : i == 3 ? Mathf.Pi - 0.4f : -0.4f, 0);
            AddChild(figure);
            if (i > 0)
            {
                PlayerModel.Wear(figure, PlayerModel.Body, Garments.WoolTunic);
                PlayerModel.Wear(figure, PlayerModel.Feet, Garments.FootWraps);
            }
            if (i == 2)
            {
                people.Add(figure);
                Stride();
            }
        }
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 1.4f, 5.2f), new Vector3(0, 0.9f, 0), Vector3.Up);
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
