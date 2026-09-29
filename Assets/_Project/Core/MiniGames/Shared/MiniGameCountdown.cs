using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Отсчёт «3-2-1-GO!» поверх игрового экрана. Unscaled time.
/// Если объект уничтожен раньше (игру закрыли) — колбэк не вызывается.
/// </summary>
public class MiniGameCountdown : MonoBehaviour
{
    private const float StepDuration = 0.6f;

    private TextMeshProUGUI _text;
    private Image           _glow;
    private Action          _onComplete;
    private Action<int>     _onStep;

    /// <param name="parent">Куда положить оверлей (растягивается на весь parent).</param>
    /// <param name="onComplete">Вызывается после «GO!».</param>
    /// <param name="onStep">Вызывается на каждом шаге: 3, 2, 1, 0 (=GO).</param>
    public static MiniGameCountdown Play(RectTransform parent, Action onComplete, Action<int> onStep = null)
    {
        if (parent == null) { onComplete?.Invoke(); return null; }

        var go = new GameObject("MiniGameCountdown");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        MiniGameTheme.Stretch(rt);
        go.transform.SetAsLastSibling();

        var cg = go.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        var cd = go.AddComponent<MiniGameCountdown>();
        cd._onComplete = onComplete;
        cd._onStep = onStep;
        cd.Build(rt);
        cd.StartCoroutine(cd.Run());
        return cd;
    }

    /// <summary>Отменить без вызова колбэка.</summary>
    public void Cancel()
    {
        _onComplete = null;
        _onStep = null;
        if (this != null) Destroy(gameObject);
    }

    private void Build(RectTransform root)
    {
        _glow = MiniGameTheme.MakeImage(root, "Glow", new Color(0f, 0f, 0f, 0.55f));
        _glow.sprite = MiniGameTheme.SoftCircleSprite;
        _glow.raycastTarget = false;
        var grt = _glow.rectTransform;
        grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
        grt.sizeDelta = new Vector2(320f, 320f);

        var tgo = new GameObject("Number");
        tgo.transform.SetParent(root, false);
        var trt = tgo.AddComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(400f, 200f);
        _text = tgo.AddComponent<TextMeshProUGUI>();
        _text.alignment = TextAlignmentOptions.Center;
        _text.fontStyle = FontStyles.Bold;
        _text.fontSize = 130;
        _text.raycastTarget = false;
        _text.text = "";
    }

    private IEnumerator Run()
    {
        string[] labels = { "3", "2", "1", "GO!" };
        for (int i = 0; i < labels.Length; i++)
        {
            bool isGo = i == labels.Length - 1;
            ShowStep(labels[i], isGo);
            MiniGameJuice.Feedback(isGo ? FeedbackType.Success : FeedbackType.Tap);
            try { _onStep?.Invoke(isGo ? 0 : 3 - i); }
            catch (Exception e) { Debug.LogException(e); }
            yield return new WaitForSecondsRealtime(StepDuration);
        }

        var cb = _onComplete;
        _onComplete = null;
        Destroy(gameObject);
        cb?.Invoke();
    }

    private void ShowStep(string label, bool isGo)
    {
        var t = _text.rectTransform;
        _text.DOKill();
        t.DOKill();
        _glow.rectTransform.DOKill();

        _text.text = label;
        _text.fontSize = isGo ? 110 : 130;
        _text.color = isGo ? MiniGameTheme.Success : Color.white;

        t.localScale = Vector3.one * 2.2f;
        _text.alpha = 0f;

        float inDur = StepDuration * 0.4f;
        float hold  = StepDuration * 0.3f;
        float outDur = StepDuration * 0.3f;

        _text.DOFade(1f, inDur * 0.5f).SetUpdate(true).SetLink(gameObject);
        t.DOScale(1f, inDur).SetEase(Ease.OutBack, 2.2f).SetUpdate(true).SetLink(gameObject);
        _text.DOFade(0f, outDur).SetDelay(inDur + hold).SetUpdate(true).SetLink(gameObject);
        t.DOScale(isGo ? 1.6f : 0.7f, outDur).SetDelay(inDur + hold).SetEase(Ease.InQuad)
            .SetUpdate(true).SetLink(gameObject);

        var g = _glow.rectTransform;
        g.localScale = Vector3.one * 0.6f;
        g.DOScale(1.1f, StepDuration).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        _glow.color = isGo ? new Color(MiniGameTheme.Success.r, MiniGameTheme.Success.g, MiniGameTheme.Success.b, 0.35f)
                           : new Color(0f, 0f, 0f, 0.55f);
    }

    private void OnDestroy()
    {
        if (_text != null) { _text.DOKill(); _text.rectTransform.DOKill(); }
        if (_glow != null) _glow.rectTransform.DOKill();
    }
}
