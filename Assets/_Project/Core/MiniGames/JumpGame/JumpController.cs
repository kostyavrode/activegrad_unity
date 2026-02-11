using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ActiveGrad.MiniGames;

public class JumpController : MonoBehaviour
{
    private JumpGameEvent _gameEvent;
    private JumpUI _ui;
    private UserDataService _userDataService;
    
    private const int TOTAL_JUMPS = 3;
    private int _currentJump = 0;
    private int _totalScore = 0;
    private int[] _jumpScores = new int[TOTAL_JUMPS];
    
    private bool _isGameActive = false;
    private bool _isSliderMoving = false;
    private bool _canJump = false;
    
    // Slider settings
    private float _sliderSpeed = 600f; // Еще больше увеличена скорость
    private float _sliderDirection = 1f; // 1 = right, -1 = left
    private float _sliderMinX = -180f;
    private float _sliderMaxX = 180f;
    private float _indicatorPosition = 0f;
    
    // Zone settings (в процентах от ширины ползунка, от -50% до +50%)
    private float _greenZoneStart = -15f; // -15% от центра
    private float _greenZoneEnd = 15f;    // +15% от центра
    private float _yellowZoneStart = -30f;
    private float _yellowZoneEnd = -15f;
    private float _redZoneStart = 15f;
    private float _redZoneEnd = 30f;
    
    // Jump animation
    private Vector2 _playerStartPos;
    private bool _isJumping = false;
    private float _jumpHeight = 200f;
    private float _jumpDuration = 0.8f;
    
    // Zone visual elements
    private List<GameObject> _zoneVisuals = new List<GameObject>();

    public void Initialize(JumpGameEvent gameEvent, JumpUI ui, UserDataService userDataService)
    {
        _gameEvent = gameEvent;
        _ui = ui;
        _userDataService = userDataService;
        
        if (_ui == null)
        {
            Debug.LogError("[JumpController] JumpUI не назначен!");
            return;
        }
        
        SetupUI();
        ShowStartScreen();
    }

    private void SetupUI()
    {
        if (_ui.StartButton != null)
            _ui.StartButton.onClick.AddListener(StartGame);
        
        if (_ui.FinishButton != null)
            _ui.FinishButton.onClick.AddListener(FinishGame);
        
        if (_ui.Player != null)
        {
            _playerStartPos = _ui.Player.anchoredPosition;
        }
    }

    private void ShowStartScreen()
    {
        _ui.ShowScreen(JumpScreen.Start);
    }

    private void StartGame()
    {
        _ui.ShowScreen(JumpScreen.Game);
        _isGameActive = true;
        _currentJump = 0;
        _totalScore = 0;
        _isSliderMoving = true;
        _canJump = true;
        
        // Создаем зоны после показа игрового экрана
        CreateZones();
        
        ResetPlayerPosition();
        StartNextJump();
    }

    private void StartNextJump()
    {
        _currentJump++;
        _ui.SetJumpCounter(_currentJump, TOTAL_JUMPS);
        _ui.SetScore(_totalScore);
        _ui.SetInstruction("Нажмите когда индикатор в зеленой зоне!");
        
        // Сбрасываем позицию индикатора
        _indicatorPosition = 0f;
        _sliderDirection = 1f;
        
        if (_ui.SliderIndicator != null)
        {
            _ui.SliderIndicator.anchoredPosition = new Vector2(_indicatorPosition, 0);
        }
    }

