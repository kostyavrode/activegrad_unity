using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class UIButtonAttentionPulse : MonoBehaviour
{
    [SerializeField] private RectTransform _target;
    [SerializeField] private float _scaleMax = 1.04f;
    [SerializeField] private float _duration = 0.85f;

    private Tween _pulseTween;
    private Vector3 _initialScale = Vector3.one;

    private void Awake()
    {
        if (_target == null)
            _target = transform as RectTransform;

        if (_target != null)
            _initialScale = _target.localScale;
    }

    private void OnEnable()
    {
        if (_target == null)
            return;

        _pulseTween?.Kill();
        _target.localScale = _initialScale;

        _pulseTween = _target
            .DOScale(_initialScale * _scaleMax, _duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        _pulseTween?.Kill();

        if (_target != null)
            _target.localScale = _initialScale;
    }

    private void OnDestroy()
    {
        _pulseTween?.Kill();
    }
}
