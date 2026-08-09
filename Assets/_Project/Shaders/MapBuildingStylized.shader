Shader "ActiveGrad/MapBuildingStylized"
{
    Properties
    {
        [Header(Colors)]
        _WallColorBottom ("Wall Bottom", Color) = (0.42, 0.40, 0.36, 1)
        _WallColorTop ("Wall Top", Color) = (0.70, 0.66, 0.60, 1)
        _RoofColor ("Roof", Color) = (0.80, 0.76, 0.70, 1)
        _WindowColor ("Window Lit", Color) = (1.0, 0.86, 0.55, 1)

        [Header(Windows)]
        _WindowDensity ("Window Density", Range(0, 1)) = 0.32
        _WindowScale ("Window Scale XY", Vector) = (7, 10, 0, 0)

        [Header(Detail)]
        _VariationStrength ("Color Variation", Range(0, 0.35)) = 0.10
        _AoStrength ("Fake AO", Range(0, 1)) = 0.45
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.12
        _RimColor ("Rim Color", Color) = (1, 0.92, 0.75, 1)

        [Header(Distance Fade)]
        _FadeStart ("Fade Start", Float) = 90
        _FadeRange ("Fade Range", Float) = 35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/_Project/Shaders/MapVisualCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _WallColorBottom;
                float4 _WallColorTop;
                float4 _RoofColor;
                float4 _WindowColor;
                float4 _RimColor;
                float4 _WindowScale;
                float _WindowDensity;
                float _VariationStrength;
                float _AoStrength;
                float _RimStrength;
                float _FadeStart;
                float _FadeRange;
            CBUFFER_END

            float _AG_DayNightBlend;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                AG_ApplyDistanceDither(input.positionWS, _FadeStart, _FadeRange, input.positionCS);

                float3 normalWS = normalize(input.normalWS);
                float roofMask = smoothstep(0.72, 0.95, normalWS.y);

                float heightT = saturate(input.positionWS.y * 0.08);
                float3 wallColor = lerp(_WallColorBottom.rgb, _WallColorTop.rgb, heightT);

                float ao = pow(1.0 - saturate(normalWS.y), 2.0) * _AoStrength;
                wallColor *= 1.0 - ao;

                float2 buildingId = floor(input.positionWS.xz * 0.05);
                float variation = (AG_Hash21(buildingId) - 0.5) * _VariationStrength;
                wallColor += variation;

                float3 color = lerp(wallColor, _RoofColor.rgb, roofMask);

                float2 windowUv = input.positionWS.xz * _WindowScale.xy;
                float2 windowCell = frac(windowUv);
                float2 windowId = floor(windowUv);
                float windowFrame = step(0.18, windowCell.x) * step(windowCell.x, 0.82) *
                                    step(0.22, windowCell.y) * step(windowCell.y, 0.78);
                float windowLit = step(1.0 - _WindowDensity, AG_Hash21(windowId + buildingId));
                float windowMask = windowFrame * windowLit * (1.0 - roofMask);
                color = lerp(color, _WindowColor.rgb, windowMask * _AG_DayNightBlend);

                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                float rim = pow(1.0 - saturate(dot(normalWS, viewDir)), 3.0) * _RimStrength;
                color += _RimColor.rgb * rim;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
