using Godot;
using LifeGame.Shared;

namespace LifeGame;

// Procedural idle and walk for a PlayerRig: a few sines per joint each frame, no
// allocations and no AnimationPlayer, so 32 players cost next to nothing. The figure
// faces +Z, so its left side is +X.
public sealed class PlayerAnimator
{
    readonly PlayerRig rig;
    readonly Pose idle = new(), walk = new(), work = new(), act = new(), swim = new(), gesture = new(), pose = new(), restFrom = new(), restTo = new();
    // Set while a craft job runs; the figure kneels into it and back out.
    public WorkMotion Work;
    WorkMotion shownWork;
    float workTime, workBlend;
    readonly float seed; // per-figure offset so a crowd never breathes or sways in step
    float time, phase;   // phase: walk cycle, a full turn is two steps
    float walkBlend, gait, run, turnLean, crouch;
    // Jumping (bugfix-25 drives it through Avatar.SetJump): how airborne, how much
    // still rising, and the knee-bend kick left over from the last landing.
    bool airborne, rising;
    float air, rise, landKick;

    // A quick action (WorkMotions.IsAct): drink, eat, pluck, pick up. Plays once over
    // whatever else is going on, fading in and out.
    WorkMotion shownAct;
    float actTime, actBlend;
    // The same action again while it plays (another sip): it loops back round at its
    // natural point instead of jumping to the start.
    bool again;

    // The action's variant (WorkMotions.VariantOf: a strike's style, a dig's tool).
    byte actVariant;

    // When a tool meets its work (an axe biting the trunk, a spade going in, a strike
    // landing), and a spade's dirt leaving the blade (second argument true), so chips,
    // dirt and thunks land on the frame.
    public System.Action<WorkMotion, bool>? Impact;
    float lastWorkCycle;

    public void Act(WorkMotion motion, byte variant = 0)
    {
        if (!WorkMotions.IsAct(motion)) return;
        actVariant = variant;
        if (motion == shownAct && actBlend > 0f)
        {
            again = true;
            return;
        }
        shownAct = motion;
        actTime = 0f;
        again = false;
    }

    // Seconds left of the action playing now, 0 when none.
    public float ActLeft => shownAct == WorkMotion.None ? 0f : Mathf.Max(ActLength - actTime, 0f);
    public float ActLength => WorkMotions.ActSeconds(shownAct, actVariant);

    // How far the feet are below the water line (0 or less on land), set each frame by
    // the avatar. Wading lifts the knees and holds the arms out; past Wading.FloatDepth
    // the body floats, treading water, or swims slowly when moving.
    public float WaterDepth;
    float wade, afloat;

    // Sitting or lying down (anim-12), from the player's rest state. Seat is how high
    // the hips sit when sitting (0 on the ground, a log's height on a log). Getting
    // down passes through a kneel, then a sit, then lying; getting up runs it backwards.
    public RestKind Rest;
    public float Seat;
    public bool Asleep;
    float restStage;

    // Roughly how much of the figure's standing height resting takes away (0 standing,
    // most of it lying down), for lowering the nameplate with the head.
    public float RestLowering
    {
        get
        {
            int from = Mathf.Min((int)restStage, 2);
            float a = Lowered(from), b = Lowered(from + 1);
            return Mathf.Lerp(a, b, Mathf.SmoothStep(0f, 1f, restStage - from));
        }
    }

    float Lowered(int stage) => stage switch
    {
        0 => 0f,
        1 => 0.3f,
        2 => Rest == RestKind.Lie || Seat <= 0f ? 0.42f : 0.42f * (1f - Mathf.Clamp(Seat / rig.LegLength, 0f, 1f)),
        _ => 0.75f,
    };
    const float RestStageSeconds = 0.55f;

    float RestTarget => walkBlend > 0.5f || airborne ? 0f : Rest switch { RestKind.Sit => 2f, RestKind.Lie => 3f, _ => 0f };

    // Already resting when first seen (a player who sat down before you arrived).
    public void SettleRest() => restStage = Rest switch { RestKind.Sit => 2f, RestKind.Lie => 3f, _ => 0f };

    // Settled on the ground, a sitter shifts into a fidget every 8 to 16 s and holds it
    // a while (anim-15).
    RestPoses.SitFidget sitFidget;
    float sitFidgetW, sitFidgetFor, sitFidgetNext = 6f;

