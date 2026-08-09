#ifndef ACTIVEGRAD_MAP_VISUAL_COMMON_INCLUDED
#define ACTIVEGRAD_MAP_VISUAL_COMMON_INCLUDED

float AG_Hash21(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float AG_Hash31(float3 p)
{
    return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
}

float AG_Dither4x4(float2 screenPos)
{
    static const float dither[16] = {
        0.0 / 16.0,  8.0 / 16.0,  2.0 / 16.0, 10.0 / 16.0,
        12.0 / 16.0, 4.0 / 16.0, 14.0 / 16.0,  6.0 / 16.0,
        3.0 / 16.0, 11.0 / 16.0,  1.0 / 16.0,  9.0 / 16.0,
        15.0 / 16.0, 7.0 / 16.0, 13.0 / 16.0,  5.0 / 16.0
    };

    int2 p = int2(fmod(floor(screenPos), 4.0));
    return dither[p.x + p.y * 4];
}

void AG_ApplyDistanceDither(float3 worldPos, float fadeStart, float fadeRange, float4 positionCS)
{
    float dist = distance(_WorldSpaceCameraPos, worldPos);
    float fade = saturate((dist - fadeStart) / max(fadeRange, 0.001));
    clip(AG_Dither4x4(positionCS.xy / positionCS.w) - fade);
}

float3 AG_ApplyFog(float3 color, float3 worldPos)
{
#ifdef UNITY_FOG
    float fogFactor = ComputeFogFactor(TransformWorldToHClip(worldPos).z);
    color = MixFog(color, fogFactor);
#endif
    return color;
}

#endif
