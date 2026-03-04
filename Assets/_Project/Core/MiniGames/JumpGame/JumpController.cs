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
    private float _inputBlockTimer = 0f;

    private float _sliderSpeed = 300f;
    private float _sliderDirection = 1f;
    private float _sliderMinX = -180f;
    private float _sliderMaxX = 180f;
    private float _indicatorPosition = 0f;

    // Zones as percentage of half-width: green ±20%, yellow ±35%, red beyond
    private float _greenZoneStart = -20f;
    private float _greenZoneEnd = 20f;
    private float _yellowZoneStart = -35f;
    private float _redZoneStart = 35f;

    private Vector2 _playerStartPos;
    private bool _isJumping = false;
    private float _jumpHeight = 180f;
    private float _jumpDuration = 0.7f;

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

        if (_ui.CloseButton != null)
            _ui.CloseButton.onClick.AddListener(() => _gameEvent?.CloseGame());

        if (_ui.FinishButton != null)
            _ui.FinishButton.onClick.AddListener(FinishGame);

        int agility = _userDataService != null ? _userDataService.Agility : 1;
        _ui.SetSkillInfo("Ловкость", agility);

        if (_ui.Player != null)
            _playerStartPos = _ui.Player.anchoredPosition;
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
        _inputBlockTimer = 0.3f;

        CreateZones();
        ResetPlayerPosition();
        StartNextJump();
    }

    private void StartNextJump()
    {
        _currentJump++;
        _ui.SetJumpCounter(_currentJump, TOTAL_JUMPS);
        _ui.SetScore(_totalScore);
        _ui.SetInstructionWithColor(
            "Нажмите когда индикатор в зелёной зоне!",
            Color.white);

        bool startFromLeft = Random.value > 0.5f;
        _indicatorPosition = startFromLeft ? _sliderMinX : _sliderMaxX;
        _sliderDirection = startFromLeft ? 1f : -1f;

        if (_ui.SliderIndicator != null)
            _ui.SliderIndicator.anchoredPosition = new Vector2(_indicatorPosition, 0);
    }

    private void CreateZones()
    {
        if (_ui.ZonesContainer == null || _ui.SliderBackground == null)
            return;

        ClearZones();

        float sliderWidth = _ui.SliderBackground.sizeDelta.x;
        if (sliderWidth <= 0) return;

        float hw = sliderWidth * 0.5f;

        CreateZone("RedZoneLeft",
            -hw,
            hw * (_yellowZoneStart / 100f),
            new Color(0.85f, 0.22f, 0.28f, 0.82f));

        CreateZone("YellowZoneLeft",
            hw * (_yellowZoneStart / 100f),
            hw * (_greenZoneStart / 100f),
            new Color(1f, 0.78f, 0.18f, 0.82f));

        CreateZone("GreenZone",
            hw * (_greenZoneStart / 100f),
            hw * (_greenZoneEnd / 100f),
            new Color(0.22f, 0.82f, 0.42f, 0.82f));

        CreateZone("YellowZoneRight",
            hw * (_greenZoneEnd / 100f),
            hw * (_redZoneStart / 100f),
            new Color(1f, 0.78f, 0.18f, 0.82f));

        CreateZone("RedZoneRight",
            hw * (_redZoneStart / 100f),
            hw,
            new Color(0.85f, 0.22f, 0.28f, 0.82f));
    }

    private void CreateZone(string name, float startX, float endX, Color color)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(_ui.ZonesContainer, false);

        Image zoneImage = zone.AddComponent<Image>();
        zoneImage.color = color;

        RectTransform zoneRect = zone.GetComponent<RectTransform>();
        float width = Mathf.Abs(endX - startX);
        float centerX = (startX + endX) * 0.5f;

        zoneRect.anchorMin = new Vector2(0.5f, 0.5f);
        zoneRect.anchorMax = new Vector2(0.5f, 0.5f);
        zoneRect.sizeDelta = new Vector2(width, _ui.SliderBackground.sizeDelta.y);
        zoneRect.anchoredPosition = new Vector2(centerX, 0);
        zoneRect.pivot = new Vector2(0.5f, 0.5f);

        _zoneVisuals.Add(zone);
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
            UpdateSlider();

        if (_inputBlockTimer > 0f)
        {
            _inputBlockTimer -= Time.deltaTime;
            return;
        }

        if (_canJump && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
            HandleJump();
    }

    private void UpdateSlider()
    {
        if (_ui.SliderIndicator == null)
            return;

        float deltaX = _sliderSpeed * Time.deltaTime * _sliderDirection;
        _indicatorPosition += deltaX;

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

        float halfWidth = _ui.SliderBackground.sizeDelta.x * 0.5f;
        float normalizedPos = (_indicatorPosition / halfWidth) * 100f;

        int points = CalculatePoints(normalizedPos);
        _jumpScores[_currentJump - 1] = points;
        _totalScore += points;

        _ui.SetScore(_totalScore);
        _ui.SetInstructionWithColor(
            GetZoneFeedback(normalizedPos),
            GetZoneFeedbackColor(normalizedPos));

        StartCoroutine(JumpAnimation());
    }

    private int CalculatePoints(float pos)
    {
        if (pos >= _greenZoneStart && pos <= _greenZoneEnd)
            return 100;

        if ((pos >= _yellowZoneStart && pos < _greenZoneStart) ||
            (pos > _greenZoneEnd && pos <= _redZoneStart))
            return 50;

        return 10;
    }

    private string GetZoneFeedback(float pos)
    {
        if (pos >= _greenZoneStart && pos <= _greenZoneEnd)
            return "Отлично! +100";

        if ((pos >= _yellowZoneStart && pos < _greenZoneStart) ||
            (pos > _greenZoneEnd && pos <= _redZoneStart))
            return "Хорошо! +50";

        return "Мимо! +10";
    }

    private Color GetZoneFeedbackColor(float pos)
    {
        if (pos >= _greenZoneStart && pos <= _greenZoneEnd)
            return new Color(0.3f, 1f, 0.5f);

        if ((pos >= _yellowZoneStart && pos < _greenZoneStart) ||
            (pos > _greenZoneEnd && pos <= _redZoneStart))
            return new Color(1f, 0.88f, 0.35f);

        return new Color(1f, 0.4f, 0.45f);
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

        float halfDur = _jumpDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDur);
            float eased = 1f - (1f - t) * (1f - t);
            _ui.Player.anchoredPosition = Vector2.Lerp(startPos, peakPos, eased);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDur);
            float eased = t * t;
            _ui.Player.anchoredPosition = Vector2.Lerp(peakPos, endPos, eased);
            yield return null;
        }

        _ui.Player.anchoredPosition = endPos;
        _isJumping = false;

        if (_currentJump >= TOTAL_JUMPS)
        {
            EndGame();
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
            _inputBlockTimer = 0.15f;
            StartNextJump();
            _isSliderMoving = true;
            _canJump = true;
        }
    }

    private void ResetPlayerPosition()
    {
        if (_ui.Player != null)
            _ui.Player.anchoredPosition = _playerStartPos;
    }

    private int _finalScore;

    private void EndGame()
    {
        _isGameActive = false;
        _isSliderMoving = false;
        _canJump = false;

        _ui.SetTotalScore(_totalScore);
        _ui.SetJumpResults(_jumpScores);
        _ui.ShowScreen(JumpScreen.End);

        var bonusSlider = _ui.BonusSliderComponent;
        if (bonusSlider != null)
        {
            int agility = _userDataService != null ? _userDataService.Agility : 1;
            float agilityLevel = Mathf.Clamp01((agility - 1) / 9f);
            if (bonusSlider.LeftLabel != null)
                bonusSlider.LeftLabel.text = "Ловкость";
            bonusSlider.Run(
                () => agilityLevel,
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
            _gameEvent.OnGameEnded(_finalScore > 0 ? _finalScore : _totalScore);
    }

    private void OnDestroy()
    {
        if (_ui != null)
        {
            if (_ui.StartButton != null)
                _ui.StartButton.onClick.RemoveAllListeners();
            if (_ui.CloseButton != null)
                _ui.CloseButton.onClick.RemoveAllListeners();
            if (_ui.FinishButton != null)
                _ui.FinishButton.onClick.RemoveAllListeners();
        }

        ClearZones();
    }
}
