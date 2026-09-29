using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// "Сочность" мини-игр: процедурные UI-частицы, тряска, вспышки, фейды экранов
/// и безопасный доступ к IFeedbackService (контроллеры мини-игр не инжектятся Zenject'ом).
/// Все твины — в unscaled time.
/// </summary>
public static class MiniGameJuice
{
    // ══════════════════════════════════════════════════════════════════════════
    // FEEDBACK SERVICE
    // ══════════════════════════════════════════════════════════════════════════

    private static IFeedbackService _feedback;
    private static float _nextResolveTime = -1f;

    /// <summary>Кэшированный сервис фидбека или null, если он не забинжен.</summary>
    public static IFeedbackService FeedbackService
    {
        get
        {
            // Сервис-MonoBehaviour мог быть уничтожен
            if (_feedback is UnityEngine.Object uo && uo == null) _feedback = null;
            if (_feedback != null) return _feedback;

            // Не дёргаем контейнер каждый кадр, если сервиса нет
            if (Time.unscaledTime < _nextResolveTime) return null;
            _nextResolveTime = Time.unscaledTime + 2f;

            try
            {
                if (ProjectContext.HasInstance)
                    _feedback = ProjectContext.Instance.Container.TryResolve<IFeedbackService>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MiniGameJuice] IFeedbackService resolve failed: {e.Message}");
                _feedback = null;
            }
            return _feedback;
        }
    }

    public static void Feedback(FeedbackType type, Vector3? pos = null)
    {
        var svc = FeedbackService;
        if (svc == null) return;
        try { svc.Play(type, pos); }
        catch (Exception e) { Debug.LogWarning($"[MiniGameJuice] Feedback {type} failed: {e.Message}"); }
    }

    public static void Haptic(HapticType type)
    {
        var svc = FeedbackService;
        if (svc == null) return;
        try { svc.Haptic(type); }
        catch (Exception e) { Debug.LogWarning($"[MiniGameJuice] Haptic {type} failed: {e.Message}"); }
    }

    public static void Sfx(AudioClip clip, float pitchJitter = 0.05f, float pitch = 1f)
    {
        if (clip == null) return;
        var svc = FeedbackService;
        if (svc == null) return;
        try { svc.PlaySfx(clip, pitchJitter, pitch); }
        catch (Exception e) { Debug.LogWarning($"[MiniGameJuice] Sfx failed: {e.Message}"); }
    }

    /// <summary>Повышение тона звука по комбо: 1.0 … 1.48.</summary>
    public static float ComboPitch(int combo) => 1f + Mathf.Min(Mathf.Max(combo, 0), 12) * 0.04f;

    // ══════════════════════════════════════════════════════════════════════════
    // PARTICLES
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Взрыв из мягких кружков, разлетающихся из anchoredPos (координаты относительно центра parent).
    /// </summary>
    public static void Burst(RectTransform parent, Vector2 anchoredPos, Color color, int count = 12,
        float distance = 110f, float particleSize = 22f, float duration = 0.55f)
    {
        if (parent == null || count <= 0) return;

        // ударная волна
        var ring = MakeParticle(parent, anchoredPos, particleSize * 3f, new Color(color.r, color.g, color.b, 0.45f));
        ring.rectTransform.localScale = Vector3.one * 0.4f;
        DOTween.Sequence()
            .Join(ring.rectTransform.DOScale(2.4f, duration * 0.7f).SetEase(Ease.OutCubic))
            .Join(ring.DOFade(0f, duration * 0.7f).SetEase(Ease.OutQuad))
            .SetUpdate(true)
            .SetLink(ring.gameObject)
            .OnComplete(() => { if (ring != null) UnityEngine.Object.Destroy(ring.gameObject); });

        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float ang  = (i * step + UnityEngine.Random.Range(-step * 0.4f, step * 0.4f)) * Mathf.Deg2Rad;
            float dist = distance * UnityEngine.Random.Range(0.55f, 1.05f);
            float size = particleSize * UnityEngine.Random.Range(0.6f, 1.25f);
            float dur  = duration * UnityEngine.Random.Range(0.8f, 1.15f);

            // лёгкий разброс оттенка к белому
            Color c = Color.Lerp(color, Color.white, UnityEngine.Random.Range(0f, 0.35f));
            var p = MakeParticle(parent, anchoredPos, size, c);
            var rt = p.rectTransform;
            Vector2 target = anchoredPos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

            DOTween.Sequence()
                .Insert(0f, rt.DOAnchorPos(target, dur).SetEase(Ease.OutCubic))
                .Insert(0f, rt.DOScale(0.15f, dur).SetEase(Ease.InQuad))
                .Insert(dur * 0.4f, p.DOFade(0f, dur * 0.6f))
                .SetUpdate(true)
                .SetLink(p.gameObject)
                .OnComplete(() => { if (p != null) UnityEngine.Object.Destroy(p.gameObject); });
        }
    }

    private static Image MakeParticle(RectTransform parent, Vector2 pos, float size, Color color)
    {
        var go = new GameObject("FxParticle");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.sprite = MiniGameTheme.SoftCircleSprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SHAKE / FLASH / PUNCH
    // ══════════════════════════════════════════════════════════════════════════

    private static readonly Dictionary<RectTransform, Tween> _shakes = new Dictionary<RectTransform, Tween>();

    /// <summary>Тряска anchoredPosition с гарантированным возвратом в исходную точку.</summary>
    public static void Shake(RectTransform target, float strength = 12f, float duration = 0.25f)
    {
        if (target == null) return;

        // Уже трясётся — останавливаем (OnKill вернёт позицию), затем начинаем заново
        if (_shakes.TryGetValue(target, out var running) && running != null && running.IsActive())
            running.Kill();
        _shakes.Remove(target);

        Vector2 origin = target.anchoredPosition;
        Tween t = target.DOShakeAnchorPos(duration, strength, 22, 90f, false, true)
            .SetUpdate(true)
            .SetLink(target.gameObject);
        t.OnKill(() =>
        {
            if (target != null) target.anchoredPosition = origin;
            if (_shakes.TryGetValue(target, out var cur) && cur == t) _shakes.Remove(target);
        });
        _shakes[target] = t;
    }

    /// <summary>Полноэкранная вспышка поверх parent.</summary>
    public static void Flash(RectTransform parent, Color color, float alpha = 0.25f, float duration = 0.35f)
    {
        if (parent == null) return;
        var img = MiniGameTheme.MakeImage(parent, "FxFlash", new Color(color.r, color.g, color.b, alpha));
        MiniGameTheme.Stretch(img.rectTransform);
        img.raycastTarget = false;
        img.transform.SetAsLastSibling();
        img.DOFade(0f, duration).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(img.gameObject)
            .OnComplete(() => { if (img != null) UnityEngine.Object.Destroy(img.gameObject); });
    }

    /// <summary>Punch-scale, не накапливающий искажения при частых вызовах.</summary>
    public static void Punch(Transform t, float amount = 0.2f, float duration = 0.25f)
    {
        if (t == null) return;
        t.DOKill(true);
        t.localScale = Vector3.one;
        t.DOPunchScale(Vector3.one * amount, duration, 6, 0.6f).SetUpdate(true).SetLink(t.gameObject);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SCREEN FADES
    // ══════════════════════════════════════════════════════════════════════════

    private static CanvasGroup GetGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    /// <summary>Активирует объект и плавно проявляет его CanvasGroup.</summary>
    public static void FadeIn(GameObject go, float duration = 0.25f)
    {
        if (go == null) return;
        var cg = GetGroup(go);
        cg.DOKill();
        go.SetActive(true);
        cg.alpha = 0f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
        cg.DOFade(1f, duration).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(go);
    }

    /// <summary>Плавно скрывает CanvasGroup и деактивирует объект.</summary>
    public static void FadeOut(GameObject go, float duration = 0.2f)
    {
        if (go == null || !go.activeSelf) return;
        var cg = GetGroup(go);
        cg.DOKill();
        cg.blocksRaycasts = false;
        cg.DOFade(0f, duration).SetEase(Ease.InQuad).SetUpdate(true).SetLink(go)
            .OnComplete(() =>
            {
                if (go == null) return;
                go.SetActive(false);
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
            });
    }

    /// <summary>Мгновенно скрыть (без анимации), сбросив возможный фейд.</summary>
    public static void HideImmediate(GameObject go)
    {
        if (go == null) return;
        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null) { cg.DOKill(); cg.alpha = 1f; cg.blocksRaycasts = true; }
        go.SetActive(false);
    }
}
