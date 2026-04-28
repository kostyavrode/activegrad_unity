using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private Image _progressImageFiller;
    [SerializeField] private Image _completedIcon;
    
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
            if (_progressText != null)
            {
                _progressText.text = $"0/{quest.count}";
            }
            UpdateProgressFill(0, quest.count);
        }
    }
    
    public void UpdateProgress(QuestProgressData progress)
    {
        _progressData = progress;
        
        if (_progressText != null)
        {
            _progressText.text = $"{progress.currentProgress}/{progress.requiredCount}";
        }
        
        UpdateProgressFill(progress.currentProgress, progress.requiredCount);
        
        if (progress.isCompleted)
        {
            MarkAsCompleted();
        }
    }
    
    private void UpdateProgressFill(int currentProgress, int requiredCount)
    {
        if (_progressImageFiller != null && requiredCount > 0)
        {
            float fillAmount = (float)currentProgress / requiredCount;
            _progressImageFiller.fillAmount = Mathf.Clamp01(fillAmount);
        }
    }
    
    public void MarkAsCompleted()
    {
        if (_completedIcon != null)
        {
            _completedIcon.gameObject.SetActive(true);
        }
        
        if (_titleText != null)
        {
            _titleText.color = Color.green;
        }
        
        if (_progressImageFiller != null)
        {
            _progressImageFiller.fillAmount = 1f;
        }
    }
    
    public class Factory : Zenject.PlaceholderFactory<QuestItemView> { }
}