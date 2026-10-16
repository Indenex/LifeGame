using Godot;

namespace LifeGame;

// Weather you can see coming (atmosphere-14, D-085). When the server turns the
// weather (Weather.TargetCloud and TargetRain move ahead of Cloud and Rain), the
// change shows first on the upwind side of the sky: thicker, darker cloud low on
// that horizon and grey curtains of rain hanging under it, minutes before it
// arrives. When rain clears with the sun out and low, a faint rainbow stands
// opposite the sun.
//
// Command line: --front=1 shows a storm front approaching, --rainbow=1 holds a
// rainbow, for screenshots.
public partial class Atmosphere
{
    static readonly StringName NFrontDir = "front_dir";
    static readonly StringName NFrontCloud = "front_cloud";
    static readonly StringName NFrontRain = "front_rain";
    static readonly StringName NFrontAmount = "front_amount";
    static readonly StringName NRainbow = "rainbow";

    float aheadCloud = 0.2f, aheadRain, front, rainbow;
    float sinceRain = 999f;
    float? argFront, argRainbow;

    void ReadyFronts()
    {
        argFront = ArgFloat("front");
        argRainbow = ArgFloat("rainbow");
    }

    void OnWeatherAhead(SpacetimeDB.Types.Weather w)
    {
        aheadCloud = w.TargetCloud;
        aheadRain = w.TargetRain;
    }

    void UpdateFronts(float dt, float sunHeight, float overcast)
    {
        float cloudAhead = argFront is { } f ? Mathf.Lerp(cloud, 1f, f) : aheadCloud;
        float rainAhead = argFront is { } g ? Mathf.Lerp(rainAmount, 0.8f, g) : aheadRain;
        // How much worse the weather is about to get than it is now.
        float coming = Mathf.SmoothStep(0.05f, 0.35f, Mathf.Max(cloudAhead - cloud, rainAhead - rainAmount));
        front = Mathf.Lerp(front, argFront ?? coming, 1f - Mathf.Exp(-dt / 8f));

        // A rainbow for a few minutes after rain, while the sun is out and not high.
        sinceRain = rainAmount > 0.12f ? 0f : sinceRain + dt;
        float after = rainAmount < 0.08f ? 1f - Mathf.SmoothStep(60f, 240f, sinceRain) : 0f;
        float sunLow = Mathf.SmoothStep(0.02f, 0.1f, sunHeight) * (1f - Mathf.SmoothStep(0.45f, 0.65f, sunHeight));
        float bow = argRainbow ?? after * sunLow * (1f - Mathf.SmoothStep(0.55f, 0.85f, overcast));
        rainbow = Mathf.Lerp(rainbow, bow, 1f - Mathf.Exp(-dt / 6f));

        // Weather arrives from upwind: the opposite of where the clouds drift to.
        sky.SetShaderParameter(NFrontDir, new Vector3(-WindDirection.X, 0f, -WindDirection.Y));
        sky.SetShaderParameter(NFrontCloud, cloudAhead);
        sky.SetShaderParameter(NFrontRain, rainAhead);
        sky.SetShaderParameter(NFrontAmount, front);
        sky.SetShaderParameter(NRainbow, rainbow);
    }
}
