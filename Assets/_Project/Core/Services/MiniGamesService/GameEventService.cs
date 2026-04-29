using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapbox.Examples;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;
using Zenject;

public class GameEventService : IInitializable, IDisposable, ITickable
{
    private readonly LocationService _locationService;
    private readonly AbstractMap _map;
    private readonly GameEventView.Factory _gameEventViewFactory;
    private readonly MiniGamesService _miniGamesService;
    private readonly DiContainer _container;
    
    private readonly float _spawnInterval = 5f;
    private readonly float _minSpawnRadius = 50f;
    private readonly float _maxSpawnRadius = 200f;
    private readonly float _eventLifetime = 600f;
    private readonly int _maxActiveEvents = 5;
    private readonly float _collisionCheckRadius = 10f;
    
    private bool _isRunning;
    private float _nextSpawnTime;
    private Dictionary<string, GameEventData> _activeEvents = new Dictionary<string, GameEventData>();
    private Dictionary<string, GameObject> _spawnedObjects = new Dictionary<string, GameObject>();
    
    public event Action<string, GameEventResult> OnGameCompleted;

    private GameObject _gameEventMarkerPrefab;

    public GameEventService(
        LocationService locationService,
        AbstractMap map,
        GameEventView.Factory gameEventViewFactory,
        MiniGamesService miniGamesService,
        DiContainer container,
        [Inject(Id = "GameEventMarker")] GameObject gameEventMarkerPrefab)
    {
        _locationService = locationService;
        _map = map;
        _gameEventViewFactory = gameEventViewFactory;
        _miniGamesService = miniGamesService;
        _container = container;
        _gameEventMarkerPrefab = gameEventMarkerPrefab;
    }

    public void Initialize()
    {
        _isRunning = true;
        _nextSpawnTime = Time.time + _spawnInterval;
        RunUpdateLoop();
    }

    public void Dispose()
    {
        _isRunning = false;
        CleanupAllEvents();
    }

    private async void RunUpdateLoop()
    {
        while (_isRunning)
        {
            await Task.Delay(1000);
            
            if (!_isRunning) break;
            
            UpdateEvents();
            
            if (Time.time >= _nextSpawnTime && _activeEvents.Count < _maxActiveEvents)
            {
                await TrySpawnEvent();
                _nextSpawnTime = Time.time + _spawnInterval;
            }
        }
    }

