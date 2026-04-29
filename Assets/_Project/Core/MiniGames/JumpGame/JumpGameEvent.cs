using UnityEngine;
using Zenject;

public class JumpGameEvent : BaseGameEvent
{
    private GameObject _gameRoot;
    private JumpController _controller;

    [Inject] private UserDataService _userDataService;

    protected override void OnStartGame()
    {
        LoadGamePrefab();
    }

    private void LoadGamePrefab()
    {
        var prefab = Resources.Load<GameObject>("MiniGames/JumpGame");

        if (prefab == null)
        {
            Debug.LogError("[JumpGameEvent] Префаб JumpGame не найден в Resources/MiniGames/JumpGame.prefab");
            return;
        }

        _gameRoot = Object.Instantiate(prefab, _parentContainer);
        _gameRoot.name = "JumpGame";

        var ui = _gameRoot.GetComponent<JumpUI>();
        if (ui == null)
        {
            Debug.LogError("[JumpGameEvent] Компонент JumpUI не найден на префабе!");
            return;
        }

        _controller = _gameRoot.GetComponent<JumpController>();
        if (_controller == null)
            _controller = _gameRoot.AddComponent<JumpController>();

        _controller.Initialize(this, ui, _userDataService);
    }

    protected override void OnCleanup()
    {
        if (_gameRoot != null)
            Object.Destroy(_gameRoot);
    }

    public void OnGameEnded(int totalScore)
    {
        FinishGame(true, totalScore);
    }

    public void CloseGame()
    {
        FinishGame(false, 0);
    }
}
