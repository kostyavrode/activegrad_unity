using UnityEngine;
using Zenject;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;

public class AppLoader : MonoBehaviour
{
    [Inject] private APIService _apiService;
    [Inject] private UserDataService _userData;
    [Inject] private UIManager _uiManager;

    private async void Start()
    {
        Debug.Log("AppLoader: Starting initialization...");

        Application.targetFrameRate = GetTargetFrameRate();
        
        if (!_apiService.IsLoggedIn)
        {
            if (!string.IsNullOrEmpty(_userData.Data.username) &&
                !string.IsNullOrEmpty(_userData.Data.password))
            {
                Debug.Log("AppLoader: Найдены сохранённые данные. Пробуем авто-логин...");

                bool loginSuccess = await _apiService.Login(
                    _userData.Data.username,
                    _userData.Data.password
                );

                if (loginSuccess)
                {
                    Debug.Log("AppLoader: Автологин успешен ✅");
                    _uiManager.Dispose();
                    await LoadMainSceneAsync();
                }
                else
                {
                    Debug.LogWarning("AppLoader: Автологин не удался ❌, показываем логин");
                    _uiManager.Show<LoginWindow>(); // ⬅ вместо ShowLoginScreen()
                }
            }
            else
            {
                Debug.Log("AppLoader: Данных нет. Показываем окно логина.");
                _uiManager.Show<LoginWindow>(); // ⬅ вместо ShowLoginScreen()
            }
        }
        else
        {
            Debug.Log("AppLoader: Уже залогинен, грузим сцену");
            await LoadMainSceneAsync();
        }
    }

    private async Task LoadMainSceneAsync()
    {
        await LoadingOverlay.LoadSceneWithOverlayAsync("SampleScene");
    }

    private static int GetTargetFrameRate()
    {
        // На телефонах 90–120 fps удваивают нагрузку, телефон греется и троттлит — стабильные 60 плавнее.
        if (Application.isMobilePlatform)
            return 60;

        try
        {
            double hz = Screen.currentResolution.refreshRateRatio.value;
            if (double.IsNaN(hz) || double.IsInfinity(hz) || hz <= 0)
                return 60;
            return Mathf.Clamp((int)Math.Round(hz), 60, 120);
        }
        catch
        {
            return 60;
        }
    }
}