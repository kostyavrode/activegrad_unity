using System;
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
    
    private Quest[] _quests;
    

    public QuestMediator(UIManager uiManager, APIService apiService, QuestWindow questWindow)
    {
        _uiManager = uiManager;
        _apiService = apiService;
        _questWindow = questWindow;
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
        if (success)
        {
            Quest[] quests = PostProcessQuests(message);
            Debug.Log($"Quests loaded: {quests.Length}");
            _questWindow.SetQuests(quests);
        }
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
}
