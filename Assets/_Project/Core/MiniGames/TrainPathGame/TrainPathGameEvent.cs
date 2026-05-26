using UnityEngine;
using Zenject;

public class TrainPathGameEvent : BaseGameEvent
{
    private GameObject _gameRoot;
    private TrainPathController _controller;

    [Inject] private UserDataService _userDataService;

    protected override void OnStartGame()
    {
        _gameRoot = new GameObject("TrainPathGame");
        _gameRoot.transform.SetParent(_parentContainer, false);

        var rt = _gameRoot.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _controller = _gameRoot.AddComponent<TrainPathController>();
        _controller.Initialize(this, _userDataService != null ? _userDataService.Intelligence : 1);
    }

    protected override void OnCleanup()
    {
        if (_gameRoot != null)
            Object.Destroy(_gameRoot);
    }

    public void OnGameEndedWithFinalScore(int finalScore)
    {
        FinishGame(true, finalScore);
    }

    public void CloseGame()
    {
        FinishGame(false, 0);
    }
}
