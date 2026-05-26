using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActiveGrad.MiniGames;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TrainPath mini-game controller.
/// Builds its entire UI in code — no prefab required.
/// </summary>
public class TrainPathController : MonoBehaviour
{
    // ── Wiring ────────────────────────────────────────────────────────────────
    private TrainPathGameEvent _gameEvent;
    private int _intelligence = 1;

    // ── Map ───────────────────────────────────────────────────────────────────
    private List<Station>             _stations = new List<Station>();
    private List<TrainPathConnection> _paths    = new List<TrainPathConnection>();
    private Station    _startStation, _endStation, _currentStation;
    private RectTransform _mapContainer;
    private TrainMapGenerator _mapGenerator;

    // Fallback размеры карты (используются только если Canvas.ForceUpdateCanvases не успел отработать)
    private const float MapW = 278f;
    private const float MapH = 398f;

    // ── Train ─────────────────────────────────────────────────────────────────
    private RectTransform _trainRect;
    private Image         _trainImg;
    private Tween         _trainGlowTween;

    // ── Game state ────────────────────────────────────────────────────────────
    private bool  _gameStarted;
    private bool  _gameEnded;
    private bool  _isMoving;
    private int   _totalCargo;
    private int   _cargoCollected;
    private float _countdownTime;
    private float _remainingTime;
    private int   _rawScore;
    private int   _finalScore;

    // ── UI refs ───────────────────────────────────────────────────────────────
    private GameObject         _startScreen;
    private GameObject         _gameScreen;
    private GameObject         _endScreen;
    private TextMeshProUGUI    _timerTxt;
    private Image              _timerBarFill;
    private TextMeshProUGUI    _cargoTxt;
    private TextMeshProUGUI    _finalScoreTxt;
    private TextMeshProUGUI    _resultTitleTxt;
    private TextMeshProUGUI    _intelligenceTxt;
    private TextMeshProUGUI    _rewardTxt;
    private BonusSliderComponent _bonusSlider;
    private GameObject         _finishBtnGo;

    // ── Visual constants ──────────────────────────────────────────────────────
    private static readonly Color ColBg        = new Color(0.04f, 0.06f, 0.12f);
    private static readonly Color ColMapBg     = new Color(0.06f, 0.09f, 0.18f);
    private static readonly Color ColMapBorder = new Color(0.12f, 0.20f, 0.38f);
    private static readonly Color ColHeader    = new Color(0.05f, 0.08f, 0.16f, 0.96f);
    private static readonly Color ColTrain     = new Color(0.10f, 0.88f, 0.96f);
    private static readonly Color ColTrainGlow = new Color(0.10f, 0.88f, 0.96f, 0.22f);

    // ── Shared sprites ────────────────────────────────────────────────────────
    private static Sprite _whiteSquare;
    private static Sprite _circleSprite;

    // ══════════════════════════════════════════════════════════════════════════
    // PUBLIC INIT
    // ══════════════════════════════════════════════════════════════════════════

