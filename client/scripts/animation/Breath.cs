using Godot;

namespace LifeGame;

// A faint puff of breath from the mouth in cold air (D-144), on every player. One small
// particle burst every few seconds per avatar, so 32 players cost next to nothing.
public partial class Breath : CpuParticles3D
{
    // How cold the air is around here, 0 (no breath) to 1 (clearly visible). Survival
    // sets it; until then nobody breathes visibly.
    public static float Coldness;

    float wait;

    public Breath()
    {
        Emitting = false;
        OneShot = true;
        Amount = 6;
        Lifetime = 1.4f;
        Explosiveness = 0.75f;
        LocalCoords = false; // the puff stays in the air when the head moves on
        Direction = new Vector3(0, 0.15f, 1);
        Spread = 14f;
        InitialVelocityMin = 0.18f;
        InitialVelocityMax = 0.3f;
        Gravity = new Vector3(0, 0.05f, 0);
        DampingMin = DampingMax = 0.6f;
        ScaleAmountMin = 0.6f;
        ScaleAmountMax = 1f;
        ScaleAmountCurve = new Curve();
        ScaleAmountCurve.AddPoint(new Vector2(0, 0.4f));
        ScaleAmountCurve.AddPoint(new Vector2(1, 1.6f));
        var fade = new Gradient();
        fade.SetColor(0, new Color(1, 1, 1, 0.22f));
        fade.SetColor(1, new Color(1, 1, 1, 0));
        ColorRamp = fade;
        Mesh = new QuadMesh
        {
            Size = new Vector2(0.09f, 0.09f),
            Material = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
                VertexColorUseAsAlbedo = true,
                AlbedoColor = new Color(0.92f, 0.94f, 0.97f),
            },
        };
        wait = (float)GD.RandRange(0.0, 3.0);
    }

    public override void _Process(double delta)
    {
        wait -= (float)delta;
        if (wait > 0f) return;
        // A breath every 3 to 4 s; colder air shows more of them.
        wait = (float)GD.RandRange(3.0, 4.2);
        if (Coldness <= 0.01f || GD.Randf() > Coldness + 0.2f || !IsVisibleInTree()) return;
        Restart();
    }
}
