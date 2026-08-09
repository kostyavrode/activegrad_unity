using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class GameEventView : MonoBehaviour
{
    [SerializeField] private Transform _gameContainer;
    [SerializeField] private Button _closeButton;
    
    private IGameEvent _currentGame;
    private string _eventId;
    private UIModalAnimator _modalAnimator;
    
    public event Action<string, GameEventResult> OnGameFinished;
    
    public Transform GameContainer => _gameContainer;
    public string EventId => _eventId;

    private void Awake()
    {
        _modalAnimator = GetComponent<UIModalAnimator>();
        if (_modalAnimator == null)
            _modalAnimator = gameObject.AddComponent<UIModalAnimator>();

        if (_closeButton != null)
            _closeButton.onClick.AddListener(Close);
    }

    public void Initialize(string eventId, IGameEvent game)
    {
        _eventId = eventId;
        _currentGame = game;
        
        if (_currentGame != null)
        {
            _currentGame.OnGameFinished += HandleGameFinished;
            _currentGame.Initialize(_gameContainer);
            _currentGame.StartGame();
        }
    }

    private void HandleGameFinished(GameEventResult result)
    {
        result.EventId = _eventId;
        OnGameFinished?.Invoke(_eventId, result);
        Close();
    }

    private void Close()
    {
        if (_modalAnimator != null && _modalAnimator.IsClosing)
            return;

        if (_currentGame != null)
        {
            _currentGame.OnGameFinished -= HandleGameFinished;
            _currentGame.Cleanup();
            _currentGame = null;
        }

        if (_modalAnimator != null)
        {
            _modalAnimator.PlayHide(() => Destroy(gameObject));
            return;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_currentGame != null)
        {
            _currentGame.OnGameFinished -= HandleGameFinished;
            _currentGame.Cleanup();
            _currentGame = null;
        }
        
        if (_closeButton != null)
            _closeButton.onClick.RemoveAllListeners();
    }
    
    public class Factory : PlaceholderFactory<GameEventView> { }
}
