# Система отслеживания выполнения квестов

## Обзор архитектуры

Система построена на паттернах **Strategy** и **Observer**, что позволяет легко добавлять новые типы условий квестов без изменения основной логики.

## Компоненты системы

### 1. Интерфейс условия квеста

```csharp
// Assets/_Project/Core/Services/QuestService/IQuestCondition.cs

public interface IQuestCondition
{
    /// <summary>
    /// Тип условия (для идентификации с сервера)
    /// </summary>
    string ConditionType { get; }
    
    /// <summary>
    /// Текущий прогресс выполнения
    /// </summary>
    int CurrentProgress { get; }
    
    /// <summary>
    /// Требуемое количество для выполнения
    /// </summary>
    int RequiredCount { get; }
    
    /// <summary>
    /// Проверяет, выполнено ли условие
    /// </summary>
    bool IsCompleted { get; }
    
    /// <summary>
    /// Инициализация условия с параметрами квеста
    /// </summary>
    void Initialize(int questId, int requiredCount, int currentProgress = 0);
    
    /// <summary>
    /// Подписка на события для отслеживания прогресса
    /// </summary>
    void Subscribe();
    
    /// <summary>
    /// Отписка от событий
    /// </summary>
    void Unsubscribe();
    
    /// <summary>
    /// Событие изменения прогресса
    /// </summary>
    event Action<int, int> OnProgressChanged; // questId, newProgress
}
```

### 2. Базовый класс для условий

```csharp
// Assets/_Project/Core/Services/QuestService/BaseQuestCondition.cs

public abstract class BaseQuestCondition : IQuestCondition
{
    public abstract string ConditionType { get; }
    
    public int CurrentProgress { get; protected set; }
    public int RequiredCount { get; protected set; }
    public int QuestId { get; protected set; }
    
    public bool IsCompleted => CurrentProgress >= RequiredCount;
    
    public event Action<int, int> OnProgressChanged;
    
    public virtual void Initialize(int questId, int requiredCount, int currentProgress = 0)
    {
        QuestId = questId;
        RequiredCount = requiredCount;
        CurrentProgress = currentProgress;
    }
    
    public abstract void Subscribe();
    public abstract void Unsubscribe();
    
    protected void UpdateProgress(int newProgress)
    {
        if (newProgress != CurrentProgress)
        {
            CurrentProgress = Mathf.Min(newProgress, RequiredCount);
            OnProgressChanged?.Invoke(QuestId, CurrentProgress);
        }
    }
    
    protected void IncrementProgress(int amount = 1)
    {
        UpdateProgress(CurrentProgress + amount);
    }
}
```

### 3. Условие: отметка в местах

```csharp
// Assets/_Project/Core/Services/QuestService/Conditions/VisitSightsCondition.cs

using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class VisitSightsCondition : BaseQuestCondition
{
    public override string ConditionType => "visit_sights";
    
    private readonly APIService _apiService;
    private readonly HashSet<int> _visitedSights = new HashSet<int>();
    
    public VisitSightsCondition(APIService apiService)
    {
        _apiService = apiService;
    }
    
    public override void Initialize(int questId, int requiredCount, int currentProgress = 0)
    {
        base.Initialize(questId, requiredCount, currentProgress);
        _visitedSights.Clear();
    }
    
    public override void Subscribe()
    {
        // Подписываемся на событие отметки места
        SightMarkedEvent.OnSightMarked += HandleSightMarked;
    }
    
    public override void Unsubscribe()
    {
        SightMarkedEvent.OnSightMarked -= HandleSightMarked;
    }
    
    private void HandleSightMarked(int sightId)
    {
        // Проверяем, что это новое место (не дубликат)
        if (!_visitedSights.Contains(sightId))
        {
            _visitedSights.Add(sightId);
            IncrementProgress();
        }
    }
}

// Статический класс для событий отметки мест
// Можно разместить в SightService или отдельном файле
public static class SightMarkedEvent
{
    public static event Action<int> OnSightMarked;
    
    public static void Invoke(int sightId)
    {
        OnSightMarked?.Invoke(sightId);
    }
}
```

