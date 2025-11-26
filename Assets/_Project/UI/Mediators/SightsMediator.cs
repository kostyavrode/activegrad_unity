using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

public class SightsMediator : IInitializable, IDisposable
{
    private readonly SightsUpdater _sightsUpdater;
    private readonly UIManager _uiManager;
    private readonly SightsWindow _sightsWindow;
    private readonly SightItemFactory _factory;
    
    private List<SightItemView> _sightItemViews = new List<SightItemView>();

    public SightsMediator(
        SightsUpdater sightsUpdater,
        UIManager uiManager,
        SightsWindow sightsWindow,
        SightItemFactory factory)
    {
        _sightsUpdater = sightsUpdater;
        _uiManager = uiManager;
        _sightsWindow = sightsWindow;
        _factory = factory;
    }

    public void Initialize()
    {
        Debug.Log("Initializing SightsMediator");
        _sightsWindow.OnWindowOpened += LoadSights;
        _sightsWindow.OnBackClicked += () => _uiManager.Back();  
    }

    public void Dispose()
    {
        _sightsWindow.OnWindowOpened -= LoadSights;
    }

    private void LoadSights()
    {
        ClearItems();

        foreach (var info in _sightsUpdater.CachedNearestSights)
        {
            var item = _factory.Create();
            
            item.transform.SetParent(_sightsWindow.ContentParent, false);

            item.Title.text = info.Title;
            item.Distance.text = $"{info.Distance} м";
            
            _sightItemViews.Add(item);
            
            /*if (!string.IsNullOrEmpty(info.ImageUrl))
            {
                var sprite = await _imageLoader.LoadSpriteAsync(info.ImageUrl);
                item.SetImage(sprite);
            }*/
        }
        Debug.Log("Loaded SightsMediator!!!!!!!!!!!!!!!!!");
        SetImagesInSights();
    }

    private async void SetImagesInSights()
    {
        for (int i = 0; i < _sightItemViews.Count; i++)
        {
            Debug.Log(_sightsUpdater.CachedSights[i].OriginalImageUrl);
            string url = FixUrl(_sightsUpdater.CachedSights[i].OriginalImageUrl);

            if (string.IsNullOrEmpty(url))
                continue;

            var sprite = await LoadSpriteAsync(url);

            if (sprite != null)
                _sightItemViews[i].SetImage(sprite);
        }
    }

    
    public async Task<Sprite> LoadSpriteAsync(string url)
    {
        Debug.Log("Loading image: " + url);

        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            var op = req.SendWebRequest();

            while (!op.isDone)
                await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Image load error ({url}): {req.error}");
                return null;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
        }
    }

    private string FixUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        url = url.Trim();

        // Википедия часто даёт URL без схемы
        if (url.StartsWith("//"))
            url = "https:" + url;

        // Заменяем пробелы
        url = url.Replace(" ", "%20");

        return url;
    }
    
    private void ClearItems()
    {
        foreach (Transform t in _sightsWindow.ContentParent)
        {
            GameObject.Destroy(t.gameObject);
        }
        _sightItemViews.Clear();
    }
}