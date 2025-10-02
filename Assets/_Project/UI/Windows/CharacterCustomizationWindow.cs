using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCustomizationWindow : BaseWindow
{
    [SerializeField] private Button genderButton;
    [SerializeField] private TextMeshProUGUI genderText;

    [Header("Clothes selectors")]
    [SerializeField] private Button[] leftButtons;   // массив из 5 кнопок "<"
    [SerializeField] private Button[] rightButtons;  // массив из 5 кнопок ">"
    [SerializeField] private TextMeshProUGUI[] valuesText; // отображение текущего значения

    [SerializeField] private Button confirmButton;

    public event Action OnToggleGender;
    public event Action<int, int> OnClothesChanged; // (category, value)
    public event Action OnConfirm;
    public event Action OnActivate;

    protected override void OnShow()
    {
        OnActivate?.Invoke();
        
        genderButton.onClick.AddListener(() => OnToggleGender?.Invoke());
        confirmButton.onClick.AddListener(() => OnConfirm?.Invoke());

        for (int i = 0; i < leftButtons.Length; i++)
        {
            int category = i;
            leftButtons[i].onClick.AddListener(() => OnClothesChanged?.Invoke(category, -1));
            rightButtons[i].onClick.AddListener(() => OnClothesChanged?.Invoke(category, +1));
        }
    }

    protected override void OnHide()
    {
        genderButton.onClick.RemoveAllListeners();
        confirmButton.onClick.RemoveAllListeners();

        foreach (var btn in leftButtons)
            btn.onClick.RemoveAllListeners();

        foreach (var btn in rightButtons)
            btn.onClick.RemoveAllListeners();
    }

    public void SetGender(bool isMale)
    {
        genderText.text = isMale ? "М" : "Ж";
    }

    public void SetClothesValue(int category, int value)
    {
        if (category < 0 || category >= valuesText.Length) return;
        valuesText[category].text = value.ToString();
    }
}