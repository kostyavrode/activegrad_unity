using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

public class QuestCompletionService : IInitializable, IDisposable, ITickable
{
    private readonly APIService _apiService;
    private readonly UserDataService _userDataService;
    private readonly IPopupService _popupService;
    private readonly IStepsService _stepsService;
    private readonly IRewardService _rewardService; // может быть не забинжен — тогда текстовые попапы
    private readonly HashSet<int> _completingQuests = new HashSet<int>(); // идёт синхронизация с сервером
    private readonly HashSet<int> _rewardShownQuests = new HashSet<int>(); // церемония уже показана
    private readonly Dictionary<string, Func<IQuestCondition>> _conditionFactories;
    private readonly Dictionary<int, QuestProgressTracker> _activeQuests = new Dictionary<int, QuestProgressTracker>();
    
    private const string QuestProgressKey = "QuestProgress";
    private const string QuestLoadDateKey = "QuestLoadDate";
    private string _lastQuestLoadDate;
    
    public event Action<int> OnQuestCompleted; // questId
    public event Action<int, int> OnQuestProgressChanged; // questId, progress
    
    public QuestCompletionService(
        APIService apiService,
        UserDataService userDataService,
        IPopupService popupService,
        IStepsService stepsService,
        [InjectOptional] IRewardService rewardService = null)
    {
        _apiService = apiService;
        _userDataService = userDataService;
        _popupService = popupService;
        _stepsService = stepsService;
        _rewardService = rewardService;
        _conditionFactories = new Dictionary<string, Func<IQuestCondition>>();
    }
    
    public void Initialize()
    {
        _lastQuestLoadDate = PlayerPrefs.GetString(QuestLoadDateKey, string.Empty);
        LoadQuestProgress();
        CheckDailyReset();
    }
    

    public void RegisterConditionFactory(string conditionType, Func<IQuestCondition> factory)
    {
        _conditionFactories[conditionType] = factory;
        Debug.Log($"[QuestService] Registered condition factory for type: '{conditionType}' (Total: {_conditionFactories.Count})");
    }
    
    public async Task LoadQuestsAsync()
    {
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
        // Сохраняем в UTC — должно совпадать с CheckDailyReset
        _lastQuestLoadDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        PlayerPrefs.SetString(QuestLoadDateKey, _lastQuestLoadDate);
        PlayerPrefs.Save();
        
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
                SaveQuestProgress();
                OnQuestProgressChanged?.Invoke(questId, progress);
                CheckQuestCompletion(questId);
            };
            
            _activeQuests[quest.id] = tracker;
            LoadQuestProgress(quest.id);
            
            if (condition.ConditionType == "steps" && _stepsService != null)
            {
                int steps = _stepsService.StepsToday;
                int progress = Mathf.Min(steps, tracker.ProgressData.requiredCount);
                tracker.ProgressData.currentProgress = progress;
                tracker.ProgressData.isCompleted = progress >= tracker.ProgressData.requiredCount;
                condition.Initialize(quest.id, tracker.ProgressData.requiredCount, progress);
                OnQuestProgressChanged?.Invoke(quest.id, progress);
            }
            
