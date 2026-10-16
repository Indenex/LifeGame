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
        // Kneel on the right knee: right thigh straight down, shin flat behind, foot laid
        // back; left thigh forward and shin straight down to a planted foot.
        p.HipsOffset = new Vector3(0, -legLength * 0.44f, -0.05f);
        p[Joint.KneeR].X = Mathf.Pi / 2f;
        p[Joint.FootR].X = Mathf.Pi / 2f;
        p[Joint.HipL] = new Vector3(-1.45f, 0, 0.06f);
        p[Joint.KneeL].X = 1.45f;
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

    // Felling with an axe: feet planted wide, wind the chest and both arms back over the
    // right shoulder, then whip the swing across into the trunk at waist height.
    static void Chop(Pose p, float t)
    {
        float u = Mathf.PosMod(t * 0.85f, 1f);
        // 0 = struck, 1 = fully wound up.
        float wind = u < 0.62f ? Mathf.SmoothStep(0f, 1f, u / 0.62f)
            : u < 0.78f ? 1f - Mathf.SmoothStep(0f, 1f, (u - 0.62f) / 0.16f) * 1.05f
            : -0.05f * (1f - (u - 0.78f) / 0.22f);
        p.HipsOffset = new Vector3(0.02f * wind, -0.05f, 0);
        p[Joint.Hips].Y = -0.25f * wind;
        p[Joint.HipL] = new Vector3(-0.15f, 0.25f * wind, 0.12f);
        p[Joint.HipR] = new Vector3(0.1f, 0.25f * wind, -0.12f);
        p[Joint.KneeL].X = 0.3f;
        p[Joint.KneeR].X = 0.22f;
        p[Joint.FootL].X = -0.12f;
        p[Joint.FootR].X = -0.1f;
        p[Joint.Spine] = new Vector3(0.22f + 0.08f * (1f - wind), -0.6f * wind + 0.15f, 0.08f * wind);
        p[Joint.Neck] = new Vector3(0.1f, 0.5f * wind - 0.1f, 0);
        // Both hands on the haft: the right arm leads the lift, the left reaches across.
        Arm(p, false, -1.05f - 0.75f * wind, 0.1f - 0.25f * wind, -0.25f - 0.55f * wind);
        Arm(p, true, -1.15f - 0.55f * wind, -0.45f, -0.2f - 0.6f * wind);
    }

    // Shoulder swing (negative is forward), shoulder sideways (inward is negative on the
    // left and positive on the right) and elbow bend (negative folds the forearm forward).
    static void Arm(Pose p, bool left, float swing, float sideways, float elbow)
    {
        p[left ? Joint.ShoulderL : Joint.ShoulderR] = new Vector3(swing, 0, (left ? 0.12f : -0.12f) + sideways);
        p[left ? Joint.ElbowL : Joint.ElbowR].X = elbow;
    }
}
