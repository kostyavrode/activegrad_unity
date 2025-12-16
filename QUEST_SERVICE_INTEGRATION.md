# Интеграция QuestService в проект

## Анализ текущего кода

### ✅ Что уже есть:
1. **QuestCompletionService** - главный сервис отслеживания
2. **QuestProgressTracker** - трекер прогресса
3. **BaseQuestCondition** - базовый класс условий
4. **SightMarkQuestCondition** - условие для отметки мест (но не подключено)
5. **Модель Quest** уже имеет поле `type`

### ❌ Что нужно добавить:
1. Событие для отметки мест (`SightMarkedEvent`)
2. Установщик Zenject для QuestService
3. Регистрация в `ProjectInstaller`
4. Обновление `APIService` для вызова события
5. Обновление `SightMarkQuestCondition` для подписки
6. Обновление `QuestMediator` для интеграции с сервисом

---

## Пошаговая инструкция

### Шаг 1: Создать событие для отметки мест

Создайте файл: `Assets/_Project/Core/Services/QuestService/Events/SightMarkedEvent.cs`

```csharp
using System;

/// <summary>
/// Статический класс для событий отметки достопримечательностей
/// </summary>
public static class SightMarkedEvent
{
    /// <summary>
    /// Событие вызывается когда игрок отмечается в достопримечательности
    /// </summary>
    public static event Action<int> OnSightMarked; // sightId
    
    /// <summary>
    /// Вызвать событие отметки места
    /// </summary>
    public static void Invoke(int sightId)
    {
        OnSightMarked?.Invoke(sightId);
        UnityEngine.Debug.Log($"[SightMarkedEvent] Sight {sightId} marked");
    }
}
```

### Шаг 2: Обновить APIService для вызова события

В файле `Assets/_Project/Core/Services/APIService.cs`, метод `SetSightMarked`:

```csharp
public async Task<(bool success, string message)> SetSightMarked(int sightID)
{
    if (!IsLoggedIn)
        return (false, "Not logged in");

    var url = $"{BaseUrl}landmarks/save/";

    var payload = new SaveSightRequest
    {
        player_id = _userData.ID,
        external_ids = new[] { sightID.ToString() }
    };

    var result = await SendRequest(url, "POST", payload, requireAuth: true);

    if (result.success)
    {
        Debug.Log("Set sight marked achieved successfully");
        
        // ⬇️ ДОБАВИТЬ ЭТУ СТРОКУ:
        SightMarkedEvent.Invoke(sightID);
    }

    return result;
}
```

### Шаг 3: Обновить SightMarkQuestCondition

В файле `Assets/_Project/Core/Services/QuestService/Quests/SightMarkQuestCondition.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SightMarkQuestCondition : BaseQuestCondition
{
    public override string ConditionType => "mark_sights"; // ⚠️ Важно: должно совпадать с типом с сервера
    
    private readonly APIService _apiService;
    private readonly HashSet<int> _visitedSights = new HashSet<int>();
    
    public SightMarkQuestCondition(APIService apiService)
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
        // ⬇️ ДОБАВИТЬ ПОДПИСКУ:
        SightMarkedEvent.OnSightMarked += HandleSightMarked;
    }
    
    public override void Unsubscribe()
    {
        // ⬇️ ДОБАВИТЬ ОТПИСКУ:
        SightMarkedEvent.OnSightMarked -= HandleSightMarked;
    }
    
    private void HandleSightMarked(int sightId)
    {
        if (!_visitedSights.Contains(sightId))
        {
            _visitedSights.Add(sightId);
            IncrementProgress();
        }
    }
}
```

### Шаг 4: Создать установщик Zenject для QuestService

Создайте файл: `Assets/_Project/Core/Installers/QuestServiceInstaller.cs`

```csharp
using System;
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
        
        // Регистрируем фабрики для условий квестов
        // Используем MemoryPool для переиспользования объектов
        Container.BindMemoryPool<SightMarkQuestCondition, SightMarkQuestCondition.Pool>()
            .WithInitialSize(5)
            .FromMethod(CreateSightMarkCondition);
        
        // Регистрируем фабрики в QuestCompletionService после его создания
        Container.BindExecutionOrder<QuestCompletionService>(-100);
    }
    
    private SightMarkQuestCondition CreateSightMarkCondition(DiContainer container)
    {
        return container.Instantiate<SightMarkQuestCondition>();
    }
}

// Фабрика для SightMarkQuestCondition
public class SightMarkQuestConditionFactory : PlaceholderFactory<SightMarkQuestCondition> { }
```

