using Zenject;
using UnityEngine;
using System;

public class LoginMediator : IInitializable, IDisposable
{
    private readonly LoginWindow _loginWindow;
    private readonly APIService _apiService;
    private readonly UIManager _uiManager;

    public LoginMediator(LoginWindow loginWindow, APIService apiService, UIManager uiManager)
    {
        _loginWindow = loginWindow;
        _apiService = apiService;
        _uiManager = uiManager;
    }

    public void Initialize()
    {
        _loginWindow.OnLoginClicked += HandleLoginClicked;
        _loginWindow.OnRegisterClicked += HandleRegisterClicked;
    }

    public void Dispose()
    {
        _loginWindow.OnLoginClicked -= HandleLoginClicked;
        _loginWindow.OnRegisterClicked -= HandleRegisterClicked;
    }

    private async void HandleLoginClicked(string login, string password)
    {
        Debug.Log("LoginWindow.HandleLoginClicked pressed");
        bool success = await _apiService.Login(login, password);

//        Debug.Log("LoginWindow.HandleLoginClicked pressed");
        if (success)
        {
            Debug.Log("LoginMediator: вход успешен!");
            _uiManager.Show<MenuWindow>();
        }
        else
        {
            Debug.LogWarning("LoginMediator: неверный логин/пароль");
            // можно показать popup с ошибкой
        }
    }

    private void HandleRegisterClicked()
    {
        Debug.Log("LoginMediator: переход к окну регистрации");
        _uiManager.Show<RegisterWindow>();
    }
}