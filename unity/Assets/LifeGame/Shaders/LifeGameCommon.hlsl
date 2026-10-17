// Shared by the LifeGame spike shaders. A port of the pieces every Godot shader in
// client/scripts/Look.cs shares (its WeatherCode block): the global weather values
// and the cloud shadow. Globals are set by SpikeWorld from the Godot export
// (scene.json "global_shader_params"), named lg_<godot name>.
#ifndef LIFEGAME_COMMON_INCLUDED
#define LIFEGAME_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float lg_wetness;
float lg_wind_strength;
float lg_wind_gust;
float2 lg_wind_dir;
float lg_cloud_cover;
float2 lg_cloud_offset;
float lg_world_seconds;
float3 lg_player_position;

TEXTURE2D(lg_clouds);
SAMPLER(samplerlg_clouds);

// Godot's coordinates are mirrored on z (right-handed); the export flips z when it
// lands in Unity. Patterns keyed to world xz read it back in Godot's frame so the
// cloud shadows, brush strokes and grass gusts fall on the same spots.
float3 GodotPos(float3 p) { return float3(p.x, p.y, -p.z); }

float CloudShadow(float3 positionWS)
{
    if (lg_cloud_cover <= 0.01) return 1.0;
    float3 p = GodotPos(positionWS);
    float n = SAMPLE_TEXTURE2D(lg_clouds, samplerlg_clouds, (p.xz + lg_cloud_offset) * 0.01).r;
    float edge = lerp(0.68, 0.2, lg_cloud_cover);
    return 1.0 - smoothstep(edge - 0.1, edge + 0.12, n) * 0.5;
}

// Godot authors palette colours in sRGB and the shaders raise them to 2.2.
float3 FromSrgb(float3 c) { return pow(max(c, 0.0), 2.2); }

float3 ShadowCasterPosition(float3 positionWS, float3 normalWS, float3 lightDirection)
{
    return ApplyShadowBias(positionWS, normalWS, lightDirection);
}

#endif
