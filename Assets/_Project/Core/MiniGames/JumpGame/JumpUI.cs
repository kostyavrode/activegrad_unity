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

    public void SetStartScreen(GameObject screen) => _startScreen = screen;
    public void SetGameScreen(GameObject screen) => _gameScreen = screen;
    public void SetEndScreen(GameObject screen) => _endScreen = screen;
    public void SetStartButton(Button button) => _startButton = button;
    public void SetSkillText(TMP_Text text) => _skillText = text;
    public void SetCloseButton(Button button) => _closeButton = button;
    public void SetTitleText(TMP_Text text) => _titleText = text;
    public void SetRulesText(TMP_Text text) => _rulesText = text;
    public void SetPlayer(RectTransform player) => _player = player;
    public void SetPlayerHead(RectTransform head) => _playerHead = head;
    public void SetSliderBackground(RectTransform bg) => _sliderBackground = bg;
    public void SetZonesContainer(RectTransform container) => _zonesContainer = container;
    public void SetSliderIndicator(RectTransform indicator) => _sliderIndicator = indicator;
    public void SetJumpCounterText(TMP_Text text) => _jumpCounterText = text;
    public void SetScoreText(TMP_Text text) => _scoreText = text;
    public void SetInstructionText(TMP_Text text) => _instructionText = text;
    public void SetResultText(TMP_Text text) => _resultText = text;
    public void SetTotalScoreText(TMP_Text text) => _totalScoreText = text;
    public void SetJumpResultsText(TMP_Text text) => _jumpResultsText = text;
    public void SetBonusSliderComponent(BonusSliderComponent c) => _bonusSliderComponent = c;
    public void SetEndScreenButtonsContainer(GameObject go) => _endScreenButtonsContainer = go;
    public void SetFinishButton(Button button) => _finishButton = button;
    public void SetBackground(Image image) => _background = image;

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
