using System;
using UnityEngine;
using Zenject;

public class RegisterMediator : IInitializable, IDisposable
{
    private readonly RegisterWindow _registerWindow;
    private readonly APIService _apiService;
    private readonly UIManager _uiManager;

    public RegisterMediator(RegisterWindow registerWindow, APIService apiService, UIManager uiManager)
    {
        _registerWindow = registerWindow;
        _apiService = apiService;
        _uiManager = uiManager;
    }

    public void Initialize()
    {
        _registerWindow.OnRegisterClicked += HandleRegisterClicked;
        _registerWindow.OnBackClicked += HandleBackClicked;
    }

    public void Dispose()
    {
        _registerWindow.OnRegisterClicked -= HandleRegisterClicked;
        _registerWindow.OnBackClicked -= HandleBackClicked;
    }

    private async void HandleRegisterClicked(string[] input)
    {

        var (success, message) = await _apiService.Register(input[0], firstName: input[2], lastName: input[3], input[1]);

        if (success)
        {
            var loginSuccess = await _apiService.Login(input[0], input[1]);

            if (loginSuccess)
            {
                _uiManager.Show<CharacterCustomizationWindow>();
            }
            else
            {
                Debug.LogWarning("RegisterMediator: зарегистрировались, но не смогли войти");
                _uiManager.Show<LoginWindow>();
            }
        }
        else
        {
            Debug.LogWarning($"RegisterMediator: регистрация не удалась → {message}");
        }
    }

    private void HandleBackClicked()
    {
        _uiManager.Show<LoginWindow>();
    }
}
