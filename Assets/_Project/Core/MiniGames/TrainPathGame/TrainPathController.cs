using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ActiveGrad.MiniGames;

public class TrainPathController : MonoBehaviour
{
    [SerializeField] private TrainPathConfig _config;

    private TrainPathGameEvent _gameEvent;
    private TrainPathUI _ui;
    private UserDataService _userDataService;

    private TrainMapGenerator _mapGenerator;
    private List<Station> _stations = new List<Station>();
    private List<TrainPathConnection> _paths = new List<TrainPathConnection>();
    private Station _startStation;
    private Station _endStation;

    private Station _currentStation;
    private bool _isMoving = false;
    private bool _gameStarted = false;
    private bool _gameEnded = false;

    private int _totalCargo = 0;
    private int _cargoCollected = 0;
    private float _countdownTime = 0f;
    private float _remainingTime = 0f;
    private int _finalScore = 0;

    public void Initialize(TrainPathGameEvent gameEvent, TrainPathUI ui, UserDataService userDataService)
    {
        _gameEvent = gameEvent;
        _ui = ui;
        _userDataService = userDataService;

        if (_ui == null)
        {
            Debug.LogError("[TrainPathController] TrainPathUI не назначен!");
            return;
        }

        if (_config == null)
            Debug.LogWarning("[TrainPathController] TrainPathConfig не назначен — используются значения по умолчанию.");

        _mapGenerator = new TrainMapGenerator(_config);

        ApplyTrainVisuals();
        SetupUI();
        GenerateNewMap();
        ShowStartScreen();
    }

    private void ApplyTrainVisuals()
    {
        if (_config == null || _ui.Train == null) return;

        _ui.Train.sizeDelta = _config.TrainSize;

        var trainImage = _ui.Train.GetComponent<Image>();
        if (trainImage != null && _config.TrainSprite != null)
            trainImage.sprite = _config.TrainSprite;
    }

    private void SetupUI()
    {
        if (_ui.StartButton != null)
            _ui.StartButton.onClick.AddListener(StartGame);

        if (_ui.CloseButton != null)
            _ui.CloseButton.onClick.AddListener(() => _gameEvent?.CloseGame());

        if (_ui.RestartButton != null)
            _ui.RestartButton.onClick.AddListener(RestartGame);

        if (_ui.FinishButton != null)
            _ui.FinishButton.onClick.AddListener(FinishGame);

        int intelligence = _userDataService != null ? _userDataService.Intelligence : 1;
        _ui.SetSkillInfo("Интеллект", intelligence);
    }

    private void ShowStartScreen()
    {
        _ui.ShowScreen(TrainPathScreen.Start);
    }

    private void StartGame()
    {
        _totalCargo = _stations.Count(s => s.IsCargo);

        float baseTime = _config != null ? _config.BaseCountdownTime : 90f;
        float timePerCargo = _config != null ? _config.TimePerCargo : 20f;
        _countdownTime = baseTime + _totalCargo * timePerCargo;
        _remainingTime = _countdownTime;
        _cargoCollected = 0;
        _gameStarted = true;
        _currentStation = _startStation;
        UpdateTrainPosition(_currentStation);

        _ui.ShowScreen(TrainPathScreen.Game);
        _ui.SetCountdown(_remainingTime);
        _ui.SetCargoStatus(_cargoCollected, _totalCargo);

        HighlightAvailableStations();
    }

    private void RestartGame()
    {
        GenerateNewMap();
        StartGame();
    }

    private void FinishGame()
    {
        if (_gameEvent != null)
            _gameEvent.OnGameEndedWithFinalScore(_finalScore);
    }

    private void GenerateNewMap()
    {
        ClearMap();
        _mapGenerator.GenerateMap(_ui.MapContainer, out _stations, out _paths, out _startStation, out _endStation);
        _gameEnded = false;
        _isMoving = false;
    }

    private void ClearMap()
    {
        if (_ui.MapContainer != null)
        {
            foreach (Transform child in _ui.MapContainer)
                Destroy(child.gameObject);
        }

        _stations.Clear();
        _paths.Clear();
    }

    private void Update()
    {
        if (!_gameStarted || _gameEnded)
            return;

        _remainingTime -= Time.deltaTime;
        _ui.SetCountdown(Mathf.Max(0f, _remainingTime));

        if (_remainingTime <= 0f)
        {
            TimeOut();
            return;
        }

        if (_isMoving)
            return;

        if (Input.GetMouseButtonDown(0))
            HandleClick();
    }

