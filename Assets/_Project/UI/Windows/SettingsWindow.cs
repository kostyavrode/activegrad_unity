using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsWindow : BaseWindow
{
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;
    
    [SerializeField] private TMP_Text _musicPercentText;
    [SerializeField] private TMP_Text _sfxPercentText;
    
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
        
        _musicSlider.onValueChanged.AddListener(OnMusicSliderValueChanged);
        _sfxSlider.onValueChanged.AddListener(OnSfxSliderValueChanged);
        
        // Инициализация отображения процентов при открытии окна
        UpdateMusicPercentText(_musicSlider.value);
        UpdateSfxPercentText(_sfxSlider.value);
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
        UpdateMusicPercentText(value);
    }

    public void SetSfxSliderValue(float value)
    {
        _sfxSlider.SetValueWithoutNotify(value);
        UpdateSfxPercentText(value);
    }
    
    private void OnMusicSliderValueChanged(float value)
    {
        OnMusicVolumeChanged?.Invoke(value);
        UpdateMusicPercentText(value);
    }
    
    private void OnSfxSliderValueChanged(float value)
    {
        OnSfxVolumeChanged?.Invoke(value);
        UpdateSfxPercentText(value);
    }
    
    private void UpdateMusicPercentText(float sliderValue)
    {
        if (_musicPercentText != null)
        {
            int percent = Mathf.RoundToInt(sliderValue * 100f);
            _musicPercentText.text = $"{percent}%";
        }
    }
    
    private void UpdateSfxPercentText(float sliderValue)
    {
        if (_sfxPercentText != null)
        {
            int percent = Mathf.RoundToInt(sliderValue * 100f);
            _sfxPercentText.text = $"{percent}%";
        }
    }
}