            if (tracker.IsCompleted && !tracker.IsRewardClaimed)
            {
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
        string questType = quest.type;
        
        if (string.IsNullOrEmpty(questType))
        {
            return null;
        }
        
        switch (questType.ToLower())
        {
            case "visit_sights":
            case "mark_sights":
            case "sight_mark":
            case "visit_places":
                return "mark_sights"; // Все эти типы маппятся на mark_sights

            case "steps":
            case "walk":
                return "steps";
            
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

        // Прогресс может меняться повторно, пока идёт запрос — не запускаем синхронизацию дважды
        if (!_completingQuests.Add(questId))
            return;

        Debug.Log($"[QuestService] Quest {questId} completed locally! Showing reward, syncing with server in background...");

        // Награду показываем сразу, не дожидаясь сервера
        if (_rewardService != null && _rewardShownQuests.Add(questId))
            _rewardService.ShowQuestComplete(tracker.QuestData.title,
                BuildQuestRewards(tracker.QuestData.reward_type, tracker.QuestData.reward_amount));

        int stepsToSend = 0;
        bool success;
        APIService.QuestCompleteResponse response;
        try
        {
            if (tracker.Condition.ConditionType == "steps")
            {
                stepsToSend = _stepsService?.StepsToday ?? 0;
                var (stepsOk, stepsMsg) = await _apiService.UpdateDailySteps(stepsToSend);
                if (!stepsOk)
                    Debug.LogWarning($"[QuestService] Failed to sync steps: {stepsMsg}");
            }

            Debug.Log($"[QuestService] Sending completion to server for quest {questId}...");

            (success, response) = await _apiService.CompleteQuest(questId, stepsToSend);
        }
        catch (Exception ex)
        {
            _completingQuests.Remove(questId);
            Debug.LogError($"[QuestService] Error while completing quest {questId}: {ex}");
            return;
        }

        // Помечаем локально выполненным в любом случае — чтобы не слать повторные запросы
        tracker.MarkRewardClaimed();
        SaveQuestProgress();
        _completingQuests.Remove(questId);

        if (!success || response == null)
        {
            Debug.LogWarning($"[QuestService] Failed to complete quest {questId} on server.");
            return;
        }

        if (!response.success)
        {
            Debug.LogWarning($"[QuestService] Server rejected quest {questId} completion: {response.message}");
            return;
        }
        
        string message = response.message;
        bool levelUp = response.level_up_notification != null;
        
        if (levelUp)
        {
            var levelUpInfo = response.level_up_notification;
            int pointsGained = levelUpInfo.stat_upgrade_points_gained;
            if (_rewardService != null)
            {
                // Старый уровень берём до UpdatePlayerStats (ниже)
                int oldLevel = _userDataService.Level;
                if (oldLevel <= 0 || oldLevel >= levelUpInfo.new_level)
                    oldLevel = levelUpInfo.new_level - Mathf.Max(1, levelUpInfo.levels_gained);
                _rewardService.ShowLevelUp(oldLevel, levelUpInfo.new_level, pointsGained);
            }
            else
            {
                string levelMsg = pointsGained > 0
                    ? $"Уровень повышен до {levelUpInfo.new_level}! +{pointsGained} очков прокачки"
                    : $"Уровень повышен до {levelUpInfo.new_level}!";
                _popupService.ShowSuccess($"🎉 {message}\n{levelMsg}");
            }
            Debug.Log($"[QuestService] Level up! New level: {levelUpInfo.new_level}, Stat points gained: {pointsGained}");
        }
        else if (_rewardService == null)
        {
            _popupService.ShowSuccess($"Квест выполнен: {tracker.QuestData.title}");
        }
        
        if (response.player_stats != null)
        {
            UpdatePlayerStats(response.player_stats);
        }
        
        OnQuestCompleted?.Invoke(questId);
    }
    
    private static List<RewardEntry> BuildQuestRewards(string rewardType, int amount)
    {
        var list = new List<RewardEntry>(1);
        switch ((rewardType ?? "").ToLowerInvariant())
        {
            case "coins":
            case "coin":
                list.Add(RewardEntry.Coins(amount));
                break;
            case "experience":
            case "exp":
            case "xp":
                list.Add(RewardEntry.Xp(amount));
                break;
            case "item":
                list.Add(new RewardEntry("item", "Предмет", amount, RewardEntry.DefaultTint("item")));
                break;
            default:
                list.Add(new RewardEntry("reward", "Награда", amount, RewardEntry.DefaultTint("reward")));
                break;
        }
        return list;
    }

    private void UpdatePlayerStats(APIService.PlayerStats stats)
    {
        _userDataService.SetProfile(
            _userDataService.Data.gender,
            _userDataService.Data.boots,
            _userDataService.Data.pants,
            _userDataService.Data.tshirt,
            _userDataService.Data.cap,
            stats.coins,
            stats.level,
            stats.experience,
            _userDataService.Steps,
            _userDataService.FirstName,
            _userDataService.LastName,
            _userDataService.DateOfStart,
            _userDataService.ID,
            stats.strength > 0 ? stats.strength : 1,
            stats.intelligence > 0 ? stats.intelligence : 1,
            stats.agility > 0 ? stats.agility : 1,
            stats.stat_upgrade_points
        );
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
        if (!_apiService.IsLoggedIn)
            return;

        // Используем UTC — сервер работает в UTC, сброс происходит в 00:00 UTC
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (_lastQuestLoadDate != today)
        {
            Debug.Log($"[QuestService] Daily reset triggered (lastLoad={_lastQuestLoadDate}, todayUTC={today}) — reloading quests");
            // Чистим сохранённый прогресс, чтобы старые isCompleted=true не восстановились
            PlayerPrefs.DeleteKey(QuestProgressKey);
            PlayerPrefs.Save();
            ClearQuests();
            _ = LoadQuestsAsync();
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
    
    private float _nextDailyResetCheck;
    private const float DailyResetCheckInterval = 5f;

    public void Tick()
    {
        // Проверяем ежедневный сброс раз в несколько секунд: форматирование даты каждый кадр
        // давало лишние аллокации (и сборки мусора) на телефонах.
        // Проверяем только если не идет загрузка квестов
        if (_isLoadingQuests || Time.unscaledTime < _nextDailyResetCheck)
            return;

        _nextDailyResetCheck = Time.unscaledTime + DailyResetCheckInterval;
        CheckDailyReset();
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