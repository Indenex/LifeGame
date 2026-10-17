using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The water in a set-down clay trough (ceramics-13): a disc that rises with the level,
// and a small label while the player is close. The basin is Building's StructureView.
public partial class TroughView : Node3D
{
    // Basin dimensions at PotView's scale: water sits between these heights.
    const float EmptyY = 0.03f, FullY = 0.20f;

    readonly MeshInstance3D water = new()
    {
        Mesh = new CylinderMesh { TopRadius = 1f, BottomRadius = 1f, Height = 0.004f, RadialSegments = 28 },
        MaterialOverride = ClayLook.TroughWater,
        Visible = false,
    };
    readonly Label3D label = new()
    {
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
        FontSize = 22,
        PixelSize = 0.005f,
        Modulate = Look.Cream,
        OutlineSize = 6,
        OutlineModulate = new Color(0.1f, 0.1f, 0.08f, 0.55f),
        Visible = false,
        NoDepthTest = true,
        Position = new Vector3(0, 0.55f, 0),
    };

    public int Litres { get; private set; }
    public int Capacity { get; private set; } = ClayRules.TroughMin;

    public bool ShowLabel { set => label.Visible = value; }

    public TroughView()
    {
        AddChild(water);
        AddChild(label);
    }

    public void Show(int litres, int capacity)
    {
        Litres = litres;
        Capacity = Mathf.Max(1, capacity);
        water.Visible = litres > 0;
        float t = Mathf.Clamp(litres / (float)Capacity, 0f, 1f);
        float y = Mathf.Lerp(EmptyY, FullY, t);
        water.Position = new Vector3(0, y, 0);
        // Follow the bowl's inside, a hair in so the clay edge shows round the water.
        float r = PotMesh.TroughInnerRadius(y / PotView.Size) * PotView.Size - 0.01f;
        water.Scale = new Vector3(r, 1f, r);
        label.Text = litres > 0 ? $"Trough  {litres}/{Capacity} L" : "Empty trough";
    }
}