### 4. Условие: шаги (для будущего)

```csharp
// Assets/_Project/Core/Services/QuestService/Conditions/StepsCondition.cs

using System;
using UnityEngine;
using Zenject;

public class StepsCondition : BaseQuestCondition
{
    public override string ConditionType => "steps";
    
    private readonly UserDataService _userDataService;
    private int _initialSteps;
    
    public StepsCondition(UserDataService userDataService)
    {
        _userDataService = userDataService;
    }
    
    public override void Initialize(int questId, int requiredCount, int currentProgress = 0)
    {
        base.Initialize(questId, requiredCount, currentProgress);
        _initialSteps = _userDataService.Steps;
    }
    
    public override void Subscribe()
    {
        StepsUpdatedEvent.OnStepsUpdated += HandleStepsUpdated;
    }
    
    public override void Unsubscribe()
    {
        StepsUpdatedEvent.OnStepsUpdated -= HandleStepsUpdated;
    }
    
    private void HandleStepsUpdated(int totalSteps)
    {
        int stepsProgress = totalSteps - _initialSteps;
        UpdateProgress(stepsProgress);
    }
}

// Статический класс для событий обновления шагов
public static class StepsUpdatedEvent
{
    public static event Action<int> OnStepsUpdated;
    
    public static void Invoke(int steps)
    {
        OnStepsUpdated?.Invoke(steps);
    }
}
```

### 5. Трекер прогресса квеста

```csharp
// Assets/_Project/Core/Services/QuestService/QuestProgressTracker.cs

using System;
using UnityEngine;

[Serializable]
public class QuestProgressData
{
    public int questId;
    public string conditionType;
    public int currentProgress;
    public int requiredCount;
    public bool isCompleted;
    public bool rewardClaimed;
    public string lastUpdateDate;
}

public class QuestProgressTracker
{
    public int QuestId { get; private set; }
    public Quest QuestData { get; private set; }
    public IQuestCondition Condition { get; private set; }
    public QuestProgressData ProgressData { get; private set; }
    
    public bool IsCompleted => ProgressData.isCompleted;
    public bool IsRewardClaimed => ProgressData.rewardClaimed;
    
    public QuestProgressTracker(Quest quest, IQuestCondition condition)
    {
        QuestId = quest.id;
        QuestData = quest;
        Condition = condition;
        
        ProgressData = new QuestProgressData
        {
            questId = quest.id,
            conditionType = condition.ConditionType,
            currentProgress = 0,
            requiredCount = quest.count,
            isCompleted = false,
            rewardClaimed = false,
            lastUpdateDate = DateTime.Now.ToString("yyyy-MM-dd")
        };
        
        // Инициализируем условие
        condition.Initialize(quest.id, quest.count, ProgressData.currentProgress);
        
        // Подписываемся на изменения прогресса
        condition.OnProgressChanged += HandleProgressChanged;
        
        // Подписываемся на события
        condition.Subscribe();
    }
    
    private void HandleProgressChanged(int questId, int newProgress)
    {
        if (questId != QuestId) return;
        
        ProgressData.currentProgress = newProgress;
        ProgressData.isCompleted = Condition.IsCompleted;
        ProgressData.lastUpdateDate = DateTime.Now.ToString("yyyy-MM-dd");
        
        Debug.Log($"[QuestTracker] Quest {QuestId} progress: {newProgress}/{ProgressData.requiredCount}");
    }
    
    public void MarkRewardClaimed()
    {
        ProgressData.rewardClaimed = true;
    }
    
    public void Dispose()
    {
        if (Condition != null)
        {
            Condition.OnProgressChanged -= HandleProgressChanged;
            Condition.Unsubscribe();
        }
    }
}
```

### 6. Главный сервис отслеживания квестов

