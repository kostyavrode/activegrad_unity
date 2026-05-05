using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

public class PopupView : MonoBehaviour
{
    private const float AnimationDuration = 0.15f;

    [SerializeField] private Image _indicator;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _autoHideDelay = 3f;

    private Tween _tween;

    public void Setup(string message, Color indicatorColor)
    {
        _messageText.text = message;
        _indicator.color = indicatorColor;
        _closeButton.onClick.AddListener(Close);

        _canvasGroup.alpha = 0;
        _tween = _canvasGroup.DOFade(1f, AnimationDuration).SetUpdate(true);

        Invoke(nameof(Close), _autoHideDelay);
    }

    private void Close()
    {
        CancelInvoke(nameof(Close));
        _closeButton.onClick.RemoveAllListeners();

        _tween?.Kill();
        _tween = _canvasGroup.DOFade(0f, AnimationDuration)
            .SetUpdate(true)
            .OnComplete(() => Destroy(gameObject));
    }

    public class Factory : PlaceholderFactory<PopupView> { }
}
