using UnityEngine;
using Zenject;

public class JumpGameEvent : BaseGameEvent
{
    private GameObject _gameRoot;
    private JumpController _controller;

    [Inject] private UserDataService _userDataService;
    [InjectOptional] private JumpGameConfig _config;

    protected override void OnStartGame()
    {
        _gameRoot = new GameObject("JumpGame");
        _gameRoot.transform.SetParent(_parentContainer, false);

        var rootRect = _gameRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        _controller = _gameRoot.AddComponent<JumpController>();
        _controller.Initialize(this, _userDataService != null ? _userDataService.Agility : 1, _config);
    }

    protected override void OnCleanup()
    {
        if (_gameRoot != null)
            Object.Destroy(_gameRoot);
    }

    // Вызывается кнопкой «Получить награду» на экране результата.
    // Игрок дошёл до конца → IsSuccess всегда true; бэкенд сам решает размер награды.
    public void OnGameEnded(int score)
    {
        FinishGame(true, score);
    }

    // Вызывается только при закрытии крестиком до окончания игры
    public void CloseGame()
    {
        FinishGame(false, 0);
    }
}
