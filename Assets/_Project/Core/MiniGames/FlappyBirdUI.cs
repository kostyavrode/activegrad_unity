using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlappyBirdUI : MonoBehaviour
{
    [Header("Game Elements")]
    [SerializeField] private RectTransform _bird;
    [SerializeField] private Transform _pipesContainer;
    
    [Header("UI Elements")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _gameOverText;
    [SerializeField] private Button _finishButton;
    
    [Header("Background")]
    [SerializeField] private Image _background;
    
    public RectTransform Bird => _bird;
    public Transform PipesContainer => _pipesContainer;
    public TMP_Text ScoreText => _scoreText;
    public TMP_Text GameOverText => _gameOverText;
    public Button FinishButton => _finishButton;
    public Image Background => _background;
    
    public void SetScore(int score)
    {
        if (_scoreText != null)
            _scoreText.text = $"Score: {score}";
    }
    
    public void ShowGameOver(bool show)
    {
        if (_gameOverText != null)
            _gameOverText.gameObject.SetActive(show);
        if (_finishButton != null)
            _finishButton.gameObject.SetActive(show);
    }
}


