using UnityEngine;
using Zenject;
using UnityEngine.SceneManagement;

public class AppLoader : MonoBehaviour
{
    [Inject] private APIService _apiService;

    private async void Start()
    {
        Debug.Log("AppLoader: Starting initialization...");
        
        if (!_apiService.IsLoggedIn)
        {
            Debug.Log("APIService: Пользователь не вошёл. Можно показать логин-окно.");
        }

        //await LoadMainSceneAsync();
    }

    private async System.Threading.Tasks.Task LoadMainSceneAsync()
    {
        var asyncOp = SceneManager.LoadSceneAsync("SampleScene");
        while (!asyncOp.isDone)
            await System.Threading.Tasks.Task.Yield();
    }
}