using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

public class PopupView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float FadeInDuration = 0.2f;
    private const float SlideInDuration = 0.35f;
    private const float SlideInOffsetY = 120f;
    private const float StackMoveDuration = 0.25f;
    private const float CloseDuration = 0.2f;
    private const float CloseOffsetY = 80f;
    private const float SwipeCloseThreshold = 40f;
    private const float ResumeAutoHideDelay = 1.5f;

    [SerializeField] private Image _indicator;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _autoHideDelay = 3f;

    private RectTransform _rect;
    private Canvas _rootCanvas;
    private Vector2 _basePosition;
    private float _stackOffset;
    private bool _isClosing;
    private bool _closedRaised;
    private bool _isDragging;
    private float _dragOffsetY;

    private Sequence _tween;
    private Tween _moveTween;
    private Tween _indicatorTween;

    public event Action<PopupView> Closed;

    public bool IsClosing => _isClosing;

    public float Height
    {
        get
        {
            var rect = transform as RectTransform;
            return rect != null ? rect.rect.height : 0f;
        }
    }

    public void Setup(string message, Color indicatorColor, float stackOffset = 0f)
    {
        _rect = transform as RectTransform;
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _rootCanvas = GetComponentInParent<Canvas>();

        _messageText.text = message;
        _indicator.color = indicatorColor;
        _closeButton.onClick.AddListener(Close);

        _stackOffset = stackOffset;
        _canvasGroup.alpha = 0f;

        _tween = DOTween.Sequence().SetUpdate(true);
        _tween.Join(_canvasGroup.DOFade(1f, FadeInDuration).SetEase(Ease.OutQuad));

        if (_rect != null)
        {
            _basePosition = _rect.anchoredPosition;
            var target = GetTargetPosition();
            _rect.anchoredPosition = target + new Vector2(0f, SlideInOffsetY);
            _tween.Join(_rect.DOAnchorPos(target, SlideInDuration).SetEase(Ease.OutBack));
        }

        if (_indicator != null)
        {
            _indicatorTween = _indicator.transform
                .DOPunchScale(Vector3.one * 0.3f, 0.35f, 6, 0.6f)
                .SetDelay(SlideInDuration * 0.6f)
                .SetUpdate(true);
        }

        Invoke(nameof(Close), _autoHideDelay);
    }

    /// <summary>
    /// Sets the downward stacking offset (in canvas units) relative to the popup's initial position; animated.
    /// </summary>
    public void SetStackOffset(float offset, bool animated = true)
    {
        _stackOffset = offset;

        if (_rect == null || _isClosing || _isDragging)
            return;

        KillMoveTween();

        if (!animated)
        {
            _rect.anchoredPosition = GetTargetPosition();
            return;
        }

        _moveTween = _rect.DOAnchorPos(GetTargetPosition(), StackMoveDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isClosing || _rect == null)
            return;

        _isDragging = true;
        _dragOffsetY = 0f;
        CancelInvoke(nameof(Close));
        KillMoveTween();

        // Finish the slide-in position part so drag starts from a stable point.
        if (_tween != null && _tween.IsActive())
            _tween.Complete();
        _rect.anchoredPosition = GetTargetPosition();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _isClosing || _rect == null)
            return;

        var scale = _rootCanvas != null && _rootCanvas.scaleFactor > 0f ? _rootCanvas.scaleFactor : 1f;
        _dragOffsetY += eventData.delta.y / scale;

        // Only follow upward drags fully; resist downward.
        var visualOffset = _dragOffsetY > 0f ? _dragOffsetY : _dragOffsetY * 0.2f;
        _rect.anchoredPosition = GetTargetPosition() + new Vector2(0f, visualOffset);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        _isDragging = false;

        if (_isClosing || _rect == null)
            return;

        if (_dragOffsetY > SwipeCloseThreshold)
        {
            Close();
            return;
        }

        KillMoveTween();
        _moveTween = _rect.DOAnchorPos(GetTargetPosition(), StackMoveDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        Invoke(nameof(Close), ResumeAutoHideDelay);
    }

    private Vector2 GetTargetPosition()
    {
        return _basePosition - new Vector2(0f, _stackOffset);
    }

    private void KillMoveTween()
    {
        if (_moveTween != null)
        {
            _moveTween.Kill();
            _moveTween = null;
        }
    }

    private void Close()
    {
        if (_isClosing)
            return;

        _isClosing = true;
        CancelInvoke(nameof(Close));
        if (_closeButton != null)
            _closeButton.onClick.RemoveAllListeners();

        KillMoveTween();
        _tween?.Kill();

        _tween = DOTween.Sequence().SetUpdate(true);
        _tween.Join(_canvasGroup.DOFade(0f, CloseDuration).SetEase(Ease.InQuad));
        if (_rect != null)
        {
            _tween.Join(_rect.DOAnchorPos(_rect.anchoredPosition + new Vector2(0f, CloseOffsetY), CloseDuration)
                .SetEase(Ease.InQuad));
        }
        _tween.OnComplete(() => Destroy(gameObject));

        RaiseClosed();
    }

    private void RaiseClosed()
    {
        if (_closedRaised)
            return;

        _closedRaised = true;
        Closed?.Invoke(this);
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(Close));
        _tween?.Kill();
        KillMoveTween();
        _indicatorTween?.Kill();
        RaiseClosed();
    }

    public class Factory : PlaceholderFactory<PopupView> { }
}
