using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class WikipediaApiClient
{
    private const string Base = "https://ru.wikipedia.org/w/api.php";

    public async Task<string> GetAsync(string url)
    {
        Debug.Log("[WikipediaApi] Request: " + url);

        using var request = UnityWebRequest.Get(url);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[WikipediaApi] Error: " + request.error);
            return null;
        }

        string text = request.downloadHandler.text;

        Debug.Log("[WikipediaApi] Response OK, length = " + text.Length);

        return text;
    }


    public string BuildGeoSearchUrl(Vector2 coordinates, int radius)
    {
        Debug.Log("[WikipediaApi] BuildGeoSearchUrl");
        var lat = coordinates.x.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lon = coordinates.y.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return $"https://ru.wikipedia.org/w/api.php?action=query&list=geosearch&gscoord={lat}|{lon}&gsradius={radius}&gslimit=25&format=json";
    }


    public string BuildExtractUrl(int pageId) =>
        $"{Base}?action=query&prop=extracts|pageimages&exintro=1&explaintext=1&pageids={pageId}&pithumbsize=500&format=json";
}