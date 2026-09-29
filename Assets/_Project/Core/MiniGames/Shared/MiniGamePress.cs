using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Лёгкий эффект нажатия для кнопок мини-игр: сжатие при нажатии, упругий возврат при отпускании.
/// Работает в unscaled time.
/// </summary>
public class MiniGamePress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float PressedScale = 0.93f;

    private Vector3    _baseScale = Vector3.one;
    private bool       _hasBase;
    private bool       _down;
    private Tween      _tween;
    private Selectable _selectable;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_selectable == null) _selectable = GetComponent<Selectable>();
        if (_selectable != null && !_selectable.IsInteractable()) return;

        if (!_hasBase) { _baseScale = transform.localScale; _hasBase = true; }
        _down = true;
        Animate(_baseScale * PressedScale, 0.08f, Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)   => Release();
    public void OnPointerExit(PointerEventData eventData) => Release();

    private void Release()
    {
        if (!_down) return;
        _down = false;
        Animate(_baseScale, 0.28f, Ease.OutBack);
    }

    private void Animate(Vector3 target, float duration, Ease ease)
    {
        _tween?.Kill();
        _tween = transform.DOScale(target, duration).SetEase(ease).SetUpdate(true).SetLink(gameObject);
    }

    private void OnDisable()
    {
        _tween?.Kill();
        _down = false;
        if (_hasBase) transform.localScale = _baseScale;
    }

    private void OnDestroy()
    {
        _tween?.Kill();
    }
}
