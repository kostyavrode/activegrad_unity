using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSearchWindow : BaseWindow
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Button _searchButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private Transform _resultContainer;
    
    public Transform ResultContainer => _resultContainer;
    public string InputFieldData => _inputField.text;
    
    public event Action OnBackClicked;
    public event Action OnSearchClicked;

    protected override void OnShow()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        _searchButton.onClick.AddListener(() => OnSearchClicked?.Invoke());
    }

    protected override void OnHide()
    {
        _backButton.onClick.RemoveAllListeners();
        _searchButton.onClick.RemoveAllListeners();
    }
}