**Альтернативный вариант (проще, без MemoryPool):**

```csharp
using System;
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
    }
    
    // Вызывается после создания всех биндингов
    public override void Start()
    {
        base.Start();
        
        // Регистрируем фабрики условий в сервисе
        var questService = Container.Resolve<QuestCompletionService>();
        
        // Регистрируем фабрику для отметки мест
        questService.RegisterConditionFactory("mark_sights", () =>
        {
            return Container.Instantiate<SightMarkQuestCondition>();
        });
        
        // В будущем можно добавить другие типы:
        // questService.RegisterConditionFactory("steps", () => Container.Instantiate<StepsCondition>());
    }
}
```

**Рекомендую использовать второй вариант (проще и понятнее).**

### Шаг 5: Обновить ProjectInstaller

В файле `Assets/_Project/Core/Installers/ProjectInstaller.cs`:

```csharp
public class ProjectInstaller : MonoInstaller
{
    [SerializeField] private MonoBehaviour coroutineRunner;
    [SerializeField] private PopupView popupPrefab;
    [SerializeField] private SightDetailsView sightDetailsPrefab;
    [SerializeField] private AudioSource _audioRootPrefab;

    public override void InstallBindings()
    {
        BindAudio();
        
        Container.Bind<APIService>().AsSingle().WithArguments(coroutineRunner).NonLazy();
        Container.Bind<SceneLoader>().AsSingle().NonLazy();
        Container.Bind<UIManager>().AsSingle().NonLazy();
        Container.Bind<UserDataService>().AsSingle().NonLazy();
        Container.Bind<CharacterPreviewService>().AsSingle().NonLazy();
        Container.BindInterfacesTo<PopupService>().AsSingle();
        
        Container.BindFactory<PopupView, PopupView.Factory>()
            .FromComponentInNewPrefab(popupPrefab)
            .UnderTransformGroup("Popups");
        Container.BindFactory<SightDetailsView, SightDetailsView.Factory>()
            .FromComponentInNewPrefab(sightDetailsPrefab)
            .UnderTransformGroup("SightDetails");
        
        // ⬇️ ДОБАВИТЬ ЭТУ СТРОКУ (если используете отдельный установщик):
        // Container.Install<QuestServiceInstaller>();
        
        // ИЛИ регистрируем напрямую здесь:
        Container.BindInterfacesAndSelfTo<QuestCompletionService>()
            .AsSingle()
            .NonLazy();
    }
    
    // ⬇️ ДОБАВИТЬ ЭТОТ МЕТОД (если регистрируем напрямую):
    public override void Start()
    {
        base.Start();
        
        // Регистрируем фабрики условий после создания сервиса
        var questService = Container.Resolve<QuestCompletionService>();
        
        questService.RegisterConditionFactory("mark_sights", () =>
        {
            return Container.Instantiate<SightMarkQuestCondition>();
        });
    }

    private void BindAudio()
    {
        // ... существующий код ...
    }
}
```

### Шаг 6: Обновить QuestCompletionService для регистрации фабрик

В файле `Assets/_Project/Core/Services/QuestService/QuestCompletionService.cs`, метод `RegisterConditionFactories`:

```csharp
private void RegisterConditionFactories()
{
    // Этот метод теперь пустой, фабрики регистрируются из установщика
    // через метод RegisterConditionFactory
}
```

**Важно:** Фабрики должны быть зарегистрированы **до** вызова `LoadQuests()`, иначе квесты не смогут создавать условия.

### Шаг 7: Обновить QuestMediator для интеграции с сервисом

В файле `Assets/_Project/UI/Mediators/QuestMediator.cs`:

```csharp
public class QuestMediator : IInitializable, IDisposable
{
    private readonly QuestWindow _questWindow;
    private readonly UIManager _uiManager;
    private readonly APIService _apiService;
    private readonly QuestItemView.Factory _questItemFactory;
    private readonly QuestCompletionService _questService; // ⬇️ ДОБАВИТЬ
    
    private Quest[] _quests;
    private readonly List<QuestItemView> _spawnedItems = new();

    public QuestMediator(
        UIManager uiManager, 
        APIService apiService, 
        QuestWindow questWindow, 
        QuestItemView.Factory questItemFactory,
        QuestCompletionService questService) // ⬇️ ДОБАВИТЬ
    {
        _uiManager = uiManager;
        _apiService = apiService;
        _questWindow = questWindow;
        _questItemFactory = questItemFactory;
        _questService = questService; // ⬇️ ДОБАВИТЬ
    }
    
    public void Initialize()
    {
        _questWindow.OnWindowOpened += LoadQuests;
        _questWindow.OnBackClicked += HandleBackClicked;
        
        // ⬇️ ДОБАВИТЬ подписки на события квестов:
        _questService.OnQuestProgressChanged += HandleQuestProgressChanged;
        _questService.OnQuestCompleted += HandleQuestCompleted;
    }

    public void Dispose()
    {
        _questWindow.OnWindowOpened -= LoadQuests;
        _questWindow.OnBackClicked -= HandleBackClicked;
        
        // ⬇️ ДОБАВИТЬ отписки:
        _questService.OnQuestProgressChanged -= HandleQuestProgressChanged;
        _questService.OnQuestCompleted -= HandleQuestCompleted;
    }

    private async void LoadQuests()
    {
        // ⬇️ ИЗМЕНИТЬ: загружаем квесты через сервис
        _questService.LoadQuests();
        
        // Получаем все активные квесты из сервиса
        var questTrackers = _questService.GetAllQuests();
        
        foreach (var tracker in questTrackers)
        {
            var view = _questItemFactory.Create();
            view.SetData(tracker.QuestData, tracker.ProgressData); // ⬇️ Обновить SetData если нужно
            _spawnedItems.Add(view);
        }
    }
    
    // ⬇️ ДОБАВИТЬ обработчики событий:
    private void HandleQuestProgressChanged(int questId, int progress)
    {
        // Обновляем UI элемента квеста
        var view = _spawnedItems.FirstOrDefault(v => 
        {
            // Нужно добавить метод GetQuestId в QuestItemView
            // или хранить связь view -> questId
            return true; // временно
        });
        
        if (view != null)
        {
            var progressData = _questService.GetQuestProgress(questId);
            // Обновить прогресс в view
        }
    }
    
    private void HandleQuestCompleted(int questId)
    {
        Debug.Log($"Quest {questId} completed!");
        // Можно обновить UI для завершенного квеста
    }

    private void ClearQuests()
    {
        _spawnedItems.Clear();
    }

    private Quest[] PostProcessQuests(string message)
    {
        QuestsWrapper wrapper = JsonUtility.FromJson<QuestsWrapper>(message);
        Quest[] questsData = wrapper.quests;
        return questsData;
    }
    
    private void HandleBackClicked()
    {
        _uiManager.Back();
        ClearQuests();
    }
}
```

### Шаг 8: Обновить QuestItemView (опционально)

Если нужно отображать прогресс квеста, обновите `Assets/_Project/UI/Views/QuestItemView.cs`:

```csharp
public class QuestItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private TMP_Text _progressText; // ⬇️ ДОБАВИТЬ
    [SerializeField] private Slider _progressSlider; // ⬇️ ДОБАВИТЬ (опционально)
    
    private int _questId; // ⬇️ ДОБАВИТЬ для связи с трекером

    public void SetData(Quest quest)
    {
        _questId = quest.id; // ⬇️ ДОБАВИТЬ
        _titleText.text = quest.title;
        _descriptionText.text = quest.description;
        _countText.text = $"x{quest.count}";
    }
    
    // ⬇️ ДОБАВИТЬ метод для обновления прогресса:
    public void UpdateProgress(QuestProgressData progress)
    {
        _progressText.text = $"{progress.currentProgress}/{progress.requiredCount}";
        
        if (_progressSlider != null)
        {
            _progressSlider.value = (float)progress.currentProgress / progress.requiredCount;
        }
    }
    
    public int GetQuestId() => _questId; // ⬇️ ДОБАВИТЬ
    
    public class Factory : Zenject.PlaceholderFactory<QuestItemView> { }
}
```

### Шаг 9: Проверить тип квеста с сервера

**Важно:** Убедитесь, что сервер возвращает квесты с полем `type`, которое совпадает с `ConditionType` в условиях:

- Для `SightMarkQuestCondition`: `ConditionType = "mark_sights"`
- Сервер должен возвращать: `{ "type": "mark_sights", ... }`

Если тип отличается, измените `ConditionType` в `SightMarkQuestCondition` или добавьте маппинг в `GetConditionType`:

```csharp
private string GetConditionType(Quest quest)
{
    // Если сервер возвращает другой тип, можно сделать маппинг:
    switch (quest.type)
    {
        case "visit_sights":
        case "mark_sights":
            return "mark_sights";
        default:
            return quest.type ?? "mark_sights";
    }
}
```

