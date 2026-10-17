using Godot;
using LifeGame.Shared;

namespace LifeGame;

// What the hands are doing while a craft job runs (the WorkMotion enum is in shared/).
// Small crafts kneel on the right knee with the left foot planted and loop their own arm
// motion; Station stands working at a waist-high station (crafting table, spindle,
// drying rack: D-098). Hands hold nothing.

public static class WorkMotions
{
    // A process names its motion on its Def (ProcessDef.Motion). Older entries leave it
    // unset and get a guess from their id prefix; new families fall back to Gather.
    public static WorkMotion For(string? processId)
    {
        if (string.IsNullOrEmpty(processId)) return WorkMotion.None;
        var def = Processes.Find(processId);
        if (def is { Motion: not WorkMotion.None }) return def.Motion;
        if (def is { Station: not StationKind.None }) return WorkMotion.Station;
        if (processId == "fire.bow_drill") return WorkMotion.BowDrill;
        if (processId == "hide.scrape") return WorkMotion.Scrape;
        if (processId == "hide.stretch") return WorkMotion.Wrap;
        string family = processId.Split('.')[0];
        return family switch
        {
            "knap" => WorkMotion.Knap,
            "fire" => WorkMotion.HandDrill,
            "cord" or "weave" or "spin" => WorkMotion.Twist,
            "clay" => WorkMotion.Knead,
            "lash" or "haft" => WorkMotion.Wrap,
            "carve" => WorkMotion.Carve,
            "chop" or "fell" => WorkMotion.Chop,
            "cook" => WorkMotion.Tend,
            _ => WorkMotion.Gather,
        };
    }

