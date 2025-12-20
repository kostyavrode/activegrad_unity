using System.Collections;
using System.Threading.Tasks;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

public class MapService : ITickable, IInitializable
{
    private readonly string _apiKey;
    private readonly AbstractMap _map;
    private readonly LocationService _locationService;
    
    private Vector2 _lastCoordinates = Vector2.zero;
    private bool _isMapInitialized = false;
    private const float COORDINATE_THRESHOLD = 0.0001f;
    
    public MapService(string apiKey, AbstractMap map, LocationService locationService)
    {
        _apiKey = apiKey;
        _map = map;
        _locationService = locationService;
        
        if (_map != null)
        {
            Debug.Log($"[MapService] Constructor: disabling auto-initialization. Current InitializeOnStart: {_map.InitializeOnStart}");
            _map.InitializeOnStart = false;
            Debug.Log($"[MapService] Constructor: InitializeOnStart set to: {_map.InitializeOnStart}");
        }
    }
    
    public void Initialize()
    {
        Debug.Log($"[MapService] Initialize called. Current InitializeOnStart: {_map.InitializeOnStart}");
        if (_map != null)
        {
            _map.InitializeOnStart = false;
        }
    }

    public async Task<Texture2D> LoadMap(string longitude, string latitude, int zoom = 18, int size = 450)
    {
        string url = $"https://static-maps.yandex.ru/v1?ll={longitude},{latitude}&lang=ru_RU&size={size},{size}&z={zoom}&apikey={_apiKey}";

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            var op = www.SendWebRequest();
            Debug.Log(url);
            while (!op.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Ошибка загрузки карты: " + www.error + " | "+www.url);
                return null;
            }
            else
            {
                return DownloadHandlerTexture.GetContent(www);
            }
        }
    }

    public void Tick()
    {
        var coords = _locationService.GetCoordinates();
        
        if (coords == Vector2.zero)
        {
            Debug.LogWarning("[MapService] Coordinates are zero, skipping update");
            return;
        }
        
        if (!_isMapInitialized)
        {
            int zoom = (int)_map.Options.locationOptions.zoom;
            if (zoom <= 0) zoom = 15;
            Debug.Log($"[MapService] Initializing map with coordinates: lat={coords.y}, lon={coords.x}, zoom={zoom}");
            _map.Initialize(new Vector2d((double)coords.y, (double)coords.x), zoom);
            _lastCoordinates = coords;
            _isMapInitialized = true;
            Debug.Log($"[MapService] Map initialized successfully");
            return;
        }
        
        float distance = Vector2.Distance(coords, _lastCoordinates);
        bool coordinatesChanged = distance > COORDINATE_THRESHOLD;
        
        if (coordinatesChanged)
        {
            _lastCoordinates = coords;
            Debug.Log($"[MapService] Updating map to coordinates: lat={coords.y}, lon={coords.x}");
            _map.UpdateMap(new Vector2d((double)coords.y, (double)coords.x));
            Debug.Log($"[MapService] Map updated successfully");
        }
    }
}