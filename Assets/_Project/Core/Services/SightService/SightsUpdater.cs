using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Mapbox.Examples;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

public class SightsUpdater : IInitializable, IDisposable
{
    public Dictionary<int, SightShortInfo> CachedNearestSights { get; private set; }
    public Dictionary<int, SightFullInfo> CachedSights { get; private set; }
    public Dictionary<int, APIService.PartnerStoreInfo> CachedNearestPartnerStores { get; private set; }

    public Dictionary<int, Sprite> ImageCache { get; private set; } = new Dictionary<int, Sprite>();
    public Dictionary<int, Sprite> PartnerStoreImageCache { get; private set; } = new Dictionary<int, Sprite>();

    public event Action<int, Sprite> OnImageLoaded;
    public event Action OnSightsUpdated;
    public event Action OnPartnerStoresUpdated;

    private readonly ISightService _sightService;
    private readonly LocationService _locationService;
    private readonly SightDetailsView.Factory _sightDetailsViewFactory;
    private readonly PartnerStoreDetailsView.Factory _partnerStoreDetailsViewFactory;
    private readonly IPopupService _popupService;
    private readonly SpawnOnMap _spawnOnMap;
    private readonly APIService _apiService;
    private readonly UserDataService _userData;
    private readonly IInventoryService _inventoryService;

    private bool _isRunning;
    private bool _isLoadingImages;
    private const float MinUpdateIntervalSec = 15f;
    private const float MaxUpdateWithoutMovementSec = 30f;
    private const float MinDistanceDegrees = 0.0005f;
    private const float PollIntervalSec = 3f;
    private const int DetailsBatchSize = 5;
    private const int DetailsBatchDelayMs = 2000;
    private Vector2 _lastUpdateCoords;
    private float _lastUpdateTime;
    private bool _lastUpdateCoordsInitialized;

    public SightsUpdater(ISightService sightService, LocationService locationService, SightDetailsView.Factory sightDetailsViewFactory, PartnerStoreDetailsView.Factory partnerStoreDetailsViewFactory, IPopupService popupService, SpawnOnMap spawnOnMap,
        APIService apiService, UserDataService userDataService, IInventoryService inventoryService)
    {
        _sightService = sightService;
        _locationService = locationService;
        _sightDetailsViewFactory = sightDetailsViewFactory;
        _partnerStoreDetailsViewFactory = partnerStoreDetailsViewFactory;
        _popupService = popupService;
        _spawnOnMap = spawnOnMap;
        _apiService = apiService;
        _userData = userDataService;
        _inventoryService = inventoryService;
    }

    public void Initialize()
    {
        _isRunning = true;
        RunUpdateLoop();
    }

    public void Dispose()
    {
        _isRunning = false;
    }

    public async void CreateSightDetailsPopup(int pageID, bool isOtherSights=false)
    {
        Transform canvasTransform = GameObject.FindGameObjectWithTag("Canvas").transform;
        if (canvasTransform.childCount > 1)
        {
            Debug.LogError("[SightsUpdater] Canvas child count is more than 1]");
            return;
        }
        var popup = _sightDetailsViewFactory.Create();
        popup.transform.SetParent(canvasTransform, false);
        if (!ImageCache.TryGetValue(pageID, out var sprite))
            sprite = Resources.Load<Sprite>("no_image");
        
        popup.Init(sprite, CachedSights[pageID].Title, CachedSights[pageID].Description,
            CachedSights[pageID].PageId);

        popup.OnCaptureClicked += HandleCaptureButtonClicked;

        await LoadAndDisplayCaptureInfo(popup, pageID);

        _ = RefreshCaptureInfoLoop(popup, pageID);

        float distToSight = GetDistanceToSight(pageID);
        if (distToSight <= 600 && _userData.CheckSight(pageID))
        {
            popup.SetCheckInButtonState(true);
            popup.OnCheckInClicked += HandleCheckInButtonClicked;
        }
    }

