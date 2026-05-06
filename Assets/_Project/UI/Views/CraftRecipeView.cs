using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Виджет рецепта крафта.
/// Автоматически отображает иконки и количество ресурсов из слотов, заданных в инспекторе.
/// Каждый ResourceSlot привязан к конкретному resourceId (например "wood", "metal").
/// </summary>
public class CraftRecipeView : MonoBehaviour
{
    [Serializable]
    public struct ResourceSlot
    {
        [Tooltip("id ресурса с сервера: wood, metal, blueprints и т.д.")]
        public string resourceId;
        public Image iconImage;
        public TMP_Text amountText;
    }

    [SerializeField] private TMP_Text _itemNameText;
    [SerializeField] private Button _craftButton;
    [SerializeField] private TMP_Text _buttonLabel;

    [Header("Слоты ресурсов")]
    [Tooltip("Заполни resourceId для каждого слота и привяжи нужные Image и TMP_Text из префаба")]
    [SerializeField] private ResourceSlot[] _resourceSlots;

    [Header("Состояние 'уже скрафчено' / макс. уровень")]
    [Tooltip("GameObject, который показывается когда предмет уже скрафчен или достигнут макс. уровень")]
    [SerializeField] private GameObject _craftedBadge;
    [SerializeField] private TMP_Text _craftedBadgeText;

    private string _itemId;

    public string ItemId => _itemId;

    public event Action<string> OnCraftClicked;

    private void Awake()
    {
        if (_craftButton != null)
            _craftButton.onClick.AddListener(() => OnCraftClicked?.Invoke(_itemId));
    }

    private void OnDestroy()
    {
        if (_craftButton != null)
            _craftButton.onClick.RemoveAllListeners();
    }

    public void Init(
        string itemId,
        string displayName,
        APIService.InventoryRequirementData[] requirements,
        IconConfig iconConfig,
        bool canCraft,
        bool isCrafted = false,
        string buttonLabel = null,
        string badgeText = null)
    {
        _itemId = itemId ?? "";

        if (_itemNameText != null)
            _itemNameText.text = displayName ?? "";

        ApplyResourceSlots(requirements, iconConfig);

        bool showBadge = isCrafted && requirements == null;
        bool showButton = !showBadge;

        if (_craftButton != null)
            _craftButton.gameObject.SetActive(showButton);

        if (_craftedBadge != null)
            _craftedBadge.SetActive(showBadge);

        if (showBadge && _craftedBadgeText != null && badgeText != null)
            _craftedBadgeText.text = badgeText;

        if (showButton)
        {
            if (_buttonLabel != null)
                _buttonLabel.text = buttonLabel ?? "Создать";

            SetInteractable(canCraft);
        }
    }

    public void SetInteractable(bool canCraft)
    {
        if (_craftButton != null)
            _craftButton.interactable = canCraft;
    }

    /// <summary>
    /// Для каждого слота ищет совпадение по resourceId среди требований рецепта.
    /// Если нашёл — ставит иконку из IconConfig и количество. Если нет — скрывает слот.
    /// </summary>
    private void ApplyResourceSlots(APIService.InventoryRequirementData[] requirements, IconConfig iconConfig)
    {
        if (_resourceSlots == null) return;

        foreach (var slot in _resourceSlots)
        {
            var requirement = FindRequirement(requirements, slot.resourceId);
            bool hasRequirement = requirement != null;

            // Показываем/скрываем иконку
            if (slot.iconImage != null)
            {
                slot.iconImage.gameObject.SetActive(hasRequirement);
                if (hasRequirement)
                {
                    var sprite = iconConfig?.GetSprite(slot.resourceId);
                    if (sprite != null)
                        slot.iconImage.sprite = sprite;
                }
            }

            // Показываем/скрываем количество
            if (slot.amountText != null)
            {
                slot.amountText.gameObject.SetActive(hasRequirement);
                if (hasRequirement)
                    slot.amountText.text = requirement.amount.ToString();
            }
        }
    }

    private static APIService.InventoryRequirementData FindRequirement(
        APIService.InventoryRequirementData[] requirements,
        string resourceId)
    {
        if (requirements == null || string.IsNullOrEmpty(resourceId)) return null;

        foreach (var req in requirements)
        {
            if (req != null && req.resource_id == resourceId)
                return req;
        }

        return null;
    }

    public class Factory : Zenject.PlaceholderFactory<CraftRecipeView> { }
}