    private void UpdateEvents()
    {
        var eventsToRemove = new List<string>();
        
        foreach (var kvp in _activeEvents)
        {
            var eventData = kvp.Value;
            
            if (Time.time - eventData.SpawnTime > eventData.Lifetime)
            {
                eventsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var eventId in eventsToRemove)
        {
            RemoveEvent(eventId);
        }
    }

    private async Task TrySpawnEvent()
    {
        Vector2 playerCoords = await WaitForValidCoordinates();
        
        if (playerCoords == Vector2.zero)
            return;
        
        Vector2d spawnCoords = GenerateRandomSpawnPosition(playerCoords);
        Vector3 worldPosition = _map.GeoToWorldPosition(spawnCoords, true);
        
        if (!IsValidSpawnPosition(worldPosition))
        {
            return;
        }
        
        SpawnEvent(spawnCoords);
    }

    private Vector2d GenerateRandomSpawnPosition(Vector2 playerCoords)
    {
        float lon = playerCoords.x;
        float lat = playerCoords.y;
        
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = UnityEngine.Random.Range(_minSpawnRadius, _maxSpawnRadius);
        
        float latOffset = distance * Mathf.Cos(angle) / 111000f;
        float lonOffset = distance * Mathf.Sin(angle) / (111000f * Mathf.Cos(lat * Mathf.Deg2Rad));
        
        return new Vector2d(lat + latOffset, lon + lonOffset);
    }

    private bool IsValidSpawnPosition(Vector3 worldPosition)
    {
        Collider[] colliders = Physics.OverlapSphere(worldPosition, _collisionCheckRadius);
        
        foreach (var collider in colliders)
        {
            if (collider.GetComponent<SightObject>() != null || 
                collider.GetComponent<GameEventObject>() != null)
            {
                return false;
            }
        }
        
        return true;
    }

    private void SpawnEvent(Vector2d coordinates)
    {
        string eventId = Guid.NewGuid().ToString();
        GameEventType eventType = GetRandomEventType();
        
        var eventData = new GameEventData
        {
            EventId = eventId,
            EventType = eventType,
            Coordinates = new Vector2((float)coordinates.y, (float)coordinates.x),
            SpawnTime = Time.time,
            Lifetime = _eventLifetime,
            IsActive = true
        };
        
        _activeEvents[eventId] = eventData;
        
        SpawnEventObject(eventData, coordinates);
    }

    private static readonly GameEventType[] _availableGameTypes =
    {
        GameEventType.Jump,
        GameEventType.TrainPath,
        GameEventType.FlappyBird
    };

    private GameEventType GetRandomEventType()
    {
        return _availableGameTypes[UnityEngine.Random.Range(0, _availableGameTypes.Length)];
    }

    private void SpawnEventObject(GameEventData eventData, Vector2d coordinates)
    {
        if (_gameEventMarkerPrefab == null)
        {
            Debug.LogError("[GameEventService] GameEventMarker prefab не назначен в OtherInstaller!");
            return;
        }
        
        Vector3 worldPosition = _map.GeoToWorldPosition(coordinates, true);
        
        GameObject instance = UnityEngine.Object.Instantiate(_gameEventMarkerPrefab);
        instance.transform.SetParent(_map.transform, false);
        instance.transform.localPosition = worldPosition;
        instance.transform.localScale = Vector3.one;
        
        GameEventObject eventObject = instance.GetComponent<GameEventObject>();
        if (eventObject == null)
        {
            eventObject = instance.AddComponent<GameEventObject>();
        }
        
        eventObject.SetEventData(eventData.EventId, eventData.EventType);
        
        _spawnedObjects[eventData.EventId] = instance;
    }

    public void Tick()
    {
        if (_spawnedObjects == null || _spawnedObjects.Count == 0)
            return;
        
        foreach (var kvp in _spawnedObjects.ToList())
        {
            UpdateEventObjectPosition(kvp.Key);
        }
    }

    private void UpdateEventObjectPosition(string eventId)
    {
        if (!_spawnedObjects.ContainsKey(eventId) || !_activeEvents.ContainsKey(eventId))
            return;
        
        var eventData = _activeEvents[eventId];
        var obj = _spawnedObjects[eventId];
        
        if (obj != null)
        {
            Vector2d coords = new Vector2d(eventData.Coordinates.y, eventData.Coordinates.x);
            Vector3 worldPosition = _map.GeoToWorldPosition(coords, true);
            obj.transform.localPosition = worldPosition;
        }
    }

    public void HandleEventClicked(string eventId)
    {
        Debug.Log ("HandleEventClicked: " + eventId);
        if (!_activeEvents.ContainsKey(eventId))
            return;
        
        var eventData = _activeEvents[eventId];
        OpenGameEventView(eventData);
    }

    private void OpenGameEventView(GameEventData eventData)
    {

        var canvas = GameObject.FindGameObjectWithTag("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[GameEventService] Canvas с тегом 'Canvas' не найден!");
            return;
        }
        else if (canvas.transform.childCount > 1)
        {
            Debug.LogError("[GameEventService] Уже открыто другое окно" + canvas.GetComponentsInChildren<RectTransform>().Length);
            return;
        }
        
        IGameEvent game = _miniGamesService.CreateGameInstance(eventData.EventType);
        
        if (game == null)
        {
            Debug.LogWarning($"Failed to create game instance for type: {eventData.EventType}");
            return;
        }
        
        var view = _gameEventViewFactory.Create();
        
        view.transform.SetParent(canvas.transform, false);
        
        view.OnGameFinished += HandleGameFinished;
        view.Initialize(eventData.EventId, game);
    }

    private void HandleGameFinished(string eventId, GameEventResult result)
    {
        result.EventId = eventId;
        OnGameCompleted?.Invoke(eventId, result);
        if (result.IsSuccess)
        {
            RemoveEvent(eventId);
        }
    }

    private void RemoveEvent(string eventId)
    {
        if (_spawnedObjects.ContainsKey(eventId))
        {
            var obj = _spawnedObjects[eventId];
            if (obj != null)
            {
                UnityEngine.Object.Destroy(obj);
            }
            _spawnedObjects.Remove(eventId);
        }
        
        _activeEvents.Remove(eventId);
    }

    private void CleanupAllEvents()
    {
        var eventIds = _activeEvents.Keys.ToList();
        foreach (var eventId in eventIds)
        {
            RemoveEvent(eventId);
        }
    }

    private async Task<Vector2> WaitForValidCoordinates()
    {
        Vector2 coords = _locationService.GetCoordinates();
        
        int attempts = 0;
        while (coords == Vector2.zero && attempts < 10)
        {
            await Task.Delay(200);
            coords = _locationService.GetCoordinates();
            attempts++;
        }
        
        return coords;
    }
}



