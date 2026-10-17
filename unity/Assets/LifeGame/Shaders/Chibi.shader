// A port of ChibiLook (client/scripts/models/ChibiLook.cs, model-6): flat colour
// from the vertex paint, a darker rim, and three crisp light bands whose shade
// leans violet.
Shader "LifeGame/Chibi"
{
    Properties
    {
        _ShadeTint("Shade tint", Color) = (0.6, 0.56, 0.84, 1)
        _RimDark("Rim dark", Float) = 0.58
    }

    HLSLINCLUDE
    #include "LifeGameCommon.hlsl"
    CBUFFER_START(UnityPerMaterial)
        float4 _ShadeTint;
        float _RimDark;
    CBUFFER_END

    struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; };
    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 positionWS : TEXCOORD0;
        float3 normalWS : TEXCOORD1;
        float3 tint : TEXCOORD2;
        float fogFactor : TEXCOORD3;
    };

    Varyings ChibiVertex(Attributes v)
    {
        Varyings o;
        o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
        o.normalWS = TransformObjectToWorldNormal(v.normalOS);
        o.positionCS = TransformWorldToHClip(o.positionWS);
        o.tint = FromSrgb(v.color.rgb);
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
            #pragma vertex ChibiVertex
            #pragma fragment Frag
            #pragma multi_compile_local _ _UNITY_PBR
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            half4 Frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float3 N = normalize(i.normalWS) * (IS_FRONT_VFACE(face, true, false) ? 1.0 : -1.0);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 c = i.tint * (0.94 + 0.12 * smoothstep(0.25, 0.75, normalize(i.normalWS).y));
                float rim = 1.0 - saturate(abs(dot(N, V)));
                c *= lerp(1.0, _RimDark, smoothstep(0.62, 0.86, rim));
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float ssao = 1.0;
            #if defined(_SCREEN_SPACE_OCCLUSION)
                ssao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS)).indirectAmbientOcclusion;
            #endif
                float l = saturate(dot(N, mainLight.direction)) * mainLight.shadowAttenuation;
            #if defined(_UNITY_PBR)
                float3 direct = mainLight.color * l;
            #else
                float lit = 0.35 * smoothstep(0.06, 0.1, l) + 0.65 * smoothstep(0.42, 0.47, l);
                float3 tone = lerp(_ShadeTint.rgb, float3(1, 1, 1), smoothstep(0.06, 0.1, l));
                float3 direct = mainLight.color * tone * (0.2 + 0.6 * lit);
            #endif
                float3 outColor = saturate(c) * (SampleSH(N) * ssao + direct);
                return half4(MixFog(outColor, i.fogFactor), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            float3 _LightDirection;
            float4 ShadowVertex(Attributes v) : SV_POSITION
            {
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                float3 n = TransformObjectToWorldNormal(v.normalOS);
                return ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(p, n, _LightDirection)));
            }
            half4 ShadowFragment() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            float4 DepthVertex(Attributes v) : SV_POSITION { return TransformObjectToHClip(v.positionOS.xyz); }
            half DepthFragment() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex ChibiVertex
            #pragma fragment NormalsFragment
            half4 NormalsFragment(Varyings i) : SV_Target { return half4(normalize(i.normalWS), 0.0); }
            ENDHLSL
        }
    }
}
