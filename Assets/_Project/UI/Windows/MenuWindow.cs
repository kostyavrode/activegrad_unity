using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class MenuWindow : BaseWindow
{
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button profileButton;

    [Inject] private LazyInject<UIManager> _uiManager;

    protected override void OnShow()
    {
        settingsButton.onClick.AddListener(OnSettingsClicked);
        profileButton.onClick.AddListener(OnProfileClicked);
    }

    protected override void OnHide()
    {
        settingsButton.onClick.RemoveAllListeners();
        profileButton.onClick.RemoveAllListeners();
    }

    private void OnSettingsClicked()
    {
        _uiManager.Value.Show<SettingsWindow>();
    }

    private void OnProfileClicked()
    {
        _uiManager.Value.Show<ProfileWindow>();
    }
}
