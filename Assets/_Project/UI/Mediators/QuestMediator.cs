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
        _questWindow.OnWindowOpened += LoadQuests;
        _questWindow.OnBackClicked += HandleBackClicked;
    }

    public void Dispose()
    {
        _questWindow.OnWindowOpened -= LoadQuests;
        _questWindow.OnBackClicked -= HandleBackClicked;
    }

    private async void LoadQuests()
    {
        // Загружаем квесты через сервис (он сам вызовет API и создаст трекеры)
        _questService.LoadQuests();
        
        // Ждем немного, чтобы сервис успел загрузить квесты
        await System.Threading.Tasks.Task.Delay(500);
        
        // Получаем только те квесты, для которых созданы трекеры (т.е. есть фабрики)
        var questTrackers = _questService.GetAllQuests();
        
        Debug.Log($"[QuestMediator] Displaying {questTrackers.Count} quests (only with registered factories)");

        foreach (var tracker in questTrackers)
        {
            var view = _questItemFactory.Create();
            view.SetData(tracker.QuestData);
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
