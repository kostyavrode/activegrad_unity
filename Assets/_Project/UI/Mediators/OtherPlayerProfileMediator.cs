using System;
using UnityEngine;
using Zenject;

public class OtherPlayerProfileMediator : IInitializable, IDisposable
{
    private readonly APIService _apiService;
    private readonly UIManager _uiManager;
    private readonly OtherPlayerProfileView _otherPlayerProfileView;

    public OtherPlayerProfileMediator(APIService apiService, UIManager uiManager, OtherPlayerProfileView otherPlayerProfileView)
    {
        _apiService = apiService;
        _uiManager = uiManager;
        _otherPlayerProfileView = otherPlayerProfileView;
    }

    public void Initialize()
    {
        _otherPlayerProfileView.OnBackClicked += HandleBackClicked;
    }

    public void Dispose()
    {
        _otherPlayerProfileView.OnBackClicked -= HandleBackClicked;
    }
    
    private void HandleBackClicked()
    {
        _uiManager.Back();
    }
}
