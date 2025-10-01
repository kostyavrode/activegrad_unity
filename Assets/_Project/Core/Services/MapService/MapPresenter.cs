using UnityEngine;
using Zenject;

public class MapPresenter : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    private MapService _mapService;
    private LocationService _locationService;

    private Vector2 _lastCoords;
    private bool _mapLoading;

    [Inject]
    public void Construct(MapService mapService, LocationService locationService)
    {
        _mapService = mapService;
        _locationService = locationService;
    }

    private async void Update()
    {
        Debug.Log(_mapLoading+ " | "+_locationService);
        if (_mapLoading) return;

        Vector2 coords = _locationService.GetCoordinates();
        
        if (coords != _lastCoords && coords != Vector2.zero)
        {
            _mapLoading = true;
            _lastCoords = coords;
            
            Debug.Log(coords.x+ coords.y);

            Texture2D tex = await _mapService.LoadMap(coords.x.ToString().Replace(',', '.'), coords.y.ToString().Replace(',', '.'), zoom: 17);

            if (tex != null)
            {
                targetRenderer.material.mainTexture = tex;
            }

            _mapLoading = false;
        }
    }
}