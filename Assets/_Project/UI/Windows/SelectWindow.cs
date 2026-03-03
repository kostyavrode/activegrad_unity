using System;
using UnityEngine;
using UnityEngine.UI;

public class SelectWindow : BaseWindow
{
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _playerSearchButton;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _promoCodesButton;
    [SerializeField] private Button _friendsButton;
    [SerializeField] private Button _clansButton;
    [SerializeField] private Button _inventoryButton;

    public event Action OnBackClicked;
    public event Action OnPlayerSearchClicked;
    public event Action OnShopClicked;
    public event Action OnPromoCodesClicked;
    public event Action OnFriendsClicked;
    public event Action OnClansClicked;
    public event Action OnInventoryClicked;

    protected override void OnShow()
    {
        if (_backButton != null) _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        if (_playerSearchButton != null) _playerSearchButton.onClick.AddListener(() => OnPlayerSearchClicked?.Invoke());
        if (_shopButton != null) _shopButton.onClick.AddListener(() => OnShopClicked?.Invoke());
        if (_promoCodesButton != null) _promoCodesButton.onClick.AddListener(() => OnPromoCodesClicked?.Invoke());
        if (_friendsButton != null) _friendsButton.onClick.AddListener(() => OnFriendsClicked?.Invoke());
        if (_clansButton != null) _clansButton.onClick.AddListener(() => OnClansClicked?.Invoke());
        if (_inventoryButton != null) _inventoryButton.onClick.AddListener(() => OnInventoryClicked?.Invoke());
    }

    protected override void OnHide()
    {
        if (_backButton != null) _backButton.onClick.RemoveAllListeners();
        if (_playerSearchButton != null) _playerSearchButton.onClick.RemoveAllListeners();
        if (_shopButton != null) _shopButton.onClick.RemoveAllListeners();
        if (_promoCodesButton != null) _promoCodesButton.onClick.RemoveAllListeners();
        if (_friendsButton != null) _friendsButton.onClick.RemoveAllListeners();
        if (_clansButton != null) _clansButton.onClick.RemoveAllListeners();
        if (_inventoryButton != null) _inventoryButton.onClick.RemoveAllListeners();
    }
}
