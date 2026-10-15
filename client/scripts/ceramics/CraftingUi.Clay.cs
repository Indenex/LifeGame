using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LifeGame.Shared;

namespace LifeGame;

// The clay minigames, plugged into the crafting panel (Justin, D-014).
public partial class CraftingUi
{
    // For screenshots: opens a clay minigame part way through, without a server.
    public void DemoClayMinigame(string processId, float progress)
    {
        if (Processes.Find(processId) is not { } def) return;
        Minigame_? game = null;
        MinigameFor(def, ref game);
        if (game == null) return;
        minigame = game;
        AddChild(game.Root);
        if (game is ShapeGame shape) shape.Demo(progress);
    }

    partial void MinigameFor(ProcessDef def, ref Minigame_? game)
    {
        if (def.Minigame == Minigame.Shape) game = new ShapeGame(def);
        else if (def.Minigame == Minigame.Coil) game = new CoilGame(def);
    }

    // Pinch a pot: the ball of clay turns in your hands. Hold Space to press the
    // wall thinner and taller, and let go while the wall is in the sweet spot.
    // Too far and the wall tears. Three pinches around the pot.
    sealed class ShapeGame : Minigame_
    {
        const int Pulls = 3;
        readonly List<float> pulls = new();
        readonly ColorRect zone = new() { Color = new Color(1f, 0.92f, 0.6f, 0.45f), MouseFilter = Control.MouseFilterEnum.Ignore };
        readonly PotSketch sketch = new(ClayForm.PinchPot);
        float wall = 1f, low, high, speed, pause;
        bool holding;

        public ShapeGame(ProcessDef process) : base(process, "Hold Space to thin the wall. Let go in the bright band. Three pinches.")
        {
            Bar.AddChild(zone);
            ((VBoxContainer)Root.GetChild(0)).AddChild(sketch);
            ((VBoxContainer)Root.GetChild(0)).MoveChild(sketch, 1);
            Next();
        }

        void Next()
        {
            wall = 1f;
            float center = 0.42f - pulls.Count * 0.07f;
            low = center - 0.07f;
            high = center + 0.07f;
            speed = 0.5f + pulls.Count * 0.12f;
            float w = Bar.CustomMinimumSize.X;
            zone.Position = new Vector2(low * w, 0);
            zone.Size = new Vector2((high - low) * w, Bar.CustomMinimumSize.Y);
        }

        public override void Key(Key key) { }

        public void Demo(float progress)
        {
            pulls.Add(0.9f);
            wall = 1f - progress;
            holding = false;
            Bar.Value = wall;
            sketch.Wall = wall;
            sketch.Height = (pulls.Count + progress) / Pulls;
            speed = 0f;
        }

        public override void Tick(float dt)
        {
            sketch.Turn += dt * 2.4f;
            if (pause > 0f)
            {
                pause -= dt;
                if (pause <= 0f)
                {
                    if (pulls.Count >= Pulls) Finish();
                    else Next();
                }
                return;
            }

            if (speed == 0f) return;
            bool down = Input.IsPhysicalKeyPressed(Godot.Key.Space);
            if (down)
            {
                holding = true;
                wall = Math.Max(0f, wall - speed * dt);
                if (wall <= 0.06f) Release(torn: true);
            }
            else if (holding) Release(torn: false);

            Bar.Value = wall;
            sketch.Wall = wall;
            sketch.Height = (pulls.Count + (1f - wall)) / Pulls;
        }

        void Release(bool torn)
        {
            holding = false;
            float center = (low + high) / 2f;
            float score = torn ? 0f : Math.Clamp(1f - Math.Abs(wall - center) / 0.3f, 0f, 1f);
            if (!torn && wall >= low && wall <= high) score = Math.Max(score, 0.85f);
            pulls.Add(score);
            Title.Text = torn ? "The wall tears. Press it back together..." : score > 0.8f ? "An even, thin wall" : wall > high ? "Still thick and heavy" : "Thin in places";
            pause = 0.6f;
        }

        void Finish()
        {
            Score = pulls.Average();
            Done = true;
        }
    }

    // Build a coil pot: roll a coil and lay it on as the turning pot brings the
    // seam round. Press Space as the mark crosses the bright spot; five coils (fewer with skill).
    sealed class CoilGame : Minigame_
    {
        readonly int total;
        readonly List<float> coils = new();
        readonly ColorRect zone = new() { Color = new Color(1f, 0.92f, 0.6f, 0.45f), MouseFilter = Control.MouseFilterEnum.Ignore };
        readonly PotSketch sketch = new(ClayForm.CoilPot);
        float angle, speed = 0.45f;
        const float Seam = 0.5f;

