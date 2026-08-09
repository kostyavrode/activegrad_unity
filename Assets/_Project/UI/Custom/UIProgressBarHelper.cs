using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class UIProgressBarHelper
{
    private const float DefaultDuration = 0.5f;

    private static readonly Dictionary<int, Tween> ActiveTweens = new Dictionary<int, Tween>();

    public static void SetFillAmount(
        Image image,
        float target,
        float duration = DefaultDuration,
        bool instant = false,
        bool animateFromZero = false)
    {
        if (image == null)
            return;

        var key = image.GetInstanceID();
        if (ActiveTweens.TryGetValue(key, out var activeTween))
        {
            activeTween.Kill();
            ActiveTweens.Remove(key);
        }

        target = Mathf.Clamp01(target);

        if (instant || duration <= 0f)
        {
            image.fillAmount = target;
            return;
        }

        if (animateFromZero)
            image.fillAmount = 0f;

        if (!image.gameObject.activeInHierarchy)
        {
            image.fillAmount = target;
            return;
        }

        if (!animateFromZero && Mathf.Approximately(image.fillAmount, target))
        {
            image.fillAmount = target;
            return;
        }

        var tween = image
            .DOFillAmount(target, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnKill(() => ActiveTweens.Remove(key))
            .OnComplete(() => ActiveTweens.Remove(key));

        ActiveTweens[key] = tween;
    }

    public static void Kill(Image image)
    {
        if (image == null)
            return;

        var key = image.GetInstanceID();
        if (ActiveTweens.TryGetValue(key, out var activeTween))
        {
            activeTween.Kill();
            ActiveTweens.Remove(key);
        }
    }

    public static void ResetFill(Image image)
    {
        Kill(image);
        if (image != null)
            image.fillAmount = 0f;
    }
}
