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
        
        condition.Initialize(quest.id, quest.count, ProgressData.currentProgress);
        condition.OnProgressChanged += HandleProgressChanged;
        condition.Subscribe();
        Debug.Log($"[QuestProgressTracker] Created tracker for Quest {quest.id}, Subscribe() called");
    }
    
    private void HandleProgressChanged(int questId, int newProgress)
    {
        if (questId != QuestId) return;
        
        ProgressData.currentProgress = newProgress;
        ProgressData.isCompleted = Condition.IsCompleted;
        ProgressData.lastUpdateDate = DateTime.Now.ToString("yyyy-MM-dd");
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