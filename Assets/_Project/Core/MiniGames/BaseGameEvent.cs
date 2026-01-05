using System;
using UnityEngine;

public abstract class BaseGameEvent : IGameEvent
{
    public event Action<GameEventResult> OnGameFinished;
    
    protected Transform _parentContainer;
    protected float _startTime;
    protected bool _isGameActive;

    public virtual void Initialize(Transform parentContainer)
    {
        _parentContainer = parentContainer;
    }

    public virtual void StartGame()
    {
        _isGameActive = true;
        _startTime = Time.time;
        OnStartGame();
    }

    public virtual void Cleanup()
    {
        _isGameActive = false;
        OnCleanup();
    }

    protected void FinishGame(bool isSuccess, int score = 0)
    {
        if (!_isGameActive) return;
        
        _isGameActive = false;
        float timeSpent = Time.time - _startTime;
        
        var result = new GameEventResult
        {
            EventId = "",
            IsSuccess = isSuccess,
            Score = score,
            TimeSpent = timeSpent
        };
        
        OnGameFinished?.Invoke(result);
    }

    protected abstract void OnStartGame();
    protected abstract void OnCleanup();
}

