using System;
using UnityEngine;
using Zenject;

public class PlayerSearchMediator : IInitializable, IDisposable
{
    private readonly APIService _apiService;
    private readonly UIManager _uiManager;
    private readonly PlayerSearchWindow _playerSearchWindow;
    private readonly IPopupService _popupService;
    
    public PlayerSearchMediator(APIService apiService, UIManager uiManager, PlayerSearchWindow playerSearchWindow, IPopupService popupService)
    {
        _apiService = apiService;
        _uiManager = uiManager;
        _playerSearchWindow = playerSearchWindow;
        _popupService = popupService;
    }
    
    public void Initialize()
    {
        _playerSearchWindow.OnBackClicked += HandleBackClicked;
        _playerSearchWindow.OnSearchClicked += HandleSearchClicked;
    }

    public void Dispose()
    {
        _playerSearchWindow.OnBackClicked -= HandleBackClicked;
        _playerSearchWindow.OnSearchClicked -= HandleSearchClicked;
    }

    private async void HandleSearchClicked()
    {
        Debug.Log("Search Clicked");
        int playerID = Convert.ToInt32(_playerSearchWindow.InputFieldData);
        var (success, message) = await _apiService.SearchPlayer(playerID);
        
        if (!success)
        {
            _popupService.ShowError(message);
            Debug.LogError(message);
            return;
        }

        var (requestSuccess, requestResponse) = await _apiService.SendFriendRequest(playerID);
        
        if (requestSuccess && requestResponse != null)
        {
            _popupService.ShowSuccess("Friend request sent successfully");
        }
        else
        {
            _popupService.ShowError("Failed to send friend request");
        }
    }
    
    private void HandleBackClicked()
    {
        _uiManager.Back();
    }
}
