using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ProfileWindow : BaseWindow
{
    [SerializeField] private TMP_Text _nickNameText;
    [SerializeField] private TMP_Text _firstNameText;
    [SerializeField] private TMP_Text _lastNameText;
    [SerializeField] private TMP_Text _registrationDateText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _expText;
    [SerializeField] private TMP_Text _idText;
    [SerializeField] private TMP_Text _strengthText;
    [SerializeField] private TMP_Text _intelligenceText;
    [SerializeField] private TMP_Text _agilityText;
    [SerializeField] private Button _strengthUpgradeBtn;
    [SerializeField] private Button _intelligenceUpgradeBtn;
    [SerializeField] private Button _agilityUpgradeBtn;

    [Header("Прогресс-бары")]
    [SerializeField] private Image _strengthBar;
    [SerializeField] private Image _intelligenceBar;
    [SerializeField] private Image _agilityBar;
    [SerializeField] private Image _expBar;

    [SerializeField] private Button _backButton;
    
    public event Action OnBackClicked;
    public event Action OnWindowOpened;
    public event Action<string> OnUpgradeStatClicked;

    protected override void OnShow()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        if (_strengthUpgradeBtn != null) _strengthUpgradeBtn.onClick.AddListener(() => OnUpgradeStatClicked?.Invoke("strength"));
        if (_intelligenceUpgradeBtn != null) _intelligenceUpgradeBtn.onClick.AddListener(() => OnUpgradeStatClicked?.Invoke("intelligence"));
        if (_agilityUpgradeBtn != null) _agilityUpgradeBtn.onClick.AddListener(() => OnUpgradeStatClicked?.Invoke("agility"));
        OnWindowOpened?.Invoke();
    }

    protected override void OnHide()
    {
        _backButton.onClick.RemoveAllListeners();
        if (_strengthUpgradeBtn != null) _strengthUpgradeBtn.onClick.RemoveAllListeners();
        if (_intelligenceUpgradeBtn != null) _intelligenceUpgradeBtn.onClick.RemoveAllListeners();
        if (_agilityUpgradeBtn != null) _agilityUpgradeBtn.onClick.RemoveAllListeners();
    }

    public void SetInfo(string[] userData)
    {
        if (_nickNameText != null) _nickNameText.text = userData.Length > 0 ? userData[0] : "";
        if (_firstNameText != null) _firstNameText.text = userData.Length > 1 ? userData[1] : "";
        if (_lastNameText != null) _lastNameText.text = userData.Length > 2 ? userData[2] : "";
        if (_levelText != null) _levelText.text = userData.Length > 3 ? userData[3] : "";
        if (_expText != null) _expText.text = userData.Length > 4 ? userData[4] : "";
        if (_registrationDateText != null) _registrationDateText.text = userData.Length > 5 && userData[5].Length >= 10 ? userData[5].Substring(0, 10) : "";
        if (_idText != null) _idText.text = userData.Length > 6 ? userData[6] : "";

        if (_expBar != null && userData.Length > 4 && int.TryParse(userData[4], out int exp))
            _expBar.fillAmount = Mathf.Clamp01(exp / 1000f);
    }

    public void SetStats(int strength, int intelligence, int agility, int statUpgradePoints)
    {
        if (_strengthText != null) _strengthText.text = strength.ToString();
        if (_intelligenceText != null) _intelligenceText.text = intelligence.ToString();
        if (_agilityText != null) _agilityText.text = agility.ToString();

        Debug.Log($"[ProfileWindow] SetStats bars: str={strength} ({_strengthBar != null}) int={intelligence} ({_intelligenceBar != null}) agi={agility} ({_agilityBar != null})");
        if (_strengthBar != null) _strengthBar.fillAmount = Mathf.Clamp01(strength / 10f);
        if (_intelligenceBar != null) _intelligenceBar.fillAmount = Mathf.Clamp01(intelligence / 10f);
        if (_agilityBar != null) _agilityBar.fillAmount = Mathf.Clamp01(agility / 10f);

        bool canUpgrade = statUpgradePoints > 0;
        if (_strengthUpgradeBtn != null) _strengthUpgradeBtn.gameObject.SetActive(canUpgrade);
        if (_intelligenceUpgradeBtn != null) _intelligenceUpgradeBtn.gameObject.SetActive(canUpgrade);
        if (_agilityUpgradeBtn != null) _agilityUpgradeBtn.gameObject.SetActive(canUpgrade);
    }
}
