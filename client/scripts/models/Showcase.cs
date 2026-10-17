using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// A standalone scene that lines up the model styles side by side under the game's
// light, so the Render lane can capture them for Justin (model-1). No server.
//   godot --path client res://scripts/models/Showcase.tscn -- --view=row --shot=out.png
// Views: row (everyone), people, stylefaces (model-1 heads), faces (chibi face states), sheep, walk, items (--old for today's, --compare for today vs painted), held, clothes, stations, props, env, hearth, (people mid-stride, from the side).
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

        if (view is "items" or "held" or "clothes" or "hearth" or "stations" or "props" or "env" or "cooking" or "deer" or "hen" or "stonehearth" or "treelod" or "rocks" or "bushes" or "logs" or "wear" or "devices" or "crowd" or "roster" or "animals" or "structures" or "faces" or "pots" or "turntable" or "art")
        {
            if (view == "faces") ChibiFaces();
            else
            if (view == "hearth") Hearth();
            else if (view == "stations") Stations();
            else if (view == "props") Props();
            else if (view == "env") EnvProps();
            else if (view == "cooking") Cooking();
            else if (view == "deer") Deer();
            else if (view == "coop") Coop();
            else if (view == "hen") Hen();
            else if (view == "stonehearth") StoneHearthView();
            else if (view == "treelod") TreeLod();
            else if (view == "rocks") Rocks();
            else if (view == "structures") Structures(args.GetValueOrDefault("set", "homes"));
            else if (view == "animals") Animals();
            else if (view == "bushes") Bushes();
            else if (view == "logs") Logs();
            else if (view == "pots") Pots();
            else if (view == "wear") WearRows();
            else if (view == "devices") Devices();
            else if (view == "crowd") Crowd();
            else if (view == "items" && args.ContainsKey("compare")) PaintedItemRows();
            else if (view == "roster") Roster();
            else if (view == "turntable") Turntable();
            else if (view == "art") ArtPiece();
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
        float gap = view == "stylefaces" ? 0.75f : 2.2f;
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
            Label(view == "stylefaces" ? $"{style.Key}  {style.Name}"
                : view == "sheep" ? $"{style.Key}  {fleece}\n{SheepFigure.Build(fleece, wool).Triangles:N0} tris"
                : $"{style.Key}  {style.Name}\nperson {tris:N0} tris, sheep {SheepFigure.Build(fleece, wool).Triangles:N0}", new Vector3(x, view == "sheep" ? -1.1f : 0, 0));
        }
        if (view == "walk") Stride();
        // The faces view is close enough that the labels would cover the heads.
        if (view == "stylefaces")
            foreach (var child in GetChildren())
                if (child is Label3D label) label.Position += new Vector3(0, -2.15f + 0.1f, 0.3f);

        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        var (from, at) = view switch
        {
            "people" => (new Vector3(0f, 1.3f, 8.4f), new Vector3(0f, 0.95f, 0f)),
            "stylefaces" => (new Vector3(0f, 1.6f, 4.2f), new Vector3(0f, 1.45f, 0f)),
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
        // --ids=207,211 shows only those items.
        if (args.TryGetValue("ids", out var only))
        {
            var wanted = only.Split(',').Select(t => (LifeGame.Shared.Item)ushort.Parse(t)).ToHashSet();
            items.RemoveAll(i => !wanted.Contains(i));
        }
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

    // painted-6: representative items in pairs of rows, today's look behind, painted in front.
    void PaintedItemRows()
    {
        var items = new[]
        {
            Item.StoneAxe, Item.StoneKnife, Item.Stone, Item.HandAxe, (Item)18, Item.Spear, Item.Cordage,
            Item.Waterskin, Item.PinchPot, Item.StorageJar, Item.Egg, Item.Berries, Item.CookedMutton, Item.SheepHide,
        };
        const int columns = 7;
        const float step = 0.62f;
        for (int i = 0; i < items.Length; i++)
        {
            var painted = ItemLook.MeshFor(items[i]);
            float x = (i % columns - (columns - 1) / 2f) * step;
            float z = (i / columns) * 1.25f - 0.62f;
            // Each scaled to fill its cell, so small things (an egg) read as well as big ones.
            float fit = 0.5f / painted.GetAabb().Size.Length();
            AddChild(new MeshInstance3D { Mesh = PaintedItems.Unpaint(painted), Position = new Vector3(x, 0, z - 0.5f), Scale = Vector3.One * fit, Rotation = new Vector3(0, -0.5f, 0) });
            AddChild(new MeshInstance3D { Mesh = painted, Position = new Vector3(x, 0, z), Scale = Vector3.One * fit, Rotation = new Vector3(0, -0.5f, 0) });
            AddChild(new Label3D
            {
                Text = Items.All.TryGetValue(items[i], out var def) ? def.Name : "?",
                FontSize = 22, PixelSize = 0.0022f, Modulate = Look.Cream, OutlineSize = 6,
                OutlineModulate = new Color(0.1f, 0.1f, 0.08f, 0.8f),
                Position = new Vector3(x, 0.02f, z + 0.24f), Rotation = new Vector3(-Mathf.Pi / 2f + 0.9f, 0, 0),
            });
        }
        foreach (var (text, z) in new[] { ("today", -1.12f), ("painted", -0.62f), ("today", 0.13f), ("painted", 0.63f) })
            AddChild(new Label3D { Text = text, FontSize = 26, PixelSize = 0.0024f, Modulate = Look.Cream, OutlineSize = 6, Position = new Vector3(-2.3f, 0.02f, z), Rotation = new Vector3(-1.2f, 0, 0) });
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 3.6f, 2.4f), new Vector3(0, 0, -0.1f), Vector3.Up);
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

    // The hen coop (model-7) at each build stage, frame, walls and roof, left to right,
    // turned a little so the pop-hole, ramp and side show.
    void Coop()
    {
        int kind = (int)SpacetimeDB.Types.StructureKind.HenCoop;
        for (int c = 0; c < 3; c++)
            foreach (var piece in StructureModel.Pieces(kind))
                if (piece.Stage >= 0 && piece.Stage <= c)
                    AddChild(new MeshInstance3D { Mesh = piece.Mesh, MaterialOverride = piece.Material, Position = new Vector3(-2.6f + c * 2.6f, 0, 0), Rotation = new Vector3(0, 0.55f, 0) });
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 2.4f, 6.2f), new Vector3(0, 0.55f, 0), Vector3.Up);
    }

    // The hen in three feather colours (white, brown, grey, from the sheep's fleece
    // tints), one pecking and one flapping, beside a sheep and the player for scale.
    void Hen()
    {
        var tints = new[] { new Color(0.95f, 0.93f, 0.88f), new Color(0.62f, 0.4f, 0.24f), new Color(0.6f, 0.58f, 0.55f) };
        for (int i = 0; i < 5; i++)
        {
            var hen = HenFigure.Create(Look.Solid(tints[i % 3]));
            hen.Position = new Vector3(-1.1f + i * 0.55f, 0, 0.4f * (i % 2));
            hen.Rotation = new Vector3(0, 0.9f - i * 0.25f, 0);
            if (i == 3) hen.SetBonePoseRotation(HenFigure.HeadBone, Quaternion.FromEuler(new Vector3(1.2f, 0, 0)));
            if (i == 4)
                for (int side = 0; side < 2; side++)
                    hen.SetBonePoseRotation(HenFigure.FirstLegBone + 2 + side, Quaternion.FromEuler(new Vector3(0, 0, side == 0 ? -1.1f : 1.1f)));
            AddChild(hen);
        }
        var sheep = SheepFigure.Create(SheepFigure.Variant, Look.Solid(new Color(0.74f, 0.71f, 0.63f)));
        sheep.Position = new Vector3(2.2f, 0, -0.3f);
        sheep.Rotation = new Vector3(0, -0.8f, 0);
        AddChild(sheep);
        var camera = new Camera3D { Fov = 35f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0.3f, 1.1f, 3.4f), new Vector3(0.3f, 0.25f, 0), Vector3.Up);
    }

    // The stone-ringed hearth (model-9, D-286): its first course on the left, the
    // finished kerb on the right, the player beside for scale.
    void StoneHearthView()
    {
        var pieces = StructureModel.StoneHearth();
        for (int c = 0; c < 2; c++)
            foreach (var piece in pieces)
                if (piece.Stage >= 0 && piece.Stage <= c)
                    AddChild(new MeshInstance3D { Mesh = piece.Mesh, MaterialOverride = piece.Material, Position = new Vector3(-0.75f + c * 1.5f, 0, 0) });
        var figure = PlayerModel.Build();
        figure.Position = new Vector3(2.0f, 0, -0.4f);
        figure.Rotation = new Vector3(0, -0.6f, 0);
        AddChild(figure);
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0.3f, 1.9f, 3.0f), new Vector3(0.3f, 0.15f, 0), Vector3.Up);
    }

    // The painted trees as the world draws them (model-10): each variant's detailed
    // version in the front row and its far version behind, pines left, broadleaf right.
    void TreeLod()
    {
        for (int v = 0; v < PaintedTrees.Variants; v++)
            for (int far = 0; far < 2; far++)
            {
                var basis = Basis.FromScale(Vector3.One * 1.5f);
                AddChild(new MeshInstance3D { Mesh = PaintedTrees.Pine(v, far == 1), MaterialOverride = PaintedTrees.PineMaterial, Position = new Vector3(-11f + v * 2.8f, 0, -far * 4.5f), Basis = basis });
                AddChild(new MeshInstance3D { Mesh = PaintedTrees.Broadleaf(v, far == 1), MaterialOverride = PaintedTrees.BroadleafMaterial, Position = new Vector3(1.5f + v * 3f, 0, -far * 4.5f), Basis = basis });
            }
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        // --top: from high above, as the camera sees the crowns when zoomed out.
        if (args.ContainsKey("top")) camera.LookAtFromPosition(new Vector3(-4.5f, 19f, 9f), new Vector3(-4.5f, 2.5f, -2f), Vector3.Up);
        else camera.LookAtFromPosition(new Vector3(0f, 6f, 22f), new Vector3(0f, 3.0f, -2f), Vector3.Up);
        if (args.ContainsKey("time-of-day")) AddChild(new Atmosphere());
    }

    // Rocks as the world draws them (painted-1): today's material in the back row, the
    // painted one in front: boulders, outcrops and river stones.
    void Rocks()
    {
        void Place(Mesh mesh, Material material, Vector3 at, Color tint, float scale = 1f)
        {
            var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = mesh, InstanceCount = 1 };
            multi.SetInstanceTransform(0, new Transform3D(new Basis(Vector3.Up, 0.6f).Scaled(Vector3.One * scale), at));
            multi.SetInstanceColor(0, tint);
            AddChild(new MultiMeshInstance3D { Multimesh = multi, MaterialOverride = material });
        }
        for (int row = 0; row < 2; row++)
        {
            var material = row == 0 ? Look.RockSurface : Painted.Rock;
            float z = row == 0 ? -3.5f : 0.5f;
            for (int v = 0; v < PropMeshes.RockVariants; v++)
                Place(PropMeshes.Rock(v), material, new Vector3(-6.5f + v * 1.6f, 0, z), Look.WorldRock, 0.9f);
            for (int v = 0; v < 2; v++)
                Place(PropMeshes.Outcrop(v), material, new Vector3(0.6f + v * 2.8f, 0, z - 0.6f), Look.Rock);
            for (int v = 0; v < 4; v++)
                Place(PropMeshes.RiverStone(v), material, new Vector3(6.2f + v * 0.5f, 0, z + 0.6f), Look.Rock.Lightened(0.1f), 0.9f);
        }
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0.5f, 4.2f, 9.5f), new Vector3(0.5f, 0.3f, -1.2f), Vector3.Up);
        if (args.ContainsKey("time-of-day")) AddChild(new Atmosphere());
    }

    // Built structures (painted-5): today's materials in the back row, painted in front.
    // set=homes: lean-to, clay hut, log cabin; set=small: table, kiln, stone hearth, fire
    // ring, fence and jar.
    void Structures(string set)
    {
        bool homes = set != "small";
        var builds = homes
            ? new List<(System.Func<List<StructureModel.Piece>>, float)>
            {
                (() => StructureModel.Pieces((int)SpacetimeDB.Types.StructureKind.LeanTo), LifeGame.Shared.Structures.ScaleOf((int)SpacetimeDB.Types.StructureKind.LeanTo)),
                (() => StructureModel.Pieces((int)SpacetimeDB.Types.StructureKind.ClayHut), LifeGame.Shared.Structures.ScaleOf((int)SpacetimeDB.Types.StructureKind.ClayHut)),
                (() => StructureModel.Pieces((int)SpacetimeDB.Types.StructureKind.LogCabin), LifeGame.Shared.Structures.ScaleOf((int)SpacetimeDB.Types.StructureKind.LogCabin)),
            }
            : new List<(System.Func<List<StructureModel.Piece>>, float)>
            {
                (() => StructureModel.Pieces((int)SpacetimeDB.Types.StructureKind.CraftingTable), 1f),
                (() => StructureModel.Pieces((int)SpacetimeDB.Types.StructureKind.Kiln), 1f),
                (() => StructureModel.StoneHearth(), 1f),
                (() => new List<StructureModel.Piece> { new() { Mesh = HearthModel.Ring, Material = HearthModel.Ring.SurfaceGetMaterial(0) } }, 1f),
                (() => StructureModel.Pieces((int)SpacetimeDB.Types.StructureKind.Fence), 1f),
                (() => StructureModel.Pieces((int)SpacetimeDB.Types.StructureKind.StorageJar), 1f),
            };
        float gap = homes ? 6.5f : 2.0f, depth = homes ? 7.5f : 2.4f;
        for (int i = 0; i < builds.Count; i++)
            for (int row = 0; row < 2; row++)
            {
                var (pieces, scale) = builds[i];
                var root = new Node3D { Position = new Vector3((i - (builds.Count - 1) / 2f) * gap, 0, row == 0 ? -depth : 0), Rotation = new Vector3(0, 0.45f, 0), Scale = Vector3.One * scale };
                AddChild(root);
                foreach (var piece in pieces())
                    if (piece.Stage >= 0)
                    {
                        var material = row == 0 ? PaintedStructures.Unpainted(piece.Material) : piece.Material;
                        // --cutaway: the roofs half faded, as StructureView does from inside (D-168).
                        if (args.ContainsKey("cutaway") && piece.Role == StructureModel.PieceRole.Roof && material is ShaderMaterial solid)
                        {
                            var ghost = Painted.StructureGhostOf(solid) ?? Look.StructureGhost(solid);
                            ghost.SetShaderParameter("fade_override", 0.5f);
                            material = ghost;
                        }
                        root.AddChild(new MeshInstance3D { Mesh = piece.Mesh, MaterialOverride = material });
                    }
            }
        var figure = PlayerModel.Build();
        figure.Position = homes ? new Vector3(-gap * 0.5f, 0, 2.2f) : new Vector3(-gap * 3.3f, 0, 0.8f);
        AddChild(figure);
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        if (homes) camera.LookAtFromPosition(new Vector3(0, 7.5f, 15f), new Vector3(0, 1.0f, -3.2f), Vector3.Up);
        else camera.LookAtFromPosition(new Vector3(-0.6f, 3.9f, 8.2f), new Vector3(-0.6f, 0.3f, -1.2f), Vector3.Up);
        if (args.ContainsKey("time-of-day")) AddChild(new Atmosphere());
    }

    // The animals painted (painted-4): today's look in the back row, the painted one in
    // front: a sheep in two fleece colours, a deer hind and a stag, and two hens.
    void Animals()
    {
        ShaderMaterial Wool(Color color)
        {
            // As Sheep.WoolLook, before painting.
            var wool = Look.Solid(color);
            wool.SetShaderParameter("detail_scale", 1.5f);
            wool.SetShaderParameter("bump", 0.15f);
            wool.SetShaderParameter("cavity", 0.08f);
            return wool;
        }
        // The meshes are shared, so each animal wears its materials on its own instance.
        void Put(Skeleton3D animal, Material outer, Material inner, Vector3 at, float yaw)
        {
            foreach (var child in animal.GetChildren())
                if (child is MeshInstance3D mesh)
                {
                    mesh.SetSurfaceOverrideMaterial(0, outer);
                    if (mesh.Mesh.GetSurfaceCount() > 1) mesh.SetSurfaceOverrideMaterial(1, inner);
                }
            animal.Position = at;
            animal.Rotation = new Vector3(0, yaw, 0);
            AddChild(animal);
        }
        var cream = new Color(0.74f, 0.71f, 0.63f);
        var dark = new Color(0.34f, 0.27f, 0.22f);
        var coat = new Color(0.47f, 0.30f, 0.19f);
        var hens = new[] { new Color(0.95f, 0.93f, 0.88f), new Color(0.62f, 0.4f, 0.24f) };
        var oldSkin = Look.Solid(Colors.White);
        for (int row = 0; row < 2; row++)
        {
            bool painted = row == 1;
            float z = painted ? 0.6f : -2.4f;
            Material W(Color c) => painted ? PaintedAnimals.Wool(Wool(c)) : Wool(c);
            Material C(Color c) => painted ? PaintedAnimals.Coat(Look.Solid(c)) : Look.Solid(c);
            Material skin = painted ? PaintedAnimals.Skin : oldSkin;
            Put(SheepFigure.Create(SheepFigure.Variant, oldSkin), W(cream), skin, new Vector3(-3.3f, 0, z), 1.2f);
            Put(SheepFigure.Create(SheepFigure.Variant, oldSkin), W(dark), skin, new Vector3(-2.0f, 0, z + 0.3f), 0.9f);
            Put(DeerFigure.Create(false, oldSkin), C(coat), skin, new Vector3(-0.4f, 0, z), 1.3f);
            Put(DeerFigure.Create(true, oldSkin), C(coat), skin, new Vector3(1.4f, 0, z - 0.3f), 1.0f);
            for (int i = 0; i < 2; i++)
                Put(HenFigure.Create(oldSkin), W(hens[i]), skin, new Vector3(2.9f + i * 0.6f, 0, z + 0.5f), 0.7f - i * 0.6f);
            Label(painted ? "Painted" : "Today", new Vector3(-3.0f, painted ? -2.05f : -1.2f, z + (painted ? 1.0f : 0.4f)));
        }
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0f, 2.3f, 6.6f), new Vector3(0f, 0.55f, -0.9f), Vector3.Up);
        if (args.ContainsKey("time-of-day")) AddChild(new Atmosphere());
    }

    // Bushes as the world draws them (painted-2): today's in the back row (forest bush,
    // heather, gorse, a stand-in for the old berry bush's sphere lobes), the painted ones
    // in front, with a berry bush ripe and one picked.
    void Bushes()
    {
        void Place(Mesh mesh, Material material, Vector3 at, Color tint, float scale = 1f)
        {
            var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = mesh, InstanceCount = 1 };
            multi.SetInstanceTransform(0, new Transform3D(new Basis(Vector3.Up, 0.6f).Scaled(Vector3.One * scale), at));
            multi.SetInstanceColor(0, tint);
            AddChild(new MultiMeshInstance3D { Multimesh = multi, MaterialOverride = material });
        }
        var leaf = Look.Leaf.Lerp(Look.Pine, 0.2f);
        for (int row = 0; row < 2; row++)
        {
            bool painted = row == 1;
            float z = painted ? 0.6f : -2.6f;
            var foliage = painted ? Painted.Foliage : Look.Foliage;
            for (int i = 0; i < 3; i++)
            {
                var tint = leaf.Lerp(Look.Pine, i * 0.2f);
                if (painted) Place(PaintedBushes.Bush, Painted.Bush, new Vector3(-5.2f + i * 1.3f, 0, z + (i % 2) * 0.4f), PaintedBushes.Tint(tint), 0.8f + i * 0.2f);
                else Place(Undergrowth.Bush, Look.Foliage, new Vector3(-5.2f + i * 1.3f, 0, z + (i % 2) * 0.4f), Undergrowth.Olive(tint), 0.8f + i * 0.2f);
            }
            for (int v = 0; v < 2; v++)
                Place(PropMeshes.Heather(v), foliage, new Vector3(-1.2f + v * 0.8f, 0, z + 0.3f), Colors.White);
            for (int v = 0; v < 2; v++)
                Place(PropMeshes.Gorse(v), foliage, new Vector3(0.9f + v * 1.3f, 0, z), Colors.White);
        }
        // Today's berry bush: four faceted sphere lobes, berries left off.
        var oldLobe = Look.Faceted(new SphereMesh { Radius = 0.42f, Height = 0.6f, RadialSegments = 8, Rings = 4 }, new Color(0.20f, 0.33f, 0.14f));
        Vector3[] centers = { new(0, 0.3f, 0), new(0.3f, 0.22f, 0.1f), new(-0.25f, 0.2f, 0.18f), new(0.05f, 0.24f, -0.3f) };
        foreach (var c in centers) AddChild(new MeshInstance3D { Mesh = oldLobe, Position = new Vector3(4.4f, 0, -2.6f) + c, Scale = Vector3.One * 0.9f });
        AddChild(new BerryBush(424242) { Position = new Vector3(3.9f, 0, 0.7f), Ripe = true });
        AddChild(new BerryBush(77) { Position = new Vector3(5.2f, 0, 1.4f), Ripe = false });
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0.2f, 3.4f, 9.8f), new Vector3(0.2f, 0.3f, -0.6f), Vector3.Up);
        if (args.ContainsKey("time-of-day")) AddChild(new Atmosphere());
    }

    // Bark and leaves as the world draws them (painted-3): today's materials in the back
    // row, the painted ones in front: birches with their canopies and fallen logs.
    void Logs()
    {
        void Place(Mesh mesh, Material material, Transform3D at, Color tint)
        {
            var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = mesh, InstanceCount = 1 };
            multi.SetInstanceTransform(0, at);
            multi.SetInstanceColor(0, tint);
            AddChild(new MultiMeshInstance3D { Multimesh = multi, MaterialOverride = material });
        }
        for (int row = 0; row < 2; row++)
        {
            var bark = row == 0 ? Look.TreeBark : Painted.Bark;
            var leaves = row == 0 ? Look.TreeFoliage : Painted.Leaves;
            float z = row == 0 ? -5f : 0f;
            for (int v = 0; v < 3; v++)
            {
                var at = new Transform3D(new Basis(Vector3.Up, v * 1.3f).Scaled(Vector3.One * 0.6f), new Vector3(-7f + v * 3f, 0, z));
                Place(PropMeshes.Birch(v % PropMeshes.BirchVariants), bark, at.ScaledLocal(new Vector3(1f, 1.1f, 1f)), Colors.White.Darkened(v * 0.05f));
                if (row == 0) Place(Terrain.LeafyCanopy, leaves, at.ScaledLocal(new Vector3(0.6f, 1.1f, 0.6f)), Look.LeafLight.Lerp(Look.Leaf, v * 0.15f));
                else Place(PaintedTrees.BroadleafCrown(v, false), PaintedTrees.BroadleafMaterial, Terrain.BirchCrown(at), Terrain.BirchLeaf.Darkened(v * 0.05f));
            }
            for (int v = 0; v < PropMeshes.FallenLogVariants && v < 3; v++)
                Place(PropMeshes.FallenLog(v), bark, new Transform3D(new Basis(Vector3.Up, 0.3f + v * 0.5f), new Vector3(3.5f + v * 1.4f, 0, z + 1.5f + v * 0.5f)), Look.Bark.Darkened(0.1f));
        }
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0f, 7f, 18f), new Vector3(0f, 2f, -2.5f), Vector3.Up);
        if (args.ContainsKey("time-of-day")) AddChild(new Atmosphere());
    }

    // Ceramics' clay under ClayLook (painted-8): one row per form, wet, dry, pit-fired,
    // kiln-fired and cracked left to right, and a slip-banded coil pot at the end.
    void Pots()
    {
        var forms = new[] { ClayForm.CoilPot, ClayForm.PinchPot, ClayForm.Lamp };
        var looks = new[]
        {
            ClayLook.For(ClayStage.Drying, FireKind.Pit, 1f, 0), ClayLook.For(ClayStage.Drying, FireKind.Pit, 0f, 0),
            ClayLook.For(ClayStage.Fired, FireKind.Pit, 0f, 1), ClayLook.For(ClayStage.Fired, FireKind.Kiln, 0f, 2),
            ClayLook.For(ClayStage.Cracked, FireKind.Pit, 0f, 0),
        };
        for (int f = 0; f < forms.Length; f++)
        {
            for (int s = 0; s < looks.Length; s++)
                AddChild(new MeshInstance3D { Mesh = PotMesh.For(forms[f], (ulong)s), Scale = Vector3.One * PotView.Size, MaterialOverride = looks[s], Position = new Vector3((s - 2.5f) * 0.55f, 0, (f - 1) * 0.6f) });
        }
        var slip = ClayLook.Decorate(looks[3], ClayForm.CoilPot, -1, Colors.White, 3, 2);
        AddChild(new MeshInstance3D { Mesh = PotMesh.For(ClayForm.CoilPot, 9), Scale = Vector3.One * PotView.Size, MaterialOverride = slip, Position = new Vector3(3 * 0.55f, 0, -0.6f) });
        var camera = new Camera3D { Fov = 40f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, 1.6f, 2.6f), new Vector3(0, 0.1f, 0), Vector3.Up);
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
        camera.LookAtFromPosition(new Vector3(0, 2.2f, 5.2f), new Vector3(0, 1.0f, -0.8f), Vector3.Up);
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

    // The chibi face states (model-15), close up: Bramble's row above Pebble's, each
    // open, blink, happy and sleepy. --far steps the camera back to a play distance.
    void ChibiFaces()
    {
        float yaw = Mathf.DegToRad(float.Parse(args.GetValueOrDefault("yaw", "-12"), System.Globalization.CultureInfo.InvariantCulture));
        var races = new[] { new ChibiFigure.Look(ChibiFigure.Race.Bramble, 0, 2, 0), new ChibiFigure.Look(ChibiFigure.Race.Pebble, 0, 0, 0) };
        const float gap = 0.9f, lift = 1.55f;
        for (int r = 0; r < races.Length; r++)
            for (int f = 0; f < 4; f++)
            {
                var figure = ChibiFigure.Create(races[r]);
                figure.Position = new Vector3((f - 1.5f) * gap, r == 0 ? lift : 0f, 0);
                figure.Rotation = new Vector3(0, yaw, 0);
                AddChild(figure);
                PlayerModel.SetFace(figure, (Face)f);
                // The upper row floats; it casts no shadow onto the heads below.
                if (r == 0)
                    foreach (var child in figure.GetChildren())
                        if (child is GeometryInstance3D g) g.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
                if (r == 1) Label(((Face)f).ToString(), new Vector3((f - 1.5f) * gap, -1.17f, 0.4f));
            }
        var camera = new Camera3D { Fov = 35f, Current = true };
        AddChild(camera);
        float back = args.ContainsKey("far") ? 9f : 4f;
        camera.LookAtFromPosition(new Vector3(0f, 2f, back), new Vector3(0f, 1.95f, 0f), Vector3.Up);
        if (args.ContainsKey("time-of-day")) AddChild(new Atmosphere());
    }

    // The chibi player base (model-6) beside today's figure and a 0.75 m table:
    // Bramble and Pebble, each in a couple of looks, front and three-quarter.
    void Roster()
    {
        float yaw = Mathf.DegToRad(float.Parse(args.GetValueOrDefault("yaw", "-20"), System.Globalization.CultureInfo.InvariantCulture));
        var today = PlayerModel.Build();
        today.Position = new Vector3(-2.6f, 0, 0);
        today.Rotation = new Vector3(0, yaw, 0);
        AddChild(today);
        // --flat draws them with today's shader, to compare with the painted look.
        ChibiFigure.Painted = !args.ContainsKey("flat");
        var looks = new[]
        {
            new ChibiFigure.Look(ChibiFigure.Race.Bramble, 0, 2, 0), new ChibiFigure.Look(ChibiFigure.Race.Bramble, 3, 1, 1),
            new ChibiFigure.Look(ChibiFigure.Race.Pebble, 0, 0, 0), new ChibiFigure.Look(ChibiFigure.Race.Pebble, 1, 1, 1),
        };
        for (int i = 0; i < looks.Length; i++)
        {
            var figure = ChibiFigure.Create(looks[i]);
            // --dressed puts on the wool tunic and the foot wraps.
            if (args.ContainsKey("dressed"))
            {
                figure.GetNode(PlayerModel.SlotNode(PlayerModel.Body)).Free();
                foreach (var (slot, item) in new[] { (PlayerModel.Body, Garments.WoolTunic), (PlayerModel.Feet, Garments.FootWraps) })
                    if (ChibiFigure.Garment(looks[i], slot, item) is { } worn)
                        figure.AddChild(new MeshInstance3D { Mesh = worn, Skeleton = new NodePath("..") });
            }
            figure.Position = new Vector3(-1.6f + i * 1.25f, 0, 0);
            figure.Rotation = new Vector3(0, yaw + (i % 2 == 1 ? -0.6f : 0f), 0);
            AddChild(figure);
        }
        foreach (var piece in StationMeshes.CraftingTable())
            if (piece.Stage >= 0) AddChild(new MeshInstance3D { Mesh = piece.Mesh, MaterialOverride = piece.Material, Position = new Vector3(4f, 0, -0.3f) });
        var camera = new Camera3D { Fov = 35f, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0.6f, 1.7f, 7.4f), new Vector3(0.6f, 0.85f, 0), Vector3.Up);
    }

    // One chibi look turned through five views in a row (art-rnd-1): front three-quarter,
    // side, back three-quarter, back and the other side, close up, so a part can be
    // checked all the way round in one shot. --race=1 --main= --second= --style= pick the look.
    void Turntable()
    {
        int Arg(string name, int fallback) => int.TryParse(args.GetValueOrDefault(name, ""), out int v) ? v : fallback;
        var look = new ChibiFigure.Look((ChibiFigure.Race)Arg("race", 1), Arg("main", 0), Arg("second", 0), Arg("style", 1));
        // --built-shell draws Pebble with ChibiFigure's built shell instead of the authored one.
        ChibiShell.Enabled = !args.ContainsKey("built-shell");
        var yaws = new[] { 25f, -35f, -95f, -125f, 145f };
        // --variants: the same view of five looks instead (Pebble: the three shell
        // colours with scutes, then two worn smooth).
        bool variants = args.ContainsKey("variants");
        for (int i = 0; i < yaws.Length; i++)
        {
            var figure = ChibiFigure.Create(variants ? look with { Second = i % 3, Style = i < 3 ? 1 : 0 } : look);
            if (variants) yaws[i] = -95f;
            figure.Position = new Vector3((i - 2) * 0.95f, 0, 0);
            figure.Rotation = new Vector3(0, Mathf.DegToRad(yaws[i]), 0);
            AddChild(figure);
        }
        var camera = new Camera3D { Fov = 30f, Current = true };
        AddChild(camera);
        // --close: the middle three views, near.
        if (args.ContainsKey("close"))
            camera.LookAtFromPosition(new Vector3(0, 0.95f, 3.7f), new Vector3(0, 0.66f, 0), Vector3.Up);
        else
            camera.LookAtFromPosition(new Vector3(0, 1.1f, 6.2f), new Vector3(0, 0.62f, 0), Vector3.Up);
    }

    // Today's model beside its Blender-authored version (art-rnd-1), each turned through
    // three views: --view=art --piece=<name> finds the static class LifeGame.Art<Name>
    // (client/scripts/models/art/), which has Node3D Today(), Node3D Authored() and the
    // float fields Spacing (m between models) and Height (m, for framing).
    // --yaws=30,-60,-150 picks the views; --close frames the authored ones only.
    void ArtPiece()
    {
        string piece = args.GetValueOrDefault("piece", "tree");
        var type = System.Type.GetType("LifeGame.Art" + char.ToUpperInvariant(piece[0]) + piece[1..])
            ?? throw new System.ArgumentException($"no art piece {piece}");
        Node3D Make(string name) => (Node3D)type.GetMethod(name)!.Invoke(null, null)!;
        float spacing = (float)type.GetField("Spacing")!.GetValue(null)!;
        float height = (float)type.GetField("Height")!.GetValue(null)!;
        var yaws = args.GetValueOrDefault("yaws", "30,-60,-150").Split(',');
        bool close = args.ContainsKey("close");
        int n = yaws.Length, count = close ? n : 2 * n;
        for (int i = 0; i < count; i++)
        {
            var model = close || i >= n ? Make("Authored") : Make("Today");
            model.Position = new Vector3((i - (count - 1) * 0.5f) * spacing + (close || i < n ? 0 : spacing * 0.4f), 0, 0);
            model.Rotation = new Vector3(0, Mathf.DegToRad(float.Parse(yaws[i % n], System.Globalization.CultureInfo.InvariantCulture)), 0);
            AddChild(model);
        }
        float fov = 30f;
        float width = count * spacing + spacing * 0.6f;
        float distance = Mathf.Max(width * 0.5f / Mathf.Tan(Mathf.DegToRad(fov * 0.5f * 16f / 9f)), height * 0.5f / Mathf.Tan(Mathf.DegToRad(fov * 0.5f))) * 1.08f;
        var camera = new Camera3D { Fov = fov, Current = true };
        AddChild(camera);
        camera.LookAtFromPosition(new Vector3(0, height * 0.55f + distance * 0.12f, distance), new Vector3(0, height * 0.48f, 0), Vector3.Up);
    }
}
