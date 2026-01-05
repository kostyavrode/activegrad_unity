using System;
using UnityEngine;

public interface IGameEvent
{
    event Action<GameEventResult> OnGameFinished;
    
    void Initialize(Transform parentContainer);
    void StartGame();
    void Cleanup();
}

[Serializable]
public class GameEventResult
{
    public string EventId;
    public bool IsSuccess;
    public int Score;
    public float TimeSpent;
}

