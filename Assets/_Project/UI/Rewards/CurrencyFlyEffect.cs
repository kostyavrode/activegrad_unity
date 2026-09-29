using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

/// <summary>
/// Полёт иконок валюты/ресурсов по кривой Безье в HUD-цель с "тиком" и punch при прилёте.
/// </summary>
public static class CurrencyFlyEffect
{
    public const int MaxIcons = 8;

    private static readonly Dictionary<RectTransform, Tween> Punches = new Dictionary<RectTransform, Tween>();
    private static float _lastTickTime;

    private static readonly string[] CoinKeywords = { "coin", "money" };

    /// <summary>Самостоятельный полёт из экранной точки (создаёт временный слой поверх канваса).</summary>
    public static void FlyFromScreen(RewardEntry entry, Vector2 fromScreenPos, RectTransform target, IFeedbackService feedback)
    {
        var canvasT = RewardUiFactory.FindCanvasTransform();
        if (canvasT == null) return;

        var layer = RewardUiFactory.CreateRoot(canvasT, "FlyLayer", false, out _);
        var cam = RewardUiFactory.GetCanvasCamera(layer);
        var from = RewardUiFactory.ScreenToLocal(layer, fromScreenPos, cam);

        float duration = Fly(layer, entry, from, target, feedback);
        var go = layer.gameObject;
        DOVirtual.DelayedCall(duration + 0.15f, () => { if (go != null) Object.Destroy(go); }, true);
    }

    /// <summary>
    /// Запускает полёт внутри layer (координаты layer, pivot по центру). Возвращает общую длительность.
    /// layer должен жить не меньше возвращённого времени.
    /// </summary>
    public static float Fly(RectTransform layer, RewardEntry entry, Vector2 fromLocal, RectTransform target,
        IFeedbackService feedback, float startDelay = 0f)
    {
        if (layer == null) return 0f;
        if (target == null) target = FindTarget(entry);

        int count = Mathf.Clamp(entry.Amount, 1, MaxIcons);
        var layerCam = RewardUiFactory.GetCanvasCamera(layer);
        var layerRect = layer.rect;
        Vector2 fallback = new Vector2(layerRect.width * 0.5f - 90f, layerRect.height * 0.5f - 120f);

        Func<Vector2> targetPos = () =>
        {
            if (target != null && target.gameObject.activeInHierarchy)
            {
                var tCam = RewardUiFactory.GetCanvasCamera(target);
                var screen = RectTransformUtility.WorldToScreenPoint(tCam, target.TransformPoint(target.rect.center));
                return RewardUiFactory.ScreenToLocal(layer, screen, layerCam);
            }
            return fallback;
        };

        Vector2 initialTarget = targetPos();
        float total = 0f;

        for (int i = 0; i < count; i++)
        {
            var rt = RewardUiFactory.MakeFlyIcon(layer, entry, fromLocal, 64f);
            var go = rt.gameObject;
            rt.localScale = Vector3.zero;

            float delay = startDelay + i * 0.05f;
            Vector2 spread = fromLocal + Random.insideUnitCircle * 90f;

            rt.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetDelay(delay).U(go);
            rt.DOAnchorPos(spread, 0.25f).SetEase(Ease.OutQuad).SetDelay(delay).U(go);

            // Контрольная точка: середина + случайное смещение перпендикулярно направлению
            Vector2 dir = initialTarget - spread;
            Vector2 perp = dir.sqrMagnitude > 1f ? new Vector2(-dir.y, dir.x).normalized : Vector2.right;
            Vector2 ctrlOffset = perp * Random.Range(-260f, 260f) + Vector2.down * Random.Range(40f, 200f);

            float flyDelay = delay + 0.3f + i * 0.04f;
            float flyDur = Random.Range(0.5f, 0.68f);
            float t = 0f;

            DOTween.To(() => t, x =>
                {
                    t = x;
                    Vector2 p2 = targetPos();
                    Vector2 p1 = Vector2.Lerp(spread, p2, 0.5f) + ctrlOffset;
                    float u = 1f - x;
                    rt.anchoredPosition = u * u * spread + 2f * u * x * p1 + x * x * p2;
                    float s = Mathf.Lerp(1f, 0.55f, x);
                    rt.localScale = new Vector3(s, s, 1f);
                }, 1f, flyDur)
                .SetEase(Ease.InQuad)
                .SetDelay(flyDelay)
                .OnComplete(() =>
                {
                    Tick(feedback);
                    PunchTarget(target);
                    if (go != null) Object.Destroy(go);
                })
                .U(go);

            total = Mathf.Max(total, flyDelay + flyDur);
        }

        return total;
    }

    private static void Tick(IFeedbackService feedback)
    {
        if (feedback == null) return;
        if (Time.unscaledTime - _lastTickTime < 0.045f) return;
        _lastTickTime = Time.unscaledTime;
        feedback.Play(FeedbackType.CoinTick);
    }

    public static void PunchTarget(RectTransform target)
    {
        if (target == null) return;

        if (Punches.Count > 32)
        {
            var dead = new List<RectTransform>();
            foreach (var kv in Punches)
                if (kv.Key == null || kv.Value == null || !kv.Value.IsActive()) dead.Add(kv.Key);
            foreach (var k in dead) Punches.Remove(k);
        }

        if (Punches.TryGetValue(target, out var prev) && prev != null && prev.IsActive())
            prev.Complete(); // вернёт исходный масштаб

        Punches[target] = target.DOPunchScale(Vector3.one * 0.22f, 0.25f, 8, 0.6f).U(target.gameObject);
    }

    /// <summary>Ищет HUD-элемент для валюты: сначала по Id, затем по "coin"/"money".</summary>
    public static RectTransform FindTarget(RewardEntry entry)
    {
        var byId = FindByKeywords(KeywordsFor(entry.Id));
        if (byId != null) return byId;
        return FindByKeywords(CoinKeywords);
    }

    private static string[] KeywordsFor(string id)
    {
        switch ((id ?? "").ToLowerInvariant())
        {
            case "coins":      return CoinKeywords;
            case "xp":         return new[] { "exp" };
            case "metal":      return new[] { "metal" };
            case "wood":       return new[] { "wood" };
            case "blueprints": return new[] { "blueprint" };
            default:           return Array.Empty<string>();
        }
    }

    private static RectTransform FindByKeywords(string[] keywords)
    {
        if (keywords == null || keywords.Length == 0) return null;

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        RectTransform best = null;
        foreach (var canvas in canvases)
        {
            if (canvas == null || !canvas.isActiveAndEnabled || !canvas.isRootCanvas) continue;

            foreach (var rt in canvas.GetComponentsInChildren<RectTransform>(false))
            {
                string n = rt.name.ToLowerInvariant();
                bool match = false;
                foreach (var k in keywords)
                    if (n.Contains(k)) { match = true; break; }
                if (!match) continue;
                if (rt.rect.width <= 1f || RewardUiFactory.IsRewardObject(rt)) continue;

                if (n.Contains("icon")) return rt; // иконка — лучший вариант
                if (best == null) best = rt;
            }
        }
        return best;
    }
}