    private void CreateZones()
    {
        if (_ui.ZonesContainer == null || _ui.SliderBackground == null)
        {
            Debug.LogWarning("[JumpController] ZonesContainer или SliderBackground не назначены!");
            return;
        }
        
        ClearZones();
        
        float sliderWidth = _ui.SliderBackground.sizeDelta.x;
        if (sliderWidth <= 0)
        {
            Debug.LogWarning("[JumpController] Ширина ползунка некорректна!");
            return;
        }
        
        // Зоны в пикселях от центра (0 - это центр ползунка)
        // Ползунок шириной 400px, значит от -200 до +200
        
        // Красная зона (левая, от -200 до -120)
        CreateZone("RedZoneLeft",
            sliderWidth * (-0.5f),
            sliderWidth * (_yellowZoneStart / 100f),
            Color.red);
        
        // Желтая зона (левая, от -120 до -60)
        CreateZone("YellowZoneLeft",
            sliderWidth * (_yellowZoneStart / 100f),
            sliderWidth * (_greenZoneStart / 100f),
            Color.yellow);
        
        // Зеленая зона (центральная, от -60 до +60)
        CreateZone("GreenZone",
            sliderWidth * (_greenZoneStart / 100f),
            sliderWidth * (_greenZoneEnd / 100f),
            Color.green);
        
        // Желтая зона (правая, от +60 до +120)
        CreateZone("YellowZoneRight",
            sliderWidth * (_greenZoneEnd / 100f),
            sliderWidth * (_redZoneStart / 100f),
            Color.yellow);
        
        // Красная зона (правая, от +120 до +200)
        CreateZone("RedZoneRight",
            sliderWidth * (_redZoneStart / 100f),
            sliderWidth * 0.5f,
            Color.red);
        
        Debug.Log($"[JumpController] Создано зон: {_zoneVisuals.Count}, ширина ползунка: {sliderWidth}");
    }

    private void CreateZone(string name, float startX, float endX, Color color)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(_ui.ZonesContainer, false);
        
        Image zoneImage = zone.AddComponent<Image>();
        // Полная непрозрачность для лучшей видимости
        zoneImage.color = new Color(color.r, color.g, color.b, 0.8f);
        
        RectTransform zoneRect = zone.GetComponent<RectTransform>();
        float width = Mathf.Abs(endX - startX);
        float centerX = (startX + endX) * 0.5f;
        
        // Используем center anchor для правильного позиционирования
        zoneRect.anchorMin = new Vector2(0.5f, 0.5f);
        zoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        zoneRect.sizeDelta = new Vector2(width, _ui.SliderBackground.sizeDelta.y);
        zoneRect.anchoredPosition = new Vector2(centerX, 0);
        zoneRect.pivot = new Vector2(0.5f, 0.5f);
        
        // Устанавливаем порядок отрисовки (зоны должны быть под индикатором, но поверх фона)
        // Не используем SetSiblingIndex, так как зоны создаются в контейнере
        
        _zoneVisuals.Add(zone);
        
