Shader "Mapbox/MapboxStylesURP"
{
    Properties
    {
        [PerRendererData]_BaseColor ("BaseColor", Color) = (1,1,1,1)
        [PerRendererData]_DetailColor1 ("DetailColor1", Color) = (1,1,1,1)
        [PerRendererData]_DetailColor2 ("DetailColor2", Color) = (1,1,1,1)

        _BaseTex ("Base", 2D) = "white" {}
        _DetailTex1 ("Detail 1", 2D) = "white" {}
        _DetailTex2 ("Detail 2", 2D) = "white" {}

        _Emission ("Emission", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ---- CORRECT URP texture declarations ----

            TEXTURE2D(_BaseTex);
            SAMPLER(sampler_BaseTex);

            TEXTURE2D(_DetailTex1);
            SAMPLER(sampler_DetailTex1);

            TEXTURE2D(_DetailTex2);
            SAMPLER(sampler_DetailTex2);

            float4 _BaseColor;
            float4 _DetailColor1;
            float4 _DetailColor2;
            float _Emission;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, IN.uv);
                float4 detail1 = SAMPLE_TEXTURE2D(_DetailTex1, sampler_DetailTex1, IN.uv);
                float4 detail2 = SAMPLE_TEXTURE2D(_DetailTex2, sampler_DetailTex2, IN.uv);

                float4 blend1 = lerp(_BaseColor, _DetailColor1, detail1.a);
                float4 blend2 = lerp(blend1, _DetailColor2, detail2.a);

                float4 finalColor = baseTex * blend2;

                // Simple Lambert
                float3 normalWS = float3(0,1,0);
                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                float3 litColor = finalColor.rgb * (mainLight.color * ndotl + _Emission);

                return float4(litColor, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}