```csharp
// Assets/_Project/Core/Services/QuestService/QuestCompletionService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class QuestCompletionService : IInitializable, IDisposable, ITickable
{
    private readonly APIService _apiService;
    private readonly UserDataService _userDataService;
    private readonly IPopupService _popupService;
    private readonly Dictionary<string, Func<IQuestCondition>> _conditionFactories;
    private readonly Dictionary<int, QuestProgressTracker> _activeQuests = new Dictionary<int, QuestProgressTracker>();
    
    private const string QuestProgressKey = "QuestProgress";
    private string _lastQuestLoadDate;
    
    public event Action<int> OnQuestCompleted; // questId
    public event Action<int, int> OnQuestProgressChanged; // questId, progress
    
    public QuestCompletionService(
        APIService apiService,
        UserDataService userDataService,
        IPopupService popupService)
    {
        _apiService = apiService;
        _userDataService = userDataService;
        _popupService = popupService;
        
        // Регистрируем фабрики условий
        _conditionFactories = new Dictionary<string, Func<IQuestCondition>>();
    }
    
    public void Initialize()
    {
        RegisterConditionFactories();
        LoadQuestProgress();
        CheckDailyReset();
    }
    
    private void RegisterConditionFactories()
    {
        // Регистрируем фабрики для каждого типа условия
        // В реальной реализации это будет через Zenject
    }
    
    /// <summary>
    /// Регистрация фабрики условия (вызывается из установщика Zenject)
    /// </summary>
    public void RegisterConditionFactory(string conditionType, Func<IQuestCondition> factory)
    {
        _conditionFactories[conditionType] = factory;
    }
    
    /// <summary>
    /// Загрузка квестов с сервера и инициализация отслеживания
    /// </summary>
    public async void LoadQuests()
    {
        var (success, response) = await _apiService.GetDailyQuests();
        if (!success)
        {
            Debug.LogError($"[QuestService] Failed to load quests: {response}");
            return;
        }
        
        var quests = ParseQuests(response);
        _lastQuestLoadDate = DateTime.Now.ToString("yyyy-MM-dd");
        
        // Очищаем старые квесты
        ClearQuests();
        
        // Создаем трекеры для каждого квеста
        foreach (var quest in quests)
        {
            CreateQuestTracker(quest);
        }
        
        Debug.Log($"[QuestService] Loaded {quests.Length} quests");
    }
    
    /// <summary>
    /// Создание трекера для квеста
    /// </summary>
    private void CreateQuestTracker(Quest quest)
    {
        // Определяем тип условия из квеста (нужно добавить поле type в Quest)
        string conditionType = GetConditionType(quest);
        
        if (!_conditionFactories.TryGetValue(conditionType, out var factory))
        {
            Debug.LogWarning($"[QuestService] Unknown condition type: {conditionType}");
            return;
        }
        
        var condition = factory();
        var tracker = new QuestProgressTracker(quest, condition);
        
        // Подписываемся на события трекера
        tracker.Condition.OnProgressChanged += (questId, progress) =>
        {
            OnQuestProgressChanged?.Invoke(questId, progress);
            CheckQuestCompletion(questId);
        };
        
        _activeQuests[quest.id] = tracker;
        
        // Загружаем сохраненный прогресс
        LoadQuestProgress(quest.id);
    }
    
    /// <summary>
    /// Получение типа условия из квеста
    /// </summary>
    private string GetConditionType(Quest quest)
    {
        // Предполагаем, что в Quest есть поле type
        // Если нет, можно использовать description или другие поля
        // Временное решение - определяем по описанию или добавляем поле type
        return quest.type ?? "visit_sights"; // fallback
    }
    
    /// <summary>
    /// Проверка завершения квеста
    /// </summary>
    private async void CheckQuestCompletion(int questId)
    {
        if (!_activeQuests.TryGetValue(questId, out var tracker))
            return;
        
        if (!tracker.IsCompleted || tracker.IsRewardClaimed)
            return;
        
        Debug.Log($"[QuestService] Quest {questId} completed!");
        
        // Уведомляем о завершении
        OnQuestCompleted?.Invoke(questId);
        
        // Показываем уведомление
        _popupService.ShowSuccess($"Квест выполнен: {tracker.QuestData.title}");
        
        // Отмечаем награду как полученную (можно вызвать API для подтверждения)
        tracker.MarkRewardClaimed();
        SaveQuestProgress();
    }
    
    /// <summary>
    /// Получение прогресса квеста
    /// </summary>
    public QuestProgressData GetQuestProgress(int questId)
    {
        if (_activeQuests.TryGetValue(questId, out var tracker))
        {
            return tracker.ProgressData;
        }
        return null;
    }
    
    /// <summary>
    /// Получение всех активных квестов
    /// </summary>
    public List<QuestProgressTracker> GetAllQuests()
    {
        return _activeQuests.Values.ToList();
    }
    
    /// <summary>
    /// Проверка ежедневного сброса
    /// </summary>
    private void CheckDailyReset()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        
        if (_lastQuestLoadDate != today)
        {
            Debug.Log("[QuestService] Daily reset - clearing quests");
            ClearQuests();
            LoadQuests();
        }
    }
    
    /// <summary>
    /// Очистка всех квестов
    /// </summary>
    private void ClearQuests()
    {
        foreach (var tracker in _activeQuests.Values)
        {
            tracker.Dispose();
        }
        _activeQuests.Clear();
    }
    
    /// <summary>
    /// Сохранение прогресса квестов
    /// </summary>
    private void SaveQuestProgress()
    {
        var progressList = _activeQuests.Values
            .Select(t => t.ProgressData)
            .ToList();
        
        var wrapper = new QuestProgressWrapper { progress = progressList.ToArray() };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(QuestProgressKey, json);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Загрузка прогресса квестов
    /// </summary>
    private void LoadQuestProgress()
    {
        if (!PlayerPrefs.HasKey(QuestProgressKey))
            return;
        
        string json = PlayerPrefs.GetString(QuestProgressKey);
        var wrapper = JsonUtility.FromJson<QuestProgressWrapper>(json);
        
        foreach (var progress in wrapper.progress)
        {
            if (_activeQuests.TryGetValue(progress.questId, out var tracker))
            {
                tracker.ProgressData.currentProgress = progress.currentProgress;
                tracker.ProgressData.isCompleted = progress.isCompleted;
                tracker.ProgressData.rewardClaimed = progress.rewardClaimed;
                
                // Обновляем условие
                tracker.Condition.Initialize(
                    progress.questId,
                    progress.requiredCount,
                    progress.currentProgress
                );
            }
        }
    }
    
    /// <summary>
    /// Загрузка прогресса конкретного квеста
    /// </summary>
    private void LoadQuestProgress(int questId)
    {
        if (!PlayerPrefs.HasKey(QuestProgressKey))
            return;
        
        string json = PlayerPrefs.GetString(QuestProgressKey);
        var wrapper = JsonUtility.FromJson<QuestProgressWrapper>(json);
        
        var savedProgress = wrapper.progress.FirstOrDefault(p => p.questId == questId);
        if (savedProgress != null && _activeQuests.TryGetValue(questId, out var tracker))
        {
            tracker.ProgressData.currentProgress = savedProgress.currentProgress;
            tracker.ProgressData.isCompleted = savedProgress.isCompleted;
            tracker.ProgressData.rewardClaimed = savedProgress.rewardClaimed;
            
            tracker.Condition.Initialize(
                questId,
                savedProgress.requiredCount,
                savedProgress.currentProgress
            );
        }
    }
    
    public void Tick()
    {
        // Периодическая проверка ежедневного сброса
        CheckDailyReset();
    }
    
    public void Dispose()
    {
        ClearQuests();
    }
    
    private Quest[] ParseQuests(string json)
    {
        var wrapper = JsonUtility.FromJson<QuestsWrapper>(json);
        return wrapper.quests;
    }
}

[Serializable]
public class QuestProgressWrapper
{
    public QuestProgressData[] progress;
}
```

