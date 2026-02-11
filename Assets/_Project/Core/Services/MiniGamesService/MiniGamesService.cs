using System;
using UnityEngine;
using Zenject;

public class MiniGamesService
{
    private readonly DiContainer _container;
    
    public MiniGamesService(DiContainer container)
    {
        _container = container;
    }
    
    public IGameEvent CreateGameInstance(GameEventType gameType)
    {
        switch (gameType)
        {
            case GameEventType.TrainPath:
                return _container.Instantiate<TrainPathGameEvent>();
            case GameEventType.Jump:
                return _container.Instantiate<JumpGameEvent>();
            case GameEventType.FlappyBird:
                return _container.Instantiate<FlappyBirdGameEvent>();
            case GameEventType.Quiz:
                return _container.Instantiate<QuizGameEvent>();
            case GameEventType.Puzzle:
                return _container.Instantiate<PuzzleGameEvent>();
            case GameEventType.Memory:
                return _container.Instantiate<MemoryGameEvent>();
            case GameEventType.Reaction:
                return _container.Instantiate<ReactionGameEvent>();
            default:
                Debug.LogWarning($"Unknown game type: {gameType}");
                return null;
        }
    }
}



