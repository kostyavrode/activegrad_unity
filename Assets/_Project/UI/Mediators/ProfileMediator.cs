using System;
using UnityEngine;
using Zenject;

public class ProfileMediator : IInitializable, IDisposable
{
    private readonly ProfileWindow _profileWindow;
    private readonly UserDataService _userDataService;
    private readonly UIManager _uiManager;

    public ProfileMediator(ProfileWindow profileWindow,UserDataService userDataService, UIManager uiManager)
    {
        _profileWindow = profileWindow;
        _userDataService = userDataService;
        _uiManager = uiManager;
    }
    
    public void Initialize()
    {
        _profileWindow.OnBackClicked += HandleBackClicked;
        _profileWindow.OnProfileOpened += LoadInfo;
        Debug.Log($"OnProfileOpened");
    }

    public void Dispose()
    {
        _profileWindow.OnBackClicked -= HandleBackClicked;
        _profileWindow.OnProfileOpened -= LoadInfo;
    }

    private void LoadInfo()
    {
        string[] info = new string[5];
        info[0] = _userDataService.Username;
        info[1] = _userDataService.FirstName;
        info[2] = _userDataService.LastName;
        info[3] = _userDataService.Level.ToString();
        info[4] = _userDataService.Experience.ToString();
        
        _profileWindow.SetInfo(info);
        Debug.Log("Profile Loaded");
    }
    
    private void HandleBackClicked()
    {
        _uiManager.Back();
    }
}