    private async Task LoadAndDisplayCaptureInfo(SightDetailsView popup, int pageID, APIService.LandmarkCaptureRecord freshCapture = null)
    {
        var (success, response) = await _apiService.GetLandmarkCaptureInfo(pageID);

        if (success && response != null)
        {
            string capturedBy, capturedAt, clanName;
            if (freshCapture != null)
            {
                capturedBy = freshCapture.captured_by != null ? freshCapture.captured_by.username : "";
                capturedAt = freshCapture.captured_at ?? "";
                clanName = freshCapture.clan != null ? freshCapture.clan.name : "";
            }
            else
            {
                capturedBy = response.captured_by != null ? response.captured_by.username : "";
                capturedAt = response.captured_at ?? "";
                clanName = response.clan != null ? response.clan.name : "";
            }

            int? shieldLevel = response.defender_shield_level.HasValue && response.defender_shield_level.Value > 0 ? response.defender_shield_level : (int?)null;
            bool hasCooldown = (response.time_until_next_capture_minutes ?? 0) > 0 || (response.time_until_next_capture_seconds ?? 0) > 0;
            int? timeMin = hasCooldown ? response.time_until_next_capture_minutes : (int?)null;
            int? timeSec = hasCooldown ? response.time_until_next_capture_seconds : (int?)null;

            popup.SetCaptureInfo(response.captured, capturedBy, capturedAt, clanName,
                shieldLevel, timeMin, timeSec, response.block_reason);

            float distToSight = GetDistanceToSight(pageID);
            if (distToSight <= 600)
            {
                popup.SetCaptureButtonState(response.can_capture_now);
            }
            else
            {
                popup.SetCaptureButtonState(false);
            }
        }
        else if (freshCapture == null)
        {
            popup.SetCaptureInfo(false, "", "", "");
            popup.SetCaptureButtonState(true);
        }
    }

    private const float CaptureInfoRefreshIntervalSec = 10f;

    private async Task RefreshCaptureInfoLoop(SightDetailsView popup, int pageId)
    {
        while (popup != null && popup.gameObject != null)
        {
            await Task.Delay((int)(CaptureInfoRefreshIntervalSec * 1000));
            if (popup == null || !popup.gameObject) break;
            await LoadAndDisplayCaptureInfo(popup, pageId);
        }
    }

    private async void RunUpdateLoop()
    {
        while (_isRunning)
        {
            Vector2 coords = _locationService.GetCoordinates();
            if (coords != Vector2.zero)
            {
                float timeSinceUpdate = _lastUpdateCoordsInitialized ? Time.realtimeSinceStartup - _lastUpdateTime : float.MaxValue;
                float distDegrees = _lastUpdateCoordsInitialized ? Vector2.Distance(coords, _lastUpdateCoords) : float.MaxValue;
                bool timeOk = timeSinceUpdate >= MinUpdateIntervalSec;
                bool distanceOk = distDegrees >= MinDistanceDegrees;
                bool staleByTime = timeSinceUpdate >= MaxUpdateWithoutMovementSec;

                if (timeOk && (distanceOk || staleByTime))
                {
                    // Partner stores first: UpdateSights() spends a long time on Wikipedia details + image
                    // loading before it returns; otherwise partner markers only appear after that work finishes.
                    await UpdatePartnerStores(coords);
                    await UpdateSights();
                    _lastUpdateCoords = _locationService.GetCoordinates();
                    _lastUpdateTime = Time.realtimeSinceStartup;
                    _lastUpdateCoordsInitialized = true;
                }
            }
            await Task.Delay((int)(PollIntervalSec * 1000));
        }
    }

