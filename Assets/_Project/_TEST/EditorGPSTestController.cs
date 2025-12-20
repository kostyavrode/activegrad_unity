using UnityEngine;
using Zenject;

#if UNITY_EDITOR
public class EditorGPSTestController : MonoBehaviour
{
    private float coordinateStep = 0.0001f;
    [SerializeField] private Vector2 startCoordinates = new Vector2(30.394770f, 59.875774f);
    
    private ILocationProvider _locationProvider;
    private GPSLocationProvider _gpsProvider;
    private Vector2 _currentTestCoordinates;
    private bool _isInitialized = false;
    
    [Inject]
    private void Construct(ILocationProvider locationProvider)
    {
        _locationProvider = locationProvider;
        _gpsProvider = locationProvider as GPSLocationProvider;
        if (_gpsProvider != null)
        {
            Debug.Log($"[EditorGPSTest] Construct: GPSProvider instance: {_gpsProvider.GetHashCode()}, ILocationProvider instance: {_locationProvider.GetHashCode()}");
        }
        else
        {
            Debug.LogError("[EditorGPSTest] GPSProvider is null!");
        }
    }
    
    private void Start()
    {
        if (!Application.isEditor)
        {
            enabled = false;
            return;
        }
        
        _currentTestCoordinates = startCoordinates;
        _isInitialized = true;
        
        if (_gpsProvider != null)
        {
            _gpsProvider.SetTestCoordinates(_currentTestCoordinates);
        }
    }
    
    private void Update()
    {
        if (!Application.isEditor || !_isInitialized || _gpsProvider == null)
            return;
        
        bool moved = false;
        Vector2 newCoords = _currentTestCoordinates;
        
        if (Input.GetKey(KeyCode.UpArrow))
        {
            newCoords.y += coordinateStep;
            moved = true;
        }
        
        if (Input.GetKey(KeyCode.DownArrow))
        {
            newCoords.y -= coordinateStep;
            moved = true;
        }
        
        if (Input.GetKey(KeyCode.RightArrow))
        {
            newCoords.x += coordinateStep;
            moved = true;
        }
        
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            newCoords.x -= coordinateStep;
            moved = true;
        }
        
        if (moved)
        {
            _currentTestCoordinates = newCoords;
        }
    }
    
    private void LateUpdate()
    {
        if (!Application.isEditor || !_isInitialized || _gpsProvider == null)
            return;
        
        if (_currentTestCoordinates != startCoordinates || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.LeftArrow))
        {
            SetTestCoordinates(_currentTestCoordinates);
        }
    }
    
    private void SetTestCoordinates(Vector2 coords)
    {
        if (_gpsProvider != null)
        {
            _gpsProvider.SetTestCoordinates(coords);
        }
    }
    
    private void OnGUI()
    {
        if (!Application.isEditor || !_isInitialized)
            return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 10, 500, 30), $"[Editor GPS Test] Use Arrow Keys to move", style);
        GUI.Label(new Rect(10, 40, 500, 30), $"Current: Lat={_currentTestCoordinates.y:F6}, Lon={_currentTestCoordinates.x:F6}", style);
        GUI.Label(new Rect(10, 70, 500, 30), $"Step: {coordinateStep}", style);
    }
}

#endif

