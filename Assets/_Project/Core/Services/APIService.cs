using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapbox.Json;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using Zenject;
using JsonConvert = Newtonsoft.Json.JsonConvert;

public class APIService
{
    private const string BaseUrl = "http://87.228.97.188/api/";
    private readonly MonoBehaviour _coroutineRunner;    
    private readonly UserDataService _userData;
    private readonly IPopupService _popupService;


    private string _accessToken;
    private string _refreshToken;
    private ErrorResponse _lastErrorResponse;

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
                var loginResponse = JsonConvert.DeserializeObject<LoginResponseDto>(response);
                _accessToken = loginResponse.access;
                _refreshToken = loginResponse.refresh;
                
                Debug.Log("Login="+loginResponse.user.id);
                
                _userData.SetAuthData(username, password, _accessToken, _refreshToken);

                if (loginResponse.user != null)
                {
                    var u = loginResponse.user;
                    int exp = u.experience > 0 ? u.experience : u.exp;
                    int steps = u.daily_steps > 0 ? u.daily_steps : u.steps;
                    string firstName = u.first_name ?? "";
                    string lastName = u.last_name ?? "";
                    _userData.SetProfile(
                        gender: u.gender ?? "",
                        boots: u.boots,
                        pants: u.pants,
                        tshirt: u.tshirt,
                        cap: u.cap,
                        coins: u.coins,
                        level: u.level,
                        exp: exp,
                        steps: steps,
                        firstName: firstName,
                        lastName: lastName,
                        dateOfStart: u.registration_date ?? "",
                        id: u.player_id > 0 ? u.player_id : u.id,
                        strength: u.strength > 0 ? u.strength : 1,
                        intelligence: u.intelligence > 0 ? u.intelligence : 1,
                        agility: u.agility > 0 ? u.agility : 1,
                        statUpgradePoints: u.stat_upgrade_points
                    );
                    if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                    {
                        TryFetchProfileNames(u.player_id > 0 ? u.player_id : u.id);
                    }
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

    private async void TryFetchProfileNames(int playerId)
    {
        await FetchProfileNamesAsync(playerId);
    }

    public async Task<bool> FetchProfileNamesAsync(int playerId)
    {
        var (success, message) = await SearchPlayer(playerId);
        if (!success || string.IsNullOrEmpty(message)) return false;
        try
        {
            var response = JsonConvert.DeserializeObject<RootResponse>(message);
            if (response?.playerData != null &&
                (!string.IsNullOrEmpty(response.playerData.FirstName) || !string.IsNullOrEmpty(response.playerData.LastName)))
            {
                _userData.UpdateNames(response.playerData.FirstName, response.playerData.LastName);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[APIService] Failed to parse profile for names: {ex.Message}");
        }
        return false;
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

    public async Task<(bool success, PromoCodesResponse response)> GetPromoCodes()
    {
        if (!IsLoggedIn)
            return (false, null);
        
        var url = $"{BaseUrl}shop/promo-codes/";
        
        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);
        
        if (!success)
        {
            return (false, null);
        }
        
        try
        {
            var response = JsonConvert.DeserializeObject<PromoCodesResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse promo codes response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
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

    public int[] ParsePartnerStoreIds(string json)
    {
        var data = JsonConvert.DeserializeObject<PartnerStoresPlayerResponse>(json);
        return data?.store_ids ?? Array.Empty<int>();
    }

    public async Task<(bool success, PartnerStoresNearbyResponse response)> GetNearbyPartnerStores(float latitude, float longitude)
    {
        if (!IsLoggedIn)
            return (false, null);

        string lat = latitude.ToString(CultureInfo.InvariantCulture);
        string lon = longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"{BaseUrl}partner-stores/nearby/?latitude={lat}&longitude={lon}";

        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);
        if (!success)
        {
            Debug.LogWarning($"[PartnerStores nearby] Request failed. URL={url}\nError/body: {message}");
            return (false, null);
        }

        try
        {
            var response = JsonConvert.DeserializeObject<PartnerStoresNearbyResponse>(message);
            var ok = response?.success == true;
            var storeCount = response?.stores?.Length ?? 0;
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine(
                $"[PartnerStores nearby] success={ok} lat={latitude.ToString(inv)} lon={longitude.ToString(inv)} radius_km={(response != null ? response.radius_km.ToString(inv) : "-")} total_count={response?.total_count} stores.Length={storeCount}");
            sb.AppendLine($"[PartnerStores nearby] Raw JSON: {message}");
            if (response?.stores != null)
            {
                for (var i = 0; i < response.stores.Length; i++)
                {
                    var s = response.stores[i];
                    if (s == null) continue;
                    sb.AppendLine(
                        $"  [{i}] id={s.id} name={s.name} dist_km={s.distance_km.ToString(inv)} lat={s.latitude.ToString(inv)} lon={s.longitude.ToString(inv)}");
                }
            }
            Debug.Log(sb.ToString());
            return (ok, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse partner-stores/nearby response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    public async Task<(bool success, PartnerStoresSaveResponse response)> SavePartnerStores(int[] storeIds)
    {
        if (!IsLoggedIn)
            return (false, null);

        var payload = new SavePartnerStoresRequest
        {
            player_id = _userData.ID,
            store_ids = storeIds
        };

        var url = $"{BaseUrl}partner-stores/save/";
        var (success, message) = await SendRequest(url, "POST", payload, requireAuth: true);
        if (!success)
            return (false, null);

        try
        {
            var response = JsonConvert.DeserializeObject<PartnerStoresSaveResponse>(message);
            return (response?.success == true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse partner-stores/save response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    public async Task<(bool success, PartnerStoresPlayerResponse response)> GetPlayerPartnerStores(int playerId)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}partner-stores/player/{playerId}/";
        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);
        if (!success)
            return (false, null);

        try
        {
            var response = JsonConvert.DeserializeObject<PartnerStoresPlayerResponse>(message);
            return (response?.success == true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse partner-stores/player response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    /// <summary>
    /// Отметиться на достопримечательности. За новую отметку выдаются ресурсы (metal, wood, blueprints).
    /// </summary>
    public async Task<(bool success, SaveLandmarksResponse response)> SetSightMarked(int sightID)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}landmarks/save/";

        var payload = new SaveSightRequest
        {
            player_id = _userData.ID,
            external_ids = new[] { sightID.ToString() }
        };

        var (success, message) = await SendRequest(url, "POST", payload, requireAuth: true);

        if (!success)
            return (false, null);

        try
        {
            var response = JsonConvert.DeserializeObject<SaveLandmarksResponse>(message);
            if (response != null)
            {
                SightMarkedEvent.Invoke(sightID);
                return (true, response);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse landmarks/save response: {ex.Message}\nResponse: {message}");
        }

        return (false, null);
    }

    /// <summary>
    /// Получить инвентарь игрока (ресурсы + предметы). Универсальный формат: массивы.
    /// </summary>
    public async Task<(bool success, InventoryResponse response)> GetInventory()
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}inventory/";
        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);

        if (!success)
            return (false, null);

        try
        {
            var response = JsonConvert.DeserializeObject<InventoryResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse inventory response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    /// <summary>
    /// Получить рецепты крафта. Универсальный формат: массив рецептов.
    /// </summary>
    public async Task<(bool success, InventoryRecipesResponse response)> GetInventoryRecipes()
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}inventory/recipes/";
        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);

        if (!success)
            return (false, null);

        try
        {
            var response = JsonConvert.DeserializeObject<InventoryRecipesResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse recipes response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    /// <summary>
    /// Скрафтить предмет по ID. Универсальный метод.
    /// </summary>
    public async Task<(bool success, InventoryCraftResponse response)> CraftItem(string itemId)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}inventory/craft/{itemId}/";
        var (success, message) = await SendRequest(url, "POST", new EmptyRequest(), requireAuth: true);

        if (!success)
            return (false, null);

        try
        {
            var response = JsonConvert.DeserializeObject<InventoryCraftResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse craft response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    /// <summary>
    /// Улучшить меч. Ресурсы влияют на вероятность успеха.
    /// </summary>
    public async Task<(bool success, InventoryUpgradeResponse response)> UpgradeSword(int metal, int wood, int blueprints)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}inventory/upgrade/sword/";
        var payload = new InventoryUpgradeRequest { metal = metal, wood = wood, blueprints = blueprints };
        var (success, message) = await SendRequest(url, "POST", payload, requireAuth: true);

        if (!success)
            return (false, null);

        try
        {
            var response = JsonConvert.DeserializeObject<InventoryUpgradeResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse upgrade sword response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    /// <summary>
    /// Улучшить щит.
    /// </summary>
    public async Task<(bool success, InventoryUpgradeResponse response)> UpgradeShield(int metal, int wood, int blueprints)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}inventory/upgrade/shield/";
        var payload = new InventoryUpgradeRequest { metal = metal, wood = wood, blueprints = blueprints };
        var (success, message) = await SendRequest(url, "POST", payload, requireAuth: true);

        if (!success)
            return (false, null);

        try
        {
            var response = JsonConvert.DeserializeObject<InventoryUpgradeResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse upgrade shield response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    /// <summary>
    /// Получить информацию о захвате достопримечательности.
    /// </summary>
    public async Task<(bool success, LandmarkCaptureInfoResponse response)> GetLandmarkCaptureInfo(int externalId)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}landmarks/{externalId}/capture/";

        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);

        if (!success)
        {
            return (false, null);
        }

        try
        {
            var response = JsonConvert.DeserializeObject<LandmarkCaptureInfoResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse landmark capture info response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    /// <summary>
    /// Захват достопримечательности (новая серверная логика).
    /// </summary>
    public async Task<(bool success, LandmarkCaptureCreateResponse response)> CaptureLandmark(int externalId)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}landmarks/capture/";

        var payload = new LandmarkCaptureRequest
        {
            external_id = externalId.ToString()
        };

        var (success, message) = await SendRequest(url, "POST", payload, requireAuth: true);

        if (!success)
        {
            // Ошибка уже показана внутри SendRequest через _popupService (если это 400/другая ошибка).
            return (false, null);
        }

        try
        {
            var response = JsonConvert.DeserializeObject<LandmarkCaptureCreateResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse landmark capture response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }
    
    public async Task<(bool success, string message)> UpdateDailySteps(int steps)
    {
        if (!IsLoggedIn) return (false, "Not logged in");
        var url = $"{BaseUrl}player/daily-steps/";
        var payload = new DailyStepsRequest { daily_steps = steps };
        var (ok, msg) = await SendRequest(url, "POST", payload, requireAuth: true, suppressErrorPopup: true);
        return (ok, ok ? "" : msg);
    }

    public async Task<(bool success, QuestCompleteResponse response)> CompleteQuest(int questId, int steps = 0)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}quests/{questId}/complete/";
        
        var payload = new QuestCompleteRequest
        {
            player_id = _userData.ID,
            steps = steps
        };
        
        var (success, message) = await SendRequest(url, "POST", payload, requireAuth: true);
        
        if (!success)
        {
            Debug.LogError(message);
            return (false, null);
        }
        
        try
        {
            var response = JsonConvert.DeserializeObject<QuestCompleteResponse>(message);
            
            if (response != null && !response.success)
            {
                Debug.LogWarning($"[APIService] Server rejected quest completion: {response.message}");
                return (false, response);
            }
            
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse quest complete response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }
    
    public async Task<(bool success, PlayerStatsResponse response)> GetPlayerStats()
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}player/stats/";
        
        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);
        
        if (!success)
        {
            return (false, null);
        }
        
        try
        {
            var response = JsonConvert.DeserializeObject<PlayerStatsResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse player stats response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }
    
    public async Task<(bool success, UpgradeStatResponse response)> UpgradeStat(string statType)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}player/upgrade-stat/";
        var payload = new UpgradeStatRequest { stat_type = statType };

