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

    private CountUpText _counter;

    private CountUpText Counter
    {
        get
        {
            if (_counter == null && _amountText != null)
            {
                _counter = _amountText.GetComponent<CountUpText>();
                if (_counter == null)
                    _counter = _amountText.gameObject.AddComponent<CountUpText>();
            }
            return _counter;
        }
    }

    public void Init(string displayName, int amount, Sprite icon = null)
    {
        if (_nameText != null)
            _nameText.text = displayName ?? "";
        if (_amountText != null)
        {
            _amountText.text = amount.ToString();
            Counter.SetValue(amount, false);
        }
        if (_iconImage != null && icon != null)
            _iconImage.sprite = icon;
    }

    public void SetAmount(int amount)
    {
        if (_amountText == null)
            return;

        var counter = Counter;
        // Анимация (счёт + punch) только при росте, уменьшение — мгновенно
        counter.SetValue(amount, amount > counter.Value);
    }

    public class Factory : Zenject.PlaceholderFactory<ResourceItemView> { }
}