    // Writes the whole working pose into p; t is seconds since the work began.
    public static void Pose(Pose p, WorkMotion work, float t, float legLength)
    {
        p.Clear();
        if (work == WorkMotion.Station)
        {
            Station(p, t);
            return;
        }
        if (work == WorkMotion.Chop)
        {
            Chop(p, t);
            return;
        }
        Kneel(p, legLength);
        p[Joint.Neck].X = 0.15f;
        p[Joint.ShoulderL].Z = 0.12f;
        p[Joint.ShoulderR].Z = -0.12f;

        float lean;
        switch (work)
        {
            case WorkMotion.Knap:
            {
                lean = 0.45f;
                // Left hand holds the core low and steady; the right lifts the hammerstone
                // slowly and snaps it down.
                Arm(p, true, -0.75f, -0.1f, -0.9f);
                float u = Mathf.PosMod(t * 1.3f, 1f);
                float raise = u < 0.75f ? Mathf.SmoothStep(0f, 1f, u / 0.75f) : 1f - (u - 0.75f) / 0.25f;
                Arm(p, false, -0.55f - 0.8f * raise, 0.15f, -0.45f - 0.7f * raise);
                p[Joint.Spine].Y = -0.08f * raise;
                break;
            }
            case WorkMotion.HandDrill:
            {
                lean = 0.4f;
                // Palms together around the spindle, rubbing fast and working down it.
                float rub = Mathf.Sin(t * 10f) * 0.12f;
                float down = Mathf.PosMod(t * 0.6f, 1f) * 0.35f;
                Arm(p, true, -0.85f + rub + down, -0.38f, -1.45f + down);
                Arm(p, false, -0.85f - rub + down, 0.38f, -1.45f + down);
                break;
            }
            case WorkMotion.BowDrill:
            {
                lean = 0.45f;
                // Left hand bears down on the socket; the right saws the bow.
                Arm(p, true, -0.75f, -0.25f, -1.0f);
                float saw = Mathf.Sin(t * 6.5f);
                Arm(p, false, -0.55f + 0.35f * saw, 0.1f, -1.05f - 0.4f * saw);
                p[Joint.Spine].Y = 0.05f * saw;
                break;
            }
            case WorkMotion.Twist:
            {
                lean = 0.25f;
                // Rolling fibre between the hands at the belly.
                float roll = Mathf.Sin(t * 3.5f) * 0.18f;
                Arm(p, true, -0.4f + roll, -0.3f, -1.5f - roll);
                Arm(p, false, -0.4f - roll, 0.3f, -1.5f + roll);
                break;
            }
            case WorkMotion.Knead:
            {
                // Both hands press down into the clay together, the back helping.
                float push = 0.5f + 0.5f * Mathf.Sin(t * 3.2f);
                lean = 0.55f + 0.08f * push;
                Arm(p, true, -1.0f + 0.15f * push, -0.15f, -0.75f + 0.45f * push);
                Arm(p, false, -1.0f + 0.15f * push, 0.15f, -0.75f + 0.45f * push);
                break;
            }
            case WorkMotion.Scrape:
            {
                // Long pushes away along the hide, quick returns.
                float u = Mathf.PosMod(t * 0.9f, 1f);
                float stroke = u < 0.7f ? Mathf.SmoothStep(0f, 1f, u / 0.7f) : 1f - (u - 0.7f) / 0.3f;
                lean = 0.5f + 0.15f * stroke;
                Arm(p, true, -0.7f - 0.35f * stroke, -0.12f, -0.9f + 0.6f * stroke);
                Arm(p, false, -0.7f - 0.35f * stroke, 0.12f, -0.9f + 0.6f * stroke);
                break;
            }
            case WorkMotion.Wrap:
            {
                lean = 0.35f;
                // Left hand holds the work; the right winds the cord round it.
                Arm(p, true, -0.7f, -0.25f, -1.2f);
                Arm(p, false, -0.7f + 0.18f * Mathf.Sin(t * 4f), 0.25f + 0.15f * Mathf.Cos(t * 4f), -1.1f);
                p[Joint.HandR].Y = 0.3f * Mathf.Sin(t * 4f);
                break;
            }
            case WorkMotion.Carve:
            {
                lean = 0.35f;
                // Left hand holds the stick; the right shaves away from the body.
                Arm(p, true, -0.7f, -0.25f, -1.2f);
                float u = Mathf.PosMod(t * 1.1f, 1f);
                float stroke = u < 0.4f ? Mathf.SmoothStep(0f, 1f, u / 0.4f) : 1f - (u - 0.4f) / 0.6f;
                Arm(p, false, -0.6f - 0.3f * stroke, 0.2f, -1.3f + 0.7f * stroke);
                break;
            }
            case WorkMotion.Tend:
            {
                lean = 0.2f;
                // Holding food over the fire, turning it now and then.
                Arm(p, true, -0.3f, 0.05f, -0.5f);
                Arm(p, false, -1.15f, 0.05f, -0.3f);
                p[Joint.HandR].Y = 0.5f * Mathf.Sin(t * 0.8f);
                break;
            }
            default: // Gather, and anything new: rummaging at the ground, hands taking turns.
            {
                lean = 0.6f;
                float s = Mathf.Sin(t * 2.4f);
                Arm(p, true, -0.95f + 0.25f * s, -0.05f, -0.3f - 0.25f * Mathf.Max(s, 0f));
                Arm(p, false, -0.95f - 0.25f * s, 0.05f, -0.3f - 0.25f * Mathf.Max(-s, 0f));
                break;
            }
        }
        p[Joint.Spine].X = lean;
        // Breathing goes on underneath.
        p[Joint.Spine].X += 0.01f * Mathf.Sin(t * 1.65f);
    }

    // Kneel on the right knee: right thigh straight down, shin flat behind, foot laid
    // back; left thigh forward and shin straight down to a planted foot.
    internal static void Kneel(Pose p, float legLength)
    {
        p.HipsOffset = new Vector3(0, -legLength * 0.44f, -0.05f);
        p[Joint.KneeR].X = Mathf.Pi / 2f;
        p[Joint.FootR].X = Mathf.Pi / 2f;
        p[Joint.HipL] = new Vector3(-1.45f, 0, 0.06f);
        p[Joint.KneeL].X = 1.45f;
    }

    // --- Quick actions (anim-4): played once, blended in and out by PlayerAnimator ---

    public static bool IsAct(WorkMotion m) => m >= WorkMotion.Drink && m <= WorkMotion.Flinch;

    // How a strike is thrown, from what is in the hand.
    public enum StrikeStyle : byte { Fist, Thrust, Slash }

    public static StrikeStyle StyleOf(Item? weapon) =>
        weapon is not { } w || !Items.IsKnown((ushort)w) ? StrikeStyle.Fist
        : Items.Def(w).Tags.HasFlag(ItemTags.Piercing) ? StrikeStyle.Thrust
        : Items.Def(w).Tags.HasFlag(ItemTags.Cutting) ? StrikeStyle.Slash
        : StrikeStyle.Fist;

