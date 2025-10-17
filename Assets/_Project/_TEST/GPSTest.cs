using System.Collections;
using TMPro;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class GPSTest : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text coordsText;

    private IEnumerator Start()
    {
        statusText.text = "⏳ Запрос разрешения...";

#if UNITY_ANDROID
        // Проверяем разрешение
        while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            statusText.text = "📱 Запрашиваем разрешение GPS...";
            yield return new WaitForSeconds(1f);
        }
#endif

        if (!Input.location.isEnabledByUser)
        {
            statusText.text = "⚠️ GPS выключен пользователем!";
            yield break;
        }

        // Запускаем сервис
        Input.location.Start(1f, 0.1f);
        statusText.text = "🚀 Инициализация GPS...";

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            statusText.text = $"⏳ Инициализация... {maxWait}s";
            maxWait--;
        }

        if (maxWait <= 0)
        {
            statusText.text = "❌ Таймаут при запуске GPS.";
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            statusText.text = "❌ Ошибка: не удалось получить координаты.";
            yield break;
        }

        statusText.text = "✅ GPS запущен!";
        StartCoroutine(UpdateGPS());
    }

    private IEnumerator UpdateGPS()
    {
        while (true)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                var data = Input.location.lastData;
                coordsText.text = $"🌍 Координаты:\nШирота: {data.latitude:F6}\nДолгота: {data.longitude:F6}\nТочность: {data.horizontalAccuracy:F2}м";
            }
            else
            {
                coordsText.text = $"⚠️ GPS статус: {Input.location.status}";
            }

            yield return new WaitForSeconds(2f);
        }
    }
}
