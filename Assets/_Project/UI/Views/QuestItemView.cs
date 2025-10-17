using TMPro;
using UnityEngine;

public class QuestItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _countText;

    public void SetData(Quest quest)
    {
        _titleText.text = quest.title;
        _descriptionText.text = quest.description;
        _countText.text = $"x{quest.count}";
    }
    
    public class Factory : Zenject.PlaceholderFactory<QuestItemView> { }

}