    public void Initialize(TrainPathGameEvent gameEvent, int intelligence)
    {
        _gameEvent     = gameEvent;
        _intelligence  = Mathf.Max(1, intelligence);
        _mapGenerator  = new TrainMapGenerator(null); // code-defaults only

        EnsureSprites();
        BuildUI();
        ShowStart();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UI BUILDING
    // ══════════════════════════════════════════════════════════════════════════

    private void BuildUI()
    {
        var root = GetComponent<RectTransform>();

        // Background
        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(root, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bgGo.AddComponent<Image>().color = ColBg;

        _startScreen = BuildStartScreen(root);
        _gameScreen  = BuildGameScreen(root);
        _endScreen   = BuildEndScreen(root);
    }

    // ── Start screen ──────────────────────────────────────────────────────────

    private GameObject BuildStartScreen(RectTransform root)
    {
        var screen = MakeOverlay(root, "StartScreen");
        var rt     = screen.GetComponent<RectTransform>();

        // Title
        var title = MakeText(rt, "Title", "ЖЕЛЕЗНАЯ\nДОРОГА", 46,
            new Vector2(0, 145), TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.color     = new Color(0.18f, 0.84f, 0.96f);

        // Subtitle
        var sub = MakeText(rt, "Sub",
            "Собери все грузы и отвези их на финальную станцию\nЧем быстрее — тем больше очков",
            17, new Vector2(0, 50), TextAlignmentOptions.Center);
        sub.color = new Color(0.60f, 0.72f, 0.85f);

        // Legend row
        var leg = MakeText(rt, "Legend",
            "🟢 Старт   🔴 Финиш   🟠 Груз   🟡 Доступна",
            14, new Vector2(0, -5), TextAlignmentOptions.Center);
        leg.color = new Color(0.50f, 0.60f, 0.70f);

        // Intelligence badge
        var badge = MakeText(rt, "Intelligence",
            $"Интеллект {_intelligence}  ·  влияет на ползунок",
            15, new Vector2(0, -48), TextAlignmentOptions.Center);
        badge.color = new Color(0.40f, 1f, 0.65f);

        // Start button
        var startBtn = MakeButton(rt, "НАЧАТЬ",
            new Color(0.10f, 0.60f, 0.32f), new Vector2(0, -112), new Vector2(220, 56));
        startBtn.onClick.AddListener(StartGame);

        // Close button
        var closeBtn = MakeButton(rt, "✕  Выйти",
            new Color(0.42f, 0.10f, 0.10f), new Vector2(0, -180), new Vector2(220, 44));
        closeBtn.onClick.AddListener(() => _gameEvent?.CloseGame());

        return screen;
    }

    // ── Game screen ───────────────────────────────────────────────────────────

    private GameObject BuildGameScreen(RectTransform root)
    {
        var screen = MakeOverlay(root, "GameScreen");
        screen.SetActive(false);

        var rt = screen.GetComponent<RectTransform>();

        // Header
        var hdrGo = new GameObject("Header");
        hdrGo.transform.SetParent(rt, false);
        var hdrRt = hdrGo.AddComponent<RectTransform>();
        hdrRt.anchorMin = new Vector2(0, 1); hdrRt.anchorMax = new Vector2(1, 1);
        hdrRt.sizeDelta        = new Vector2(0, 68);
        hdrRt.anchoredPosition = new Vector2(0, -34);
        hdrGo.AddComponent<Image>().color = ColHeader;

        // Timer
        _timerTxt = MakeText(hdrRt, "Timer", "2:00", 34, new Vector2(0, 6), TextAlignmentOptions.Center);
        _timerTxt.fontStyle = FontStyles.Bold;

        // Timer progress bar
        var barBg = new GameObject("BarBg");
        barBg.transform.SetParent(hdrRt, false);
        var barBgRt = barBg.AddComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0.05f, 0); barBgRt.anchorMax = new Vector2(0.95f, 0);
        barBgRt.sizeDelta        = new Vector2(0, 5);
        barBgRt.anchoredPosition = new Vector2(0, 9);
        barBg.AddComponent<Image>().color = new Color(0.12f, 0.18f, 0.28f);

        var barFillGo = new GameObject("BarFill");
        barFillGo.transform.SetParent(barBg.transform, false);
        var barFillRt = barFillGo.AddComponent<RectTransform>();
        barFillRt.anchorMin = barFillRt.anchorMax = Vector2.zero;
        barFillRt.pivot     = new Vector2(0, 0.5f);
        barFillRt.anchorMin = new Vector2(0, 0); barFillRt.anchorMax = new Vector2(0, 1);
        barFillRt.offsetMin = barFillRt.offsetMax = Vector2.zero;
        _timerBarFill = barFillGo.AddComponent<Image>();
        _timerBarFill.color = new Color(0.20f, 0.85f, 0.44f);
        _timerBarFill.type  = Image.Type.Filled;
        _timerBarFill.fillMethod = Image.FillMethod.Horizontal;
        _timerBarFill.fillAmount = 1f;

        // Cargo counter
        _cargoTxt = MakeText(hdrRt, "Cargo", "📦 0/0", 16, new Vector2(0, -20), TextAlignmentOptions.Center);
        _cargoTxt.color = new Color(0.95f, 0.80f, 0.28f);

        // Map border — растягивается на весь экран под хедером (68px)
        var borderGo = new GameObject("MapBorder");
        borderGo.transform.SetParent(rt, false);
        var borderRt = borderGo.AddComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(0, 0);
        borderRt.offsetMax = new Vector2(0, -68);
        borderGo.AddComponent<Image>().color = ColMapBorder;

        // Map panel — 3px inset от border
        var mapPanelGo = new GameObject("MapPanel");
        mapPanelGo.transform.SetParent(rt, false);
        var mapPanelRt = mapPanelGo.AddComponent<RectTransform>();
        mapPanelRt.anchorMin = Vector2.zero; mapPanelRt.anchorMax = Vector2.one;
        mapPanelRt.offsetMin = new Vector2(3, 3);
        mapPanelRt.offsetMax = new Vector2(-3, -71);
        mapPanelGo.AddComponent<Image>().color = ColMapBg;

        // Map container — 10px inset внутри панели
        var mcGo = new GameObject("MapContainer");
        mcGo.transform.SetParent(mapPanelGo.transform, false);
        _mapContainer = mcGo.AddComponent<RectTransform>();
        _mapContainer.anchorMin = Vector2.zero; _mapContainer.anchorMax = Vector2.one;
        _mapContainer.offsetMin = new Vector2(10, 10); _mapContainer.offsetMax = new Vector2(-10, -10);

        return screen;
    }

    // ── End screen ────────────────────────────────────────────────────────────

    private GameObject BuildEndScreen(RectTransform root)
    {
        var screen = MakeOverlay(root, "EndScreen", new Color(0f, 0f, 0f, 0.88f));
        screen.SetActive(false);
        var rt = screen.GetComponent<RectTransform>();

        // Title
        _resultTitleTxt = MakeText(rt, "Title", "ДОСТАВКА ЗАВЕРШЕНА!", 32,
            new Vector2(0, 200), TextAlignmentOptions.Center);
        _resultTitleTxt.fontStyle = FontStyles.Bold;

        // Score counter
        _finalScoreTxt = MakeText(rt, "Score", "0", 72, new Vector2(0, 115), TextAlignmentOptions.Center);
        _finalScoreTxt.color     = new Color(1f, 0.85f, 0.25f);
        _finalScoreTxt.fontStyle = FontStyles.Bold;

        MakeText(rt, "ScoreLabel", "очков из 100", 16, new Vector2(0, 62), TextAlignmentOptions.Center)
            .color = new Color(0.58f, 0.62f, 0.68f);

        // Intelligence hint
        _intelligenceTxt = MakeText(rt, "Intelligence", "", 15, new Vector2(0, 28), TextAlignmentOptions.Center);
        _intelligenceTxt.color = new Color(0.40f, 1f, 0.65f);

        // Slider labels
        var leftLbl = MakeText(rt, "SliderL", "Интеллект", 14,
            new Vector2(-105, -12), TextAlignmentOptions.Center);
        var rightLbl = MakeText(rt, "SliderR", "% задания", 14,
            new Vector2(105, -12), TextAlignmentOptions.Center);
        leftLbl.color  = rightLbl.color = new Color(0.55f, 0.60f, 0.68f);
        leftLbl.GetComponent<RectTransform>().sizeDelta  = new Vector2(130f, 24f);
        rightLbl.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 24f);

        // Slider track
        const float TW = 280f, TH = 28f;
        var trackGo = new GameObject("SliderTrack");
        trackGo.transform.SetParent(rt, false);
        var trackRt = trackGo.AddComponent<RectTransform>();
        trackRt.anchorMin = trackRt.anchorMax = new Vector2(0.5f, 0.5f);
        trackRt.sizeDelta        = new Vector2(TW, TH);
        trackRt.anchoredPosition = new Vector2(0, -50f);
        trackGo.AddComponent<Image>().color = new Color(0.10f, 0.13f, 0.18f);

        float hw = TW * 0.5f;
        (float from, float to, Color col)[] zones =
        {
            (-hw,       -hw * 0.55f, new Color(0.85f, 0.22f, 0.22f)),
            (-hw * 0.55f, -hw * 0.22f, new Color(0.90f, 0.74f, 0.18f)),
            (-hw * 0.22f,  hw * 0.22f, new Color(0.22f, 0.80f, 0.38f)),
            ( hw * 0.22f,  hw * 0.55f, new Color(0.90f, 0.74f, 0.18f)),
            ( hw * 0.55f,  hw,          new Color(0.85f, 0.22f, 0.22f)),
        };
        foreach (var (f, t, c) in zones)
        {
            var z = new GameObject("Zone");
            z.transform.SetParent(trackGo.transform, false);
            var zrt = z.AddComponent<RectTransform>();
            zrt.anchorMin = zrt.anchorMax = new Vector2(0.5f, 0.5f);
            zrt.sizeDelta        = new Vector2(t - f - 1f, TH - 4f);
            zrt.anchoredPosition = new Vector2((f + t) * 0.5f, 0f);
            z.AddComponent<Image>().color = c;
        }

        var indGo = new GameObject("Indicator");
        indGo.transform.SetParent(trackGo.transform, false);
        var indRt = indGo.AddComponent<RectTransform>();
        indRt.anchorMin = indRt.anchorMax = new Vector2(0.5f, 0.5f);
        indRt.sizeDelta = new Vector2(5f, TH + 10f);
        indGo.AddComponent<Image>().color = Color.white;

        var sliderHost = new GameObject("SliderHost");
        sliderHost.transform.SetParent(rt, false);
        sliderHost.AddComponent<RectTransform>();
        _bonusSlider = sliderHost.AddComponent<BonusSliderComponent>();
        _bonusSlider.Setup(trackRt, indRt, leftLbl, rightLbl, null);

        // Reward text
        _rewardTxt = MakeText(rt, "Reward", "", 18, new Vector2(0, -96), TextAlignmentOptions.Center);

        // Finish button (shown after slider completes)
        var finishBtn = MakeButton(rt, "Получить награду",
            new Color(0.10f, 0.52f, 0.86f), new Vector2(0, -158), new Vector2(260, 56));
        finishBtn.onClick.AddListener(OnFinishClicked);
        _finishBtnGo = finishBtn.gameObject;
        _finishBtnGo.SetActive(false);

        // Restart button
        var restartBtn = MakeButton(rt, "Ещё раз",
            new Color(0.16f, 0.20f, 0.28f), new Vector2(0, -226), new Vector2(260, 44));
        restartBtn.onClick.AddListener(RestartGame);

        return screen;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GAME FLOW
    // ══════════════════════════════════════════════════════════════════════════

    private void ShowStart()
    {
        _startScreen.SetActive(true);
        _gameScreen.SetActive(false);
        _endScreen.SetActive(false);
    }

    private void StartGame()
    {
        GenerateNewMap();

        _totalCargo    = _stations.Count(s => s.IsCargo);
        _countdownTime = 90f + _totalCargo * 25f;
        _remainingTime = _countdownTime;
        _cargoCollected = 0;
        _gameStarted   = true;
        _gameEnded     = false;
        _isMoving      = false;
        _currentStation = _startStation;

        PlaceTrainAt(_startStation);

        _startScreen.SetActive(false);
        _gameScreen.SetActive(true);
        _endScreen.SetActive(false);

        UpdateHUD();
        HighlightAvailable();
    }

    private void RestartGame()
    {
        _gameStarted = _gameEnded = _isMoving = false;
        _endScreen.SetActive(false);
        _gameScreen.SetActive(true);
        StartGame();
    }

    private void GenerateNewMap()
    {
        // Форсируем пересчёт layout, чтобы _mapContainer.rect отражал реальный размер
        Canvas.ForceUpdateCanvases();

        // Destroy old content (stations, connections, old train)
        foreach (Transform child in _mapContainer)
            Destroy(child.gameObject);
        _stations.Clear();
        _paths.Clear();
        _trainRect = null;
        _trainImg  = null;

        // Читаем реальный размер контейнера (после ForceUpdateCanvases он уже корректный)
        float mapW = _mapContainer.rect.width;
        float mapH = _mapContainer.rect.height;
        if (mapW < 10f) mapW = MapW;   // fallback на случай если layout ещё не рассчитан
        if (mapH < 10f) mapH = MapH;

        _mapGenerator.GenerateMap(_mapContainer,
            out _stations, out _paths, out _startStation, out _endStation,
            mapW, mapH);

        CreateTrain();
    }

    private void CreateTrain()
    {
        var trainGo = new GameObject("Train");
        trainGo.transform.SetParent(_mapContainer, false);

        _trainRect = trainGo.AddComponent<RectTransform>();
        _trainRect.anchorMin = _trainRect.anchorMax = new Vector2(0.5f, 0.5f);
        _trainRect.sizeDelta = new Vector2(22f, 14f);
        _trainRect.anchoredPosition = _startStation?.Position ?? Vector2.zero;

        // Glow halo (behind body)
        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(trainGo.transform, false);
        var glowRt = glowGo.AddComponent<RectTransform>();
        glowRt.anchorMin = glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.sizeDelta = new Vector2(36f, 28f);
        glowGo.AddComponent<Image>().color = ColTrainGlow;
        glowGo.transform.SetAsFirstSibling();

        // Body
        _trainImg        = trainGo.AddComponent<Image>();
        _trainImg.color  = ColTrain;
        _trainImg.sprite = _whiteSquare;

        // Nose indicator
        var noseGo = new GameObject("Nose");
        noseGo.transform.SetParent(trainGo.transform, false);
        var noseRt = noseGo.AddComponent<RectTransform>();
        noseRt.anchorMin = noseRt.anchorMax = new Vector2(0.5f, 1f);
        noseRt.sizeDelta = new Vector2(6f, 5f);
        noseRt.anchoredPosition = new Vector2(0, 2f);
        noseGo.AddComponent<Image>().color = Color.white;

        // Continuous soft glow pulse
        _trainGlowTween?.Kill();
        _trainGlowTween = _trainImg
            .DOColor(new Color(ColTrain.r, ColTrain.g, ColTrain.b, 0.55f), 0.9f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void EndGame(bool isTimeout)
    {
        if (_gameEnded) return;
        _gameEnded   = true;
        _gameStarted = false;
        _isMoving    = false;

        // Base score
        int baseScore = isTimeout
            ? Mathf.RoundToInt((_totalCargo > 0 ? (float)_cargoCollected / _totalCargo : 0f) * 50f)
            : Mathf.RoundToInt(60f + (_remainingTime / _countdownTime) * 40f);

        _rawScore = Mathf.Clamp(baseScore, 0, 100);

        // Title / color
        _resultTitleTxt.text  = isTimeout ? "ВРЕМЯ ВЫШЛО" : "ДОСТАВКА ЗАВЕРШЕНА!";
        _resultTitleTxt.color = isTimeout
            ? new Color(0.92f, 0.28f, 0.28f)
            : new Color(0.22f, 0.90f, 0.52f);

        _intelligenceTxt.text = _intelligence > 1
            ? $"Интеллект {_intelligence}  ·  влияет на точность ползунка"
            : "Интеллект не прокачан";

        _finishBtnGo.SetActive(false);
        _rewardTxt.text = "";
        _endScreen.SetActive(true);

        // Fade in
        var cg = _endScreen.GetComponent<CanvasGroup>();
        if (cg != null) { cg.alpha = 0f; cg.DOFade(1f, 0.35f).SetUpdate(true); }

        // Animate score counter to rawScore
        _finalScoreTxt.text = "0";
        DOTween.To(() => 0f, v => _finalScoreTxt.text = Mathf.RoundToInt(v).ToString(),
            _rawScore, 0.9f).SetEase(Ease.OutCubic).SetUpdate(true);

        // Run bonus slider
        if (_bonusSlider != null)
        {
            float intelligenceNorm = Mathf.Clamp01((_intelligence - 1) / 9f);
            _bonusSlider.Run(() => intelligenceNorm, _rawScore, OnSliderComplete);
        }
        else
        {
            _finalScore = _rawScore;
            ApplyRewardDisplay(_finalScore);
            _finishBtnGo.SetActive(true);
        }
    }

    private void OnSliderComplete(int sliderScore, float bonus)
    {
        // Slider may only add — never penalise below rawScore
        int boosted = bonus >= 1f ? Mathf.RoundToInt(_rawScore * bonus) : _rawScore;
        _finalScore = Mathf.Clamp(boosted, 0, 100);

        // Animate counter from rawScore → finalScore
        DOTween.To(() => (float)_rawScore, v => _finalScoreTxt.text = Mathf.RoundToInt(v).ToString(),
            _finalScore, 0.5f).SetEase(Ease.OutCubic).SetUpdate(true);

        _intelligenceTxt.text = bonus > 1.05f
            ? $"Интеллект {_intelligence}  ·  бонус ×{bonus:F2} ✓"
            : (_intelligence > 1 ? $"Интеллект {_intelligence}  ·  без бонуса" : "Интеллект не прокачан");

        ApplyRewardDisplay(_finalScore);
        _finishBtnGo.SetActive(true);

        Debug.Log($"[TrainPath] rawScore={_rawScore} bonus={bonus:F2} finalScore={_finalScore}");
    }

    private void ApplyRewardDisplay(int score)
    {
        if (score >= 90)
        {
            _rewardTxt.text  = "🎁  2 случайных ресурса";
            _rewardTxt.color = new Color(1f, 0.85f, 0.25f);
        }
        else if (score >= 65)
        {
            _rewardTxt.text  = "🎁  1 случайный ресурс";
            _rewardTxt.color = new Color(0.75f, 0.95f, 0.45f);
        }
        else
        {
            _rewardTxt.text  = "Результат недостаточен — без награды";
            _rewardTxt.color = new Color(0.70f, 0.35f, 0.35f);
        }
    }

    private void OnFinishClicked() => _gameEvent?.OnGameEndedWithFinalScore(_finalScore);

    // ══════════════════════════════════════════════════════════════════════════
    // UPDATE LOOP
    // ══════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (!_gameStarted || _gameEnded) return;

        _remainingTime -= Time.deltaTime;
        UpdateHUD();

        if (_remainingTime <= 0f)
        {
            _remainingTime = 0f;
            EndGame(isTimeout: true);
            return;
        }

        if (!_isMoving)
        {
            bool tapped = Input.touchCount > 0
                ? Input.GetTouch(0).phase == TouchPhase.Began
                : Input.GetMouseButtonDown(0);

            if (tapped) HandleClick();
        }
    }

    private void HandleClick()
    {
        Vector2 inputPos = Input.touchCount > 0
            ? (Vector2)Input.GetTouch(0).position
            : (Vector2)Input.mousePosition;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _mapContainer, inputPos, canvas.worldCamera, out Vector2 local);

        Station clicked = _stations.FirstOrDefault(s => Vector2.Distance(local, s.Position) < 34f);
        if (clicked != null && CanMoveTo(clicked))
            MoveToStation(clicked);
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private bool CanMoveTo(Station target)
    {
        if (_currentStation == null || target == null || _currentStation == target) return false;
        if (target == _endStation && _cargoCollected < _totalCargo) return false;

        return _paths.Any(p =>
            (p.From == _currentStation && p.To == target) ||
            (p.From == target && p.To == _currentStation));
    }

    private void MoveToStation(Station target)
    {
        if (_isMoving) return;

        TrainPathConnection conn = _paths.FirstOrDefault(p =>
            (p.From == _currentStation && p.To == target) ||
            (p.From == target && p.To == _currentStation));
        if (conn == null) return;

        _isMoving       = true;
        _currentStation = target;
        HighlightAvailable();

        conn.PlayActiveFlash(conn.TravelTime);
        StartCoroutine(MoveTrain(target, conn.TravelTime));
    }

    private IEnumerator MoveTrain(Station target, float travelTime)
    {
        Vector2 startPos = _trainRect.anchoredPosition;
        Vector2 endPos   = target.Position;
        float   elapsed  = 0f;

        // Point train towards destination
        Vector2 dir   = (endPos - startPos).normalized;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        _trainRect.DORotate(new Vector3(0, 0, angle), 0.12f).SetUpdate(true);

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / travelTime;
            // Smooth-step for ease-in-out feel
            t = t * t * (3f - 2f * t);
            _trainRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        _trainRect.anchoredPosition = endPos;

        // Arrival "thud"
        _trainRect.DOPunchScale(Vector3.one * 0.28f, 0.18f, 5).SetUpdate(true);

        yield return new WaitForSeconds(target.WaitTime);

        TryCollectCargo(target);

        _isMoving = false;

        if (target == _endStation && _cargoCollected >= _totalCargo)
            EndGame(isTimeout: false);
        else
            HighlightAvailable();
    }

    private void TryCollectCargo(Station station)
    {
        if (!station.IsCargo || station.IsCargoCollected) return;
        station.CollectCargo();
        _cargoCollected++;
        UpdateHUD();
    }

    private void PlaceTrainAt(Station station)
    {
        if (_trainRect != null && station != null)
            _trainRect.anchoredPosition = station.Position;
    }

    private void HighlightAvailable()
    {
        foreach (var s in _stations)
            s.SetHighlight(CanMoveTo(s));
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    private void UpdateHUD()
    {
        if (_timerTxt != null)
        {
            int m = Mathf.FloorToInt(_remainingTime / 60f);
            int s = Mathf.FloorToInt(_remainingTime % 60f);
            _timerTxt.text  = $"{m}:{s:00}";
            _timerTxt.color = _remainingTime < 20f ? new Color(1f, 0.25f, 0.25f)
                            : _remainingTime < 40f ? new Color(1f, 0.78f, 0.10f)
                            : Color.white;
        }

        if (_timerBarFill != null)
        {
            float t = _countdownTime > 0 ? _remainingTime / _countdownTime : 0f;
            _timerBarFill.fillAmount = t;
            _timerBarFill.color = t > 0.5f ? new Color(0.20f, 0.85f, 0.44f)
                                : t > 0.25f ? new Color(0.95f, 0.75f, 0.10f)
                                :             new Color(0.90f, 0.20f, 0.20f);
        }

        if (_cargoTxt != null)
        {
            bool done = _cargoCollected >= _totalCargo && _totalCargo > 0;
            _cargoTxt.text = done
                ? $"📦 {_cargoCollected}/{_totalCargo}  ✓  Теперь к финишу!"
                : $"📦 {_cargoCollected}/{_totalCargo}";
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // UI HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private static TextMeshProUGUI MakeText(RectTransform parent, string name, string text,
        float size, Vector2 pos, TextAlignmentOptions align)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(380f, 70f);
        rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = align;
        tmp.color     = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button MakeButton(RectTransform parent, string label, Color color, Vector2 pos, Vector2 size)
    {
        var go = new GameObject($"Btn_{label}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.sprite = _whiteSquare;
        img.color  = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cols = btn.colors;
        cols.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        cols.pressedColor     = Color.Lerp(color, Color.black, 0.20f);
        btn.colors = cols;

        var lgo = new GameObject("Label");
        lgo.transform.SetParent(go.transform, false);
        var lrt = lgo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var tmp = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 21;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }

    private static GameObject MakeOverlay(RectTransform parent, string name,
        Color? bgColor = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        if (bgColor.HasValue)
            go.AddComponent<Image>().color = bgColor.Value;
        go.AddComponent<CanvasGroup>();
        return go;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SPRITE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private static void EnsureSprites()
    {
        if (_whiteSquare == null)
        {
            var tex = new Texture2D(4, 4);
            for (int i = 0; i < 16; i++) tex.SetPixel(i % 4, i / 4, Color.white);
            tex.Apply();
            _whiteSquare = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
        }

        if (_circleSprite == null)
        {
            const int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float cx = sz * 0.5f, r = cx - 1f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Mathf.Sqrt((x - cx + 0.5f) * (x - cx + 0.5f) +
                                         (y - cx + 0.5f) * (y - cx + 0.5f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((r - d) / 1.5f)));
                }
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f);
        }
    }

    private void OnDestroy()
    {
        _trainGlowTween?.Kill();
        DOTween.Kill(gameObject);
    }
}
