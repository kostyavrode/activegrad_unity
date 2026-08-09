using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIModalAnimator : MonoBehaviour
{
    private const float ShowDuration = 0.2f;
    private const float HideDuration = 0.2f;
    private const float BackdropAlpha = 0.65f;

    [SerializeField] private RectTransform _contentPanel;
    [SerializeField] private CanvasGroup _backdrop;

    private CanvasGroup _contentGroup;
    private Tween _activeSequence;
    private bool _isClosing;
    private bool _isShown;

    public bool IsClosing => _isClosing;

    private void Start()
    {
        PlayShow();
    }

    private void OnDestroy()
    {
        _activeSequence?.Kill();
        DestroyBackdrop();
    }

    public void PlayShow()
    {
        if (_isShown)
            return;

        EnsureSetup();
        _isClosing = false;
        _activeSequence?.Kill();

        if (_contentGroup != null)
            _contentGroup.alpha = 1f;

        if (_backdrop == null)
            return;

        _isShown = true;
        _backdrop.alpha = 0f;
        _backdrop.gameObject.SetActive(true);

        _activeSequence = _backdrop
            .DOFade(1f, ShowDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void PlayHide(Action onComplete)
    {
        EnsureSetup();

        if (_isClosing)
            return;

        _isClosing = true;
        _activeSequence?.Kill();

        if (_backdrop == null)
        {
            onComplete?.Invoke();
            return;
        }

        _activeSequence = _backdrop
            .DOFade(0f, HideDuration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                DestroyBackdrop();
                onComplete?.Invoke();
            });
    }

    private void EnsureSetup()
    {
        if (_contentPanel == null)
            _contentPanel = transform as RectTransform;

        if (_contentGroup == null && _contentPanel != null)
        {
            _contentGroup = _contentPanel.GetComponent<CanvasGroup>();
            if (_contentGroup == null)
                _contentGroup = _contentPanel.gameObject.AddComponent<CanvasGroup>();
        }

        if (_backdrop != null)
            return;

        var parent = transform.parent;
        if (parent == null)
            return;

        var backdropObject = new GameObject(
            "ModalBackdrop",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));

        backdropObject.transform.SetParent(parent, false);
        backdropObject.transform.SetSiblingIndex(transform.GetSiblingIndex());

        var backdropRect = backdropObject.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        var backdropImage = backdropObject.GetComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, BackdropAlpha);
        backdropImage.raycastTarget = true;

        _backdrop = backdropObject.GetComponent<CanvasGroup>();
        _backdrop.alpha = 0f;
        _backdrop.blocksRaycasts = true;
        _backdrop.interactable = false;
    }

    private void DestroyBackdrop()
    {
        if (_backdrop == null)
            return;

        var backdropObject = _backdrop.gameObject;
        _backdrop = null;

        if (backdropObject != null)
            Destroy(backdropObject);
    }
}
