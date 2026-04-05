using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsWindow : BaseWindow
{
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;
    
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _logoutButton;

    public event Action OnBackClicked;
    public event Action OnWindowOpened;
    public event Action<float> OnMusicVolumeChanged;
    public event Action<float> OnSfxVolumeChanged;
    public event Action OnLogoutClicked;

    protected override void OnShow()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        _logoutButton?.onClick.AddListener(() => OnLogoutClicked?.Invoke());
        OnWindowOpened?.Invoke();
        _musicSlider.onValueChanged.AddListener(v => OnMusicVolumeChanged?.Invoke(v));
        _sfxSlider.onValueChanged.AddListener(v => OnSfxVolumeChanged?.Invoke(v));
    }

    protected override void OnHide()
    {
        _backButton.onClick.RemoveAllListeners();
        _logoutButton?.onClick.RemoveAllListeners();
        _musicSlider.onValueChanged.RemoveAllListeners();
        _sfxSlider.onValueChanged.RemoveAllListeners();
    }

    public void SetMusicSliderValue(float value)
    {
        _musicSlider.SetValueWithoutNotify(value);
    }

    public void SetSfxSliderValue(float value)
    {
        _sfxSlider.SetValueWithoutNotify(value);
    }
}