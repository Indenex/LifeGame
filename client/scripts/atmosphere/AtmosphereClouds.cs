using Godot;

namespace LifeGame;

// Painted clouds in the sky (env-39, Justin: volumetric clouds). The sky shader marches a
// few steps through a slab of cloud 160 to 230 m up, drawn at half resolution: soft
// Ghibli cumulus with flat bases and domed tops, lit in soft painted bands (warm white on
// top and on the sun's side, a cool lavender grey underneath), warm at golden hour, grey
// and heavy in rain. They are the same cloud field as the shadows on the ground
// (Look.cloud_shadow: cloud_cover, cloud_offset), so a cloud overhead casts the patch of
// shade under it, and they drift with the wind.
//
// Quality is a Graphics option: 0 flat (the old single layer, cheapest), 1 painted, 2 high
// (more steps). Command line: --clouds=0|1|2 overrides it.
public partial class Atmosphere
{
    public static int CloudQuality = 1;
    static readonly StringName NCloudQuality = "cloud_quality", NCloudCam = "cloud_cam", NRainHeavy = "rain_heavy";

    void ReadyClouds()
    {
        CloudQuality = Mathf.Clamp(Settings.Get("graphics", "clouds", 1), 0, 2);
        if (ArgFloat("clouds") is { } level) CloudQuality = Mathf.Clamp((int)level, 0, 2);
    }

    void UpdateClouds()
    {
        sky.SetShaderParameter(NCloudQuality, (float)CloudQuality);
        // The clouds sit over the world, not round the camera, so they line up with their
        // shadows on the ground.
        if (GetViewport()?.GetCamera3D() is { } camera) sky.SetShaderParameter(NCloudCam, camera.GlobalPosition);
        sky.SetShaderParameter(NRainHeavy, Mathf.SmoothStep(0.1f, 0.8f, rainAmount));
    }
}
