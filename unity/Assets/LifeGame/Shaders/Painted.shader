// A port of Look.PaintedCode (client/scripts/Look.cs): the ground, rocks, bark and
// leaves, plus the PAINTED_FOLIAGE variant the painted trees use (model-10). The
// Godot shader is the spec; the lines below keep its names and order so the two
// can be read side by side. Left out for the spike (all off at noon in summer):
// seasons and snow, puddles, wading wetness, felled-tree animation, see-through
// ghosts, the far-land rim.
//
// _UNITY_PBR swaps the hand-written Godot light() for URP's own PBR lighting on
// the same painted albedo, to show what Unity's stock lighting does with it.
Shader "LifeGame/Painted"
{
    Properties
    {
        [MainColor] _Albedo("Albedo", Color) = (1, 1, 1, 1)
        _InstColor("Instance colour (multimesh)", Color) = (1, 1, 1, 1)
        _Brush("Brush", 2D) = "grey" {}
        _Detail("Detail", 2D) = "grey" {}
        _Dabs("Dabs", 2D) = "grey" {}
        _Sway("Sway", Float) = 0
        _SwayHeight("Sway height", Float) = 1
        _BrushScale("Brush scale", Float) = 0.09
        _BrushStrength("Brush strength", Float) = 0.14
        _DetailScale("Detail scale", Float) = 1
        _DetailStretch("Detail stretch", Vector) = (1, 1, 1, 0)
        _Bump("Bump", Float) = 1
        _Cavity("Cavity", Float) = 0.4

        // PAINTED_FOLIAGE
        _ShadeCol("Shade", Color) = (0.11, 0.155, 0.145, 1)
        _BaseCol("Base", Color) = (0.16, 0.22, 0.2, 1)
        _LitCol("Lit", Color) = (0.22, 0.29, 0.24, 1)
        _Ink("Ink", Color) = (0.1, 0.11, 0.08, 1)
        _BarkDark("Bark dark", Color) = (0.24, 0.16, 0.19, 1)
        _BarkLit("Bark lit", Color) = (0.39, 0.31, 0.27, 1)
        _ShadeTint("Shade tint", Color) = (0.72, 0.7, 0.9, 1)
        _WarmTint("Warm tint", Color) = (1, 0.98, 0.92, 1)
        _DabSize("Dab size", Float) = 3.5
        _ConeLight("Cone light", Float) = 0.75
        _CrownY("Crown y", Float) = -1
        _TreeHeight("Tree height", Float) = 4.4

        [Toggle(_PAINTED_FOLIAGE)] _PaintedFoliage("Painted foliage", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    HLSLINCLUDE
    #include "LifeGameCommon.hlsl"

    TEXTURE2D(_Brush);  SAMPLER(sampler_Brush);
    TEXTURE2D(_Detail); SAMPLER(sampler_Detail);
    TEXTURE2D(_Dabs);   SAMPLER(sampler_Dabs);

    CBUFFER_START(UnityPerMaterial)
        float4 _Albedo;
        float _Sway, _SwayHeight, _BrushScale, _BrushStrength, _DetailScale, _Bump, _Cavity;
        float4 _DetailStretch;
        float4 _ShadeCol, _BaseCol, _LitCol, _Ink, _BarkDark, _BarkLit, _ShadeTint, _WarmTint;
        float _DabSize, _ConeLight, _CrownY, _TreeHeight;
    CBUFFER_END

    UNITY_INSTANCING_BUFFER_START(PerInstance)
        UNITY_DEFINE_INSTANCED_PROP(float4, _InstColor)
    UNITY_INSTANCING_BUFFER_END(PerInstance)

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float4 color : COLOR;
        float2 uv : TEXCOORD0;
        float2 uv2 : TEXCOORD1;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 positionWS : TEXCOORD0;
        float3 normalWS : TEXCOORD1;
        float3 tint : TEXCOORD2;
        float4 uvs : TEXCOORD3;      // uv, uv2
        float3 obj : TEXCOORD4;      // object position in Godot's frame
        float3 coneN : TEXCOORD5;    // crown normal, world space
        float fogFactor : TEXCOORD6;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    // vertex(): wind sway, then the tint. Object space here is Unity's (z mirrored).
    Varyings PaintedVertex(Attributes v)
    {
        Varyings o = (Varyings)0;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        float3 vertex = v.positionOS.xyz;
        float3 world = TransformObjectToWorld(vertex);
        if (_Sway > 0.0)
        {
            float3 g = GodotPos(world);
            float h = clamp(vertex.y / _SwayHeight, 0.0, 1.5);
            float t = _Time.y * 0.9 + g.x * 0.21 + g.z * 0.17;
            float gust = (0.45 + 0.45 * lg_wind_gust + 0.15 * sin(_Time.y * 0.23 + g.x * 0.03)) * (0.5 + lg_wind_strength * 1.6);
            vertex.x += sin(t) * _Sway * h * h * gust;
            vertex.z -= cos(t * 0.73) * _Sway * 0.6 * h * h * gust;
        }
        o.positionWS = TransformObjectToWorld(vertex);
        o.normalWS = TransformObjectToWorldNormal(v.normalOS);
        o.obj = float3(vertex.x, vertex.y, -vertex.z);
        float3 outDir = length(vertex.xz) > 1e-4 ? normalize(float3(vertex.x, 0.0, vertex.z)) : float3(0.0, 0.0, 1.0);
        float3 crown = _CrownY < 0.0 ? outDir + float3(0.0, 0.45, 0.0) : (vertex - float3(0.0, _CrownY, 0.0)) * float3(1.0, 0.75, 1.0);
        o.coneN = normalize(TransformObjectToWorldDir(crown));
        float4 inst = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstColor);
        o.tint = FromSrgb(v.color.rgb * inst.rgb) * _Albedo.rgb;
        o.uvs = float4(v.uv, v.uv2);
        o.positionCS = TransformWorldToHClip(o.positionWS);
        o.fogFactor = ComputeFogFactor(o.positionCS.z);
        return o;
    }

    float DabAt(float3 positionWS)
    {
        float3 p = GodotPos(positionWS);
        float2 a = float2(p.x * 0.8, p.y * 1.3) / _DabSize;
        float2 b = float2(p.z * 0.8, p.y * 1.3) / _DabSize + 0.37;
        return lerp(SAMPLE_TEXTURE2D(_Dabs, sampler_Dabs, a).r, SAMPLE_TEXTURE2D(_Dabs, sampler_Dabs, b).r, 0.5);
    }

    // World-space detail projected from three sides.
    float Relief(float3 positionWS, float3 n)
    {
        float3 p = GodotPos(positionWS);
        float3 w = pow(abs(n), 4.0);
        w /= (w.x + w.y + w.z);
        float3 q = p * _DetailStretch.xyz * _DetailScale * 0.25;
        return SAMPLE_TEXTURE2D(_Detail, sampler_Detail, q.zy).r * w.x
             + SAMPLE_TEXTURE2D(_Detail, sampler_Detail, q.xz).r * w.y
             + SAMPLE_TEXTURE2D(_Detail, sampler_Detail, q.xy).r * w.z;
    }

    float3 PaintedColor(float2 uv, float2 uv2, bool front, float up, float3 obj, float dab)
    {
        if (uv2.x < 0.5)
        {
            float3 o = obj / _TreeHeight;
            float ang = atan2(o.x, o.z);
            float stroke = frac(ang * 1.4 + sin(o.y * 18.0 + ang * 3.0) * 0.12);
            return lerp(_BarkDark.rgb, _BarkLit.rgb, 0.7 * smoothstep(0.55, 0.62, stroke) * (1.0 - smoothstep(0.78, 0.85, stroke)));
        }
        if (uv2.x < 1.5) return _ShadeCol.rgb;
        float across = abs(uv.x), along = uv.y, rnd = uv2.y;
        float edge = 0.55 + 0.25 * (dab - 0.5) + 0.12 * rnd;
        float patch = (1.0 - smoothstep(edge - 0.04, edge + 0.04, across))
            * smoothstep(0.2 + 0.12 * rnd, 0.28 + 0.12 * rnd, along)
            * (1.0 - smoothstep(0.8, 0.86, along));
        float spots = smoothstep(0.6, 0.63, dab) * smoothstep(0.1, 0.3, along);
        patch = max(patch * 0.85, spots);
        patch *= front ? smoothstep(0.05, 0.3, up) : 0.0;
        float3 col = lerp(_BaseCol.rgb, _LitCol.rgb, patch);
        col *= lerp(float3(1, 1, 1), _WarmTint.rgb, smoothstep(0.7, 0.9, along) * patch);
        col = lerp(col, _ShadeCol.rgb, 0.65 * smoothstep(0.72, 0.98, across) * (1.0 - patch));
        col = lerp(_ShadeCol.rgb, col, smoothstep(0.16, 0.4, along + (dab - 0.5) * 0.15));
        if (!front) col = lerp(col, _ShadeCol.rgb, 0.6);
        float line_ = smoothstep(0.9, 1.0, across) * smoothstep(0.7, 0.9, along) * 0.5 + smoothstep(0.9, 1.0, along);
        return lerp(col, _Ink.rgb, 0.7 * saturate(line_));
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Cull [_Cull]

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PaintedVertex
            #pragma fragment PaintedFragment
            #pragma shader_feature_local _PAINTED_FOLIAGE
            #pragma multi_compile_local _ _UNITY_PBR
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            half4 PaintedFragment(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                bool front = IS_FRONT_VFACE(face, true, false);
                float3 worldPos = i.positionWS;
                float3 g = GodotPos(worldPos);
                // Godot flips NORMAL on back faces before the shader runs (cull off).
                float3 N = normalize(i.normalWS) * (front ? 1.0 : -1.0);

                float2 buv = g.xz * _BrushScale + (g.y * 0.21).xx;
                float a = SAMPLE_TEXTURE2D(_Brush, sampler_Brush, buv).r;
                float b = SAMPLE_TEXTURE2D(_Brush, sampler_Brush, buv * 2.7 + float2(0.31, 0.77)).r;
                float paint = (a - 0.5) * _BrushStrength + (b - 0.5) * _BrushStrength * 0.6;

                float h = Relief(worldPos, N);
                float dab = 0.5;
            #if defined(_PAINTED_FOLIAGE)
                dab = DabAt(worldPos);
                float3 painted = PaintedColor(i.uvs.xy, i.uvs.zw, front, N.y, i.obj, dab);
                float3 color = painted * i.tint;
            #else
                float3 color = i.tint * (1.0 + paint * 2.0);
            #endif
                color *= lerp(1.0 - _Cavity, 1.0 + _Cavity * 0.35, smoothstep(0.15, 0.75, h));
                float wet = lg_wetness * lerp(0.45, 1.0, smoothstep(0.4, 0.9, N.y)) * lerp(0.75, 1.0, h);
                color *= lerp(1.0, 0.8, wet);
                float3 albedo = saturate(color);
                float shade = CloudShadow(worldPos);

                // Relief bends the normal (screen-space derivative bump, as in Godot).
                float3 dpdx = ddx(worldPos), dpdy = ddy(worldPos);
                float3 r1 = cross(dpdy, N), r2 = cross(N, dpdx);
                float det = dot(dpdx, r1);
                float3 grad = sign(det) * (ddx(h) * r1 + ddy(h) * r2);
                N = normalize(abs(det) * N - grad * _Bump * 0.035);
            #if defined(_PAINTED_FOLIAGE)
                if (i.uvs.z > 0.5) N = normalize(lerp(front ? N : -N, i.coneN, _ConeLight));
            #endif
                float ao = lerp(1.0, smoothstep(0.0, 0.6, h), _Cavity * 0.6);
                const float aoLightAffect = 0.3;

                float2 screenUV = GetNormalizedScreenSpaceUV(i.positionCS);
                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);
                Light mainLight = GetMainLight(shadowCoord);
                float3 V = GetWorldSpaceNormalizeViewDir(worldPos);
                float ssaoDirect = 1.0, ssaoIndirect = 1.0;
            #if defined(_SCREEN_SPACE_OCCLUSION)
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screenUV);
                ssaoDirect = aoFactor.directAmbientOcclusion;
                ssaoIndirect = aoFactor.indirectAmbientOcclusion;
            #endif

            #if defined(_UNITY_PBR)
                InputData inputData = (InputData)0;
                inputData.positionWS = worldPos;
                inputData.positionCS = i.positionCS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = shadowCoord;
                inputData.fogCoord = i.fogFactor;
                inputData.bakedGI = SampleSH(N);
                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask = half4(1, 1, 1, 1);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.specular = 0;
                surface.metallic = 0;
                surface.smoothness = lerp(0.0, 0.25, wet);
                surface.occlusion = ao;
                surface.alpha = 1;
                half4 pbr = UniversalFragmentPBR(inputData, surface);
                pbr.rgb = MixFog(pbr.rgb, i.fogFactor);
                return pbr;
            #else
                // light(): the hand-painted light model, one directional sun.
                float3 L = mainLight.direction;
                float atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                float3 diffuse = 0;
                float sky = shade;
            #if defined(_PAINTED_FOLIAGE)
                float elevation = L.y;
                float lowSun = 1.0 - smoothstep(0.2, 0.55, elevation);
                float wrap = lerp(0.4, 0.85, lowSun);
                float wrapped = saturate(dot(N, L) * (1.0 - wrap) + wrap);
                float l = wrapped * atten * sky + (dab - 0.5) * 0.22;
                const float soft = 0.025;
                float mid = smoothstep(0.3 - soft, 0.3 + soft, l);
                float lit = smoothstep(0.62 - soft, 0.62 + soft, l);
                float band = 0.1 + 0.3 * lowSun + 0.36 * mid + 0.3 * lit;
                float3 tone = lerp(_ShadeTint.rgb, float3(1, 1, 1), mid);
                diffuse += mainLight.color * tone * band;
                float back = max(0.0, dot(V, -L));
                float edgeV = 1.0 - abs(dot(N, V));
                diffuse += mainLight.color * sky * (back * back * (0.35 + 0.5 * edgeV)) * lerp(0.25, 1.0, lowSun);
            #else
                float wrapped = saturate((dot(N, L) + 0.3) / 1.3);
                diffuse += mainLight.color * wrapped * wrapped * atten * sky;
            #endif
            #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint li = 0; li < count; li++)
                {
                    Light add = GetAdditionalLight(li, worldPos);
                    float w2 = saturate((dot(N, add.direction) + 0.3) / 1.3);
                    diffuse += add.color * w2 * w2 * add.distanceAttenuation * add.shadowAttenuation;
                }
            #endif
                float aoAll = min(ao, ssaoIndirect);
                float3 ambient = SampleSH(N) * aoAll;
                float3 direct = diffuse * lerp(1.0, min(ao, ssaoDirect), aoLightAffect);
                float3 outColor = albedo * (ambient + direct);
                outColor = MixFog(outColor, i.fogFactor);
                return half4(outColor, 1.0);
            #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;

            float4 ShadowVertex(Attributes v) : SV_POSITION
            {
                Varyings o = PaintedVertex(v);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - o.positionWS);
            #else
                float3 lightDir = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(o.positionWS, o.normalWS, lightDir));
                return ApplyShadowClamping(positionCS);
            }

            half4 ShadowFragment() : SV_Target { return 0; }
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

            float4 DepthVertex(Attributes v) : SV_POSITION { return PaintedVertex(v).positionCS; }
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
            #pragma vertex PaintedVertex
            #pragma fragment NormalsFragment
            #pragma multi_compile_instancing

            half4 NormalsFragment(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float3 n = normalize(i.normalWS) * (IS_FRONT_VFACE(face, true, false) ? 1.0 : -1.0);
                return half4(n, 0.0);
            }
            ENDHLSL
        }
    }
}
