using System.Collections;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;
using Zenject;

public class SmoothMapMovementService : IInitializable, ITickable
{
    private readonly AbstractMap _map;
    private readonly LocationService _locationService;
    private readonly CoroutineRunner _coroutineRunner;
    
    private bool _isMapInitialized = false;
    private bool _isLerping = false;
    private double _lastLongitude;
    private double _lastLatitude;
    private bool _lastCoordinatesInitialized = false;
    private const float COORDINATE_THRESHOLD = 0.00001f;
    
    private Vector2d _startLatLong;
    private Vector2d _endLatLong;
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private float _timeStartedLerping;
    private float _lerpDuration = 1f;
    private const float MIN_LERP_DURATION = 0.3f;
    private const float MAX_LERP_DURATION = 2f;
    private const float LERP_SPEED = 0.00005f;
    
    private Coroutine _lerpCoroutine;
    private Vector3 _currentMapDirection = Vector3.zero;
    private bool _realtimeFollow;
    private float _realtimeFollowUntil;
    private const float RealtimeFollowMaxStep = 0.001f;
    private const float RealtimeFollowStopDelay = 0.15f;
    
    public SmoothMapMovementService(AbstractMap map, LocationService locationService, CoroutineRunner coroutineRunner)
    {
        _map = map;
        _locationService = locationService;
        _coroutineRunner = coroutineRunner;
    }
    
    public void Initialize()
    {
        if (_map != null)
        {
            _map.InitializeOnStart = false;
        }
    }
    
    public void Tick()
    {
        _locationService.GetCoordinatesPrecise(out double longitude, out double latitude);

        if (longitude == 0 && latitude == 0)
        {
            return;
        }

        if (!_isMapInitialized)
        {
            int zoom = (int)_map.Options.locationOptions.zoom;
            if (zoom <= 0) zoom = 15;

            _map.Initialize(new Vector2d(latitude, longitude), zoom);
            _lastLongitude = longitude;
            _lastLatitude = latitude;
            _lastCoordinatesInitialized = true;
            _isMapInitialized = true;
            Debug.Log($"[SmoothMapMovement] Map initialized with coordinates: Lat={latitude}, Lon={longitude}");
            return;
        }

        if (!_lastCoordinatesInitialized)
        {
            _lastLongitude = longitude;
            _lastLatitude = latitude;
            _lastCoordinatesInitialized = true;
            Debug.Log($"[SmoothMapMovement] Coordinates initialized: Lat={latitude}, Lon={longitude}");
            return;
        }

        double dx = longitude - _lastLongitude;
        double dy = latitude - _lastLatitude;
        double distance = System.Math.Sqrt(dx * dx + dy * dy);

#if UNITY_EDITOR || UNITY_STANDALONE
        bool coordinatesChanged = distance > 1e-12;
#else
        bool coordinatesChanged = distance > COORDINATE_THRESHOLD;
#endif

        if (coordinatesChanged)
        {
            if (distance <= RealtimeFollowMaxStep)
            {
                FollowInRealtime(longitude, latitude);
            }
            else if (_isLerping && !_realtimeFollow)
            {
                _startLatLong = _map.CenterLatitudeLongitude;
                _startPosition = _map.GeoToWorldPosition(_startLatLong, false);
                _endLatLong = new Vector2d(latitude, longitude);
                _endPosition = _map.GeoToWorldPosition(_endLatLong, false);
                _currentMapDirection = (_endPosition - _startPosition).normalized;
                _timeStartedLerping = Time.time;
                float newDistance = Vector3.Distance(_startPosition, _endPosition);
                _lerpDuration = Mathf.Clamp(newDistance / LERP_SPEED, MIN_LERP_DURATION, MAX_LERP_DURATION);
            }
            else
            {
                StartLerping(longitude, latitude);
            }
            _lastLongitude = longitude;
            _lastLatitude = latitude;
        }
        else if (_realtimeFollow && Time.time >= _realtimeFollowUntil)
        {
            _realtimeFollow = false;
            _isLerping = false;
            _currentMapDirection = Vector3.zero;
        }
    }

    private void FollowInRealtime(double longitude, double latitude)
    {
        if (_lerpCoroutine != null)
        {
            _coroutineRunner.StopCoroutine(_lerpCoroutine);
            _lerpCoroutine = null;
        }

        var target = new Vector2d(latitude, longitude);
        var from = _map.GeoToWorldPosition(_map.CenterLatitudeLongitude, false);
        var to = _map.GeoToWorldPosition(target, false);
        var dir = to - from;
        if (dir.sqrMagnitude > 0.0000001f)
            _currentMapDirection = dir.normalized;

        _map.UpdateMap(target, _map.Zoom);
        _realtimeFollow = true;
        _isLerping = true;
        _realtimeFollowUntil = Time.time + RealtimeFollowStopDelay;
    }

    private void StartLerping(double longitude, double latitude)
    {
        if (_lerpCoroutine != null)
        {
            _coroutineRunner.StopCoroutine(_lerpCoroutine);
        }

        _isLerping = true;
        _realtimeFollow = false;
        _timeStartedLerping = Time.time;

        _startLatLong = _map.CenterLatitudeLongitude;
        _endLatLong = new Vector2d(latitude, longitude);

        _startPosition = _map.GeoToWorldPosition(_startLatLong, false);
        _endPosition = _map.GeoToWorldPosition(_endLatLong, false);

        _currentMapDirection = (_endPosition - _startPosition).normalized;

        float distance = Vector3.Distance(_startPosition, _endPosition);
        _lerpDuration = Mathf.Clamp(distance / LERP_SPEED, MIN_LERP_DURATION, MAX_LERP_DURATION);

        _lerpCoroutine = _coroutineRunner.StartCoroutine(LerpMapCoroutine());
    }
    
    private IEnumerator LerpMapCoroutine()
    {
        
        int frameCount = 0;
        while (_isLerping)
        {
            float timeSinceStarted = Time.time - _timeStartedLerping;
            float percentageComplete = timeSinceStarted / _lerpDuration;
            
            _startPosition = _map.GeoToWorldPosition(_startLatLong, false);
            _endPosition = _map.GeoToWorldPosition(_endLatLong, false);
            
            var position = Vector3.Lerp(_startPosition, _endPosition, percentageComplete);
            var latLong = _map.WorldToGeoPosition(position);
            _map.UpdateMap(latLong, _map.Zoom);
            
            _currentMapDirection = (_endPosition - _startPosition).normalized;
            
            if (frameCount % 30 == 0)
            {
                //Debug.Log($"[SmoothMapMovement] Lerping: percentage={percentageComplete:F2}, latLong=Lat={latLong.x}, Lon={latLong.y}");
            }
            frameCount++;
            
            if (percentageComplete >= 1.0f)
            {
                _isLerping = false;
                break;
            }
            
            yield return null;
        }

        _lerpCoroutine = null;
    }
    
    public Vector3 GetCurrentMapDirection()
    {
        return _currentMapDirection;
    }
    
    public bool IsLerping()
    {
        return _isLerping;
    }
}

