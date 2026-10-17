using Godot;

namespace LifeGame;

// How a figure rests (anim-12): sitting on the ground or on a seat (a log, a stool),
// and lying curled on its side on a mat or bed. Getting down goes through a kneel and
// getting up back through it (PlayerAnimator blends the stages), so nothing pops.
public enum RestKind : byte { None, Sit, Lie }

public static class RestPoses
{
    // Sitting with the hips seat metres off the ground (0 on the ground). On the
    // ground the knees come up and the feet rest out in front; on a seat the thighs
    // level out and the shins drop to the ground. Forearms rest on the knees.
    public static void Sit(Pose p, PlayerRig body, float seat, float t)
    {
        p.Clear();
        float ankle = body.HipHeight - body.LegLength;
        float buttock = Mathf.Max(0.04f, 0.09f * body.HipHeight);
        float hips = seat + buttock;
        p.HipsOffset = new Vector3(0, hips - body.HipHeight, -0.04f);

        // Thigh angle from straight down, forward: knees above the hips on the ground,
        // level on a seat as high as the shin.
        float high = Mathf.Clamp(seat / Mathf.Max(body.Shin + ankle, 0.1f), 0f, 1f);
        float thigh = Mathf.Lerp(2.0f, 1.5f, high);
        // Bend the knee until the foot reaches the ground: the shin's angle from down.
        float kneeY = -body.Thigh * Mathf.Cos(thigh);
        float reach = (kneeY + hips - ankle) / body.Shin;
        float shin = Mathf.Acos(Mathf.Clamp(reach, -1f, 1f));
        float knee = thigh - shin;
        // Short legs on a high seat don't reach the ground: the feet dangle and swing.
        float dangle = Mathf.Clamp((reach - 1f) * 4f, 0f, 1f);
        for (int s = 0; s < 2; s++)
        {
            float side = s == 0 ? 1f : -1f;
            var (hip, kn, foot) = s == 0 ? (Joint.HipL, Joint.KneeL, Joint.FootL) : (Joint.HipR, Joint.KneeR, Joint.FootR);
            p[hip] = new Vector3(-thigh, 0.08f * side, 0.1f * side * (1f - high));
            p[kn].X = knee - 0.18f * dangle * Mathf.Sin(t * 1.9f + s * 2.2f);
            p[foot].X = thigh - knee - 0.15f;
        }

        float breath = Mathf.Sin(t * 1.5f);
        float look = Mathf.Sin(t * 0.21f) * Mathf.Sin(t * 0.13f + 1f);
        p[Joint.Spine] = new Vector3(0.16f + 0.015f * breath, 0, 0);
        p[Joint.Neck] = new Vector3(-0.08f + 0.04f * Mathf.Sin(t * 0.29f), 0.3f * look, 0);
        // Forearms over the knees, hands hanging loose between them.
        p[Joint.ShoulderL] = new Vector3(-0.75f + 0.25f * high, 0, 0.18f);
        p[Joint.ShoulderR] = new Vector3(-0.75f + 0.25f * high, 0, -0.18f);
        p[Joint.ElbowL].X = -0.85f;
        p[Joint.ElbowR].X = -0.85f;
        p[Joint.HandL] = new Vector3(0.25f, 0, 0.1f);
        p[Joint.HandR] = new Vector3(0.25f, 0, -0.1f);
    }

    // What a sitter does now and then (anim-15), laid over Sit by weight w: hug the
    // knees, lean back on the hands, look up at the night sky, or hold the palms out to
    // a fire. Hugging and leaning back only on the ground.
    public enum SitFidget : byte { None, HugKnees, LeanBack, LookUp, Warm }