### 7. Расширенная модель квеста

```csharp
// Обновление существующей модели Quest
// Assets/_Project/UI/Mediators/QuestMediator.cs (дополнение)

[Serializable]
public class Quest
{
    public int id;
    public string title;
    public string description;
    public int count; // требуемое количество
    public string type; // тип условия: "visit_sights", "steps", "collect_coins" и т.д.
    public string reward_type; // тип награды: "coins", "experience", "item"
    public int reward_amount; // количество награды
}
```

### 8. Интеграция с существующим кодом

#### 8.1. Обновление APIService для отметки мест

```csharp
// В методе SetSightMarked после успешного запроса:

public async Task<(bool success, string message)> SetSightMarked(int sightID)
{
    // ... существующий код ...
    
    if (result.success)
    {
        Debug.Log("Set sight marked achieved successfully");
        
        // Вызываем событие для системы квестов
        SightMarkedEvent.Invoke(sightID);
    }
    
    return result;
}
```

#### 8.2. Обновление UserDataService для шагов

```csharp
// В методе AddSteps:

public void AddSteps(int amount)
{
    CheckDailyStepsReset();
    _data.dailySteps += amount;
    Save();
    
    // Вызываем событие для системы квестов
    StepsUpdatedEvent.Invoke(_data.dailySteps);
}
```

