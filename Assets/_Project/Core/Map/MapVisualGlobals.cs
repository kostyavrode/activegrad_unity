using UnityEngine;

public static class MapVisualGlobals
{
    public static readonly int DayNightBlendId = Shader.PropertyToID("_AG_DayNightBlend");

    public static void Apply(MapVisualStyleConfig config)
    {
        if (config == null)
            return;

        Shader.SetGlobalFloat(DayNightBlendId, config.DayNightBlend);
    }

    public static void SetDayNightBlend(float value)
    {
        Shader.SetGlobalFloat(DayNightBlendId, Mathf.Clamp01(value));
    }
}
