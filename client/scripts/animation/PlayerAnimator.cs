using Godot;

namespace LifeGame;

// Procedural idle and walk for a PlayerRig: a few sines per joint each frame, no
// allocations and no AnimationPlayer, so 32 players cost next to nothing. The figure
// faces +Z, so its left side is +X.
public sealed class PlayerAnimator
{
    readonly PlayerRig rig;
    readonly Pose idle = new(), walk = new(), work = new(), pose = new();
    // Set while a craft job runs; the figure kneels into it and back out.
    public WorkMotion Work;
    WorkMotion shownWork;
    float workTime, workBlend;
    readonly float seed; // per-figure offset so a crowd never breathes or sways in step
    float time, phase;   // phase: walk cycle, a full turn is two steps
    float walkBlend, gait, run, turnLean, crouch;

    public PlayerAnimator(PlayerRig rig, ulong seed)
    {
        this.rig = rig;
        this.seed = seed % 997 * 0.618034f;
    }

    // speed: ground speed in m/s; turnRate: yaw speed in rad/s, positive turning left.
    public void Update(float dt, float speed, float turnRate, bool sneaking)
    {
        time += dt;
        float k = 1f - Mathf.Exp(-6f * dt);
        walkBlend = Mathf.Lerp(walkBlend, Smooth(0.15f, 1.1f, speed), k);
        // 0 is a stroll (sneaking, heavy loads), 1 the brisk pace of a normal walk.
        gait = Mathf.Lerp(gait, Smooth(1.8f, 4.0f, speed), k);
        // Above the brisk walk-jog (4.2 m/s) a held run takes over (D-088).
        run = Mathf.Lerp(run, Smooth(4.6f, 6.2f, speed), k);
        // Longer steps at speed keep the feet from skating.
        float stepLength = 0.6f + 0.2f * speed;
        phase = Mathf.PosMod(phase + speed * dt / stepLength * Mathf.Pi, Mathf.Tau);
        turnLean = Mathf.Lerp(turnLean, Mathf.Clamp(turnRate * speed * 0.035f, -0.2f, 0.2f), k);
        crouch = Mathf.Lerp(crouch, sneaking ? 1f : 0f, 1f - Mathf.Exp(-8f * dt));

        if (walkBlend < 0.995f) Idle(idle);
        if (walkBlend > 0.005f) Walk(walk);
        pose.Blend(idle, walk, walkBlend > 0.995f ? 1f : walkBlend < 0.005f ? 0f : walkBlend);

        if (crouch > 0.005f) Crouch(pose, crouch);

        if (Work != WorkMotion.None && Work != shownWork)
        {
            shownWork = Work;
            workTime = 0f;
        }
        workTime += dt;
        workBlend = Mathf.Lerp(workBlend, Work != WorkMotion.None && walkBlend < 0.5f ? 1f : 0f, 1f - Mathf.Exp(-5f * dt));
        if (workBlend > 0.005f)
        {
            WorkMotions.Pose(work, shownWork, workTime, rig.LegLength);
            pose.Blend(pose, work, workBlend > 0.995f ? 1f : workBlend);
        }
        else shownWork = WorkMotion.None;
        // Lean into turns; the head stays level.
        pose[Joint.Hips].Z -= turnLean;
        pose[Joint.Neck].Z += turnLean * 0.7f;
        rig.Apply(pose);
    }

    void Idle(Pose p)
    {
        p.Clear();
        float t = time + seed * 13f;
        float breath = Mathf.Sin(t * 1.65f);
        // Weight rests on one leg for a few seconds, then moves to the other.
        float shift = Mathf.Clamp(Mathf.Sin(t * 0.42f + seed) * 1.8f, -1f, 1f);
        float onLeft = Mathf.Max(shift, 0f), onRight = Mathf.Max(-shift, 0f);

        p.HipsOffset = new Vector3(0.035f * shift, -0.012f * Mathf.Abs(shift), 0);
        p[Joint.Hips].Z = 0.05f * shift; // the free leg's hip drops
        p[Joint.Hips].Y = -0.06f * shift;
        Legs(p);
        // The relaxed leg bends at the knee and rests slightly forward.
        p[Joint.HipL].X -= 0.07f * onRight;
        p[Joint.KneeL].X = 0.03f + 0.17f * onRight;
        p[Joint.FootL].X = -0.08f * onRight;
        p[Joint.HipR].X -= 0.07f * onLeft;
        p[Joint.KneeR].X = 0.03f + 0.17f * onLeft;
        p[Joint.FootR].X = -0.08f * onLeft;

        p[Joint.Spine] = new Vector3(0.02f + 0.012f * breath, 0.04f * shift, -0.07f * shift);
        // Now and then glance around.
        float look = Mathf.Sin(t * 0.23f + seed * 5f) * Mathf.Sin(t * 0.11f + seed);
        p[Joint.Neck] = new Vector3(0.04f * Mathf.Sin(t * 0.31f + seed * 2f) - 0.01f * breath, 0.35f * look, 0.02f * shift);
        p[Joint.Head].X = 0.03f;

        float lift = 0.012f * breath; // shoulders rise on the in-breath
        p[Joint.ShoulderL] = new Vector3(-0.04f + 0.02f * Mathf.Sin(t * 0.7f), 0, 0.09f + lift);
        p[Joint.ShoulderR] = new Vector3(-0.04f + 0.02f * Mathf.Sin(t * 0.7f + 1.3f), 0, -0.09f - lift);
        p[Joint.ElbowL].X = -0.14f - 0.02f * breath;
        p[Joint.ElbowR].X = -0.16f - 0.02f * breath;
        p[Joint.HandL] = new Vector3(-0.05f, 0, 0.05f);
        p[Joint.HandR] = new Vector3(-0.05f, 0, -0.05f);
    }

