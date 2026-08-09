using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class UIButtonGlowEffect
{
    private static readonly Dictionary<int, Tween> ActiveTweens = new Dictionary<int, Tween>();
    private static readonly Dictionary<int, Color> BaseColors = new Dictionary<int, Color>();

    public static void SetActive(Button button, bool active)
    {
        if (button == null)
            return;

        var graphic = button.targetGraphic;
        if (graphic == null)
            return;

        var key = graphic.GetInstanceID();
        Stop(button);

        if (!active)
            return;

        if (!BaseColors.ContainsKey(key))
            BaseColors[key] = graphic.color;

        var baseColor = BaseColors[key];
        var glowColor = new Color(
            Mathf.Min(baseColor.r + 0.12f, 1f),
            Mathf.Min(baseColor.g + 0.22f, 1f),
            Mathf.Min(baseColor.b + 0.12f, 1f),
            baseColor.a);

        graphic.color = baseColor;
        ActiveTweens[key] = graphic
            .DOColor(glowColor, 0.55f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    public static void Stop(Button button)
    {
        if (button == null)
            return;

        var graphic = button.targetGraphic;
        if (graphic == null)
            return;

        var key = graphic.GetInstanceID();
        if (ActiveTweens.TryGetValue(key, out var tween))
        {
            tween.Kill();
            ActiveTweens.Remove(key);
        }

        if (BaseColors.TryGetValue(key, out var baseColor))
            graphic.color = baseColor;
    }
}
