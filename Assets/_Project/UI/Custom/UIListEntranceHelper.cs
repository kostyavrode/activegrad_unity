using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public static class UIListEntranceHelper
{
    private const float DefaultItemDelay = 0.04f;
    private const float DefaultDuration = 0.2f;

    private static readonly Dictionary<int, Sequence> ActiveSequences = new Dictionary<int, Sequence>();
    private static readonly Dictionary<int, LayoutGroup> TrackedLayoutGroups = new Dictionary<int, LayoutGroup>();

    public static void PlayStaggeredEntrance(
        Transform content,
        float itemDelay = DefaultItemDelay,
        float duration = DefaultDuration)
    {
        if (content == null || content.childCount == 0)
            return;

        Kill(content);

        var layoutGroup = content.GetComponent<LayoutGroup>();
        var hadLayoutEnabled = layoutGroup != null && layoutGroup.enabled;
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
            TrackedLayoutGroups[content.GetInstanceID()] = layoutGroup;
        }

        var rootSequence = DOTween.Sequence().SetUpdate(true);
        ActiveSequences[content.GetInstanceID()] = rootSequence;

        for (var i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i);
            if (child == null)
                continue;

            var canvasGroup = child.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = child.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;

            rootSequence.Insert(i * itemDelay, canvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad));
        }

        rootSequence.OnKill(() => Finish(content.GetInstanceID(), hadLayoutEnabled));
        rootSequence.OnComplete(() => Finish(content.GetInstanceID(), hadLayoutEnabled));
    }

    public static void Kill(Transform content)
    {
        if (content == null)
            return;

        var key = content.GetInstanceID();

        if (ActiveSequences.TryGetValue(key, out var sequence))
        {
            sequence.Kill();
            ActiveSequences.Remove(key);
        }

        RestoreContentState(content);
        TrackedLayoutGroups.Remove(key);
    }

    private static void Finish(int contentId, bool restoreLayout)
    {
        ActiveSequences.Remove(contentId);

        if (!TrackedLayoutGroups.TryGetValue(contentId, out var layoutGroup))
            return;

        if (restoreLayout && layoutGroup != null)
            layoutGroup.enabled = true;

        TrackedLayoutGroups.Remove(contentId);
    }

    private static void RestoreContentState(Transform content)
    {
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

            child.localScale = Vector3.one;
        }
    }
}
