using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestWindow : BaseWindow
{
    [SerializeField] private TMP_Text[] _questFields;
    
    [SerializeField] private Button _backButton;
    
    public event Action OnBackClicked;
    public event Action OnWindowOpened;

    protected override void OnShow()
    {
        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        OnWindowOpened?.Invoke();
    }

    protected override void OnHide()
    {
        _backButton.onClick.RemoveAllListeners();
    }

    public void SetQuests(Quest[] quests)
    {
        for (int i = 0; i < quests.Length; i++)
        {
            _questFields[i].text = quests[i].title;
        }
    }
}