        Debug.Log($"[JumpController] Создана зона {name}: ширина={width}, центр={centerX}, цвет={color}, позиция={zoneRect.anchoredPosition}");
    }

    private void ClearZones()
    {
        foreach (var zone in _zoneVisuals)
        {
            if (zone != null)
                Destroy(zone);
        }
        _zoneVisuals.Clear();
    }

    private void Update()
    {
        if (!_isGameActive || _isJumping)
            return;
        
        if (_isSliderMoving)
        {
            UpdateSlider();
        }
        
        if (_canJump && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            HandleJump();
        }
    }

    private void UpdateSlider()
    {
        if (_ui.SliderIndicator == null)
            return;
        
        float sliderWidth = _ui.SliderBackground.sizeDelta.x;
        float deltaX = _sliderSpeed * Time.deltaTime * _sliderDirection;
        _indicatorPosition += deltaX;
        
        // Отскок от краев
        if (_indicatorPosition >= _sliderMaxX)
        {
            _indicatorPosition = _sliderMaxX;
            _sliderDirection = -1f;
        }
        else if (_indicatorPosition <= _sliderMinX)
        {
            _indicatorPosition = _sliderMinX;
            _sliderDirection = 1f;
        }
        
        _ui.SliderIndicator.anchoredPosition = new Vector2(_indicatorPosition, 0);
    }

    private void HandleJump()
    {
        if (!_canJump || _isJumping)
            return;
        
        _canJump = false;
        _isSliderMoving = false;
        
        // Определяем зону
        // _indicatorPosition от -180 до +180, нужно преобразовать в проценты от центра
        float sliderWidth = _ui.SliderBackground.sizeDelta.x;
        float normalizedPos = (_indicatorPosition / (sliderWidth * 0.5f)) * 100f; // в процентах от центра (-100% до +100%)
        
        int points = CalculatePoints(normalizedPos);
        _jumpScores[_currentJump - 1] = points;
        _totalScore += points;
        
        _ui.SetScore(_totalScore);
        _ui.SetInstruction(GetZoneFeedback(normalizedPos));
        
        // Анимация прыжка
        StartCoroutine(JumpAnimation());
    }

    private int CalculatePoints(float normalizedPosition)
    {
        // Зеленая зона: 100 очков
        if (normalizedPosition >= _greenZoneStart && normalizedPosition <= _greenZoneEnd)
            return 100;
        
        // Желтая зона: 50 очков
        if ((normalizedPosition >= _yellowZoneStart && normalizedPosition < _greenZoneStart) ||
            (normalizedPosition > _greenZoneEnd && normalizedPosition <= _redZoneStart))
            return 50;
        
        // Красная зона: 10 очков
        return 10;
    }

    private string GetZoneFeedback(float normalizedPosition)
    {
        if (normalizedPosition >= _greenZoneStart && normalizedPosition <= _greenZoneEnd)
            return "Отлично! Зеленая зона! +100";
        
        if ((normalizedPosition >= _yellowZoneStart && normalizedPosition < _greenZoneStart) ||
            (normalizedPosition > _greenZoneEnd && normalizedPosition <= _redZoneStart))
            return "Хорошо! Желтая зона! +50";
        
        return "Мимо! Красная зона! +10";
    }

    private IEnumerator JumpAnimation()
    {
        _isJumping = true;
        
        if (_ui.Player == null)
        {
            _isJumping = false;
            yield break;
        }
        
        Vector2 startPos = _ui.Player.anchoredPosition;
        Vector2 peakPos = startPos + new Vector2(0, _jumpHeight);
        Vector2 endPos = startPos;
        
        float elapsed = 0f;
        
        // Взлет
        while (elapsed < _jumpDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (_jumpDuration * 0.5f);
            _ui.Player.anchoredPosition = Vector2.Lerp(startPos, peakPos, t);
            yield return null;
        }
        
        // Падение
        elapsed = 0f;
        while (elapsed < _jumpDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (_jumpDuration * 0.5f);
            _ui.Player.anchoredPosition = Vector2.Lerp(peakPos, endPos, t);
            yield return null;
        }
        
        _ui.Player.anchoredPosition = endPos;
        _isJumping = false;
        
        // Проверяем, закончилась ли игра
        if (_currentJump >= TOTAL_JUMPS)
        {
            EndGame();
        }
        else
        {
            // Начинаем следующий прыжок
            yield return new WaitForSeconds(0.5f);
            StartNextJump();
            _isSliderMoving = true;
            _canJump = true;
        }
    }

    private void ResetPlayerPosition()
    {
        if (_ui.Player != null)
        {
            _ui.Player.anchoredPosition = _playerStartPos;
        }
    }

    private int _finalScore;

    private void EndGame()
    {
        _isGameActive = false;
        _isSliderMoving = false;
        _canJump = false;
        
        _ui.SetTotalScore(_totalScore);
        _ui.ShowScreen(JumpScreen.End);
        
        var bonusSlider = _ui.BonusSliderComponent;
        if (bonusSlider != null)
        {
            bonusSlider.Run(
                () => _userDataService != null ? _userDataService.GetProfessionLevel() : 0.5f,
                _totalScore,
                OnBonusSliderComplete);
        }
        else
        {
            _finalScore = _totalScore;
            if (_ui.EndScreenButtonsContainer != null)
                _ui.EndScreenButtonsContainer.SetActive(true);
        }
    }

    private void OnBonusSliderComplete(int finalScore, float bonus)
    {
        _finalScore = finalScore;
        _ui.SetTotalScore(finalScore);
    }

    private void FinishGame()
    {
        if (_gameEvent != null)
        {
            _gameEvent.OnGameEnded(_finalScore > 0 ? _finalScore : _totalScore);
        }
    }

    private void OnDestroy()
    {
        if (_ui != null)
        {
            if (_ui.StartButton != null)
                _ui.StartButton.onClick.RemoveAllListeners();
            if (_ui.FinishButton != null)
                _ui.FinishButton.onClick.RemoveAllListeners();
        }
        
        ClearZones();
    }
}