### 9. Установщик Zenject

```csharp
// Assets/_Project/Core/Installers/QuestServiceInstaller.cs

using UnityEngine;
using Zenject;

public class QuestServiceInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Регистрируем главный сервис
        Container.BindInterfacesAndSelfTo<QuestCompletionService>()
            .AsSingle()
            .NonLazy();
        
        // Регистрируем фабрики условий
        Container.BindFactory<VisitSightsCondition, VisitSightsCondition.Factory>();
        Container.BindFactory<StepsCondition, StepsCondition.Factory>();
        
        // После биндинга регистрируем фабрики в сервисе
        Container.BindInitializableExecutionOrder<QuestCompletionService>(-100);
    }
}

// Альтернативный вариант - через метод Initialize в QuestCompletionService
// где мы регистрируем фабрики через RegisterConditionFactory
```

### 10. Использование в QuestMediator

```csharp
// Обновление QuestMediator для отображения прогресса

public class QuestMediator : IInitializable, IDisposable
{
    // ... существующие поля ...
    private readonly QuestCompletionService _questService;
    
    public QuestMediator(
        UIManager uiManager, 
        APIService apiService, 
        QuestWindow questWindow, 
        QuestItemView.Factory questItemFactory,
        QuestCompletionService questService) // добавляем сервис
    {
        // ... существующий код ...
        _questService = questService;
    }
    
    public void Initialize()
    {
        _questWindow.OnWindowOpened += LoadQuests;
        _questWindow.OnBackClicked += HandleBackClicked;
        
        // Подписываемся на события прогресса квестов
        _questService.OnQuestProgressChanged += HandleQuestProgressChanged;
        _questService.OnQuestCompleted += HandleQuestCompleted;
    }
    
    public void Dispose()
    {
        _questWindow.OnWindowOpened -= LoadQuests;
        _questWindow.OnBackClicked -= HandleBackClicked;
        
        _questService.OnQuestProgressChanged -= HandleQuestProgressChanged;
        _questService.OnQuestCompleted -= HandleQuestCompleted;
    }
    
    private async void LoadQuests()
    {
        // Загружаем квесты через сервис
        _questService.LoadQuests();
        
        // Получаем все активные квесты
        var quests = _questService.GetAllQuests();
        
        foreach (var questTracker in quests)
        {
            var view = _questItemFactory.Create();
            view.SetData(questTracker.QuestData, questTracker.ProgressData);
            _spawnedItems.Add(view);
        }
    }
    
    private void HandleQuestProgressChanged(int questId, int progress)
    {
        // Обновляем UI элемента квеста
        var view = _spawnedItems.FirstOrDefault(v => v.QuestId == questId);
        if (view != null)
        {
            var tracker = _questService.GetQuestProgress(questId);
            view.UpdateProgress(tracker);
        }
    }
    
    private void HandleQuestCompleted(int questId)
    {
        // Обновляем UI для завершенного квеста
        var view = _spawnedItems.FirstOrDefault(v => v.QuestId == questId);
        if (view != null)
        {
            view.MarkAsCompleted();
        }
    }
}
```

