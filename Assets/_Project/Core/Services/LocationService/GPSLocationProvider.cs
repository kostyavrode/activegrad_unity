    using UnityEngine;
    using System.Collections;
    using Zenject;
    #if UNITY_ANDROID
    using UnityEngine.Android;
    #endif

    public class GPSLocationProvider : ILocationProvider, IInitializable, ITickable
    {
        private readonly CoroutineRunner _coroutineRunner;
        private Vector2 _lastCoordinates = Vector2.zero;

        private readonly Vector2 _minCoords = new(55.70f, 37.60f);
        private readonly Vector2 _maxCoords = new(55.80f, 37.70f);

        private float _updateInterval = 5f;
        private float _timer = 0f;
        private bool _isRunning = false;
        private bool _gpsRequestInProgress = false;
#if UNITY_EDITOR
        private bool _isTestMode = false;
#endif

        [Inject]
        public GPSLocationProvider(CoroutineRunner coroutineRunner)
        {
            _coroutineRunner = coroutineRunner;
        }

        public void Initialize()
        {
            BeginGpsRequest();
        }

        private void BeginGpsRequest()
        {
            if (_gpsRequestInProgress)
                return;

            _coroutineRunner.StartCoroutine(RequestAndStartGPS());
        }

        private IEnumerator RequestAndStartGPS()
        {
            _gpsRequestInProgress = true;

    #if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                Permission.RequestUserPermission(Permission.FineLocation);

            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                yield return new WaitForSeconds(1f);
    #endif

    #if UNITY_ANDROID || UNITY_IOS
            while (!Input.location.isEnabledByUser)
                yield return new WaitForSeconds(2f);

            while (true)
            {
                Input.location.Stop();
                Input.location.Start(1f, 1f);

                int maxWait = 20;
                while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
                {
                    yield return new WaitForSeconds(1);
                    maxWait--;
                }

                if (Input.location.status == LocationServiceStatus.Running)
                {
                    _isRunning = true;
                    _gpsRequestInProgress = false;
                    yield break;
                }

                Input.location.Stop();
                yield return new WaitForSeconds(2f);
            }
    #else
            _gpsRequestInProgress = false;
            yield break;
    #endif
        }

        public void Tick()
        {
    #if UNITY_ANDROID || UNITY_IOS
            if (_isRunning && Input.location.status == LocationServiceStatus.Running)
            {
                var data = Input.location.lastData;

                if (data.latitude != 0 && data.longitude != 0)
                {
                    _lastCoordinates = new Vector2(data.longitude, data.latitude);
                }
            }
            else if (_isRunning && Input.location.status == LocationServiceStatus.Stopped)
            {
                _isRunning = false;
                _lastCoordinates = Vector2.zero;
                _gpsRequestInProgress = false;
                BeginGpsRequest();
            }
    #else
            if (!Application.isEditor)
            {
                _timer += Time.deltaTime;
                if (_timer >= _updateInterval)
                {
                    _timer = 0f;
                    _lastCoordinates = GetRandomCoords();
                }
            }
    #endif
        }

        public Vector2 GetCoordinates()
        {
#if UNITY_EDITOR
            if (_lastCoordinates == Vector2.zero)
            {
                _lastCoordinates = startCoordinates;
            }
#elif UNITY_STANDALONE
            if (_lastCoordinates == Vector2.zero)
            {
                _lastCoordinates = GetRandomCoords();
            }
#endif

            return _lastCoordinates;
        }
        
#if UNITY_EDITOR
        private static readonly Vector2 startCoordinates = new Vector2(30.394770f, 59.875774f);
#endif
        
#if UNITY_EDITOR
        public void SetTestCoordinates(Vector2 coords)
        {
            _lastCoordinates = coords;
            _isTestMode = true;
        }
#endif

        
        private Vector2 GetRandomCoords()
        {
            float lat = Random.Range(_minCoords.x, _maxCoords.x);
            float lon = Random.Range(_minCoords.y, _maxCoords.y);
            return new Vector2(30.394770f, 59.875774f);
        }
    }

