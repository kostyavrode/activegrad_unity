using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class MapService
{
    private readonly string _apiKey;

    public MapService(string apiKey)
    {
        _apiKey = apiKey;
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
}