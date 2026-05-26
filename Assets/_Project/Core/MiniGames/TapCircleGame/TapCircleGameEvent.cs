using UnityEngine;
using Zenject;

public class TapCircleGameEvent : BaseGameEvent
{
    private GameObject _gameRoot;

    [Inject] private UserDataService _userDataService;

    protected override void OnStartGame()
    {
        _gameRoot = new GameObject("TapCircleGame");
        _gameRoot.transform.SetParent(_parentContainer, false);

        var rect = _gameRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var controller = _gameRoot.AddComponent<TapCircleController>();
        controller.Initialize(this, _userDataService);
    }

    protected override void OnCleanup()
    {
        if (_gameRoot != null)
            Object.Destroy(_gameRoot);
    }

    // Игрок дошёл до экрана результата → IsSuccess всегда true; бэкенд решает размер награды.
    public void OnGameEnded(int score)
    {
        FinishGame(true, score);
    }

    public void CloseGame()
    {
        FinishGame(false, 0);
    }
}
