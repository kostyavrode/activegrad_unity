using UnityEngine;
using Zenject;

public class FlappyBirdGameEvent : BaseGameEvent
{
    private GameObject _gameRoot;
    private FlappyBirdController _controller;

    [Inject] private UserDataService _userDataService;

    protected override void OnStartGame()
    {
        LoadGamePrefab();
    }

    private void LoadGamePrefab()
    {
        var prefab = Resources.Load<GameObject>("MiniGames/FlappyBirdGame");
        if (prefab == null)
        {
            Debug.LogError("[FlappyBirdGameEvent] Префаб FlappyBirdGame не найден в Resources/MiniGames/");
            return;
        }

        _gameRoot = Object.Instantiate(prefab, _parentContainer);
        _gameRoot.name = "FlappyBirdGame";

        var ui = _gameRoot.GetComponent<FlappyBirdUI>();
        if (ui == null)
        {
            Debug.LogError("[FlappyBirdGameEvent] Компонент FlappyBirdUI не найден на префабе!");
            return;
        }

        _controller = _gameRoot.GetComponent<FlappyBirdController>();
        if (_controller == null)
        {
            Debug.LogError("[FlappyBirdGameEvent] FlappyBirdController не найден на префабе! Добавь компонент вручную.");
            return;
        }

        _controller.Initialize(this, ui, _userDataService);
    }

    protected override void OnCleanup()
    {
        if (_gameRoot != null)
            Object.Destroy(_gameRoot);
    }

    public void OnGameEnded(int finalScore)
    {
        FinishGame(true, finalScore);
    }

    public void CloseGame()
    {
        FinishGame(false, 0);
    }
}
