using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class InventoryItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _statText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _progressBar;

    public void Init(string displayName, string statInfo, Sprite icon = null)
    {
        if (_nameText != null)
            _nameText.text = displayName ?? "";

        if (_statText != null)
        {
            _statText.text = statInfo ?? "";
            _statText.gameObject.SetActive(!string.IsNullOrEmpty(statInfo));
            
            UpdateProgressBar(statInfo);
        }

        if (_iconImage != null && icon != null)
            _iconImage.sprite = icon;
    }

    private void UpdateProgressBar(string statInfo)
    {
        if (_progressBar == null) return;
        
        if (!string.IsNullOrEmpty(statInfo))
        {
            Match match = Regex.Match(statInfo, @"\d+([,.]\d+)?");
            if (match.Success && float.TryParse(match.Value.Replace('.', ','), out float value))
            {
                value = Mathf.Clamp(value, 0f, 10f);
                _progressBar.fillAmount = value / 10f;
                _progressBar.gameObject.SetActive(true);
                return;
            }
        }
        
        _progressBar.gameObject.SetActive(false);
    }

    public class Factory : Zenject.PlaceholderFactory<InventoryItemView> { }
}