using Godot;
using LifeGame.Shared;

namespace LifeGame;

// One piece of worked clay in the world. Its color follows its moisture as it
// dries, so you can see from across the camp which pots are ready to fire.
public partial class PotView : Node3D
{
    // Pots are drawn a little larger than life so they read from the follow camera.
    public const float Size = 1.6f;

    readonly MeshInstance3D body = new();
    readonly MeshInstance3D water = new();
    ClayForm form;
    ClayStage stage;
    FireKind fire;
    ulong id;
    int lastStep = -1;

    // Drying is predicted locally from the moisture the server stored and its rate.
    float moisture, rate;
    double since;

    public PotView()
    {
        AddChild(body);
        AddChild(water);
        Scale = Vector3.One * Size;
    }

    public void Show(ulong potId, ClayForm potForm, ClayStage potStage, FireKind potFire, float potMoisture, float dryRate, bool full)
    {
        id = potId;
        form = potForm;
        stage = potStage;
        fire = potFire;
        moisture = potMoisture;
        rate = dryRate;
        since = Time.GetTicksMsec() / 1000.0;
        body.Mesh = PotMesh.For(form, id);
        Rotation = new Vector3(0, (id % 97) / 97f * Mathf.Tau, 0);
        lastStep = -1;
        Refresh();

        var line = PotMesh.WaterLine(form);
        water.Visible = full && stage == ClayStage.Fired && line.X > 0f;
        if (water.Visible)
        {
            water.Mesh = new CylinderMesh { TopRadius = line.X, BottomRadius = line.X, Height = 0.002f, RadialSegments = 24 };
            water.MaterialOverride = ClayLook.Water;
            water.Position = new Vector3(0, line.Y, 0);
        }
    }

    public float Moisture => ClayRules.MoistureAt(moisture, rate, (float)(Time.GetTicksMsec() / 1000.0 - since));

    public override void _Process(double delta)
    {
        if (stage == ClayStage.Drying) Refresh();
    }

    void Refresh()
    {
        float now = Moisture;
        int step = Mathf.RoundToInt(now * 10f);
        if (step == lastStep) return;
        lastStep = step;
        body.MaterialOverride = ClayLook.For(stage, fire, now, id);
    }
}
