using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SightMarkQuestCondition : BaseQuestCondition
{
    public override string ConditionType => "mark_sights";
    
    private readonly APIService _apiService;
    private readonly HashSet<int> _visitedSights = new HashSet<int>();
    
    public SightMarkQuestCondition(APIService apiService)
    {
        _apiService = apiService;
    }
    
    public override void Initialize(int questId, int requiredCount, int currentProgress = 0)
    {
        Debug.Log($"[SightMarkQuestCondition] Initialize called for Quest {questId}, currentProgress: {currentProgress}, RequiredCount: {requiredCount}, _visitedSights.Count: {_visitedSights.Count}");
        base.Initialize(questId, requiredCount, currentProgress);
        // Очищаем только если прогресс = 0 (первая инициализация)
        // Если прогресс > 0, значит уже были посещения, не очищаем
        if (currentProgress == 0 && _visitedSights.Count == 0)
        {
            _visitedSights.Clear(); // Очищаем только при первой инициализации
        }
    }
    
    public override void Subscribe()
    {
        SightMarkedEvent.OnSightMarked += HandleSightMarked;
        Debug.Log($"[SightMarkQuestCondition] Subscribed to SightMarkedEvent for Quest {QuestId}");
    }
    
    public override void Unsubscribe()
    {
        SightMarkedEvent.OnSightMarked -= HandleSightMarked;
    }
    
    private void HandleSightMarked(int sightId)
    {
        Debug.Log($"[SightMarkQuestCondition] HandleSightMarked called for Quest {QuestId}, sightId: {sightId}, already visited: {_visitedSights.Contains(sightId)}");
        if (!_visitedSights.Contains(sightId))
        {
            _visitedSights.Add(sightId);
            IncrementProgress();
            Debug.Log($"[QuestService] Quest {QuestId} progress: {CurrentProgress}/{RequiredCount} (sight {sightId} marked)");
        }
        else
        {
            Debug.Log($"[SightMarkQuestCondition] Quest {QuestId}: Sight {sightId} already visited, skipping");
        }
    }
    
    // Фабрика для Zenject
    public class Factory : PlaceholderFactory<SightMarkQuestCondition> { }
}