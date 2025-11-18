using System;
using Zenject;

public class SettingsMediator : IInitializable, IDisposable
{
    private readonly SettingsWindow _settingsWindow;
    private readonly AudioManager _audioManager;
    private readonly AudioSettings _settings;
    private readonly UIManager _uiManager;

    public SettingsMediator(SettingsWindow window, AudioManager audioManager, AudioSettings settings, UIManager uiManager)
    {
        _settingsWindow = window;
        _audioManager = audioManager;
        _settings = settings;
        _uiManager = uiManager;
    }

    public void Initialize()
    {
        _settingsWindow.OnWindowOpened += OnOpened;
        _settingsWindow.OnMusicVolumeChanged += OnMusicChanged;
        _settingsWindow.OnSfxVolumeChanged += OnSfxChanged;
        _settingsWindow.OnBackClicked += () => { _uiManager.Back(); };
    }

    public void Dispose()
    {
        _settingsWindow.OnWindowOpened -= OnOpened;
    }

    private void OnOpened()
    {
        _settingsWindow.SetMusicSliderValue(_settings.MusicVolume);
        _settingsWindow.SetSfxSliderValue(_settings.SfxVolume);
    }

    private void OnMusicChanged(float v)
    {
        _settings.SetMusicVolume(v);
        _audioManager.SetMusicVolume(v);
    }

    private void OnSfxChanged(float v)
    {
        _settings.SetSfxVolume(v);
        _audioManager.SetSfxVolume(v);
    }
}