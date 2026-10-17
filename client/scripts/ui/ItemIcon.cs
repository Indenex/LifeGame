using System.Collections.Generic;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// A small picture of each item, rendered once from the same mesh that lies on the
// ground (ItemLook.MeshFor), so the inventory shows the things themselves. A pot with a
// maker's mark gets its own picture with the mark on (ui-46, Ceramics' ClayItemLook), so
// what you made looks like what you made in the bag too (D-081).
public static class ItemIcon
{
    const int Size = 96;
    static readonly Dictionary<(Item, uint), Texture2D> Icons = new();
    static Node? holder;

    // The node the little render viewports live under; Hud sets it.
    public static void Attach(Node node) => holder = node;

    // decor: the maker (Marks.MakerOf) of a marked pot, 0 for the plain picture.
    public static Texture2D? For(Item item, uint decor = 0)
    {
        var mesh = decor != 0 && ClayItemLook.MarkedMesh(item, decor) is { } marked ? marked : null;
        if (mesh == null) decor = 0;
        if (Icons.TryGetValue((item, decor), out var icon)) return icon;
        if (holder == null) return null;
        mesh ??= ItemLook.MeshFor(item);

        var viewport = new SubViewport
        {
            Size = new Vector2I(Size, Size),
            OwnWorld3D = true,
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
        };
        var aabb = mesh.GetAabb();
        var model = new MeshInstance3D { Mesh = mesh, Position = -aabb.GetCenter() };
        // A slight turn so flat things show their face and long things lie across.
        var pivot = new Node3D { Rotation = new Vector3(0, Mathf.DegToRad(35), 0) };
        pivot.AddChild(model);
        viewport.AddChild(pivot);

        float radius = Mathf.Max(aabb.Size.Length() * 0.5f, 0.05f);
        var camera = new Camera3D { Fov = 30, Current = true };
        var look = new Vector3(0, 0.75f, 1f).Normalized() * radius / Mathf.Sin(Mathf.DegToRad(15)) * 0.72f;
        viewport.AddChild(camera);
        camera.LookAtFromPosition(look, Vector3.Zero, Vector3.Up);

        // Warm key light and a soft fill, like the late sun in the world.
        viewport.AddChild(new DirectionalLight3D { LightColor = new Color(1f, 0.93f, 0.8f), LightEnergy = 1.3f, Rotation = new Vector3(Mathf.DegToRad(-50), Mathf.DegToRad(-35), 0) });
        viewport.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.ClearColor,
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.62f, 0.66f, 0.7f),
                AmbientLightEnergy = 0.9f,
            },
        });
        holder.AddChild(viewport);
        icon = viewport.GetTexture();
        Icons[(item, decor)] = icon;
        return icon;
    }
}
