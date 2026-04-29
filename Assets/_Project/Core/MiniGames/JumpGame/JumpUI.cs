using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ActiveGrad.MiniGames;

public enum JumpScreen
{
    Start,
    Game,
    End
}

public class JumpUI : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject _startScreen;
    [SerializeField] private GameObject _gameScreen;
    [SerializeField] private GameObject _endScreen;

    [Header("Start Screen")]
    [SerializeField] private Button _startButton;
    [SerializeField] private TMP_Text _skillText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _rulesText;

    [Header("Game Screen")]
    [SerializeField] private RectTransform _player;
    [SerializeField] private RectTransform _playerHead;
    [SerializeField] private RectTransform _sliderBackground;
    [SerializeField] private RectTransform _zonesContainer;
    [SerializeField] private RectTransform _sliderIndicator;
    [SerializeField] private TMP_Text _jumpCounterText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _instructionText;

    [Header("End Screen")]
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private TMP_Text _totalScoreText;
    [SerializeField] private TMP_Text _jumpResultsText;
    [SerializeField] private BonusSliderComponent _bonusSliderComponent;
    [SerializeField] private GameObject _endScreenButtonsContainer;
    [SerializeField] private Button _finishButton;

    [Header("Background")]
    [SerializeField] private Image _background;

    public GameObject StartScreen => _startScreen;
    public GameObject GameScreen => _gameScreen;
    public GameObject EndScreen => _endScreen;
    public Button StartButton => _startButton;
    public TMP_Text SkillText => _skillText;
    public Button CloseButton => _closeButton;
    public TMP_Text TitleText => _titleText;
    public TMP_Text RulesText => _rulesText;
    public RectTransform Player => _player;
    public RectTransform PlayerHead => _playerHead;
    public RectTransform SliderBackground => _sliderBackground;
    public RectTransform ZonesContainer => _zonesContainer;
    public RectTransform SliderIndicator => _sliderIndicator;
    public TMP_Text JumpCounterText => _jumpCounterText;
    public TMP_Text ScoreText => _scoreText;
    public TMP_Text InstructionText => _instructionText;
    public TMP_Text ResultText => _resultText;
    public TMP_Text TotalScoreText => _totalScoreText;
    public TMP_Text JumpResultsText => _jumpResultsText;
    public BonusSliderComponent BonusSliderComponent => _bonusSliderComponent;
    public GameObject EndScreenButtonsContainer => _endScreenButtonsContainer;
    public Button FinishButton => _finishButton;
    public Image Background => _background;

    public void ShowScreen(JumpScreen screenType)
    {
        if (_startScreen != null) _startScreen.SetActive(screenType == JumpScreen.Start);
        if (_gameScreen != null) _gameScreen.SetActive(screenType == JumpScreen.Game);
        if (_endScreen != null) _endScreen.SetActive(screenType == JumpScreen.End);
    }

    public void SetJumpCounter(int current, int total)
    {
        if (_jumpCounterText != null)
            _jumpCounterText.text = $"Прыжок: {current}/{total}";
    }

    public void SetScore(int score)
    {
        if (_scoreText != null)
            _scoreText.text = $"Очки: {score}";
    }

    public void SetTotalScore(int totalScore)
    {
        if (_totalScoreText != null)
            _totalScoreText.text = $"Итого: {totalScore}";
    }

    public void SetInstruction(string text)
    {
        if (_instructionText != null)
            _instructionText.text = text;
    }

    public void SetInstructionWithColor(string text, Color color)
    {
        if (_instructionText != null)
        {
            _instructionText.text = text;
            _instructionText.color = color;
        }
    }

    public void SetSkillInfo(string skillName, int value)
    {
        if (_skillText != null)
            _skillText.text = $"{skillName}: {value}";
    }

    public void SetJumpResults(int[] scores)
    {
        if (_jumpResultsText == null) return;
        string result = "";
        for (int i = 0; i < scores.Length; i++)
        {
            result += $"Прыжок {i + 1}: +{scores[i]}\n";
        }
        _jumpResultsText.text = result;
    }
}
