using System;
using UnityEngine;
using Zenject;

public class PlayerSearchMediator : IInitializable, IDisposable
{
    private readonly APIService _apiService;
    private readonly UIManager _uiManager;
    private readonly PlayerSearchWindow _playerSearchWindow;
    //private readonly PopupService _popupService;
    private readonly GamePopupService _gamePopupService;
    
    public PlayerSearchMediator(APIService apiService, UIManager uiManager, PlayerSearchWindow playerSearchWindow/*, PopupService popupService*/, GamePopupService gamePopupService)
    {
        _apiService = apiService;
        _uiManager = uiManager;
        _playerSearchWindow = playerSearchWindow;
        //_popupService = popupService;
        _gamePopupService = gamePopupService;
    }
    public void Initialize()
    {
        _playerSearchWindow.OnBackClicked += HandleBackClicked;
        _playerSearchWindow.OnSearchClicked +=HandleSearchClicked;
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
            //_popupService.ShowError($"Failed to load quests: {message}");
            Debug.LogError(message);
            return;
        }

        _gamePopupService.CreateOtherPlayerProfilePopup(message);
        Debug.Log($"Loaded Player: {message}");
    }
    
    private void HandleBackClicked()
    {
        _uiManager.Back();
    }
}
