using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class UIListEntranceHelper
{
    private const float DefaultItemDelay = 0.04f;
    private const float DefaultDuration = 0.2f;
    private const float StartOffsetY = -20f;
    private const float StartScale = 0.96f;

    private struct ChildState
    {
        public Transform Child;
        public Vector3 LocalPosition;
        public Vector3 LocalScale;
    }

    private static readonly Dictionary<int, Sequence> ActiveSequences = new Dictionary<int, Sequence>();
    private static readonly Dictionary<int, LayoutGroup> TrackedLayoutGroups = new Dictionary<int, LayoutGroup>();
    private static readonly Dictionary<int, List<ChildState>> OriginalChildStates = new Dictionary<int, List<ChildState>>();

    public static void PlayStaggeredEntrance(
        Transform content,
        float itemDelay = DefaultItemDelay,
        float duration = DefaultDuration)
    {
        if (content == null || content.childCount == 0)
            return;

        Kill(content);

        var contentId = content.GetInstanceID();

        var contentRect = content as RectTransform;
        if (contentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        var layoutGroup = content.GetComponent<LayoutGroup>();
        var hadLayoutEnabled = layoutGroup != null && layoutGroup.enabled;
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
            TrackedLayoutGroups[contentId] = layoutGroup;
        }

        var states = new List<ChildState>(content.childCount);
        OriginalChildStates[contentId] = states;

        var rootSequence = DOTween.Sequence().SetUpdate(true);
        ActiveSequences[contentId] = rootSequence;

        for (var i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i);
            if (child == null)
                continue;

            var originalPosition = child.localPosition;
            var originalScale = child.localScale;
            states.Add(new ChildState
            {
                Child = child,
                LocalPosition = originalPosition,
                LocalScale = originalScale
            });

            var canvasGroup = child.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = child.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            child.localPosition = originalPosition + new Vector3(0f, StartOffsetY, 0f);
            child.localScale = originalScale * StartScale;

            var delay = i * itemDelay;
            rootSequence.Insert(delay, canvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad));
            rootSequence.Insert(delay, child.DOLocalMove(originalPosition, duration).SetEase(Ease.OutCubic));
            rootSequence.Insert(delay, child.DOScale(originalScale, duration).SetEase(Ease.OutCubic));
        }

        rootSequence.OnKill(() => Finish(contentId, hadLayoutEnabled));
        rootSequence.OnComplete(() => Finish(contentId, hadLayoutEnabled));
    }

    public static void Kill(Transform content)
    {
        if (content == null)
            return;

        var key = content.GetInstanceID();

        if (ActiveSequences.TryGetValue(key, out var sequence))
        {
            ActiveSequences.Remove(key);
            sequence.Kill();
        }

        RestoreContentState(content);
        TrackedLayoutGroups.Remove(key);
    }

    private static void Finish(int contentId, bool restoreLayout)
    {
        ActiveSequences.Remove(contentId);

        RestoreChildStates(contentId);

        if (!TrackedLayoutGroups.TryGetValue(contentId, out var layoutGroup))
            return;

        if (restoreLayout && layoutGroup != null)
            layoutGroup.enabled = true;

        TrackedLayoutGroups.Remove(contentId);
    }

    private static void RestoreChildStates(int contentId)
    {
        if (!OriginalChildStates.TryGetValue(contentId, out var states))
            return;

        OriginalChildStates.Remove(contentId);

        for (var i = 0; i < states.Count; i++)
        {
            var state = states[i];
            if (state.Child == null)
                continue;

            state.Child.localPosition = state.LocalPosition;
            state.Child.localScale = state.LocalScale;

            var canvasGroup = state.Child.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
    }

    private static void RestoreContentState(Transform content)
    {
        RestoreChildStates(content.GetInstanceID());

        var layoutGroup = content.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
            layoutGroup.enabled = true;

        for (var i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i);
            if (child == null)
                continue;

            var canvasGroup = child.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
    }
}
