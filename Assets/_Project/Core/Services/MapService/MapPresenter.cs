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
        var c = _locationService.GetCoordinates(); 
        _map.UpdateMap(new Vector2d((double)c.y, (double)c.x));
        Debug.Log(c);
        /*
//        Debug.Log(_mapLoading+ " | "+_locationService);
        if (_mapLoading) return;
        
        Vector2 coords = _locationService.GetCoordinates();
        //Debug.Log(coords);
        
        if (coords != _lastCoords && coords != Vector2.zero)
        {
            _mapLoading = true;
            _lastCoords = coords;
            
//            Debug.Log(coords.x+ coords.y);

            Texture2D tex = await _mapService.LoadMap(coords.x.ToString().Replace(',', '.'), coords.y.ToString().Replace(',', '.'), zoom: 17);

            if (tex != null)
            {
                targetRenderer.material.mainTexture = tex;
            }

            _mapLoading = false;
        }*/
        
    }
}