using UnityEngine;
using Zenject;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class AppLoader : MonoBehaviour
{
    [Inject] private APIService _apiService;
    [Inject] private UserDataService _userData;
    [Inject] private UIManager _uiManager;

    private async void Start()
    {
        Debug.Log("AppLoader: Starting initialization...");

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
        var asyncOp = SceneManager.LoadSceneAsync("SampleScene");
        while (!asyncOp.isDone)
            await Task.Yield();
    }
}