    void SitFidgets(float dt)
    {
        bool settled = Rest == RestKind.Sit && restStage >= 1.99f;
        if (!settled)
        {
            sitFidgetW = 0f;
            sitFidget = RestPoses.SitFidget.None;
            sitFidgetNext = 6f;
            return;
        }
        if (sitFidget == RestPoses.SitFidget.None)
        {
            sitFidgetNext -= dt;
            if (sitFidgetNext > 0f) return;
            var (warmth, _) = Surroundings?.Invoke() ?? (0f, null);
            int pick = (int)Mathf.PosMod(time * 5.1f + seed * 7f, 4f);
            bool ground = Seat <= 0.05f;
            sitFidget = warmth > 0.2f && pick < 2 ? RestPoses.SitFidget.Warm
                : Atmosphere.Night > 0.5f && pick == 2 ? RestPoses.SitFidget.LookUp
                : !ground ? (pick % 2 == 0 ? RestPoses.SitFidget.LookUp : RestPoses.SitFidget.Warm)
                : pick % 2 == 0 ? RestPoses.SitFidget.HugKnees : RestPoses.SitFidget.LeanBack;
            if (!ground && sitFidget == RestPoses.SitFidget.Warm && warmth <= 0.2f) sitFidget = RestPoses.SitFidget.LookUp;
            sitFidgetFor = 0f;
        }
        sitFidgetFor += dt;
        float hold = 7f + Mathf.PosMod(seed * 11f, 5f);
        sitFidgetW = Mathf.Clamp(Mathf.Min(sitFidgetFor / 1.2f, (hold - sitFidgetFor) / 1.2f), 0f, 1f);
        if (sitFidgetFor >= hold)
        {
            sitFidget = RestPoses.SitFidget.None;
            sitFidgetNext = 8f + Mathf.PosMod(seed * 23f + time, 8f);
        }
    }

    void Resting(float dt)
    {
        SitFidgets(dt);
        float target = RestTarget;
        restStage = Mathf.MoveToward(restStage, target, dt / RestStageSeconds);
        if (restStage <= 0f) return;
        int from = Mathf.Min((int)restStage, 2);
        float f = Mathf.SmoothStep(0f, 1f, restStage - from);
        if (from == 0) restFrom.Blend(pose, pose, 0f);
        else RestStage(restFrom, from);
        RestStage(restTo, from + 1);
        pose.Blend(restFrom, restTo, f);
    }

    void RestStage(Pose p, int stage)
    {
        switch (stage)
        {
            case 1:
                p.Clear();
                WorkMotions.Kneel(p, rig.LegLength);
                p[Joint.Spine].X = 0.3f;
                p[Joint.ShoulderL] = new Vector3(-0.4f, 0, 0.12f);
                p[Joint.ShoulderR] = new Vector3(-0.4f, 0, -0.12f);
                p[Joint.ElbowL].X = -0.3f;
                p[Joint.ElbowR].X = -0.3f;
                break;
            case 2:
                RestPoses.Sit(p, rig, Rest == RestKind.Lie ? 0f : Seat, time + seed * 13f);
                if (Rest == RestKind.Sit) RestPoses.Fidget(p, sitFidget, Mathf.SmoothStep(0f, 1f, sitFidgetW), time + seed * 13f);
                break;
            default:
                RestPoses.Lie(p, rig, time + seed * 13f, Asleep);
                break;
        }
    }

    public void Jump(bool airborne, bool rising, float landing)
    {
        this.airborne = airborne;
        this.rising = rising;
        if (landing > landKick) landKick = Mathf.Clamp(landing, 0f, 1f);
    }

    // The face to show (#16): blinking every few seconds, happy while eating or waving or
    // swaying by a fire, sleepy when lying down, yawning or idle late at night. The
    // Modeling's Face parts show it (PlayerModel.SetFace).
    public Face Face { get; private set; }
    float blinkIn = 3f, blinkLeft;
    int blinkCount;

    void Expressions(float dt)
    {
        bool lying = Rest == RestKind.Lie && restStage > 2.5f;
        Face mood =
            lying && Asleep ? Face.Closed
            : lying || shownGesture == Gesture.Yawn ? Face.Sleepy
            : shownAct == WorkMotion.Eat || shownGesture is Gesture.Wave or Gesture.Sway ? Face.Happy
            : walkBlend < 0.3f && Atmosphere.Night > 0.7f ? Face.Sleepy
            : Face.Open;
        // Blink every 2.5 to 6 s, now and then twice; sleepy eyes blink slower.
        blinkIn -= dt;
        if (blinkLeft > 0f) blinkLeft -= dt;
        else if (blinkIn <= 0f)
        {
            blinkLeft = 0.13f;
            bool twice = Mathf.PosMod(time * 3.7f + seed, 1f) < 0.2f && blinkCount++ % 2 == 0;
            blinkIn = twice ? 0.25f : (mood == Face.Sleepy ? 4f : 2.5f) + Mathf.PosMod(seed * 17f + time * 1.3f, 3.5f);
        }
        Face = blinkLeft > 0f && mood is Face.Open or Face.Sleepy ? Face.Closed : mood;
    }

