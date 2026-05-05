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
        _registrationDateText.text = userData[5];
        _sightsText.text = userData[1];

        if (_idText != null)
            _idText.text = userData.Length > 6 ? userData[6] : "";

        _sightsID = sightIDs;
    }

    public class Factory : PlaceholderFactory<OtherPlayerProfileView> { }
}
