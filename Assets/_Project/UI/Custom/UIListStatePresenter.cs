using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIListStatePresenter : MonoBehaviour
{
    private const float SkeletonHeight = 96f;
    private const float SkeletonSpacing = 12f;

    private static readonly Dictionary<int, UIListStatePresenter> Presenters = new Dictionary<int, UIListStatePresenter>();

    private Transform _listContent;
    private GameObject _loadingRoot;
    private GameObject _emptyRoot;
    private readonly List<Tween> _activeTweens = new List<Tween>();

    public static UIListStatePresenter GetOrCreate(Transform listContent)
    {
        if (listContent == null)
            return null;

        var key = listContent.GetInstanceID();
        if (Presenters.TryGetValue(key, out var existing) && existing != null)
            return existing;

        var overlayObject = new GameObject("UIListStateOverlay", typeof(RectTransform));
        var presenter = overlayObject.AddComponent<UIListStatePresenter>();
        presenter.Initialize(listContent);
        Presenters[key] = presenter;
        return presenter;
    }

    public static void HideFor(Transform listContent)
    {
        if (listContent == null)
            return;

        var key = listContent.GetInstanceID();
        if (!Presenters.TryGetValue(key, out var presenter) || presenter == null)
            return;

        presenter.Hide();
    }

    private void Initialize(Transform listContent)
    {
        _listContent = listContent;

        var overlayRect = transform as RectTransform;
        var anchorParent = listContent.parent;

        var scrollRect = listContent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null && scrollRect.viewport != null)
            anchorParent = scrollRect.viewport;

        transform.SetParent(anchorParent, false);
        transform.SetAsLastSibling();

        if (overlayRect != null)
        {
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
        }
    }

    private void OnDestroy()
    {
        if (_listContent == null)
            return;

        Presenters.Remove(_listContent.GetInstanceID());
        KillTweens();
    }

    public void ShowLoading(int skeletonCount = 3)
    {
        Hide();

        _loadingRoot = new GameObject("LoadingState", typeof(RectTransform));
        _loadingRoot.transform.SetParent(transform, false);

        var rootRect = _loadingRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(700f, skeletonCount * SkeletonHeight + (skeletonCount - 1) * SkeletonSpacing);

        var totalHeight = skeletonCount * SkeletonHeight + (skeletonCount - 1) * SkeletonSpacing;
        var startY = totalHeight * 0.5f - SkeletonHeight * 0.5f;

        for (var i = 0; i < skeletonCount; i++)
        {
            var skeleton = CreateSkeletonBlock(_loadingRoot.transform, i, startY - i * (SkeletonHeight + SkeletonSpacing));
            AnimateSkeleton(skeleton);
        }
    }

    public void ShowEmpty(string message)
    {
        Hide();

        _emptyRoot = new GameObject("EmptyState", typeof(RectTransform));
        _emptyRoot.transform.SetParent(transform, false);

        var rootRect = _emptyRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var textObject = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(_emptyRoot.transform, false);

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(32f, 32f);
        textRect.offsetMax = new Vector2(-32f, -32f);

        var label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = message;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 28;
        label.color = new Color(1f, 1f, 1f, 0.72f);
        label.enableWordWrapping = true;

        var referenceFont = _listContent.GetComponentInParent<TextMeshProUGUI>(true);
        if (referenceFont != null)
            label.font = referenceFont.font;
    }

    public void Hide()
    {
        KillTweens();

        if (_loadingRoot != null)
        {
            Destroy(_loadingRoot);
            _loadingRoot = null;
        }

        if (_emptyRoot != null)
        {
            Destroy(_emptyRoot);
            _emptyRoot = null;
        }
    }

    private GameObject CreateSkeletonBlock(Transform parent, int index, float yPos)
    {
        var block = new GameObject($"Skeleton_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        block.transform.SetParent(parent, false);

        var rect = block.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(680f, SkeletonHeight);
        rect.anchoredPosition = new Vector2(0f, yPos);

        var image = block.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.12f);

        return block;
    }

    private void AnimateSkeleton(GameObject skeleton)
    {
        var canvasGroup = skeleton.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0.35f;

        var tween = canvasGroup
            .DOFade(0.75f, 0.7f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);

        _activeTweens.Add(tween);
    }

    private void KillTweens()
    {
        for (var i = 0; i < _activeTweens.Count; i++)
            _activeTweens[i]?.Kill();

        _activeTweens.Clear();
    }
}
