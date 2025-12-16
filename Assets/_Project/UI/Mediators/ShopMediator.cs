using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Zenject;

public class ShopMediator : IInitializable, IDisposable
{
    private readonly ShopWindow _shopWindow;
    private readonly UIManager _uiManager;
    private readonly APIService _apiService;
    private readonly ShopItemView.Factory _shopItemViewFactory;
    private readonly IImageLoadService _imageLoadService;
    private readonly UserDataService _userDataService;
    
    private List<ShopItemView> _shopItemViews = new List<ShopItemView>();

    public ShopMediator(ShopWindow shopWindow, UIManager uiManager, APIService apiService, ShopItemView.Factory shopItemViewFactory, IImageLoadService imageLoadService, UserDataService userDataService)
    {
        _shopWindow = shopWindow;
        _uiManager = uiManager;
        _apiService = apiService;
        _shopItemViewFactory = shopItemViewFactory;
        _imageLoadService = imageLoadService;
        _userDataService = userDataService;
    }
    
    public void Initialize()
    {
        _shopWindow.OnWindowOpened += CreateShopItems;
        _shopWindow.OnBackClicked += HandleBackClicked;
    }

    public void Dispose()
    {
        _shopWindow.OnWindowOpened -= CreateShopItems;
        _shopWindow.OnBackClicked -= HandleBackClicked;
    }

    private void HandleBackClicked()
    {
        DestroyShopItems();
        _uiManager.Back();
    }
    

    private async void CreateShopItems()
    {
        _shopWindow.UpdateCoinsDisplay(_userDataService.Coins);
        
        await LoadPlayerStats();
        
        DestroyShopItems();
        var (success, message) = await _apiService.GetShopitemsList();
        ShopApiResponse response = JsonConvert.DeserializeObject<ShopApiResponse>(message);

        for (int i = 0; i < response.TotalCount; i++)
        {
            ShopItemView sItem = _shopItemViewFactory.Create();

            int tempID = response.Items[i].Id;
            
            sItem.transform.SetParent(_shopWindow.ContentParent, false);
            
            sItem.BuyButton.onClick.AddListener( () => _apiService.BuyShopItem(tempID) );
            
            _shopItemViews.Add(sItem);
            _shopItemViews[i].Init(response.Items[i].Name, response.Items[i].Description, response.Items[i].Price.ToString());
            
            _ = LoadAndApplyImageAsync(sItem, response.Items[i].ImageUrl);
        }
    }
    
    private async Task LoadPlayerStats()
    {
        var (success, response) = await _apiService.GetPlayerCoins();
        
        if (success && response != null)
        {
            _shopWindow.UpdateCoinsDisplay(response.coins);
            _userDataService.UpdateCoins(response.coins);
            Debug.Log($"[ShopMediator] Player coins updated: {response.coins}");
        }
        else
        {
            Debug.LogWarning("[ShopMediator] Failed to load coins from server, using local data");
        }
    }
    
    private async Task LoadAndApplyImageAsync(ShopItemView view, string imageUrl)
    {
        Sprite sprite = await _imageLoadService.LoadSpriteAsync(imageUrl);
        
        if (sprite == null || view == null)
            return;

        view.SetImage(sprite);
    }


    private void DestroyShopItems()
    {
        foreach (var VARIABLE in _shopItemViews)
        {
            GameObject.Destroy(VARIABLE.gameObject);
        }
        _shopItemViews.Clear();
    }
}
public class ShopItem
{
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("description")]
    public string Description { get; set; }
    
    [JsonProperty("image_url")]
    public string ImageUrl { get; set; }
    
    [JsonProperty("price")]
    public float Price { get; set; }
    
    [JsonProperty("is_active")]
    public bool IsActive { get; set; }
    
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class ShopApiResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonProperty("items")]
    public List<ShopItem> Items { get; set; }
    
    [JsonProperty("total_count")]
    public int TotalCount { get; set; }
}