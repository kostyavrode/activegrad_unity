using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Подбирает, что в окне можно двигать/масштабировать, не показывая края фона.
/// Полноэкранные фоны (растянутые Graphic) только растворяются, анимируется контент внутри них.
/// </summary>
public static class UIAnimTargets
{
    public struct Target
    {
        public RectTransform Rect;
        public Vector2 Position;
        public Vector3 Scale;
        public bool CanMove; // false — позицию задаёт LayoutGroup родителя
    }

    private const float StretchTolerance = 0.01f;
    private const float MaxSizeDeltaForFullScreen = 120f;
    private const int MaxDepth = 2;

    public static bool IsFullScreen(RectTransform rect)
    {
        if (rect == null)
            return false;

        return rect.anchorMin.x <= StretchTolerance && rect.anchorMin.y <= StretchTolerance
            && rect.anchorMax.x >= 1f - StretchTolerance && rect.anchorMax.y >= 1f - StretchTolerance
            && Mathf.Abs(rect.sizeDelta.x) < MaxSizeDeltaForFullScreen
            && Mathf.Abs(rect.sizeDelta.y) < MaxSizeDeltaForFullScreen;
    }

    public static List<Target> Collect(RectTransform root)
    {
        var result = new List<Target>();
        if (root == null)
            return result;

        if (!IsFullScreen(root))
        {
            Add(root, result);
            return result;
        }

        CollectChildren(root, result, MaxDepth);
        return result;
    }

    /// <summary>Есть ли у окна своё затемнение (тёмный полупрозрачный полноэкранный фон).</summary>
    public static bool HasOwnDim(RectTransform root)
    {
        if (root == null)
            return false;

        if (IsDimGraphic(root.GetComponent<Graphic>()))
            return true;

        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i) as RectTransform;
            if (child != null && IsFullScreen(child) && IsDimGraphic(child.GetComponent<Graphic>()))
                return true;
        }

        return false;
    }

    private static bool IsDimGraphic(Graphic graphic)
    {
        if (graphic == null || !graphic.enabled)
            return false;

        var c = graphic.color;
        var luminance = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        return c.a >= 0.2f && c.a < 0.98f && luminance < 0.5f;
    }

    private static void CollectChildren(RectTransform parent, List<Target> result, int depth)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i) as RectTransform;
            if (child == null)
                continue;

            if (!IsFullScreen(child))
            {
                Add(child, result);
                continue;
            }

            // Полноэкранный фон остаётся на месте; пустой полноэкранный контейнер — смотрим внутрь
            if (depth > 0 && child.GetComponent<Graphic>() == null && child.GetComponent<ScrollRect>() == null)
                CollectChildren(child, result, depth - 1);
        }
    }

    private static void Add(RectTransform rect, List<Target> result)
    {
        var parent = rect.parent;
        var layoutDriven = parent != null && parent.GetComponent<LayoutGroup>() != null;

        result.Add(new Target
        {
            Rect = rect,
            Position = rect.anchoredPosition,
            Scale = rect.localScale,
            CanMove = !layoutDriven
        });
    }
}
