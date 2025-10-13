using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegisterWindow : BaseWindow
{
    [SerializeField] private TMP_InputField loginInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField firstNameInput;
    [SerializeField] private TMP_InputField lastNameInput;
    
    [SerializeField] private Button registerButton;
    [SerializeField] private Button backButton;

    public event Action<string[]> OnRegisterClicked;
    public event Action OnBackClicked;

    protected override void OnShow()
    {
        registerButton.onClick.AddListener(() => OnRegisterClicked?.Invoke(CollectInfo()));
        backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
    }

    protected override void OnHide()
    {
        registerButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
    }

    private string[] CollectInfo()
    {
        string[] result = new string[4];
        
        result[0] = loginInput.text;
        result[1] = passwordInput.text;
        result[2] = firstNameInput.text;
        result[3] = lastNameInput.text;
        
        return result;
    }
}