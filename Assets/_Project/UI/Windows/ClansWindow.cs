 using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClansWindow : BaseWindow
{
    [SerializeField] private Button _backButton;
    [SerializeField] private Transform _content;
    [SerializeField] private Button _topClansTabButton;
    [SerializeField] private Button _searchTabButton;
    [SerializeField] private Button _myClanTabButton;
    [SerializeField] private Button _createClanTabButton;
    [SerializeField] private GameObject _searchPanel;
    [SerializeField] private TMP_InputField _searchInputField;
    [SerializeField] private Button _searchButton;
    
    public Transform Content => _content;
    public TMP_InputField SearchInputField => _searchInputField;
    
    public event Action OnBackClicked;
    public event Action OnWindowOpened;
    public event Action OnTopClansTabClicked;
    public event Action OnSearchTabClicked;
    public event Action OnMyClanTabClicked;
    public event Action OnCreateClanTabClicked;
    public event Action<string> OnSearchClicked;
    
    protected override void OnShow()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        _topClansTabButton.onClick.AddListener(() => OnTopClansTabClicked?.Invoke());
        _searchTabButton.onClick.AddListener(() => OnSearchTabClicked?.Invoke());
        _myClanTabButton.onClick.AddListener(() => OnMyClanTabClicked?.Invoke());
        _createClanTabButton.onClick.AddListener(() => OnCreateClanTabClicked?.Invoke());
        
        if (_searchButton != null)
        {
            _searchButton.onClick.AddListener(() => OnSearchClicked?.Invoke(_searchInputField?.text ?? ""));
        }
        
        OnWindowOpened?.Invoke();
    }
    
    protected override void OnHide()
    {
        OnWindowOpened = null;
        _backButton.onClick.RemoveAllListeners();
        _topClansTabButton.onClick.RemoveAllListeners();
        _searchTabButton.onClick.RemoveAllListeners();
        _myClanTabButton.onClick.RemoveAllListeners();
        _createClanTabButton.onClick.RemoveAllListeners();
        
        if (_searchButton != null)
        {
            _searchButton.onClick.RemoveAllListeners();
        }
        
        ClearContent();
    }
    
    public void ClearContent()
    {
        if (_content == null) return;
        
        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            Destroy(_content.GetChild(i).gameObject);
        }
    }
    
    public void ShowSearchPanel(bool show)
    {
        if (_searchPanel != null)
        {
            _searchPanel.SetActive(show);
        }
    }
}

