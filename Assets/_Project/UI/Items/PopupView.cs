using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

public class PopupView : MonoBehaviour
{
    [SerializeField] private Image _indicator;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private float _autoHideDelay = 3f;

    public void Setup(string message, Color indicatorColor)
    {
        _messageText.text = message;
        _indicator.color = indicatorColor;
        _closeButton.onClick.AddListener(Close);
        Invoke(nameof(Close), _autoHideDelay);
    }

    private void Close()
    {
        Destroy(gameObject);
    }

    public class Factory : PlaceholderFactory<PopupView> { }
}