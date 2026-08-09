using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;


[Serializable]
public class QuestsWrapper
{
    public Quest[] quests;
}

[Serializable]
public class Quest
{
    public int id;
    public string title;
    public string description;
    public int count; // требуемое количество
    public string type; // тип условия: "visit_sights", "steps", "collect_coins" и т.д.
    public string reward_type; // тип награды: "coins", "experience", "item"
    public int reward_amount; // количество награды
}

public class QuestMediator : IInitializable, IDisposable
{
    private readonly QuestWindow _questWindow;
    private readonly UIManager _uiManager;
    private readonly APIService _apiService;
    private readonly QuestItemView.Factory _questItemFactory;
    private readonly QuestCompletionService _questService;
    
    private Quest[] _quests;
    private readonly List<QuestItemView> _spawnedItems = new();
    

    public QuestMediator(UIManager uiManager, APIService apiService, QuestWindow questWindow, QuestItemView.Factory questItemFactory, QuestCompletionService questService)
    {
        _uiManager = uiManager;
        _apiService = apiService;
        _questWindow = questWindow;
        _questItemFactory = questItemFactory;
        _questService = questService;
    }
    
    public void Initialize()
    {
        _questWindow.OnWindowOpened += HandleWindowOpened;
        _questWindow.OnBackClicked += HandleBackClicked;

        _questService.OnQuestProgressChanged += HandleQuestProgressChanged;
        _questService.OnQuestCompleted += HandleQuestCompleted;
    }

    public void Dispose()
    {
        _questWindow.OnWindowOpened -= HandleWindowOpened;
        _questWindow.OnBackClicked -= HandleBackClicked;

        _questService.OnQuestProgressChanged -= HandleQuestProgressChanged;
        _questService.OnQuestCompleted -= HandleQuestCompleted;

        ClearQuests();
    }

    private void HandleWindowOpened() => LoadQuests();

    private async void LoadQuests()
    {
        ClearQuests();

        var listState = UIListStatePresenter.GetOrCreate(_questWindow.ContentParent);
        listState.ShowLoading(3);

        await _questService.LoadQuestsAsync();
        
        var questTrackers = _questService.GetAllQuests();
        
        Debug.Log($"[QuestMediator] Displaying {questTrackers.Count} quests (only with registered factories)");

        if (questTrackers.Count == 0)
        {
            listState.ShowEmpty("Нет активных заданий");
            return;
        }

        listState.Hide();

        foreach (var tracker in questTrackers)
        {
            var view = _questItemFactory.Create();
            view.transform.SetParent(_questWindow.ContentParent, false);
            view.SetData(tracker.QuestData, tracker.ProgressData);
            _spawnedItems.Add(view);
        }

        if (_spawnedItems.Count > 0)
            _questWindow.PlayTabAnimation();
    }

    private void ClearQuests()
    {
        for (var i = _spawnedItems.Count - 1; i >= 0; i--)
        {
            if (_spawnedItems[i] != null)
                UnityEngine.Object.Destroy(_spawnedItems[i].gameObject);
        }

        _spawnedItems.Clear();
        _questWindow.ClearQuests();
    }

    private Quest[] PostProcessQuests(string message)
    {
        QuestsWrapper wrapper = JsonUtility.FromJson<QuestsWrapper>(message);
        Debug.Log($"Quests loaded: {wrapper.quests.Length}");
        Quest[] questsData = wrapper.quests;
        return questsData;
    }
    
    private void HandleBackClicked()
    {
        _uiManager.Back();
    }
    
    private void HandleQuestProgressChanged(int questId, int progress)
    {
        var view = _spawnedItems.FirstOrDefault(v => v != null && v.QuestId == questId);
        if (view != null)
        {
            var progressData = _questService.GetQuestProgress(questId);
            if (progressData != null)
            {
                view.UpdateProgress(progressData);
            }
        }
    }
    
    private void HandleQuestCompleted(int questId)
    {
        Debug.Log($"[QuestMediator] Quest {questId} completed!");
        var view = _spawnedItems.FirstOrDefault(v => v != null && v.QuestId == questId);
        if (view != null)
        {
            view.MarkAsCompleted();
        }
    }
}
