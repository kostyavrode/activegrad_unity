Shader "ActiveGrad/MapRoadTint"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.28, 0.30, 0.34, 1)
        _AccentColor ("Accent Color", Color) = (0.36, 0.38, 0.42, 1)
        _NoiseScale ("Noise Scale", Float) = 0.35
        _NoiseStrength ("Noise Strength", Range(0, 0.4)) = 0.12
        _LanduseGreen ("Landuse Green", Color) = (0.34, 0.52, 0.30, 1)
        _LanduseMode ("Landuse Mode", Float) = 0
        _FadeStart ("Fade Start", Float) = 90
        _FadeRange ("Fade Range", Float) = 35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry-5"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/_Project/Shaders/MapVisualCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _AccentColor;
                float4 _LanduseGreen;
                float _NoiseScale;
                float _NoiseStrength;
                float _LanduseMode;
                float _FadeStart;
                float _FadeRange;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                AG_ApplyDistanceDither(input.positionWS, _FadeStart, _FadeRange, input.positionCS);

                float noise = AG_Hash21(input.positionWS.xz * _NoiseScale);
                float3 roadColor = lerp(_BaseColor.rgb, _AccentColor.rgb, noise);
                roadColor *= 1.0 - noise * _NoiseStrength;

                float3 landuseColor = _LanduseGreen.rgb * (0.92 + noise * 0.16);
                float3 color = lerp(roadColor, landuseColor, saturate(_LanduseMode));

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
