using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class OtherPlayerSightsDetailsView : MonoBehaviour
{
    [SerializeField] private Button _closeButton;

    public Transform Content;

    private UIModalAnimator _modalAnimator;

    private void Awake()
    {
        _modalAnimator = GetComponent<UIModalAnimator>();
        if (_modalAnimator == null)
            _modalAnimator = gameObject.AddComponent<UIModalAnimator>();

        _closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        _closeButton.onClick.RemoveAllListeners();
    }

    public void Close()
    {
        if (_modalAnimator != null && _modalAnimator.IsClosing)
            return;

        if (_modalAnimator != null)
        {
            _modalAnimator.PlayHide(() => Destroy(gameObject));
            return;
        }

        Destroy(gameObject);
    }

    public class Factory : PlaceholderFactory<OtherPlayerSightsDetailsView> { }
}
