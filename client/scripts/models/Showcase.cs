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
        if (view is "items" or "held" or "clothes" or "hearth" or "stations" or "props" or "env" or "cooking" or "deer" or "wear" or "devices" or "materials" or "painted" or "crowd" or "pinetest")
        {
            if (view == "hearth") Hearth();
            else if (view == "stations") Stations();
            else if (view == "props") Props();
            else if (view == "env") EnvProps();
            else if (view == "cooking") Cooking();
            else if (view == "deer") Deer();
            else if (view == "wear") WearRows();
            else if (view == "devices") Devices();
            else if (view == "materials") Materials();
            else if (view == "painted") PaintedScene();
            else if (view == "crowd") Crowd();
            else if (view == "pinetest") PineTest();
            else if (view == "items") ItemGrid(args.ContainsKey("old"));
            else if (view == "held") HeldTools();
            else Clothes();
            // --look=D repaints any view in the painted prototype (model-4).
            if (args.GetValueOrDefault("look", "") == "D" && view is not ("materials" or "painted")) Repaint(this);
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
        ItemModels.BowItem ??= (LifeGame.Shared.Item)16;
        ItemModels.ArrowItem ??= (LifeGame.Shared.Item)17;
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

    // Each tool that wears, as new, worn and spent (D-155), lying in rows.
    void WearRows()
    {
        var tools = new[] { LifeGame.Shared.Item.StoneAxe, LifeGame.Shared.Item.StoneKnife, LifeGame.Shared.Item.Spear, LifeGame.Shared.Item.DiggingStick, LifeGame.Shared.Item.BowDrill, (LifeGame.Shared.Item)16 };
        for (int t = 0; t < tools.Length; t++)
            for (int w = 0; w < 3; w++)
            {
                var mesh = ItemModels.GroundMesh(tools[t], w)!;
                float fit = Mathf.Min(1f, 0.9f / mesh.GetAabb().Size.Length());
                AddChild(new MeshInstance3D { Mesh = mesh, Scale = Vector3.One * fit, Position = new Vector3((w - 1) * 0.9f, 0, (t - 2.5f) * 0.45f) });
            }
        foreach (var (text, x) in new[] { ("new", -0.9f), ("worn", 0f), ("spent", 0.9f) })
            AddChild(new Label3D { Text = text, FontSize = 28, PixelSize = 0.003f, Position = new Vector3(x, 0.02f, 1.55f), Rotation = new Vector3(-1.2f, 0, 0), Modulate = Look.Cream, OutlineSize = 6 });
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 2.6f, 2.0f), new Vector3(0, 0, 0.1f), Vector3.Up);
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
            ("Repair table", StationMeshes.RepairTable()),
        };
        for (int i = 0; i < stations.Length; i++)
        {
            var root = new Node3D { Position = new Vector3((i % 3 - 1f) * 2.6f, 0, (i / 3) * -2.4f), Rotation = new Vector3(0, 0.35f, 0) };
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

    // Batch devices (D-195): each at fill 1 idle in front, full and working behind.
    void Devices()
    {
        var builds = new (string, System.Func<int, bool, List<StructureModel.Piece>>)[]
        {
            ("Settling tub", StationMeshes.SettlingTub), ("Crushing block", StationMeshes.CrushingBlock), ("Tempering trough", StationMeshes.TemperingTrough),
        };
        for (int i = 0; i < builds.Length; i++)
            for (int row = 0; row < 2; row++)
            {
                var root = new Node3D { Position = new Vector3((i - 1f) * 2.2f, 0, row * -2f), Rotation = new Vector3(0, 0.35f, 0) };
                AddChild(root);
                foreach (var piece in builds[i].Item2(row == 0 ? 1 : 3, row == 1))
                    if (piece.Stage >= 0) root.AddChild(new MeshInstance3D { Mesh = piece.Mesh, MaterialOverride = piece.Material });
                if (row == 0) Label(builds[i].Item1, root.Position + new Vector3(0, -0.7f, 0.5f));
            }
        var figure = PlayerModel.Build();
        figure.Position = new Vector3(-3.4f, 0, 0.4f);
        AddChild(figure);
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 2.6f, 4.4f), new Vector3(0, 0.2f, -0.9f), Vector3.Up);
    }

    // Justin's art-direction test set (D-154): log cabin, pines, oaks, boulders, a
    // sheep and the player, in today's materials or a painted-texture prototype
    // (--look=current|A|B|C, see MaterialLook), at --time=day|golden|night (night
    // has a campfire).
    void Materials()
    {
        var variant = args.GetValueOrDefault("look", "A") switch
        {
            "current" => MaterialLook.Variant.Current, "B" => MaterialLook.Variant.B, "C" => MaterialLook.Variant.C, "D" => MaterialLook.Variant.D, "E" => MaterialLook.Variant.E, _ => MaterialLook.Variant.A,
        };
        string time = args.GetValueOrDefault("time", "golden");
        // With --time-of-day the game's own Atmosphere drives sun, sky, fog and grade
        // (its args: --time-of-day, --cloud, --wind, --grade); otherwise a stand-in.
        bool atmosphere = args.ContainsKey("time-of-day");
        if (atmosphere)
        {
            float tod = float.Parse(args["time-of-day"], System.Globalization.CultureInfo.InvariantCulture);
            time = tod > 0.85f || tod < 0.2f ? "night" : "atmosphere";
            AddChild(new Atmosphere());
        }
        bool now = variant == MaterialLook.Variant.Current;
        bool d = variant is MaterialLook.Variant.D or MaterialLook.Variant.E;

        Material Pick(Material original, MaterialLook.Pattern? pattern = null, Color? color = null)
        {
            if (now)
            {
                if (color is not { } c || original is not ShaderMaterial sm) return original;
                var tinted = (ShaderMaterial)sm.Duplicate();
                tinted.SetShaderParameter("albedo", c);
                return tinted;
            }
            var kind = MaterialLook.Classify(original, out var found);
            return MaterialLook.For(pattern ?? kind, variant, color ?? found);
        }
        void Place(Mesh mesh, Material material, Vector3 at, float yaw = 0f, float scale = 1f)
        {
            AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = at, Rotation = new Vector3(0, yaw, 0), Scale = Vector3.One * scale });
        }

        // The cabin, as House building draws it.
        int cabin = 0;
        for (int kind = 0; kind < LifeGame.Shared.Structures.Shapes.Length; kind++)
            if (LifeGame.Shared.Structures.Shape(kind).Name == "Log cabin") cabin = kind;
        var cabinRoot = new Node3D { Position = new Vector3(0, 0, -4.5f), Rotation = new Vector3(0, 0.3f, 0) };
        AddChild(cabinRoot);
        foreach (var piece in StructureModel.Pieces(cabin))
            if (piece.Stage >= 0) cabinRoot.AddChild(new MeshInstance3D { Mesh = piece.Mesh, MaterialOverride = Pick(piece.Material) });

        // Trees as Terrain builds them (its meshes are private; read for this view only).
        var terrain = typeof(Terrain);
        terrain.GetMethod("EnsureMeshes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, null);
        var leafy = (Mesh)terrain.GetField("leafyMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
        var pine = (Mesh)terrain.GetField("pineMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
        var bark = Pick(Look.BarkSurface, MaterialLook.Pattern.Bark, Look.Bark);
        foreach (var (x, z, s) in new[] { (-6.5f, -3.5f, 1.1f), (-8.5f, -0.5f, 0.9f), (-5f, -7.5f, 1.25f) })
        {
            Place(PropMeshes.Trunk, bark, new Vector3(x, 0, z), x, s);
            Place(d ? PropMeshes.ScallopPine : pine, Pick(Look.Foliage, MaterialLook.Pattern.Leaves, Look.Pine), new Vector3(x, 0, z), x, s);
        }
        foreach (var (x, z, s) in new[] { (6.5f, -3f, 1.1f), (8.5f, 0.5f, 0.95f), (5.5f, -8f, 1.2f) })
        {
            Place(PropMeshes.Trunk, bark, new Vector3(x, 0, z), z, s);
            Place(d ? PropMeshes.LeafClumps : leafy, Pick(Look.Foliage, MaterialLook.Pattern.Leaves, Look.Leaf), new Vector3(x, 0, z), z, s);
        }
        var rock = Pick(Look.RockSurface, MaterialLook.Pattern.Stone, Look.Rock);
        Place(PropMeshes.Rock(0), rock, new Vector3(-3f, 0, 1.2f), 0.4f, 1.1f);
        Place(PropMeshes.Rock(0), rock, new Vector3(3.6f, 0, 1.6f), 2.1f, 0.8f);
        Place(PropMeshes.Rock(2), rock, new Vector3(-1.4f, 0, -1.2f), 1.0f, 0.9f);
        // Ground cover from the reference art: a stump, mushrooms and twigs.
        var plain = now ? Look.Solid(Colors.White) : MaterialLook.For(MaterialLook.Pattern.Plain, variant);
        Place(PropMeshes.Stump, Pick(Look.BarkSurface, MaterialLook.Pattern.Bark, Look.Bark.Lightened(0.15f)), new Vector3(3.2f, 0, -0.6f), 0.5f);
        for (int i = 0; i < 3; i++)
        {
            Place(PropMeshes.Mushroom(i), plain, new Vector3(-2.2f + i * 0.35f, 0, 1.9f + (i % 2) * 0.2f), i * 1.3f);
            Place(PropMeshes.Twig(i), Pick(Look.BarkSurface, MaterialLook.Pattern.Bark, Look.Bark), new Vector3(1.0f + i * 0.6f, 0, 2.8f - i * 0.3f), i * 0.9f);
        }

        // A sheep and the player in front.
        var woolColor = new Color(0.74f, 0.71f, 0.63f);
        var woolNow = Look.Solid(woolColor);
        woolNow.SetShaderParameter("detail_scale", 1.5f);
        woolNow.SetShaderParameter("bump", 0.15f);
        woolNow.SetShaderParameter("cavity", 0.08f);
        var sheep = new MeshInstance3D { Mesh = SheepFigure.Build(SheepFigure.Variant, Look.Solid(Colors.White)).Mesh, Position = new Vector3(2.2f, 0, 1.2f), Rotation = new Vector3(0, -0.9f, 0) };
        sheep.SetSurfaceOverrideMaterial(0, now ? woolNow : MaterialLook.For(MaterialLook.Pattern.Wool, variant, woolColor));
        sheep.SetSurfaceOverrideMaterial(1, now ? Look.Solid(Colors.White) : MaterialLook.For(MaterialLook.Pattern.Skin, variant));
        AddChild(sheep);
        var player = PlayerModel.Build();
        player.Position = new Vector3(-0.2f, 0, 2.2f);
        player.Rotation = new Vector3(0, 0.3f, 0);
        AddChild(player);
        if (!now)
            foreach (var child in player.GetChildren())
                if (child is MeshInstance3D mi)
                    mi.MaterialOverride = MaterialLook.For(mi.Name == PlayerModel.SlotNode(PlayerModel.Body) ? MaterialLook.Pattern.Leaves : MaterialLook.Pattern.Skin, variant);

        // Ground: today's grass, or the painted plain pattern in grass colour.
        foreach (var child in GetChildren())
            if (child is MeshInstance3D { Mesh: PlaneMesh } ground && !now)
                ground.MaterialOverride = d ? MaterialLook.For(MaterialLook.Pattern.Grass, variant, new Color(0.62f, 0.64f, 0.38f)) : MaterialLook.For(MaterialLook.Pattern.Plain, variant, Look.GrassLow);

        // Light for the time of day.
        foreach (var child in GetChildren())
        {
            if (atmosphere) break;
            if (child is DirectionalLight3D sun)
            {
                if (time == "golden")
                {
                    sun.LightColor = new Color(1.0f, 0.68f, 0.42f);
                    sun.LightEnergy = 2.2f;
                    sun.Rotation = new Vector3(Mathf.DegToRad(-11f), Mathf.DegToRad(-60f), 0);
                }
                else if (time == "night")
                {
                    sun.LightColor = new Color(0.45f, 0.55f, 0.85f);
                    sun.LightEnergy = 0.35f;
                    sun.Rotation = new Vector3(Mathf.DegToRad(-40f), Mathf.DegToRad(30f), 0);
                }
            }
            if (child is WorldEnvironment world && time != "day")
            {
                var env = world.Environment;
                var sky = (ProceduralSkyMaterial)env.Sky.SkyMaterial;
                if (time == "golden")
                {
                    sky.SkyTopColor = new Color(0.32f, 0.42f, 0.62f);
                    sky.SkyHorizonColor = new Color(0.98f, 0.66f, 0.42f);
                    sky.GroundHorizonColor = new Color(0.9f, 0.62f, 0.42f);
                    env.FogLightColor = new Color(0.95f, 0.7f, 0.5f);
                    env.FogDensity = 0.012f;
                }
                else
                {
                    sky.SkyTopColor = new Color(0.02f, 0.03f, 0.07f);
                    sky.SkyHorizonColor = new Color(0.07f, 0.09f, 0.15f);
                    sky.GroundHorizonColor = new Color(0.05f, 0.06f, 0.08f);
                    sky.GroundBottomColor = new Color(0.02f, 0.02f, 0.03f);
                    env.AmbientLightEnergy = 0.3f;
                    env.FogLightColor = new Color(0.08f, 0.1f, 0.16f);
                    env.FogDensity = 0.02f;
                }
            }
        }
        if (time == "night")
        {
            // A campfire between the player and the cabin.
            var fire = new Vector3(0.6f, 0, 0.4f);
            AddChild(new MeshInstance3D { Mesh = HearthModel.Ring, Position = fire });
            // As FireView lights a campfire at night.
            var flame = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(1f, 0.55f, 0.18f), EmissionEnabled = true, Emission = new Color(1f, 0.45f, 0.12f), EmissionEnergyMultiplier = 6f };
            AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.16f, Height = 0.5f, RadialSegments = 8, Rings = 4 }, MaterialOverride = flame, Position = fire + new Vector3(0, 0.25f, 0) });
            AddChild(new OmniLight3D { Position = fire + new Vector3(0, 0.7f, 0), LightColor = new Color(1f, 0.58f, 0.24f), LightEnergy = 2.6f, OmniRange = 14f, OmniAttenuation = 1.15f, ShadowEnabled = true });
        }

        var camera = new Camera3D { Fov = 42f, Current = true };
        AddChild(camera);
        if (args.ContainsKey("close"))
            camera.LookAtFromPosition(new Vector3(2.6f, 1.9f, 4.6f), new Vector3(-0.6f, 1.0f, -1.5f), Vector3.Up);
        else
            camera.LookAtFromPosition(new Vector3(1.5f, 3.2f, 8.5f), new Vector3(0, 1.3f, -2f), Vector3.Up);
    }

    // Justin's bar (D-232/235): a frame should read as the reference artist's. Trees,
    // bushes, rocks, stumps, logs and ground cover are the Golden Skull pack's own
    // painted sprites stood up as upright cards that turn to face the camera (as
    // Octopath and Dofus-likes do); the cabin, player and sheep stay 3D in variant
    // D's banded, rimmed paint. Prototype for model-4; the art lives in docs/ until
    // picked. Use with Atmosphere's args, e.g. --time-of-day=0.62 --grade=1.
    void PaintedScene()
    {
        const string pack = "res://../docs/art-packs/golden-skull/foliage/";
        var cache = new Dictionary<string, Texture2D>();
        Texture2D Tex(string name)
        {
            if (cache.TryGetValue(name, out var t)) return t;
            var image = Image.LoadFromFile(ProjectSettings.GlobalizePath(pack + name + ".png"));
            image.GenerateMipmaps();
            return cache[name] = ImageTexture.CreateFromImage(image);
        }
        var rng = new RandomNumberGenerator { Seed = 7 };
        // A card `height` metres tall standing on its bottom edge at `at`.
        // Cards face the camera around the upright axis but take light as if they faced
        // mostly up, like the grass, so they catch sun, sky and moonlight instead of
        // going dark whenever the light is behind them.
        var cardShader = new Shader { Code = """
            shader_type spatial;
            render_mode cull_disabled;
            uniform sampler2D tex : source_color, filter_linear_mipmap;
            uniform vec3 tint = vec3(1.0);
            uniform bool flip = false;
            void vertex() {
                vec3 right = normalize(cross(vec3(0.0, 1.0, 0.0), INV_VIEW_MATRIX[2].xyz));
                vec3 back = normalize(cross(right, vec3(0.0, 1.0, 0.0)));
                vec3 scale = vec3(length(MODEL_MATRIX[0].xyz), length(MODEL_MATRIX[1].xyz), length(MODEL_MATRIX[2].xyz));
                vec3 world = MODEL_MATRIX[3].xyz + right * VERTEX.x * scale.x + vec3(0.0, VERTEX.y * scale.y, 0.0);
                VERTEX = (inverse(MODEL_MATRIX) * vec4(world, 1.0)).xyz;
                NORMAL = (inverse(MODEL_MATRIX) * vec4(normalize(vec3(0.0, 1.0, 0.0) + back * 0.5), 0.0)).xyz;
                if (flip) UV.x = 1.0 - UV.x;
            }
            void fragment() {
                vec4 c = texture(tex, UV);
                ALBEDO = c.rgb * tint;
                ALPHA = c.a;
                ALPHA_SCISSOR_THRESHOLD = 0.5;
                ROUGHNESS = 0.95;
                SPECULAR = 0.1;
            }
            """ };
        void Card(string name, Vector3 at, float height, bool shadow = true, float light = 1f)
        {
            var tex = Tex(name);
            float width = height * tex.GetWidth() / tex.GetHeight();
            var material = new ShaderMaterial { Shader = cardShader };
            material.SetShaderParameter("tex", tex);
            material.SetShaderParameter("tint", new Vector3(light, light, light * 0.9f));
            material.SetShaderParameter("flip", rng.Randf() < 0.5f);
            AddChild(new MeshInstance3D
            {
                Mesh = new QuadMesh { Size = new Vector2(width, height), CenterOffset = new Vector3(0, height * 0.5f, 0) },
                MaterialOverride = material,
                Position = at,
                CastShadow = shadow ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off,
            });
        }

        // Ground: the pack's grass tile painted over the plane, in D's banded light.
        foreach (var child in GetChildren())
            if (child is MeshInstance3D { Mesh: PlaneMesh } ground)
                ground.MaterialOverride = MaterialLook.For(MaterialLook.Pattern.Grass, MaterialLook.Variant.D, new Color(0.52f, 0.57f, 0.26f));

        // The cabin, sheep and player in variant D; the roof in rounded-row thatch.
        StructureModel.RoundThatch = !args.ContainsKey("flat-thatch");
        int cabin = 0;
        for (int kind = 0; kind < LifeGame.Shared.Structures.Shapes.Length; kind++)
            if (LifeGame.Shared.Structures.Shape(kind).Name == "Log cabin") cabin = kind;
        var cabinRoot = new Node3D { Position = new Vector3(0.5f, 0, -4.5f), Rotation = new Vector3(0, 0.3f, 0) };
        AddChild(cabinRoot);
        foreach (var piece in StructureModel.Pieces(cabin))
            if (piece.Stage >= 0)
            {
                var kindOf = MaterialLook.Classify(piece.Material, out var albedo);
                cabinRoot.AddChild(new MeshInstance3D { Mesh = piece.Mesh, MaterialOverride = MaterialLook.For(kindOf, MaterialLook.Variant.D, albedo) });
            }
        var sheep = new MeshInstance3D { Mesh = SheepFigure.Build(SheepFigure.Variant, Look.Solid(Colors.White)).Mesh, Position = new Vector3(2.2f, 0, 1.0f), Rotation = new Vector3(0, -0.9f, 0) };
        sheep.SetSurfaceOverrideMaterial(0, MaterialLook.For(MaterialLook.Pattern.Wool, MaterialLook.Variant.D, new Color(0.86f, 0.82f, 0.72f)));
        sheep.SetSurfaceOverrideMaterial(1, MaterialLook.For(MaterialLook.Pattern.Skin, MaterialLook.Variant.D));
        AddChild(sheep);
        var player = PlayerModel.Build(HumanFigure.Appearance.For(3));
        player.Position = new Vector3(-0.3f, 0, 1.6f);
        player.Rotation = new Vector3(0, 0.4f, 0);
        AddChild(player);
        foreach (var child in player.GetChildren())
            if (child is MeshInstance3D mi)
                mi.MaterialOverride = MaterialLook.For(mi.Name == PlayerModel.SlotNode(PlayerModel.Body) ? MaterialLook.Pattern.Leaves : MaterialLook.Pattern.Skin, MaterialLook.Variant.D);

        // A soft dark outline round the 3D models, like the sprites' painted edge:
        // the mesh drawn again a little larger, back faces only, in dark brown.
        var outline = new ShaderMaterial { Shader = new Shader { Code = """
            shader_type spatial;
            render_mode cull_front;
            void vertex() { VERTEX += NORMAL * 0.03; }
            void fragment() { ALBEDO = vec3(0.13, 0.09, 0.1); }
            """ } };
        void Outline(Node node)
        {
            foreach (var child in node.GetChildren())
            {
                if (child is MeshInstance3D mi)
                {
                    if (mi.MaterialOverride is ShaderMaterial m) { var copy = (ShaderMaterial)m.Duplicate(); copy.NextPass = outline; mi.MaterialOverride = copy; }
                    for (int i = 0; i < mi.GetSurfaceOverrideMaterialCount(); i++)
                        if (mi.GetSurfaceOverrideMaterial(i) is ShaderMaterial sm) { var copy = (ShaderMaterial)sm.Duplicate(); copy.NextPass = outline; mi.SetSurfaceOverrideMaterial(i, copy); }
                }
                Outline(child);
            }
        }
        Outline(cabinRoot);
        Outline(player);
        foreach (int i in new[] { 0, 1 })
            if (sheep.GetSurfaceOverrideMaterial(i) is ShaderMaterial sm) { var copy = (ShaderMaterial)sm.Duplicate(); copy.NextPass = outline; sheep.SetSurfaceOverrideMaterial(i, copy); }

        // A forest edge behind, a meadow in front.
        string[] needles = { "Forest_big_tree_needle_04", "Forest_big_tree_needle_04" };
        string[] cypress = { "Forest_big_cypress_01", "Forest_big_cypress_02", "Forest_big_cypress_03" };
        // --trees=meshes shows variant D's 3D scalloped pines instead of the cards.
        bool meshTrees = args.GetValueOrDefault("trees", "cards") == "meshes";
        var bark = MaterialLook.For(MaterialLook.Pattern.Bark, MaterialLook.Variant.D, Look.Bark);
        var needlesMat = MaterialLook.For(MaterialLook.Pattern.Leaves, MaterialLook.Variant.D, Look.Pine);
        foreach (var (x, z) in new[] { (-6.5f, -6f), (-8.5f, -3f), (-4.5f, -9f), (7f, -7.5f), (-10f, -8f), (9.5f, -4f), (4f, -10f) })
        {
            float tall = 6.5f + rng.Randf() * 1.5f;
            if (!meshTrees) { Card(needles[rng.RandiRange(0, 1)], new Vector3(x, 0, z), tall); continue; }
            var scale = Vector3.One * tall / 4.6f;
            AddChild(new MeshInstance3D { Mesh = PropMeshes.Trunk, MaterialOverride = bark, Position = new Vector3(x, 0, z), Scale = scale });
            AddChild(new MeshInstance3D { Mesh = PropMeshes.ScallopPine, MaterialOverride = needlesMat, Position = new Vector3(x, 0, z), Scale = scale, Rotation = new Vector3(0, x, 0) });
        }
        foreach (var (x, z) in new[] { (5.5f, -3.5f), (-2.5f, -8.5f) })
            Card(cypress[rng.RandiRange(0, 2)], new Vector3(x, 0, z), 5.5f + rng.Randf());
        Card("Forest_big_tree_leaf_01", new Vector3(8f, 0, -1f), 6f);
        Card("Forest_big_stone_01", new Vector3(-4.2f, 0, -1.8f), 1.6f);
        Card("Forest_big_stone_02", new Vector3(6.2f, 0, 2.4f), 1.2f);
        Card("Forest_small_tree_stump_01", new Vector3(3.6f, 0, -0.8f), 0.7f);
        Card("Forest_small_tree_stump_03", new Vector3(-5.5f, 0, 1.5f), 0.6f);
        Card("Forest_small_tree_log_01", new Vector3(-2.6f, 0, 3.2f), 0.6f);
        Card("forest_bush_3", new Vector3(-3.4f, 0, -3.8f), 1.1f);
        Card("forest_bush_4", new Vector3(4.6f, 0, -5.2f), 1.2f);
        Card("forest_bush_5", new Vector3(-7f, 0, 0.5f), 1.0f);
        Card("sone_2", new Vector3(1.6f, 0, 3.6f), 0.5f);
        // Bare trees and dead brush among the living, as in reference 02.
        Card("Forest_deadTree_1", new Vector3(-2.2f, 0, -6.5f), 4.2f);
        Card("Forest_deadTree_2", new Vector3(5.8f, 0, 0.6f), 3.6f);
        Card("Forest_deadBush_3", new Vector3(-4.6f, 0, 2.6f), 0.8f);
        Card("Forest_deadbush5", new Vector3(3.4f, 0, 3.4f), 0.7f);
        Card("Forest_deadBush_3", new Vector3(-8f, 0, -1.2f), 0.9f);
        Card("sone_31", new Vector3(-1.2f, 0, 0.4f), 0.25f);
        // Ground cover: the game's blade grass (Justin wants it back, restyled; D-232),
        // in the pack's olive-yellow, with only a few painted accents.
        // Environment's restyled painted blade grass (PaintedGround): broad arching
        // tufts of about a square metre each, in banded tones.
        PaintedGround.Prewarm();
        RenderingServer.GlobalShaderParameterSet("player_position", Vector3.Zero);
        // One tuft per metre cell, turned at random, the variant picked per cell, as
        // the game lays them; only the cabin floor stays bare.
        var cells = new List<Transform3D>[3] { new(), new(), new() };
        for (int x = -14; x < 14; x++)
            for (int z = -16; z < 6; z++)
            {
                var at = new Vector3(x + 0.5f, 0, z + 0.5f);
                if (Mathf.Abs(at.X - 0.5f) < 2.2f && Mathf.Abs(at.Z + 4.5f) < 1.8f) continue;
                cells[rng.RandiRange(0, 2)].Add(new Transform3D(new Basis(Vector3.Up, rng.Randf() * Mathf.Tau), at));
            }
        for (int v = 0; v < 3; v++)
        {
            var blades = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = PaintedGround.BroadTufts(v), InstanceCount = cells[v].Count };
            for (int i = 0; i < cells[v].Count; i++)
            {
                blades.SetInstanceTransform(i, cells[v][i]);
                blades.SetInstanceColor(i, PaintedGround.Grass(new Color(0.47f, 0.58f, 0.2f), rng.Randf()));
            }
            AddChild(new MultiMeshInstance3D { Multimesh = blades, MaterialOverride = PaintedGround.GrassMaterial, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        }
        // Undergrowth as in reference 02: shrubs, stones, ferns and dead brush all
        // through the grass, so the meadow reads dense rather than open.
        string[] clutter = { "forest_bush_1", "forest_bush_2", "forest_bush_3", "forest_bush_4", "forest_bush_5", "Forest_small_bush_01", "Forest_small_bush_02", "Forest_small_fern_01", "Forest_small_fern_02", "Forest_deadBush_3", "Forest_deadbush5", "sone_29", "sone_31", "sone_2" };
        for (int i = 0; i < 170; i++)
        {
            var at = new Vector3(rng.RandfRange(-13f, 13f), 0, rng.RandfRange(-14f, 6f));
            if (Mathf.Abs(at.X - 0.5f) < 2.8f && Mathf.Abs(at.Z + 4.5f) < 2.4f) continue;
            if (at.DistanceTo(new Vector3(-0.3f, 0, 1.6f)) < 1.2f || at.DistanceTo(new Vector3(2.2f, 0, 1.0f)) < 1.2f) continue;
            string name = clutter[rng.RandiRange(0, clutter.Length - 1)];
            float size = name.StartsWith("sone") ? 0.25f + rng.Randf() * 0.35f : name.StartsWith("forest_bush") ? 0.6f + rng.Randf() * 0.5f : 0.4f + rng.Randf() * 0.4f;
            Card(name, at, size, size > 0.5f, 1.0f);
        }
        string[] extras = { "Forest_small_flower_01", "Forest_small_flower_02", "Forest_small_flower_03", "Forest_small_mushroom_01", "Forest_small_mushroom_03", "Forest_small_fern_01" };
        for (int i = 0; i < 90; i++)
        {
            var at = new Vector3(rng.RandfRange(-12f, 12f), 0, rng.RandfRange(-12f, 6f));
            if (Mathf.Abs(at.X - 0.5f) < 2.6f && Mathf.Abs(at.Z + 4.5f) < 2.2f) continue;
            Card(extras[rng.RandiRange(0, extras.Length - 1)], at, 0.3f + rng.Randf() * 0.15f, false, 1.3f);
        }

        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        // The reference art's high three-quarter view, the far distance softly blurred.
        if (args.ContainsKey("near"))
            camera.LookAtFromPosition(new Vector3(1.8f, 4.6f, 7.2f), new Vector3(0, 0.8f, -1.5f), Vector3.Up);
        else
            camera.LookAtFromPosition(new Vector3(2.5f, 7.5f, 11f), new Vector3(0, 0.8f, -2f), Vector3.Up);
        camera.Attributes = new CameraAttributesPractical { DofBlurFarEnabled = true, DofBlurFarDistance = 17f, DofBlurFarTransition = 10f, DofBlurAmount = 0.1f };
        if (!args.ContainsKey("time-of-day")) return;
        AddChild(new Atmosphere());
        float tod = float.Parse(args["time-of-day"], System.Globalization.CultureInfo.InvariantCulture);
        if (tod > 0.85f || tod < 0.2f)
        {
            // A campfire by the cabin at night, lit as FireView lights one.
            var fire = new Vector3(1.0f, 0, -1.2f);
            AddChild(new MeshInstance3D { Mesh = HearthModel.Ring, Position = fire });
            var flame = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(1f, 0.55f, 0.18f), EmissionEnabled = true, Emission = new Color(1f, 0.45f, 0.12f), EmissionEnergyMultiplier = 6f };
            AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.16f, Height = 0.5f, RadialSegments = 8, Rings = 4 }, MaterialOverride = flame, Position = fire + new Vector3(0, 0.25f, 0) });
            AddChild(new OmniLight3D { Position = fire + new Vector3(0, 0.7f, 0), LightColor = new Color(1f, 0.58f, 0.24f), LightEnergy = 2.6f, OmniRange = 14f, OmniAttenuation = 1.15f, ShadowEnabled = true });
        }
    }

    // Players side by side, each with their own look from their player id (skin, hair,
    // cut, beard), the first in the default look.
    void Crowd()
    {
        float yaw = Mathf.DegToRad(float.Parse(args.GetValueOrDefault("yaw", "0"), System.Globalization.CultureInfo.InvariantCulture));
        for (int i = 0; i < 10; i++)
        {
            var look = i == 0 ? HumanFigure.Appearance.Default : HumanFigure.Appearance.For((uint)i);
            var figure = PlayerModel.Build(look);
            figure.Position = new Vector3((i % 5 - 2f) * 0.9f, 0, (i / 5) * -1.6f);
            figure.Rotation = new Vector3(0, yaw + (i % 2 == 0 ? -0.35f : 0.35f), 0);
            AddChild(figure);
            PlayerModel.Wear(figure, PlayerModel.Body, (ushort)(i % 3 == 1 ? 200 : 0));
        }
        var camera = new Camera3D { Fov = 35f, Current = true };
        AddChild(camera);
        if (args.ContainsKey("close"))
            camera.LookAtFromPosition(new Vector3(0, 1.75f, 2.2f), new Vector3(0, 1.55f, 0), Vector3.Up);
        else
            camera.LookAtFromPosition(new Vector3(0, 2.2f, 5.2f), new Vector3(0, 1.0f, -0.8f), Vector3.Up);
    }

    // Swaps every painted-shader material under node for its variant D counterpart,
    // keeping the colour each one carries.
    static void Repaint(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is MeshInstance3D mi && mi.Mesh != null)
            {
                if (mi.MaterialOverride is ShaderMaterial m)
                    mi.MaterialOverride = MaterialLook.For(MaterialLook.Classify(m, out var c), MaterialLook.Variant.D, c);
                else
                    for (int i = 0; i < mi.Mesh.GetSurfaceCount(); i++)
                        if ((mi.GetSurfaceOverrideMaterial(i) ?? mi.Mesh.SurfaceGetMaterial(i)) is ShaderMaterial sm)
                            mi.SetSurfaceOverrideMaterial(i, MaterialLook.For(MaterialLook.Classify(sm, out var c2), MaterialLook.Variant.D, c2));
            }
            Repaint(child);
        }
    }

    // The artist's card pine beside the 3D pine painted from it (Justin: "85% the
    // same"), then the 3D pine turned 90, 180 and 270 degrees.
    void PineTest()
    {
        var image = Image.LoadFromFile(ProjectSettings.GlobalizePath("res://../docs/art-packs/golden-skull/foliage/Forest_big_tree_needle_04.png"));
        image.GenerateMipmaps();
        var card = ImageTexture.CreateFromImage(image);
        const float tall = 6.5f;
        var sprite = new Sprite3D { Texture = card, PixelSize = tall / card.GetHeight(), Billboard = BaseMaterial3D.BillboardModeEnum.FixedY, Shaded = true, DoubleSided = true, AlphaCut = SpriteBase3D.AlphaCutMode.Discard, Centered = false, Offset = new Vector2(-card.GetWidth() * 0.5f, 0), Position = new Vector3(-6f, 0, 0) };
        AddChild(sprite);
        var material = MaterialLook.CardPine(card);
        for (int i = 0; i < 4; i++)
            AddChild(new MeshInstance3D { Mesh = PropMeshes.PaintedPine, MaterialOverride = material, Position = new Vector3(-2f + i * 4f, 0, 0), Scale = Vector3.One * tall, Rotation = new Vector3(0, i * Mathf.Pi / 2, 0) });
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(2f, 4.5f, 18f), new Vector3(2f, 3.0f, 0), Vector3.Up);
        if (args.ContainsKey("time-of-day")) AddChild(new Atmosphere());
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
