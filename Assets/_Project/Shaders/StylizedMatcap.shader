// Единый стилизованный шейдер проекта. Вариативность материалов — только ползунками:
//  - Matcap (Lighting)   — освещение из matcap-текстуры по нормали во view-space;
//  - Matcap (Reflection) — второй matcap, сэмплится по отражённому вектору (отражает «сферу» окружения);
//  - Vertical Gradient   — затемнение/тонирование по оси Y (книзу темнее);
//  - Linear Fog          — линейный туман по расстоянию до камеры. Цвет/старт/конец — глобальные
//                          (StylizedFogController), в материале только интенсивность.
//  - Per-object tint     — оттенок здания из UV1 (MapBuildingTintModifier) и цвет вершин (деревья).
//  - Curved World        — мир «загибается» вниз к горизонту (глобально, CurvedWorldController).
//  - Windows             — процедурные окна на стенах домов (без текстур), часть окон «горит».
// Совместим с SRP Batcher: все свойства материала в UnityPerMaterial, одинаково во всех проходах.
Shader "ActiveGrad/StylizedMatcap"
{
    Properties
    {
        [Header(Base)]
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        _ColorVariation ("Color Variation (world cells)", Range(0, 0.3)) = 0
        _VertexColorIntensity ("Vertex Color", Range(0, 1)) = 0

        [Header(Per Building Tint)]
        _TintIntensity ("Tint Intensity", Range(0, 1)) = 0
        _TintWarmth ("Warmth Range", Range(0, 0.3)) = 0.08
        _TintLightness ("Lightness Range", Range(0, 0.3)) = 0.07

        [Header(Matcap Lighting)]
        [NoScaleOffset] _MatcapTex ("Matcap (Lighting)", 2D) = "white" {}
        _MatcapIntensity ("Matcap Intensity", Range(0, 1)) = 1
        _MatcapBrightness ("Matcap Brightness", Range(0, 3)) = 1
        _LightInfluence ("Main Light Tint", Range(0, 1)) = 0.5

        [Header(Matcap Reflection)]
        [NoScaleOffset] _ReflectTex ("Matcap (Reflection)", 2D) = "black" {}
        _ReflectColor ("Reflection Tint", Color) = (1, 1, 1, 1)
        _ReflectIntensity ("Reflection Intensity", Range(0, 2)) = 0
        _ReflectFresnel ("Reflection Fresnel", Range(0, 1)) = 0.5
        _ReflectFresnelPower ("Fresnel Power", Range(0.5, 8)) = 3

        [Header(Vertical Gradient)]
        _GradientBottomColor ("Bottom Color", Color) = (0.36, 0.38, 0.48, 1)
        _GradientTopColor ("Top Color", Color) = (1, 1, 1, 1)
        _GradientIntensity ("Gradient Intensity", Range(0, 1)) = 0
        _GradientHeight ("Gradient Height", Float) = 20
        _GradientOffset ("Gradient Offset", Float) = 0
        _GradientPower ("Gradient Curve", Range(0.2, 4)) = 1
        [ToggleUI] _GradientWorldSpace ("Absolute World Y (else from pivot)", Float) = 0

        [Header(Received Shadows)]
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0
        _ShadowColor ("Shadow Color", Color) = (0.55, 0.6, 0.78, 1)

        [Header(Linear Fog)]
        _FogIntensity ("Fog Intensity", Range(0, 1)) = 1

        [Header(Windows)]
        _WindowIntensity ("Windows", Range(0, 1)) = 0
        _WindowGlassColor ("Glass Tint (unlit)", Color) = (0.62, 0.70, 0.80, 1)
        [HDR] _WindowLitColor ("Lit Window", Color) = (1.0, 0.86, 0.55, 1)
        _WindowLitAmount ("Lit Share", Range(0, 1)) = 0.3
        _WindowLitDay ("Lit Glow By Day", Range(0, 1)) = 0.7
        _WindowFadeStart ("Window Fade Start", Float) = 60
        _WindowFadeRange ("Window Fade Range", Float) = 40

        [Header(Emission)]
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)

        [Header(Surface)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

        [Header(Overlay on Ground)]
        _OffsetFactor ("Depth Offset Factor", Float) = 0
        _OffsetUnits ("Depth Offset Units", Float) = 0
        [IntRange] _StencilRef ("Stencil Ref", Range(0, 255)) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _ReflectColor;
            float4 _GradientBottomColor;
            float4 _GradientTopColor;
            float4 _EmissionColor;
            float4 _ShadowColor;
            float4 _WindowGlassColor;
            float4 _WindowLitColor;
            float _WindowIntensity;
            float _WindowLitAmount;
            float _WindowLitDay;
            float _WindowFadeStart;
            float _WindowFadeRange;
            float _ColorVariation;
            float _MatcapIntensity;
            float _MatcapBrightness;
            float _LightInfluence;
            float _ReflectIntensity;
            float _ReflectFresnel;
            float _ReflectFresnelPower;
            float _GradientIntensity;
            float _GradientHeight;
            float _GradientOffset;
            float _GradientPower;
            float _GradientWorldSpace;
            float _FogIntensity;
            float _ShadowStrength;
            float _VertexColorIntensity;
            float _TintIntensity;
            float _TintWarmth;
            float _TintLightness;
        CBUFFER_END

        // Curved World: xyz = центр (под персонажем), w = 1 если включено;
        // params: x = радиус плоской зоны, y = сила изгиба. Ставит CurvedWorldController.
        float4 _AG_CurveCenter;
        float4 _AG_CurveParams;

        float3 AG_ApplyCurvature(float3 positionWS)
        {
            float2 delta = positionWS.xz - _AG_CurveCenter.xz;
            float t = max(0.0, length(delta) - _AG_CurveParams.x);
            positionWS.y -= t * t * _AG_CurveParams.y * _AG_CurveCenter.w;
            return positionWS;
        }
        ENDHLSL

        // Предварительная запись глубины для полупрозрачных объектов (дома): рисуется перед Forward,
        // и в прозрачном Forward видна только ближняя поверхность, без просвечивания стен друг через друга.
        // Для непрозрачных материалов этот проход выключен в самом материале (disabledShaderPasses: SRPDefaultUnlit).
        Pass
        {
            Name "TransparentDepthPrepass"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex PrepassVert
            #pragma fragment PrepassFrag
            #pragma target 2.0

            float4 PrepassVert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformWorldToHClip(AG_ApplyCurvature(TransformObjectToWorld(positionOS.xyz)));
            }

            half4 PrepassFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            // Плоские слои на земле (дороги): вытягиваем по глубине вместо подъёма над землёй,
            // а stencil не даёт полупрозрачным перекрёсткам смешиваться дважды.
            Offset [_OffsetFactor], [_OffsetUnits]
            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/_Project/Shaders/MapVisualCommon.hlsl"

            TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MatcapTex);  SAMPLER(sampler_MatcapTex);
            TEXTURE2D(_ReflectTex); SAMPLER(sampler_ReflectTex);

            // x = start, y = end, z = 1 / (end - start), w = 1 если туман включён. Ставит StylizedFogController.
            float4 _AG_LinearFogColor;
            float4 _AG_LinearFogParams;

            // 0 = день, 1 = ночь. Ставит MapVisualGlobals.
            float _AG_DayNightBlend;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1; // x = теплота, y = светлота оттенка (-1..1)
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 uvHeight : TEXCOORD2; // xy = uv, z = высота для градиента
                float3 tint : TEXCOORD3;     // цвет вершин * оттенок объекта
                float4 window : TEXCOORD4;   // xy = UV окон (целое число ячеек на стену), z = маска стены, w = сид стены
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 flatPositionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 positionWS = AG_ApplyCurvature(flatPositionWS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                // Высота от пивота объекта (тайл Mapbox стоит на земле) либо абсолютная мировая Y.
                // Берём до изгиба, чтобы дальние здания не темнели от кривизны.
                float pivotY = GetObjectToWorldMatrix()._m13;
                float baseY = lerp(pivotY, 0.0, _GradientWorldSpace);
                output.uvHeight = float3(TRANSFORM_TEX(input.uv, _BaseMap), flatPositionWS.y - baseY);

                // Оттенок: тёплый/холодный сдвиг + светлее/темнее. Без UV1 (0,0) — без изменений.
                float warm = input.uv1.x * _TintWarmth;
                float3 objectTint = float3(1.0 + warm, 1.0 + warm * 0.3, 1.0 - warm) * (1.0 + input.uv1.y * _TintLightness);
                objectTint = lerp(float3(1.0, 1.0, 1.0), objectTint, _TintIntensity);
                float3 vertexColor = lerp(float3(1.0, 1.0, 1.0), input.color.rgb, _VertexColorIntensity);
                output.tint = objectTint * vertexColor;

                // Окна: UV0 стен переписан в MapBuildingTintModifier так, что на каждую стену приходится
                // целое число окон и этажей (1 ячейка = 1 единица UV) — окна не режутся углом и крышей.
                float2 normalXZ = input.normalOS.xz;
                float wallAmount = length(normalXZ);
                float2 alongWall = wallAmount > 1e-4 ? float2(-normalXZ.y, normalXZ.x) / wallAmount : float2(1.0, 0.0);
                output.window = float4(input.uv, wallAmount, dot(alongWall, float2(12.9898, 78.233)));
                return output;
            }

            // Matcap без искажений у краёв экрана: базис строится от направления взгляда на точку.
            float2 MatcapUV(float3 viewDirVS, float3 normalVS)
            {
                float3 c = cross(viewDirVS, normalVS);
                return float2(-c.y, c.x) * 0.5 + 0.5;
            }

            // Классический sphere-map по отражённому вектору.
            float2 ReflectUV(float3 viewDirVS, float3 normalVS)
            {
                float3 r = reflect(viewDirVS, normalVS);
                float m = 2.0 * sqrt(r.x * r.x + r.y * r.y + (r.z + 1.0) * (r.z + 1.0));
                return r.xy / max(m, 1e-4) + 0.5;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 toCameraWS = _WorldSpaceCameraPos - input.positionWS;
                float cameraDistance = length(toCameraWS);
                float3 viewDirWS = toCameraWS / max(cameraDistance, 1e-4);

                // View-space: камера смотрит в -Z, viewDirVS — от камеры к точке.
                float3 viewDirVS = normalize(TransformWorldToView(input.positionWS));
                float3 normalVS = normalize(TransformWorldToViewDir(normalWS, false));

                // --- Albedo
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uvHeight.xy);
                float3 albedo = baseSample.rgb * _BaseColor.rgb * input.tint;
                float alpha = baseSample.a * _BaseColor.a;

                float variation = (AG_Hash21(floor(input.positionWS.xz * 0.05)) - 0.5) * _ColorVariation;
                albedo = saturate(albedo + variation);

                // --- Vertical gradient (книзу темнее)
                float gradientT = saturate((input.uvHeight.z - _GradientOffset) / max(_GradientHeight, 1e-3));
                gradientT = pow(gradientT, _GradientPower);
                float3 gradientTint = lerp(_GradientBottomColor.rgb, _GradientTopColor.rgb, gradientT);
                albedo *= lerp(float3(1.0, 1.0, 1.0), gradientTint, _GradientIntensity);

                // --- Matcap lighting
                float3 matcap = SAMPLE_TEXTURE2D(_MatcapTex, sampler_MatcapTex, MatcapUV(viewDirVS, normalVS)).rgb;
                float3 lighting = lerp(float3(1.0, 1.0, 1.0), matcap * _MatcapBrightness, _MatcapIntensity);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                lighting *= lerp(float3(1.0, 1.0, 1.0), mainLight.color, _LightInfluence);

                // Тень от солнца — цветная (не чёрная), сила регулируется ползунком.
                float3 shadowTint = lerp(_ShadowColor.rgb, float3(1.0, 1.0, 1.0), mainLight.shadowAttenuation);
                lighting *= lerp(float3(1.0, 1.0, 1.0), shadowTint, _ShadowStrength);

                float3 color = albedo * lighting;

                // --- Windows (только вертикальные стены, вдали растворяются, чтобы не рябило)
                float2 windowUv = input.window.xy;
                float2 windowCell = frac(windowUv);
                float2 windowId = floor(windowUv);
                float windowFrame =
                    smoothstep(0.20, 0.26, windowCell.x) * (1.0 - smoothstep(0.74, 0.80, windowCell.x)) *
                    smoothstep(0.24, 0.30, windowCell.y) * (1.0 - smoothstep(0.72, 0.78, windowCell.y));
                float windowFade = 1.0 - saturate((cameraDistance - _WindowFadeStart) / max(_WindowFadeRange, 1e-3));
                float windowMask = windowFrame * smoothstep(0.6, 0.9, input.window.z) * windowFade * _WindowIntensity;
                float windowLit = step(1.0 - _WindowLitAmount, AG_Hash21(fmod(windowId, 61.0) + input.window.w));
                windowLit *= lerp(_WindowLitDay, 1.0, _AG_DayNightBlend);
                float3 windowColor = lerp(color * _WindowGlassColor.rgb, _WindowLitColor.rgb, windowLit);
                color = lerp(color, windowColor, windowMask);

                // --- Matcap reflection
                float3 reflection = SAMPLE_TEXTURE2D(_ReflectTex, sampler_ReflectTex, ReflectUV(viewDirVS, normalVS)).rgb;
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _ReflectFresnelPower);
                float reflectMask = lerp(1.0, fresnel, _ReflectFresnel) * _ReflectIntensity;
                color += reflection * _ReflectColor.rgb * reflectMask;

                color += _EmissionColor.rgb;

                // --- Linear fog
                float fog = saturate((cameraDistance - _AG_LinearFogParams.x) * _AG_LinearFogParams.z);
                fog *= _AG_LinearFogParams.w * _AG_LinearFogColor.a * _FogIntensity;
                color = lerp(color, _AG_LinearFogColor.rgb, fog);

                return half4(color, alpha);
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
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma target 2.0
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 ShadowVert(ShadowAttributes input) : SV_POSITION
            {
                float3 positionWS = AG_ApplyCurvature(TransformObjectToWorld(input.positionOS.xyz));
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                return ApplyShadowClamping(positionCS);
            }

            half4 ShadowFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma target 2.0

            float4 DepthVert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformWorldToHClip(AG_ApplyCurvature(TransformObjectToWorld(positionOS.xyz)));
            }

            half DepthFrag(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #pragma target 2.0
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct DepthNormalsAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct DepthNormalsVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            DepthNormalsVaryings DepthNormalsVert(DepthNormalsAttributes input)
            {
                DepthNormalsVaryings output;
                output.positionCS = TransformWorldToHClip(AG_ApplyCurvature(TransformObjectToWorld(input.positionOS.xyz)));
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFrag(DepthNormalsVaryings input) : SV_Target
            {
                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                half3 packedNormalWS = PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5));
                return half4(packedNormalWS, 0.0);
            #else
                return half4(normalWS, 0.0);
            #endif
            }
            ENDHLSL
        }
    }

    FallBack Off
}