    private async Task<List<SightFullInfo>> LoadSightDetailsBatchSafe(List<int> pageIds)
    {
        int retry = 3;
        int retryDelayMs = 2000;
        while (retry-- > 0)
        {
            try
            {
                return await _sightService.LoadSightDetailsBatchAsync(pageIds);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SightsUpdater] Batch load failed, retry in {retryDelayMs / 1000}s: {e.Message}");
                await Task.Delay(retryDelayMs);
                retryDelayMs *= 2;
            }
        }
        return null;
    }

    private async Task LoadSightDetailsBatchedAsync(List<int> pageIds)
    {
        for (int i = 0; i < pageIds.Count; i += DetailsBatchSize)
        {
            if (!_isRunning) break;

            var chunk = pageIds.Skip(i).Take(DetailsBatchSize).ToList();
            var results = await LoadSightDetailsBatchSafe(chunk);

            if (results == null)
            {
                Debug.LogWarning("[SightsUpdater] Chunk failed after retries, stopping batch loading for this cycle");
                break;
            }

            foreach (var sight in results)
            {
                if (sight != null)
                    CachedSights[sight.PageId] = sight;
            }

            bool isLastChunk = i + DetailsBatchSize >= pageIds.Count;
            if (!isLastChunk)
                await Task.Delay(DetailsBatchDelayMs);
        }
    }

    private async Task UpdateSights()
    {
        Vector2 coords = await WaitForValidCoordinates();
        
        var nearestList = await _sightService.LoadNearestSightsAsync(coords, 5000);
         
        CachedNearestSights = nearestList
            .Where(s => s != null)
            .ToDictionary(s => s.PageId, s => s);

        PushToSpawnOnMap(CachedNearestSights.Values.ToArray());
        _spawnOnMap.SpawnObjects();
        
        CachedSights ??= new Dictionary<int, SightFullInfo>();
        
        var pageIdsToLoad = CachedNearestSights.Keys
            .Where(pageId => !CachedSights.ContainsKey(pageId))
            .ToList();
        
        if (pageIdsToLoad.Count > 0)
        {
            await LoadSightDetailsBatchedAsync(pageIdsToLoad);
        }
        
        await LoadAllImages();
        var (s, message) = await _apiService.GetSightsList(_userData.ID);
        _userData.SetSights(_apiService.ParseExternalIds(message));
        OnSightsUpdated?.Invoke();
    }

    private async Task UpdatePartnerStores(Vector2 coords)
    {
        var (success, response) = await _apiService.GetNearbyPartnerStores(coords.y, coords.x);
        if (!success || response?.stores == null)
        {
            Debug.LogWarning(
                $"[SightsUpdater] Partner stores nearby skipped: success={success}, stores null={(response?.stores == null)}, lat={coords.y} lon={coords.x}");
            return;
        }

        CachedNearestPartnerStores = response.stores
            .Where(store => store != null)
            .ToDictionary(store => store.id, store => store);

        PushPartnerStoresToSpawnOnMap(CachedNearestPartnerStores.Values.ToArray());
        PushToSpawnOnMap(CachedNearestSights?.Values.ToArray() ?? Array.Empty<SightShortInfo>());
        _spawnOnMap.SpawnObjects();

        await SyncPartnerStoreMarksFromServer();
        OnPartnerStoresUpdated?.Invoke();
    }




    private async Task LoadAllImages()
    {
        if (_isLoadingImages) return;
        _isLoadingImages = true;

        const int imageBatchSize = 3;
        const int imageBatchDelayMs = 1500;

        try
        {
            var toLoad = CachedSights.Values
                .Where(s => !string.IsNullOrEmpty(s.OriginalImageUrl) && !ImageCache.ContainsKey(s.PageId))
                .ToList();

            for (int i = 0; i < toLoad.Count; i += imageBatchSize)
            {
                if (!_isRunning) break;

                var batch = toLoad.Skip(i).Take(imageBatchSize).ToList();

                var tasks = batch.Select(async sight =>
                {
                    var url = FixUrl(sight.OriginalImageUrl);
                    var sprite = await LoadSpriteAsync(url);
                    if (sprite != null)
                    {
                        ImageCache[sight.PageId] = sprite;
                        OnImageLoaded?.Invoke(sight.PageId, sprite);
                    }
                });

                await Task.WhenAll(tasks);

                bool isLastBatch = i + imageBatchSize >= toLoad.Count;
                if (!isLastBatch)
                    await Task.Delay(imageBatchDelayMs);
            }
        }
        finally
        {
            _isLoadingImages = false;
        }
    }

    private float GetDistanceToSight(int pageId)
    {
        if (CachedNearestSights == null || !CachedNearestSights.TryGetValue(pageId, out var sight))
            return float.MaxValue;
        Vector2 myCoords = _locationService.GetCoordinates();
        return GeoDistanceMeters((float)sight.Latitude, (float)sight.Longitude, myCoords.y, myCoords.x);
    }

    private static float GeoDistanceMeters(float lat1, float lon1, float lat2, float lon2)
    {
        const float R = 6371000f;
        float dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        float dLon = (lon2 - lon1) * Mathf.Deg2Rad;
        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                  Mathf.Cos(lat1 * Mathf.Deg2Rad) * Mathf.Cos(lat2 * Mathf.Deg2Rad) * Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return R * c;
    }

    private string FixUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        url = url.Trim();

        if (url.StartsWith("//"))
            url = "https:" + url;

        return url.Replace(" ", "%20");
    }

    private void PushToSpawnOnMap(SightShortInfo[] shortInfos)
    {
        List<string> coords = new List<string>();
        List<int> pageIds = new List<int>();
        foreach (var sightInfo in shortInfos)
        {

            string lat = sightInfo.Latitude.ToString();
            string lon = sightInfo.Longitude.ToString();
        
            lat = lat.Replace(',', '.');
            lon = lon.Replace(',', '.');
        
            string coordString = $"{lat},{lon}";
            pageIds.Add(sightInfo.PageId);
            coords.Add(coordString);
//            Debug.Log(coordString);
        }
    
        string[] finalCoords = coords.ToArray();
        int[] finalPageIds = pageIds.ToArray();
        _spawnOnMap._locationStrings = finalCoords;
        _spawnOnMap.pageIds = finalPageIds;
    }

    private void PushPartnerStoresToSpawnOnMap(APIService.PartnerStoreInfo[] storeInfos)
    {
        List<string> coords = new List<string>();
        List<int> storeIds = new List<int>();

        foreach (var storeInfo in storeInfos)
        {
            string lat = storeInfo.latitude.ToString(CultureInfo.InvariantCulture);
            string lon = storeInfo.longitude.ToString(CultureInfo.InvariantCulture);
            coords.Add($"{lat},{lon}");
            storeIds.Add(storeInfo.id);
        }

        _spawnOnMap.partnerStoreLocationStrings = coords.ToArray();
        _spawnOnMap.partnerStoreIds = storeIds.ToArray();
    }

    private async Task<Sprite> LoadSpriteAsync(string url)
    {
        int retryDelayMs = 10000;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            using (var req = UnityWebRequestTexture.GetTexture(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var tex = DownloadHandlerTexture.GetContent(req);
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                }

                if (req.responseCode == 429)
                {
                    Debug.LogWarning($"[SightsUpdater] Image rate limited (429), waiting {retryDelayMs / 1000}s before retry");
                    await Task.Delay(retryDelayMs);
                    retryDelayMs *= 2;
                }
                else
                {
                    Debug.LogError("Image load failed: " + req.error);
                    return null;
                }
            }
        }

        Debug.LogError($"[SightsUpdater] Image load failed after retries: {url}");
        return null;
    }
    
    private async Task<Vector2> WaitForValidCoordinates()
    {
        Vector2 coords = _locationService.GetCoordinates();
        
        while (coords == Vector2.zero)
        {
            await Task.Delay(200);
            coords = _locationService.GetCoordinates();
        }

        return coords;
    }

    private async void HandleCheckInButtonClicked(int pageId)
    {
        var (success, response) = await _apiService.SetSightMarked(pageId);
        if (success)
        {
            _userData.AddSightToMarked(pageId);

            if (response?.resources_gained != null)
            {
                await RefreshInventoryAndShowReward(response.resources_gained);
            }
        }
    }

    public async void HandlePartnerStoreClicked(int storeId)
    {
        if (!_userData.IsPartnerStoreUnmarked(storeId))
            return;

        var (success, response) = await _apiService.SavePartnerStores(new[] { storeId });
        if (!success || response == null)
            return;

        if (response.saved_store_ids != null)
        {
            foreach (var id in response.saved_store_ids)
                _userData.AddPartnerStoreToMarked(id);
        }

        if (response.resources_gained != null)
            await RefreshInventoryAndShowReward(response.resources_gained);

        await SyncPartnerStoreMarksFromServer();
        OnPartnerStoresUpdated?.Invoke();
    }

    private async Task SyncPartnerStoreMarksFromServer()
    {
        var (success, response) = await _apiService.GetPlayerPartnerStores(_userData.ID);
        if (!success || response == null)
            return;

        _userData.SetPartnerStoreIds(response.store_ids ?? Array.Empty<int>());
    }

    private async Task RefreshInventoryAndShowReward(APIService.ResourcesGained resourcesGained)
    {
        var (invSuccess, invResponse) = await _apiService.GetInventory();
        if (invSuccess && invResponse?.resources != null)
            _inventoryService.SetResources(invResponse.resources);

        var parts = new List<string>();
        if (resourcesGained.metal > 0) parts.Add($"+{resourcesGained.metal} металл");
        if (resourcesGained.wood > 0) parts.Add($"+{resourcesGained.wood} дерево");
        if (resourcesGained.blueprints > 0) parts.Add($"+{resourcesGained.blueprints} чертежи");

        if (parts.Count > 0)
            _popupService.ShowSuccess("Вы получили: " + string.Join(", ", parts));
    }

    private async void HandleCaptureButtonClicked(int pageId)
    {
        // Захват достопримечательности (новая серверная логика)
        var (captureSuccess, captureResponse) = await _apiService.CaptureLandmark(pageId);

        if (captureSuccess && captureResponse != null)
        {
            var message = !string.IsNullOrEmpty(captureResponse.message)
                ? captureResponse.message
                : "Достопримечательность захвачена!";
            _popupService.ShowSuccess(message);

            await RefreshCaptureInfoInPopup(pageId, captureResponse.capture);
        }
        // Если захват не удался, сообщение об ошибке уже показано внутри APIService (SendRequest)
    }

    private async Task RefreshCaptureInfoInPopup(int pageId, APIService.LandmarkCaptureRecord captureFromResponse = null)
    {
        var popups = GameObject.FindObjectsOfType<SightDetailsView>();
        SightDetailsView targetPopup = null;

        foreach (var popup in popups)
        {
            if (popup.SightID == pageId)
            {
                targetPopup = popup;
                break;
            }
        }

        if (targetPopup != null)
        {
            if (captureFromResponse != null)
            {
                string capturedBy = captureFromResponse.captured_by != null ? captureFromResponse.captured_by.username : "";
                string clanName = captureFromResponse.clan != null ? captureFromResponse.clan.name : "";
                targetPopup.SetCaptureInfo(true, capturedBy, captureFromResponse.captured_at, clanName);
                targetPopup.SetCaptureButtonState(false);
            }

            await LoadAndDisplayCaptureInfo(targetPopup, pageId, captureFromResponse);
        }
    }
    

    public bool TryGetImage(int pageId, out Sprite sprite)
        => ImageCache.TryGetValue(pageId, out sprite);

    public async void CreatePartnerStoreDetailsPopup(int storeId)
    {
        Transform canvasTransform = GameObject.FindGameObjectWithTag("Canvas").transform;
        if (canvasTransform.childCount > 1)
        {
            Debug.LogError("[SightsUpdater] Canvas child count is more than 1");
            return;
        }

        if (CachedNearestPartnerStores == null || !CachedNearestPartnerStores.TryGetValue(storeId, out var store))
        {
            Debug.LogError($"[SightsUpdater] Partner store {storeId} not found in cache");
            return;
        }

        var popup = _partnerStoreDetailsViewFactory.Create();
        popup.transform.SetParent(canvasTransform, false);

        Sprite sprite = null;
        if (!PartnerStoreImageCache.TryGetValue(storeId, out sprite) && !string.IsNullOrEmpty(store.image_url))
        {
            var url = FixUrl(store.image_url);
            sprite = await LoadSpriteAsync(url);
            if (sprite != null)
                PartnerStoreImageCache[storeId] = sprite;
        }
        if (sprite == null)
            sprite = Resources.Load<Sprite>("no_image");

        bool visited = !_userData.IsPartnerStoreUnmarked(storeId);
        popup.Init(sprite, store.name, store.address, storeId);
        popup.SetVisitStatus(visited);
        popup.SetVisitButtonState(!visited);

        if (!visited)
            popup.OnVisitClicked += HandlePartnerStoreVisit;
    }

    private async void HandlePartnerStoreVisit(int storeId)
    {
        var (success, response) = await _apiService.SavePartnerStores(new[] { storeId });
        if (!success || response == null)
            return;

        if (response.saved_store_ids != null)
        {
            foreach (var id in response.saved_store_ids)
                _userData.AddPartnerStoreToMarked(id);
        }

        if (response.resources_gained != null)
            await RefreshInventoryAndShowReward(response.resources_gained);

        await SyncPartnerStoreMarksFromServer();
        OnPartnerStoresUpdated?.Invoke();
    }
}
