using DG.Tweening;
using UnityEngine;

public static class UIScrollListAnimations
{
    public static void PrepareForShow(
        RectTransform scrollViewRect,
        ref Vector2 originalPos,
        ref bool originInitialized,
        ref Tween scrollTween,
        Transform listContent = null)
    {
        scrollTween?.Kill();
        UIListEntranceHelper.Kill(listContent);

        if (scrollViewRect == null)
            return;

        if (!originInitialized)
        {
            originalPos = scrollViewRect.anchoredPosition;
            originInitialized = true;
        }

        scrollViewRect.anchoredPosition = originalPos;
    }

    public static Tween PlaySlideUpWithStagger(
        RectTransform scrollViewRect,
        Vector2 targetPos,
        float slideUpOffset,
        float duration,
        Transform listContent,
        Tween previousTween)
    {
        previousTween?.Kill();
        UIListEntranceHelper.Kill(listContent);

        if (scrollViewRect != null)
            scrollViewRect.anchoredPosition = targetPos;

        return null;
    }
}
