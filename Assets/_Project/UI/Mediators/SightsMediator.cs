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
    private readonly IPopupService _popupService;
    
    private List<SightItemView> _sightItemViews = new List<SightItemView>();

    public SightsMediator(
        SightsUpdater sightsUpdater,
        UIManager uiManager,
        SightsWindow sightsWindow,
        SightItemFactory factory,
        IPopupService popupService)
    {
        _sightsUpdater = sightsUpdater;
        _uiManager = uiManager;
        _sightsWindow = sightsWindow;
        _factory = factory;
        _popupService = popupService;
    }

    public void Initialize()
    {
        _sightsWindow.OnWindowOpened += LoadSights;
        _sightsWindow.OnBackClicked += () => _uiManager.Back();
        
        _sightsUpdater.OnImageLoaded += HandleImageLoaded;
    }

    public void Dispose()
    {
        _sightsWindow.OnWindowOpened -= LoadSights;
        
        _sightsUpdater.OnImageLoaded -= HandleImageLoaded;
    }

    private void LoadSights()
    {
        ClearItems();
        try
        {
            foreach (var info in _sightsUpdater.CachedNearestSights)
            {
                var item = _factory.Create();
                item.transform.SetParent(_sightsWindow.ContentParent, false);

                item.Title.text = info.Title;
                item.Distance.text = $"{info.Distance} м";
                item.PageId = info.PageId;
                item.OnClicked += HandleItemClicked;

                if (_sightsUpdater.TryGetImage(info.PageId, out var sprite))
                    item.SetImage(sprite);

                _sightItemViews.Add(item);
            }
        }
        catch (Exception e)
        {
            _popupService.ShowError("Sights not loaded, wait pls.");
        }

    }
    
    private void HandleItemClicked(int pageId)
    {
        Debug.Log("Sight clicked: " + pageId);
        _sightsUpdater.CreateSightDetailsPopup(pageId);
    }
    
    private void HandleImageLoaded(int pageId, Sprite sprite)
    {
        foreach (var item in _sightItemViews)
        {
            if (item.PageId == pageId)
            {
                item.SetImage(sprite);
                break;
            }
        }
    }
    
    private void ClearItems()
    {
        foreach (var item in _sightItemViews)
            item.OnClicked -= HandleItemClicked;
        foreach (Transform t in _sightsWindow.ContentParent)
        {
            GameObject.Destroy(t.gameObject);
        }
        _sightItemViews.Clear();
    }
}