        public CoilGame(ProcessDef process) : base(process, "Press Space as the mark crosses the bright spot to lay each coil.")
        {
            total = Steps(process, 5);
            Hint.Text = $"Press Space as the mark crosses the bright spot to lay each coil. {Times(total, "coil", "coils")}.    Esc gives up.";
            Bar.AddChild(zone);
            float w = Bar.CustomMinimumSize.X;
            zone.Position = new Vector2((Seam - 0.06f) * w, 0);
            zone.Size = new Vector2(0.12f * w, Bar.CustomMinimumSize.Y);
            ((VBoxContainer)Root.GetChild(0)).AddChild(sketch);
            ((VBoxContainer)Root.GetChild(0)).MoveChild(sketch, 1);
            sketch.Wall = 0.35f;
        }

        public override void Key(Key key)
        {
            if (key != Godot.Key.Space || Done) return;
            float mark = (float)Bar.Value;
            float score = Math.Clamp(1f - Math.Abs(mark - Seam) / 0.3f, 0f, 1f);
            coils.Add(score);
            sketch.Coils = coils.Count;
            sketch.Height = coils.Count / (float)total;
            Title.Text = score > 0.8f ? "The coil joins cleanly" : score > 0.4f ? "A little off the seam" : "Lumpy. Smooth it in";
            speed += 0.12f;
            if (coils.Count >= total)
            {
                Score = coils.Average();
                Done = true;
            }
        }

        public override void Tick(float dt)
        {
            angle += dt * speed;
            Bar.Value = angle % 1f;
            sketch.Turn = angle * Mathf.Tau;
        }
    }

    // A pot in cross-section, drawn as it grows under your hands: wet clay, a
    // highlight sliding round as it turns, and coil lines for a coil pot.
    sealed partial class PotSketch : Control
    {
        public float Turn, Wall = 1f, Height;
        public int Coils;
        readonly ClayForm form;

        public PotSketch(ClayForm form)
        {
            this.form = form;
            CustomMinimumSize = new Vector2(420, 130);
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Process(double delta) => QueueRedraw();

        public override void _Draw()
        {
            var size = CustomMinimumSize;
            float baseY = size.Y - 8f, cx = size.X / 2f;
            float h = 14f + Mathf.Clamp(Height, 0f, 1f) * (size.Y - 30f);
            // A pinch pot starts as a squat ball and rises as the wall thins.
            float belly = form == ClayForm.CoilPot ? 46f : 40f + Wall * 14f;
            float thick = 4f + Wall * 16f;
            var wet = ClayLook.Wet.Lightened(0.15f);

            var outer = new List<Vector2>();
            var inner = new List<Vector2>();
            const int steps = 14;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float r = belly * (form == ClayForm.CoilPot ? 0.75f + 0.35f * Mathf.Sin(t * Mathf.Pi * 0.95f) - 0.15f * t : 0.8f + 0.25f * Mathf.Sin(t * Mathf.Pi * 0.8f));
                outer.Add(new Vector2(r, baseY - t * h));
                inner.Add(new Vector2(Mathf.Max(r - thick, 2f), baseY - Mathf.Max(t * h, thick * 0.6f)));
            }
            var poly = new List<Vector2>();
            foreach (var p in outer) poly.Add(new Vector2(cx - p.X, p.Y));
            for (int i = outer.Count - 1; i >= 0; i--) poly.Add(new Vector2(cx + outer[i].X, outer[i].Y));
            DrawColoredPolygon(poly.ToArray(), wet);

            // The hollow inside, darker.
            var hollow = new List<Vector2>();
            foreach (var p in inner) hollow.Add(new Vector2(cx - p.X, p.Y));
            for (int i = inner.Count - 1; i >= 0; i--) hollow.Add(new Vector2(cx + inner[i].X, inner[i].Y));
            if (h > thick) DrawColoredPolygon(hollow.ToArray(), ClayLook.Wet.Darkened(0.45f));

            // A glint sliding round the turning pot.
            float glint = Mathf.Sin(Turn) * 0.6f;
            for (int i = 0; i < outer.Count - 1; i++)
            {
                var a = outer[i];
                var b = outer[i + 1];
                float xa = cx + a.X * glint, xb = cx + b.X * glint;
                DrawLine(new Vector2(xa, a.Y), new Vector2(xb, b.Y), new Color(1f, 0.95f, 0.85f, 0.25f), 3f);
            }

            if (form == ClayForm.CoilPot)
                for (int c = 1; c <= Coils; c++)
                {
                    float y = baseY - c / 5f * (size.Y - 30f);
                    float r = belly * 0.9f;
                    DrawLine(new Vector2(cx - r, y), new Vector2(cx + r, y), ClayLook.Wet.Darkened(0.25f), 1.5f);
                }
        }
    }
}
