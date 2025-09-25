using System;
using UnityEngine;
using Zenject;

public class CharacterCustomizationMediator : IInitializable, IDisposable
{
    private readonly CharacterCustomizationWindow _window;
    private readonly APIService _apiService;
    private readonly UIManager _uiManager;

    private bool _isMale = true;
    private readonly int[] _clothes = new int[4]; // boots, pants, tshirt, cap
    private const int MinValue = 0;
    private const int MaxValue = 4;

    public CharacterCustomizationMediator(CharacterCustomizationWindow window, APIService apiService, UIManager uiManager)
    {
        _window = window;
        _apiService = apiService;
        _uiManager = uiManager;
    }

    public void Initialize()
    {
        _window.OnToggleGender += HandleToggleGender;
        _window.OnClothesChanged += HandleClothesChanged;
        _window.OnConfirm += HandleConfirm;
        
        _window.SetGender(_isMale);
        for (int i = 0; i < _clothes.Length; i++)
            _window.SetClothesValue(i, _clothes[i]);
    }

    public void Dispose()
    {
        _window.OnToggleGender -= HandleToggleGender;
        _window.OnClothesChanged -= HandleClothesChanged;
        _window.OnConfirm -= HandleConfirm;
    }

    private void HandleToggleGender()
    {
        _isMale = !_isMale;
        _window.SetGender(_isMale);
    }

    private void HandleClothesChanged(int category, int delta)
    {
        int newValue = _clothes[category] + delta;
        newValue = Mathf.Clamp(newValue, MinValue, MaxValue);
        _clothes[category] = newValue;

        _window.SetClothesValue(category, newValue);
    }

    private async void HandleConfirm()
    {
        var (success, message) = await _apiService.UpdateClothes(
            boots: _clothes[0],
            pants: _clothes[1],
            tshirt: _clothes[2],
            cap: _clothes[3],
            gender: "M"
        );

        if (success)
        {
            Debug.Log("Одежда успешно обновлена");
            _uiManager.Show<MenuWindow>();
        }
        else
        {
            Debug.LogWarning($"Ошибка при обновлении одежды: {message}");
        }
    }
}
