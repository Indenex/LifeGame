using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The spinning minigame (Justin, D-070), plugged into the crafting panel through
// AnimalsMinigameFor.
public partial class CraftingUi
{
    partial void AnimalsMinigameFor(ProcessDef def, ref Minigame_? game)
    {
        if (def.Minigame == Minigame.Spin) game = new SpinGame(def);
    }

    // Spin yarn on a drop spindle: flick it with Space and it whirls, drawing the
    // wool into yarn, then slows. Flick again as it slows into the bright band.
    // Flick too soon and the yarn kinks; wait too long and the spindle drops and the
    // yarn breaks back a little. About six good flicks fill the spindle (fewer with skill).
    sealed class SpinGame : Minigame_
    {
        // Slowing per second at full spin: from a flick to the band takes about two seconds.
        const float Slowing = 0.32f;
        const float BandLow = 0.12f, BandHigh = 0.35f;
        readonly int flicks;
        readonly float limit;
        readonly List<float> marks = new();
        readonly ColorRect band = new() { Color = new Color(1f, 0.92f, 0.6f, 0.45f), MouseFilter = Control.MouseFilterEnum.Ignore };
        readonly ProgressBar yarnBar = new() { MinValue = 0, MaxValue = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(420, 8) };
        readonly SpindleSketch sketch = new();
        // Yarn drawn per unit of spin, so `flicks` well-timed flicks fill the spindle.
        readonly float draw;
        float spin, yarn, elapsed;
        bool started;

        public SpinGame(ProcessDef process) : base(process, "Press Space to flick the spindle. Flick again as it slows into the bright band.")
        {
            flicks = Steps(process, 6);
            limit = flicks * 3f + 8f;
            // Spin from a flick at 1 down to the middle of the band, integrated.
            float mid = (BandLow + BandHigh) / 2f;
            draw = 1f / (flicks * (1f - mid * mid) / (2f * Slowing));
            Hint.Text = $"Press Space to flick the spindle. Flick again as it slows into the bright band. {Times(flicks, "flick", "flicks")}.    Esc gives up.";
            float w = Bar.CustomMinimumSize.X;
            band.Position = new Vector2(BandLow * w, 0);
            band.Size = new Vector2((BandHigh - BandLow) * w, Bar.CustomMinimumSize.Y);
            Bar.AddChild(band);
            yarnBar.Modulate = new Color(0.95f, 0.9f, 0.78f);
            var box = (VBoxContainer)Root.GetChild(0);
            box.AddChild(sketch);
            box.MoveChild(sketch, 1);
            box.AddChild(yarnBar);
            box.MoveChild(yarnBar, 3);
        }

        public override void Key(Key key)
        {
            if (key != Godot.Key.Space) return;
            if (!started)
            {
                started = true;
                Title.Text = "The spindle whirls";
            }
            else if (spin > BandHigh)
            {
                // Too soon: the extra twist runs up into the yarn and kinks it.
                marks.Add(Math.Clamp(0.6f - (spin - BandHigh), 0.1f, 0.6f));
                Title.Text = "Too soon. The yarn kinks";
            }
            else if (spin > 0f)
            {
                marks.Add(1f);
                Title.Text = "An even thread";
            }
            else Title.Text = "The spindle whirls again";
            spin = 1f;
            sketch.Flick();
        }

        public override void Tick(float dt)
        {
            elapsed += dt;
            if (started && spin > 0f)
            {
                yarn = Math.Min(1f, yarn + spin * draw * dt);
                spin = Math.Max(0f, spin - Slowing * dt);
                if (spin == 0f)
                {
                    // Dropped: the thread snaps back a little and the turn is lost.
                    marks.Add(0f);
                    yarn = Math.Max(0f, yarn - 0.5f / flicks);
                    Title.Text = "It wobbles and drops. Flick it again";
                    sketch.Drop();
                }
                else if (spin <= BandHigh && Title.Text != "Slowing. Flick it")
                    Title.Text = "Slowing. Flick it";
            }
            Bar.Value = spin;
            yarnBar.Value = yarn;
            sketch.Spin = spin;
            sketch.Yarn = yarn;

            if (yarn >= 1f || elapsed >= limit)
            {
                float timing = marks.Count > 0 ? marks.Average() : 0.5f;
                // Out of time: what was spun counts for its share.
                Score = yarn >= 1f ? timing : timing * yarn;
                Done = true;
            }
        }
    }

    // A drop spindle seen from the side: a shaft with a stone whorl near the bottom,
    // the yarn winding on above it as a growing cop, and a thread rising to the wool.
    sealed partial class SpindleSketch : Control
    {
        public float Spin, Yarn;
        float turn, wobble;

        public SpindleSketch()
        {
            CustomMinimumSize = new Vector2(420, 110);
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public void Flick() => wobble = 0f;
        public void Drop() => wobble = 1f;

        public override void _Process(double delta)
        {
            turn += (float)delta * Spin * 18f;
            wobble = Math.Max(0f, wobble - (float)delta * 0.8f);
            QueueRedraw();
        }

        public override void _Draw()
        {
            var cream = new Color(Look.Cream, 0.9f);
            var wood = new Color(0.62f, 0.47f, 0.3f);
            var stone = new Color(0.55f, 0.53f, 0.5f);
            var wool = new Color(0.93f, 0.9f, 0.82f);
            float cx = Size.X / 2f + MathF.Sin(turn * 0.5f) * wobble * 10f, top = 6f, bottom = Size.Y - 6f;
            // The thread up to the wool in the spinner's hand.
            DrawLine(new Vector2(cx, 0), new Vector2(cx, top + 6f), new Color(wool, 0.8f), 1.5f);
            // Shaft.
            DrawLine(new Vector2(cx, top + 6f), new Vector2(cx, bottom), wood, 3f);
            // The cop of spun yarn, fatter as the spindle fills.
            float copHalf = 3f + Yarn * 14f, copTop = top + 18f, copBottom = bottom - 30f;
            DrawRect(new Rect2(cx - copHalf, copTop, copHalf * 2f, copBottom - copTop), wool);
            for (int i = 0; i < 6; i++)
            {
                // Winding lines drift with the turn, so the cop visibly spins.
                float y = copTop + ((i / 6f + turn * 0.05f) % 1f) * (copBottom - copTop);
                DrawLine(new Vector2(cx - copHalf, y), new Vector2(cx + copHalf, y + 4f), new Color(0.8f, 0.76f, 0.66f), 1f);
            }
            // The whorl, an ellipse with marks that sweep round as it turns.
            var whorl = new Vector2(cx, bottom - 16f);
            DrawSetTransform(whorl, 0f, new Vector2(1f, 0.32f));
            DrawCircle(Vector2.Zero, 30f, stone);
            for (int i = 0; i < 4; i++)
            {
                float a = turn + i * MathF.PI / 2f;
                DrawLine(Vector2.Zero, new Vector2(MathF.Cos(a), MathF.Sin(a)) * 28f, new Color(0.4f, 0.38f, 0.36f), 4f);
            }
            DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
            DrawLine(new Vector2(cx - 6f, top + 6f), new Vector2(cx + 6f, top + 2f), cream, 1.5f);
        }
    }
}
