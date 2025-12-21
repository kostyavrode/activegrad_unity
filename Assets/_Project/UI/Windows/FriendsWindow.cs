using System;
using UnityEngine;
using UnityEngine.UI;

public class FriendsWindow : BaseWindow
{
    [SerializeField] private Button _backButton;
    [SerializeField] private Transform _content;
    [SerializeField] private Button _friendsTabButton;
    [SerializeField] private Button _pendingTabButton;
    [SerializeField] private Button _sentTabButton;
    
    public Transform Content => _content;
    
    public event Action OnBackClicked;
    public event Action OnWindowOpened;
    public event Action OnFriendsTabClicked;
    public event Action OnPendingTabClicked;
    public event Action OnSentTabClicked;
    
    protected override void OnShow()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        _friendsTabButton.onClick.AddListener(() => OnFriendsTabClicked?.Invoke());
        _pendingTabButton.onClick.AddListener(() => OnPendingTabClicked?.Invoke());
        _sentTabButton.onClick.AddListener(() => OnSentTabClicked?.Invoke());
        OnWindowOpened?.Invoke();
    }
    
    protected override void OnHide()
    {
        OnWindowOpened = null;
        _backButton.onClick.RemoveAllListeners();
        _friendsTabButton.onClick.RemoveAllListeners();
        _pendingTabButton.onClick.RemoveAllListeners();
        _sentTabButton.onClick.RemoveAllListeners();
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
}

