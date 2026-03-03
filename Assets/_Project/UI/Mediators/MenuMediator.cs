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
        _menuWindow.OnSelectClicked += HandleSelectClicked;
    }

    public void Dispose()
    {
        _menuWindow.OnQuestsClicked -= HandleQuestsClicked;
        _menuWindow.OnProfileClicked -= HandleProfileClicked;
        _menuWindow.OnSettingsClicked -= HandleSettingsClicked;
        _menuWindow.OnSightClicked -= HandleSightsClicked;
        _menuWindow.OnSelectClicked -= HandleSelectClicked;
    }

    private void HandleSelectClicked()
    {
        _uiManager.Show<SelectWindow>();
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
}
