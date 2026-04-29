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

    public void SetCountdown(float seconds)
    {
        if (_timeText == null) return;
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        _timeText.text = $"⏱ {m}:{s:00}";
        _timeText.color = seconds < 15f ? new Color(1f, 0.25f, 0.25f) :
                          seconds < 30f ? new Color(1f, 0.75f, 0.10f) : Color.white;
    }

    public void SetCargoStatus(int collected, int total)
    {
        if (_pathInfoText == null) return;
        _pathInfoText.text = collected >= total && total > 0
            ? $"📦 {collected}/{total} — Теперь к финишу!"
            : $"📦 {collected}/{total} — Собери все грузы";
    }

    public void SetResult(int cargoCollected, int totalCargo, float timeRemaining, bool isTimeout, int score)
    {
        if (_resultText != null)
            _resultText.text = isTimeout ? "Время вышло!" : "Все грузы доставлены!";

        if (_playerTimeText != null)
            _playerTimeText.text = $"Собрано грузов: {cargoCollected}/{totalCargo}";

        if (_optimalTimeText != null)
            _optimalTimeText.text = isTimeout ? "Время истекло" : $"Осталось: {timeRemaining:F0}с";

        if (_scoreText != null)
            _scoreText.text = $"Очки: {score}";
    }

    public void SetSkillInfo(string skillName, int value)
    {
        if (_skillText != null)
            _skillText.text = $"{skillName}: {value}";
    }
}
