// A port of Look.WaterCode (client/scripts/Look.cs): a calm deep colour that only
// softly catches the light, slow wind ripples, shallows showing the bank, and a
// soft fade at the edge. Rain rings and the far-land rim are left out.
// Needs the URP asset's Depth Texture (SpikeSetup turns it on).
Shader "LifeGame/Water"
{
    Properties
    {
        _WaterColor("Water", Color) = (0.14, 0.34, 0.40, 0.9)
        _ShallowColor("Shallow", Color) = (0.30, 0.55, 0.55, 1)
        _SkyTint("Sky tint", Color) = (0.62, 0.72, 0.78, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "LifeGameCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // The material's colours arrive in linear (SetColor converts), so no pow 2.2 here.
            CBUFFER_START(UnityPerMaterial)
                float4 _WaterColor, _ShallowColor, _SkyTint;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float fogFactor : TEXCOORD1; };

            float Ripples(float2 p, float t, float stir)
            {
                float h = sin(dot(p, float2(0.9, 0.5)) * 1.7 + t * 1.1) * 0.5
                        + sin(dot(p, float2(-0.4, 1.0)) * 2.6 + t * 1.6) * 0.3
                        + sin(dot(p, float2(0.7, -0.8)) * 4.3 + t * 2.3) * 0.15;
                return h * stir;
            }

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 p = GodotPos(i.positionWS).xz;
                float t = _Time.y;
                float stir = 0.03 + 0.06 * lg_wind_strength * (0.5 + 0.8 * lg_wind_gust);
                float e = 0.05;
                float h0 = Ripples(p, t, stir);
                float2 slope = float2(Ripples(p + float2(e, 0.0), t, stir) - h0, Ripples(p + float2(0.0, e), t, stir) - h0) / e;
                float3 nG = normalize(float3(-slope.x, 1.0, -slope.y));
                float3 N = float3(nG.x, nG.y, -nG.z);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);

                float2 screenUV = GetNormalizedScreenSpaceUV(i.positionCS);
                float sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float fragEye = LinearEyeDepth(i.positionCS.z, _ZBufferParams);
                float through = clamp(sceneEye - fragEye, 0.0, 10.0);

                float across = pow(1.0 - saturate(dot(N, V)), 3.0);
                float glint = clamp(length(slope) * 0.6, 0.0, 0.5);
                float3 albedo = lerp(lerp(_ShallowColor.rgb, _WaterColor.rgb, smoothstep(0.0, 1.2, through)), _SkyTint.rgb, across * 0.35 + glint);
                float alpha = _WaterColor.a * lerp(0.85, 1.0, smoothstep(0.05, 0.8, through)) * smoothstep(0.0, 0.04, through);

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.positionCS = i.positionCS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                inputData.fogCoord = i.fogFactor;
                inputData.bakedGI = SampleSH(N);
                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask = half4(1, 1, 1, 1);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                // Godot: SPECULAR 0.12 (F0 = 0.16 * 0.12^2), ROUGHNESS 0.32.
                surface.specular = (0.16 * 0.12 * 0.12).xxx;
                surface.smoothness = 1.0 - 0.32;
                surface.occlusion = 1;
                surface.alpha = alpha;
                half4 c = UniversalFragmentBlinnPhong(inputData, surface);
                c.rgb = MixFog(c.rgb, i.fogFactor);
                c.a = alpha;
                return c;
            }
            ENDHLSL
        }
    }
}
