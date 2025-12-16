using System.Collections;
using System.Threading.Tasks;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

public class MapService : ITickable
{
    private readonly string _apiKey;
    private readonly AbstractMap _map;
    private readonly LocationService _locationService;
    
    public MapService(string apiKey, AbstractMap map, LocationService locationService)
    {
        _apiKey = apiKey;
        _map = map;
        _locationService = locationService;
    }

    public async Task<Texture2D> LoadMap(string longitude, string latitude, int zoom = 18, int size = 450)
    {
        string url = $"https://static-maps.yandex.ru/v1?ll={longitude},{latitude}&lang=ru_RU&size={size},{size}&z={zoom}&apikey={_apiKey}";

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            var op = www.SendWebRequest();
            Debug.Log(url);
            while (!op.isDone)
                await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Ошибка загрузки карты: " + www.error + " | "+www.url);
                return null;
            }
            else
            {
                return DownloadHandlerTexture.GetContent(www);
            }
        }
    }

    public void Tick()
    {
        var c = _locationService.GetCoordinates(); 
        _map.UpdateMap(new Vector2d((double)c.x, (double)c.y));
        Debug.Log(c);
    }
}