using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The emote wheel (ui-48, D-323: wave, cheer, a little dance and a bow). Hold Q and a round
// paper wheel opens in the middle of the screen with Animation's emotes (shared/Emotes.cs) on
// petals around it; move the mouse towards one and let go of Q to do it. A quick tap of Q
// leaves the wheel open, so you can click a petal instead. Esc, or a click off the petals,
// closes it. Main sends the emote (Play); the server checks it and everyone sees it. A new
// emote in Emotes.All gets a petal of its own, with a star until it has a doodle here.
public partial class EmoteWheel : CanvasLayer
{
    static IReadOnlyList<EmoteDef> All => Emotes.All;

    public Action<EmoteDef> Play = _ => { };
    // True while something else has the screen (a panel, placing, typing).
    public Func<bool> Blocked = () => false;
    // True when another player stands close: the moment to first teach the Q key (Hints).
    public Func<bool> SomeoneNear = () => false;
    float nearCheck;
    bool hinted;

    public bool IsOpen => wheel.Visible;

    // Under this many pixels from the middle, nothing is picked yet.
    const float DeadZone = 34f;
    // A press shorter than this leaves the wheel open for clicking.
    const float TapTime = 0.25f;

    WheelDrawing wheel = null!;
    float heldFor;
    bool holding;

    public override void _Ready()
    {
        Layer = 4;
        wheel = new WheelDrawing { Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        wheel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        wheel.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            if (wheel.Hovered is { } picked) Do(picked);
            else Close();
        };
        AddChild(wheel);
        if (OS.GetCmdlineUserArgs().Contains("--screenshot-emotes")) Callable.From(() => Open(0)).CallDeferred();
    }

    void Open(int? hovered = null)
    {
        wheel.Hovered = hovered;
        wheel.Visible = true;
        wheel.Opened();
    }

    void Close()
    {
        wheel.Visible = false;
        holding = false;
    }

    // The server refuses emotes closer together than this; don't ask it to.
    ulong nextAllowed;

    void Do(int i)
    {
        Close();
        if (Time.GetTicksMsec() < nextAllowed) return;
        nextAllowed = Time.GetTicksMsec() + (ulong)(Emotes.CooldownMicros / 1000);
        Play(All[i]);
    }

