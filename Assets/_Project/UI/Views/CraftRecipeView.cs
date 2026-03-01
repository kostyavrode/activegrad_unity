using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Универсальный виджет рецепта крафта. Отображает предмет, требования и кнопку крафта.
/// Кнопка активна только когда ресурсов достаточно.
/// </summary>
public class CraftRecipeView : MonoBehaviour
{
    [SerializeField] private TMP_Text _itemNameText;
    [SerializeField] private TMP_Text _requirementsText;
    [SerializeField] private Button _craftButton;

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

    public void Init(string itemId, string displayName, string requirementsText, bool canCraft)
    {
        _itemId = itemId ?? "";

        if (_itemNameText != null)
            _itemNameText.text = displayName ?? "";

        if (_requirementsText != null)
            _requirementsText.text = requirementsText ?? "";

        SetInteractable(canCraft);
    }

    public void SetInteractable(bool canCraft)
    {
        if (_craftButton != null)
            _craftButton.interactable = canCraft;
    }

    public class Factory : Zenject.PlaceholderFactory<CraftRecipeView> { }
}