    // How much hop is in the walk (#17): full on the chibi, a trace on a tall figure.
    float Bounce => rig.HipHeight < 0.6f ? 1f : 0.15f;

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
        run = Mathf.Lerp(run, Smooth(4.5f, 5.5f, speed), k);
        // Longer steps at speed keep the feet from skating.
        float stepLength = 0.6f + 0.2f * speed;
        phase = Mathf.PosMod(phase + speed * dt / stepLength * Mathf.Pi, Mathf.Tau);
        turnLean = Mathf.Lerp(turnLean, Mathf.Clamp(turnRate * speed * 0.035f, -0.2f, 0.2f), k);
        crouch = Mathf.Lerp(crouch, sneaking ? 1f : 0f, 1f - Mathf.Exp(-8f * dt));

        float depth = airborne ? 0f : WaterDepth;
        bool floating = depth > Wading.FloatDepth - 0.03f;
        wade = Mathf.Lerp(wade, floating ? 0f : Smooth(0.1f, Wading.FloatDepth, depth), 1f - Mathf.Exp(-4f * dt));
        afloat = Mathf.Lerp(afloat, floating ? 1f : 0f, 1f - Mathf.Exp(-4f * dt));

        if (walkBlend < 0.995f) Idle(idle);
        if (walkBlend > 0.005f) Walk(walk);
        pose.Blend(idle, walk, walkBlend > 0.995f ? 1f : walkBlend < 0.005f ? 0f : walkBlend);

        if (crouch > 0.005f) Crouch(pose, crouch);
        if (wade > 0.005f) Wade(pose, wade);
        if (afloat > 0.005f)
        {
            Swim(swim, walkBlend);
            pose.Blend(pose, swim, afloat > 0.995f ? 1f : afloat);
        }

        // A new job while another still shows: ease out of the old pose first, then into
        // the new one, so nothing snaps.
        if (Work != shownWork && Work != WorkMotion.None && (shownWork == WorkMotion.None || workBlend < 0.05f))
        {
            shownWork = Work;
            workTime = 0f;
        }
        workTime += dt;
        // The tool's bite in a craft loop (chopping, knapping).
        var (rate, hit) = WorkMotions.WorkImpact(shownWork);
        if (rate > 0f)
        {
            float cycle = Mathf.PosMod(workTime * rate, 1f);
            bool crossed = lastWorkCycle < hit ? cycle >= hit || cycle < lastWorkCycle : cycle >= hit && cycle < lastWorkCycle;
            if (crossed && workBlend > 0.7f) Impact?.Invoke(shownWork, false);
            lastWorkCycle = cycle;
        }
        bool working = Work != WorkMotion.None && Work == shownWork && walkBlend < 0.5f;
        workBlend = Mathf.Lerp(workBlend, working ? 1f : 0f, 1f - Mathf.Exp(-8f * dt));
        if (workBlend > 0.005f)
        {
            WorkMotions.Pose(work, shownWork, workTime, rig.LegLength);
            pose.Blend(pose, work, workBlend > 0.995f ? 1f : workBlend);
        }
        else if (Work == WorkMotion.None) shownWork = WorkMotion.None;

        Resting(dt);
        Carry(dt);
        Gestures(dt);
        if (shownAct != WorkMotion.None) PlayAct(dt);
        Expressions(dt);
        air = Mathf.Lerp(air, airborne ? 1f : 0f, 1f - Mathf.Exp(-16f * dt));
        rise = Mathf.Lerp(rise, rising ? 1f : 0f, 1f - Mathf.Exp(-10f * dt));
        landKick *= Mathf.Exp(-7f * dt);
        if (air > 0.005f) Air(pose, air, rise);
        if (landKick > 0.01f) Crouch(pose, landKick * 0.7f);

