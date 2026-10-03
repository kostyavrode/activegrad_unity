// Слой надписей карты (второй растровый стиль Mapbox «только надписи», прозрачный фон).
// Рисуется ПОВЕРХ 3D (ZTest Always), но:
//  - изгибается вместе с землёй (Curved World, те же глобалы, что у StylizedMatcap);
//  - уходит в линейный туман (прозрачностью, чтобы не было «цветных плашек» вдали);
//  - не рисуется поверх персонажа: CharacterRimLit пишет stencil-бит 64, здесь он исключается.
Shader "ActiveGrad/MapLabelsOverlay"
{
    Properties
    {
        [MainTexture] _BaseMap ("Labels (RGBA)", 2D) = "black" {}
        _Opacity ("Opacity", Range(0, 1)) = 1
        _HeightOffset ("Height Offset", Float) = 0.02
        [IntRange] _ExcludeStencilBit ("Exclude Stencil Bit (character, not 0)", Range(1, 128)) = 64
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+30"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Back

            Stencil
            {
                Ref [_ExcludeStencilBit]
                ReadMask [_ExcludeStencilBit]
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Opacity;
                float _HeightOffset;
                float _ExcludeStencilBit;
            CBUFFER_END

            float4 _AG_CurveCenter;
            float4 _AG_CurveParams;
            float4 _AG_LinearFogColor;
            float4 _AG_LinearFogParams;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fade : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS.y += _HeightOffset;

                // Curved World — та же формула, что в StylizedMatcap.
                float2 delta = positionWS.xz - _AG_CurveCenter.xz;
                float t = max(0.0, length(delta) - _AG_CurveParams.x);
                positionWS.y -= t * t * _AG_CurveParams.y * _AG_CurveCenter.w;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                // Линейный туман: надписи растворяются вдали.
                float distance = length(_WorldSpaceCameraPos - positionWS);
                float fog = saturate((distance - _AG_LinearFogParams.x) * _AG_LinearFogParams.z) * _AG_LinearFogParams.w;
                output.fade = 1.0 - fog;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                color.a *= _Opacity * input.fade;
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
