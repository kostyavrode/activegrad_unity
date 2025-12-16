using System;

public interface IQuestCondition
{
    string ConditionType { get; }
    
    int CurrentProgress { get; }
    
    int RequiredCount { get; }
    
    bool IsCompleted { get; }
    
    void Initialize(int questId, int requiredCount, int currentProgress = 0);
    
    void Subscribe();
    
    void Unsubscribe();
    
    event Action<int, int> OnProgressChanged; // questId, newProgress
}