---

## Как создаются Conditions

### Процесс создания:

1. **При загрузке квестов** (`QuestCompletionService.LoadQuests()`):
   - Получаем список квестов с сервера
   - Для каждого квеста вызывается `CreateQuestTracker(quest)`

2. **В `CreateQuestTracker`**:
   ```csharp
   string conditionType = GetConditionType(quest); // Получаем тип из квеста
   
   // Ищем фабрику по типу
   if (!_conditionFactories.TryGetValue(conditionType, out var factory))
       return; // Если фабрика не найдена - пропускаем квест
   
   // Создаем условие через фабрику
   var condition = factory(); // Вызываем Func<IQuestCondition>
   
   // Создаем трекер с этим условием
   var tracker = new QuestProgressTracker(quest, condition);
   ```

3. **Фабрика создает условие**:
   - Zenject инжектит зависимости (например, `APIService` в `SightMarkQuestCondition`)
   - Возвращает готовый экземпляр условия

4. **Трекер инициализирует условие**:
   - Вызывает `condition.Initialize(questId, requiredCount)`
   - Подписывается на `condition.OnProgressChanged`
   - Вызывает `condition.Subscribe()` для подписки на игровые события

### Пример потока для квеста "Отметись в 3 местах":

```
1. Сервер возвращает: { id: 1, type: "mark_sights", count: 3, ... }
   ↓
2. QuestCompletionService.CreateQuestTracker()
   - conditionType = "mark_sights"
   - Находит фабрику для "mark_sights"
   - factory() → создает SightMarkQuestCondition через Zenject
   ↓
3. QuestProgressTracker(quest, condition)
   - condition.Initialize(1, 3) // questId=1, requiredCount=3
   - condition.Subscribe() → подписывается на SightMarkedEvent
   ↓
4. Игрок отмечается в месте
   - APIService.SetSightMarked(123) → успех
   - SightMarkedEvent.Invoke(123)
   ↓
5. SightMarkQuestCondition.HandleSightMarked(123)
   - Добавляет 123 в _visitedSights
   - IncrementProgress() → CurrentProgress = 1
   - Вызывает OnProgressChanged(1, 1)
   ↓
6. QuestProgressTracker.HandleProgressChanged()
   - Обновляет ProgressData
   - Проверяет IsCompleted
   ↓
7. QuestCompletionService.CheckQuestCompletion()
   - Если выполнено → показывает уведомление
   - Сохраняет прогресс
```

---

## Проверка работы

### Чек-лист:

1. ✅ Создан `SightMarkedEvent.cs`
2. ✅ `APIService.SetSightMarked` вызывает `SightMarkedEvent.Invoke`
3. ✅ `SightMarkQuestCondition` подписывается на событие
4. ✅ `QuestServiceInstaller` зарегистрирован в `ProjectInstaller`
5. ✅ Фабрики условий зарегистрированы в `QuestCompletionService`
6. ✅ `QuestCompletionService` зарегистрирован в Zenject
7. ✅ `QuestMediator` использует `QuestCompletionService`

### Тестирование:

1. Запустите игру
2. Откройте окно квестов
3. Должны загрузиться квесты с сервера
4. Отметьтесь в достопримечательности
5. Проверьте логи:
   - `[SightMarkedEvent] Sight X marked`
   - `[QuestTracker] Quest Y progress: 1/3`
6. После отметки в 3 местах должно появиться уведомление о завершении квеста

---

## Добавление новых типов условий в будущем

Когда появятся шаги, добавьте:

1. **Создайте `StepsCondition.cs`**:
```csharp
public class StepsCondition : BaseQuestCondition
{
    public override string ConditionType => "steps";
    // ... реализация
}
```

2. **Создайте событие** (если еще нет):
```csharp
public static class StepsUpdatedEvent
{
    public static event Action<int> OnStepsUpdated;
    public static void Invoke(int steps) => OnStepsUpdated?.Invoke(steps);
}
```

3. **В `UserDataService.AddSteps`** добавьте:
```csharp
StepsUpdatedEvent.Invoke(_data.dailySteps);
```

4. **В `ProjectInstaller.Start`** добавьте:
```csharp
questService.RegisterConditionFactory("steps", () =>
{
    return Container.Instantiate<StepsCondition>();
});
```

Готово! Система автоматически начнет отслеживать квесты на шаги.

