Shader "ActiveGrad/MapWaterSimple"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.08, 0.22, 0.34, 1)
        _ShallowColor ("Shallow Color", Color) = (0.18, 0.48, 0.58, 1)
        _WaveSpeed ("Wave Speed", Float) = 0.35
        _WaveScale ("Wave Scale", Float) = 0.06
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.35
        _FadeStart ("Fade Start", Float) = 90
        _FadeRange ("Fade Range", Float) = 35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry-10"
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
                float4 _DeepColor;
                float4 _ShallowColor;
                float _WaveSpeed;
                float _WaveScale;
                float _FresnelStrength;
                float _FadeStart;
                float _FadeRange;
            CBUFFER_END

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

                float2 waveUv = input.positionWS.xz * _WaveScale + _Time.y * _WaveSpeed;
                float wave = sin(waveUv.x * 3.0) * sin(waveUv.y * 2.0) * 0.5 + 0.5;
                float3 color = lerp(_DeepColor.rgb, _ShallowColor.rgb, wave);

                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                float fresnel = pow(1.0 - saturate(dot(normalize(input.normalWS), viewDir)), 3.0);
                color += fresnel * _FresnelStrength * _ShallowColor.rgb;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
