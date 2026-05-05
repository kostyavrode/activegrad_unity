using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class OtherPlayerProfileView : MonoBehaviour
{
    private const float AnimationDuration = 0.15f;

    [SerializeField] private TMP_Text _nickNameText;
    [SerializeField] private TMP_Text _firstNameText;
    [SerializeField] private TMP_Text _lastNameText;
    [SerializeField] private TMP_Text _registrationDateText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _sightsText;
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

    [SerializeField] private CanvasGroup _canvasGroup;

    [SerializeField] private Button _backButton;
    [SerializeField] private Button _sightsButton;

    private int[] _sightsID;
    private Tween _tween;

    public event Action OnBackClicked;
    public event Action OnWindowOpened;
    public event Action<int[]> OnSightsButtonClicked;

    private void Awake()
    {
        _backButton.onClick.AddListener(Close);
        _sightsButton.onClick.AddListener(() => OnSightsButtonClicked?.Invoke(_sightsID));
        OnWindowOpened?.Invoke();

        if (_strengthUpgradeBtn != null) _strengthUpgradeBtn.gameObject.SetActive(false);
        if (_intelligenceUpgradeBtn != null) _intelligenceUpgradeBtn.gameObject.SetActive(false);
        if (_agilityUpgradeBtn != null) _agilityUpgradeBtn.gameObject.SetActive(false);

        _canvasGroup.alpha = 0;
        _tween = _canvasGroup.DOFade(1f, AnimationDuration).SetUpdate(true);
    }

    private void OnDestroy()
    {
        _backButton.onClick.RemoveAllListeners();
        _sightsButton.onClick.RemoveAllListeners();
    }

    public void Close()
    {
        // Сначала уведомляем сервис (чтобы он сбросил ссылку), затем делаем fade-out и уничтожаем
        OnBackClicked?.Invoke();

        _tween?.Kill();
        _tween = _canvasGroup.DOFade(0f, AnimationDuration)
            .SetUpdate(true)
            .OnComplete(() => Destroy(gameObject));
    }

    public void SetInfo(string[] userData, int[] sightIDs)
    {
        _nickNameText.text = userData[0];
        _levelText.text = userData[2];
        _firstNameText.text = userData[3];
        _lastNameText.text = userData[4];
        if (_registrationDateText != null)
            _registrationDateText.text = userData.Length > 5 && userData[5].Length >= 10 ? userData[5].Substring(0, 10) : "";
        _sightsText.text = userData[1];

        if (_idText != null)
            _idText.text = userData.Length > 6 ? userData[6] : "";

        if (userData.Length > 7 && int.TryParse(userData[7], out int strength))
        {
            if (_strengthText != null) _strengthText.text = strength.ToString();
            if (_strengthBar != null) _strengthBar.fillAmount = Mathf.Clamp01(strength / 10f);
        }

        if (userData.Length > 8 && int.TryParse(userData[8], out int intelligence))
        {
            if (_intelligenceText != null) _intelligenceText.text = intelligence.ToString();
            if (_intelligenceBar != null) _intelligenceBar.fillAmount = Mathf.Clamp01(intelligence / 10f);
        }

        if (userData.Length > 9 && int.TryParse(userData[9], out int agility))
        {
            if (_agilityText != null) _agilityText.text = agility.ToString();
            if (_agilityBar != null) _agilityBar.fillAmount = Mathf.Clamp01(agility / 10f);
        }

        _sightsID = sightIDs;
    }

    public class Factory : PlaceholderFactory<OtherPlayerProfileView> { }
}
