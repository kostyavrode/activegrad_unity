using System;
using System.Collections.Generic;
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

    public Dictionary<int, Sprite> ImageCache { get; private set; } = new Dictionary<int, Sprite>();
    
    public event Action<int, Sprite> OnImageLoaded;

    private readonly ISightService _sightService;
    private readonly LocationService _locationService;
    private readonly SightDetailsView.Factory _sightDetailsViewFactory;
    private readonly IPopupService _popupService;
    private readonly SpawnOnMap _spawnOnMap;
    private readonly APIService _apiService;
    private readonly UserDataService _userData;

    private bool _isRunning;
    private readonly float _interval = 5f;

    public SightsUpdater(ISightService sightService, LocationService locationService, SightDetailsView.Factory sightDetailsViewFactory, IPopupService popupService, SpawnOnMap spawnOnMap, 
        APIService apiService, UserDataService userDataService)
    {
        _sightService = sightService;
        _locationService = locationService;
        _sightDetailsViewFactory = sightDetailsViewFactory;
        _popupService = popupService;
        _spawnOnMap = spawnOnMap;
        _apiService = apiService;
        _userData = userDataService;
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
        Sprite sprite;
        try
        {
            sprite = ImageCache[pageID];
        }
        catch (Exception e)
        {
            _popupService.ShowError(e.Message);
            sprite = Resources.Load<Sprite>("no_image");
        }
        
        popup.Init(sprite, CachedSights[pageID].Title, CachedSights[pageID].Description,
            CachedSights[pageID].PageId);

        popup.OnCaptureClicked += HandleCaptureButtonClicked;

        await LoadAndDisplayCaptureInfo(popup, pageID);

        if (CachedNearestSights[pageID].Distance <= 600 && _userData.CheckSight(pageID))
        {
            popup.SetCheckInButtonState(true);
            popup.OnCheckInClicked += HandleCheckInButtonClicked;
        }
    }

    private async Task LoadAndDisplayCaptureInfo(SightDetailsView popup, int pageID)
    {
        var (success, response) = await _apiService.GetLandmarkCaptureInfo(pageID);
        
        if (success && response != null)
        {
            string capturedBy = response.captured_by != null ? response.captured_by.username : "";
            string clanName = response.clan != null ? response.clan.name : "";
            
            popup.SetCaptureInfo(response.captured, capturedBy, response.captured_at, clanName);
            if (CachedNearestSights[pageID].Distance <= 600)
            {
                popup.SetCaptureButtonState(response.can_capture_now);
            }
            else
            {
                popup.SetCaptureButtonState(false);
            }
        }
        else
        {
            popup.SetCaptureInfo(false, "", "", "");
            popup.SetCaptureButtonState(true);
        }
    }

    private async void RunUpdateLoop()
    {
        while (_isRunning)
        {
            /*try
            {
                await UpdateSights();
            }
            catch (Exception e)
            {
                Debug.LogError("UpdateSights FAILED: " + e.Message);
            }*/
            
            await UpdateSights();
            await Task.Delay((int)(_interval * 1000));
        }
    }

    private async Task<SightFullInfo> LoadSafeSight(int pageId)
    {
        int retry = 3;

        while (retry-- > 0)
        {
            try
            {
                return await _sightService.LoadSightDetailsAsync(pageId);
            }
            catch { await Task.Delay(500); }
        }

        return null;
    }

    private async Task UpdateSights()
    {
        Vector2 coords = await WaitForValidCoordinates();
        
        var nearestList = await _sightService.LoadNearestSightsAsync(coords, 5000);

        var spawnData = nearestList.ToArray();
        
         PushToSpawnOnMap(spawnData);
         
        CachedNearestSights = nearestList
            .Where(s => s != null)
            .ToDictionary(s => s.PageId, s => s);
        
        var tasks = CachedNearestSights.Keys
            .Select(pageId => LoadSafeSight(pageId))
            .ToList();

        var results = await Task.WhenAll(tasks);
        
        CachedSights = results
            .Where(r => r != null)
            .ToDictionary(r => r.PageId, r => r);
        
        

        await LoadAllImages();
        //await _apiService.GetSightsList(1);
        var (s, message) = await _apiService.GetSightsList(_userData.ID);
        _userData.SetSights(_apiService.ParseExternalIds(message));
        //_userData.SetSights(message.ToArray());
    }




    private async Task LoadAllImages()
    {
        foreach (var pair in CachedSights)
        {
            int pageId = pair.Key;
            var sight = pair.Value;

            if (string.IsNullOrEmpty(sight.OriginalImageUrl))
                continue;

            if (ImageCache.ContainsKey(pageId))
                continue;

            var url = FixUrl(sight.OriginalImageUrl);
            var sprite = await LoadSpriteAsync(url);

            if (sprite != null)
            {
                ImageCache[pageId] = sprite;
                OnImageLoaded?.Invoke(pageId, sprite);
            }
        }
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
        _spawnOnMap.SpawnObjects();
    }

    private async Task<Sprite> LoadSpriteAsync(string url)
    {
        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Image load failed: " + req.error);
                return null;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
        }
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
        // Старый функционал "отметиться" (для квестов и статистики)
        await _apiService.SetSightMarked(pageId);
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

            // Обновляем информацию о захвате в попапе
            await RefreshCaptureInfoInPopup(pageId);
        }
        // Если захват не удался, сообщение об ошибке уже показано внутри APIService (SendRequest)
    }

    private async Task RefreshCaptureInfoInPopup(int pageId)
    {
        // Находим открытый попап для этой достопримечательности
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
            // Загружаем обновленную информацию о захвате
            await LoadAndDisplayCaptureInfo(targetPopup, pageId);
        }
    }
    

    public bool TryGetImage(int pageId, out Sprite sprite)
        => ImageCache.TryGetValue(pageId, out sprite);
}
