using System;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// A clay oil lamp on the ground (ceramics-9): the fired bowl, and while it burns a
// small flame at the spout and a warm, softly flickering light about 4 m round.
public partial class LampView : Node3D
{
    readonly MeshInstance3D body = new();
    readonly MeshInstance3D flame = new()
    {
        Mesh = new SphereMesh { Radius = 0.018f, Height = 0.06f, RadialSegments = 8, Rings = 4 },
        MaterialOverride = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(1f, 0.78f, 0.38f),
            EmissionEnabled = true,
            Emission = new Color(1f, 0.6f, 0.2f),
            EmissionEnergyMultiplier = 3f,
        },
    };
    readonly OmniLight3D light = new()
    {
        LightColor = new Color(1.0f, 0.66f, 0.32f),
        OmniRange = ClayRules.LampLightRadius,
        OmniAttenuation = 1.3f,
        LightEnergy = 0.9f,
        ShadowEnabled = false,
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
    };
    double burnsUntil;
    float time;

    public bool Lit => SecondsLeft > 0;
    public double SecondsLeft => Math.Max(0, burnsUntil - PotView.Now);
    public bool ShowLabel { set => label.Visible = value; }

    public LampView()
    {
        body.Scale = Vector3.One * PotView.Size;
        AddChild(body);
        AddChild(flame);
        AddChild(light);
        AddChild(label);
    }

    // `mark` is the lamp's maker (shared/Marks.cs), drawn as a stamp like the ones on a pot; 0 for none.
    public void Show(ulong id, double burnsUntil, uint mark = 0)
    {
        this.burnsUntil = burnsUntil;
        body.Mesh = PotMesh.For(ClayForm.Lamp, id);
        body.MaterialOverride = ClayLook.WithMark(ClayLook.For(ClayStage.Fired, FireKind.Pit, 0f, id), mark, ClayForm.Lamp);
        float top = PotMesh.Height(ClayForm.Lamp) * PotView.Size;
        // The wick sits at the rim, on the spout side.
        flame.Position = new Vector3(0.11f, top + 0.02f, 0);
        light.Position = new Vector3(0.11f, top + 0.12f, 0);
        label.Position = new Vector3(0, top + 0.35f, 0);
        Rotation = new Vector3(0, (id % 97) / 97f * Mathf.Tau, 0);
    }

    public override void _Process(double delta)
    {
        time += (float)delta;
        bool lit = Lit;
        flame.Visible = light.Visible = lit;
        if (lit)
        {
            float flicker = 0.85f + 0.1f * Mathf.Sin(time * 9.1f) + 0.05f * Mathf.Sin(time * 23.7f + 1.3f);
            light.LightEnergy = 0.9f * flicker;
            flame.Scale = new Vector3(1f, 0.9f + 0.2f * flicker, 1f);
        }
        if (label.Visible)
        {
            int left = (int)Math.Ceiling(SecondsLeft);
            label.Text = lit ? $"Lamp  {left / 60}:{left % 60:00}" : "Lamp, out";
        }
    }
}
