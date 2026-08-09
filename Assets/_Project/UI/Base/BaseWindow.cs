using DG.Tweening;
using UnityEngine;

public abstract class BaseWindow : MonoBehaviour, IWindow
{
    private const float ShowDuration = 0.2f;
    private const float HideDuration = 0.2f;
    private const float BackgroundDuration = 0.2f;
    private const float BackgroundAlpha = 0.65f;

    [SerializeField] private CanvasGroup canvasGroup;

    private Tween _tween;
    private bool _isInBackground;

    public bool IsVisible { get; private set; }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnDestroy()
    {
        _tween?.Kill();
    }

    public virtual void Show()
    {
        _tween?.Kill();
        _isInBackground = false;

        IsVisible = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        gameObject.SetActive(true);

        canvasGroup.alpha = 0f;
        _tween = canvasGroup
            .DOFade(1f, ShowDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        OnShow();
    }

    public void Hide()
    {
        if (!gameObject.activeSelf && !IsVisible && !_isInBackground)
            return;

        IsVisible = false;
        _isInBackground = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        OnHide();

        _tween?.Kill();
        _tween = canvasGroup
            .DOFade(0f, HideDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                canvasGroup.alpha = 1f;
                gameObject.SetActive(false);
            });
    }

    public void PushToBackground()
    {
        if (!gameObject.activeSelf)
            return;

        _tween?.Kill();
        OnHide();

        _isInBackground = true;
        IsVisible = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        _tween = canvasGroup
            .DOFade(BackgroundAlpha, BackgroundDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void PopFromBackground()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        _tween?.Kill();
        _isInBackground = false;
        IsVisible = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        OnShow();

        _tween = canvasGroup
            .DOFade(1f, BackgroundDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    protected virtual void OnShow() { }

    protected virtual void OnHide() { }
}
