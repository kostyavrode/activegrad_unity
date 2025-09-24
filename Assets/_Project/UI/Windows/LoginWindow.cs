using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginWindow : BaseWindow
{
    [SerializeField] private TMP_InputField loginInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;

    public event Action<string, string> OnLoginClicked;
    public event Action OnRegisterClicked;

    protected override void OnShow()
    {
        Debug.Log($"{name}.OnShow()");
        loginButton.onClick.AddListener(HandleLoginClicked);
        registerButton.onClick.AddListener(() => OnRegisterClicked?.Invoke());
    }

    protected override void OnHide()
    {
        loginButton.onClick.RemoveAllListeners();
        registerButton.onClick.RemoveAllListeners();
    }

    private void HandleLoginClicked()
    {
        OnLoginClicked?.Invoke(loginInput.text, passwordInput.text);
    }
}