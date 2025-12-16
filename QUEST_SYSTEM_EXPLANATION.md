# Объяснение системы квестов и создания объектов

## Как работает BindQuests в OtherInstaller

### 1. Регистрация в InstallBindings()

```csharp
private void BindQuests()
{
    // Регистрируем главный сервис отслеживания квестов
    Container.BindInterfacesAndSelfTo<QuestCompletionService>().AsSingle().NonLazy();
    
    // Регистрируем фабрику для создания условий квестов
    Container.BindFactory<SightMarkQuestCondition, SightMarkQuestCondition.Factory>()
        .AsTransient();
}
```

**Что происходит:**
- `QuestCompletionService` регистрируется как Singleton (один экземпляр на всё приложение)
- Реализует интерфейсы: `IInitializable`, `IDisposable`, `ITickable`
- `NonLazy()` означает, что объект создастся сразу при старте, а не при первом запросе
- Фабрика для `SightMarkQuestCondition` регистрируется для создания условий через Zenject

### 2. Регистрация фабрик в Start()

```csharp
public override void Start()
{
    base.Start();
    
    var questService = Container.Resolve<QuestCompletionService>();
    
    questService.RegisterConditionFactory("mark_sights", () =>
    {
        return Container.Instantiate<SightMarkQuestCondition>();
    });
}
```

**Что происходит:**
- `Start()` вызывается **после** создания всех биндингов, но **до** `Initialize()`
- Получаем уже созданный `QuestCompletionService` из контейнера
- Регистрируем фабрику в словаре `_conditionFactories` сервиса
- Ключ `"mark_sights"` должен совпадать с `quest.type` с сервера
- Фабрика - это `Func<IQuestCondition>`, которая создает условие через Zenject

---

## Жизненный цикл создания объектов квестов

### Этап 1: Инициализация приложения

```
1. Unity запускает сцену
   ↓
2. Zenject находит все MonoInstaller'ы в сцене
   ↓
3. Вызывается OtherInstaller.InstallBindings()
   ↓
4. Регистрируется QuestCompletionService
   ↓
5. Регистрируется фабрика SightMarkQuestCondition
   ↓
6. Вызывается OtherInstaller.Start()
   ↓
7. Регистрируются фабрики условий в QuestCompletionService
   ↓
8. Вызывается QuestCompletionService.Initialize()
   - Проверяется ежедневный сброс
   - Загружается сохраненный прогресс
```

### Этап 2: Загрузка квестов с сервера

**Когда:** При открытии окна квестов или при старте приложения (если настроено авто-загрузка)

```
1. QuestMediator.LoadQuests() или QuestCompletionService.LoadQuests()
   ↓
2. APIService.GetDailyQuests() → запрос к серверу
   ↓
3. Сервер возвращает JSON:
   {
     "quests": [
       {
         "id": 1,
         "type": "mark_sights",  ← Важно: совпадает с ключом фабрики
         "title": "Отметься в 3 местах",
         "description": "...",
         "count": 3
       }
     ]
   }
   ↓
4. QuestCompletionService.ParseQuests() → парсит JSON в массив Quest
   ↓
5. Для каждого квеста вызывается CreateQuestTracker(quest)
```

### Этап 3: Создание трекера и условия

**Метод:** `QuestCompletionService.CreateQuestTracker(Quest quest)`

```csharp
private void CreateQuestTracker(Quest quest)
{
    // 1. Получаем тип условия из квеста
    string conditionType = GetConditionType(quest); // "mark_sights"
    
    // 2. Ищем фабрику по типу
    if (!_conditionFactories.TryGetValue(conditionType, out var factory))
    {
        Debug.LogWarning($"Unknown condition type: {conditionType}");
        return; // Если фабрика не найдена - пропускаем квест
    }
    
    // 3. Вызываем фабрику → создается условие
    var condition = factory(); // ← ЗДЕСЬ ПРОИСХОДИТ СОЗДАНИЕ
    
    // 4. Создаем трекер с условием
    var tracker = new QuestProgressTracker(quest, condition);
    
    // 5. Подписываемся на изменения прогресса
    tracker.Condition.OnProgressChanged += (questId, progress) =>
    {
        OnQuestProgressChanged?.Invoke(questId, progress);
        CheckQuestCompletion(questId);
    };
    
    // 6. Сохраняем трекер в словарь
    _activeQuests[quest.id] = tracker;
    
    // 7. Загружаем сохраненный прогресс (если есть)
    LoadQuestProgress(quest.id);
}
```