    public override void _Process(double delta)
    {
        if (!hinted && (nearCheck -= (float)delta) <= 0f)
        {
            nearCheck = 1f;
            if (SomeoneNear()) { hinted = true; Hints.Request("emote"); }
        }
        if (!IsOpen) return;
        if (Blocked()) { Close(); return; }
        if (holding) heldFor += (float)delta;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("emote") && !e.IsEcho())
        {
            if (IsOpen) { Close(); GetViewport().SetInputAsHandled(); return; }
            if (Blocked()) return;
            holding = true;
            heldFor = 0f;
            Open();
            GetViewport().SetInputAsHandled();
        }
        else if (e.IsActionReleased("emote") && holding)
        {
            holding = false;
            // A held press picks what the mouse points at; a tap leaves the wheel up to click.
            if (heldFor >= TapTime || wheel.Hovered != null)
            {
                if (wheel.Hovered is { } picked) Do(picked);
                else Close();
            }
            GetViewport().SetInputAsHandled();
        }
        else if (IsOpen && e.IsActionPressed("ui_cancel"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    // The wheel itself: a paper disc with four round petals, each with a little doodle. The
    // petal the mouse points at grows a touch and gets a coral ring.
    partial class WheelDrawing : Control
    {
        public int? Hovered;
        const float Petal = 50f;
        // The ring widens as emotes are added, so petals never overlap (seven fit at about 132).
        static float Ring => Mathf.Max(112f, Petal * 1.15f / Mathf.Sin(Mathf.Pi / Mathf.Max(All.Count, 1)));
        float[] grow = new float[All.Count];
        float open, time;

        public void Opened()
        {
            open = Bounce.Reduce ? 1f : 0f;
            grow = new float[All.Count];
        }

        Vector2 Middle => Size / 2;

        // The first on top, then clockwise (four: Wave top, Cheer right, Dance below, Bow left).
        static Vector2 Dir(int i) => Vector2.FromAngle(-Mathf.Pi / 2 + i * Mathf.Tau / All.Count);

        public override void _Process(double delta)
        {
            if (!Visible) return;
            float dt = (float)delta;
            time += dt;
            open = Mathf.MoveToward(open, 1f, dt / 0.16f);
            var mouse = GetLocalMousePosition() - Middle;
            if (mouse.Length() > DeadZone)
            {
                // The nearest petal by angle.
                int best = 0;
                float bestDot = float.MinValue;
                for (int i = 0; i < All.Count; i++)
                {
                    float dot = Dir(i).Dot(mouse.Normalized());
                    if (dot > bestDot) { bestDot = dot; best = i; }
                }
                Hovered = best;
            }
            for (int i = 0; i < All.Count; i++)
                grow[i] = Mathf.MoveToward(grow[i], Hovered == i ? 1f : 0f, dt / 0.1f);
            QueueRedraw();
        }

        public override void _Draw()
        {
            // A soft pop: from a little small to a touch too big, settling.
            float t = open;
            float pop = Bounce.Reduce ? 1f : 0.85f + 0.15f * (1f + 2.2f * Mathf.Pow(t - 1f, 3) + 1.2f * Mathf.Pow(t - 1f, 2));
            var c = Middle;
            float alpha = Mathf.Clamp(t * 1.6f, 0f, 1f);

            // A faint shadow, then the paper disc behind the petals.
            DrawCircle(c + new Vector2(0, 6), (Ring + Petal + 14) * pop, new Color(0.15f, 0.08f, 0.05f, 0.22f * alpha));
            DrawCircle(c, (Ring + Petal + 12) * pop, new Color(WarmLook.Paper, 0.92f * alpha));
            Wobbly(c, (Ring + Petal + 12) * pop, new Color(WarmLook.Ink, 0.25f * alpha), 3f, 64);

            var font = Ui.Font(800);
            for (int i = 0; i < All.Count; i++)
            {
                var motion = All[i].Motion;
                string name = All[i].Name;
                var at = c + Dir(i) * Ring * pop;
                float r = Petal * pop * (1f + 0.12f * grow[i]);
                var ground = WarmLook.Paper.Darkened(0.06f).Lerp(WarmLook.Butter.Lightened(0.45f), grow[i]);
                DrawCircle(at, r, new Color(ground, alpha));
                Wobbly(at, r, new Color(WarmLook.Ink.Lerp(WarmLook.Coral, grow[i]), (0.35f + 0.65f * grow[i]) * alpha), 2.5f + grow[i], 32);
                // The bob only on the chosen one.
                var icon = at + new Vector2(0, -8 * pop + Mathf.Sin(time * 6f) * 2f * grow[i]);
                Doodle(motion, name, icon, pop * (1f + 0.12f * grow[i]), new Color(WarmLook.Ink, alpha));
                var size = font.GetStringSize(name, HorizontalAlignment.Center, -1, 15);
                DrawString(font, at + new Vector2(-size.X / 2, r * 0.62f), name, HorizontalAlignment.Center, -1, 15, new Color(WarmLook.Ink, alpha));
            }

            // The middle says what letting go will do.
            string middle = Hovered is { } h ? All[h].Name : "Q";
            int fontSize = Hovered != null ? 20 : 16;
            var ms = font.GetStringSize(middle, HorizontalAlignment.Center, -1, fontSize);
            DrawCircle(c, 30 * pop, new Color(WarmLook.Paper.Darkened(0.04f), alpha));
            DrawString(font, c + new Vector2(-ms.X / 2, ms.Y * 0.3f), middle, HorizontalAlignment.Center, -1, fontSize,
                new Color(Hovered != null ? WarmLook.Coral.Darkened(0.25f) : WarmLook.Ink, alpha));
        }

        // A hand-drawn ring: a circle with a gentle wobble, so it reads as pencil, not a compass.
        void Wobbly(Vector2 c, float r, Color colour, float width, int steps)
        {
            var points = new Vector2[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float a = i * Mathf.Tau / steps;
                float wobble = 1f + 0.012f * Mathf.Sin(a * 3f + 0.7f) + 0.008f * Mathf.Sin(a * 5f);
                points[i] = c + Vector2.FromAngle(a) * r * wobble;
            }
            DrawPolyline(points, colour, width, true);
        }

        // Little ink doodles, one per emote, drawn around `c` at scale `s`.
        void Doodle(WorkMotion e, string name, Vector2 c, float s, Color ink)
        {
            const float w = 2.6f;
            switch (e)
            {
                case WorkMotion.Wave:
                    // An open hand with three motion lines.
                    DrawArc(c + new Vector2(0, 4) * s, 8 * s, 0f, Mathf.Tau, 20, ink, w, true);
                    for (int f = 0; f < 4; f++)
                    {
                        var root = c + new Vector2(-6 + f * 4, -2) * s;
                        DrawLine(root, root + new Vector2(-1.5f + f, -10 + Mathf.Abs(f - 1.5f) * 2) * s, ink, w, true);
                    }
                    DrawLine(c + new Vector2(8, 6) * s, c + new Vector2(14, 0) * s, ink, w, true);
                    for (int m = 0; m < 3; m++)
                        DrawArc(c + new Vector2(-4, 2) * s, (18 + m * 4) * s, -2.6f, -2.0f, 6, new Color(ink, 0.6f), 2f, true);
                    break;
                case WorkMotion.Cheer:
                    // A little figure with both arms up, and a sparkle.
                    DrawCircle(c + new Vector2(0, -8) * s, 5 * s, ink);
                    DrawLine(c + new Vector2(0, -3) * s, c + new Vector2(0, 9) * s, ink, w, true);
                    DrawLine(c + new Vector2(0, 0) * s, c + new Vector2(-9, -12) * s, ink, w, true);
                    DrawLine(c + new Vector2(0, 0) * s, c + new Vector2(9, -12) * s, ink, w, true);
                    DrawLine(c + new Vector2(0, 9) * s, c + new Vector2(-6, 17) * s, ink, w, true);
                    DrawLine(c + new Vector2(0, 9) * s, c + new Vector2(6, 17) * s, ink, w, true);
                    Sparkle(c + new Vector2(14, -16) * s, 4 * s, new Color(WarmLook.Butter.Darkened(0.2f), ink.A));
                    Sparkle(c + new Vector2(-15, -18) * s, 3 * s, new Color(WarmLook.Coral, ink.A));
                    break;
                case WorkMotion.Dance:
                    // Two music notes, one tipped.
                    Note(c + new Vector2(-7, 4) * s, s, ink);
                    Note(c + new Vector2(7, -2) * s, s * 0.85f, new Color(WarmLook.Coral.Darkened(0.15f), ink.A));
                    break;
                case WorkMotion.Bow:
                    // A figure bowing from the waist, with a small heart.
                    DrawCircle(c + new Vector2(-10, -1) * s, 5 * s, ink);
                    DrawLine(c + new Vector2(-5, -2) * s, c + new Vector2(6, -6) * s, ink, w, true);
                    DrawLine(c + new Vector2(6, -6) * s, c + new Vector2(6, 14) * s, ink, w, true);
                    DrawLine(c + new Vector2(0, -4) * s, c + new Vector2(-2, 6) * s, ink, w, true);
                    Heart(c + new Vector2(12, -15) * s, 4 * s, new Color(WarmLook.Coral, ink.A));
                    break;
                // anim-18 (#705) emotes, matched by name so they draw as soon as they're in the list.
                case var _ when name == "Clap":
                    // Two palms meeting, with little burst lines.
                    DrawSetTransform(c + new Vector2(-4, 2) * s, 0.35f, Vector2.One);
                    DrawArc(Vector2.Zero, 7 * s, 0f, Mathf.Tau, 18, ink, w, true);
                    DrawSetTransform(c + new Vector2(4, 2) * s, -0.35f, Vector2.One);
                    DrawArc(Vector2.Zero, 7 * s, 0f, Mathf.Tau, 18, ink, w, true);
                    DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
                    for (int b = 0; b < 3; b++)
                    {
                        var d = Vector2.FromAngle(-Mathf.Pi / 2 + (b - 1) * 0.55f);
                        DrawLine(c + d * 13 * s, c + d * 18 * s, new Color(WarmLook.Butter.Darkened(0.25f), ink.A), 2.2f, true);
                    }
                    break;
                case var _ when name == "Shrug":
                    // A head between raised shoulders, both palms up, and a small question mark.
                    DrawCircle(c + new Vector2(0, -4) * s, 5 * s, ink);
                    DrawPolyline(new[] { c + new Vector2(-15, -6) * s, c + new Vector2(-9, 4) * s, c + new Vector2(9, 4) * s, c + new Vector2(15, -6) * s }, ink, w, true);
                    DrawLine(c + new Vector2(-15, -6) * s, c + new Vector2(-19, -8) * s, ink, w, true);
                    DrawLine(c + new Vector2(15, -6) * s, c + new Vector2(19, -8) * s, ink, w, true);
                    DrawLine(c + new Vector2(0, 4) * s, c + new Vector2(0, 14) * s, ink, w, true);
                    DrawArc(c + new Vector2(12, -18) * s, 3 * s, -Mathf.Pi, Mathf.Pi * 0.4f, 10, new Color(WarmLook.Coral, ink.A), 2f, true);
                    DrawCircle(c + new Vector2(12, -11) * s, 1.2f * s, new Color(WarmLook.Coral, ink.A));
                    break;
                case var _ when name == "Point":
                    // A fist with the first finger out, and a dotted line to where it points.
                    DrawArc(c + new Vector2(-8, 2) * s, 6 * s, 0f, Mathf.Tau, 16, ink, w, true);
                    DrawLine(c + new Vector2(-3, -1) * s, c + new Vector2(8, -1) * s, ink, w, true);
                    for (int d = 0; d < 3; d++)
                        DrawCircle(c + new Vector2(12 + d * 4, -1) * s, 1.2f * s, new Color(WarmLook.Coral, ink.A));
                    DrawPolyline(new[] { c + new Vector2(20, -5) * s, c + new Vector2(24, -1) * s, c + new Vector2(20, 3) * s }, new Color(WarmLook.Coral, ink.A), 2f, true);
                    break;
                default:
                    // A new emote without a doodle yet: a star.
                    var star = new Vector2[11];
                    for (int p = 0; p < 11; p++)
                        star[p] = c + Vector2.FromAngle(-Mathf.Pi / 2 + p * Mathf.Pi / 5) * (p % 2 == 0 ? 14 : 6) * s;
                    DrawPolyline(star, ink, w, true);
                    break;
            }
        }

        void Note(Vector2 at, float s, Color ink)
        {
            DrawSetTransform(at, -0.25f, Vector2.One);
            DrawCircle(new Vector2(0, 8) * s, 4.5f * s, ink);
            DrawLine(new Vector2(4, 8) * s, new Vector2(4, -10) * s, ink, 2.6f, true);
            DrawLine(new Vector2(4, -10) * s, new Vector2(10, -6) * s, ink, 2.6f, true);
            DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        }

        void Sparkle(Vector2 at, float r, Color colour)
        {
            DrawLine(at + new Vector2(0, -r), at + new Vector2(0, r), colour, 2f, true);
            DrawLine(at + new Vector2(-r, 0), at + new Vector2(r, 0), colour, 2f, true);
        }

        void Heart(Vector2 at, float r, Color colour)
        {
            DrawCircle(at + new Vector2(-r * 0.5f, 0), r * 0.6f, colour);
            DrawCircle(at + new Vector2(r * 0.5f, 0), r * 0.6f, colour);
            DrawColoredPolygon(new[] { at + new Vector2(-r * 1.05f, r * 0.2f), at + new Vector2(r * 1.05f, r * 0.2f), at + new Vector2(0, r * 1.3f) }, colour);
        }
    }
}