        var (success, message) = await SendRequest(url, "POST", payload, requireAuth: true);

        if (!success)
            return (false, null);

        try
        {
            var response = JsonConvert.DeserializeObject<UpgradeStatResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse upgrade stat response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    public async Task<(bool success, CoinsResponse response)> GetPlayerCoins()
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}player/coins/";
        
        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);
        
        if (!success)
        {
            return (false, null);
        }
        
        try
        {
            var response = JsonConvert.DeserializeObject<CoinsResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse coins response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    private static bool HttpMethodTypicallyHasNoBody(string method)
    {
        if (string.IsNullOrEmpty(method)) return false;
        switch (method.ToUpperInvariant())
        {
            case "GET":
            case "HEAD":
            case "DELETE":
                return true;
            default:
                return false;
        }
    }

    private async Task<(bool success, string response)> SendRequest(string url, string method, object payload, bool requireAuth, bool suppressErrorPopup = false)
    {
        string requestBody;
        if (payload != null)
            requestBody = JsonUtility.ToJson(payload);
        else if (HttpMethodTypicallyHasNoBody(method))
            requestBody = "";
        else
            requestBody = "{}";

        var tcs = new TaskCompletionSource<(bool, string)>();

        _coroutineRunner.StartCoroutine(SendCoroutine(url, method, requestBody, requireAuth, tcs, retry: true, suppressErrorPopup));

        return await tcs.Task;
    }

    private IEnumerator SendCoroutine(
        string url,
        string method,
        string requestBody,
        bool requireAuth,
        TaskCompletionSource<(bool, string)> tcs,
        bool retry,
        bool suppressErrorPopup = false
    )
    {
        using var request = new UnityWebRequest(url, method);
        if (!string.IsNullOrEmpty(requestBody))
        {
            var bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");
        }

        request.downloadHandler = new DownloadHandlerBuffer();

        if (requireAuth && IsLoggedIn)
            request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");

        if (string.IsNullOrEmpty(requestBody))
            Debug.Log($"[APIService] {method} {url}");
        else
            Debug.Log($"[APIService] {method} {url} → {requestBody}");

        yield return request.SendWebRequest();

        if (request.responseCode == 401 && requireAuth && retry)
        {
            Debug.LogWarning("[APIService] Access токен истёк, пробуем обновить...");

            var refreshTask = RefreshToken();
            yield return new WaitUntil(() => refreshTask.IsCompleted);

            if (refreshTask.Result)
            {
                _coroutineRunner.StartCoroutine(SendCoroutine(url, method, requestBody, requireAuth, tcs, retry: false, suppressErrorPopup));
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
            var errorText = request.downloadHandler.text;
            
            if (request.responseCode == 400 && !string.IsNullOrEmpty(errorText))
            {
                try
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(errorText);
                    if (errorResponse != null)
                    {
                        _lastErrorResponse = errorResponse; // Сохраняем последний ответ об ошибке
                        var errorTextToShow = !string.IsNullOrEmpty(errorResponse.message)
                            ? errorResponse.message
                            : errorResponse.error ?? "";
                        if (!string.IsNullOrEmpty(errorTextToShow))
                        {
                            Debug.LogWarning($"[APIService] Server error (400): {errorTextToShow}");
                            _popupService.ShowError(errorTextToShow);
                            tcs.TrySetResult((false, errorTextToShow));
                            yield break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[APIService] Failed to parse error response: {ex.Message}");
                }
            }
            
            Debug.LogError($"[APIService] Error: {request.error}\nResponse: {errorText}");
            if (!suppressErrorPopup)
                _popupService.ShowError($"[APIService] Error: {request.error}");
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

    private class LoginResponseDto
    {
        [Newtonsoft.Json.JsonProperty("access")] public string access;
        [Newtonsoft.Json.JsonProperty("refresh")] public string refresh;
        [Newtonsoft.Json.JsonProperty("user")] public LoginUserDto user;
    }

    private class LoginUserDto
    {
        [Newtonsoft.Json.JsonProperty("id")] public int id;
        [Newtonsoft.Json.JsonProperty("player_id")] public int player_id;
        [Newtonsoft.Json.JsonProperty("username")] public string username;
        [Newtonsoft.Json.JsonProperty("first_name")] public string first_name;
        [Newtonsoft.Json.JsonProperty("last_name")] public string last_name;
        [Newtonsoft.Json.JsonProperty("registration_date")] public string registration_date;
        [Newtonsoft.Json.JsonProperty("gender")] public string gender;
        [Newtonsoft.Json.JsonProperty("coins")] public int coins;
        [Newtonsoft.Json.JsonProperty("boots")] public int boots;
        [Newtonsoft.Json.JsonProperty("pants")] public int pants;
        [Newtonsoft.Json.JsonProperty("tshirt")] public int tshirt;
        [Newtonsoft.Json.JsonProperty("cap")] public int cap;
        [Newtonsoft.Json.JsonProperty("level")] public int level;
        [Newtonsoft.Json.JsonProperty("experience")] public int experience;
        [Newtonsoft.Json.JsonProperty("exp")] public int exp;
        [Newtonsoft.Json.JsonProperty("daily_steps")] public int daily_steps;
        [Newtonsoft.Json.JsonProperty("steps")] public int steps;
        [Newtonsoft.Json.JsonProperty("strength")] public int strength;
        [Newtonsoft.Json.JsonProperty("intelligence")] public int intelligence;
        [Newtonsoft.Json.JsonProperty("agility")] public int agility;
        [Newtonsoft.Json.JsonProperty("stat_upgrade_points")] public int stat_upgrade_points;
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
    public class SaveLandmarksResponse
    {
        public bool success;
        public string message;
        public int player_id;
        public string[] saved_external_ids;
        public int total_saved;
        public ResourcesGained resources_gained;
    }

    [Serializable]
    public class ResourcesGained
    {
        public int metal;
        public int wood;
        public int blueprints;
    }

    [Serializable]
    public class PartnerStoreInfo
    {
        public int id;
        public string name;
        public string address;
        public string image_url;
        public float latitude;
        public float longitude;
        public float distance_km;
        public string[] tags;
    }

    [Serializable]
    public class PartnerStoresNearbyResponse
    {
        public bool success;
        public float latitude;
        public float longitude;
        public float radius_km;
        public PartnerStoreInfo[] stores;
        public int total_count;
    }

    [Serializable]
    private class SavePartnerStoresRequest
    {
        public int player_id;
        public int[] store_ids;
    }

    [Serializable]
    public class PartnerStoresSaveResponse
    {
        public bool success;
        public string message;
        public int player_id;
        public int[] saved_store_ids;
        public int total_saved;
        public ResourcesGained resources_gained;
    }

    [Serializable]
    public class PartnerStoresPlayerResponse
    {
        public bool success;
        public int player_id;
        public string player_username;
        public int[] store_ids;
        public int total_count;
    }

    [Serializable]
    private class EmptyRequest { }
    
    [Serializable]
    private class LandmarkCaptureRequest
    {
        public string external_id;
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
    
    [Serializable]
    private class DailyStepsRequest { public int daily_steps; }

    [Serializable]
    public class QuestCompleteRequest
    {
        public int player_id;
        public int steps;
    }
    
    [Serializable]
    public class QuestCompleteResponse
    {
        public bool success;
        public string message;
        public RewardGiven reward_given;
        public PlayerStats player_stats;
        public LevelUpNotification level_up_notification;
    }
    
    [Serializable]
    public class RewardGiven
    {
        public string type;
        public int amount;
        public int new_experience;
    }
    
    [Serializable]
    public class PlayerStats
    {
        public int coins;
        public int experience;
        public int level;
        public int experience_to_next_level;
        public int strength;
        public int intelligence;
        public int agility;
        public int stat_upgrade_points;
        public LevelUpInfo level_up;
    }
    
    [Serializable]
    public class LevelUpInfo
    {
        public int new_level;
        public int levels_gained;
    }
    
    [Serializable]
    public class LevelUpNotification
    {
        public int new_level;
        public int levels_gained;
        public int stat_upgrade_points_gained;
    }
    
    [Serializable]
    public class PlayerStatsResponse
    {
        public bool success;
        public PlayerStatsData player_stats;
    }
    
    [Serializable]
    public class PlayerStatsData
    {
        public int id;
        public int player_id;
        public string username;
        public int coins;
        public int experience;
        public int level;
        public int experience_to_next_level;
        public int experience_per_level;
        public float progress_to_next_level_percent;
        public int strength;
        public int intelligence;
        public int agility;
        public int stat_upgrade_points;
    }
    
    [Serializable]
    public class CoinsResponse
    {
        public bool success;
        public int coins;
        public int player_id;
    }
    
    [Serializable]
    public class UpgradeStatRequest
    {
        public string stat_type;
    }

    [Serializable]
    public class UpgradeStatResponse
    {
        public bool success;
        public string message;
        public string stat_upgraded;
        public int new_value;
        public int stat_upgrade_points_remaining;
        public UpgradeStatPlayerStats player_stats;
    }

    [Serializable]
    public class UpgradeStatPlayerStats
    {
        public int strength;
        public int intelligence;
        public int agility;
        public int stat_upgrade_points;
    }

    [Serializable]
    public class ErrorResponse
    {
        public bool success;
        public string message;
        public string error;
        public Dictionary<string, string[]> errors;
    }
    
    [Serializable]
    public class PromoCodesResponse
    {
        public bool success;
        public int player_id;
        public PromoCodeData[] promo_codes;
        public int total_count;
    }
    
    [Serializable]
    public class PromoCodeData
    {
        public int id;
        public int quest_id;
        public string quest_title;
        public string quest_description;
        public string quest_image_url;
        public string promo_code;
        public string date;
        public string obtained_at;
    }
    
    public async Task<(bool success, SendFriendRequestResponse response)> SendFriendRequest(int toUserId)
    {
        if (!IsLoggedIn)
            return (false, null);
        
        var url = $"{BaseUrl}friends/requests/send/";
        var payload = new SendFriendRequestPayload { to_user_id = toUserId };
        
        var (success, message) = await SendRequest(url, "POST", payload, requireAuth: true);
        
        if (!success)
        {
            return (false, null);
        }
        
        try
        {
            var response = JsonConvert.DeserializeObject<SendFriendRequestResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse send friend request response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }
    
    public async Task<(bool success, AcceptFriendRequestResponse response)> AcceptFriendRequest(int requestId)
    {
        if (!IsLoggedIn)
            return (false, null);
        
        var url = $"{BaseUrl}friends/requests/{requestId}/accept/";
        
        var (success, message) = await SendRequest(url, "POST", null, requireAuth: true);
        
        if (!success)
        {
            return (false, null);
        }
        
        try
        {
            var response = JsonConvert.DeserializeObject<AcceptFriendRequestResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse accept friend request response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }
    
    public async Task<(bool success, string message)> RejectFriendRequest(int requestId)
    {
        if (!IsLoggedIn)
            return (false, "Not logged in");
        
        var url = $"{BaseUrl}friends/requests/{requestId}/reject/";
        
        var (success, message) = await SendRequest(url, "POST", null, requireAuth: true);
        
        return (success, message);
    }
    
    public async Task<(bool success, FriendsListResponse response)> GetFriends()
    {
        if (!IsLoggedIn)
            return (false, null);
        
        var url = $"{BaseUrl}friends/";
        
        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);
        
        if (!success)
        {
            return (false, null);
        }
        
        try
        {
            var response = JsonConvert.DeserializeObject<FriendsListResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse friends list response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }
    
    public async Task<(bool success, PendingFriendRequestsResponse response)> GetPendingFriendRequests()
    {
        if (!IsLoggedIn)
            return (false, null);
        
        var url = $"{BaseUrl}friends/requests/pending/";
        
        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);
        
        if (!success)
        {
            return (false, null);
        }
        
        try
        {
            var response = JsonConvert.DeserializeObject<PendingFriendRequestsResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse pending friend requests response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }
    
    public async Task<(bool success, SentFriendRequestsResponse response)> GetSentFriendRequests()
    {
        if (!IsLoggedIn)
            return (false, null);
        
        var url = $"{BaseUrl}friends/requests/sent/";
        
        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);
        
        if (!success)
        {
            return (false, null);
        }
        
        try
        {
            var response = JsonConvert.DeserializeObject<SentFriendRequestsResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse sent friend requests response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }
    
    public async Task<(bool success, string message)> RemoveFriend(int friendId)
    {
        if (!IsLoggedIn)
            return (false, "Not logged in");
        
        var url = $"{BaseUrl}friends/{friendId}/remove/";
        
        var (success, message) = await SendRequest(url, "DELETE", null, requireAuth: true);
        
        return (success, message);
    }
    
    [Serializable]
    public class SendFriendRequestPayload
    {
        public int to_user_id;
    }
    
    [Serializable]
    public class SendFriendRequestResponse
    {
        public bool success;
        public string message;
        public FriendRequestData friend_request;
    }
    
    [Serializable]
    public class AcceptFriendRequestResponse
    {
        public bool success;
        public string message;
        public FriendshipData friendship;
    }
    
    [Serializable]
    public class FriendsListResponse
    {
        public bool success;
        public FriendData[] friends;
        public int total_count;
    }
    
    [Serializable]
    public class PendingFriendRequestsResponse
    {
        public bool success;
        public FriendRequestData[] pending_requests;
        public int total_count;
    }
    
    [Serializable]
    public class SentFriendRequestsResponse
    {
        public bool success;
        public FriendRequestData[] sent_requests;
        public int total_count;
    }
    
    [Serializable]
    public class FriendData
    {
        public int id;
        public string username;
        public string first_name;
        public string last_name;
        public int level;
        public string gender;
    }
    
    [Serializable]
    public class FriendRequestData
    {
        public int id;
        public FriendData from_user;
        public FriendData to_user;
        public string status;
        public string created_at;
        public string updated_at;
    }
    
    [Serializable]
    public class FriendshipData
    {
        public int id;
        public FriendData friend;
        public string created_at;
    }
    
    [Serializable]
    public class InventoryUpgradeRequest
    {
        public int metal;
        public int wood;
        public int blueprints;
    }

    [Serializable]
    public class InventoryResponse
    {
        public bool success;
        public InventoryResourceData[] resources;
        public InventoryItemData[] items;
        public InventoryData inventory;
    }

    [Serializable]
    public class InventoryResourceData
    {
        public string id;
        public int amount;
        public string display_name;
    }

    [Serializable]
    public class InventoryItemData
    {
        public string id;
        public string display_name;
        public bool has_item;
        public int? sharpness;
        public int? durability;
    }

    [Serializable]
    public class InventoryData
    {
        public InventoryResourceData[] resources;
        public InventoryItemData[] items;
    }

    [Serializable]
    public class InventoryRecipesResponse
    {
        public bool success;
        public InventoryRecipeData[] recipes;
    }

    [Serializable]
    public class InventoryRecipeData
    {
        public string id;
        public string display_name;
        public InventoryRequirementData[] requirements;
    }

    [Serializable]
    public class InventoryRequirementData
    {
        public string resource_id;
        public int amount;
        public string display_name;
    }

    [Serializable]
    public class InventoryCraftResponse
    {
        public bool success;
        public string message;
        public InventoryData inventory;
    }

    [Serializable]
    public class InventoryUpgradeResponse
    {
        public bool success;
        public bool upgraded;
        public float probability;
        public float roll;
        public int sword_sharpness;   // для меча
        public int? shield_durability; // для щита
        public InventoryData inventory;
    }

    [Serializable]
    public class LandmarkCaptureInfoResponse
    {
        public bool success;
        public bool captured;
        public bool can_capture_now;
        public LandmarkCaptureUser captured_by;
        public string captured_at;
        public LandmarkCaptureClan clan;
        public int? defender_shield_level;
        public int? time_until_next_capture_minutes;
        public int? time_until_next_capture_seconds;
        public string block_reason;
    }

    [Serializable]
    public class LandmarkCaptureCreateResponse
    {
        public bool success;
        public string message;
        public LandmarkCaptureRecord capture;
    }

    [Serializable]
    public class LandmarkCaptureRecord
    {
        public int id;
        public string external_id;
        public LandmarkCaptureUser captured_by;
        public string captured_at;
        public LandmarkCaptureClan clan;
    }

    [Serializable]
    public class LandmarkCaptureUser
    {
        public int id;
        public string username;
    }

    [Serializable]
    public class LandmarkCaptureClan
    {
        public int id;
        public string name;
    }
    
    public async Task<(bool success, CreateClanResponse response)> CreateClan(string name, string description = "")
    {
        if (!IsLoggedIn)
            return (false, null);

        _lastErrorResponse = null;

        var url = $"{BaseUrl}clans/create/";
        var payload = new CreateClanRequest
        {
            name = name,
            description = description
        };

        var (success, message) = await SendRequest(url, "POST", payload, requireAuth: true);

        if (!success)
        {
            return (false, null);
        }

        try
        {
            var response = JsonConvert.DeserializeObject<CreateClanResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse create clan response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }
    
    public ErrorResponse GetLastErrorResponse()
    {
        return _lastErrorResponse;
    }
    
    public async Task<(bool success, JoinClanResponse response)> JoinClan(int clanId)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}clans/join/";
        var payload = new JoinClanRequest
        {
            clan_id = clanId
        };

        var (success, message) = await SendRequest(url, "POST", payload, requireAuth: true);

        if (!success)
        {
            Debug.LogError("[APIService] Failed to join clan response: " + message);
            return (false, null);
        }

        try
        {
            var response = JsonConvert.DeserializeObject<JoinClanResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse join clan response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }
    
    public async Task<(bool success, string message)> LeaveClan()
    {
        if (!IsLoggedIn)
            return (false, "Not logged in");

        var url = $"{BaseUrl}clans/leave/";

        var (success, message) = await SendRequest(url, "POST", null, requireAuth: true);

        return (success, message);
    }
    
    public async Task<(bool success, SearchClansResponse response)> SearchClans(string query)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}clans/search/?query={UnityWebRequest.EscapeURL(query)}";

        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);

        if (!success)
        {
            return (false, null);
        }

        try
        {
            var response = JsonConvert.DeserializeObject<SearchClansResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse search clans response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }
    
    public async Task<(bool success, TopClansResponse response)> GetTopClans()
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}clans/top/";

        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);

        if (!success)
        {
            return (false, null);
        }

        try
        {
            var response = JsonConvert.DeserializeObject<TopClansResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse top clans response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    public async Task<(bool success, ClanMembersResponse response)> GetClanMembers(int clanId)
    {
        if (!IsLoggedIn)
            return (false, null);

        var url = $"{BaseUrl}clans/{clanId}/members/";

        var (success, message) = await SendRequest(url, "GET", null, requireAuth: true);

        if (!success)
            return (false, null);

        try
        {
            var response = JsonConvert.DeserializeObject<ClanMembersResponse>(message);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse clan members response: {ex.Message}\nResponse: {message}");
            return (false, null);
        }
    }

    public async Task<(bool success, ClanData clan)> GetMyClan()
    {
        if (!IsLoggedIn)
            return (false, null);

        var (success, message) = await SearchPlayer(_userData.ID);
        
        if (!success)
        {
            Debug.LogWarning($"[APIService] GetMyClan: SearchPlayer failed. Message: {message}");
            return (false, null);
        }

        try
        {
            Debug.Log($"[APIService] GetMyClan: Raw response: {message}");
            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<RootResponse>(message);
            
            if (response == null)
            {
                Debug.LogError("[APIService] GetMyClan: Deserialized response is null");
                return (false, null);
            }
            
            Debug.Log($"[APIService] GetMyClan: Response success={response.Success}, playerData={(response.playerData != null ? "not null" : "null")}");
            
            if (response.playerData != null)
            {
                Debug.Log($"[APIService] GetMyClan: Clan={(response.playerData.clan != null ? $"id={response.playerData.clan.id}, name={response.playerData.clan.name}" : "null")}");
                
                if (response.playerData.clan != null)
                {
                    return (true, response.playerData.clan);
                }
            }
            
            Debug.Log("[APIService] GetMyClan: No clan found in response");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[APIService] Failed to parse my clan response: {ex.Message}\nStack trace: {ex.StackTrace}\nResponse: {message}");
            return (false, null);
        }
    }
    
    
    [Serializable]
    private class CreateClanRequest
    {
        public string name;
        public string description;
    }

    [Serializable]
    public class CreateClanResponse
    {
        public bool success;
        public string message;
        public ClanData clan;
    }

    [Serializable]
    private class JoinClanRequest
    {
        public int clan_id;
    }

    [Serializable]
    public class JoinClanResponse
    {
        public bool success;
        public string message;
        public ClanData clan;
    }

    [Serializable]
    public class SearchClansResponse
    {
        public bool success;
        public ClanData[] clans;
        public int total_count;
        public string query;
    }

    [Serializable]
    public class TopClansResponse
    {
        public bool success;
        public ClanData[] top_clans;
        public int total_count;
    }
}


[Serializable]
public class ClanData
{
    public int id;
    public string name;
    public string description;
    public string created_at;
    public int created_by_id;
    public string created_by_username;
    public int member_count;
    public int captured_landmarks_count;
}

[Serializable]
public class ClanMember
{
    public int id;
    public string username;
    public int level;
    public string joined_at;
}

[Serializable]
public class ClanMembersResponse
{
    public bool success;
    public int clan_id;
    public ClanMember[] members;
}
