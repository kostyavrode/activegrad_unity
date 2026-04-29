using UnityEngine;
using Zenject;

public class TrainPathGameEvent : BaseGameEvent
{
    private GameObject _gameRoot;
    private TrainPathController _controller;

    [Inject] private UserDataService _userDataService;

    protected override void OnStartGame()
    {
        LoadGamePrefab();
    }

    private void LoadGamePrefab()
    {
        var prefab = Resources.Load<GameObject>("MiniGames/TrainPathGame");

        if (prefab == null)
        {
            Debug.LogError("[TrainPathGameEvent] Префаб TrainPathGame не найден в Resources/MiniGames/TrainPathGame.prefab");
            return;
        }

        _gameRoot = Object.Instantiate(prefab, _parentContainer);
        _gameRoot.name = "TrainPathGame";

        var ui = _gameRoot.GetComponent<TrainPathUI>();
        if (ui == null)
        {
            Debug.LogError("[TrainPathGameEvent] Компонент TrainPathUI не найден на префабе!");
            return;
        }

        _controller = _gameRoot.GetComponent<TrainPathController>();
        if (_controller == null)
            _controller = _gameRoot.AddComponent<TrainPathController>();

        _controller.Initialize(this, ui, _userDataService);
    }

    protected override void OnCleanup()
    {
        if (_gameRoot != null)
            Object.Destroy(_gameRoot);
    }

    public void OnGameEnded(float playerTime, float optimalTime, bool isOptimal)
    {
        int score = CalculateScore(playerTime, optimalTime, isOptimal);
        FinishGame(true, score);
    }

    public void OnGameEndedWithFinalScore(int finalScore)
    {
        FinishGame(true, finalScore);
    }

    private int CalculateScore(float playerTime, float optimalTime, bool isOptimal)
    {
        if (isOptimal)
            return 100;

        float ratio = optimalTime / playerTime;
        return Mathf.RoundToInt(ratio * 100);
    }

    public void CloseGame()
    {
        FinishGame(false, 0);
    }
}
