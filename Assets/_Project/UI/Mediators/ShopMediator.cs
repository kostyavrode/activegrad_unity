using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Zenject;

public class ShopMediator : IInitializable, IDisposable
{
    private readonly ShopWindow _shopWindow;
    private readonly UIManager _uiManager;
    private readonly APIService _apiService;
    private readonly ShopItemView.Factory _shopItemViewFactory;
    
    private List<ShopItemView> _shopItemViews = new List<ShopItemView>();

    public ShopMediator(ShopWindow shopWindow, UIManager uiManager, APIService apiService, ShopItemView.Factory shopItemViewFactory)
    {
        _shopWindow = shopWindow;
        _uiManager = uiManager;
        _apiService = apiService;
        _shopItemViewFactory = shopItemViewFactory;
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
        DestroyShopItems();
        var (success, message) = await _apiService.GetShopitemsList();
        ShopApiResponse response = JsonConvert.DeserializeObject<ShopApiResponse>(message);

        for (int i = 0; i < response.TotalCount; i++)
        {
            ShopItemView sItem = _shopItemViewFactory.Create();

            int tempID = response.Items[i].Id;
            
            sItem.transform.SetParent(_shopWindow.ContentParent, false);
            
            sItem.BuyButton.onClick.AddListener( () => _apiService.BuyShopItem(tempID) );
            
            Debug.Log(tempID);
            
            _shopItemViews.Add(sItem);
            _shopItemViews[i].Init(response.Items[i].Name, response.Items[i].Description, response.Items[i].Price.ToString());
        }
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