using System;
using UnityEngine;
using Zenject;

public class CharacterCustomizationMediator : IInitializable, IDisposable
{
    private readonly CharacterCustomizationWindow _window;
    private readonly APIService _apiService;
    private readonly UIManager _uiManager;
    private readonly CharacterPreviewService _characterPreviewService;
    private readonly SceneLoader _sceneLoader;

    private bool _isMale = true;
    private readonly int[] _clothes = new int[4]; // boots, pants, tshirt, cap
    private const int MinValue = 0;
    private const int MaxValue = 4;

    public CharacterCustomizationMediator(CharacterCustomizationWindow window, APIService apiService,
        UIManager uiManager, CharacterPreviewService characterPreviewService, SceneLoader sceneLoader)
    {
        _window = window;
        _apiService = apiService;
        _uiManager = uiManager;
        _characterPreviewService = characterPreviewService;
        _sceneLoader = sceneLoader;
    }

    public void Initialize()
    {
        _window.OnToggleGender += HandleToggleGender;
        _window.OnClothesChanged += HandleClothesChanged;
        _window.OnConfirm += HandleConfirm;
        _window.OnActivate += SpawnPreviewCharacter;
        
        _window.SetGender(_isMale);
        for (int i = 0; i < _clothes.Length; i++)
            _window.SetClothesValue(i, _clothes[i]);
    }

    public void Dispose()
    {
        _window.OnToggleGender -= HandleToggleGender;
        _window.OnClothesChanged -= HandleClothesChanged;
        _window.OnConfirm -= HandleConfirm;
        _window.OnActivate -= SpawnPreviewCharacter;
    }

    private void SpawnPreviewCharacter()
    {
        _characterPreviewService.SpawnPreviewCharacter();
    }
    
    private void HandleToggleGender()
    {
        _isMale = !_isMale;
        _window.SetGender(_isMale);
    }

    private void HandleClothesChanged(int category, int delta)
    {
        if (category < 0 || category >= _clothes.Length)
        {
            Debug.LogError($"HandleClothesChanged: неверный индекс {category}, всего категорий {_clothes.Length}");
            return;
        }

        int newValue = _clothes[category] + delta;
        newValue = Mathf.Clamp(newValue, MinValue, MaxValue);
        _clothes[category] = newValue;

        _window.SetClothesValue(category, newValue);
        _characterPreviewService.ApplyClothing(_clothes);
    }


    private async void HandleConfirm()
    {
        _characterPreviewService.Dispose();
        
        var (success, message) = await _apiService.UpdateClothes(
            boots: _clothes[3],
            pants: _clothes[2],
            tshirt: _clothes[1],
            cap: _clothes[0],
            gender: "M"
        );
        
        

        if (success)
        {
            Debug.Log("Одежда успешно обновлена");
            _sceneLoader.LoadScene("SampleScene");
        }
        else
        {
            Debug.LogWarning($"Ошибка при обновлении одежды: {message}");
        }
    }
}