    // How a hole is dug, from the tool: bare hands scoop, a digging stick stabs and
    // levers, a spade is stepped into, levered and the dirt thrown aside.
    public enum DigStyle : byte { Hands, Stick, Spade }

    // The spade is Crafting's (id appended when it lands); found by name so this needs
    // no change then. Any other digging tool is used like a stick.
    static readonly Item? SpadeItem = System.Enum.TryParse<Item>("Spade", out var spade) ? spade : null;

    public static DigStyle DigStyleOf(Item? tool) =>
        tool is not { } t ? DigStyle.Hands : t == SpadeItem ? DigStyle.Spade : DigStyle.Stick;

    // The variant of an action its item picks: a strike's style, a dig's style.
    public static byte VariantOf(WorkMotion m, Item? item) => m switch
    {
        WorkMotion.Strike => (byte)StyleOf(item),
        WorkMotion.Dig => (byte)DigStyleOf(item),
        _ => 0,
    };

    // How long one plays, in seconds, fades included.
    public static float ActSeconds(WorkMotion m, byte variant) => m == WorkMotion.Dig ? (DigStyle)variant switch
    {
        DigStyle.Spade => 1.35f,
        DigStyle.Hands => 0.8f,
        _ => 0.9f,
    } : ActSeconds(m);

    public static float ActSeconds(WorkMotion m) => m switch
    {
        WorkMotion.Drink => 2.4f,
        WorkMotion.Eat => 1.6f,
        WorkMotion.Pluck => 1.0f,
        WorkMotion.PickUp => 0.45f,
        WorkMotion.Strike => 0.6f,
        WorkMotion.Shoot => 1.0f,
        WorkMotion.Dig => 0.9f,
        WorkMotion.Shear => 1.4f,
        WorkMotion.Butcher => 1.6f,
        WorkMotion.Feed => 0.9f,
        WorkMotion.Flinch => 0.35f,
        _ => 0f,
    };

    // Where an action repeated while it plays loops back to: from loopAt seconds to
    // loopTo, two moments with the same pose (hands back at the water, at the side).
    public static (float loopAt, float loopTo) ActLoop(WorkMotion m, byte variant) => m switch
    {
        WorkMotion.Dig => (ActSeconds(m, variant), 0f),
        WorkMotion.Drink => (2.3f, 0.3f),
        WorkMotion.Eat => (1.15f, 0.15f),
        WorkMotion.Pluck => (0.85f, 0f),
        WorkMotion.Shear => (1.2f, 0.2f),
        WorkMotion.Butcher => (1.4f, 0.2f),
        _ => (ActSeconds(m, variant), 0f),
    };

    // When the tool meets its work, for chips, dirt and thunks to land on the frame
    // (Avatar.Impact): seconds into a quick action, or the point in a craft loop's cycle
    // (0 to 1) with how many cycles it plays a second. A negative time means no impact.
    public static float ActImpact(WorkMotion m, byte variant) => m switch
    {
        WorkMotion.Strike => 0.21f,
        WorkMotion.Dig => (DigStyle)variant switch { DigStyle.Spade => 0.55f, DigStyle.Hands => 0.3f, _ => 0.36f },
        WorkMotion.Feed => 0.5f,
        WorkMotion.PickUp => 0.22f,
        _ => -1f,
    };

    // A spade's second beat: the dirt thrown off the blade.
    public static float ActThrow(WorkMotion m, byte variant) =>
        m == WorkMotion.Dig && (DigStyle)variant == DigStyle.Spade ? 1.02f : -1f;

    public static (float rate, float phase) WorkImpact(WorkMotion m) => m switch
    {
        WorkMotion.Chop => (ChopRate, ChopHit),
        WorkMotion.Knap => (1.3f, 0.99f),
        _ => (0f, -1f),
    };

    // Seconds to blend in and out; a pick-up is quick and light so it never slows play.
    public static float ActFade(WorkMotion m) => m switch
    {
        WorkMotion.PickUp or WorkMotion.Flinch => 0.08f,
        WorkMotion.Strike => 0.1f,
        _ => 0.25f,
    };

