using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

public class MenuWindow : BaseWindow
{
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _profileButton;
    [SerializeField] private Button _questButton;
    [SerializeField] private Button _sightButton;
    [SerializeField] private Button _searchPlayerButton;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _promoCodesButton;
    [SerializeField] private Button _friendsButton;
    [SerializeField] private Button _clansButton;
    [SerializeField] private Button _inventoryButton;
    
    public event Action OnProfileClicked;
    public event Action OnSettingsClicked;
    public event Action OnQuestsClicked;
    public event Action OnSightClicked;
    public event Action OnSearchPlayerClicked;
    public event Action OnShopClicked;
    public event Action OnPromoCodesClicked;
    public event Action OnFriendsClicked;
    public event Action OnClansClicked;
    public event Action OnInventoryClicked;

    protected override void OnShow()
    {
        _settingsButton.onClick.AddListener(() => OnSettingsClicked?.Invoke());
        _profileButton.onClick.AddListener(() => OnProfileClicked?.Invoke());
        _questButton.onClick.AddListener(() => OnQuestsClicked?.Invoke());
        _sightButton.onClick.AddListener(() => OnSightClicked?.Invoke());
        _searchPlayerButton.onClick.AddListener(() => OnSearchPlayerClicked?.Invoke());
        _shopButton.onClick.AddListener(() => OnShopClicked?.Invoke());
        _promoCodesButton.onClick.AddListener(() => OnPromoCodesClicked?.Invoke());
        _friendsButton.onClick.AddListener(() => OnFriendsClicked?.Invoke());
        _clansButton.onClick.AddListener(() => OnClansClicked?.Invoke());
        if (_inventoryButton != null) _inventoryButton.onClick.AddListener(() => OnInventoryClicked?.Invoke());
    }

    protected override void OnHide()
    {
        _settingsButton.onClick.RemoveAllListeners();
        _profileButton.onClick.RemoveAllListeners();
        _questButton.onClick.RemoveAllListeners();
        _sightButton.onClick.RemoveAllListeners();
        _searchPlayerButton.onClick.RemoveAllListeners();
        _settingsButton.onClick.RemoveAllListeners();
        _shopButton.onClick.RemoveAllListeners();
        _promoCodesButton.onClick.RemoveAllListeners();
        _friendsButton.onClick.RemoveAllListeners();
        _clansButton.onClick.RemoveAllListeners();
        if (_inventoryButton != null) _inventoryButton.onClick.RemoveAllListeners();
    }
}
