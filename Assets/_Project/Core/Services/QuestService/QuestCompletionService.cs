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

    public void RegisterConditionFactory(string conditionType, Func<IQuestCondition> factory)
    {
        _conditionFactories[conditionType] = factory;
        Debug.Log($"[QuestService] Registered condition factory for type: '{conditionType}' (Total: {_conditionFactories.Count})");
    }
    
    public async void LoadQuests()
    {
        // Защита от множественных одновременных вызовов
        if (_isLoadingQuests)
        {
            Debug.LogWarning("[QuestService] LoadQuests already in progress, skipping...");
            return;
        }
        
        _isLoadingQuests = true;
        Debug.Log("[QuestService] Starting to load quests from server...");
        
        try
        {
            var (success, response) = await _apiService.GetDailyQuests();
        if (!success)
        {
            Debug.LogError($"[QuestService] Failed to load quests: {response}");
            return;
        }
        
        var quests = ParseQuests(response);
        _lastQuestLoadDate = DateTime.Now.ToString("yyyy-MM-dd");
        
        Debug.Log($"[QuestService] Received {quests.Length} quests from server");
        Debug.Log($"[QuestService] Available condition factories: {_conditionFactories.Count} ({string.Join(", ", _conditionFactories.Keys)})");
        
        ClearQuests();

        int successCount = 0;
        int failedCount = 0;

        foreach (var quest in quests)
        {
            if (CreateQuestTracker(quest))
            {
                successCount++;
            }
            else
            {
                failedCount++;
            }
        }
        
            Debug.Log($"[QuestService] Loaded {quests.Length} quests | Created: {successCount} | Failed: {failedCount}");
            Debug.Log($"[QuestService] Active quest trackers: {_activeQuests.Count}");
            
            // Выводим информацию о созданных трекерах
            foreach (var kvp in _activeQuests)
            {
                var tracker = kvp.Value;
                Debug.Log($"[QuestService]   Quest {tracker.QuestId} ({tracker.QuestData.title}): " +
                          $"Type={tracker.Condition.ConditionType}, " +
                          $"Progress={tracker.ProgressData.currentProgress}/{tracker.ProgressData.requiredCount}, " +
                          $"Completed={tracker.IsCompleted}");
            }
        }
        finally
        {
            _isLoadingQuests = false;
        }
    }

    private bool CreateQuestTracker(Quest quest)
    {
        string conditionType = GetConditionType(quest);
        
        // Если тип не указан - это ошибка данных, пропускаем квест
        if (string.IsNullOrEmpty(conditionType))
        {
            Debug.LogError($"[QuestService] ❌ Quest {quest.id} ({quest.title}): Missing quest type. Cannot initialize tracker.");
            return false;
        }
        
        if (!_conditionFactories.TryGetValue(conditionType, out var factory))
        {
            Debug.LogError($"[QuestService] ❌ Quest {quest.id} ({quest.title}): No factory for type '{conditionType}'. Available: {string.Join(", ", _conditionFactories.Keys)}");
            return false;
        }
        
        try
        {
            var condition = factory();
            var tracker = new QuestProgressTracker(quest, condition);
            
            tracker.Condition.OnProgressChanged += (questId, progress) =>
            {
                OnQuestProgressChanged?.Invoke(questId, progress);
                CheckQuestCompletion(questId);
            };
            
            _activeQuests[quest.id] = tracker;
            LoadQuestProgress(quest.id);
            
            // Проверяем, не завершен ли квест уже при загрузке (если был сохранен прогресс)
            if (tracker.IsCompleted && !tracker.IsRewardClaimed)
            {
                // Квест уже завершен, но награда не получена - вызываем проверку
                CheckQuestCompletion(quest.id);
            }
            
            Debug.Log($"[QuestService] ✓ Quest {quest.id} ({quest.title}): {condition.ConditionType}, Progress: {tracker.ProgressData.currentProgress}/{tracker.ProgressData.requiredCount}");
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[QuestService] ❌ ERROR creating tracker for quest {quest.id}: {ex.Message}");
            return false;
        }
    }
    
    private string GetConditionType(Quest quest)
    {
        // Получаем тип из квеста
        string questType = quest.type;
        
        // Если тип пустой или null - это ошибка данных, возвращаем null
        if (string.IsNullOrEmpty(questType))
        {
            return null;
        }
        
        // Маппинг типов с сервера на типы условий
        // Это позволяет обрабатывать разные варианты названий одного и того же типа
        switch (questType.ToLower())
        {
            case "visit_sights":
            case "mark_sights":
            case "sight_mark":
            case "visit_places":
                return "mark_sights"; // Все эти типы маппятся на mark_sights
            
            // В будущем можно добавить другие типы:
            // case "steps":
            // case "walk":
            //     return "steps";
            
            default:
                // Если тип неизвестен, возвращаем как есть (может быть зарегистрирован отдельно)
                return questType;
        }
    }

    private async void CheckQuestCompletion(int questId)
    {
        if (!_activeQuests.TryGetValue(questId, out var tracker))
            return;
        
        if (!tracker.IsCompleted || tracker.IsRewardClaimed)
            return;
        
        Debug.Log($"[QuestService] Quest {questId} completed!");
        
        OnQuestCompleted?.Invoke(questId);

        _popupService.ShowSuccess($"Квест выполнен: {tracker.QuestData.title}");

        tracker.MarkRewardClaimed();
        SaveQuestProgress();
    }

    public QuestProgressData GetQuestProgress(int questId)
    {
        if (_activeQuests.TryGetValue(questId, out var tracker))
        {
            return tracker.ProgressData;
        }
        return null;
    }

    public List<QuestProgressTracker> GetAllQuests()
    {
        return _activeQuests.Values.ToList();
    }

    private void CheckDailyReset()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        
        // Проверяем только если дата изменилась и если квесты еще не загружены
        if (_lastQuestLoadDate != today && _activeQuests.Count == 0)
        {
            Debug.Log("[QuestService] Daily reset - clearing quests and loading new ones");
            ClearQuests();
            LoadQuests();
        }
    }

    private void ClearQuests()
    {
        foreach (var tracker in _activeQuests.Values)
        {
            tracker.Dispose();
        }
        _activeQuests.Clear();
    }

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
                
                tracker.Condition.Initialize(
                    progress.questId,
                    progress.requiredCount,
                    progress.currentProgress
                );
            }
        }
    }
    
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
    
    private bool _isLoadingQuests = false;
    
    public void Tick()
    {
        // Проверяем ежедневный сброс, но не слишком часто
        // Проверяем только если не идет загрузка квестов
        if (!_isLoadingQuests)
        {
            CheckDailyReset();
        }
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