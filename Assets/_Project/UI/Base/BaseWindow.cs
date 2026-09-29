using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public abstract class BaseWindow : MonoBehaviour, IWindow
{
    private const float ShowDuration = 0.28f;
    private const float HideDuration = 0.2f;
    private const float BackgroundDuration = 0.25f;
    private const float BackgroundAlpha = 0.65f;

    private const float ShowOffsetY = -40f;
    private const float ShowStartScale = 0.98f;

    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("Что двигать при открытии. Пусто — контент подбирается автоматически, полноэкранные фоны только растворяются.")]
    [SerializeField] private RectTransform _animatedContent;

    private Sequence _tween;
    private bool _isInBackground;

    private bool _initialized;
    private List<UIAnimTargets.Target> _targets;

    protected virtual bool StayInBackground => true;

    public bool IsVisible { get; private set; }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        KillTween();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _targets = _animatedContent != null
            ? new List<UIAnimTargets.Target>
            {
                new UIAnimTargets.Target
                {
                    Rect = _animatedContent,
                    Position = _animatedContent.anchoredPosition,
                    Scale = _animatedContent.localScale,
                    CanMove = true
                }
            }
            : UIAnimTargets.Collect(transform as RectTransform);
    }

    private void KillTween()
    {
        if (_tween != null)
        {
            _tween.Kill();
            _tween = null;
        }
    }

    private void ResetTransformToOriginal()
    {
        foreach (var t in _targets)
        {
            if (t.Rect == null)
                continue;

            if (t.CanMove)
                t.Rect.anchoredPosition = t.Position;
            t.Rect.localScale = t.Scale;
        }
    }

    private void AppendTargets(Sequence sequence, float offsetY, float scale, float duration, Ease ease)
    {
        foreach (var t in _targets)
        {
            if (t.Rect == null)
                continue;

            if (t.CanMove)
                sequence.Join(t.Rect.DOAnchorPos(t.Position + new Vector2(0f, offsetY), duration).SetEase(ease));
            sequence.Join(t.Rect.DOScale(t.Scale * scale, duration).SetEase(ease));
        }
    }

    private void SetTargetsState(float offsetY, float scale)
    {
        foreach (var t in _targets)
        {
            if (t.Rect == null)
                continue;

            if (t.CanMove)
                t.Rect.anchoredPosition = t.Position + new Vector2(0f, offsetY);
            t.Rect.localScale = t.Scale * scale;
        }
    }

    private Sequence CreateSequence()
    {
        return DOTween.Sequence().SetUpdate(true);
    }

    public virtual void Show()
    {
        EnsureInitialized();
        KillTween();
        _isInBackground = false;

        IsVisible = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        gameObject.SetActive(true);

        canvasGroup.alpha = 0f;
        SetTargetsState(ShowOffsetY, ShowStartScale);

        _tween = CreateSequence();
        _tween.Join(canvasGroup.DOFade(1f, ShowDuration).SetEase(Ease.OutCubic));
        AppendTargets(_tween, 0f, 1f, ShowDuration, Ease.OutCubic);

        OnShow();
    }

    public void Hide()
    {
        if (!gameObject.activeSelf && !IsVisible && !_isInBackground)
            return;

        EnsureInitialized();

        IsVisible = false;
        _isInBackground = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        OnHide();

        KillTween();
        _tween = CreateSequence();
        // Закрытие — только растворение, без сдвига и масштаба
        _tween.Join(canvasGroup.DOFade(0f, HideDuration).SetEase(Ease.InQuad));

        _tween.OnComplete(() =>
        {
            _tween = null;
            gameObject.SetActive(false);
            canvasGroup.alpha = 1f;
            ResetTransformToOriginal();
        });
    }

    public void PushToBackground()
    {
        if (!gameObject.activeSelf)
            return;

        EnsureInitialized();
        KillTween();
        OnHide();

        _isInBackground = true;
        IsVisible = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (!StayInBackground)
        {
            canvasGroup.alpha = 0f;
            ResetTransformToOriginal();
            gameObject.SetActive(false);
            return;
        }

        _tween = CreateSequence();
        _tween.Join(canvasGroup.DOFade(BackgroundAlpha, BackgroundDuration).SetEase(Ease.OutQuad));
        AppendTargets(_tween, 0f, 1f, BackgroundDuration, Ease.OutQuad);
    }

    public void PopFromBackground()
    {
        EnsureInitialized();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        KillTween();
        _isInBackground = false;
        IsVisible = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        OnShow();

        _tween = CreateSequence();
        _tween.Join(canvasGroup.DOFade(1f, BackgroundDuration).SetEase(Ease.OutCubic));
        AppendTargets(_tween, 0f, 1f, BackgroundDuration, Ease.OutCubic);
    }

    protected virtual void OnShow() { }

    protected virtual void OnHide() { }
}
