using System;
using Zenject;

public class SettingsMediator : IInitializable, IDisposable
{
    private readonly SettingsWindow _settingsWindow;
    private readonly AudioManager _audioManager;
    private readonly AudioSettings _settings;
    private readonly UIManager _uiManager;
    private readonly APIService _apiService;
    private readonly UserDataService _userDataService;
    private readonly SceneLoader _sceneLoader;
    private readonly Action _backHandler;

    public SettingsMediator(
        SettingsWindow window,
        AudioManager audioManager,
        AudioSettings settings,
        UIManager uiManager,
        APIService apiService,
        UserDataService userDataService,
        SceneLoader sceneLoader)
    {
        _settingsWindow = window;
        _audioManager = audioManager;
        _settings = settings;
        _uiManager = uiManager;
        _apiService = apiService;
        _userDataService = userDataService;
        _sceneLoader = sceneLoader;
        _backHandler = HandleBackClicked;
    }

    public void Initialize()
    {
        _settingsWindow.OnWindowOpened += OnOpened;
        _settingsWindow.OnMusicVolumeChanged += OnMusicChanged;
        _settingsWindow.OnSfxVolumeChanged += OnSfxChanged;
        _settingsWindow.OnBackClicked += _backHandler;
        _settingsWindow.OnLogoutClicked += OnLogoutClicked;
    }

    public void Dispose()
    {
        _settingsWindow.OnWindowOpened -= OnOpened;
        _settingsWindow.OnMusicVolumeChanged -= OnMusicChanged;
        _settingsWindow.OnSfxVolumeChanged -= OnSfxChanged;
        _settingsWindow.OnBackClicked -= _backHandler;
        _settingsWindow.OnLogoutClicked -= OnLogoutClicked;
    }

    private void OnOpened()
    {
        _settingsWindow.SetMusicSliderValue(_settings.MusicVolume);
        _settingsWindow.SetSfxSliderValue(_settings.SfxVolume);
    }

    private void OnMusicChanged(float v)
    {
        _audioManager.SetMusicVolume(v);
    }

    private void OnSfxChanged(float v)
    {
        _audioManager.SetSfxVolume(v);
    }

    private void HandleBackClicked()
    {
        _uiManager.Back();
    }

    private void OnLogoutClicked()
    {
        _apiService.Logout();
        _sceneLoader.LoadScene("Loading");
    }
}