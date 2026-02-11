using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrainPathController : MonoBehaviour
{
    private TrainPathGameEvent _gameEvent;
    private TrainPathUI _ui;
    
    private TrainMapGenerator _mapGenerator;
    private List<Station> _stations = new List<Station>();
    private List<TrainPathConnection> _paths = new List<TrainPathConnection>();
    private Station _startStation;
    private Station _endStation;
    private List<Station> _playerPath = new List<Station>();
    private List<Station> _optimalPath = new List<Station>();
    
    private Station _currentStation;
    private bool _isMoving = false;
    private float _gameTime = 0f;
    private float _optimalTime = 0f;
    private bool _gameStarted = false;
    private bool _gameEnded = false;

    public void Initialize(TrainPathGameEvent gameEvent, TrainPathUI ui)
    {
        _gameEvent = gameEvent;
        _ui = ui;
        
        if (_ui == null)
        {
            Debug.LogError("[TrainPathController] TrainPathUI не назначен!");
            return;
        }
        
        _mapGenerator = new TrainMapGenerator();
        
        SetupUI();
        GenerateNewMap();
        ShowStartScreen();
    }

    private void SetupUI()
    {
        if (_ui.StartButton != null)
            _ui.StartButton.onClick.AddListener(StartGame);
        
        if (_ui.RestartButton != null)
            _ui.RestartButton.onClick.AddListener(RestartGame);
        
        if (_ui.FinishButton != null)
            _ui.FinishButton.onClick.AddListener(FinishGame);
    }

    private void ShowStartScreen()
    {
        _ui.ShowScreen(TrainPathScreen.Start);
    }

    private void StartGame()
    {
        _ui.ShowScreen(TrainPathScreen.Game);
        _gameStarted = true;
        _gameTime = 0f;
        _playerPath.Clear();
        _currentStation = _startStation;
        _playerPath.Add(_currentStation);
        UpdateTrainPosition(_currentStation);
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
        {
            _gameEvent.OnGameEnded(_gameTime, _optimalTime, IsOptimalPath());
        }
    }

    private void GenerateNewMap()
    {
        ClearMap();
        
        _mapGenerator.GenerateMap(_ui.MapContainer, out _stations, out _paths, out _startStation, out _endStation);
        
        CalculateOptimalPath();
        
        _gameEnded = false;
        _isMoving = false;
    }

    private void ClearMap()
    {
        if (_ui.MapContainer != null)
        {
            foreach (Transform child in _ui.MapContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        _stations.Clear();
        _paths.Clear();
        _playerPath.Clear();
        _optimalPath.Clear();
    }

    private void CalculateOptimalPath()
    {
        if (_startStation == null || _endStation == null)
            return;
        
        _optimalPath = Pathfinder.FindShortestPath(_startStation, _endStation, _stations, _paths);
        _optimalTime = CalculatePathTime(_optimalPath);
    }

    private float CalculatePathTime(List<Station> path)
    {
        if (path == null || path.Count < 2)
            return 0f;
        
        float totalTime = 0f;
        
        for (int i = 0; i < path.Count - 1; i++)
        {
            Station from = path[i];
            Station to = path[i + 1];
            
            TrainPathConnection connection = _paths.FirstOrDefault(p => 
                (p.From == from && p.To == to) || 
                (p.From == to && p.To == from));
            
            if (connection != null)
            {
                totalTime += connection.TravelTime;
            }
            
            if (i < path.Count - 1)
            {
                totalTime += to.WaitTime;
            }
        }
        
        return totalTime;
    }

    private void Update()
    {
        if (!_gameStarted || _gameEnded || _isMoving)
            return;
        
        _gameTime += Time.deltaTime;
        _ui.SetTime(_gameTime);
        
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        Vector2 mousePos = Input.mousePosition;
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _ui.MapContainer, mousePos, canvas.worldCamera, out Vector2 localPoint);
        
        Station clickedStation = GetStationAtPosition(localPoint);
        
        if (clickedStation != null && CanMoveToStation(clickedStation))
        {
            MoveToStation(clickedStation);
        }
    }

    private Station GetStationAtPosition(Vector2 position)
    {
        float clickRadius = 30f;
        
        foreach (var station in _stations)
        {
            float distance = Vector2.Distance(position, station.Position);
            if (distance < clickRadius)
            {
                return station;
            }
        }
        
        return null;
    }

    private bool CanMoveToStation(Station target)
    {
        if (_currentStation == null || target == null)
            return false;
        
        if (_currentStation == target)
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
        _playerPath.Add(target);
        _currentStation = target;
        
        StartCoroutine(MoveTrainCoroutine(_currentStation, connection.TravelTime));
        
        if (target == _endStation)
        {
            EndGame();
        }
        else
        {
            HighlightAvailableStations();
        }
    }

    private System.Collections.IEnumerator MoveTrainCoroutine(Station target, float travelTime)
    {
        Vector2 startPos = _ui.Train.anchoredPosition;
        Vector2 endPos = target.Position;
        float elapsed = 0f;
        
        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelTime;
            _ui.Train.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        _ui.Train.anchoredPosition = endPos;
        
        yield return new WaitForSeconds(target.WaitTime);
        
        _isMoving = false;
        _ui.SetPathInfo(_playerPath.Count, _stations.Count);
    }

    private void UpdateTrainPosition(Station station)
    {
        if (_ui.Train != null && station != null)
        {
            _ui.Train.anchoredPosition = station.Position;
        }
    }

    private void HighlightAvailableStations()
    {
        foreach (var station in _stations)
        {
            bool isAvailable = CanMoveToStation(station);
            station.SetHighlight(isAvailable);
        }
    }

    private void EndGame()
    {
        _gameEnded = true;
        _isMoving = false;
        
        bool isOptimal = IsOptimalPath();
        int score = CalculateScore();
        
        _ui.SetResult(_gameTime, _optimalTime, isOptimal, score);
        _ui.ShowScreen(TrainPathScreen.End);
    }

    private bool IsOptimalPath()
    {
        if (_playerPath.Count != _optimalPath.Count)
            return false;
        
        for (int i = 0; i < _playerPath.Count; i++)
        {
            if (_playerPath[i] != _optimalPath[i])
                return false;
        }
        
        return true;
    }

    private int CalculateScore()
    {
        if (_optimalTime <= 0)
            return 0;
        
        float ratio = _optimalTime / _gameTime;
        return Mathf.RoundToInt(ratio * 100);
    }

    private void OnDestroy()
    {
        if (_ui != null)
        {
            if (_ui.StartButton != null)
                _ui.StartButton.onClick.RemoveAllListeners();
            if (_ui.RestartButton != null)
                _ui.RestartButton.onClick.RemoveAllListeners();
            if (_ui.FinishButton != null)
                _ui.FinishButton.onClick.RemoveAllListeners();
        }
    }
}
