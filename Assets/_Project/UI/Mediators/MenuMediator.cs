using System;
using UnityEngine;
using Zenject;

public class MenuMediator : IInitializable, IDisposable
{
    private readonly MenuWindow _menuWindow;
    private readonly UIManager _uiManager;

    public MenuMediator(MenuWindow menuWindow, UIManager uiManager)
    {
        _menuWindow = menuWindow;
        _uiManager = uiManager;
    }
    
    public void Initialize()
    {
        _menuWindow.OnQuestsClicked += HandleQuestsClicked;
        _menuWindow.OnProfileClicked += HandleProfileClicked;
        _menuWindow.OnSettingsClicked += HandleSettingsClicked;
        _menuWindow.OnSightClicked += HandleSightsClicked;
        _menuWindow.OnSearchPlayerClicked += HandleSearchPlayerClicked;
        _menuWindow.OnShopClicked += HandleShopClicked;
        _menuWindow.OnPromoCodesClicked += HandlePromoCodeClicked;
        _menuWindow.OnFriendsClicked += HandleFriendButtonClicked;
        _menuWindow.OnClansClicked += HandleClansClicked;
    }

    public void Dispose()
    {
        _menuWindow.OnQuestsClicked -= HandleQuestsClicked;
        _menuWindow.OnProfileClicked -= HandleProfileClicked;
        _menuWindow.OnSettingsClicked -= HandleSettingsClicked;
        _menuWindow.OnSightClicked -= HandleSightsClicked;
        _menuWindow.OnSearchPlayerClicked -= HandleSearchPlayerClicked;
        _menuWindow.OnShopClicked -= HandleShopClicked;
        _menuWindow.OnPromoCodesClicked -= HandlePromoCodeClicked;
        _menuWindow.OnFriendsClicked -= HandleFriendButtonClicked;
        _menuWindow.OnClansClicked -= HandleClansClicked;
    }

    private void HandleSettingsClicked()
    {
        _uiManager.Show<SettingsWindow>();
    }

    private void HandleProfileClicked()
    {
        _uiManager.Show<ProfileWindow>();
    }

    private void HandleQuestsClicked()
    {
        _uiManager.Show<QuestWindow>();
    }

    private void HandleSightsClicked()
    {
        _uiManager.Show<SightsWindow>();
    }

    private void HandleSearchPlayerClicked()
    {
        _uiManager.Show<PlayerSearchWindow>();
    }

    private void HandleShopClicked()
    {
        _uiManager.Show<ShopWindow>();
    }

    private void HandlePromoCodeClicked()
    {
        _uiManager.Show<PromoCodesWindow>();
    }

    private void HandleFriendButtonClicked()
    {
        _uiManager.Show<FriendsWindow>();
    }

    private void HandleClansClicked()
    {
        _uiManager.Show<ClansWindow>();
    }
}
