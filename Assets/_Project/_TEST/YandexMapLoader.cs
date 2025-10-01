using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class YandexMapLoader : MonoBehaviour
{
    [SerializeField] private string url = "https://static-maps.yandex.ru/v1?ll=37.620070,55.753630&lang=ru_RU&size=450,450&z=19&pt=37.620070,55.753630,pmwtm1~37.64,55.76363,pmwtm99&apikey=7955252a-2f7b-4c01-968f-19e1c095f7b5";
    [SerializeField] private Renderer targetRenderer; // укажи сюда Quad или Plane в инспекторе

    private void Start()
    {
        StartCoroutine(LoadMap());
    }

    private IEnumerator LoadMap()
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Ошибка загрузки карты: " + www.error + " | "+ www.url);
            }
            else
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(www);
                targetRenderer.material.mainTexture = tex; // применяем текстуру на объект
            }
        }
    }
}