### Этап 4: Детали создания условия

**Что происходит при вызове `factory()`:**

```csharp
// Фабрика зарегистрирована так:
questService.RegisterConditionFactory("mark_sights", () =>
{
    return Container.Instantiate<SightMarkQuestCondition>();
});

// При вызове factory():
1. Container.Instantiate<SightMarkQuestCondition>()
   ↓
2. Zenject анализирует конструктор SightMarkQuestCondition:
   public SightMarkQuestCondition(APIService apiService)
   ↓
3. Zenject ищет APIService в контейнере
   ↓
4. Создает экземпляр SightMarkQuestCondition с инжектированным APIService
   ↓
5. Возвращает готовый объект
```

**Что происходит в QuestProgressTracker:**

```csharp
public QuestProgressTracker(Quest quest, IQuestCondition condition)
{
    QuestId = quest.id;
    QuestData = quest;
    Condition = condition;
    
    // Создаем структуру данных прогресса
    ProgressData = new QuestProgressData { ... };
    
    // Инициализируем условие с параметрами квеста
    condition.Initialize(quest.id, quest.count, ProgressData.currentProgress);
    
    // Подписываемся на изменения прогресса
    condition.OnProgressChanged += HandleProgressChanged;
    
    // ⬇️ ВАЖНО: Подписываем условие на игровые события
    condition.Subscribe(); // ← Вызывает SightMarkQuestCondition.Subscribe()
}
```

**В Subscribe():**

```csharp
public override void Subscribe()
{
    // Подписываемся на событие отметки мест
    SightMarkedEvent.OnSightMarked += HandleSightMarked;
}
```

Теперь условие слушает события и будет обновлять прогресс!

---

## Когда создаются объекты

### Время создания:

1. **QuestCompletionService** - создается при старте сцены (NonLazy)
2. **Условия квестов (SightMarkQuestCondition)** - создаются **лениво** (lazy):
   - Только когда загружаются квесты с сервера
   - Только для активных квестов
   - По одному экземпляру на каждый квест

### Пример временной линии:

```
T=0s:  Запуск приложения
       ↓
T=1s:  OtherInstaller.InstallBindings()
       - Регистрируется QuestCompletionService
       - Регистрируется фабрика
       ↓
T=2s:  OtherInstaller.Start()
       - Регистрируются фабрики в сервисе
       ↓
T=3s:  QuestCompletionService.Initialize()
       - Проверка ежедневного сброса
       - Загрузка сохраненного прогресса
       ↓
T=10s: Игрок открывает окно квестов
       ↓
T=11s: QuestMediator.LoadQuests()
       ↓
T=12s: QuestCompletionService.LoadQuests()
       - Запрос к серверу
       ↓
T=13s: Получен ответ с сервера
       ↓
T=14s: CreateQuestTracker() для каждого квеста
       - factory() → создается SightMarkQuestCondition
       - Создается QuestProgressTracker
       - condition.Subscribe() → подписка на события
       ↓
T=15s: Квесты отображаются в UI
       ↓
T=30s: Игрок отмечается в месте
       - APIService.SetSightMarked(123)
       - SightMarkedEvent.Invoke(123)
       ↓
T=31s: SightMarkQuestCondition.HandleSightMarked(123)
       - IncrementProgress()
       - OnProgressChanged(1, 1)
       ↓
T=32s: QuestProgressTracker обновляет прогресс
       - CheckQuestCompletion()
       - Если выполнено → уведомление
```

---

## Что еще нужно добавить для работы системы

### ✅ Уже сделано:

1. ✅ `BindQuests()` дописан в `OtherInstaller`
2. ✅ Фабрика зарегистрирована
3. ✅ `SightMarkQuestCondition` обновлен (Subscribe/Unsubscribe)
4. ✅ Создан `SightMarkedEvent`
5. ✅ `APIService` обновлен для вызова события

### ⚠️ Что нужно проверить/добавить:

#### 1. Проверить тип квеста с сервера

