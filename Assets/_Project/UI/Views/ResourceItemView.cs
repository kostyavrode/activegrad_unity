using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Универсальный виджет отображения ресурса. Создаётся динамически для каждого типа ресурса.
/// </summary>
public class ResourceItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _amountText;
    [SerializeField] private Image _iconImage;

    public void Init(string displayName, int amount, Sprite icon = null)
    {
        if (_nameText != null)
            _nameText.text = displayName ?? "";
        if (_amountText != null)
            _amountText.text = amount.ToString();
        if (_iconImage != null && icon != null)
            _iconImage.sprite = icon;
    }

    public void SetAmount(int amount)
    {
        if (_amountText != null)
            _amountText.text = amount.ToString();
    }

    public class Factory : Zenject.PlaceholderFactory<ResourceItemView> { }
}
