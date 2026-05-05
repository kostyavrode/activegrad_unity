using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class OtherPlayerSightsDetailsView : MonoBehaviour
{
    private const float AnimationDuration = 0.15f;

    [SerializeField] private Button _closeButton;
    [SerializeField] private CanvasGroup _canvasGroup;

    public Transform Content;

    private Tween _tween;

    private void Awake()
    {
        _closeButton.onClick.AddListener(Close);

        _canvasGroup.alpha = 0;
        _tween = _canvasGroup.DOFade(1f, AnimationDuration).SetUpdate(true);
    }

    private void OnDestroy()
    {
        _closeButton.onClick.RemoveAllListeners();
    }

    private void Close()
    {
        _tween?.Kill();
        _tween = _canvasGroup.DOFade(0f, AnimationDuration)
            .SetUpdate(true)
            .OnComplete(() => Destroy(gameObject));
    }

    public class Factory : PlaceholderFactory<OtherPlayerSightsDetailsView> { }
}
