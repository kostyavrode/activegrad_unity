using System;
using UnityEngine;
using Zenject;

public class SightsMediator : IInitializable, IDisposable
{
    private readonly SightsUpdater _sightsUpdater;
    private readonly UIManager _uiManager;
    private readonly SightsWindow _sightsWindow;
    private readonly SightItemFactory _factory;

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
            
            /*if (!string.IsNullOrEmpty(info.ImageUrl))
            {
                var sprite = await _imageLoader.LoadSpriteAsync(info.ImageUrl);
                item.SetImage(sprite);
            }*/
        }
    }

    private void ClearItems()
    {
        foreach (Transform t in _sightsWindow.ContentParent)
            GameObject.Destroy(t.gameObject);
    }
}