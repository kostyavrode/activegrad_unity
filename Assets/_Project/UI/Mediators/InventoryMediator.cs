using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

public class InventoryMediator : IInitializable, IDisposable
{
    private readonly InventoryWindow _inventoryWindow;
    private readonly UIManager _uiManager;
    private readonly APIService _apiService;
    private readonly IInventoryService _inventoryService;
    private readonly IPopupService _popupService;
    private readonly ResourceItemView.Factory _resourceItemFactory;
    private readonly InventoryItemView.Factory _inventoryItemFactory;
    private readonly CraftRecipeView.Factory _craftRecipeFactory;

    private readonly List<ResourceItemView> _resourceViews = new List<ResourceItemView>();
    private readonly List<InventoryItemView> _itemViews = new List<InventoryItemView>();
    private readonly List<CraftRecipeView> _recipeViews = new List<CraftRecipeView>();

    private APIService.InventoryRecipeData[] _recipes = Array.Empty<APIService.InventoryRecipeData>();
    private APIService.InventoryResourceData[] _lastResources = Array.Empty<APIService.InventoryResourceData>();
    private APIService.InventoryItemData[] _lastItems = Array.Empty<APIService.InventoryItemData>();

    public InventoryMediator(
        InventoryWindow inventoryWindow,
        UIManager uiManager,
        APIService apiService,
        IInventoryService inventoryService,
        IPopupService popupService,
        ResourceItemView.Factory resourceItemFactory,
        InventoryItemView.Factory inventoryItemFactory,
        CraftRecipeView.Factory craftRecipeFactory)
    {
        _inventoryWindow = inventoryWindow;
        _uiManager = uiManager;
        _apiService = apiService;
        _inventoryService = inventoryService;
        _popupService = popupService;
        _resourceItemFactory = resourceItemFactory;
        _inventoryItemFactory = inventoryItemFactory;
        _craftRecipeFactory = craftRecipeFactory;
    }

    public void Initialize()
    {
        _inventoryWindow.OnBackClicked += HandleBackClicked;
        _inventoryWindow.OnWindowOpened += HandleWindowOpened;
    }

    public void Dispose()
    {
        _inventoryWindow.OnBackClicked -= HandleBackClicked;
        _inventoryWindow.OnWindowOpened -= HandleWindowOpened;

        foreach (var view in _recipeViews)
        {
            if (view != null)
                view.OnCraftClicked -= HandleCraftClicked;
        }
    }

    private void HandleBackClicked()
    {
        _uiManager.Back();
    }

    private async void HandleWindowOpened()
    {
        await LoadAndRefresh();
    }

    private async Task LoadAndRefresh()
    {
        var (invSuccess, invResponse) = await _apiService.GetInventory();
        if (invSuccess && invResponse != null)
        {
            ApplyInventoryResponse(invResponse);
        }
        else
        {
            Debug.LogWarning("[InventoryMediator] Failed to load inventory");
        }

        var (recipesSuccess, recipesResponse) = await _apiService.GetInventoryRecipes();
        if (recipesSuccess && recipesResponse?.recipes != null)
        {
            _recipes = recipesResponse.recipes;
        }
        else
        {
            _recipes = Array.Empty<APIService.InventoryRecipeData>();
        }

        RefreshUI();
    }

    private void ApplyInventoryResponse(APIService.InventoryResponse response)
    {
        if (response.resources != null)
        {
            _lastResources = response.resources;
            _inventoryService.SetResources(response.resources);
        }

        if (response.items != null)
        {
            _lastItems = response.items;
            _inventoryService.SetItems(response.items);
        }

        if (response.inventory != null)
        {
            _inventoryService.ApplyInventoryData(response.inventory);
            if (response.inventory.resources != null)
                _lastResources = response.inventory.resources;
            if (response.inventory.items != null)
                _lastItems = response.inventory.items;
        }
    }

    private void RefreshUI()
    {
        RefreshResources();
        RefreshRecipes();
    }

    private void RefreshResources()
    {
        foreach (var v in _resourceViews)
        {
            if (v != null && v.gameObject != null)
                UnityEngine.Object.Destroy(v.gameObject);
        }
        _resourceViews.Clear();

        foreach (var v in _itemViews)
        {
            if (v != null && v.gameObject != null)
                UnityEngine.Object.Destroy(v.gameObject);
        }
        _itemViews.Clear();

        var container = _inventoryWindow.ResourcesContainer;
        if (container == null) return;

        foreach (var r in _lastResources)
        {
            if (r == null) continue;
            var view = _resourceItemFactory.Create();
            view.transform.SetParent(container, false);
            view.Init(r.display_name ?? r.id, r.amount);
            _resourceViews.Add(view);
        }

        foreach (var item in _lastItems)
        {
            if (item == null || !item.has_item) continue;

            string statInfo = null;
            if (item.sharpness.HasValue)
                statInfo = $"Острота: {item.sharpness.Value}";
            else if (item.durability.HasValue)
                statInfo = $"Стойкость: {item.durability.Value}";

            var view = _inventoryItemFactory.Create();
            view.transform.SetParent(container, false);
            view.Init(item.display_name ?? item.id, statInfo);
            _itemViews.Add(view);
        }
    }

    private void RefreshRecipes()
    {
        foreach (var view in _recipeViews)
        {
            if (view != null)
            {
                view.OnCraftClicked -= HandleCraftClicked;
                if (view.gameObject != null)
                    UnityEngine.Object.Destroy(view.gameObject);
            }
        }
        _recipeViews.Clear();

        var container = _inventoryWindow.CraftRecipesContainer;
        if (container == null) return;

        foreach (var recipe in _recipes)
        {
            if (recipe == null) continue;
            if (_inventoryService.HasItem(recipe.id)) continue;

            var requirementsStr = BuildRequirementsText(recipe.requirements);
            var canCraft = CanCraft(recipe);

            var view = _craftRecipeFactory.Create();
            view.transform.SetParent(container, false);
            view.Init(recipe.id, recipe.display_name ?? recipe.id, requirementsStr, canCraft);
            view.OnCraftClicked += HandleCraftClicked;
            _recipeViews.Add(view);
        }
    }

    private string BuildRequirementsText(APIService.InventoryRequirementData[] requirements)
    {
        if (requirements == null || requirements.Length == 0)
            return "";

        var parts = requirements
            .Where(r => r != null)
            .Select(r => $"{r.display_name ?? r.resource_id}: {r.amount}");
        return string.Join(", ", parts);
    }

    private bool CanCraft(APIService.InventoryRecipeData recipe)
    {
        if (recipe.requirements == null) return false;
        if (_inventoryService.HasItem(recipe.id)) return false;

        foreach (var req in recipe.requirements)
        {
            if (req == null) continue;
            var have = _inventoryService.GetResourceAmount(req.resource_id);
            if (have < req.amount)
                return false;
        }
        return true;
    }

    private async void HandleCraftClicked(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        var (success, response) = await _apiService.CraftItem(itemId);
        if (success && response?.success == true)
        {
            _popupService.ShowSuccess(response.message ?? "Предмет скрафчен!");
            if (response.inventory != null)
            {
                _inventoryService.ApplyInventoryData(response.inventory);
                if (response.inventory.resources != null)
                    _lastResources = response.inventory.resources;
                if (response.inventory.items != null)
                    _lastItems = response.inventory.items;
            }
            await LoadAndRefresh();
        }
        else
        {
            _popupService.ShowError("Не удалось скрафтить. Недостаточно ресурсов или предмет уже есть.");
        }
    }
}