    // Whether the legs take part. A pick-up bends only the back and arms (with a slight
    // knee dip), so it plays over walking; the others stop when you walk off.
    // Strikes, shots, feeding a fire and flinching are upper-body too.
    public static bool ActUsesLegs(WorkMotion m) =>
        m is not (WorkMotion.PickUp or WorkMotion.Strike or WorkMotion.Shoot or WorkMotion.Feed or WorkMotion.Flinch);

    // Which hand holds the action's item: a bow is held in the left.
    public static bool ActItemInLeft(WorkMotion m) => m == WorkMotion.Shoot;

    // The action's pose at t seconds in. The fades come from PlayerAnimator.
    public static void ActPose(Pose p, WorkMotion m, float t, PlayerRig body, byte variant = 0)
    {
        p.Clear();
        float legLength = body.LegLength;
        switch (m)
        {
            case WorkMotion.Strike:
                Strike(p, t, (StrikeStyle)variant);
                break;
            case WorkMotion.Shoot:
            {
                // Turn side-on, bow arm straight out at the target, draw the string back to
                // the cheek, hold a moment, loose, and the drawing hand flies back.
                float up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(t / 0.25f, 0f, 1f));
                float draw = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.15f) / 0.4f, 0f, 1f));
                float loose = Mathf.Clamp((t - 0.72f) / 0.08f, 0f, 1f);
                p[Joint.Hips].Y = 0.5f * up;
                p[Joint.Spine].Y = 0.35f * up;
                p[Joint.Neck].Y = -0.8f * up;
                Arm(p, true, -1.5f * up, -0.15f * up, -0.1f);
                Arm(p, false, Mathf.Lerp(-0.4f, -1.45f, up), Mathf.Lerp(0.1f, 0.45f, draw) - 0.25f * loose, Mathf.Lerp(-0.6f, -2.3f, draw) + 1.1f * loose);
                break;
            }
            case WorkMotion.Dig when (DigStyle)variant == DigStyle.Spade:
                Spade(p, t, legLength);
                break;
            case WorkMotion.Dig when (DigStyle)variant == DigStyle.Hands:
            {
                // Crouched low, both hands scoop the earth toward the knees.
                float scoop = Mathf.Sin(Mathf.Clamp(t / 0.8f, 0f, 1f) * Mathf.Pi);
                float pull = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.3f) / 0.35f, 0f, 1f)) * scoop;
                Kneel(p, legLength);
                p[Joint.Spine].X = 0.75f + 0.1f * scoop;
                p[Joint.Neck].X = -0.1f;
                Arm(p, true, -1.15f - 0.1f * scoop + 0.45f * pull, -0.15f, -0.2f - 0.5f * pull);
                Arm(p, false, -1.15f - 0.1f * scoop + 0.45f * pull, 0.15f, -0.2f - 0.5f * pull);
                break;
            }
            case WorkMotion.Dig:
            {
                // Feet apart and knees bent, both hands drive the stick down into the earth
                // and lever it back.
                float stab = Mathf.Sin(Mathf.Clamp(t / 0.9f, 0f, 1f) * Mathf.Pi);
                float lever = Mathf.Sin(Mathf.Clamp((t - 0.35f) / 0.45f, 0f, 1f) * Mathf.Pi);
                p.HipsOffset = new Vector3(0, -0.18f * legLength, -0.05f);
                p[Joint.HipL] = new Vector3(-0.55f, 0, 0.12f);
                p[Joint.HipR] = new Vector3(-0.55f, 0, -0.12f);
                p[Joint.KneeL].X = 0.95f;
                p[Joint.KneeR].X = 0.95f;
                p[Joint.FootL].X = -0.4f;
                p[Joint.FootR].X = -0.4f;
                p[Joint.Spine].X = 0.6f + 0.15f * stab;
                p[Joint.Neck].X = -0.15f;
                Arm(p, true, -0.9f - 0.15f * stab + 0.35f * lever, -0.25f, -0.5f + 0.35f * stab);
                Arm(p, false, -0.9f - 0.15f * stab + 0.35f * lever, 0.25f, -0.5f + 0.35f * stab);
                break;
            }
            case WorkMotion.Shear:
            case WorkMotion.Butcher:
            {
                // Down on one knee at the animal: the left hand holds the fleece or the hide
                // flat, the right works the blade in short strokes.
                Kneel(p, legLength);
                bool butcher = m == WorkMotion.Butcher;
                float stroke = Mathf.Sin(t * (butcher ? 7f : 10f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.2f) / 0.2f, 0f, 1f));
                p[Joint.Spine].X = butcher ? 0.7f : 0.45f;
                p[Joint.Neck].X = 0.15f;
                Arm(p, true, butcher ? -1.0f : -0.85f, -0.2f, -0.5f);
                Arm(p, false, (butcher ? -0.95f : -0.8f) + 0.12f * stroke, 0.15f + 0.1f * stroke, -0.7f - 0.15f * stroke);
                break;
            }
            case WorkMotion.Feed:
            {
                // A small bend and an underhand toss onto the fire.
                float swing = Mathf.Clamp(t / 0.6f, 0f, 1f);
                float back = Mathf.Sin(Mathf.Min(swing / 0.45f, 1f) * Mathf.Pi * 0.5f) * (1f - Mathf.SmoothStep(0.45f, 0.8f, swing));
                float toss = Mathf.SmoothStep(0.4f, 0.8f, swing) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.6f) / 0.3f, 0f, 1f)));
                p[Joint.Spine].X = 0.3f * (back + toss);
                p.HipsOffset = new Vector3(0, -0.05f * legLength * (back + toss), 0);
                p[Joint.KneeL].X = 0.2f * (back + toss);
                p[Joint.KneeR].X = 0.2f * (back + toss);
                Arm(p, false, 0.45f * back - 1.1f * toss, 0.05f, -0.3f);
                break;
            }
            case WorkMotion.Flinch:
            {
                // Rocked back by a blow: the chest and head snap back, arms jerk up.
                float hit = t < 0.06f ? t / 0.06f : Mathf.Exp(-(t - 0.06f) * 10f);
                p[Joint.Spine].X = -0.25f * hit;
                p[Joint.Neck].X = -0.3f * hit;
                p[Joint.Spine].Z = 0.08f * hit;
                Arm(p, true, -0.5f * hit, -0.1f * hit, -1.0f * hit);
                Arm(p, false, -0.5f * hit, 0.1f * hit, -1.0f * hit);
                break;
            }
            case WorkMotion.Drink:
            {
                // Down on one knee at the bank, lean out, cup both hands in the water, lift
                // them to the mouth and tip the head back to drink.
                Kneel(p, legLength);
                // 0 at the water, 1 at the mouth.
                float lift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.75f) / 0.5f, 0f, 1f))
                    * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 2.0f) / 0.3f, 0f, 1f)));
                float dip = Mathf.Sin(Mathf.Clamp((t - 0.3f) / 0.45f, 0f, 1f) * Mathf.Pi) * (1f - lift);
                p[Joint.Spine].X = Mathf.Lerp(0.75f + 0.08f * dip, 0.25f, lift);
                p[Joint.Neck].X = Mathf.Lerp(0.1f, -0.35f, lift);
                var (ls, lw, le) = ToMouth(body, true, 0.03f);
                var (rs, rw, re) = ToMouth(body, false, 0.03f);
                Arm(p, true, Mathf.Lerp(-1.05f - 0.1f * dip, ls, lift), Mathf.Lerp(-0.22f, lw, lift), Mathf.Lerp(-0.25f, le, lift));
                Arm(p, false, Mathf.Lerp(-1.05f - 0.1f * dip, rs, lift), Mathf.Lerp(0.22f, rw, lift), Mathf.Lerp(-0.25f, re, lift));
                // Palms turned up into a cup.
                p[Joint.HandL].Y = -0.6f;
                p[Joint.HandR].Y = 0.6f;
                break;
            }
            case WorkMotion.Eat:
            {
                // Standing: the right hand brings the food to the mouth, then a few chews.
                float lift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.15f) / 0.35f, 0f, 1f))
                    * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.85f) / 0.3f, 0f, 1f)));
                float chew = t > 0.6f ? Mathf.Max(0f, Mathf.Sin((t - 0.6f) * 16f)) * (1f - Mathf.Clamp((t - 0.8f) / 0.3f, 0f, 1f)) : 0f;
                var (ms, mw, me) = ToMouth(body, false, 0f);
                Arm(p, false, Mathf.Lerp(-0.1f, ms, lift), Mathf.Lerp(0.05f, mw, lift), Mathf.Lerp(-0.3f, me, lift));
                Arm(p, true, -0.05f, 0.02f, -0.2f);
                p[Joint.Spine].X = 0.04f;
                p[Joint.Neck].X = 0.08f * lift + 0.06f * chew;
                p[Joint.Head].X = 0.05f * chew;
                break;
            }
            case WorkMotion.Pluck:
            {
                // Reach into the bush at chest height, pick with a flick, bring the hand back.
                float reach = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(t / 0.35f, 0f, 1f))
                    * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.55f) / 0.3f, 0f, 1f)));
                float flick = Mathf.Sin(Mathf.Clamp((t - 0.35f) / 0.2f, 0f, 1f) * Mathf.Pi);
                p[Joint.Spine].X = 0.22f * reach;
                p[Joint.Neck].X = 0.1f * reach;
                Arm(p, false, Mathf.Lerp(-0.3f, -1.35f, reach), Mathf.Lerp(0.05f, 0.1f, reach), Mathf.Lerp(-0.6f, -0.25f, reach) - 0.3f * flick);
                p[Joint.HandR].X = -0.4f * flick;
                // The other hand waits low and open to take them.
                Arm(p, true, -0.45f * reach, -0.15f * reach, -1.1f * reach);
                break;
            }
            case WorkMotion.PickUp:
            {
                // A quick bend from the waist with soft knees and a grab near the feet.
                float down = Mathf.Sin(Mathf.Clamp(t / ActSeconds(m), 0f, 1f) * Mathf.Pi);
                p.HipsOffset = new Vector3(0, -0.1f * legLength * down, -0.04f * down);
                p[Joint.HipL].X = -0.35f * down;
                p[Joint.HipR].X = -0.35f * down;
                p[Joint.KneeL].X = 0.6f * down;
                p[Joint.KneeR].X = 0.6f * down;
                p[Joint.FootL].X = -0.25f * down;
                p[Joint.FootR].X = -0.25f * down;
                p[Joint.Spine].X = 0.85f * down;
                p[Joint.Neck].X = -0.2f * down;
                Arm(p, false, -0.75f * down, 0.05f, -0.15f);
                Arm(p, true, -0.2f * down, -0.05f, -0.4f * down);
                break;
            }
        }
    }

    // Arm angles (as Arm takes them) that bring a hand to just in front of the mouth,
    // worked out from this body's own arm lengths, shoulders and mouth: a two-bone
    // reach, so it lands on a tall adult and a short, big-headed chibi alike. apart
    // moves the hand that far off the middle (cupped hands side by side).
    static (float swing, float sideways, float elbow) ToMouth(PlayerRig body, bool left, float apart)
    {
        var shoulder = left ? body.ShoulderL : body.ShoulderR;
        var target = body.Mouth + new Vector3(left ? apart : -apart, -0.02f, 0.06f) - shoulder;
        float u = body.UpperArm, f = body.Forearm;
        float d = Mathf.Clamp(target.Length(), Mathf.Abs(u - f) + 0.01f, u + f - 0.001f);
        // The elbow folds forward by however much keeps shoulder-to-hand at d.
        float bend = Mathf.Pi - Mathf.Acos(Mathf.Clamp((u * u + f * f - d * d) / (2f * u * f), -1f, 1f));
        // The hand, in the upper arm's hanging frame, after that fold.
        float down = u + f * Mathf.Cos(bend), forward = f * Mathf.Sin(bend);
        // Lift sideways to the target's x, then swing forward in the y-z plane to it.
        float side = Mathf.Asin(Mathf.Clamp(target.X / Mathf.Max(down, 0.01f), -1f, 1f));
        float y = -down * Mathf.Cos(side);
        float swing = Mathf.Atan2(target.Z, target.Y) - Mathf.Atan2(forward, y);
        swing = Mathf.Wrap(swing, -Mathf.Pi, Mathf.Pi);
        return (swing, side - (left ? 0.12f : -0.12f), -bend);
    }

    // A strike over 0.6 s: a quick wind-up, the blow at about 0.2 s, then recovery.
    static void Strike(Pose p, float t, StrikeStyle style)
    {
        float wind = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(t / 0.14f, 0f, 1f)) * (1f - Mathf.Clamp((t - 0.14f) / 0.06f, 0f, 1f));
        float blow = Mathf.Clamp((t - 0.14f) / 0.08f, 0f, 1f) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.3f) / 0.3f, 0f, 1f)));
        // The chest winds against the blow and the weight comes forward into it.
        p[Joint.Spine] = new Vector3(0.12f * blow, -0.35f * wind + 0.4f * blow, 0);
        p[Joint.Hips].Y = -0.15f * wind + 0.2f * blow;
        p.HipsOffset = new Vector3(0, -0.03f * blow, 0.04f * blow);
        switch (style)
        {
            case StrikeStyle.Thrust:
                // Spear: drawn back at the hip, driven straight out, the left hand guiding.
                Arm(p, false, Mathf.Lerp(Mathf.Lerp(-0.3f, 0.35f, wind), -1.45f, blow), 0.25f, Mathf.Lerp(Mathf.Lerp(-1.2f, -1.7f, wind), -0.1f, blow));
                Arm(p, true, Mathf.Lerp(-0.6f, -1.2f, blow), -0.35f, Mathf.Lerp(-1.2f, -0.6f, blow));
                break;
            case StrikeStyle.Slash:
                // Blade: raised high over the right shoulder, cut down and across.
                Arm(p, false, Mathf.Lerp(Mathf.Lerp(-0.4f, -2.5f, wind), -0.7f, blow), Mathf.Lerp(Mathf.Lerp(0f, -0.4f, wind), 0.55f, blow), Mathf.Lerp(Mathf.Lerp(-0.6f, -1.3f, wind), -0.3f, blow));
                Arm(p, true, -0.4f, -0.1f, -1.2f);
                break;
            default:
                // Fist: guard up, a straight punch from the right shoulder.
                Arm(p, false, Mathf.Lerp(-0.6f, -1.5f, blow), Mathf.Lerp(0.2f, 0.25f, blow), Mathf.Lerp(Mathf.Lerp(-2.0f, -2.3f, wind), -0.15f, blow));
                Arm(p, true, -0.7f, 0.2f, -2.1f);
                break;
        }
    }

    // Standing at a waist-high station, knees soft, both hands working on its top and
    // the weight shifting a little with the work.
    static void Station(Pose p, float t)
    {
        float work = Mathf.Sin(t * 2.6f), sway = Mathf.Sin(t * 0.9f);
        p.HipsOffset = new Vector3(0.015f * sway, -0.02f, 0);
        p[Joint.HipL] = new Vector3(-0.08f, 0, -0.02f * sway);
        p[Joint.HipR] = new Vector3(-0.08f, 0, -0.02f * sway);
        p[Joint.KneeL].X = 0.14f;
        p[Joint.KneeR].X = 0.14f;
        p[Joint.FootL].X = -0.06f;
        p[Joint.FootR].X = -0.06f;
        p[Joint.Spine] = new Vector3(0.3f + 0.01f * Mathf.Sin(t * 1.65f), 0.05f * work, 0);
        p[Joint.Neck].X = 0.2f;
        Arm(p, true, -0.55f + 0.12f * work, -0.12f, -0.95f - 0.15f * work);
        Arm(p, false, -0.55f - 0.12f * work, 0.12f, -0.95f + 0.15f * work);
    }

    // Chop cycles a second, and where in the cycle the axe bites.
    const float ChopRate = 0.85f, ChopHit = 0.7f;

    // Felling with an axe, with weight: feet planted wide, the axe lifted back over the
    // right shoulder, held a beat at the top, then a downswing that gathers speed into
    // the trunk at waist height. On the bite the knees dip, the body jolts and the axe
    // kicks back a little before settling for the next lift.
    static void Chop(Pose p, float t)
    {
        float u = Mathf.PosMod(t * ChopRate, 1f);
        // 0 = at the trunk, 1 = fully wound up.
        float wind, shock = 0f;
        if (u < 0.45f) wind = Mathf.SmoothStep(0f, 1f, u / 0.45f);
        else if (u < 0.58f) wind = 1f + 0.06f * Mathf.Sin((u - 0.45f) / 0.13f * Mathf.Pi * 0.5f);
        else if (u < ChopHit)
        {
            // Gathering speed: slow off the top, fastest into the wood.
            float x = (u - 0.58f) / (ChopHit - 0.58f);
            wind = 1.06f * (1f - x * x * x);
        }
        else
        {
            // The recoil: a kick back off the trunk, then settling.
            float x = (u - ChopHit) / (1f - ChopHit);
            wind = 0.14f * Mathf.Sin(Mathf.Min(x / 0.35f, 1f) * Mathf.Pi) * (x < 0.35f ? 1f : 0f);
            shock = Mathf.Exp(-x * 9f);
        }
        p.HipsOffset = new Vector3(0.02f * wind, -0.05f - 0.04f * shock, 0.03f * shock);
        p[Joint.Hips].Y = -0.25f * wind;
        p[Joint.HipL] = new Vector3(-0.15f - 0.1f * shock, 0.25f * wind, 0.12f);
        p[Joint.HipR] = new Vector3(0.1f - 0.1f * shock, 0.25f * wind, -0.12f);
        p[Joint.KneeL].X = 0.3f + 0.2f * shock;
        p[Joint.KneeR].X = 0.22f + 0.2f * shock;
        p[Joint.FootL].X = -0.12f - 0.1f * shock;
        p[Joint.FootR].X = -0.1f - 0.1f * shock;
        p[Joint.Spine] = new Vector3(0.22f + 0.08f * (1f - wind) + 0.08f * shock, -0.6f * wind + 0.15f, 0.08f * wind);
        p[Joint.Neck] = new Vector3(0.1f - 0.08f * shock, 0.5f * wind - 0.1f, 0);
        // Both hands on the haft: the right arm leads the lift, the left reaches across.
        Arm(p, false, -1.05f - 0.75f * wind, 0.1f - 0.25f * wind, -0.25f - 0.55f * wind);
        Arm(p, true, -1.15f - 0.55f * wind, -0.45f, -0.2f - 0.6f * wind);
    }

    // A spade, over 1.35 s: set the blade upright in front, step the right foot onto it
    // and drive it in, lever the handle back, then lift and throw the dirt off to the left.
    static void Spade(Pose p, float t, float legLength)
    {
        float set = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(t / 0.25f, 0f, 1f));
        float foot = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.2f) / 0.2f, 0f, 1f))
            * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.6f) / 0.15f, 0f, 1f)));
        float press = Mathf.Sin(Mathf.Clamp((t - 0.42f) / 0.2f, 0f, 1f) * Mathf.Pi);
        float lever = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.62f) / 0.2f, 0f, 1f))
            * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.82f) / 0.12f, 0f, 1f)));
        float lift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 0.82f) / 0.15f, 0f, 1f))
            * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp((t - 1.05f) / 0.3f, 0f, 1f)));
        float toss = Mathf.Sin(Mathf.Clamp((t - 0.92f) / 0.2f, 0f, 1f) * Mathf.Pi);

        // The step: right thigh up and the foot on the blade, then the weight on it.
        p.HipsOffset = new Vector3(0, 0.04f * legLength * foot - 0.1f * legLength * press - 0.06f * legLength * lift, 0);
        p[Joint.HipR] = new Vector3(-0.75f * foot + 0.15f * press, 0, -0.05f);
        p[Joint.KneeR].X = 1.1f * foot - 0.2f * press;
        p[Joint.FootR].X = -0.3f * foot;
        p[Joint.KneeL].X = 0.15f + 0.35f * press + 0.3f * lift;
        p[Joint.HipL].X = -0.15f * press - 0.15f * lift;
        p[Joint.FootL].X = -0.15f * press - 0.12f * lift;
        // Back and arms: hands on the upright handle, pulling it back, then lifting and
        // swinging the blade across to the left.
        p[Joint.Spine] = new Vector3(0.18f * set + 0.15f * press - 0.12f * lever + 0.35f * lift, 0.5f * toss, 0);
        p[Joint.Neck] = new Vector3(0.05f, -0.3f * toss, 0);
        Arm(p, false, -0.7f * set + 0.35f * lever - 0.35f * lift, 0.25f + 0.2f * toss, -1.2f * set + 0.4f * lever);
        Arm(p, true, -0.55f * set + 0.25f * lever - 0.55f * lift, -0.3f - 0.2f * toss, -0.5f * set - 0.3f * lift);
    }

    // Shoulder swing (negative is forward), shoulder sideways (inward is negative on the
    // left and positive on the right) and elbow bend (negative folds the forearm forward).
    static void Arm(Pose p, bool left, float swing, float sideways, float elbow)
    {
        p[left ? Joint.ShoulderL : Joint.ShoulderR] = new Vector3(swing, 0, (left ? 0.12f : -0.12f) + sideways);
        p[left ? Joint.ElbowL : Joint.ElbowR].X = elbow;
    }
}
