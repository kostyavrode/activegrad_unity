using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ActiveGrad.MiniGames;

public enum FlappyBirdScreen { Start, Game, End }

public class FlappyBirdUI : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject _startScreen;
    [SerializeField] private GameObject _gameScreen;
    [SerializeField] private GameObject _endScreen;

    [Header("Start Screen")]
    [SerializeField] private Button   _startButton;
    [SerializeField] private Button   _closeButton;
    [SerializeField] private TMP_Text _skillText;

    [Header("Game Screen")]
    [SerializeField] private RectTransform _pipesContainer;
    [SerializeField] private RectTransform _bird;
    [SerializeField] private TMP_Text      _scoreText;
    [SerializeField] private TMP_Text      _countdownText;

    [Header("End Screen")]
    [SerializeField] private TMP_Text      _resultText;
    [SerializeField] private TMP_Text      _finalScoreText;
    [SerializeField] private TMP_Text      _bestScoreText;
    [SerializeField] private TMP_Text      _medalText;
    [SerializeField] private RectTransform _bonusSliderTrack;
    [SerializeField] private RectTransform _bonusSliderIndicator;
    [SerializeField] private TMP_Text      _bonusLeftLabel;
    [SerializeField] private TMP_Text      _bonusRightLabel;
    [SerializeField] private GameObject    _endScreenButtonsContainer;
    [SerializeField] private BonusSliderComponent _bonusSliderComponent;
    [SerializeField] private Button        _restartButton;
    [SerializeField] private Button        _finishButton;

    [Header("Overlays")]
    [SerializeField] private Image _background;
    [SerializeField] private Image _flashOverlay;

    // ── Properties ────────────────────────────────────────────────────
    public Button   StartButton  => _startButton;
    public Button   CloseButton  => _closeButton;
    public TMP_Text SkillText    => _skillText;

    public RectTransform PipesContainer => _pipesContainer;
    public RectTransform Bird           => _bird;
    public TMP_Text      ScoreText      => _scoreText;
    public TMP_Text      CountdownText  => _countdownText;

    public TMP_Text      ResultText               => _resultText;
    public TMP_Text      FinalScoreText            => _finalScoreText;
    public RectTransform BonusSliderTrack         => _bonusSliderTrack;
    public RectTransform BonusSliderIndicator     => _bonusSliderIndicator;
    public TMP_Text      BonusLeftLabel           => _bonusLeftLabel;
    public TMP_Text      BonusRightLabel          => _bonusRightLabel;
    public GameObject    EndScreenButtonsContainer => _endScreenButtonsContainer;
    public BonusSliderComponent BonusSliderComponent => _bonusSliderComponent;
    public Button        RestartButton => _restartButton;
    public Button        FinishButton  => _finishButton;

    public Image Background   => _background;
    public Image FlashOverlay => _flashOverlay;

    // ── Screen control ────────────────────────────────────────────────
    public void ShowScreen(FlappyBirdScreen screen)
    {
        if (_startScreen) _startScreen.SetActive(screen == FlappyBirdScreen.Start);
        if (_gameScreen)  _gameScreen.SetActive(screen == FlappyBirdScreen.Game);
        if (_endScreen)   _endScreen.SetActive(screen == FlappyBirdScreen.End);
    }

    // ── Setters ───────────────────────────────────────────────────────
    public void SetScore(int score)
    {
        if (_scoreText) _scoreText.text = $"🐦 {score}";
    }

    public void SetResult(int pipesPassed, int score)
    {
        if (_resultText)     _resultText.text     = "Игра окончена!";
        if (_finalScoreText) _finalScoreText.text = $"Пролетел: {pipesPassed} труб\nОчки: {score}";
    }

    public void SetSkillInfo(string skillName, int value)
    {
        if (_skillText) _skillText.text = $"{skillName}: {value}";
    }

    public void SetCountdown(string text)
    {
        if (_countdownText)
        {
            _countdownText.text    = text;
            _countdownText.enabled = !string.IsNullOrEmpty(text);
        }
    }

    public void SetBestScore(int best)
    {
        if (_bestScoreText) _bestScoreText.text = best > 0 ? $"Рекорд: {best}" : "";
    }

    public void SetMedal(string medal)
    {
        if (_medalText) _medalText.text = medal;
    }
}
