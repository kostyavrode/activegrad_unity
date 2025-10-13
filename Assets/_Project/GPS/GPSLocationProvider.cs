using UnityEngine;

public class GPSLocationProvider : ILocationProvider
{
    private Vector2 _lastCoordinates = Vector2.zero;
    
    private readonly Vector2 _minCoords = new Vector2(55.70f, 37.60f); 
    private readonly Vector2 _maxCoords = new Vector2(55.80f, 37.70f);

    private float _updateInterval = 5f;
    private float _timer = 0f;

    public void Start()
    {
#if UNITY_ANDROID || UNITY_IOS
        Input.location.Start();
#else
        // стартовая точка
        _lastCoordinates = GetRandomCoords();
#endif
    }

    public void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.location.status == LocationServiceStatus.Running)
        {
            var data = Input.location.lastData;
            _lastCoordinates = new Vector2(data.latitude, data.longitude);
        }
#else
        _timer += Time.deltaTime;
        if (_timer >= _updateInterval)
        {
            _timer = 0f;
            _lastCoordinates = GetRandomCoords();
        }
#endif
    }

    public Vector2 GetCoordinates() => _lastCoordinates;

#if !UNITY_ANDROID && !UNITY_IOS
    private Vector2 GetRandomCoords()
    {
        float lat = Random.Range(_minCoords.x, _maxCoords.x);
        float lon = Random.Range(_minCoords.y, _maxCoords.y);
        return new Vector2(lat, lon);
    }
#endif
}