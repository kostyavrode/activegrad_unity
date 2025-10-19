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

        [Inject]
        public GPSLocationProvider(CoroutineRunner coroutineRunner)
        {
            _coroutineRunner = coroutineRunner;
           // Debug.Log();
        }

        public void Initialize()
        {
            _coroutineRunner.StartCoroutine(RequestAndStartGPS());
        }

        private IEnumerator RequestAndStartGPS()
        {
    #if UNITY_ANDROID
            // Проверяем и запрашиваем разрешение
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Debug.Log("[GPS] Запрос разрешения на использование геолокации...");
                Permission.RequestUserPermission(Permission.FineLocation);
            }

            // Ждём, пока пользователь разрешит
            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Debug.Log("[GPS] Ожидание разрешения от пользователя...");
                yield return new WaitForSeconds(1f);
            }

            // Проверяем, включен ли GPS
            while (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("[GPS] GPS выключен. Ожидание включения пользователем...");
                yield return new WaitForSeconds(2f);
            }
    #endif

            Debug.Log("[GPS] Запуск службы геолокации...");
            Input.location.Start(1f, 1f); // (accuracy, minDistance)

            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                Debug.Log("[GPS] Инициализация...");
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            if (maxWait <= 0)
            {
                Debug.LogWarning("[GPS] Таймаут при запуске службы геолокации.");
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogWarning("[GPS] Не удалось получить координаты.");
                yield break;
            }

            _isRunning = true;
            Debug.Log("[GPS] Служба геолокации успешно запущена.");
        }

        public void Tick()
        {
 //           Debug.LogWarning("Zap");
    #if UNITY_ANDROID || UNITY_IOS
            if (_isRunning && Input.location.status == LocationServiceStatus.Running)
            {
                var data = Input.location.lastData;

                if (data.latitude != 0 && data.longitude != 0)
                {
                    _lastCoordinates = new Vector2(data.longitude, data.latitude);
                }
//                Debug.LogWarning("[GPS] Работает:."+_lastCoordinates);
            }
            else if (Input.location.status == LocationServiceStatus.Stopped)
            {
                Debug.LogWarning("[GPS] Служба геолокации остановлена.");
                _isRunning = false;
            }
    #else
            // Эмуляция координат в редакторе
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
