using Godot;

namespace LifeGame;

// Stamina (D-088, D-101): a small bar just under your character, shown only
// while you are short of breath. It fades in quickly when you start running
// and out gently once the bar is full again. It hides while a panel is open.
public partial class StaminaBar : CanvasLayer
{
    const float Width = 46f, Height = 5f;
    // How far below the feet, on screen, the bar sits.
    const float Drop = 16f;
    static readonly Color Fill = new(0.86f, 0.84f, 0.66f);
    static readonly Color Track = new(0f, 0f, 0f, 0.35f);

    readonly Control bar = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Size = new Vector2(Width, Height) };
    float value = 1f, shown = 1f, alpha;

    public StaminaBar()
    {
        Layer = 2;
    }

    public override void _Ready()
    {
        bar.Modulate = new Color(1, 1, 1, 0);
        bar.Draw += DrawBar;
        AddChild(bar);
    }

    // The current stamina (0..1) and where your feet are on screen (null when off screen).
    public void Set(float stamina, Vector2? feet)
    {
        value = stamina;
        if (feet is { } at)
            bar.Position = (at + new Vector2(-Width / 2f, Drop)).Round();
        bar.Visible = feet != null && bar.Modulate.A > 0.01f;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        float target = value < 0.995f && !Panels.Open() ? 1f : 0f;
        alpha = Mathf.MoveToward(alpha, target, dt * (target > 0f ? 5f : 1.5f));
        bar.Modulate = new Color(1, 1, 1, alpha);
        if (Mathf.Abs(shown - value) > 0.001f || alpha > 0f)
        {
            shown = Mathf.Lerp(shown, value, 1f - Mathf.Exp(-12f * dt));
            bar.QueueRedraw();
        }
    }

    void DrawBar()
    {
        var full = new Rect2(Vector2.Zero, new Vector2(Width, Height));
        bar.DrawRect(full.Grow(1f), Track);
        if (shown > 0.005f)
            bar.DrawRect(new Rect2(Vector2.Zero, new Vector2(Width * shown, Height)), Fill);
    }
}
