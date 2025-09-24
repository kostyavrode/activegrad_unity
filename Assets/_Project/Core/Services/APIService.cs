using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

public class APIService
{
    private const string BaseUrl = "http://127.0.0.1:8000/api/";
    private readonly MonoBehaviour _coroutineRunner;
    private string _token;

    public bool IsLoggedIn => !string.IsNullOrEmpty(_token);

    public APIService([Inject] MonoBehaviour coroutineRunner)
    {
        _coroutineRunner = coroutineRunner;
    }

    // ----------- REGISTER -----------
    public async Task<(bool success, string message)> Register(string username, string firstName, string lastName, string password)
    {
        var url = $"{BaseUrl}register/";
        var payload = new RegisterRequest
        {
            username = username,
            first_name = firstName,
            last_name = lastName,
            password = password
        };

        return await SendRequest(url, "POST", payload, requireAuth: false);
    }

    // ----------- LOGIN -----------
    public async Task<bool> Login(string username, string password)
    {
        var url = $"{BaseUrl}login/";
        var payload = new LoginRequest { username = username, password = password };

        var (success, response) = await SendRequest(url, "POST", payload, requireAuth: false);

        if (success)
        {
            try
            {
                var tokenResponse = JsonUtility.FromJson<TokenResponse>(response);
                _token = tokenResponse?.token;
            }
            catch (Exception e)
            {
                Debug.LogError($"[APIService] Ошибка парсинга токена: {e.Message}");
                return false;
            }
        }

        return success && IsLoggedIn;
    }

    // ----------- UPDATE CLOTHES -----------
    public async Task<(bool success, string message)> UpdateClothes(int boots, int pants, int tshirt, int cap)
    {
        if (!IsLoggedIn)
            return (false, "Not logged in");

        var url = $"{BaseUrl}update-clothes/";
        var payload = new ClothesRequest { boots = boots, pants = pants, tshirt = tshirt, cap = cap };

        return await SendRequest(url, "PATCH", payload, requireAuth: true);
    }

    // ----------- CORE SENDER -----------
    private async Task<(bool success, string response)> SendRequest(string url, string method, object payload, bool requireAuth)
    {
        var json = JsonUtility.ToJson(payload);
        var tcs = new TaskCompletionSource<(bool, string)>();

        _coroutineRunner.StartCoroutine(SendCoroutine(url, method, json, requireAuth, tcs));

        return await tcs.Task;
    }

    private IEnumerator SendCoroutine(string url, string method, string json, bool requireAuth, TaskCompletionSource<(bool, string)> tcs)
    {
        using var request = new UnityWebRequest(url, method);
        var bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        if (requireAuth && IsLoggedIn)
            request.SetRequestHeader("Authorization", $"Bearer {_token}");

        Debug.Log($"[APIService] {method} {url} → {json}");

        yield return request.SendWebRequest();

        if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"[APIService] Error: {request.error}");
            tcs.TrySetResult((false, request.error));
        }
        else
        {
            var response = request.downloadHandler.text;
            Debug.Log($"[APIService] Response: {response}");
            tcs.TrySetResult((true, response));
        }
    }

    // ----------- DTOs -----------
    [Serializable]
    private class RegisterRequest
    {
        public string username;
        public string first_name;
        public string last_name;
        public string password;
    }

    [Serializable]
    private class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    private class ClothesRequest
    {
        public int boots;
        public int pants;
        public int tshirt;
        public int cap;
    }

    [Serializable]
    private class TokenResponse
    {
        public string token;
    }
}
