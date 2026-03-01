using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ActiveGrad.MiniGames;

public enum TrainPathScreen
{
    Start,
    Game,
    End
}

public class TrainPathUI : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject _startScreen;
    [SerializeField] private GameObject _gameScreen;
    [SerializeField] private GameObject _endScreen;
    
    [Header("Start Screen")]
    [SerializeField] private Button _startButton;
    [SerializeField] private TMP_Text _skillText;
    [SerializeField] private Button _closeButton;
    
    [Header("Game Screen")]
    [SerializeField] private RectTransform _mapContainer;
    [SerializeField] private RectTransform _train;
    [SerializeField] private TMP_Text _timeText;
    [SerializeField] private TMP_Text _pathInfoText;
    
    [Header("End Screen")]
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private TMP_Text _playerTimeText;
    [SerializeField] private TMP_Text _optimalTimeText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private RectTransform _bonusSliderTrack;
    [SerializeField] private RectTransform _bonusSliderIndicator;
    [SerializeField] private TMP_Text _bonusLeftLabel;
    [SerializeField] private TMP_Text _bonusRightLabel;
    [SerializeField] private GameObject _endScreenButtonsContainer;
    [SerializeField] private BonusSliderComponent _bonusSliderComponent;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _finishButton;
    
    [Header("Background")]
    [SerializeField] private Image _background;

    public GameObject StartScreen => _startScreen;
    public GameObject GameScreen => _gameScreen;
    public GameObject EndScreen => _endScreen;
    public Button StartButton => _startButton;
    public TMP_Text SkillText => _skillText;
    public Button CloseButton => _closeButton;
    public RectTransform MapContainer => _mapContainer;
    public RectTransform Train => _train;
    public TMP_Text TimeText => _timeText;
    public TMP_Text PathInfoText => _pathInfoText;
    public TMP_Text ResultText => _resultText;
    public TMP_Text PlayerTimeText => _playerTimeText;
    public TMP_Text OptimalTimeText => _optimalTimeText;
    public TMP_Text ScoreText => _scoreText;
    public RectTransform BonusSliderTrack => _bonusSliderTrack;
    public RectTransform BonusSliderIndicator => _bonusSliderIndicator;
    public TMP_Text BonusLeftLabel => _bonusLeftLabel;
    public TMP_Text BonusRightLabel => _bonusRightLabel;
    public GameObject EndScreenButtonsContainer => _endScreenButtonsContainer;
    public BonusSliderComponent BonusSliderComponent => _bonusSliderComponent;
    public Button RestartButton => _restartButton;
    public Button FinishButton => _finishButton;
    public Image Background => _background;

    // Методы для программной установки ссылок
    public void SetStartScreen(GameObject screen) => _startScreen = screen;
    public void SetGameScreen(GameObject screen) => _gameScreen = screen;
    public void SetEndScreen(GameObject screen) => _endScreen = screen;
    public void SetStartButton(Button button) => _startButton = button;
    public void SetSkillText(TMP_Text text) => _skillText = text;
    public void SetCloseButton(Button button) => _closeButton = button;
    public void SetMapContainer(RectTransform container) => _mapContainer = container;
    public void SetTrain(RectTransform train) => _train = train;
    public void SetTimeText(TMP_Text text) => _timeText = text;
    public void SetPathInfoText(TMP_Text text) => _pathInfoText = text;
    public void SetResultText(TMP_Text text) => _resultText = text;
    public void SetPlayerTimeText(TMP_Text text) => _playerTimeText = text;
    public void SetOptimalTimeText(TMP_Text text) => _optimalTimeText = text;
    public void SetScoreText(TMP_Text text) => _scoreText = text;
    public void SetBonusSliderTrack(RectTransform rt) => _bonusSliderTrack = rt;
    public void SetBonusSliderIndicator(RectTransform rt) => _bonusSliderIndicator = rt;
    public void SetBonusLeftLabel(TMP_Text text) => _bonusLeftLabel = text;
    public void SetBonusRightLabel(TMP_Text text) => _bonusRightLabel = text;
    public void SetEndScreenButtonsContainer(GameObject go) => _endScreenButtonsContainer = go;
    public void SetBonusSliderComponent(BonusSliderComponent c) => _bonusSliderComponent = c;
    public void SetRestartButton(Button button) => _restartButton = button;
    public void SetFinishButton(Button button) => _finishButton = button;
    public void SetBackground(Image image) => _background = image;

    public void ShowScreen(TrainPathScreen screenType)
    {
        switch (screenType)
        {
            case TrainPathScreen.Start:
                if (_startScreen != null) _startScreen.SetActive(true);
                if (_gameScreen != null) _gameScreen.SetActive(false);
                if (_endScreen != null) _endScreen.SetActive(false);
                break;
            case TrainPathScreen.Game:
                if (_startScreen != null) _startScreen.SetActive(false);
                if (_gameScreen != null) _gameScreen.SetActive(true);
                if (_endScreen != null) _endScreen.SetActive(false);
                break;
            case TrainPathScreen.End:
                if (_startScreen != null) _startScreen.SetActive(false);
                if (_gameScreen != null) _gameScreen.SetActive(false);
                if (_endScreen != null) _endScreen.SetActive(true);
                break;
        }
    }

    public void SetTime(float time)
    {
        if (_timeText != null)
            _timeText.text = $"Время: {time:F1}с";
    }

    public void SetPathInfo(int stationsVisited, int totalStations)
    {
        if (_pathInfoText != null)
            _pathInfoText.text = $"Станций: {stationsVisited}/{totalStations}";
    }

    public void SetResult(float playerTime, float optimalTime, bool isOptimal, int score)
    {
        if (_resultText != null)
            _resultText.text = isOptimal ? "Оптимальный путь!" : "Хорошая попытка!";
        
        if (_playerTimeText != null)
            _playerTimeText.text = $"Ваше время: {playerTime:F1}с";
        
        if (_optimalTimeText != null)
            _optimalTimeText.text = $"Оптимальное время: {optimalTime:F1}с";
        
        if (_scoreText != null)
            _scoreText.text = $"Очки: {score}";
    }

    public void SetSkillInfo(string skillName, int value)
    {
        if (_skillText != null)
            _skillText.text = $"{skillName}: {value}";
    }
}
