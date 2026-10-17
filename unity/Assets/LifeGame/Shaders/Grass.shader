// A port of Look.GrassShader (client/scripts/Look.cs): blades take their tile's
// colour at the root, the stylised tufts get the art-reference colour bands, and
// slow gusts roll across the field. Seasons and the cleared-cell animation are left out.
Shader "LifeGame/Grass"
{
    Properties
    {
        _InstColor("Instance colour (multimesh)", Color) = (1, 1, 1, 1)
        _FadeFar("Fade far", Float) = 0
        _Stylised("Stylised", Float) = 0
    }

    HLSLINCLUDE
    #include "LifeGameCommon.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float _FadeFar, _Stylised;
    CBUFFER_END

    UNITY_INSTANCING_BUFFER_START(PerInstance)
        UNITY_DEFINE_INSTANCED_PROP(float4, _InstColor)
    UNITY_INSTANCING_BUFFER_END(PerInstance)

    struct Attributes
    {
        float4 positionOS : POSITION;
        float4 color : COLOR;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 positionWS : TEXCOORD0;
        float3 tint : TEXCOORD1;
        float tip : TEXCOORD2;
        float gust : TEXCOORD3;
        float fogFactor : TEXCOORD4;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    Varyings GrassVertex(Attributes v)
    {
        Varyings o = (Varyings)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float3 vertex = v.positionOS.xyz;
        if (_FadeFar > 0.0)
        {
            float3 origin = TransformObjectToWorld(float3(0, 0, 0));
            float away = length(origin.xz - lg_player_position.xz);
            vertex *= 1.0 - smoothstep(0.65 * _FadeFar, _FadeFar, away);
        }
        float3 world = TransformObjectToWorld(vertex);
        float3 g = GodotPos(world);
        float tip = v.uv.y;
        float wave = sin(dot(g.xz, float2(0.23, 0.31)) - _Time.y * 1.3);
        float gust = smoothstep(0.2, 1.0, wave);
        float flutter = sin(_Time.y * 3.1 + g.x * 1.7 + g.z * 2.3) * 0.03;
        float wind = (0.45 + lg_wind_strength * 1.8) * (0.7 + 0.6 * lg_wind_gust);
        // The bend in Godot's world frame, then into Unity's (z flipped) and the blade's own.
        float3 bendG = float3(lg_wind_dir.x, 0.0, lg_wind_dir.y) * (0.06 + gust * 0.16) * wind + float3(flutter, 0.0, -flutter) * wind;
        float3 local = TransformWorldToObjectDir(float3(bendG.x, 0.0, -bendG.z), false);
        vertex.xz += local.xz * tip * tip;
        vertex.y -= gust * 0.06 * tip * tip;
        o.positionWS = TransformObjectToWorld(vertex);
        o.positionCS = TransformWorldToHClip(o.positionWS);
        float4 inst = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstColor);
        o.tint = FromSrgb(v.color.rgb * inst.rgb);
        o.tip = tip;
        o.gust = gust;
        o.fogFactor = ComputeFogFactor(o.positionCS.z);
        return o;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GrassVertex
            #pragma fragment GrassFragment
            #pragma multi_compile_local _ _UNITY_PBR
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            half4 GrassFragment(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float tip = i.tip;
                float3 tint = i.tint;
                float3 warm = tint * float3(1.3, 1.3, 1.0) + float3(0.02, 0.02, 0.0);
                float3 color = lerp(tint, warm, smoothstep(0.0, 1.0, tip));
                if (_Stylised > 0.5)
                {
                    float lum = dot(tint, float3(0.3, 0.59, 0.11));
                    float3 olive = lerp(tint, float3(lum * 1.1, lum * 1.05, lum * 0.55), 0.3);
                    float3 rootShade = olive * float3(0.62, 0.65, 0.82);
                    float3 litTip = olive * float3(1.36, 1.3, 0.78);
                    color = lerp(rootShade, olive, smoothstep(0.05, 0.4, tip));
                    color = lerp(color, litTip, smoothstep(0.6, 0.95, tip));
                }
                color += float3(0.05, 0.05, 0.02) * i.gust * tip;
                color *= lerp(1.0, 0.88, lg_wetness);
                float3 albedo = saturate(color);
                float shade = CloudShadow(i.positionWS);
                // Both faces of a blade are lit like the ground beneath it.
                float3 N = float3(0, 1, 0);

                float2 screenUV = GetNormalizedScreenSpaceUV(i.positionCS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float ssao = 1.0;
            #if defined(_SCREEN_SPACE_OCCLUSION)
                ssao = GetScreenSpaceAmbientOcclusion(screenUV).indirectAmbientOcclusion;
            #endif
            #if defined(_UNITY_PBR)
                float wrapped = saturate(dot(N, mainLight.direction));
                float3 direct = mainLight.color * wrapped * mainLight.shadowAttenuation;
            #else
                float wrapped = saturate((dot(N, mainLight.direction) + 0.3) / 1.3);
                float3 direct = mainLight.color * wrapped * wrapped * mainLight.shadowAttenuation * shade;
            #endif
                float3 outColor = albedo * (SampleSH(N) * ssao + direct);
                return half4(MixFog(outColor, i.fogFactor), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing
            float4 DepthVertex(Attributes v) : SV_POSITION { return GrassVertex(v).positionCS; }
            half DepthFragment() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex GrassVertex
            #pragma fragment NormalsFragment
            #pragma multi_compile_instancing
            half4 NormalsFragment(Varyings i) : SV_Target { return half4(0, 1, 0, 0); }
            ENDHLSL
        }
    }
}
