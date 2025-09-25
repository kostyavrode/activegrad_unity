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

    private async void HandleRegisterClicked(string login, string password)
    {
        Debug.Log("RegisterMediator: попытка регистрации...");

        var (success, message) = await _apiService.Register(login, firstName: login, lastName: "", password);

        if (success)
        {
            Debug.Log("RegisterMediator: регистрация успешна!");
            var loginSuccess = await _apiService.Login(login, password);

            if (loginSuccess)
            {
                Debug.Log("RegisterMediator: вход после регистрации успешен!");
                // 👉 вместо MenuWindow открываем кастомизацию персонажа
                _uiManager.Show<CharacterCustomizationWindow>();
            }
            else
            {
                Debug.LogWarning("RegisterMediator: зарегистрировались, но не смогли войти");
                // ⚠️ можно кинуть попап вместо возврата
                _uiManager.Show<LoginWindow>();
            }
        }
        else
        {
            Debug.LogWarning($"RegisterMediator: регистрация не удалась → {message}");
            // Тут показываешь попап с message
        }
    }

    private void HandleBackClicked()
    {
        Debug.Log("RegisterMediator: возврат к окну логина");
        _uiManager.Show<LoginWindow>();
    }
}
