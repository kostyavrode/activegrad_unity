using UnityEngine;
using System.Collections;
using Zenject;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine.InputSystem;
#endif

public class GPSLocationProvider : ILocationProvider, IInitializable, ITickable
{
    private readonly CoroutineRunner _coroutineRunner;
    private double _longitude;
    private double _latitude;

    private readonly Vector2 _minCoords = new(55.70f, 37.60f);
    private readonly Vector2 _maxCoords = new(55.80f, 37.70f);

    private float _updateInterval = 5f;
    private float _timer = 0f;
    private bool _isRunning = false;
    private bool _gpsRequestInProgress = false;

#if UNITY_EDITOR || UNITY_STANDALONE
    private bool _isTestMode = false;
    private const double StartLongitude = 30.394770;
    private const double StartLatitude = 59.875774;
    private const double KeyboardCoordinateStep = 0.0001 / 18.0;
    private const float AutoWalkDuration = 5f;
    private double _autoWalkDeltaX;
    private double _autoWalkDeltaY;
    private float _autoWalkRemaining;
#endif

    [Inject]
    public GPSLocationProvider(CoroutineRunner coroutineRunner)
    {
        _coroutineRunner = coroutineRunner;
    }

    public void Initialize()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        SetCoords(StartLongitude, StartLatitude);
        _isTestMode = true;
#endif
        if (!Application.isEditor)
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
#if UNITY_EDITOR || UNITY_STANDALONE
        ApplyKeyboardSimulation();
        if (_isTestMode)
            return;
#endif

#if UNITY_ANDROID || UNITY_IOS
        if (Application.isEditor)
            return;

        if (_isRunning && Input.location.status == LocationServiceStatus.Running)
        {
            var data = Input.location.lastData;

            if (data.latitude != 0 && data.longitude != 0)
            {
                SetCoords(data.longitude, data.latitude);
            }
        }
        else if (_isRunning && Input.location.status == LocationServiceStatus.Stopped)
        {
            _isRunning = false;
            SetCoords(0, 0);
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
                SetCoords(GetRandomCoords());
            }
        }
#endif
    }

    public Vector2 GetCoordinates()
    {
        GetCoordinatesPrecise(out double longitude, out double latitude);
        return new Vector2((float)longitude, (float)latitude);
    }

    public void GetCoordinatesPrecise(out double longitude, out double latitude)
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (_longitude == 0 && _latitude == 0)
            SetCoords(StartLongitude, StartLatitude);
#endif
        longitude = _longitude;
        latitude = _latitude;
    }

    private void SetCoords(double longitude, double latitude)
    {
        _longitude = longitude;
        _latitude = latitude;
    }

    private void SetCoords(Vector2 coords)
    {
        SetCoords(coords.x, coords.y);
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    public void SetTestCoordinates(Vector2 coords)
    {
        SetCoords(coords.x, coords.y);
        _isTestMode = true;
    }

    private void ApplyKeyboardSimulation()
    {
        if (IsShiftHeld())
        {
            double autoX = 0;
            double autoY = 0;
            if (WasPressed(Key.UpArrow, KeyCode.UpArrow))
                autoY += KeyboardCoordinateStep;
            if (WasPressed(Key.DownArrow, KeyCode.DownArrow))
                autoY -= KeyboardCoordinateStep;
            if (WasPressed(Key.RightArrow, KeyCode.RightArrow))
                autoX += KeyboardCoordinateStep;
            if (WasPressed(Key.LeftArrow, KeyCode.LeftArrow))
                autoX -= KeyboardCoordinateStep;

            if (autoX != 0 || autoY != 0)
            {
                _autoWalkDeltaX = autoX;
                _autoWalkDeltaY = autoY;
                _autoWalkRemaining = AutoWalkDuration;
                _isTestMode = true;
            }
        }

        if (_autoWalkRemaining > 0f)
        {
            SetCoords(_longitude + _autoWalkDeltaX, _latitude + _autoWalkDeltaY);
            _autoWalkRemaining -= Time.deltaTime;
            if (_autoWalkRemaining < 0f)
            {
                _autoWalkRemaining = 0f;
                _autoWalkDeltaX = 0;
                _autoWalkDeltaY = 0;
            }
            return;
        }

        double deltaX = 0;
        double deltaY = 0;

        if (IsHeld(Key.UpArrow, KeyCode.UpArrow))
            deltaY += KeyboardCoordinateStep;
        if (IsHeld(Key.DownArrow, KeyCode.DownArrow))
            deltaY -= KeyboardCoordinateStep;
        if (IsHeld(Key.RightArrow, KeyCode.RightArrow))
            deltaX += KeyboardCoordinateStep;
        if (IsHeld(Key.LeftArrow, KeyCode.LeftArrow))
            deltaX -= KeyboardCoordinateStep;

        if (deltaX == 0 && deltaY == 0)
            return;

        SetCoords(_longitude + deltaX, _latitude + deltaY);
        _isTestMode = true;
    }

    private static bool IsShiftHeld()
    {
        if (Keyboard.current != null &&
            (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
            return true;

        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private static bool WasPressed(Key key, KeyCode keyCode)
    {
        if (Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame)
            return true;

        return Input.GetKeyDown(keyCode);
    }

    private static bool IsHeld(Key key, KeyCode keyCode)
    {
        if (Keyboard.current != null && Keyboard.current[key].isPressed)
            return true;

        return Input.GetKey(keyCode);
    }
#endif

    private Vector2 GetRandomCoords()
    {
        float lat = Random.Range(_minCoords.x, _maxCoords.x);
        float lon = Random.Range(_minCoords.y, _maxCoords.y);
        return new Vector2(30.394770f, 59.875774f);
    }
}
