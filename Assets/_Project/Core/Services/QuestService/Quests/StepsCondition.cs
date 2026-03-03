using System;
using UnityEngine;
using Zenject;

/// <summary>
/// Условие квеста для отслеживания шагов.
/// Подписывается на IStepsService и обновляет прогресс при изменении количества шагов.
/// </summary>
public class StepsCondition : BaseQuestCondition
{
    public override string ConditionType => "steps";

    private readonly IStepsService _stepsService;

    public StepsCondition(IStepsService stepsService)
    {
        _stepsService = stepsService;
    }

    public override void Initialize(int questId, int requiredCount, int currentProgress = 0)
    {
        base.Initialize(questId, requiredCount, currentProgress);
        UpdateProgressFromService();
    }

    public override void Subscribe()
    {
        _stepsService.OnStepsChanged += HandleStepsChanged;
    }

    public override void Unsubscribe()
    {
        _stepsService.OnStepsChanged -= HandleStepsChanged;
    }

    private void HandleStepsChanged(int steps)
    {
        Debug.Log($"[StepsCondition] Steps changed: {steps}, updating progress for Quest {QuestId}");
        UpdateProgressFromService();
    }

    private void UpdateProgressFromService()
    {
        int steps = _stepsService?.StepsToday ?? 0;
        UpdateProgress(Mathf.Min(steps, RequiredCount));
    }

    public class Factory : PlaceholderFactory<StepsCondition> { }
}
