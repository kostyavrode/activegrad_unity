using System;
using UnityEngine;
using Zenject;

/// <summary>
/// Условие квеста для отслеживания шагов
/// Пока что без логики отслеживания - просто создает квест для отображения
/// </summary>
public class StepsCondition : BaseQuestCondition
{
    public override string ConditionType => "steps";
    
    public StepsCondition()
    {
        // Пока что не требуется никаких зависимостей
        // В будущем можно добавить сервис для отслеживания шагов
    }
    
    public override void Initialize(int questId, int requiredCount, int currentProgress = 0)
    {
        Debug.Log($"[StepsCondition] Initialize called for Quest {questId}, currentProgress: {currentProgress}, RequiredCount: {requiredCount}");
        base.Initialize(questId, requiredCount, currentProgress);
    }
    
    public override void Subscribe()
    {
        // Пока что не подписываемся ни на какие события
        // В будущем здесь будет подписка на события шагов
        Debug.Log($"[StepsCondition] Subscribed to steps tracking for Quest {QuestId}");
    }
    
    public override void Unsubscribe()
    {
        // Пока что нечего отписывать
        Debug.Log($"[StepsCondition] Unsubscribed from steps tracking for Quest {QuestId}");
    }
    
    // Фабрика для Zenject
    public class Factory : PlaceholderFactory<StepsCondition> { }
}


