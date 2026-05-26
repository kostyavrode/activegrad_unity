using DG.Tweening;
using UnityEngine;

public class CullableObject : MonoBehaviour
{
    [SerializeField] private Vector3 _customBoundsSize = new Vector3(2f, 2f, 2f);
    [SerializeField] private float _fadeDuration = 0.2f;

    [SerializeField] public GameObject[] _distanceManagedObjects;
    [SerializeField] private float _distanceCullDistance = 20f;

    public float DistanceCullDistance => _distanceCullDistance;

    // ── состояние frustum culling ─────────────────────────────────────────────
    private Renderer    _renderer;
    private CanvasGroup _canvasGroup;
    private Vector3     _cachedSize;
    private bool        _targetVisible = true;
    private Tween       _fadeTween;

    // ── состояние distance culling ────────────────────────────────────────────
    private bool _isNear = true;

    // ── lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _renderer    = GetComponentInChildren<Renderer>();
        _canvasGroup = GetComponentInChildren<CanvasGroup>();
        _cachedSize  = _renderer != null ? _renderer.localBounds.size : _customBoundsSize;
    }

    private void OnEnable()  => FrustumCullingService.Instance?.Register(this);
    private void OnDestroy()
    {
        _fadeTween?.Kill();
        FrustumCullingService.Instance?.Unregister(this);
    }

    // ── frustum culling ───────────────────────────────────────────────────────

    public Bounds GetBounds() => new Bounds(transform.position, _cachedSize);

    public void SetVisible(bool visible)
    {
        if (_targetVisible == visible) return;
        _targetVisible = visible;

        if (_canvasGroup != null)
            FadeWithCanvasGroup(visible);
        else
            gameObject.SetActive(visible);
    }

    private void FadeWithCanvasGroup(bool visible)
    {
        _fadeTween?.Kill();
        if (visible)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _fadeTween = _canvasGroup.DOFade(1f, _fadeDuration).SetEase(Ease.OutQuad);
        }
        else
        {
            _fadeTween = _canvasGroup.DOFade(0f, _fadeDuration).SetEase(Ease.OutQuad)
                .OnComplete(() => { if (!_targetVisible) gameObject.SetActive(false); });
        }
    }

    // ── distance culling ──────────────────────────────────────────────────────

    public void SetNear(bool near)
    {
        if (_isNear == near) return;
        _isNear = near;

        foreach (var go in _distanceManagedObjects)
            if (go != null) go.SetActive(near);
    }

    // ── гизмо ────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_distanceManagedObjects == null || _distanceManagedObjects.Length == 0) return;
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, _distanceCullDistance);
    }
#endif
}
