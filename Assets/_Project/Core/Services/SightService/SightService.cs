using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

public class SightService : ISightService
{
    private readonly WikipediaApiClient _client;
    
    private readonly Dictionary<string, Texture2D> _imageCache = new Dictionary<string, Texture2D>();

    public SightService(WikipediaApiClient client)
    {
        _client = client;
    }

    public async Task<List<SightShortInfo>> LoadNearestSightsAsync(Vector2 coordinates, int radius = 5000)
    {
        string url = _client.BuildGeoSearchUrl(coordinates, radius);
        string json = await _client.GetAsync(url);
        
//        Debug.Log(json);

        var result = new List<SightShortInfo>();
        
        try
        {
            var root = JObject.Parse(json);
            var list = root["query"]["geosearch"];

            

            foreach (var item in list)
            {
                result.Add(new SightShortInfo
                {
                    PageId = item["pageid"].Value<int>(),
                    Title = item["title"].Value<string>(),
                    Distance = item["dist"].Value<float>(),
                    Latitude = item["lat"].Value<double>(),
                    Longitude = item["lon"].Value<double>()
                });
                
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            result = new List<SightShortInfo>();
            throw;
        }


        

        return result;
    }

    public async Task<SightFullInfo> LoadSightDetailsAsync(int pageId)
    {
        string url = _client.BuildExtractUrl(pageId);
        string json = await _client.GetAsync(url);
        if (string.IsNullOrEmpty(json))
            return null;

        return ParseSightFromJson(pageId, json);
    }

    public async Task<List<SightFullInfo>> LoadSightDetailsBatchAsync(List<int> pageIds)
    {
        if (pageIds == null || pageIds.Count == 0)
            return new List<SightFullInfo>();

        string url = _client.BuildExtractBatchUrl(pageIds);
        string json = await _client.GetAsync(url);
        if (string.IsNullOrEmpty(json))
            return new List<SightFullInfo>();

        var result = new List<SightFullInfo>();
        try
        {
            var root = JObject.Parse(json);
            var pages = (JObject)root["query"]["pages"];
            foreach (var kv in pages)
            {
                int pageId = kv.Value["pageid"].Value<int>();
                var page = (JObject)kv.Value;
                string description = page["extract"]?.Value<string>() ?? "";
                string thumbUrl = page["thumbnail"]?["source"]?.Value<string>();
                string originalUrl = page["original"]?["source"]?.Value<string>();

                result.Add(new SightFullInfo
                {
                    PageId = pageId,
                    Title = page["title"]?.Value<string>() ?? "",
                    Description = description,
                    ImageUrl = thumbUrl,
                    OriginalImageUrl = originalUrl ?? thumbUrl
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[SightService] Parse batch failed: " + e.Message);
        }

        return result;
    }

    private SightFullInfo ParseSightFromJson(int pageId, string json)
    {
        JObject root = JObject.Parse(json);
        JObject pages = (JObject)root["query"]["pages"];
        JObject page = (JObject)pages.First.First;

        string description = page["extract"]?.Value<string>() ?? "";
        string thumbUrl = page["thumbnail"]?["source"]?.Value<string>();
        string originalUrl = page["original"]?["source"]?.Value<string>();

        return new SightFullInfo
        {
            PageId = pageId,
            Title = page["title"].Value<string>(),
            Description = description,
            ImageUrl = thumbUrl,
            OriginalImageUrl = originalUrl ?? thumbUrl
        };
    }



    public async Task<Texture2D> LoadImageAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Проверяем кэш
        if (_imageCache.TryGetValue(url, out var cached))
            return cached;

        using var request = UnityWebRequestTexture.GetTexture(url);
        var async = request.SendWebRequest();

        while (!async.isDone)
            await Task.Yield();
        
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image load failed: " + request.error);
            return null;
        }

        Texture2D tex = DownloadHandlerTexture.GetContent(request);
        _imageCache[url] = tex; // сохраняем в кэш

        return tex;
    }

}