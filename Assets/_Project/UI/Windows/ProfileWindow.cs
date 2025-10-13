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

    [SerializeField] private Button _backButton;
    
    public event Action OnBackClicked;
    public event Action OnProfileOpened;

    protected override void OnShow()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        OnProfileOpened?.Invoke();
    }

    protected override void OnHide()
    {
        _backButton.onClick.RemoveAllListeners();
    }

    public void SetInfo(string[] userData)
    {
        _nickNameText.text = userData[0];
        _firstNameText.text = userData[1];
        _lastNameText.text = userData[2];
        _levelText.text = userData[3];
        _expText.text = userData[4];
    }
}
