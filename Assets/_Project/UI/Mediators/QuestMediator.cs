using System;
using System.Collections.Generic;
using UnityEditor;
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
    public int count;
}

public class QuestMediator : IInitializable, IDisposable
{
    private readonly QuestWindow _questWindow;
    private readonly UIManager _uiManager;
    private readonly APIService _apiService;
    private readonly QuestItemView.Factory _questItemFactory;
    
    private Quest[] _quests;
    private readonly List<QuestItemView> _spawnedItems = new();
    

    public QuestMediator(UIManager uiManager, APIService apiService, QuestWindow questWindow, QuestItemView.Factory questItemFactory)
    {
        _uiManager = uiManager;
        _apiService = apiService;
        _questWindow = questWindow;
        _questItemFactory = questItemFactory;
    }
    
    public void Initialize()
    {
        _questWindow.OnWindowOpened += () => LoadQuests();
        _questWindow.OnBackClicked += () => HandleBackClicked();
    }

    public void Dispose()
    {
        _questWindow.OnWindowOpened -= LoadQuests;
        _questWindow.OnBackClicked -= HandleBackClicked;
    }

    private async void LoadQuests()
    {
        var (success, message) = await _apiService.GetDailyQuests();
        if (!success)
        {
            Debug.LogWarning($"Failed to load quests: {message}");
            return;
        }

        var quests = PostProcessQuests(message);
        Debug.Log($"Quests loaded: {quests.Length}");

        foreach (var quest in quests)
        {
            var view = _questItemFactory.Create();
            view.SetData(quest);
            _spawnedItems.Add(view);
        }
    }

    private void ClearQuests()
    {
        _spawnedItems.Clear();
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
        ClearQuests();
    }
}
