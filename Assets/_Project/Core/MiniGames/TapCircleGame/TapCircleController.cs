using System.Collections;
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

    // ── refs ─────────────────────────────────────────────────────────────────
    private TapCircleGameEvent _gameEvent;
    private UserDataService    _userDataService;

    // UI containers
    private RectTransform _gameAreaRect;
    private GameObject    _startScreen;
    private GameObject    _gameScreen;
    private GameObject    _endScreen;

    // HUD
    private Image             _timerFill;
    private TextMeshProUGUI   _scoreText;
    private TextMeshProUGUI   _comboText;
    private TextMeshProUGUI   _timerText;

    // End-screen
    private TextMeshProUGUI   _endScoreText;
    private TextMeshProUGUI   _endStatsText;

    // ── state ─────────────────────────────────────────────────────────────────
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

    private readonly List<TapCircle> _activeCircles = new();

    // ── init ─────────────────────────────────────────────────────────────────
    public void Initialize(TapCircleGameEvent gameEvent, UserDataService userDataService)
    {
        _gameEvent       = gameEvent;
        _userDataService = userDataService;
        BuildUI();
        ShowScreen(Screen.Start);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI CONSTRUCTION
    // ─────────────────────────────────────────────────────────────────────────
    private void BuildUI()
    {
        // ── background ───────────────────────────────────────────────────────
        var bg = MakeFullPanel(transform, "BG", new Color(0.05f, 0.05f, 0.13f));

        // ── start screen ─────────────────────────────────────────────────────
        _startScreen = MakeFullPanel(transform, "StartScreen", new Color(0f, 0f, 0f, 0f)).gameObject;
        BuildStartScreen(_startScreen.transform);

        // ── game screen ──────────────────────────────────────────────────────
        _gameScreen = MakeFullPanel(transform, "GameScreen", new Color(0f, 0f, 0f, 0f)).gameObject;
        BuildGameScreen(_gameScreen.transform);

        // ── end screen ───────────────────────────────────────────────────────
        _endScreen = MakeFullPanel(transform, "EndScreen", new Color(0f, 0f, 0f, 0.75f)).gameObject;
        BuildEndScreen(_endScreen.transform);
    }

    private void BuildStartScreen(Transform parent)
    {
        // card
        var card = MakePanel(parent, "Card",
            new Color(0.10f, 0.10f, 0.22f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        card.sizeDelta = new Vector2(300f, 390f);

        // title
        var title = MakeText(card, "Title", "ТАП-РИТМ", 36, FontStyles.Bold,
            new Color(0.00f, 0.90f, 1.00f), new Vector2(0f, 130f), new Vector2(280f, 60f));

        // icon circles decorative row
        var iconRow = MakePanel(card, "IconRow", Color.clear, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        iconRow.sizeDelta = new Vector2(280f, 60f);
        iconRow.anchoredPosition = new Vector2(0f, 65f);
        float[] xs = { -90f, 0f, 90f };
        Color[] previewColors = { CircleColors[0], CircleColors[1], CircleColors[2] };
        for (int i = 0; i < 3; i++)
        {
            var dot = MakePanel(iconRow, $"Dot{i}", previewColors[i],
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            dot.sizeDelta = new Vector2(34f, 34f);
            dot.anchoredPosition = new Vector2(xs[i], 0f);
            var dotImg = dot.GetComponent<Image>();
            TapCircle.EnsureSprites();
        }

        // instructions
        MakeText(card, "Desc1", "Нажимай на кружки до того,", 15, FontStyles.Normal,
            new Color(0.85f, 0.85f, 0.85f), new Vector2(0f, 5f), new Vector2(260f, 28f));
        MakeText(card, "Desc2", "как сожмётся кольцо!", 15, FontStyles.Normal,
            new Color(0.85f, 0.85f, 0.85f), new Vector2(0f, -22f), new Vector2(260f, 28f));

        // score legend
        MakeText(card, "Leg1", "⬤  Рано  +300", 13, FontStyles.Normal,
            new Color(0.20f, 1.00f, 0.40f), new Vector2(0f, -65f), new Vector2(240f, 24f));
        MakeText(card, "Leg2", "⬤  Хорошо  +150", 13, FontStyles.Normal,
            new Color(1.00f, 0.90f, 0.20f), new Vector2(0f, -90f), new Vector2(240f, 24f));
        MakeText(card, "Leg3", "⬤  Поздно  +50", 13, FontStyles.Normal,
            new Color(1.00f, 0.50f, 0.20f), new Vector2(0f, -115f), new Vector2(240f, 24f));

        MakeText(card, "Dur", "⏱  30 секунд", 14, FontStyles.Normal,
            new Color(0.70f, 0.70f, 0.70f), new Vector2(0f, -148f), new Vector2(240f, 26f));

        // start button
        var startBtn = MakeButton(card, "StartBtn", "НАЧАТЬ",
            new Color(0.00f, 0.75f, 1.00f), Color.white,
            new Vector2(0f, -180f), new Vector2(200f, 50f));
        startBtn.onClick.AddListener(StartGame);

        // close button
        var closeBtn = MakeButton(card, "CloseBtn", "✕",
            new Color(0.30f, 0.10f, 0.10f), new Color(1f, 0.5f, 0.5f),
            new Vector2(120f, 175f), new Vector2(40f, 40f));
        closeBtn.onClick.AddListener(() => _gameEvent?.CloseGame());
    }

    private void BuildGameScreen(Transform parent)
    {
        // ── HUD bar ──────────────────────────────────────────────────────────
        var hud = MakePanel(parent, "HUD", new Color(0.07f, 0.07f, 0.18f),
            new Vector2(0f, 1f), new Vector2(1f, 1f));
        hud.anchoredPosition = Vector2.zero;
        hud.sizeDelta = new Vector2(0f, 72f);

        _scoreText = MakeText(hud, "Score", "0", 26, FontStyles.Bold,
            Color.white, new Vector2(-10f, -18f), new Vector2(160f, 40f));
        _scoreText.alignment = TextAlignmentOptions.Right;

        _comboText = MakeText(hud, "Combo", "", 18, FontStyles.Bold,
            new Color(1f, 0.85f, 0f), new Vector2(10f, -18f), new Vector2(160f, 40f));
        _comboText.alignment = TextAlignmentOptions.Left;

        _timerText = MakeText(hud, "TimerNum", "30", 20, FontStyles.Bold,
            Color.white, new Vector2(0f, -15f), new Vector2(80f, 36f));
        _timerText.alignment = TextAlignmentOptions.Center;

        // timer bar track
        var timerTrack = MakePanel(hud, "TimerTrack", new Color(0.15f, 0.15f, 0.30f),
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
        _timerFill.color = new Color(0.00f, 0.89f, 1.00f);

        // ── game area (below HUD) ─────────────────────────────────────────────
        var gameArea = MakePanel(parent, "GameArea", Color.clear,
            new Vector2(0f, 0f), new Vector2(1f, 1f));
        gameArea.offsetMin = new Vector2(0f, 0f);
        gameArea.offsetMax = new Vector2(0f, -72f);
        _gameAreaRect = gameArea;
    }

    private void BuildEndScreen(Transform parent)
    {
        var card = MakePanel(parent, "Card",
            new Color(0.08f, 0.08f, 0.20f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        card.sizeDelta = new Vector2(300f, 380f);

        MakeText(card, "Title", "ИГРА ОКОНЧЕНА", 24, FontStyles.Bold,
            new Color(0.00f, 0.90f, 1.00f), new Vector2(0f, 150f), new Vector2(280f, 40f));

        _endScoreText = MakeText(card, "Score", "0", 72, FontStyles.Bold,
            Color.white, new Vector2(0f, 60f), new Vector2(280f, 100f));
        _endScoreText.alignment = TextAlignmentOptions.Center;

        MakeText(card, "ScoreLbl", "очков", 16, FontStyles.Normal,
            new Color(0.7f, 0.7f, 0.7f), new Vector2(0f, 10f), new Vector2(280f, 28f));

        _endStatsText = MakeText(card, "Stats", "", 15, FontStyles.Normal,
            new Color(0.85f, 0.85f, 0.85f), new Vector2(0f, -55f), new Vector2(260f, 90f));
        _endStatsText.alignment = TextAlignmentOptions.Center;

        var finishBtn = MakeButton(card, "FinishBtn", "ЗАВЕРШИТЬ",
            new Color(0.00f, 0.75f, 1.00f), Color.white,
            new Vector2(0f, -155f), new Vector2(210f, 50f));
        finishBtn.onClick.AddListener(FinishGame);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GAME LOGIC
    // ─────────────────────────────────────────────────────────────────────────
    private void StartGame()
    {
        _timeRemaining = GameDuration;
        _rawScore = _hits = _misses = _combo = _maxCombo = _circleNum = 0;
        _nextSpawnTime = 0f;
        _activeCircles.Clear();
        _isGameActive = true;

        UpdateHUD();
        ShowScreen(Screen.Game);
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
            ? Color.Lerp(Color.yellow, new Color(0f, 0.9f, 1f), (frac - 0.5f) * 2f)
            : Color.Lerp(new Color(1f, 0.2f, 0.2f), Color.yellow, frac * 2f);

        // use fillAmount to shrink the fill (need proper setup)
        if (_timerFill.type != Image.Type.Filled)
        {
            _timerFill.type = Image.Type.Filled;
            _timerFill.fillMethod = Image.FillMethod.Horizontal;
            _timerFill.fillOrigin = 0;
        }

        _timerText.text = Mathf.CeilToInt(_timeRemaining).ToString();
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

        // Чем ближе кольцо к кругу (меньше frac), тем больше очков
        if (frac < 0.30f)
        {
            baseScore = ScorePerfect; label = "PERFECT!"; popupColor = new Color(0.2f, 1f, 0.5f);
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

        SpawnScorePopup(circle, $"{label}\n+{earned}", popupColor);
    }

    private void OnCircleExpired(TapCircle circle)
    {
        _activeCircles.Remove(circle);
        _misses++;
        _combo = 0;

        SpawnScorePopup(circle, "MISS", new Color(1f, 0.3f, 0.3f));
    }

    private void SpawnScorePopup(TapCircle circle, string text, Color color)
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
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var cg = go.AddComponent<CanvasGroup>();
        rect.DOAnchorPosY(pos.y + 70f, 0.75f).SetUpdate(true);
        cg.DOFade(0f, 0.75f).SetDelay(0.25f).SetUpdate(true)
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

        _endScoreText.text = "0";
        _endStatsText.text =
            $"Попаданий: {_hits}   Промахов: {_misses}\n" +
            $"Точность: {accuracy:F0}%\n" +
            $"Макс. комбо: x{_maxCombo}\n" +
            $"Сырые очки: {_rawScore}";

        ShowScreen(Screen.End);

        // Animate score count-up from 0
        int displayScore = 0;
        DOTween.To(() => displayScore, v =>
        {
            displayScore = v;
            _endScoreText.text = v.ToString();
        }, _finalScore, 1.1f).SetEase(Ease.OutQuad).SetUpdate(true);
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
        _startScreen.SetActive(screen == Screen.Start);
        _gameScreen.SetActive(screen  == Screen.Game);
        _endScreen.SetActive(screen   == Screen.End);
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
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

    private static Button MakeButton(Transform parent, string name, string label,
        Color bgColor, Color textColor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cols = btn.colors;
        cols.highlightedColor = bgColor * 1.2f;
        cols.pressedColor = bgColor * 0.7f;
        btn.colors = cols;

        var lblGo = new GameObject("Label");
        lblGo.transform.SetParent(go.transform, false);
        var lblRect = lblGo.AddComponent<RectTransform>();
        lblRect.anchorMin = Vector2.zero;
        lblRect.anchorMax = Vector2.one;
        lblRect.offsetMin = Vector2.zero;
        lblRect.offsetMax = Vector2.zero;
        var tmp = lblGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;

        return btn;
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
    }
}
