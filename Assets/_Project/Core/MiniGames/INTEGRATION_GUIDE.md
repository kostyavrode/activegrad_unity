# Интеграция мини-игр в проект

## Текущая структура

### Файлы:
- `IGameEvent.cs` - интерфейс для всех игр
- `BaseGameEvent.cs` - базовый класс для игр
- `GameEventTypes.cs` - enum типов игр
- `MiniGamesService.cs` - сервис для создания игр
- `Games/FlappyBirdGameEvent.cs` - реализация Flappy Bird
- `Games/QuizGameEvent.cs` - заглушка для Quiz
- `Games/PuzzleGameEvent.cs` - заглушка для Puzzle
- `Games/MemoryGameEvent.cs` - заглушка для Memory
- `Games/ReactionGameEvent.cs` - заглушка для Reaction

## Как добавить новую игру

### Шаг 1: Добавить тип в enum

Откройте `Assets/_Project/Core/MiniGames/GameEventTypes.cs`:

```csharp
public enum GameEventType
{
    FlappyBird,
    YourNewGame,  // ← Добавьте сюда
    Quiz,
    Puzzle,
    Memory,
    Reaction
}
```

### Шаг 2: Создать класс игры

Создайте файл `Assets/_Project/Core/MiniGames/Games/YourNewGameEvent.cs`:

```csharp
using UnityEngine;

public class YourNewGameEvent : BaseGameEvent
{
    protected override void OnStartGame()
    {
        // Создайте UI элементы здесь
        var gameUI = new GameObject("YourGameUI");
        gameUI.transform.SetParent(_parentContainer, false);
        
        // Ваша игровая логика
    }

    protected override void OnCleanup()
    {
        // Очистите все созданные объекты
        if (_parentContainer != null)
        {
            foreach (Transform child in _parentContainer)
            {
                Object.Destroy(child.gameObject);
            }
        }
    }
}
```

### Шаг 3: Добавить в MiniGamesService

Откройте `Assets/_Project/Core/Services/MiniGamesService/MiniGamesService.cs`:

```csharp
public IGameEvent CreateGameInstance(GameEventType gameType)
{
    switch (gameType)
    {
        case GameEventType.FlappyBird:
            return _container.Instantiate<FlappyBirdGameEvent>();
        case GameEventType.YourNewGame:  // ← Добавьте сюда
            return _container.Instantiate<YourNewGameEvent>();
        // ... остальные
    }
}
```

## Регистрация в Zenject

### ✅ Уже сделано:

1. **MiniGamesService** зарегистрирован в `OtherInstaller.cs`:
```csharp
Container.BindInterfacesAndSelfTo<MiniGamesService>().AsSingle();
```

2. **Игры создаются через Zenject** - не нужно регистрировать каждую игру отдельно!

### ❌ НЕ нужно регистрировать:

- ❌ Не нужно регистрировать `FlappyBirdGameEvent` отдельно
- ❌ Не нужно создавать Factory для каждой игры
- ❌ Не нужно добавлять в `OtherInstaller`

**Почему?** Потому что игры создаются через `_container.Instantiate<>()`, который автоматически разрешает зависимости через Zenject.

## Использование

### Пример создания игры:

```csharp
public class SomeService
{
    private readonly MiniGamesService _miniGamesService;
    
    public SomeService(MiniGamesService miniGamesService)
    {
        _miniGamesService = miniGamesService;
    }
    
    public void StartGame()
    {
        var game = _miniGamesService.CreateGameInstance(GameEventType.FlappyBird);
        game.Initialize(parentContainer);
        game.StartGame();
    }
}
```

## Требования к игре

1. ✅ Наследоваться от `BaseGameEvent`
2. ✅ Реализовать `OnStartGame()` - создание UI
3. ✅ Реализовать `OnCleanup()` - очистка ресурсов
4. ✅ Вызывать `FinishGame(success, score)` при завершении
5. ✅ Создавать UI внутри `_parentContainer`

## Flappy Bird - пример реализации

`FlappyBirdGameEvent` - это полный рабочий пример:
- ✅ Создает UI элементы
- ✅ Имеет игровую логику (через MonoBehaviour контроллер)
- ✅ Завершается с результатом
- ✅ Правильно очищает ресурсы

Используйте его как референс для создания новых игр!