**Важно:** Сервер должен возвращать квесты с полем `type`, которое совпадает с ключом фабрики:

```json
{
  "quests": [
    {
      "id": 1,
      "type": "mark_sights",  ← Должно совпадать с ключом в RegisterConditionFactory
      "title": "Отметься в 3 местах",
      "count": 3
    }
  ]
}
```

Если сервер возвращает другой тип (например, `"visit_sights"`), нужно либо:
- Изменить `ConditionType` в `SightMarkQuestCondition` на `"visit_sights"`
- Или добавить маппинг в `GetConditionType()`:

```csharp
private string GetConditionType(Quest quest)
{
    // Маппинг типов с сервера на типы условий
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

#### 2. Обновить QuestMediator (опционально, но рекомендуется)

Для отображения прогресса квестов в UI:

```csharp
public class QuestMediator : IInitializable, IDisposable
{
    // ... существующие поля ...
    private readonly QuestCompletionService _questService; // Добавить
    
    public QuestMediator(
        // ... существующие параметры ...
        QuestCompletionService questService) // Добавить
    {
        // ...
        _questService = questService; // Добавить
    }
    
    public void Initialize()
    {
        _questWindow.OnWindowOpened += LoadQuests;
        _questWindow.OnBackClicked += HandleBackClicked;
        
        // Подписываемся на события прогресса
        _questService.OnQuestProgressChanged += HandleQuestProgressChanged;
        _questService.OnQuestCompleted += HandleQuestCompleted;
    }
    
    private async void LoadQuests()
    {
        // Загружаем квесты через сервис
        _questService.LoadQuests();
        
        // Получаем все активные квесты
        var questTrackers = _questService.GetAllQuests();
        
        foreach (var tracker in questTrackers)
        {
            var view = _questItemFactory.Create();
            view.SetData(tracker.QuestData, tracker.ProgressData);
            _spawnedItems.Add(view);
        }
    }
    
    private void HandleQuestProgressChanged(int questId, int progress)
    {
        // Обновить UI элемента квеста
        Debug.Log($"Quest {questId} progress: {progress}");
    }
    
    private void HandleQuestCompleted(int questId)
    {
        Debug.Log($"Quest {questId} completed!");
    }
    
    public void Dispose()
    {
        // ... существующий код ...
        _questService.OnQuestProgressChanged -= HandleQuestProgressChanged;
        _questService.OnQuestCompleted -= HandleQuestCompleted;
    }
}
```

#### 3. Проверить IPopupService

`QuestCompletionService` использует `IPopupService` для показа уведомлений. Убедитесь, что:
- `PopupService` реализует `IPopupService`
- Есть метод `ShowSuccess(string message)`

Если метода нет, добавьте в `PopupService`:

```csharp
public void ShowSuccess(string message)
{
    // Реализация показа успешного уведомления
    ShowPopup(message, PopupType.Success);
}
```

#### 4. Тестирование

Для проверки работы системы:

1. Запустите игру
2. Откройте окно квестов
3. Проверьте логи:
   - `[QuestService] Loaded X quests`
   - `[QuestTracker] Quest Y progress: 0/3`
4. Отметьтесь в достопримечательности
5. Проверьте логи:
   - `[SightMarkedEvent] Sight X marked`
   - `[QuestTracker] Quest Y progress: 1/3`
6. После отметки в нужном количестве мест:
   - Должно появиться уведомление
   - `[QuestService] Quest Y completed!`

---

## Резюме: Как создаются объекты квестов

1. **При старте:** Создается один `QuestCompletionService` (Singleton)
2. **При загрузке квестов:** Для каждого квеста с сервера:
   - Вызывается фабрика по типу квеста (`"mark_sights"`)
   - Zenject создает `SightMarkQuestCondition` с инжекцией `APIService`
   - Создается `QuestProgressTracker` с этим условием
   - Условие подписывается на `SightMarkedEvent`
3. **При игровых событиях:** 
   - `APIService` вызывает `SightMarkedEvent.Invoke()`
   - Все активные условия получают событие
   - Обновляется прогресс
   - Проверяется завершение квеста

**Ключевой момент:** Условия создаются **динамически** при загрузке квестов, а не при старте приложения. Это позволяет системе работать с любым количеством квестов разных типов.