## Поток работы системы

```
1. Инициализация приложения
   ↓
2. QuestCompletionService.Initialize()
   - Регистрирует фабрики условий
   - Загружает сохраненный прогресс
   - Проверяет ежедневный сброс
   ↓
3. Загрузка квестов с сервера
   - QuestCompletionService.LoadQuests()
   - Для каждого квеста создается QuestProgressTracker
   - Создается соответствующее IQuestCondition
   - Условие подписывается на события
   ↓
4. Игровые события
   - Игрок отмечается в месте → SightMarkedEvent.Invoke()
   - VisitSightsCondition обрабатывает событие
   - Обновляет прогресс через UpdateProgress()
   - QuestProgressTracker получает уведомление
   ↓
5. Проверка завершения
   - QuestCompletionService.CheckQuestCompletion()
   - Если выполнено → показывает уведомление
   - Отмечает награду как полученную
   - Сохраняет прогресс
   ↓
6. Ежедневный сброс
   - При смене дня автоматически очищаются квесты
   - Загружаются новые квесты с сервера
```

## Преимущества архитектуры

1. **Легкое расширение**: Добавление нового типа квеста = создание нового класса условия
2. **Разделение ответственности**: Каждый компонент отвечает за свою часть
3. **Слабая связанность**: Условия не знают друг о друге
4. **Тестируемость**: Каждый компонент можно тестировать отдельно
5. **Сохранение прогресса**: Прогресс сохраняется локально и восстанавливается при старте
6. **Событийная модель**: Использование событий для связи компонентов

## Пример добавления нового типа квеста

Допустим, нужно добавить квест "Собрать N монет":

```csharp
// 1. Создаем новый класс условия
public class CollectCoinsCondition : BaseQuestCondition
{
    public override string ConditionType => "collect_coins";
    
    private readonly UserDataService _userDataService;
    private int _initialCoins;
    
    public CollectCoinsCondition(UserDataService userDataService)
    {
        _userDataService = userDataService;
    }
    
    public override void Initialize(int questId, int requiredCount, int currentProgress = 0)
    {
        base.Initialize(questId, requiredCount, currentProgress);
        _initialCoins = _userDataService.Coins;
    }
    
    public override void Subscribe()
    {
        CoinsUpdatedEvent.OnCoinsUpdated += HandleCoinsUpdated;
    }
    
    public override void Unsubscribe()
    {
        CoinsUpdatedEvent.OnCoinsUpdated -= HandleCoinsUpdated;
    }
    
    private void HandleCoinsUpdated(int totalCoins)
    {
        int coinsCollected = totalCoins - _initialCoins;
        UpdateProgress(coinsCollected);
    }
}

// 2. Создаем событие для обновления монет
public static class CoinsUpdatedEvent
{
    public static event Action<int> OnCoinsUpdated;
    public static void Invoke(int coins) => OnCoinsUpdated?.Invoke(coins);
}

// 3. В UserDataService вызываем событие при изменении монет
// 4. Регистрируем фабрику в QuestServiceInstaller
// Готово! Система автоматически начнет отслеживать этот тип квестов
```

## Заключение

Данная архитектура обеспечивает гибкую и масштабируемую систему отслеживания квестов, которая легко интегрируется с существующим кодом и позволяет добавлять новые типы условий без изменения основной логики.