    private void HandleClick()
    {
        Vector2 mousePos = Input.mousePosition;
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _ui.MapContainer, mousePos, canvas.worldCamera, out Vector2 localPoint);

        Station clickedStation = GetStationAtPosition(localPoint);

        if (clickedStation != null && CanMoveToStation(clickedStation))
            MoveToStation(clickedStation);
    }

    private Station GetStationAtPosition(Vector2 position)
    {
        float clickRadius = _config != null ? _config.ClickRadius : 30f;

        foreach (var station in _stations)
        {
            if (Vector2.Distance(position, station.Position) < clickRadius)
                return station;
        }

        return null;
    }

    private bool CanMoveToStation(Station target)
    {
        if (_currentStation == null || target == null || _currentStation == target)
            return false;

        if (target == _endStation && _cargoCollected < _totalCargo)
            return false;

        return _paths.Any(p =>
            (p.From == _currentStation && p.To == target) ||
            (p.From == target && p.To == _currentStation));
    }

    private void MoveToStation(Station target)
    {
        if (_isMoving)
            return;

        TrainPathConnection connection = _paths.FirstOrDefault(p =>
            (p.From == _currentStation && p.To == target) ||
            (p.From == target && p.To == _currentStation));

        if (connection == null)
            return;

        _isMoving = true;
        _currentStation = target;

        HighlightAvailableStations();
        StartCoroutine(MoveTrainCoroutine(target, connection.TravelTime));
    }

    private IEnumerator MoveTrainCoroutine(Station target, float travelTime)
    {
        Vector2 startPos = _ui.Train.anchoredPosition;
        Vector2 endPos = target.Position;
        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            _ui.Train.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / travelTime);
            yield return null;
        }

        _ui.Train.anchoredPosition = endPos;

        yield return new WaitForSeconds(target.WaitTime);

        TryCollectCargo(target);

        _isMoving = false;

        if (target == _endStation && _cargoCollected >= _totalCargo)
        {
            EndGame(false);
        }
        else
        {
            HighlightAvailableStations();
        }
    }

    private void TryCollectCargo(Station station)
    {
        if (!station.IsCargo || station.IsCargoCollected)
            return;

        station.CollectCargo();
        _cargoCollected++;
        _ui.SetCargoStatus(_cargoCollected, _totalCargo);
    }

    private void UpdateTrainPosition(Station station)
    {
        if (_ui.Train != null && station != null)
            _ui.Train.anchoredPosition = station.Position;
    }

    private void HighlightAvailableStations()
    {
        foreach (var station in _stations)
            station.SetHighlight(CanMoveToStation(station));
    }

    private void TimeOut()
    {
        EndGame(true);
    }

    private void EndGame(bool isTimeout)
    {
        if (_gameEnded)
            return;

        _gameEnded = true;
        _isMoving = false;
        _gameStarted = false;

        int baseScore = isTimeout
            ? Mathf.RoundToInt((_totalCargo > 0 ? (float)_cargoCollected / _totalCargo : 0f) * 50f)
            : Mathf.RoundToInt(60f + (_remainingTime / _countdownTime) * 40f);

        _ui.SetResult(_cargoCollected, _totalCargo, _remainingTime, isTimeout, baseScore);
        _ui.ShowScreen(TrainPathScreen.End);

        var bonusSlider = _ui.BonusSliderComponent;
        if (bonusSlider != null)
        {
            int intelligence = _userDataService != null ? _userDataService.Intelligence : 1;
            float intelligenceLevel = Mathf.Clamp01((intelligence - 1) / 9f);
            bonusSlider.Run(
                () => intelligenceLevel,
                baseScore,
                OnBonusSliderComplete);
        }
        else
        {
            _finalScore = baseScore;
            if (_ui.EndScreenButtonsContainer != null)
                _ui.EndScreenButtonsContainer.SetActive(true);
        }
    }

    private void OnBonusSliderComplete(int finalScore, float bonus)
    {
        _finalScore = finalScore;
        _ui.SetResult(_cargoCollected, _totalCargo, _remainingTime, false, finalScore);
    }

    private void OnDestroy()
    {
        if (_ui != null)
        {
            if (_ui.StartButton != null)
                _ui.StartButton.onClick.RemoveAllListeners();
            if (_ui.CloseButton != null)
                _ui.CloseButton.onClick.RemoveAllListeners();
            if (_ui.RestartButton != null)
                _ui.RestartButton.onClick.RemoveAllListeners();
            if (_ui.FinishButton != null)
                _ui.FinishButton.onClick.RemoveAllListeners();
        }
    }
}
