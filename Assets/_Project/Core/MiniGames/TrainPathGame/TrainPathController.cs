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
    // Карта свёрстана под эту ширину; под реальное окно она масштабируется целиком
    private const float DesignMapWidth = 330f;
    private const float MapSideInset = 24f;
    private const float MapBorderWidth = 6f;
    private const float MapInnerPadding = 10f;

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
    private RectTransform      _rootRect;
    private GameObject         _startScreen;
    private GameObject         _gameScreen;
    private RectTransform      _gameScreenRect;
    private TextMeshProUGUI    _timerTxt;
    private Image              _timerBarFill;
    private TextMeshProUGUI    _cargoTxt;
    private TextMeshProUGUI    _intelligenceTxt;
    private TextMeshProUGUI    _rewardTxt;
    private BonusSliderComponent _bonusSlider;
    private MiniGameResultPanel _resultPanel;
    private MiniGameCountdown   _countdown;
    private int                _lastTimerSecond = -1;

    // ── Visual constants ──────────────────────────────────────────────────────
    private static readonly Color ColBg        = MiniGameTheme.Background;
    private static readonly Color ColMapBg     = new Color(0.94f, 0.97f, 0.93f);
    private static readonly Color ColMapBorder = new Color(0.745f, 0.851f, 0.706f);
    private static readonly Color ColHeader    = new Color(1f, 1f, 1f, 0.96f);
    private static readonly Color ColTrain     = new Color(0.184f, 0.310f, 0.310f);
    private static readonly Color ColTrainGlow = new Color(0.184f, 0.310f, 0.310f, 0.4f);

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
        _rootRect = root;

        // Ничего не рисуем за пределами окна мини-игры
        if (root.GetComponent<RectMask2D>() == null)
            root.gameObject.AddComponent<RectMask2D>();

        // Background
        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(root, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bgGo.AddComponent<Image>().color = ColBg;

        _startScreen = BuildStartScreen(root);
        _gameScreen  = BuildGameScreen(root);
        _gameScreenRect = _gameScreen.GetComponent<RectTransform>();
    }

    // ── Start screen ──────────────────────────────────────────────────────────

    private GameObject BuildStartScreen(RectTransform root)
    {
        var screen = MakeOverlay(root, "StartScreen");
        var rt     = MiniGameTheme.CreateScaledContent(screen.GetComponent<RectTransform>(), new Vector2(400f, 470f));

        var card = MiniGameTheme.CreateCard(rt, "Card", new Vector2(400f, 470f), out _, MiniGameTheme.Card, 26f);
        card.anchoredPosition = new Vector2(0f, -10f);

        // Title
        var title = MakeText(rt, "Title", "ЖЕЛЕЗНАЯ\nДОРОГА", 46,
            new Vector2(0, 145), TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.color     = MiniGameTheme.Accent;

        // Subtitle
        var sub = MakeText(rt, "Sub",
            "Собери все грузы и отвези их на финальную станцию\nЧем быстрее — тем больше очков",
            17, new Vector2(0, 50), TextAlignmentOptions.Center);
        sub.color = MiniGameTheme.TextSecondary;

        // Legend row
        var leg = MakeText(rt, "Legend",
            "<color=#339E66>С</color> старт    <color=#D64D4D>Ф</color> финиш    <color=#C7870A>Г</color> груз",
            14, new Vector2(0, -5), TextAlignmentOptions.Center);
        leg.color = MiniGameTheme.TextSecondary;

        // Intelligence badge
        var badge = MakeText(rt, "Intelligence",
            $"Интеллект {_intelligence}  ·  влияет на ползунок",
            15, new Vector2(0, -48), TextAlignmentOptions.Center);
        badge.color = MiniGameTheme.Success;

        // Start button
        var startBtn = MakeButton(rt, "НАЧАТЬ",
            MiniGameTheme.Accent, new Vector2(0, -112), new Vector2(220, 56));
        MiniGameTheme.StyleButton(startBtn, MiniGameTheme.Accent, MiniGameTheme.TextOnAccent);
        startBtn.onClick.AddListener(StartGame);

        // Close button
        var closeBtn = MiniGameTheme.CreateExitButton(rt, new Vector2(0f, -180f));
        closeBtn.onClick.AddListener(() => _gameEvent?.CloseGame());

        return screen;
    }

    // ── Game screen ───────────────────────────────────────────────────────────

    private GameObject BuildGameScreen(RectTransform root)
    {
        var screen = MakeOverlay(root, "GameScreen");
        screen.SetActive(false);

        var rt = screen.GetComponent<RectTransform>();

        // HUD: груз слева, таймер по центру, полоса времени снизу
        var hud = MiniGameTheme.CreateHudBar(rt);

        _timerTxt = MakeText(hud, "Timer", "2:00", 72, Vector2.zero, TextAlignmentOptions.Center);
        _timerTxt.fontStyle = FontStyles.Bold;
        MiniGameTheme.PlaceHudText(_timerTxt, 0.5f, new Vector2(0f, 8f), new Vector2(260f, 110f));

        _cargoTxt = MakeText(hud, "Cargo", "Груз 0/0", 34, Vector2.zero, TextAlignmentOptions.MidlineLeft);
        _cargoTxt.fontStyle = FontStyles.Bold;
        _cargoTxt.color = new Color(0.80f, 0.52f, 0.05f);
        _cargoTxt.enableAutoSizing = true;
        _cargoTxt.fontSizeMin = 20f;
        _cargoTxt.fontSizeMax = 34f;
        MiniGameTheme.PlaceHudText(_cargoTxt, 0f, new Vector2(40f, 8f), new Vector2(270f, 96f));

        var barBg = new GameObject("BarBg");
        barBg.transform.SetParent(hud, false);
        var barBgRt = barBg.AddComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0f, 0f); barBgRt.anchorMax = new Vector2(1f, 0f);
        barBgRt.sizeDelta        = new Vector2(-96f, 10f);
        barBgRt.anchoredPosition = new Vector2(0f, 18f);
        barBg.AddComponent<Image>().color = MiniGameTheme.CardLight;

        var barFillGo = new GameObject("BarFill");
        barFillGo.transform.SetParent(barBg.transform, false);
        var barFillRt = barFillGo.AddComponent<RectTransform>();
        barFillRt.anchorMin = Vector2.zero; barFillRt.anchorMax = Vector2.one;
        barFillRt.offsetMin = barFillRt.offsetMax = Vector2.zero;
        _timerBarFill = barFillGo.AddComponent<Image>();
        _timerBarFill.color = MiniGameTheme.Success;
        _timerBarFill.type  = Image.Type.Filled;
        _timerBarFill.fillMethod = Image.FillMethod.Horizontal;
        _timerBarFill.fillAmount = 1f;

        // Рамка карты — под HUD, с отступами от краёв окна
        var borderGo = new GameObject("MapBorder");
        borderGo.transform.SetParent(rt, false);
        var borderRt = borderGo.AddComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(MapSideInset, MapSideInset);
        borderRt.offsetMax = new Vector2(-MapSideInset, -MiniGameTheme.HudZoneHeight);
        var borderImg = borderGo.AddComponent<Image>();
        borderImg.color = ColMapBorder;
        MiniGameTheme.ApplyRounded(borderImg, 32f);

        // Поле карты — 6px внутрь рамки
        var mapPanelGo = new GameObject("MapPanel");
        mapPanelGo.transform.SetParent(rt, false);
        var mapPanelRt = mapPanelGo.AddComponent<RectTransform>();
        mapPanelRt.anchorMin = Vector2.zero; mapPanelRt.anchorMax = Vector2.one;
        mapPanelRt.offsetMin = new Vector2(MapSideInset + MapBorderWidth, MapSideInset + MapBorderWidth);
        mapPanelRt.offsetMax = new Vector2(-MapSideInset - MapBorderWidth, -MiniGameTheme.HudZoneHeight - MapBorderWidth);
        var mapPanelImg = mapPanelGo.AddComponent<Image>();
        mapPanelImg.color = ColMapBg;
        MiniGameTheme.ApplyRounded(mapPanelImg, 26f);

        // Контейнер карты: размер и масштаб выставляются в GenerateNewMap под реальное окно
        var mcGo = new GameObject("MapContainer");
        mcGo.transform.SetParent(mapPanelGo.transform, false);
        _mapContainer = mcGo.AddComponent<RectTransform>();
        _mapContainer.anchorMin = _mapContainer.anchorMax = new Vector2(0.5f, 0.5f);
        _mapContainer.sizeDelta = new Vector2(MapW, MapH);

        return screen;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // GAME FLOW
    // ══════════════════════════════════════════════════════════════════════════

    private void ShowStart()
    {
        _startScreen.SetActive(false);
        _gameScreen.SetActive(false);
        MiniGameJuice.FadeIn(_startScreen, 0.25f);
    }

    private void StartGame()
    {
        GenerateNewMap();

        _totalCargo    = _stations.Count(s => s.IsCargo);
        _countdownTime = 90f + _totalCargo * 25f;
        _remainingTime = _countdownTime;
        _cargoCollected = 0;
        _gameStarted   = false; // true — после отсчёта
        _gameEnded     = false;
        _isMoving      = false;
        _currentStation = _startStation;

        PlaceTrainAt(_startStation);

        MiniGameJuice.FadeOut(_startScreen, 0.2f);
        _gameScreen.SetActive(true);
        if (_resultPanel != null) { Destroy(_resultPanel.gameObject); _resultPanel = null; }
        _lastTimerSecond = -1;

        UpdateHUD();
        HighlightAvailable();

        if (_countdown != null) _countdown.Cancel();
        _countdown = MiniGameCountdown.Play(_gameScreenRect, () =>
        {
            _countdown = null;
            _gameStarted = true;
        });
    }

    private void RestartGame()
    {
        _gameStarted = _gameEnded = _isMoving = false;
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
        // Свободное место под карту считаем от корня игры (экран карты в этот момент может быть выключен).
        float mapW = MapW;
        float mapH = MapH;
        float mapScale = 1f;
        float inset = MapSideInset + MapBorderWidth + MapInnerPadding;
        float freeW = _rootRect.rect.width - inset * 2f;
        float freeH = _rootRect.rect.height - MiniGameTheme.HudZoneHeight - MapBorderWidth - MapInnerPadding - inset;
        if (freeW > 10f && freeH > 10f)
        {
            // Станции, пути и подписи заданы в «дизайнерских» единицах — растягиваем их под окно,
            // чтобы на большом канвасе они не были крошечными.
            mapScale = Mathf.Max(1f, freeW / DesignMapWidth);
            mapW = freeW / mapScale;
            mapH = freeH / mapScale;
        }

        _mapContainer.sizeDelta = new Vector2(mapW, mapH);
        _mapContainer.anchoredPosition = Vector2.zero;
        _mapContainer.localScale = Vector3.one * mapScale;

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
        _trainRect.sizeDelta = new Vector2(28f, 18f);
        _trainRect.anchoredPosition = _startStation?.Position ?? Vector2.zero;

        // Glow halo (behind body)
        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(trainGo.transform, false);
        var glowRt = glowGo.AddComponent<RectTransform>();
        glowRt.anchorMin = glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.sizeDelta = new Vector2(52f, 52f);
        var glowImg = glowGo.AddComponent<Image>();
        glowImg.sprite = MiniGameTheme.SoftCircleSprite;
        glowImg.color = ColTrainGlow;
        glowImg.raycastTarget = false;
        glowGo.transform.SetAsFirstSibling();

        // Body
        _trainImg        = trainGo.AddComponent<Image>();
        _trainImg.color  = ColTrain;
        MiniGameTheme.ApplyRounded(_trainImg, 6f);

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

        if (isTimeout)
        {
            MiniGameJuice.Feedback(FeedbackType.Miss);
            MiniGameJuice.Flash(_rootRect, MiniGameTheme.Danger, 0.3f, 0.4f);
            MiniGameJuice.Shake(_gameScreenRect, 10f, 0.3f);
        }
        else if (_endStation != null)
        {
            MiniGameJuice.Burst(_mapContainer, _endStation.Position, MiniGameTheme.Success, 18, 120f, 20f);
        }

        // Base score
        int baseScore = isTimeout
            ? Mathf.RoundToInt((_totalCargo > 0 ? (float)_cargoCollected / _totalCargo : 0f) * 50f)
            : Mathf.RoundToInt(60f + (_remainingTime / _countdownTime) * 40f);

        _rawScore = Mathf.Clamp(baseScore, 0, 100);

        // Result panel (count-up, звёзды, рекорд)
        int remM = Mathf.FloorToInt(_remainingTime / 60f);
        int remS = Mathf.FloorToInt(_remainingTime % 60f);
        if (_resultPanel != null) Destroy(_resultPanel.gameObject);
        _resultPanel = MiniGameResultPanel.Show(_rootRect, new MiniGameResultPanel.Options
        {
            GameId         = "trainpath",
            Title          = isTimeout ? "ВРЕМЯ ВЫШЛО" : "ДОСТАВКА ЗАВЕРШЕНА!",
            TitleColor     = isTimeout ? MiniGameTheme.Danger : MiniGameTheme.Success,
            Score          = _rawScore,
            MaxScore       = 100,
            StarThresholds = new[] { 0.4f, 0.65f, 0.9f },
            ScoreCaption   = "очков из 100",
            StatLines      = new[]
            {
                $"Грузов доставлено: {_cargoCollected}/{_totalCargo}",
                isTimeout ? "Время истекло" : $"Осталось времени: {remM}:{remS:00}",
            },
            ExtraHeight    = 96f,
            ShowInfoLine   = true,
            PrimaryLabel   = "Получить награду",
            OnPrimary      = OnFinishClicked,
            PrimaryVisibleImmediately = false,
            SecondaryLabel = "Ещё раз",
            OnSecondary    = RestartGame,
            CommitBestImmediately     = false,
        });
        _bonusSlider = _resultPanel.BuildBonusSlider("Интеллект", "% задания", out _intelligenceTxt);
        _rewardTxt   = _resultPanel.InfoText;

        _intelligenceTxt.text = _intelligence > 1
            ? $"Интеллект {_intelligence}  ·  влияет на точность ползунка"
            : "Интеллект не прокачан";
        _rewardTxt.text = "";

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
            if (_resultPanel != null)
            {
                _resultPanel.UpdateScore(_finalScore);
                _resultPanel.SetPrimaryVisible(true);
            }
        }
    }

    private void OnSliderComplete(int sliderScore, float bonus)
    {
        // Slider may only add — never penalise below rawScore
        int boosted = bonus >= 1f ? Mathf.RoundToInt(_rawScore * bonus) : _rawScore;
        _finalScore = Mathf.Clamp(boosted, 0, 100);

        // Animate counter from rawScore → finalScore (+звёзды, +рекорд)
        if (_resultPanel != null) _resultPanel.UpdateScore(_finalScore);

        _intelligenceTxt.text = bonus > 1.05f
            ? $"Интеллект {_intelligence}  ·  бонус ×{bonus:F2} ✓"
            : (_intelligence > 1 ? $"Интеллект {_intelligence}  ·  без бонуса" : "Интеллект не прокачан");

        ApplyRewardDisplay(_finalScore);
        if (_resultPanel != null) _resultPanel.SetPrimaryVisible(true);

        Debug.Log($"[TrainPath] rawScore={_rawScore} bonus={bonus:F2} finalScore={_finalScore}");
    }

    private void ApplyRewardDisplay(int score)
    {
        if (score >= 90)
        {
            _rewardTxt.text  = "🎁  2 случайных ресурса";
            _rewardTxt.color = MiniGameTheme.Warning;
        }
        else if (score >= 65)
        {
            _rewardTxt.text  = "🎁  1 случайный ресурс";
            _rewardTxt.color = MiniGameTheme.Success;
        }
        else
        {
            _rewardTxt.text  = "Результат недостаточен — без награды";
            _rewardTxt.color = new Color(0.70f, 0.35f, 0.35f);
        }

        MiniGameJuice.Punch(_rewardTxt.transform, 0.2f, 0.3f);
        if (score >= 65) MiniGameJuice.Feedback(FeedbackType.Reward);
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
        if (clicked == null) return;

        if (CanMoveTo(clicked))
        {
            MiniGameJuice.Feedback(FeedbackType.Tap);
            MoveToStation(clicked);
        }
        else if (clicked != _currentStation)
        {
            // недоступная станция — «нет» (punch, а не shake: позицию поезда не трогаем)
            MiniGameJuice.Feedback(FeedbackType.Error);
            if (_trainRect != null) MiniGameJuice.Punch(_trainRect, 0.25f, 0.2f);
        }
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
        MiniGameJuice.Burst(_mapContainer, endPos, ColTrain, 6, 40f, 10f, 0.4f);

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

        MiniGameJuice.Burst(_mapContainer, station.Position, new Color(1f, 0.62f, 0.15f), 14, 90f, 16f);
        if (_cargoTxt != null) MiniGameJuice.Punch(_cargoTxt.transform, 0.3f, 0.3f);
        if (_cargoCollected >= _totalCargo)
        {
            MiniGameJuice.Feedback(FeedbackType.Success);
            MiniGameJuice.Flash(_gameScreenRect, MiniGameTheme.Success, 0.15f, 0.4f);
        }
        else
        {
            MiniGameJuice.Feedback(FeedbackType.Hit);
        }
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
                            : MiniGameTheme.TextPrimary;

            // последние 10 секунд — тик таймера
            int secLeft = Mathf.CeilToInt(_remainingTime);
            if (_gameStarted && secLeft != _lastTimerSecond)
            {
                if (_lastTimerSecond > 0 && secLeft <= 10 && secLeft > 0)
                {
                    MiniGameJuice.Punch(_timerTxt.transform, 0.25f, 0.25f);
                    MiniGameJuice.Feedback(FeedbackType.Tap);
                }
                _lastTimerSecond = secLeft;
            }
        }

        if (_timerBarFill != null)
        {
            float t = _countdownTime > 0 ? _remainingTime / _countdownTime : 0f;
            _timerBarFill.fillAmount = t;
            _timerBarFill.color = t > 0.5f ? MiniGameTheme.Success
                                : t > 0.25f ? new Color(0.95f, 0.75f, 0.10f)
                                :             new Color(0.90f, 0.20f, 0.20f);
        }

        if (_cargoTxt != null)
        {
            bool done = _cargoCollected >= _totalCargo && _totalCargo > 0;
            _cargoTxt.text = done
                ? $"Груз {_cargoCollected}/{_totalCargo} — к финишу!"
                : $"Груз {_cargoCollected}/{_totalCargo}";
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
        tmp.color     = MiniGameTheme.TextPrimary;
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
