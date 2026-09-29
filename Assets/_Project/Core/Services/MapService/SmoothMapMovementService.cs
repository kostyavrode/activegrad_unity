using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;
using Zenject;

/// <summary>
/// Плавно ведёт центр карты за координатами игрока.
/// Центр каждый кадр подтягивается к последней точке GPS, поэтому редкие обновления с телефона
/// (раз в ~1 с, скачками в несколько метров) превращаются в ровное движение, а не в «телепорт».
/// </summary>
public class SmoothMapMovementService : IInitializable, ITickable
{
    private readonly AbstractMap _map;
    private readonly LocationService _locationService;

    private bool _isMapInitialized = false;
    private double _lastLongitude;
    private double _lastLatitude;
    private bool _lastCoordinatesInitialized = false;
    private const float COORDINATE_THRESHOLD = 0.00001f;

    // Следование за целью
    private Vector2d _targetLatLong;
    private bool _hasTarget;
    private bool _isLerping;
    private float _walkUntil;
    private Vector3 _currentMapDirection = Vector3.zero;

    // Чем больше, тем быстрее карта догоняет новую точку (при обновлении GPS раз в секунду — почти без отставания)
    private const float FollowSharpness = 4f;
    // Минимальная скорость подтягивания, чтобы не «ползти» бесконечно у самой цели
    private const double MinFollowMetersPerSecond = 0.6;
    private const double ArriveMeters = 0.05;
    // Дальше этого — не анимируем, а сразу переносим (первый точный фикс GPS, возврат из фона)
    private const double SnapMeters = 300.0;
    // Ходьба не прерывается в паузах между обновлениями GPS: пауза подстраивается под их реальную частоту
    private const float MinWalkGraceSeconds = 0.3f;
    private const float MaxWalkGraceSeconds = 2.5f;
    private const float WalkGraceIntervalFactor = 1.5f;
    private float _lastTargetTime = -1f;
    private float _targetInterval = 1f;

    // Сглаженная интенсивность движения карты (только для визуала).
    private Vector2d _speedLastCenter;
    private bool _speedInitialized;
    private float _currentSpeed01;
    private const float SpeedFullMetersPerSecond = 2f;   // ~быстрый шаг => 1
    private const float SpeedSmoothing = 3f;
    private const double MetersPerDegree = 111320.0;

    /// <summary>Нормализованная (0..1) сглаженная скорость смещения центра карты. Обычный шаг ~0.6-1.</summary>
    public float CurrentSpeed01 => _currentSpeed01;

    public SmoothMapMovementService(AbstractMap map, LocationService locationService, CoroutineRunner coroutineRunner)
    {
        _map = map;
        _locationService = locationService;
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
            SetTarget(new Vector2d(latitude, longitude));
            _lastLongitude = longitude;
            _lastLatitude = latitude;
        }

        FollowTarget();
        UpdateSpeed();
    }

    private void SetTarget(Vector2d target)
    {
        var center = _map.CenterLatitudeLongitude;

        if (MetersBetween(center, target) > SnapMeters)
        {
            // Слишком далеко для анимации — переносим карту сразу, без ходьбы
            _map.UpdateMap(target, _map.Zoom);
            _hasTarget = false;
            _isLerping = false;
            _currentMapDirection = Vector3.zero;
            _speedInitialized = false;
            return;
        }

        float now = Time.time;
        if (_lastTargetTime >= 0f)
        {
            float interval = Mathf.Min(now - _lastTargetTime, MaxWalkGraceSeconds);
            _targetInterval = Mathf.Lerp(_targetInterval, interval, 0.3f);
        }
        _lastTargetTime = now;

        _targetLatLong = target;
        _hasTarget = true;
        _walkUntil = now + Mathf.Clamp(_targetInterval * WalkGraceIntervalFactor, MinWalkGraceSeconds, MaxWalkGraceSeconds);
    }

    private void FollowTarget()
    {
        if (!_hasTarget)
        {
            _isLerping = false;
            return;
        }

        var center = _map.CenterLatitudeLongitude;
        double remaining = MetersBetween(center, _targetLatLong);

        if (remaining > ArriveMeters)
        {
            float dt = Time.deltaTime;
            double step = System.Math.Max(remaining * (1.0 - System.Math.Exp(-FollowSharpness * dt)), MinFollowMetersPerSecond * dt);
            double t = System.Math.Min(1.0, step / remaining);

            var next = new Vector2d(
                center.x + (_targetLatLong.x - center.x) * t,
                center.y + (_targetLatLong.y - center.y) * t);

            var from = _map.GeoToWorldPosition(center, false);
            var to = _map.GeoToWorldPosition(_targetLatLong, false);
            var dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-10f)
                _currentMapDirection = dir.normalized;

            _map.UpdateMap(next, _map.Zoom);
        }
        else if (remaining > 0.0)
        {
            _map.UpdateMap(_targetLatLong, _map.Zoom);
        }

        bool stillMoving = remaining > ArriveMeters;
        _isLerping = stillMoving || Time.time < _walkUntil;

        if (!_isLerping)
        {
            _hasTarget = false;
            _currentMapDirection = Vector3.zero;
        }
    }

    private static double MetersBetween(Vector2d a, Vector2d b)
    {
        double dLat = (b.x - a.x) * MetersPerDegree;
        double dLon = (b.y - a.y) * MetersPerDegree * System.Math.Cos(a.x * System.Math.PI / 180.0);
        return System.Math.Sqrt(dLat * dLat + dLon * dLon);
    }

    private void UpdateSpeed()
    {
        if (!_isMapInitialized || _map == null)
            return;

        var center = _map.CenterLatitudeLongitude;
        if (!_speedInitialized)
        {
            _speedLastCenter = center;
            _speedInitialized = true;
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        float metersPerSecond = (float)(MetersBetween(_speedLastCenter, center) / dt);
        _speedLastCenter = center;

        float raw = Mathf.Clamp01(metersPerSecond / SpeedFullMetersPerSecond);
        if (float.IsNaN(raw))
            raw = 0f;

        float k = 1f - Mathf.Exp(-SpeedSmoothing * dt);
        _currentSpeed01 = Mathf.Lerp(_currentSpeed01, raw, k);
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
