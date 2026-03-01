using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Виджет отображения предмета в инвентаре (меч, щит и т.д.).
/// Показывает название и характеристику (острота, стойкость).
/// </summary>
public class InventoryItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _statText;
    [SerializeField] private Image _iconImage;

    public void Init(string displayName, string statInfo, Sprite icon = null)
    {
        if (_nameText != null)
            _nameText.text = displayName ?? "";

        if (_statText != null)
        {
            _statText.text = statInfo ?? "";
            _statText.gameObject.SetActive(!string.IsNullOrEmpty(statInfo));
        }

        if (_iconImage != null && icon != null)
            _iconImage.sprite = icon;
    }

    public class Factory : Zenject.PlaceholderFactory<InventoryItemView> { }
}
