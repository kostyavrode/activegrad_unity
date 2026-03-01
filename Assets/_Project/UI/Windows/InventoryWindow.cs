using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Окно инвентаря с универсальной структурой.
/// ResourcesContainer — контейнер для виджетов ресурсов (создаются динамически).
/// CraftingPanel — подокно с CraftRecipesContainer для виджетов рецептов.
/// </summary>
public class InventoryWindow : BaseWindow
{
    [Header("Containers")]
    [Tooltip("Контейнер для ресурсов и предметов: сначала ResourceItemView, затем InventoryItemView (меч, щит)")]
    [SerializeField] private Transform _resourcesContainer;

    [Tooltip("Подокно крафта: контейнер для виджетов рецептов (CraftRecipeView)")]
    [SerializeField] private Transform _craftRecipesContainer;

    [Header("Buttons")]
    [SerializeField] private Button _backButton;

    public Transform ResourcesContainer => _resourcesContainer;
    public Transform CraftRecipesContainer => _craftRecipesContainer;

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
