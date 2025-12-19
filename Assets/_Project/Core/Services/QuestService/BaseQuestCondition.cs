using System;
using UnityEngine;

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
        if (!IsCompleted)
        {
            UpdateProgress(CurrentProgress + amount);
        }
    }
}