    public static void Fidget(Pose p, SitFidget f, float w, float t)
    {
        if (f == SitFidget.None || w <= 0f) return;
        void To(Joint j, Vector3 v) => p[j] = p[j].Lerp(v, w);
        switch (f)
        {
            case SitFidget.HugKnees:
                // Folded forward over the knees, arms wrapped round the shins, cheek down.
                To(Joint.Spine, new Vector3(0.42f, 0, 0));
                To(Joint.Neck, new Vector3(-0.05f, 0.25f, 0.12f));
                To(Joint.ShoulderL, new Vector3(-0.95f, 0, 0.02f));
                To(Joint.ShoulderR, new Vector3(-0.95f, 0, -0.02f));
                To(Joint.ElbowL, new Vector3(-1.45f, 0, 0));
                To(Joint.ElbowR, new Vector3(-1.45f, 0, 0));
                break;
            case SitFidget.LeanBack:
                // Leaning back on straight arms, palms flat behind, face to the sky.
                To(Joint.Spine, new Vector3(-0.38f, 0, 0));
                To(Joint.Neck, new Vector3(0.15f + 0.04f * Mathf.Sin(t * 0.4f), 0.2f * Mathf.Sin(t * 0.17f), 0));
                To(Joint.ShoulderL, new Vector3(0.65f, 0, 0.3f));
                To(Joint.ShoulderR, new Vector3(0.65f, 0, -0.3f));
                To(Joint.ElbowL, new Vector3(-0.1f, 0, 0));
                To(Joint.ElbowR, new Vector3(-0.1f, 0, 0));
                To(Joint.HandL, new Vector3(-1.1f, 0, 0));
                To(Joint.HandR, new Vector3(-1.1f, 0, 0));
                break;
            case SitFidget.LookUp:
                // Head tipped right back, looking slowly across the stars.
                To(Joint.Spine, new Vector3(-0.05f, 0, 0));
                To(Joint.Neck, new Vector3(-0.55f, 0.35f * Mathf.Sin(t * 0.25f), 0));
                break;
            case SitFidget.Warm:
                // Palms held out to the fire, a gentle rub now and then.
                float rub = 0.05f * Mathf.Sin(t * 6f) * Mathf.Max(Mathf.Sin(t * 0.7f), 0f);
                To(Joint.Spine, new Vector3(0.22f, 0, 0));
                To(Joint.Neck, new Vector3(-0.12f, 0, 0));
                To(Joint.ShoulderL, new Vector3(-1.15f + rub, 0, 0.12f));
                To(Joint.ShoulderR, new Vector3(-1.15f - rub, 0, -0.12f));
                To(Joint.ElbowL, new Vector3(-0.35f, 0, 0));
                To(Joint.ElbowR, new Vector3(-0.35f, 0, 0));
                To(Joint.HandL, new Vector3(-0.8f, 0, 0.1f));
                To(Joint.HandR, new Vector3(-0.8f, 0, -0.1f));
                break;
        }
    }

    // Lying curled on the left side along the way the figure faced, head forward, knees
    // drawn up, the left hand under the cheek and the right arm over the body, breathing
    // slowly. Asleep the breath slows further.
    public static void Lie(Pose p, PlayerRig body, float t, bool asleep)
    {
        p.Clear();
        float headTop = body.Height - body.HipHeight;
        // Shoulders are wider than hips: tip the chest up off the ground so the lower
        // shoulder and hip both rest on it, and the legs back level.
        float torso = Mathf.Max(body.ShoulderL.Y - body.HipHeight, 0.1f);
        float tip = Mathf.Asin(Mathf.Clamp((body.ShoulderL.X - body.HipWidth) / torso, 0f, 0.5f));
        // Roll onto the left side (head to +X), then turn so the head points forward.
        p[Joint.Hips] = new Vector3(0, -Mathf.Pi / 2f, -Mathf.Pi / 2f + tip);
        float curled = 0.6f * body.LegLength;
        p.HipsOffset = new Vector3(0.05f, body.HipWidth + 0.05f - body.HipHeight, -(headTop - curled) / 2f);

        // Knees drawn up, the upper leg resting over the lower one.
        p[Joint.HipL] = new Vector3(-1.15f, 0, 0.05f - tip);
        p[Joint.KneeL].X = 1.45f;
        p[Joint.FootL].X = 0.3f;
        p[Joint.HipR] = new Vector3(-1.35f, 0, 0.15f - tip);
        p[Joint.KneeR].X = 1.55f;
        p[Joint.FootR].X = 0.3f;

        float breath = Mathf.Sin(t * (asleep ? 0.9f : 1.3f));
        p[Joint.Spine] = new Vector3(0.22f + 0.025f * breath, 0, 0);
        p[Joint.Neck] = new Vector3(0.12f, 0, -tip);
        p[Joint.Head].X = asleep ? 0.1f : 0f;
        // The lower arm folds up under the cheek; the upper one drapes down in front.
        p[Joint.ShoulderL] = new Vector3(-2.2f, 0, -0.35f);
        p[Joint.ElbowL].X = -1.9f;
        p[Joint.HandL] = new Vector3(0, 0, 0.3f);
        p[Joint.ShoulderR] = new Vector3(-0.8f + 0.01f * breath, 0, 0.7f);
        p[Joint.ElbowR].X = -0.5f;
        p[Joint.HandR] = new Vector3(0.2f, 0, 0);
    }
}
