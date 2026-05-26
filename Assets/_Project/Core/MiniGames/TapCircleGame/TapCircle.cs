using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TapCircle : MonoBehaviour
{
    public event Action<TapCircle> OnTapped;
    public event Action<TapCircle> OnExpired;

    private Image _bodyImage;
    private Image _ringImage;
    private TextMeshProUGUI _label;
    private Button _button;
    private CanvasGroup _canvasGroup;

    private Tween _ringTween;
    private bool _isDone;

    public float RemainingFraction { get; private set; } = 1f;

    // ── static sprite cache ──────────────────────────────────────────────────
    private static Sprite _circleSprite;
    private static Sprite _ringSprite;

    public static void EnsureSprites()
    {
        if (_circleSprite == null) _circleSprite = BuildCircleSprite(128);
        if (_ringSprite  == null) _ringSprite  = BuildRingSprite(128, 13);
    }

    // ── factory ──────────────────────────────────────────────────────────────
    public static TapCircle Create(Transform parent, int number, float lifetime, Color color)
    {
        EnsureSprites();

        // root
        var go = new GameObject($"Circle_{number}");
        go.transform.SetParent(parent, false);
        var rootRect = go.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(110f, 110f);
        var cg = go.AddComponent<CanvasGroup>();

        // ring (behind body, starts big)
        var ringGo = new GameObject("Ring");
        ringGo.transform.SetParent(go.transform, false);
        var ringRect = ringGo.AddComponent<RectTransform>();
        ringRect.sizeDelta = new Vector2(110f, 110f);
        var ringImg = ringGo.AddComponent<Image>();
        ringImg.sprite = _ringSprite;
        ringImg.color = Color.white;
        ringImg.raycastTarget = false;

        // body
        var bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(go.transform, false);
        var bodyRect = bodyGo.AddComponent<RectTransform>();
        bodyRect.sizeDelta = new Vector2(110f, 110f);
        var bodyImg = bodyGo.AddComponent<Image>();
        bodyImg.sprite = _circleSprite;
        bodyImg.color = color;
        bodyImg.raycastTarget = false;

        // number label
        var lblGo = new GameObject("Label");
        lblGo.transform.SetParent(go.transform, false);
        var lblRect = lblGo.AddComponent<RectTransform>();
        lblRect.sizeDelta = new Vector2(90f, 90f);
        var tmp = lblGo.AddComponent<TextMeshProUGUI>();
        tmp.text = number.ToString();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 40;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        // transparent button layer (on top, fills the circle area)
        var btnGo = new GameObject("Tap");
        btnGo.transform.SetParent(go.transform, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(110f, 110f);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.sprite = _circleSprite;
        btnImg.color = Color.clear;
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var cols = btn.colors;
        cols.normalColor    = Color.clear;
        cols.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        cols.pressedColor   = new Color(1f, 1f, 1f, 0.20f);
        btn.colors = cols;

        var tc = go.AddComponent<TapCircle>();
        tc._bodyImage  = bodyImg;
        tc._ringImage  = ringImg;
        tc._label      = tmp;
        tc._button     = btn;
        tc._canvasGroup = cg;

        tc.Activate(number, lifetime, color);
        return tc;
    }

    // ── lifecycle ─────────────────────────────────────────────────────────────
    private void Activate(int number, float lifetime, Color color)
    {
        _label.text = number.ToString();
        _bodyImage.color = color;

        // spawn pop-in
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.20f).SetEase(Ease.OutBack).SetUpdate(true);

        // ring starts at 2.2x and shrinks to 1x
        _ringImage.transform.localScale = Vector3.one * 2.2f;
        _ringTween = _ringImage.transform
            .DOScale(Vector3.one, lifetime)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnUpdate(UpdateRingColor)
            .OnComplete(Expire);

        _button.onClick.AddListener(HandleTap);
    }

    private void UpdateRingColor()
    {
        if (_ringTween == null) return;
        float pct = _ringTween.ElapsedPercentage();
        RemainingFraction = 1f - pct;

        // white → yellow → red as ring closes
        Color c = pct < 0.5f
            ? Color.Lerp(Color.white, Color.yellow, pct * 2f)
            : Color.Lerp(Color.yellow, new Color(1f, 0.25f, 0.25f), (pct - 0.5f) * 2f);
        _ringImage.color = new Color(c.r, c.g, c.b, 0.9f);
    }

    private void HandleTap()
    {
        if (_isDone) return;
        _isDone = true;
        _ringTween?.Kill();
        _button.interactable = false;
        RemainingFraction = _ringTween != null ? RemainingFraction : 0f;

        OnTapped?.Invoke(this);
        PlayHit();
    }

    private void Expire()
    {
        if (_isDone) return;
        _isDone = true;
        RemainingFraction = 0f;
        _button.interactable = false;

        OnExpired?.Invoke(this);
        PlayMiss();
    }

    // ── animations ────────────────────────────────────────────────────────────
    private void PlayHit()
    {
        _ringImage.gameObject.SetActive(false);
        DOTween.Kill(transform);

        // burst scale → fade out
        transform.DOScale(1.45f, 0.08f).SetUpdate(true).OnComplete(() =>
        {
            _canvasGroup.DOFade(0f, 0.13f).SetUpdate(true)
                .OnComplete(() => { if (this != null) Destroy(gameObject); });
        });
    }

    private void PlayMiss()
    {
        DOTween.Kill(transform);
        _bodyImage.DOColor(new Color(1f, 0.18f, 0.18f), 0.08f).SetUpdate(true);
        _ringImage.DOColor(new Color(1f, 0.18f, 0.18f, 0.4f), 0.08f).SetUpdate(true);

        transform.DOShakePosition(0.22f, 8f, 25, 90, false, true).SetUpdate(true);
        transform.DOScale(0f, 0.28f).SetDelay(0.18f).SetUpdate(true)
            .OnComplete(() => { if (this != null) Destroy(gameObject); });
    }

    // ── texture helpers ───────────────────────────────────────────────────────
    private static Sprite BuildCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = size * 0.5f, r = cx;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - cx + 0.5f) * (x - cx + 0.5f) + (y - cx + 0.5f) * (y - cx + 0.5f));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((r - d) / 1.5f)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }

    private static Sprite BuildRingSprite(int size, int thickness)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float cx = size * 0.5f, outerR = cx, innerR = outerR - thickness;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - cx + 0.5f) * (x - cx + 0.5f) + (y - cx + 0.5f) * (y - cx + 0.5f));
                float a;
                if (d >= innerR && d <= outerR)       a = 1f;
                else if (d < innerR)                   a = Mathf.Clamp01((d - (innerR - 1.5f)) / 1.5f);
                else                                   a = Mathf.Clamp01((outerR + 1.5f - d) / 1.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }

    private void OnDestroy()
    {
        _ringTween?.Kill();
        _button?.onClick.RemoveAllListeners();
    }
}
