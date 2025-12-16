using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapbox.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using Zenject;

public class APIService
{
    private const string BaseUrl = "http://87.228.97.188/api/";
    private readonly MonoBehaviour _coroutineRunner;    
    private readonly UserDataService _userData;
    private readonly IPopupService _popupService;


    private string _accessToken;
    private string _refreshToken;

    public bool IsLoggedIn => !string.IsNullOrEmpty(_accessToken);

    public APIService([Inject] MonoBehaviour coroutineRunner, [Inject] UserDataService userData, IPopupService popupService)
    {
        _coroutineRunner = coroutineRunner;
        _userData = userData;

        _accessToken = _userData.AccessToken;
        _refreshToken = _userData.RefreshToken;
        _popupService = popupService;
    }

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

    public async Task<bool> Login(string username, string password)
    {
        
        var url = $"{BaseUrl}login/";
        var payload = new LoginRequest { username = username, password = password };

        var (success, response) = await SendRequest(url, "POST", payload, requireAuth: false);

        if (success)
        {
            try
            {
                var loginResponse = JsonUtility.FromJson<LoginResponse>(response);
                _accessToken = loginResponse.access;
                _refreshToken = loginResponse.refresh;
                
                Debug.Log("Login="+loginResponse.user.id);
                
                _userData.SetAuthData(username, password, _accessToken, _refreshToken);

                if (loginResponse.user != null)
                {
                    Debug.Log(loginResponse.user);
                    _userData.SetProfile(
                        gender: loginResponse.user.gender,
                        boots: loginResponse.user.boots,
                        pants: loginResponse.user.pants,
                        tshirt: loginResponse.user.tshirt,
                        cap: loginResponse.user.cap,
                        coins: loginResponse.user.coins,
                        level: loginResponse.user.level,
                        exp: loginResponse.user.exp,
                        steps: loginResponse.user.steps,
                        firstName: loginResponse.user.first_name,
                        lastName: loginResponse.user.last_name,
                        dateOfStart: loginResponse.user.registration_date,
                        id: loginResponse.user.id
                    );
                }

                return !string.IsNullOrEmpty(_accessToken);
            }
            catch (Exception e)
            {
                _popupService.ShowError($"Ошибка login: {e.Message}");
                return false;
            }
        }
        else
        {
            Debug.Log("Ne success login");
        }

        return false;
    }

    public async Task<bool> TryAutoLogin()
    {
        if (string.IsNullOrEmpty(_userData.Username) || string.IsNullOrEmpty(_userData.Password))
            return false;

        return await Login(_userData.Username, _userData.Password);
    }

    private async Task<bool> RefreshToken()
    {
        Debug.Log("Refresh Token");
        if (string.IsNullOrEmpty(_refreshToken))
        {
            Debug.LogError("[APIService] Нет refresh токена, нужно перелогиниться");
            return false;
        }

        var url = $"{BaseUrl}token/refresh/";
        var payload = new RefreshRequest { refresh = _refreshToken };

        var (success, response) = await SendRequest(url, "POST", payload, requireAuth: false);

        if (success)
        {
            try
            {
                var tokenResponse = JsonUtility.FromJson<LoginResponse>(response);
                _accessToken = tokenResponse.access;
                _userData.UpdateAccessToken(_accessToken);

                Debug.Log("[APIService] Access токен успешно обновлён");
                return true;
            }
            catch (Exception e)
            {
                _popupService.ShowError($"Ошибка обновления токена: {e.Message}");
                return false;
            }
        }

        return false;
    }
    
    public async Task<(bool success, string message)> UpdateClothes(int boots, int pants, int tshirt, int cap, string gender)
    {
        if (!IsLoggedIn)
            return (false, "Not logged in");

        var url = $"{BaseUrl}update-clothes/";
        var payload = new ClothesRequest { boots = boots, pants = pants, tshirt = tshirt, cap = cap, gender = gender };

        var result = await SendRequest(url, "PATCH", payload, requireAuth: true);

        if (result.success)
        {
            _userData.SetProfile(gender, boots, pants, tshirt, cap, _userData.Coins, _userData.Level, _userData.Experience, _userData.Steps, _userData.FirstName, _userData.LastName, _userData.DateOfStart, _userData.ID);
        }

        return result;
    }

    public async Task<(bool success, string message)> GetDailyQuests()
    {
        if (!IsLoggedIn)
            return (false, "Not logged in");

        var url = $"{BaseUrl}quests/daily";
        
        var result = await SendRequest(url, "GET", null, requireAuth: true);

        return result;
    }

    public async Task<(bool success, string message)> SearchPlayer(int playerID)
    {
        if (!IsLoggedIn)
            return (false, "Not logged in");
        
        var url = $"{BaseUrl}player/{playerID}";
        
        var result = await SendRequest(url, "GET", null, requireAuth: true);
        
        return result;
    }

