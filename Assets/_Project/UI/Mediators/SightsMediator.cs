using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SightsMediator : IInitializable, IDisposable
{
    private readonly SightsUpdater _sightsUpdater;
    private readonly UIManager _uiManager;
    private readonly SightsWindow _sightsWindow;
    private readonly SightItemFactory _factory;
    private readonly IPopupService _popupService;

    private List<SightItemView> _sightItemViews = new List<SightItemView>();
    private List<SightItemView> _partnerStoreItemViews = new List<SightItemView>();

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
        _sightsWindow.OnWindowOpened += HandleWindowOpened;
        _sightsWindow.OnBackClicked += () => _uiManager.Back();

        _sightsUpdater.OnImageLoaded += HandleImageLoaded;
        _sightsUpdater.OnSightsUpdated += HandleSightsUpdated;
        _sightsUpdater.OnPartnerStoresUpdated += HandlePartnerStoresUpdated;
    }

    public void Dispose()
    {
        _sightsWindow.OnWindowOpened -= HandleWindowOpened;
        _sightsUpdater.OnImageLoaded -= HandleImageLoaded;
        _sightsUpdater.OnSightsUpdated -= HandleSightsUpdated;
        _sightsUpdater.OnPartnerStoresUpdated -= HandlePartnerStoresUpdated;
    }

    private void HandleWindowOpened() => LoadSights();

    private void HandleSightsUpdated()
    {
        if (_sightsWindow.gameObject.activeInHierarchy)
            LoadSights();
    }

    private void HandlePartnerStoresUpdated()
    {
        if (_sightsWindow.gameObject.activeInHierarchy)
            RefreshPartnerStores();
    }

    private void LoadSights()
    {
        ClearItems();

        try
        {
            foreach (var kv in _sightsUpdater.CachedNearestSights)
            {
                var info = kv.Value;

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

        AppendPartnerStores();

        if (_sightItemViews.Count > 0 || _partnerStoreItemViews.Count > 0)
            _sightsWindow.PlayTabAnimation();
    }

    private void RefreshPartnerStores()
    {
        foreach (var item in _partnerStoreItemViews)
            item.OnClicked -= HandlePartnerStoreClicked;
        foreach (var item in _partnerStoreItemViews)
        {
            if (item != null)
                GameObject.Destroy(item.gameObject);
        }
        _partnerStoreItemViews.Clear();

        AppendPartnerStores();
    }

    private void AppendPartnerStores()
    {
        if (_sightsUpdater.CachedNearestPartnerStores == null) return;

        try
        {
            foreach (var kv in _sightsUpdater.CachedNearestPartnerStores)
            {
                var store = kv.Value;

                var item = _factory.Create();
                item.transform.SetParent(_sightsWindow.ContentParent, false);

                item.Title.text = !string.IsNullOrEmpty(store.name) ? store.name : "default";
                item.Distance.text = $"{store.distance_km * 1000:F0} м";
                item.PageId = store.id;
                item.OnClicked += HandlePartnerStoreClicked;

                _partnerStoreItemViews.Add(item);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SightsMediator] Partner stores not loaded: " + e.Message);
        }
    }

    private void HandleItemClicked(int pageId)
    {
        Debug.Log("Sight clicked: " + pageId);
        _sightsUpdater.CreateSightDetailsPopup(pageId);
    }

    private void HandlePartnerStoreClicked(int storeId)
    {
        Debug.Log("Partner store clicked: " + storeId);
        _sightsUpdater.CreatePartnerStoreDetailsPopup(storeId);
    }

    private void HandleImageLoaded(int pageId, Sprite sprite)
    {
        foreach (var item in _sightItemViews)
        {
            if (item == null) continue;
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
        foreach (var item in _partnerStoreItemViews)
            item.OnClicked -= HandlePartnerStoreClicked;
        foreach (Transform t in _sightsWindow.ContentParent)
        {
            GameObject.Destroy(t.gameObject);
        }
        _sightItemViews.Clear();
        _partnerStoreItemViews.Clear();
    }
}
