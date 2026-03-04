using DG.Tweening;
using UnityEngine;

public abstract class BaseWindow : MonoBehaviour, IWindow
{
    private const float AnimationDuration = 0.15f;

    [SerializeField] private CanvasGroup canvasGroup;

    private Tween _tween;

    public bool IsVisible { get; private set; }

    public virtual void Show()
    {
        _tween?.Kill();

        IsVisible = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        gameObject.SetActive(true);

        canvasGroup.alpha = 0;
        _tween = canvasGroup.DOFade(1f, AnimationDuration).SetUpdate(true);

        OnShow();
    }

    public void Hide()
    {
        IsVisible = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        OnHide();

        _tween?.Kill();
        _tween = canvasGroup.DOFade(0f, AnimationDuration)
            .SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));
    }

    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
}
