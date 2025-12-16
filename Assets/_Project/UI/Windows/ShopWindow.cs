using System;
using UnityEngine;
using UnityEngine.UI;

public class ShopWindow : BaseWindow
{
    [SerializeField] private Transform _contentParent;
    [SerializeField] private Button _backButton;
    
    public Transform ContentParent => _contentParent;
    
    public event Action OnBackClicked;
    public event Action OnWindowOpened;
    
    protected override void OnShow()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        OnWindowOpened?.Invoke();
    }
    
    protected override void OnHide()
    {
        _backButton.onClick.RemoveAllListeners();
    }
}
