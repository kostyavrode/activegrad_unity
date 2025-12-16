# Как работает отслеживание квестов

## Текущая ситуация

Ваши квесты показывают `Progress=1/1, Completed=True` - это значит, что они были завершены ранее и сохранены в PlayerPrefs.

## Как отслеживается прогресс

### 1. Подписка на события

Когда создается трекер квеста:
```csharp
// QuestProgressTracker конструктор
condition.Subscribe(); // Подписывается на SightMarkedEvent
```

### 2. Обработка событий

Когда игрок отмечается в достопримечательности:
```
1. APIService.SetSightMarked() → вызывает SightMarkedEvent.Invoke(sightId)
2. SightMarkQuestCondition.HandleSightMarked() получает событие
3. Проверяет, не посещено ли уже это место
4. Если новое → добавляет в _visitedSights и вызывает IncrementProgress()
5. IncrementProgress() → вызывает OnProgressChanged
6. QuestProgressTracker.HandleProgressChanged() обновляет ProgressData
7. QuestCompletionService.CheckQuestCompletion() проверяет завершение
```

### 3. Проверка завершения

`CheckQuestCompletion()` вызывается только когда:
- Прогресс меняется через `OnProgressChanged` (строка 140 в QuestCompletionService)
- Квест завершен (`IsCompleted = true`)
- Награда еще не получена (`IsRewardClaimed = false`)

## Что происходит при завершении квеста

```csharp
private async void CheckQuestCompletion(int questId)
{
    // 1. Проверяет, что квест завершен и награда не получена
    if (!tracker.IsCompleted || tracker.IsRewardClaimed)
        return;
    
    // 2. Вызывает событие OnQuestCompleted
    OnQuestCompleted?.Invoke(questId);
    
    // 3. Показывает уведомление
    _popupService.ShowSuccess($"Квест выполнен: {tracker.QuestData.title}");
    
    // 4. Отмечает награду как полученную
    tracker.MarkRewardClaimed();
    
    // 5. Сохраняет прогресс
    SaveQuestProgress();
}
```

## Проблема: UI не обновляется

**QuestMediator** не подписан на события `OnQuestCompleted` и `OnQuestProgressChanged`, поэтому:
- UI не обновляется при изменении прогресса
- UI не показывает, что квест завершен
- Не отображаются уведомления о завершении

## Что нужно добавить

1. **Подписка на события в QuestMediator**
2. **Обновление UI при изменении прогресса**
3. **Отображение статуса завершенных квестов**
4. **Проверка завершенных квестов при загрузке** (если они уже завершены)

