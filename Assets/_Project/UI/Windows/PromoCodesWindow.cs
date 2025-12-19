using System;
using UnityEngine;
using UnityEngine.UI;

public class PromoCodesWindow : BaseWindow
{
    
    [SerializeField] private Button _backButton;
    [SerializeField] private Transform _content;
    
    public Transform ResultContent => _content;
    
    public event Action OnBackClicked;
    public event Action OnWindowOpened;
    public event Action OnWindowClosed;

    protected override void OnShow()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        OnWindowOpened?.Invoke();
    }

    protected override void OnHide()
    {
        OnWindowClosed?.Invoke();
        _backButton.onClick.RemoveAllListeners();
    }
}
