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
    
    [Header("Game Screen")]
    [SerializeField] private RectTransform _player;
    [SerializeField] private RectTransform _sliderBackground;
    [SerializeField] private RectTransform _zonesContainer;
    [SerializeField] private RectTransform _sliderIndicator;
    [SerializeField] private TMP_Text _jumpCounterText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _instructionText;
    
    [Header("End Screen")]
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private TMP_Text _totalScoreText;
    [SerializeField] private BonusSliderComponent _bonusSliderComponent;
    [SerializeField] private GameObject _endScreenButtonsContainer;
    [SerializeField] private Button _finishButton;
    
    [Header("Background")]
    [SerializeField] private Image _background;

    public GameObject StartScreen => _startScreen;
    public GameObject GameScreen => _gameScreen;
    public GameObject EndScreen => _endScreen;
    public Button StartButton => _startButton;
    public RectTransform Player => _player;
    public RectTransform SliderBackground => _sliderBackground;
    public RectTransform ZonesContainer => _zonesContainer;
    public RectTransform SliderIndicator => _sliderIndicator;
    public TMP_Text JumpCounterText => _jumpCounterText;
    public TMP_Text ScoreText => _scoreText;
    public TMP_Text InstructionText => _instructionText;
    public TMP_Text ResultText => _resultText;
    public TMP_Text TotalScoreText => _totalScoreText;
    public BonusSliderComponent BonusSliderComponent => _bonusSliderComponent;
    public GameObject EndScreenButtonsContainer => _endScreenButtonsContainer;
    public Button FinishButton => _finishButton;
    public Image Background => _background;

    // Методы для программной установки ссылок
    public void SetStartScreen(GameObject screen) => _startScreen = screen;
    public void SetGameScreen(GameObject screen) => _gameScreen = screen;
    public void SetEndScreen(GameObject screen) => _endScreen = screen;
    public void SetStartButton(Button button) => _startButton = button;
    public void SetPlayer(RectTransform player) => _player = player;
    public void SetSliderBackground(RectTransform bg) => _sliderBackground = bg;
    public void SetZonesContainer(RectTransform container) => _zonesContainer = container;
    public void SetSliderIndicator(RectTransform indicator) => _sliderIndicator = indicator;
    public void SetJumpCounterText(TMP_Text text) => _jumpCounterText = text;
    public void SetScoreText(TMP_Text text) => _scoreText = text;
    public void SetInstructionText(TMP_Text text) => _instructionText = text;
    public void SetResultText(TMP_Text text) => _resultText = text;
    public void SetTotalScoreText(TMP_Text text) => _totalScoreText = text;
    public void SetBonusSliderComponent(BonusSliderComponent c) => _bonusSliderComponent = c;
    public void SetEndScreenButtonsContainer(GameObject go) => _endScreenButtonsContainer = go;
    public void SetFinishButton(Button button) => _finishButton = button;
    public void SetBackground(Image image) => _background = image;

    public void ShowScreen(JumpScreen screenType)
    {
        switch (screenType)
        {
            case JumpScreen.Start:
                if (_startScreen != null) _startScreen.SetActive(true);
                if (_gameScreen != null) _gameScreen.SetActive(false);
                if (_endScreen != null) _endScreen.SetActive(false);
                break;
            case JumpScreen.Game:
                if (_startScreen != null) _startScreen.SetActive(false);
                if (_gameScreen != null) _gameScreen.SetActive(true);
                if (_endScreen != null) _endScreen.SetActive(false);
                break;
            case JumpScreen.End:
                if (_startScreen != null) _startScreen.SetActive(false);
                if (_gameScreen != null) _gameScreen.SetActive(false);
                if (_endScreen != null) _endScreen.SetActive(true);
                break;
        }
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
            _totalScoreText.text = $"Итого очков: {totalScore}";
    }

    public void SetInstruction(string text)
    {
        if (_instructionText != null)
            _instructionText.text = text;
    }
}
