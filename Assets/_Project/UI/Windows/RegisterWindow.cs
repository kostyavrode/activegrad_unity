using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegisterWindow : BaseWindow
{
    [SerializeField] private TMP_InputField loginInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button backButton;

    public event Action<string, string> OnRegisterClicked;
    public event Action OnBackClicked;

    protected override void OnShow()
    {
        registerButton.onClick.AddListener(HandleRegisterClicked);
        backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
    }

    protected override void OnHide()
    {
        registerButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
    }

    private void HandleRegisterClicked()
    {
        OnRegisterClicked?.Invoke(loginInput.text, passwordInput.text);
    }
}