Shader "ActiveGrad/POIGlow"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1.0, 0.72, 0.25, 1)
        _GlowStrength ("Glow Strength", Range(0, 3)) = 1.4
        _PulseSpeed ("Pulse Speed", Float) = 2.5
        _FresnelPower ("Fresnel Power", Range(1, 6)) = 3.0
        _CoreSize ("Core Size", Range(0.1, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+50"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float _GlowStrength;
                float _PulseSpeed;
                float _FresnelPower;
                float _CoreSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = normalize(_WorldSpaceCameraPos - positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float fresnel = pow(1.0 - saturate(dot(normalize(input.normalWS), input.viewDirWS)), _FresnelPower);
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float core = smoothstep(_CoreSize, 0.0, fresnel);
                float3 emission = _GlowColor.rgb * (_GlowStrength * (0.65 + pulse * 0.35) * (fresnel + core));
                return half4(emission, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
