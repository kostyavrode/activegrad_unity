using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

public class MenuWindow : BaseWindow
{
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _profileButton;
    [SerializeField] private Button _questButton;
    
    public event Action OnProfileClicked;
    public event Action OnSettingsClicked;
    public event Action OnQuestsClicked;

    protected override void OnShow()
    {
        _settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());
        _profileButton.onClick.AddListener(() => OnProfileClicked?.Invoke());
        _questButton.onClick.AddListener(() => OnQuestsClicked?.Invoke());
    }

    protected override void OnHide()
    {
        _settingsButton.onClick.RemoveAllListeners();
        _profileButton.onClick.RemoveAllListeners();
        _questButton.onClick.RemoveAllListeners();
    }
}