        // Lean into turns; the head stays level.
        pose[Joint.Hips].Z -= turnLean;
        pose[Joint.Neck].Z += turnLean * 0.7f;
        rig.Apply(pose);
    }

    // An equipped tool carried in the right hand (Avatar.Equip): the forearm comes up a
    // little to hold it out from the leg, and the arm's walking swing is damped. Swap()
    // plays a quick 0.3 s reach down to the hip and back, to draw or stow it.
    public bool Carrying;
    public const float SwapSeconds = 0.3f;
    float carry, swapTime = -1f;

    public void Swap() => swapTime = 0f;

    void Carry(float dt)
    {
        carry = Mathf.MoveToward(carry, Carrying ? 1f : 0f, dt / 0.2f);
        if (carry > 0f)
        {
            float c = Mathf.SmoothStep(0f, 1f, carry) * (1f - workBlend);
            pose[Joint.ShoulderR].X = Mathf.Lerp(pose[Joint.ShoulderR].X, 0.35f * pose[Joint.ShoulderR].X - 0.18f, c);
            pose[Joint.ShoulderR].Z = Mathf.Lerp(pose[Joint.ShoulderR].Z, -0.16f, c);
            pose[Joint.ElbowR].X = Mathf.Lerp(pose[Joint.ElbowR].X, Mathf.Min(pose[Joint.ElbowR].X, -0.85f), c);
            pose[Joint.HandR].X = Mathf.Lerp(pose[Joint.HandR].X, 0.15f, c);
        }
        if (swapTime < 0f) return;
        swapTime += dt;
        if (swapTime >= SwapSeconds)
        {
            swapTime = -1f;
            return;
        }
        // Down and back to the right hip at the midpoint, then up again.
        float s = Mathf.Sin(swapTime / SwapSeconds * Mathf.Pi);
        pose[Joint.ShoulderR] += new Vector3(0.45f * s, 0, -0.2f * s);
        pose[Joint.ElbowR].X -= 0.5f * s;
        pose[Joint.Spine].Y -= 0.12f * s;
        pose[Joint.Neck].Y -= 0.25f * s;
    }

    // Standing about for a few seconds, a figure now and then stretches, yawns, looks
    // around or rolls its shoulders; it waves at someone close by, sways happily by a
    // fire, and in the cold rubs its hands and shivers (#15, kept gentle: D-313). Each
    // figure keeps its own timing (seed), so a crowd never moves in step.
    enum Gesture { None, Stretch, LookAround, Shoulders, RubHands, Yawn, Wave, Sway }
    Gesture shownGesture;
    float stillFor, gestureTime, nextGesture = 4f, lastWave = -999f, waveYaw;
    const float GestureSeconds = 2.6f;

    static float LengthOf(Gesture g) => g switch { Gesture.Sway => 6f, Gesture.Yawn => 2.8f, Gesture.Wave => 2.2f, _ => GestureSeconds };

    // What is around the figure when it next fidgets: warmth from a burning fire (0 to
    // 1) and the turn to the nearest other player close enough to wave at (radians,
    // positive to the figure's left), or null. Asked only when a fidget starts.
    public System.Func<(float warmth, float? friendYaw)>? Surroundings;

    Gesture PickGesture(float cold)
    {
        var (warmth, friend) = Surroundings?.Invoke() ?? (0f, null);
        if (friend is { } yaw && time - lastWave > 60f)
        {
            lastWave = time;
            waveYaw = Mathf.Clamp(yaw, -1.2f, 1.2f);
            return Gesture.Wave;
        }
        int pick = (int)Mathf.PosMod(time * 7.3f + seed * 13f, 4f);
        if (warmth > 0.2f && pick < 2) return Gesture.Sway;
        if (cold > 0.5f && pick != 1) return Gesture.RubHands;
        return pick switch { 0 => Gesture.Stretch, 1 => Gesture.LookAround, 2 => Gesture.Shoulders, _ => Gesture.Yawn };
    }

    void Gestures(float dt)
    {
        bool still = walkBlend < 0.05f && workBlend < 0.05f && shownAct == WorkMotion.None && afloat < 0.05f && !airborne && restStage <= 0f;
        stillFor = still ? stillFor + dt : 0f;
        float cold = Breath.Coldness;
        if (shownGesture == Gesture.None && still && stillFor > nextGesture)
        {
            shownGesture = PickGesture(cold);
            gestureTime = 0f;
            stillFor = 0f;
            nextGesture = 8f + Mathf.PosMod(seed * 31f + time, 8f);
        }
        if (cold > 0.6f && still)
        {
            // A fine shiver through the shoulders and chest.
            float shiver = (cold - 0.6f) * 2.5f * Mathf.Sin(time * 38f);
            pose[Joint.ShoulderL].Z += 0.025f * shiver;
            pose[Joint.ShoulderR].Z -= 0.025f * shiver;
            pose[Joint.Spine].X += 0.01f * shiver;
        }
        if (shownGesture == Gesture.None) return;
        gestureTime += dt;
        // Any movement or work ends it at once, blending out.
        float length = LengthOf(shownGesture);
        float w = still ? Mathf.SmoothStep(0f, 1f, Mathf.Min(gestureTime / 0.4f, (length - gestureTime) / 0.5f)) : 0f;
        if (gestureTime >= length || !still)
        {
            shownGesture = Gesture.None;
            return;
        }
        GesturePose(gesture, shownGesture, gestureTime);
        if (shownGesture is Gesture.Yawn or Gesture.Wave) MoreGestures(gesture, shownGesture, gestureTime);
        // Only the upper body (and a lift onto the toes for a stretch) takes part.
        for (int i = 0; i < Pose.Count; i++)
        {
            var j = (Joint)i;
            if (j is Joint.HipL or Joint.KneeL or Joint.HipR or Joint.KneeR) continue;
            pose.Rot[i] = pose.Rot[i].Lerp(gesture.Rot[i], w);
        }
        pose.HipsOffset += gesture.HipsOffset * w;
    }

    static void GesturePose(Pose p, Gesture g, float t)
    {
        p.Clear();
        switch (g)
        {
            case Gesture.Stretch:
            {
                // Arms up over the head, a lean back, up on the toes.
                float up = Mathf.Sin(Mathf.Clamp(t / GestureSeconds, 0f, 1f) * Mathf.Pi);
                p.HipsOffset = new Vector3(0, 0.015f * up, 0);
                p[Joint.FootL].X = -0.15f * up;
                p[Joint.FootR].X = -0.15f * up;
                p[Joint.Spine].X = -0.12f * up;
                p[Joint.Neck].X = -0.18f * up;
                p[Joint.ShoulderL] = new Vector3(-2.3f * up, 0, 0.12f + 0.2f * up);
                p[Joint.ShoulderR] = new Vector3(-2.3f * up, 0, -0.12f - 0.2f * up);
                p[Joint.ElbowL].X = -0.2f;
                p[Joint.ElbowR].X = -0.2f;
                break;
            }
            case Gesture.LookAround:
            {
                // A slow look over one shoulder, then the other, the chest turning a little.
                float look = Mathf.Sin(t / GestureSeconds * Mathf.Tau);
                p[Joint.Neck].Y = 0.6f * look;
                p[Joint.Spine].Y = 0.18f * look;
                p[Joint.ShoulderL] = new Vector3(-0.04f, 0, 0.1f);
                p[Joint.ShoulderR] = new Vector3(-0.04f, 0, -0.1f);
                p[Joint.ElbowL].X = -0.15f;
                p[Joint.ElbowR].X = -0.15f;
                break;
            }
            case Gesture.Shoulders:
            {
                // Two slow shoulder rolls, the head tipping side to side.
                float roll = Mathf.Sin(t * 4.8f);
                p[Joint.ShoulderL] = new Vector3(0.15f * Mathf.Cos(t * 4.8f), 0, 0.18f + 0.1f * roll);
                p[Joint.ShoulderR] = new Vector3(0.15f * Mathf.Cos(t * 4.8f), 0, -0.18f - 0.1f * roll);
                p[Joint.ElbowL].X = -0.2f;
                p[Joint.ElbowR].X = -0.2f;
                p[Joint.Neck].Z = 0.18f * Mathf.Sin(t * 2.4f);
                break;
            }
            case Gesture.Sway:
            {
                // By a warm fire: a slow happy sway side to side, head tipping with it,
                // palms held out to the heat.
                float sway = Mathf.Sin(t * 1.7f);
                float bob = 0.5f + 0.5f * Mathf.Cos(t * 3.4f);
                p.HipsOffset = new Vector3(0, -0.012f * bob, 0);
                p[Joint.Spine].Z = 0.07f * sway;
                p[Joint.Neck].Z = 0.12f * sway;
                p[Joint.Neck].X = 0.05f;
                p[Joint.ShoulderL] = new Vector3(-0.85f, 0, 0.2f + 0.04f * sway);
                p[Joint.ShoulderR] = new Vector3(-0.85f, 0, -0.2f + 0.04f * sway);
                p[Joint.ElbowL].X = -0.45f;
                p[Joint.ElbowR].X = -0.45f;
                p[Joint.HandL].X = -0.7f;
                p[Joint.HandR].X = -0.7f;
                break;
            }
            case Gesture.RubHands:
            {
                // Hands together in front of the chest, rubbed quickly, a breath blown into them.
                float rub = Mathf.Sin(t * 14f) * 0.08f;
                p[Joint.Spine].X = 0.1f;
                p[Joint.Neck].X = 0.15f;
                p[Joint.ShoulderL] = new Vector3(-0.6f + rub, 0, 0.12f - 0.35f);
                p[Joint.ShoulderR] = new Vector3(-0.6f - rub, 0, -0.12f + 0.35f);
                p[Joint.ElbowL].X = -1.7f;
                p[Joint.ElbowR].X = -1.7f;
                break;
            }
        }
    }

    // The fidgets that need the body's measurements or the friend's direction.
    void MoreGestures(Pose p, Gesture g, float t)
    {
        p.Clear();
        float length = LengthOf(g);
        if (g == Gesture.Yawn)
        {
            // A hand comes up to cover the mouth, the head tips back and the chest
            // opens, then the other arm stretches out as it ends.
            float open = Mathf.Sin(Mathf.Clamp(t / length, 0f, 1f) * Mathf.Pi);
            float late = Mathf.Clamp((t / length - 0.55f) / 0.45f, 0f, 1f);
            var (swing, side, elbow) = WorkMotions.ToMouth(rig, false, 0.02f);
            p[Joint.ShoulderR] = new Vector3(swing * open, 0, side * open - 0.1f * (1f - open));
            p[Joint.ElbowR].X = elbow * open;
            p[Joint.Spine].X = -0.1f * open;
            p[Joint.Neck].X = -0.3f * open;
            float reach = Mathf.Sin(late * Mathf.Pi);
            p[Joint.ShoulderL] = new Vector3(-0.04f - 1.6f * reach, 0, 0.1f + 0.5f * reach);
            p[Joint.ElbowL].X = -0.15f;
            return;
        }
        // A wave: turn towards them, raise the near arm and wag the forearm a few times.
        bool left = waveYaw > 0f;
        float up = Mathf.SmoothStep(0f, 1f, Mathf.Min(t / 0.35f, (length - t) / 0.4f));
        float wag = Mathf.Sin(t * 9f) * up;
        p[Joint.Spine].Y = 0.35f * waveYaw * up;
        p[Joint.Neck].Y = 0.6f * waveYaw * up;
        var (shoulder, elbowJoint, other) = left ? (Joint.ShoulderL, Joint.ElbowL, Joint.ShoulderR) : (Joint.ShoulderR, Joint.ElbowR, Joint.ShoulderL);
        float s = left ? 1f : -1f;
        p[shoulder] = new Vector3(-2.3f * up, 0, s * (0.15f + 0.35f * up + 0.15f * wag));
        p[elbowJoint].X = -0.7f * up;
        p[elbowJoint].Z = s * 0.25f * wag;
        p[other] = new Vector3(-0.04f, 0, -s * 0.1f);
    }

    void PlayAct(float dt)
    {
        float before = actTime;
        actTime += dt;
        if (actBlend > 0.5f)
        {
            float hit = WorkMotions.ActImpact(shownAct, actVariant), thrown = WorkMotions.ActThrow(shownAct, actVariant);
            if (hit >= 0f && before < hit && actTime >= hit) Impact?.Invoke(shownAct, false);
            if (thrown >= 0f && before < thrown && actTime >= thrown) Impact?.Invoke(shownAct, true);
        }
        var (loopAt, loopTo) = WorkMotions.ActLoop(shownAct, actVariant);
        if (again && actTime >= loopAt)
        {
            actTime = loopTo + (actTime - loopAt);
            again = false;
        }
        float length = ActLength, fade = WorkMotions.ActFade(shownAct);
        bool legs = WorkMotions.ActUsesLegs(shownAct);
        // Walking off ends a whole-body action early: it blends out as you go.
        float target = (actTime < length - fade || again) && !(legs && walkBlend > 0.5f) ? 1f : 0f;
        actBlend = Mathf.MoveToward(actBlend, target, dt / fade);
        if (actBlend <= 0f && (target == 0f || actTime >= length))
        {
            shownAct = WorkMotion.None;
            return;
        }
        WorkMotions.ActPose(act, shownAct, actTime, rig, actVariant);
        float w = Mathf.SmoothStep(0f, 1f, actBlend);
        if (legs)
        {
            pose.Blend(pose, act, w);
            return;
        }
        // Over a walk the legs keep striding: they take only a little of the bend.
        float legW = w * (1f - 0.8f * walkBlend);
        for (int i = 0; i < Pose.Count; i++)
        {
            var j = (Joint)i;
            bool leg = j is Joint.HipL or Joint.KneeL or Joint.FootL or Joint.HipR or Joint.KneeR or Joint.FootR;
            pose.Rot[i] = pose.Rot[i].Lerp(act.Rot[i], leg ? legW : w);
        }
        pose.HipsOffset = pose.HipsOffset.Lerp(pose.HipsOffset + act.HipsOffset, legW);
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
        // A little hop in each step (#17): the body lifts off as the legs pass under it
        // and the chest dips as the foot lands. Most on a small, round body.
        float hop = Peak(Mathf.Abs(c)) * Bounce * rig.LegLength * Mathf.Lerp(0.05f, 0.08f, g) * (1f - r);
        p.HipsOffset = new Vector3(
            -Mathf.Lerp(0.03f, 0.018f, g) * c * (1f - 0.5f * r),
            Mathf.Lerp(walkY, runY, r) + hop,
            0);
        p[Joint.Hips] = new Vector3(0.03f * g, -Mathf.Lerp(0.1f, 0.16f, g) * s, -0.045f * c);
        Legs(p);

        Leg(p, Joint.HipL, Joint.KneeL, Joint.FootL, phase, hipAmp, kneeAmp, stance);
        Leg(p, Joint.HipR, Joint.KneeR, Joint.FootR, phase + Mathf.Pi, hipAmp, kneeAmp, stance);

        // Chest leans in with pace and twists against the hips; the head stays on target.
        p[Joint.Spine] = new Vector3(Mathf.Lerp(Mathf.Lerp(0.04f, 0.14f, g), 0.26f, r), -1.6f * p[Joint.Hips].Y, -0.7f * p[Joint.Hips].Z);
        p[Joint.Spine].X += 0.06f * Bounce * Peak(Mathf.Abs(s)) * (1f - r);
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

    // In the air: legs stop striding, tuck up on the way up and reach for the ground on
    // the way down; arms lift on take-off and spread for balance as the body falls.
    static readonly Joint[] LegJoints = { Joint.HipL, Joint.HipR, Joint.KneeL, Joint.KneeR, Joint.FootL, Joint.FootR };

    static void Air(Pose p, float a, float up)
    {
        float down = 1f - up;
        foreach (var j in LegJoints)
            p[j] *= 1f - 0.7f * a;
        p[Joint.HipL].X -= (0.75f * up + 0.3f * down) * a;
        p[Joint.HipR].X -= (0.55f * up + 0.1f * down) * a;
        p[Joint.KneeL].X += (1.25f * up + 0.35f * down) * a;
        p[Joint.KneeR].X += (1.05f * up + 0.25f * down) * a;
        p[Joint.FootL].X -= 0.3f * up * a;
        p[Joint.FootR].X -= 0.3f * up * a;
        p[Joint.ShoulderL] += new Vector3(-0.9f * up - 0.25f * down, 0, 0.55f * down) * a;
        p[Joint.ShoulderR] += new Vector3(-0.9f * up - 0.25f * down, 0, -0.55f * down) * a;
        p[Joint.ElbowL].X -= 0.35f * a;
        p[Joint.ElbowR].X -= 0.35f * a;
        p[Joint.Spine].X += (0.1f * up - 0.05f * down) * a;
    }

    // Wading: the knees come up higher to clear the water and the arms ride out to the
    // sides for balance, more the deeper it gets. Added on top of idle or walk.
    void Wade(Pose p, float w)
    {
        float s = Mathf.Sin(phase);
        float lift = 0.35f * w * walkBlend;
        p[Joint.HipL].X -= lift * Mathf.Max(s, 0f);
        p[Joint.KneeL].X += 1.6f * lift * Mathf.Max(s, 0f);
        p[Joint.HipR].X -= lift * Mathf.Max(-s, 0f);
        p[Joint.KneeR].X += 1.6f * lift * Mathf.Max(-s, 0f);
        p[Joint.Spine].X += 0.08f * w;
        p[Joint.ShoulderL] = p[Joint.ShoulderL].Lerp(new Vector3(-0.15f, 0, 0.55f), w * 0.8f);
        p[Joint.ShoulderR] = p[Joint.ShoulderR].Lerp(new Vector3(-0.15f, 0, -0.55f), w * 0.8f);
        p[Joint.ElbowL].X = Mathf.Lerp(p[Joint.ElbowL].X, -0.45f, w * 0.8f);
        p[Joint.ElbowR].X = Mathf.Lerp(p[Joint.ElbowR].X, -0.45f, w * 0.8f);
    }

    // Afloat in deep water, head and shoulders out: treading water at rest (slow leg
    // circles, hands sculling at the sides, a gentle bob), a slow breaststroke when moving.
    void Swim(Pose p, float moving)
    {
        p.Clear();
        float t = time + seed * 7f;
        float bob = Mathf.Sin(t * 1.6f);
        // Treading water.
        float tread = Mathf.Sin(t * 2.4f);
        p.HipsOffset = new Vector3(0, 0.025f * bob, 0);
        p[Joint.Spine].X = 0.12f;
        p[Joint.Neck].X = -0.1f;
        p[Joint.HipL] = new Vector3(-0.55f + 0.25f * tread, 0, 0.1f);
        p[Joint.HipR] = new Vector3(-0.55f - 0.25f * tread, 0, -0.1f);
        p[Joint.KneeL].X = 0.9f - 0.35f * tread;
        p[Joint.KneeR].X = 0.9f + 0.35f * tread;
        p[Joint.ShoulderL] = new Vector3(-0.35f, 0, 0.75f + 0.12f * tread);
        p[Joint.ShoulderR] = new Vector3(-0.35f, 0, -0.75f - 0.12f * tread);
        p[Joint.ElbowL].X = -0.7f;
        p[Joint.ElbowR].X = -0.7f;
        p[Joint.HandL].Y = 0.5f * tread;
        p[Joint.HandR].Y = -0.5f * tread;
        if (moving < 0.01f) return;

        // Breaststroke: lean into the water, arms reach forward and sweep out and back,
        // legs draw up and kick together.
        float c = Mathf.PosMod(t * 0.9f, 1f);
        float reach = c < 0.5f ? Mathf.SmoothStep(0f, 1f, c / 0.5f) : 1f - Mathf.SmoothStep(0f, 1f, (c - 0.5f) / 0.5f);
        float kick = Mathf.Sin(c * Mathf.Tau);
        float m = moving;
        p[Joint.Spine].X = Mathf.Lerp(p[Joint.Spine].X, 0.55f, m);
        p[Joint.Neck].X = Mathf.Lerp(p[Joint.Neck].X, -0.5f, m);
        p[Joint.ShoulderL] = p[Joint.ShoulderL].Lerp(new Vector3(-2.2f * reach - 0.6f, 0, 0.25f + 0.8f * (1f - reach)), m);
        p[Joint.ShoulderR] = p[Joint.ShoulderR].Lerp(new Vector3(-2.2f * reach - 0.6f, 0, -0.25f - 0.8f * (1f - reach)), m);
        p[Joint.ElbowL].X = Mathf.Lerp(p[Joint.ElbowL].X, -0.2f - 1.0f * (1f - reach), m);
        p[Joint.ElbowR].X = Mathf.Lerp(p[Joint.ElbowR].X, -0.2f - 1.0f * (1f - reach), m);
        p[Joint.HipL] = p[Joint.HipL].Lerp(new Vector3(-0.3f - 0.4f * Mathf.Max(kick, 0f), 0, 0.2f), m);
        p[Joint.HipR] = p[Joint.HipR].Lerp(new Vector3(-0.3f - 0.4f * Mathf.Max(kick, 0f), 0, -0.2f), m);
        p[Joint.KneeL].X = Mathf.Lerp(p[Joint.KneeL].X, 0.2f + 1.2f * Mathf.Max(kick, 0f), m);
        p[Joint.KneeR].X = Mathf.Lerp(p[Joint.KneeR].X, 0.2f + 1.2f * Mathf.Max(kick, 0f), m);
        // A small round body doggy-paddles instead (anim-17): head held high and
        // bobbing side to side, hands paddling in turn under the chin, quick little kicks.
        if (Bounce >= 1f) DoggyPaddle(p, t, m);
    }

    void DoggyPaddle(Pose p, float t, float m)
    {
        float paddle = t * Mathf.Tau * 1.6f;
        float l = Mathf.Sin(paddle), r = Mathf.Sin(paddle + Mathf.Pi);
        float kick = Mathf.Sin(paddle * 1.5f);
        p.HipsOffset.Y += 0.015f * Mathf.Abs(l) * m;
        p[Joint.Spine] = p[Joint.Spine].Lerp(new Vector3(0.3f, 0, 0.05f * l), m);
        p[Joint.Neck] = p[Joint.Neck].Lerp(new Vector3(-0.4f, 0.12f * l, 0.1f * l), m);
        p[Joint.ShoulderL] = p[Joint.ShoulderL].Lerp(new Vector3(-1.5f - 0.5f * l, 0, 0.15f), m);
        p[Joint.ShoulderR] = p[Joint.ShoulderR].Lerp(new Vector3(-1.5f - 0.5f * r, 0, -0.15f), m);
        p[Joint.ElbowL].X = Mathf.Lerp(p[Joint.ElbowL].X, -0.9f + 0.5f * l, m);
        p[Joint.ElbowR].X = Mathf.Lerp(p[Joint.ElbowR].X, -0.9f + 0.5f * r, m);
        p[Joint.HandL].X = Mathf.Lerp(p[Joint.HandL].X, -0.6f, m);
        p[Joint.HandR].X = Mathf.Lerp(p[Joint.HandR].X, -0.6f, m);
        p[Joint.HipL] = p[Joint.HipL].Lerp(new Vector3(-0.5f + 0.35f * kick, 0, 0.1f), m);
        p[Joint.HipR] = p[Joint.HipR].Lerp(new Vector3(-0.5f - 0.35f * kick, 0, -0.1f), m);
        p[Joint.KneeL].X = Mathf.Lerp(p[Joint.KneeL].X, 0.6f - 0.3f * kick, m);
        p[Joint.KneeR].X = Mathf.Lerp(p[Joint.KneeR].X, 0.6f + 0.3f * kick, m);
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
