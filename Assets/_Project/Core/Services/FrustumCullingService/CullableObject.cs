using DG.Tweening;
using UnityEngine;

public class CullableObject : MonoBehaviour
{
    [SerializeField] private Vector3 _customBoundsSize = new Vector3(2f, 2f, 2f);
    [SerializeField] private float _fadeDuration = 0.2f;

    private Renderer _renderer;
    private CanvasGroup _canvasGroup;
    private Vector3 _cachedSize;

    private bool _targetVisible = true;
    private Tween _fadeTween;

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _canvasGroup = GetComponentInChildren<CanvasGroup>();

        _cachedSize = _renderer != null
            ? _renderer.localBounds.size
            : _customBoundsSize;
    }

    private void OnEnable()
    {
        FrustumCullingService.Instance?.Register(this);
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();
        FrustumCullingService.Instance?.Unregister(this);
    }

    public Bounds GetBounds()
    {
        return new Bounds(transform.position, _cachedSize);
    }

    public void SetVisible(bool visible)
    {
        // Не запускаем анимацию повторно если состояние не изменилось
        if (_targetVisible == visible) return;
        _targetVisible = visible;

        if (_canvasGroup != null)
        {
            FadeWithCanvasGroup(visible);
        }
        else
        {
            // Нет CanvasGroup — просто мгновенно переключаем
            gameObject.SetActive(visible);
        }
    }

    private void FadeWithCanvasGroup(bool visible)
    {
        _fadeTween?.Kill();

        if (visible)
        {
            // Включаем сразу, затем плавно показываем
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _fadeTween = _canvasGroup
                .DOFade(1f, _fadeDuration)
                .SetEase(Ease.OutQuad);
        }
        else
        {
            // Плавно скрываем, затем отключаем
            _fadeTween = _canvasGroup
                .DOFade(0f, _fadeDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (!_targetVisible)
                        gameObject.SetActive(false);
                });
        }
    }
}
