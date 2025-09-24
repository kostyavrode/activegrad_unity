using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class MenuWindow : BaseWindow
{
    [SerializeField] private Button settingsButton;

    [Inject] private LazyInject<UIManager> _uiManager;

    protected override void OnShow()
    {
        settingsButton.onClick.AddListener(OnSettingsClicked);
    }

    protected override void OnHide()
    {
        settingsButton.onClick.RemoveAllListeners();
    }

    private void OnSettingsClicked()
    {
        _uiManager.Value.Show<SettingsWindow>();
    }
}
