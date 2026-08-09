using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class InventoryItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _statText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _progressBar;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TMP_Text _upgradeCostText;

    public event Action<string> OnUpgradeClicked;

    private string _itemId;

    private void Awake()
    {
        if (_upgradeButton != null)
            _upgradeButton.onClick.AddListener(() => OnUpgradeClicked?.Invoke(_itemId));
    }

    private void OnDestroy()
    {
        if (_upgradeButton != null)
            _upgradeButton.onClick.RemoveAllListeners();

        if (_progressBar != null)
            UIProgressBarHelper.Kill(_progressBar);
    }

    public void Init(string itemId, string displayName, string statInfo, Sprite icon = null)
    {
        _itemId = itemId;

        if (_nameText != null)
            _nameText.text = displayName ?? "";

        if (_statText != null)
        {
            _statText.text = statInfo ?? "";
            _statText.gameObject.SetActive(!string.IsNullOrEmpty(statInfo));
            UpdateProgressBar(statInfo, instant: true);
        }

        if (_iconImage != null && icon != null)
            _iconImage.sprite = icon;

        SetUpgradeInfo(null);
    }

    public void SetUpgradeInfo(APIService.UpgradeCostEntry cost)
    {
        if (_upgradeButton == null) return;

        if (cost == null || !cost.can_upgrade)
        {
            _upgradeButton.gameObject.SetActive(false);
            return;
        }

        _upgradeButton.gameObject.SetActive(true);

        if (_upgradeCostText != null && cost.requirements != null)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var req in cost.requirements)
                parts.Add($"{req.display_name}: {req.amount}");
            _upgradeCostText.text = string.Join("  ", parts);
        }
    }

    private void UpdateProgressBar(string statInfo, bool instant = false)
    {
        if (_progressBar == null) return;

        if (!string.IsNullOrEmpty(statInfo))
        {
            Match match = Regex.Match(statInfo, @"\d+([,.]\d+)?");
            if (match.Success && float.TryParse(match.Value.Replace('.', ','), out float value))
            {
                value = Mathf.Clamp(value, 0f, 10f);
                UIProgressBarHelper.SetFillAmount(_progressBar, value / 10f, instant: instant);
                _progressBar.gameObject.SetActive(true);
                _levelText.text = "Уровень: " + ((int)value).ToString();
                return;
            }
        }

        _progressBar.gameObject.SetActive(false);
    }

    public class Factory : Zenject.PlaceholderFactory<InventoryItemView> { }
}
