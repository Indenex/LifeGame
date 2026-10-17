// A port of the Atmosphere sky shader (client/scripts/atmosphere/Atmosphere.cs):
// gradient, sun glow and disc, the drifting cloud layer, and the grade's warm
// cloud rims at a low sun. Moon, stars, fronts, storms and rainbows are left out.
// Godot also lays its fog over the sky (fog_sky_affect); that is done here too.
// Directions are worked in Godot's frame (z flipped) so sun_dir and the clouds match.
Shader "LifeGame/Sky"
{
    Properties
    {
        _TopColor("Top", Color) = (0.29, 0.46, 0.7, 1)
        _HorizonColor("Horizon", Color) = (0.76, 0.82, 0.86, 1)
        _GroundColor("Ground", Color) = (0.24, 0.28, 0.22, 1)
        _SunColor("Sun", Color) = (0.9, 0.86, 0.79, 1)
        _SunDir("Sun direction (Godot frame)", Vector) = (0, 1, 0, 0)
        _Daylight("Daylight", Float) = 1
        _CloudCover("Cloud cover", Float) = 0.2
        _CloudOffset("Cloud offset", Vector) = (0, 0, 0, 0)
        _Grade("Grade", Float) = 1
        _Clouds("Clouds", 2D) = "grey" {}
        _FogColor("Fog colour", Color) = (0.69, 0.71, 0.71, 1)
        _FogSkyAffect("Fog sky affect", Float) = 0.5
        _AerialPerspective("Aerial perspective", Float) = 0.2
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Clouds); SAMPLER(sampler_Clouds);
            float4 _TopColor, _HorizonColor, _GroundColor, _SunColor, _SunDir, _CloudOffset, _FogColor;
            float _Daylight, _CloudCover, _Grade, _FogSkyAffect, _AerialPerspective;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            float3 SkyColor(float3 d)
            {
                float3 sunDir = normalize(_SunDir.xyz);
                float up = saturate(d.y);
                float3 col = lerp(_HorizonColor.rgb, _TopColor.rgb, pow(up, 0.45));
                col = lerp(col, _GroundColor.rgb, 1.0 - smoothstep(-0.25, 0.0, d.y));

                float s = max(dot(d, sunDir), 0.0);
                float sunVis = smoothstep(-0.15, 0.05, sunDir.y);
                col += _SunColor.rgb * (pow(s, 5.0) * 0.3 + pow(s, 60.0) * 0.5) * sunVis;
                col += _SunColor.rgb * smoothstep(0.9993, 0.9997, s) * 6.0 * step(-0.02, sunDir.y);

                if (d.y > 0.0)
                {
                    float2 uv = (d.xz / (d.y + 0.03) * 120.0 + _CloudOffset.xy) * 0.01;
                    float n = SAMPLE_TEXTURE2D(_Clouds, sampler_Clouds, uv).r;
                    float cover = _CloudCover;
                    float edge = 1.0 - cover;
                    float c = smoothstep(edge - 0.08, edge + 0.22, n) * smoothstep(0.0, 0.18, d.y);
                    float lit = 0.25 + 0.75 * _Daylight;
                    float3 cloudCol = lerp(_HorizonColor.rgb * 1.1, float3(0.92, 0.92, 0.95) * lit + _SunColor.rgb * 0.15 * sunVis, 0.5 + 0.5 * n);
                    cloudCol *= 1.0 - 0.45 * cover * smoothstep(0.4, 0.8, n);
                    float sunLow = smoothstep(-0.05, 0.05, sunDir.y) * (1.0 - smoothstep(0.2, 0.45, sunDir.y));
                    cloudCol += _SunColor.rgb * pow(s, 4.0) * (1.0 - smoothstep(0.35, 0.8, n)) * 1.4 * sunLow * _Grade;
                    col = lerp(col, cloudCol, c * 0.9);
                }
                return col;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 du = normalize(i.dir);
                float3 d = float3(du.x, du.y, -du.z);
                float3 col = SkyColor(d);
                float3 fogCol = lerp(_FogColor.rgb, col, _AerialPerspective);
                col = lerp(col, fogCol, _FogSkyAffect);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
