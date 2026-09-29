using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// "Нажатие" кнопки: при нажатии сжимается, при отпускании пружинит обратно.
/// </summary>
public class UIPressable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float _pressedScale = 0.94f;
    [SerializeField] private float _pressDuration = 0.08f;
    [SerializeField] private float _releaseDuration = 0.25f;

    private Selectable _selectable;
    private Vector3 _originalScale = Vector3.one;
    private bool _hasOriginalScale;
    private bool _isPressed;
    private Tween _tween;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        CaptureOriginalScale();
    }

    private void CaptureOriginalScale()
    {
        if (_hasOriginalScale)
            return;
        _originalScale = transform.localScale;
        _hasOriginalScale = true;
    }

    /// <summary>
    /// Большие кнопки (полноэкранный фон «нажми, чтобы закрыть», карточки с кнопками внутри)
    /// не сжимаем — иначе вместе с ними сжимается всё окно.
    /// </summary>
    public static bool IsSuitable(RectTransform rect)
    {
        if (rect == null)
            return false;

        if (UIAnimTargets.IsFullScreen(rect))
            return false;

        var canvas = rect.GetComponentInParent<Canvas>();
        var root = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
        if (root != null)
        {
            var size = rect.rect.size;
            var rootSize = root.rect.size;
            if (rootSize.x > 0f && rootSize.y > 0f && size.x >= rootSize.x * 0.8f && size.y >= rootSize.y * 0.5f)
                return false;
        }

        var selectables = rect.GetComponentsInChildren<Selectable>(true);
        return selectables.Length <= 1;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        if (_selectable != null && !_selectable.IsInteractable())
            return;
        if (!IsSuitable(transform as RectTransform))
            return;

        CaptureOriginalScale();
        _isPressed = true;
        KillTween();
        _tween = transform.DOScale(_originalScale * _pressedScale, _pressDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void Release()
    {
        if (!_isPressed)
            return;

        _isPressed = false;
        KillTween();
        _tween = transform.DOScale(_originalScale, _releaseDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    private void KillTween()
    {
        if (_tween != null && _tween.IsActive())
            _tween.Kill();
        _tween = null;
    }

    private void RestoreScale()
    {
        KillTween();
        _isPressed = false;
        if (_hasOriginalScale && this != null)
            transform.localScale = _originalScale;
    }

    private void OnDisable()
    {
        RestoreScale();
    }

    private void OnDestroy()
    {
        KillTween();
    }
}
