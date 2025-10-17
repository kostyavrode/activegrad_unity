using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestWindow : BaseWindow
{
    [SerializeField] private TMP_Text[] _questFields;
    [SerializeField] private Transform _contentParent;
    
    [SerializeField] private Button _backButton;
    
    public Transform ContentParent => _contentParent;
    
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
        ClearQuests();
    }

    public void ClearQuests()
    {
        QuestItemView[] quests = _contentParent.GetComponentsInChildren<QuestItemView>();
        foreach (var VARIABLE in quests)
        {
            Destroy(VARIABLE.gameObject);
        }
    }
}
