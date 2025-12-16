using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private TMP_Text _progressText; // Опционально: для отображения прогресса
    [SerializeField] private Image _completedIcon; // Опционально: иконка завершения
    
    private int _questId;
    private QuestProgressData _progressData;

    public int QuestId => _questId;

    public void SetData(Quest quest, QuestProgressData progressData = null)
    {
        _questId = quest.id;
        _titleText.text = quest.title;
        _descriptionText.text = quest.description;
        _countText.text = $"x{quest.count}";
        
        if (progressData != null)
        {
            _progressData = progressData;
            UpdateProgress(progressData);
        }
        else
        {
            // Если прогресс не передан, показываем 0
            if (_progressText != null)
            {
                _progressText.text = $"0/{quest.count}";
            }
        }
    }
    
    public void UpdateProgress(QuestProgressData progress)
    {
        _progressData = progress;
        
        if (_progressText != null)
        {
            _progressText.text = $"{progress.currentProgress}/{progress.requiredCount}";
        }
        
        // Если квест завершен, обновляем визуально
        if (progress.isCompleted)
        {
            MarkAsCompleted();
        }
    }
    
    public void MarkAsCompleted()
    {
        if (_completedIcon != null)
        {
            _completedIcon.gameObject.SetActive(true);
        }
        
        // Можно изменить цвет текста или добавить другой визуальный эффект
        if (_titleText != null)
        {
            _titleText.color = Color.green; // Или другой цвет для завершенных квестов
        }
    }
    
    public class Factory : Zenject.PlaceholderFactory<QuestItemView> { }
}