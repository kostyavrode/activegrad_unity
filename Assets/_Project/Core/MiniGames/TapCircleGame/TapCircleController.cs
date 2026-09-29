using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TapCircleController : MonoBehaviour
{
    // ── config ───────────────────────────────────────────────────────────────
    private const float GameDuration        = 30f;
    private const float LifetimeStart       = 2.5f;   // circle lifetime at t=0
    private const float LifetimeEnd         = 1.4f;   // circle lifetime at t=30
    private const float SpawnIntervalStart  = 1.15f;
    private const float SpawnIntervalEnd    = 0.45f;
    private const int   MaxCircles         = 4;
    private const int   ScorePerfect        = 300;     // ring > 55%
    private const int   ScoreGreat          = 150;     // ring > 25%
    private const int   ScoreGood           = 50;      // ring > 0%
    private const float MinCircleDistance  = 130f;    // px between circle centers

    private static readonly Color[] CircleColors =
    {
        new Color(1.00f, 0.18f, 0.47f),  // hot pink
        new Color(0.00f, 0.89f, 1.00f),  // cyan
        new Color(0.40f, 1.00f, 0.28f),  // lime
        new Color(1.00f, 0.88f, 0.00f),  // yellow
        new Color(1.00f, 0.45f, 0.00f),  // orange
        new Color(0.72f, 0.22f, 1.00f),  // purple
    };

    // Необязательный клип попадания: если задан — играется с повышением тона по комбо.
    [SerializeField] private AudioClip _hitClip;

    // ── refs ─────────────────────────────────────────────────────────────────
    private TapCircleGameEvent _gameEvent;
    private UserDataService    _userDataService;

    // UI containers
    private RectTransform _gameAreaRect;
    private RectTransform _gameScreenRect;
    private GameObject    _startScreen;
    private GameObject    _gameScreen;

    // HUD
    private Image             _timerFill;
    private TextMeshProUGUI   _scoreText;
    private TextMeshProUGUI   _comboText;
    private TextMeshProUGUI   _timerText;

    // End / countdown
    private MiniGameResultPanel _resultPanel;
    private MiniGameCountdown   _countdown;

    // ── state ─────────────────────────────────────────────────────────────────
    // NB: имя Screen перекрывает UnityEngine.Screen внутри этого класса.
    private enum Screen { Start, Game, End }

    private float _timeRemaining;
    private int   _rawScore;
    private int   _hits;
    private int   _misses;
    private int   _combo;
    private int   _maxCombo;
    private int   _circleNum;
    private float _nextSpawnTime;
    private bool  _isGameActive;
    private int   _finalScore;
    private int   _lastTimerSecond;

    private readonly List<TapCircle> _activeCircles = new();

    // ── init ─────────────────────────────────────────────────────────────────
    public void Initialize(TapCircleGameEvent gameEvent, UserDataService userDataService)
    {
        _gameEvent       = gameEvent;
        _userDataService = userDataService;
        BuildUI();
        _gameScreen.SetActive(false);
        ShowScreen(Screen.Start);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI CONSTRUCTION
    // ─────────────────────────────────────────────────────────────────────────
    private void BuildUI()
    {
        // ── background ───────────────────────────────────────────────────────
        MakeFullPanel(transform, "BG", MiniGameTheme.Background);

        // ── start screen ─────────────────────────────────────────────────────
        _startScreen = MakeFullPanel(transform, "StartScreen", new Color(0f, 0f, 0f, 0f)).gameObject;
        BuildStartScreen(_startScreen.transform);

        // ── game screen ──────────────────────────────────────────────────────
        _gameScreenRect = MakeFullPanel(transform, "GameScreen", new Color(0f, 0f, 0f, 0f));
        _gameScreen = _gameScreenRect.gameObject;
        BuildGameScreen(_gameScreen.transform);
    }

    private void BuildStartScreen(Transform parent)
    {
        // card
        var card = MiniGameTheme.CreateCard(parent, "Card", new Vector2(300f, 390f), out _, MiniGameTheme.Card, 26f);

        // title
        MakeText(card, "Title", "ТАП-РИТМ", 36, FontStyles.Bold,
            MiniGameTheme.Accent, new Vector2(0f, 130f), new Vector2(280f, 60f));

        // icon circles decorative row
        var iconRowGo = new GameObject("IconRow");
        iconRowGo.transform.SetParent(card, false);
        var iconRow = iconRowGo.AddComponent<RectTransform>();
        iconRow.sizeDelta = new Vector2(280f, 60f);
        iconRow.anchoredPosition = new Vector2(0f, 65f);
        float[] xs = { -90f, 0f, 90f };
        Color[] previewColors = { CircleColors[0], CircleColors[1], CircleColors[2] };
        for (int i = 0; i < 3; i++)
        {
            var dotImg = MiniGameTheme.MakeImage(iconRow, $"Dot{i}", previewColors[i]);
            dotImg.sprite = MiniGameTheme.CircleSprite;
            dotImg.raycastTarget = false;
            var dot = dotImg.rectTransform;
            dot.anchorMin = dot.anchorMax = new Vector2(0.5f, 0.5f);
            dot.sizeDelta = new Vector2(34f, 34f);
            dot.anchoredPosition = new Vector2(xs[i], 0f);

            // мягкое «дыхание» точек со сдвигом по фазе
            dot.DOScale(1.18f, 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
               .SetDelay(i * 0.23f).SetUpdate(true).SetLink(dot.gameObject);
        }

        // instructions
        MakeText(card, "Desc1", "Нажимай на кружки до того,", 15, FontStyles.Normal,
            MiniGameTheme.TextSecondary, new Vector2(0f, 5f), new Vector2(260f, 28f));
        MakeText(card, "Desc2", "как сожмётся кольцо!", 15, FontStyles.Normal,
            MiniGameTheme.TextSecondary, new Vector2(0f, -22f), new Vector2(260f, 28f));

        // score legend
        MakeText(card, "Leg1", "⬤  Рано  +300", 13, FontStyles.Normal,
            MiniGameTheme.Success, new Vector2(0f, -65f), new Vector2(240f, 24f));
        MakeText(card, "Leg2", "⬤  Хорошо  +150", 13, FontStyles.Normal,
            MiniGameTheme.Warning, new Vector2(0f, -90f), new Vector2(240f, 24f));
        MakeText(card, "Leg3", "⬤  Поздно  +50", 13, FontStyles.Normal,
            new Color(1.00f, 0.55f, 0.25f), new Vector2(0f, -115f), new Vector2(240f, 24f));

        MakeText(card, "Dur", "⏱  30 секунд", 14, FontStyles.Normal,
            new Color(0.62f, 0.66f, 0.76f), new Vector2(0f, -148f), new Vector2(240f, 26f));

        // start button
        var startBtn = MiniGameTheme.CreateButton(card, "StartBtn", "НАЧАТЬ", new Vector2(200f, 50f),
            MiniGameTheme.Accent, MiniGameTheme.TextDark);
        ((RectTransform)startBtn.transform).anchoredPosition = new Vector2(0f, -180f);
        startBtn.onClick.AddListener(StartGame);

        // close button
        var closeBtn = MiniGameTheme.CreateButton(card, "CloseBtn", "✕", new Vector2(40f, 40f),
            MiniGameTheme.Danger, Color.white, 20f, FeedbackType.Close);
        ((RectTransform)closeBtn.transform).anchoredPosition = new Vector2(120f, 175f);
        closeBtn.onClick.AddListener(() => _gameEvent?.CloseGame());
    }

    private void BuildGameScreen(Transform parent)
    {
        // ── HUD bar ──────────────────────────────────────────────────────────
        var hud = MakePanel(parent, "HUD", MiniGameTheme.Card,
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        hud.anchoredPosition = Vector2.zero;
        hud.sizeDelta = new Vector2(0f, 72f);

        _scoreText = MakeText(hud, "Score", "0", 26, FontStyles.Bold,
            Color.white, new Vector2(-10f, -18f), new Vector2(160f, 40f));
        _scoreText.alignment = TextAlignmentOptions.Right;

        _comboText = MakeText(hud, "Combo", "", 18, FontStyles.Bold,
            MiniGameTheme.Warning, new Vector2(10f, -18f), new Vector2(160f, 40f));
        _comboText.alignment = TextAlignmentOptions.Left;

        _timerText = MakeText(hud, "TimerNum", "30", 20, FontStyles.Bold,
            Color.white, new Vector2(0f, -15f), new Vector2(80f, 36f));
        _timerText.alignment = TextAlignmentOptions.Center;

        // timer bar track
        var timerTrack = MakePanel(hud, "TimerTrack", MiniGameTheme.CardLight,
            new Vector2(0f, 0f), new Vector2(1f, 0f));
        timerTrack.sizeDelta = new Vector2(0f, 7f);
        timerTrack.anchoredPosition = new Vector2(0f, 0f);

        // timer bar fill (left-anchored so it shrinks from right)
        var timerFillGo = new GameObject("TimerFill");
        timerFillGo.transform.SetParent(timerTrack, false);
        var fillRect = timerFillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        _timerFill = timerFillGo.AddComponent<Image>();
        _timerFill.color = MiniGameTheme.Accent;
        _timerFill.type = Image.Type.Filled;
        _timerFill.fillMethod = Image.FillMethod.Horizontal;
        _timerFill.fillOrigin = 0;

        // ── game area (below HUD) ─────────────────────────────────────────────
        var gameArea = MakePanel(parent, "GameArea", Color.clear,
            new Vector2(0f, 0f), new Vector2(1f, 1f));
        gameArea.offsetMin = new Vector2(0f, 0f);
        gameArea.offsetMax = new Vector2(0f, -72f);
        _gameAreaRect = gameArea;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GAME LOGIC
    // ─────────────────────────────────────────────────────────────────────────
    private void StartGame()
    {
        _timeRemaining = GameDuration;
        _rawScore = _hits = _misses = _combo = _maxCombo = _circleNum = 0;
        _nextSpawnTime = 0f;
        _lastTimerSecond = -1;
        _activeCircles.Clear();
        _isGameActive = false;

        UpdateHUD();
        ShowScreen(Screen.Game);

        if (_countdown != null) _countdown.Cancel();
        _countdown = MiniGameCountdown.Play(_gameScreenRect, () =>
        {
            _countdown = null;
            _nextSpawnTime = Time.time;
            _isGameActive = true;
        });
    }

    private void Update()
    {
        if (!_isGameActive) return;

        _timeRemaining -= Time.deltaTime;

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            _isGameActive = false;
            EndGame();
            return;
        }

        UpdateHUD();

        // spawn
        if (Time.time >= _nextSpawnTime && _activeCircles.Count < MaxCircles)
        {
            SpawnCircle();
            float t = 1f - (_timeRemaining / GameDuration);
            float interval = Mathf.Lerp(SpawnIntervalStart, SpawnIntervalEnd, t);
            _nextSpawnTime = Time.time + interval;
        }
    }

    private void UpdateHUD()
    {
        float frac = _timeRemaining / GameDuration;
        _timerFill.fillAmount = frac;

        // color: cyan → yellow → red
        _timerFill.color = frac > 0.5f
            ? Color.Lerp(MiniGameTheme.Warning, MiniGameTheme.Accent, (frac - 0.5f) * 2f)
            : Color.Lerp(MiniGameTheme.Danger, MiniGameTheme.Warning, frac * 2f);

        int sec = Mathf.CeilToInt(_timeRemaining);
        _timerText.text = sec.ToString();
        if (_isGameActive && sec != _lastTimerSecond)
        {
            if (_lastTimerSecond > 0 && sec <= 5 && sec > 0)
            {
                _timerText.color = MiniGameTheme.Danger;
                MiniGameJuice.Punch(_timerText.transform, 0.3f, 0.25f);
                MiniGameJuice.Feedback(FeedbackType.Tap);
            }
            _lastTimerSecond = sec;
        }
        if (sec > 5) _timerText.color = Color.white;

        _scoreText.text = _rawScore.ToString();
        _comboText.text = _combo >= 3 ? $"x{_combo} COMBO" : "";
    }

    private void SpawnCircle()
    {
        float t = 1f - (_timeRemaining / GameDuration);
        float lifetime = Mathf.Lerp(LifetimeStart, LifetimeEnd, t);
        Color color = CircleColors[_circleNum % CircleColors.Length];

        Vector2 pos = FindSpawnPosition();
        _circleNum++;

        var circle = TapCircle.Create(_gameAreaRect, _circleNum, lifetime, color);
        circle.GetComponent<RectTransform>().anchoredPosition = pos;

        circle.OnTapped  += OnCircleTapped;
        circle.OnExpired += OnCircleExpired;

        _activeCircles.Add(circle);
    }

    private Vector2 FindSpawnPosition()
    {
        // Compute usable bounds (run-time rect may not be ready first frame, use safe fallback)
        float hw = _gameAreaRect.rect.width  > 10f ? _gameAreaRect.rect.width  * 0.5f - 65f : 135f;
        float hh = _gameAreaRect.rect.height > 10f ? _gameAreaRect.rect.height * 0.5f - 65f : 200f;

        const int MaxAttempts = 20;
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidate = new Vector2(
                Random.Range(-hw, hw),
                Random.Range(-hh, hh));

            bool tooClose = false;
            foreach (var c in _activeCircles)
            {
                if (c == null) continue;
                var cr = c.GetComponent<RectTransform>();
                if (cr != null && Vector2.Distance(cr.anchoredPosition, candidate) < MinCircleDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose) return candidate;
        }

        return new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));
    }

    private void OnCircleTapped(TapCircle circle)
    {
        _activeCircles.Remove(circle);

        float frac = circle.RemainingFraction;
        int baseScore;
        string label;
        Color popupColor;
        bool perfect = false;

        // Чем ближе кольцо к кругу (меньше frac), тем больше очков
        if (frac < 0.30f)
        {
            baseScore = ScorePerfect; label = "PERFECT!"; popupColor = new Color(0.2f, 1f, 0.5f);
            perfect = true;
        }
        else if (frac < 0.65f)
        {
            baseScore = ScoreGreat;   label = "GREAT";    popupColor = new Color(1f, 0.9f, 0.2f);
        }
        else
        {
            baseScore = ScoreGood;    label = "GOOD";     popupColor = new Color(1f, 0.55f, 0.2f);
        }

        _combo++;
        _maxCombo = Mathf.Max(_maxCombo, _combo);
        float comboMult = Mathf.Min(1f + _combo * 0.08f, 2f);
        int earned = Mathf.RoundToInt(baseScore * comboMult);
        _rawScore += earned;
        _hits++;

        // ── juice ────────────────────────────────────────────────────────────
        var cr = circle.GetComponent<RectTransform>();
        if (cr != null)
            MiniGameJuice.Burst(_gameAreaRect, cr.anchoredPosition, circle.Color,
                perfect ? 16 : 10, perfect ? 140f : 100f, perfect ? 24f : 18f);

        if (_hitClip != null)
            MiniGameJuice.Sfx(_hitClip, 0.02f, MiniGameJuice.ComboPitch(_combo));

        if (perfect)
        {
            MiniGameJuice.Feedback(FeedbackType.Perfect);
            MiniGameJuice.Shake(_gameScreenRect, 7f, 0.18f);
        }
        else if (_hitClip == null)
        {
            MiniGameJuice.Feedback(FeedbackType.Hit);
        }

        _scoreText.text = _rawScore.ToString();
        MiniGameJuice.Punch(_scoreText.transform, 0.15f, 0.2f);
        if (_combo >= 3)
        {
            _comboText.text = $"x{_combo} COMBO";
            MiniGameJuice.Punch(_comboText.transform, 0.3f + Mathf.Min(_combo, 12) * 0.02f, 0.3f);
        }

        SpawnScorePopup(circle, $"{label}\n+{earned}", popupColor, perfect);
    }

    private void OnCircleExpired(TapCircle circle)
    {
        _activeCircles.Remove(circle);
        _misses++;
        bool hadCombo = _combo >= 3;
        _combo = 0;

        MiniGameJuice.Flash(_gameScreenRect, MiniGameTheme.Danger, 0.18f, 0.3f);
        MiniGameJuice.Feedback(FeedbackType.Miss);
        if (hadCombo) MiniGameJuice.Shake(_gameScreenRect, 5f, 0.2f);

        SpawnScorePopup(circle, "MISS", new Color(1f, 0.3f, 0.3f), false);
    }

    private void SpawnScorePopup(TapCircle circle, string text, Color color, bool big)
    {
        if (circle == null) return;
        var cr = circle.GetComponent<RectTransform>();
        if (cr == null) return;
        Vector2 pos = cr.anchoredPosition + new Vector2(0f, 55f);

        var go = new GameObject("ScorePopup");
        go.transform.SetParent(_gameAreaRect, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180f, 56f);
        rect.anchoredPosition = pos;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = big ? 26 : 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var cg = go.AddComponent<CanvasGroup>();
        rect.localScale = Vector3.one * 0.5f;
        rect.DOScale(1f, 0.25f).SetEase(Ease.OutBack, 2.5f).SetUpdate(true).SetLink(go);
        rect.DOAnchorPosY(pos.y + 70f, 0.75f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(go);
        cg.DOFade(0f, 0.75f).SetDelay(0.25f).SetUpdate(true).SetLink(go)
            .OnComplete(() => { if (go != null) Destroy(go); });
    }

    private void EndGame()
    {
        // Clean up remaining circles
        foreach (var c in _activeCircles)
            if (c != null) Destroy(c.gameObject);
        _activeCircles.Clear();

        // Нормализация: ~25 GREAT-попаданий с 1.3x комбо = 4875 raw ≈ 100 очков
        // Используем делитель 50, чтобы хорошая игра давала 80–100, плохая — 10–30
        _finalScore = Mathf.Clamp(Mathf.RoundToInt(_rawScore / 50f), 0, 100);

        float accuracy = (_hits + _misses) > 0
            ? (_hits / (float)(_hits + _misses)) * 100f : 0f;

        ShowScreen(Screen.End);

        if (_resultPanel != null) Destroy(_resultPanel.gameObject);
        _resultPanel = MiniGameResultPanel.Show((RectTransform)transform, new MiniGameResultPanel.Options
        {
            GameId         = "tapcircle",
            Title          = "ИГРА ОКОНЧЕНА",
            TitleColor     = MiniGameTheme.Accent,
            Score          = _finalScore,
            MaxScore       = 100,
            StarThresholds = new[] { 0.3f, 0.6f, 0.85f },
            ScoreCaption   = "очков",
            StatLines      = new[]
            {
                $"Попаданий: {_hits}   Промахов: {_misses}",
                $"Точность: {accuracy:F0}%",
                $"Макс. комбо: x{_maxCombo}",
            },
            PrimaryLabel   = "ЗАВЕРШИТЬ",
            OnPrimary      = FinishGame,
        });
    }

    private void FinishGame()
    {
        _gameEvent?.OnGameEnded(_finalScore);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCREEN MANAGEMENT
    // ─────────────────────────────────────────────────────────────────────────
    private void ShowScreen(Screen screen)
    {
        SetScreenVisible(_startScreen, screen == Screen.Start);
        // На экране результата игровое поле остаётся под затемнённой панелью
        SetScreenVisible(_gameScreen, screen == Screen.Game || screen == Screen.End);
    }

    private static void SetScreenVisible(GameObject go, bool visible)
    {
        if (go == null) return;
        if (visible)
        {
            var cg = go.GetComponent<CanvasGroup>();
            bool fadingOut = cg != null && !cg.blocksRaycasts;
            if (!go.activeSelf || fadingOut) MiniGameJuice.FadeIn(go, 0.25f);
        }
        else
        {
            MiniGameJuice.FadeOut(go, 0.2f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI HELPERS
    // ─────────────────────────────────────────────────────────────────────────
    private static RectTransform MakeFullPanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = color.a > 0f;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.AddComponent<CanvasGroup>();
        return rect;
    }

    private static RectTransform MakePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name, string text,
        float fontSize, FontStyles style, Color color, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
        if (_countdown != null) _countdown.Cancel();
    }
}