    void Walk(Pose p)
    {
        p.Clear();
        float g = gait, r = run;
        float s = Mathf.Sin(phase), c = Mathf.Cos(phase); // s > 0: left leg forward

        float hipAmp = Mathf.Lerp(Mathf.Lerp(0.32f, 0.52f, g), 0.72f, r);
        float stance = Mathf.Lerp(Mathf.Lerp(0.05f, 0.14f, g), 0.3f, r); // knees never lock straight
        float kneeAmp = Mathf.Lerp(Mathf.Lerp(0.6f, 1.15f, g), 1.75f, r);

        // Walking, the hips drop as the legs open and sway over the standing foot.
        // Running, they sink as the foot takes the weight and rise into the flight.
        float walkY = -rig.LegLength * (1f - Mathf.Cos(hipAmp * s)) * 0.85f - 0.035f * g;
        float runY = -0.09f + 0.07f * Mathf.Abs(s);
        p.HipsOffset = new Vector3(
            -Mathf.Lerp(0.03f, 0.018f, g) * c * (1f - 0.5f * r),
            Mathf.Lerp(walkY, runY, r),
            0);
        p[Joint.Hips] = new Vector3(0.03f * g, -Mathf.Lerp(0.1f, 0.16f, g) * s, -0.045f * c);
        Legs(p);

        Leg(p, Joint.HipL, Joint.KneeL, Joint.FootL, phase, hipAmp, kneeAmp, stance);
        Leg(p, Joint.HipR, Joint.KneeR, Joint.FootR, phase + Mathf.Pi, hipAmp, kneeAmp, stance);

        // Chest leans in with pace and twists against the hips; the head stays on target.
        p[Joint.Spine] = new Vector3(Mathf.Lerp(Mathf.Lerp(0.04f, 0.14f, g), 0.26f, r), -1.6f * p[Joint.Hips].Y, -0.7f * p[Joint.Hips].Z);
        p[Joint.Neck] = -0.75f * (p[Joint.Spine] + p[Joint.Hips]);

        // Arms swing against the legs and bend more at the front of the swing.
        float armAmp = Mathf.Lerp(Mathf.Lerp(0.3f, 0.55f, g), 0.8f, r);
        float elbow = Mathf.Lerp(Mathf.Lerp(0.22f, 1.2f, g), 1.5f, r);
        p[Joint.ShoulderL] = new Vector3(armAmp * s, 0, 0.1f);
        p[Joint.ShoulderR] = new Vector3(-armAmp * s, 0, -0.1f);
        p[Joint.ElbowL].X = -elbow - 0.35f * Mathf.Max(-s, 0f);
        p[Joint.ElbowR].X = -elbow - 0.35f * Mathf.Max(s, 0f);
        p[Joint.HandL] = new Vector3(-0.1f, 0, 0.06f);
        p[Joint.HandR] = new Vector3(-0.1f, 0, -0.06f);
    }

    // Sneaking: knees bent and hips low with the feet still planted, chest over the knees,
    // head up and hands held ready. Added on top of whatever idle or walk is doing.
    void Crouch(Pose p, float w)
    {
        const float bend = 0.62f; // thigh angle; the hips drop by LegLength * (1 - cos bend)
        p.HipsOffset.Y -= rig.LegLength * (1f - Mathf.Cos(bend)) * w;
        p[Joint.HipL].X -= bend * w;
        p[Joint.HipR].X -= bend * w;
        p[Joint.KneeL].X += 2f * bend * w;
        p[Joint.KneeR].X += 2f * bend * w;
        p[Joint.FootL].X -= bend * w;
        p[Joint.FootR].X -= bend * w;
        p[Joint.Spine].X += 0.42f * w;
        p[Joint.Neck].X -= 0.38f * w;
        p[Joint.ShoulderL].X -= 0.3f * w;
        p[Joint.ShoulderR].X -= 0.3f * w;
        p[Joint.ElbowL].X -= 0.55f * w;
        p[Joint.ElbowR].X -= 0.55f * w;
    }

    // One leg through the cycle; at legPhase 0 it passes under the body swinging forward.
    static void Leg(Pose p, Joint hip, Joint knee, Joint foot, float legPhase, float hipAmp, float kneeAmp, float stance)
    {
        float s = Mathf.Sin(legPhase);
        p[hip].X -= hipAmp * s;
        // The knee folds as the foot leaves the ground behind and opens again to reach.
        p[knee].X = stance + kneeAmp * Peak(Mathf.Cos(legPhase + 0.55f));
        // Sole stays near level, heel lifts and toes push off at the back of the stride.
        p[foot].X = -0.8f * (p[hip].X + p[knee].X) + 0.35f * Peak(Mathf.Cos(legPhase + 1.3f)) - 0.1f * Peak(s);
    }

    // Shared by idle and walk: undo the pelvis tilt and sideways shift so the feet stay planted.
    void Legs(Pose p)
    {
        float z = -p[Joint.Hips].Z - p.HipsOffset.X / rig.LegLength;
        float y = -p[Joint.Hips].Y;
        p[Joint.HipL] = new Vector3(0, y, z);
        p[Joint.HipR] = new Vector3(0, y, z);
    }

    static float Peak(float x) => x <= 0f ? 0f : x * Mathf.Sqrt(x);

    static float Smooth(float from, float to, float x) => Mathf.SmoothStep(from, to, x);
}
