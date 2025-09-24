using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class SettingsWindow : BaseWindow
{
    [SerializeField] private Button backButton;
    
    [Inject] private LazyInject<UIManager> _uiManager;

    protected override void OnShow()
    {
        backButton.onClick.AddListener(OnBackClicked);
    }

    protected override void OnHide()
    {
        backButton.onClick.RemoveAllListeners();
    }

    private void OnBackClicked()
    {
        _uiManager.Value.Back();
    }
}
