using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;
using Zenject;

public class MapPresenter : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    private MapService _mapService;
    private LocationService _locationService;
    private AbstractMap _map;

    private Vector2 _lastCoords;
    public bool _mapLoading;

    [Inject]
    public void Construct(MapService mapService, LocationService locationService, AbstractMap map)
    {
        _mapService = mapService;
        _locationService = locationService;
        _map = map;
    }

    private void FixedUpdate()
    {
        
    }
}