    public async Task<(bool success, string message)> GetSightsList(int playerID)
    {
        if (!IsLoggedIn)
            return (false, "Not logged in");
        
        var url = $"{BaseUrl}player/{playerID}/landmarks/";

        var result = await SendRequest(url, "GET", null, requireAuth: true);
        
        return result;
    }
    
    public async Task<(bool success, string message)> GetShopitemsList()
    {
        if (!IsLoggedIn)
            return (false, "Not logged in");
        
        var url = $"{BaseUrl}shop/items/";

        var result = await SendRequest(url, "GET", null, requireAuth: true);
        
        return result;
    }

    public async Task<(bool success, string message)> BuyShopItem(int shopItemID)
    {
        if (!IsLoggedIn)
            return (false, "Not logged in");

        var url = $"{BaseUrl}shop/purchase/";

        var payload = new BuyShopItemRequest
        {
            item_id = shopItemID
        };

        return await SendRequest(url, "POST", payload, requireAuth: true);
    }

    
    public int[] ParseExternalIds(string json)
    {
        var data = JsonConvert.DeserializeObject<ResponseData>(json);

        if (data.external_ids == null)
            return new int[0];

        return data.external_ids
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(int.Parse)
            .ToArray();
    }

    public async Task<(bool success, string message)> SetSightMarked(int sightID)
    {
        if (!IsLoggedIn)
            return (false, "Not logged in");

        var url = $"{BaseUrl}landmarks/save/";

        var payload = new SaveSightRequest
        {
            player_id = _userData.ID,
            external_ids = new[] { sightID.ToString() }
        };

        var result = await SendRequest(url, "POST", payload, requireAuth: true);

        if (result.success)
        {
            Debug.Log("Set sight marked achieved successfully");
        }

        return result;
    }

    private async Task<(bool success, string response)> SendRequest(string url, string method, object payload, bool requireAuth)
    {
        var json = JsonUtility.ToJson(payload);
        var tcs = new TaskCompletionSource<(bool, string)>();

        _coroutineRunner.StartCoroutine(SendCoroutine(url, method, json, requireAuth, tcs, retry: true));

        return await tcs.Task;
    }

    private IEnumerator SendCoroutine(
        string url,
        string method,
        string json,
        bool requireAuth,
        TaskCompletionSource<(bool, string)> tcs,
        bool retry
    )
    {
        using var request = new UnityWebRequest(url, method);
        var bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        if (requireAuth && IsLoggedIn)
            request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");

        Debug.Log($"[APIService] {method} {url} → {json}");

        yield return request.SendWebRequest();

        if (request.responseCode == 401 && requireAuth && retry)
        {
            Debug.LogWarning("[APIService] Access токен истёк, пробуем обновить...");

            var refreshTask = RefreshToken();
            yield return new WaitUntil(() => refreshTask.IsCompleted);

            if (refreshTask.Result)
            {
                _coroutineRunner.StartCoroutine(SendCoroutine(url, method, json, requireAuth, tcs, retry: false));
                yield break;
            }
            else
            {
                tcs.TrySetResult((false, "Unauthorized: refresh failed"));
                yield break;
            }
        }

        if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"[APIService] Error: {request.error + request.downloadHandler.text}");
            _popupService.ShowError($"[APIService] Error: {request.error + request.downloadHandler.text}");
            tcs.TrySetResult((false, request.error));
        }
        else
        {
            var response = request.downloadHandler.text;
            Debug.Log($"[APIService] Response: {response}");
            tcs.TrySetResult((true, response));
        }
    }
    
    [Serializable]
    private class RegisterRequest
    {
        public string username;
        public string first_name;
        public string last_name;
        public string password;
    }
    
    [Serializable]
    public class BuyShopItemRequest
    {
        public int item_id;
    }

    [Serializable]
    private class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    private class RefreshRequest
    {
        public string refresh;
    }

    [Serializable]
    private class ClothesRequest
    {
        public int boots;
        public int pants;
        public int tshirt;
        public int cap;
        public string gender;
    }

    [Serializable]
    private class LoginResponse
    {
        public string access;
        public string refresh;
        public UserResponse user;
    }

    [Serializable]
    private class UserResponse
    {
        public int id;
        public string username;
        public string first_name;
        public string last_name;
        public string registration_date;
        public string gender;
        public int coins;
        public int boots;
        public int pants;
        public int tshirt;
        public int cap;
        public int level;
        public int exp;
        public int steps;
    }
    
    [Serializable]
    private class SaveSightRequest
    {
        public int player_id;
        public string[] external_ids;
    }
    
    [Serializable]
    public class ResponseData
    {
        public bool success { get; set; }
        public int player_id { get; set; }
        public string player_username { get; set; }
        public string[] external_ids { get; set; }
        public int total_count { get; set; }
    }
    
    [System.Serializable]
    public class PurchaseResult
    {
        public bool success;
        public string message;
        public string item_name;
        public int price_paid;
        public int remaining_coins;
        public string promo_code;
    }
}
