using System;
using UnityEngine;
using Zenject;

public class SelectMediator : IInitializable, IDisposable
{
    private readonly SelectWindow _selectWindow;
    private readonly UIManager _uiManager;

    public SelectMediator(SelectWindow selectWindow, UIManager uiManager)
    {
        _selectWindow = selectWindow;
        _uiManager = uiManager;
    }

    public void Initialize()
    {
        _selectWindow.OnBackClicked += HandleBackClicked;
        _selectWindow.OnPlayerSearchClicked += HandlePlayerSearchClicked;
        _selectWindow.OnShopClicked += HandleShopClicked;
        _selectWindow.OnPromoCodesClicked += HandlePromoCodesClicked;
        _selectWindow.OnFriendsClicked += HandleFriendsClicked;
        _selectWindow.OnClansClicked += HandleClansClicked;
        _selectWindow.OnInventoryClicked += HandleInventoryClicked;
    }

    public void Dispose()
    {
        _selectWindow.OnBackClicked -= HandleBackClicked;
        _selectWindow.OnPlayerSearchClicked -= HandlePlayerSearchClicked;
        _selectWindow.OnShopClicked -= HandleShopClicked;
        _selectWindow.OnPromoCodesClicked -= HandlePromoCodesClicked;
        _selectWindow.OnFriendsClicked -= HandleFriendsClicked;
        _selectWindow.OnClansClicked -= HandleClansClicked;
        _selectWindow.OnInventoryClicked -= HandleInventoryClicked;
    }

    private void HandleBackClicked()
    {
        _uiManager.Show<MenuWindow>();
    }

    private void HandlePlayerSearchClicked()
    {
        _uiManager.Show<PlayerSearchWindow>();
    }

    private void HandleShopClicked()
    {
        _uiManager.Show<ShopWindow>();
    }

    private void HandlePromoCodesClicked()
    {
        _uiManager.Show<PromoCodesWindow>();
    }

    private void HandleFriendsClicked()
    {
        _uiManager.Show<FriendsWindow>();
    }

    private void HandleClansClicked()
    {
        _uiManager.Show<ClansWindow>();
    }

    private void HandleInventoryClicked()
    {
        if (_uiManager.HasWindow<InventoryWindow>())
            _uiManager.Show<InventoryWindow>();
        else
            Debug.LogWarning("[SelectMediator] InventoryWindow not registered in scene.");
    }
}
