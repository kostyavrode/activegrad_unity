using System.Collections.Generic;
using ActiveGrad.MiniGames;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JumpController : MonoBehaviour
{
    // ── wiring ────────────────────────────────────────────────────────────────
    private JumpGameEvent _gameEvent;

    // ── constants ─────────────────────────────────────────────────────────────
    private const float GameDuration       = 30f;
    private const float GroundY            = -220f;
    private const float PlayerX            = -260f;
    private const float ObstacleStartX     =  500f;
    private const float ObstacleKillX      = -550f;
    private const float BaseSpeed          =  380f;
    private const float SpeedRampPer5s     =   40f;
    private const float JumpVelocity       = 1050f;
    private const float Gravity            = -2800f;
    private const float DoubleJumpVel      =  900f;
    private const float SlamVelocity       = 4000f;  // резкий удар вниз
    private const float PlayerW            =   54f;
    private const float PlayerH            =   62f;
    private const float SpawnMinGap        =    0.6f;
    private const float SpawnMaxGap        =    1.1f;
    private const int   PointsPerCoin      =  100;
    private const int   PointsClear        =   40;
    private const int   PointsHitPenalty   =   60;   // штраф за удар
    private const float CrouchScaleY       =   0.4f; // приседание — 40% высоты
    private const float CrouchScaleX       =   1.5f; // компенсация ширины
    private const float CrouchAnimDur      =   0.07f;
    private const float SlamCrouchDuration =   3.75f; // сколько держим сплющивание после удара
    private const float SwipeDownThreshold =   80f;  // пикселей вниз для свайпа

    // Балка-препятствие: требует приседания
    // Центр балки на GroundY + BeamCenterOffset, высота BeamH
    // Нижний край балки = GroundY + BeamCenterOffset - BeamH/2 ≈ -198
    // Верх игрока стоя  = GroundY + PlayerH/2 ≈ -189  → удар
    // Верх игрока сидя  = GroundY + (PlayerH*CrouchScaleY)/2 ≈ -208 → проходит
    private const float BeamCenterOffset   =  42f;
    private const float BeamH              =  40f;
    private const float BeamW              = 100f;

    // ── state ─────────────────────────────────────────────────────────────────
    private bool  _running;
    private float _timeLeft;
    private float _elapsed;
    private float _nextSpawnIn;
    private int   _score;
    private int   _combo;
    private float _velY;
    private bool  _onGround;
    private bool  _hasDoubleJump;
    private bool  _isDead;
    private int   _agility = 1;
    private int   _finalScore;   // сохраняем для кнопки «Завершить»

    // crouch / slam
    private bool  _isCrouching;
    private bool  _isSlamming;
    private Tween _crouchTween;

    // swipe / hold detection
    private Vector2 _touchStartPos;
    private bool    _touchTracking;
    private bool    _fingerHeld;     // true пока палец / кнопка зажата

    // ── ui refs ───────────────────────────────────────────────────────────────
    private RectTransform    _playerRect;
    private Image            _playerImg;
    private TextMeshProUGUI  _scoreTxt;
    private TextMeshProUGUI  _timerTxt;
    private TextMeshProUGUI  _comboTxt;
    private GameObject       _startScreen;
    private GameObject       _gameScreen;
    private RectTransform    _rootRect;
    private RectTransform    _bgRect;

    // ── juice / result ────────────────────────────────────────────────────────
    private MiniGameResultPanel _resultPanel;
    private MiniGameCountdown   _countdown;
    private int              _maxCombo;
    private int              _coinsCollected;
    private int              _obstacleHits;
    private int              _lastHudCombo;
    private int              _lastTimerSecond = -1;

    // ── obstacles / coins ─────────────────────────────────────────────────────
    private struct Lane
    {
        public RectTransform rect;
        public bool isCoin;
        public bool isCeiling; // балка сверху — нужно приседать
        public bool passed;
        public bool wasHit;    // был ли уже столкновение
    }
    private List<Lane> _lanes = new List<Lane>();
    private RectTransform _laneContainer;

    // ── bg layers ─────────────────────────────────────────────────────────────
    private RectTransform[] _bgLayers;
    private float[]         _bgSpeeds;
    private float[]         _bgWidths;

    // ── sprites ───────────────────────────────────────────────────────────────
    private static Sprite _whiteSquare;
    private static Sprite _circleSprite;

    // ── config ────────────────────────────────────────────────────────────────
    private JumpGameConfig _cfg;

    // ── public init ───────────────────────────────────────────────────────────
    public void Initialize(JumpGameEvent gameEvent, int agility = 1, JumpGameConfig config = null)
    {
        _gameEvent = gameEvent;
        _agility   = Mathf.Max(1, agility);
        _cfg       = config;
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

        // Обрезаем всё, что выходит за окно мини-игры (холмы, препятствия, тряска фона)
        if (root.GetComponent<RectMask2D>() == null)
            root.gameObject.AddComponent<RectMask2D>();

        var bg = MakePanel(root, "BG", _cfg != null ? _cfg.skyColor : MiniGameTheme.Background);
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
        _bgRect = bg;

        BuildBackground(bg);

        // Земля: от линии ног игрока до низа экрана
        var ground = MakePanel(bg, "Ground", _cfg != null ? _cfg.groundColor : new Color(0.42f, 0.62f, 0.45f, 1f));
        ground.anchorMin = new Vector2(0, 0); ground.anchorMax = new Vector2(1, 0.5f);
        ground.offsetMin = Vector2.zero;
        ground.offsetMax = new Vector2(0, GroundY - PlayerH * 0.5f);

        var groundColor = ground.GetComponent<Image>().color;
        var grass = MakePanel(ground, "Grass", Color.Lerp(groundColor, Color.black, 0.18f));
        grass.anchorMin = new Vector2(0, 1); grass.anchorMax = new Vector2(1, 1);
        grass.pivot = new Vector2(0.5f, 1f);
        grass.sizeDelta = new Vector2(0, 14f);
        grass.anchoredPosition = Vector2.zero;

        var laneGo = new GameObject("Lanes");
        laneGo.transform.SetParent(bg, false);
        _laneContainer = laneGo.AddComponent<RectTransform>();
        _laneContainer.anchorMin = Vector2.zero; _laneContainer.anchorMax = Vector2.one;
        _laneContainer.offsetMin = Vector2.zero;  _laneContainer.offsetMax = Vector2.zero;

        var playerGo = new GameObject("Player");
        playerGo.transform.SetParent(bg, false);
        _playerRect = playerGo.AddComponent<RectTransform>();
        _playerRect.sizeDelta = new Vector2(PlayerW, PlayerH);
        _playerRect.anchorMin = _playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        _playerRect.anchoredPosition = new Vector2(PlayerX, GroundY);
        _playerImg = playerGo.AddComponent<Image>();
        _playerImg.sprite = (_cfg != null && _cfg.playerSprite != null) ? _cfg.playerSprite : _whiteSquare;
        _playerImg.color  = _cfg != null ? _cfg.playerColor : new Color(0.35f, 0.75f, 1f);

        AddEyes(playerGo.transform);

        BuildHUD(bg);

        _startScreen = BuildStartScreen(bg);
        _gameScreen  = new GameObject("GameScreen");
        _gameScreen.transform.SetParent(bg, false);
        _gameScreen.AddComponent<RectTransform>();
    }

    private void BuildBackground(RectTransform parent)
    {
        // Три слоя холмов-силуэтов. Центры холмов лежат на линии земли: нижнюю половину закрывает Ground.
        const float layerWidth = 1200f;
        const int hillsPerLayer = 5;
        _bgLayers = new RectTransform[3];
        _bgSpeeds = new float[] { 0.15f, 0.35f, 0.6f };
        _bgWidths = new float[3];

        Color[] cols = _cfg != null
            ? new[] { _cfg.bgLayer0Color, _cfg.bgLayer1Color, _cfg.bgLayer2Color }
            : new[] { new Color(0.74f, 0.82f, 0.74f), new Color(0.67f, 0.78f, 0.68f), new Color(0.60f, 0.74f, 0.61f) };
        Vector2[] widths  = { new Vector2(380f, 520f), new Vector2(260f, 380f), new Vector2(160f, 260f) };
        Vector2[] heights = { new Vector2(300f, 420f), new Vector2(200f, 280f), new Vector2(110f, 170f) };
        float groundTop = GroundY - PlayerH * 0.5f;

        for (int i = 0; i < 3; i++)
        {
            var go = new GameObject($"BgLayer{i}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(layerWidth, 10f);
            rt.anchoredPosition = new Vector2(0, groundTop);
            _bgLayers[i] = rt;
            _bgWidths[i] = layerWidth;

            for (int b = 0; b < hillsPerLayer; b++)
            {
                float bx = -layerWidth * 0.5f + (b + 0.5f) * (layerWidth / hillsPerLayer) + Random.Range(-40f, 40f);
                float bw = Random.Range(widths[i].x, widths[i].y);
                float bh = Random.Range(heights[i].x, heights[i].y);

                // копии на ±ширину слоя — чтобы прокрутка зацикливалась без разрывов
                for (int k = -1; k <= 1; k++)
                {
                    var bump = new GameObject($"Hill{b}");
                    bump.transform.SetParent(go.transform, false);
                    var brt = bump.AddComponent<RectTransform>();
                    brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                    brt.sizeDelta = new Vector2(bw, bh);
                    brt.anchoredPosition = new Vector2(bx + k * layerWidth, 0f);
                    var bimg = bump.AddComponent<Image>();
                    bimg.sprite = (_cfg != null && _cfg.bgHillSprite != null) ? _cfg.bgHillSprite : _circleSprite;
                    bimg.color = cols[i];
                    bimg.raycastTarget = false;
                }
            }
        }
    }

    private void BuildHUD(RectTransform parent)
    {
        // Белая плашка сверху: комбо слева, таймер по центру, счёт справа
        var bar = MakePanel(parent, "HUD", MiniGameTheme.Card);
        bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.sizeDelta = new Vector2(-48f, 150f);
        bar.anchoredPosition = new Vector2(0, -60f);
        var barImg = bar.GetComponent<Image>();
        MiniGameTheme.ApplyRounded(barImg, 40f);
        barImg.raycastTarget = false;

        _scoreTxt = MakeText(bar, "Score", "0", 60, Vector2.zero, TextAlignmentOptions.MidlineRight);
        _scoreTxt.fontStyle = FontStyles.Bold;
        PlaceHudText(_scoreTxt, 1f, new Vector2(-40f, 0f), new Vector2(360f, 110f));

        _timerTxt = MakeText(bar, "Timer", "30", 84, Vector2.zero, TextAlignmentOptions.Center);
        _timerTxt.fontStyle = FontStyles.Bold;
        PlaceHudText(_timerTxt, 0.5f, Vector2.zero, new Vector2(260f, 130f));

        _comboTxt = MakeText(bar, "Combo", "", 38, Vector2.zero, TextAlignmentOptions.MidlineLeft);
        _comboTxt.fontStyle = FontStyles.Bold;
        _comboTxt.color = MiniGameTheme.Warning;
        PlaceHudText(_comboTxt, 0f, new Vector2(40f, 0f), new Vector2(360f, 110f));
    }

    private static void PlaceHudText(TextMeshProUGUI txt, float anchorX, Vector2 pos, Vector2 size)
    {
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 0.5f);
        rt.pivot = new Vector2(anchorX, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    private GameObject BuildStartScreen(RectTransform parent)
    {
        var screen = MakeFullPanel(parent, "StartScreen", new Color(MiniGameTheme.Background.r, MiniGameTheme.Background.g, MiniGameTheme.Background.b, 0.92f));
        var srt = MiniGameTheme.CreateScaledContent(screen.GetComponent<RectTransform>(), new Vector2(400f, 450f));

        var card = MiniGameTheme.CreateCard(srt, "Card", new Vector2(400f, 450f), out _, MiniGameTheme.Card, 26f);
        card.anchoredPosition = new Vector2(0f, -10f);

        var title = MakeText(srt, "Title", "ПРЫЖКИ", 52, new Vector2(0, 140), TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.color = MiniGameTheme.Accent;

        var sub = MakeText(srt, "Sub",
            "Нажми — прыгнуть\nДважды — двойной прыжок\nСвайп вниз — удар о землю\nСобирай монеты, избегай препятствий",
            20, new Vector2(0, 35), TextAlignmentOptions.Center);
        sub.color = MiniGameTheme.TextSecondary;

        var btn = MakeButton(srt, "НАЧАТЬ",
            MiniGameTheme.Accent, new Vector2(0, -110), new Vector2(220, 56));
        MiniGameTheme.StyleButton(btn, MiniGameTheme.Accent, MiniGameTheme.TextOnAccent);
        btn.onClick.AddListener(StartGame);

        var closeBtn = MiniGameTheme.CreateExitButton(srt, new Vector2(0f, -180f));
        closeBtn.onClick.AddListener(() => _gameEvent?.CloseGame());

        return screen;
    }

    // поля экрана результатов (строятся в MiniGameResultPanel)
    private TextMeshProUGUI    _agilityBonusTxt;
    private TextMeshProUGUI    _rewardTxt;
    private BonusSliderComponent _bonusSlider;
    private int                _rawScore;

    private void AddEyes(Transform playerT)
    {
        for (int i = 0; i < 2; i++)
        {
            var eye = new GameObject($"Eye{i}");
            eye.transform.SetParent(playerT, false);
            var rt = eye.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(8f, 8f);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(i == 0 ? -10f : 10f, 10f);
            var img = eye.AddComponent<Image>();
            img.sprite = _circleSprite;
            img.color = new Color(0.08f, 0.08f, 0.15f);
            img.raycastTarget = false;
        }
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
        if (_countdown != null) return; // отсчёт уже идёт

        MiniGameJuice.FadeOut(_startScreen, 0.2f);
        _gameScreen.SetActive(true);
        if (_resultPanel != null) { Destroy(_resultPanel.gameObject); _resultPanel = null; }

        _running       = false; // запуск — после отсчёта
        _timeLeft      = GameDuration;
        _elapsed       = 0f;
        _score         = 0;
        _combo         = 0;
        _velY          = 0f;
        _onGround      = true;
        _hasDoubleJump = true;
        _isDead        = false;
        _isCrouching   = false;
        _isSlamming    = false;
        _touchTracking = false;
        _fingerHeld    = false;
        _nextSpawnIn   = Random.Range(0.6f, 1.2f);

        _playerRect.anchoredPosition = new Vector2(PlayerX, GroundY);
        _playerRect.localScale = Vector3.one;
        _playerImg.color = new Color(0.35f, 0.75f, 1f);

        foreach (var l in _lanes)
            if (l.rect != null) Destroy(l.rect.gameObject);
        _lanes.Clear();

        _maxCombo = _coinsCollected = _obstacleHits = 0;
        _lastHudCombo = 0;
        _lastTimerSecond = -1;
        UpdateHUD();

        _countdown = MiniGameCountdown.Play(_rootRect, () =>
        {
            _countdown = null;
            _running = true;
        });
    }

    private void EndGame()
    {
        _running = false;
        _crouchTween?.Kill();

        foreach (var l in _lanes)
            if (l.rect != null) Destroy(l.rect.gameObject);
        _lanes.Clear();

        // ── базовый счёт (до ползунка) ────────────────────────────────────────
        _rawScore = Mathf.Clamp(_score / 8, 0, 100);

        // ── подпись ловкости ──────────────────────────────────────────────────
        if (_resultPanel != null) Destroy(_resultPanel.gameObject);
        _resultPanel = MiniGameResultPanel.Show(_rootRect, new MiniGameResultPanel.Options
        {
            GameId         = "jump",
            Title          = "ИГРА ОКОНЧЕНА",
            TitleColor     = MiniGameTheme.Accent,
            Score          = _rawScore,
            MaxScore       = 100,
            StarThresholds = new[] { 0.35f, 0.65f, 0.9f },
            ScoreCaption   = "очков из 100",
            StatLines      = new[]
            {
                $"Очки забега: {_score}",
                $"Монет: {_coinsCollected}   Столкновений: {_obstacleHits}",
                $"Макс. комбо: x{_maxCombo}",
            },
            ExtraHeight    = 96f,
            ShowInfoLine   = true,
            PrimaryLabel   = "Получить награду",
            OnPrimary      = FinishGame,
            PrimaryVisibleImmediately = false,
            CommitBestImmediately     = false,
        });
        _bonusSlider = _resultPanel.BuildBonusSlider("Ловкость", "% задания", out _agilityBonusTxt);
        _rewardTxt   = _resultPanel.InfoText;

        _agilityBonusTxt.text = _agility > 1
            ? $"Ловкость {_agility} · влияет на точность ползунка"
            : "Ловкость не прокачана";

        // ── показываем экран ──────────────────────────────────────────────────
        _rewardTxt.text = "";
        // Счёт (count-up до rawScore) и звёзды анимирует MiniGameResultPanel; после ползунка — UpdateScore

        // ── запускаем ползунок ────────────────────────────────────────────────
        if (_bonusSlider != null)
        {
            float agilityNorm = Mathf.Clamp01((_agility - 1) / 9f);
            _bonusSlider.Run(
                () => agilityNorm,
                _rawScore,
                (sliderScore, bonus) =>
                {
                    // Ползунок может только добавить бонус — штраф (bonus < 1) игнорируем.
                    // Итоговый счёт не опускается ниже rawScore.
                    int boosted = bonus >= 1f
                        ? Mathf.RoundToInt(_rawScore * bonus)
                        : _rawScore;
                    _finalScore = Mathf.Clamp(boosted, 0, 100);

                    // Обновляем счёт (+звёзды, +рекорд)
                    if (_resultPanel != null) _resultPanel.UpdateScore(_finalScore);

                    // Показываем бонус ловкости
                    if (bonus > 1.05f)
                        _agilityBonusTxt.text = $"Ловкость {_agility} · бонус ×{bonus:F2} ✓";
                    else
                        _agilityBonusTxt.text = _agility > 1
                            ? $"Ловкость {_agility} · без бонуса"
                            : "Ловкость не прокачана";

                    // Показываем награду
                    ApplyRewardDisplay(_finalScore);

                    Debug.Log($"[JumpGame] rawScore={_rawScore} bonus={bonus:F2} finalScore={_finalScore}");

                    // Показываем кнопку
                    if (_resultPanel != null) _resultPanel.SetPrimaryVisible(true);
                });
        }
        else
        {
            // Нет ползунка — агилити даёт небольшой фиксированный бонус
            int agilityBonus = (_agility - 1) * 2;
            _finalScore = Mathf.Clamp(_rawScore + agilityBonus, 0, 100);
            Debug.Log($"[JumpGame] rawScore={_rawScore} agilityBonus={agilityBonus} finalScore={_finalScore}");
            ApplyRewardDisplay(_finalScore);
            if (_resultPanel != null)
            {
                _resultPanel.UpdateScore(_finalScore);
                _resultPanel.SetPrimaryVisible(true);
            }
        }
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
            _rewardTxt.color = new Color(0.7f, 0.35f, 0.35f);
        }

        MiniGameJuice.Punch(_rewardTxt.transform, 0.2f, 0.3f);
        if (score >= 65) MiniGameJuice.Feedback(FeedbackType.Reward);
    }

    // Вызывается кнопкой «Получить награду» — только здесь стреляем ивент
    private void FinishGame() => _gameEvent?.OnGameEnded(_finalScore);

    // ══════════════════════════════════════════════════════════════════════════
    // UPDATE
    // ══════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (!_running) return;

        float dt = Time.deltaTime;
        _elapsed  += dt;
        _timeLeft -= dt;

        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f;
            _running  = false;
            UpdateHUD();
            EndGame();
            return;
        }

        HandleInput();
        UpdatePlayer(dt);
        ScrollBackground(dt);
        UpdateObstacles(dt);
        _nextSpawnIn -= dt;
        if (_nextSpawnIn <= 0f)
            SpawnObstacle();
        CheckCollisions();
        UpdateHUD();
    }

    // ── ввод ──────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        Vector2 currentPos = Input.touchCount > 0
            ? (Vector2)Input.GetTouch(0).position
            : (Vector2)Input.mousePosition;

        bool fingerDown = Input.touchCount > 0
            ? Input.GetTouch(0).phase == TouchPhase.Began
            : Input.GetMouseButtonDown(0);

        bool fingerMoving = Input.touchCount > 0
            ? (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(0).phase == TouchPhase.Stationary)
            : Input.GetMouseButton(0);

        bool fingerUp = Input.touchCount > 0
            ? Input.GetTouch(0).phase == TouchPhase.Ended
            : Input.GetMouseButtonUp(0);

        bool keyDown = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        bool keyUp   = Input.GetKeyUp(KeyCode.S)   || Input.GetKeyUp(KeyCode.DownArrow);

        // ── удержание ─────────────────────────────────────────────────────────
        if (fingerDown || keyDown) _fingerHeld = true;
        if (fingerUp   || keyUp)   _fingerHeld = false;

        // ── начало касания — только запоминаем позицию, не прыгаем ───────────
        // Прыжок откладываем до fingerUp (чтобы дать шанс свайпу перехватить)
        if (fingerDown)
        {
            _touchStartPos = currentPos;
            _touchTracking = true;
        }

        // ── детект свайпа вниз (тач/мышь) ────────────────────────────────────
        if (_touchTracking && fingerMoving)
        {
            float deltaY = currentPos.y - _touchStartPos.y;
            if (deltaY < -SwipeDownThreshold)
            {
                _touchTracking = false; // помечаем: свайп перехватил жест
                HandleSwipeDown();
                return;
            }
        }

        // ── свайп вниз (клавиатура) ───────────────────────────────────────────
        if (keyDown)
        {
            HandleSwipeDown();
            return;
        }

        // ── отпускание клавиши ────────────────────────────────────────────────
        if (keyUp && _isCrouching && _onGround)
            StopCrouch();

        // ── отпускание пальца/кнопки мыши ────────────────────────────────────
        if (fingerUp && !_isDead)
        {
            bool wasTap = _touchTracking; // свайп НЕ был зафиксирован = это тап
            _touchTracking = false;

            if (wasTap)
            {
                // Обычный тап — прыжок
                if (_onGround)
                {
                    _velY = JumpVelocity;
                    _onGround = false;
                    _hasDoubleJump = true;
                    if (_isCrouching) StopCrouch();
                    PunchPlayer();
                    return;
                }
                else if (_hasDoubleJump)
                {
                    _velY = DoubleJumpVel;
                    _hasDoubleJump = false;
                    SpawnDoubleJumpFX();
                    return;
                }
            }

            // Свайп был зафиксирован — при отпускании встаём из приседания
            if (_isCrouching && _onGround)
                StopCrouch();
        }
        else if (fingerUp)
        {
            _touchTracking = false;
        }
    }

    private void HandleSwipeDown()
    {
        if (_isDead) return;

        if (!_onGround)
        {
            // В воздухе — резкий удар вниз
            _velY = -SlamVelocity;
            _isSlamming = true;
            // Визуально вытягиваем вниз
            _crouchTween?.Kill();
            _crouchTween = _playerRect.DOScaleY(1.5f, 0.05f).SetUpdate(true);
        }
        else
        {
            // На земле — присесть
            StartCrouch();
        }
    }

    // ── приседание ────────────────────────────────────────────────────────────

    private void StartCrouch()
    {
        if (_isCrouching) return;
        _isCrouching = true;
        _crouchTween?.Kill();
        // Сплющиваем: уменьшаем Y, расширяем X
        _crouchTween = DOTween.Sequence()
            .Append(_playerRect.DOScaleY(CrouchScaleY, CrouchAnimDur).SetUpdate(true))
            .Join(_playerRect.DOScaleX(CrouchScaleX, CrouchAnimDur).SetUpdate(true))
            .SetUpdate(true);
    }

    private void StopCrouch()
    {
        if (!_isCrouching) return;
        _isCrouching = false;
        _crouchTween?.Kill();
        _crouchTween = DOTween.Sequence()
            .Append(_playerRect.DOScaleY(1f, CrouchAnimDur).SetUpdate(true))
            .Join(_playerRect.DOScaleX(1f, CrouchAnimDur).SetUpdate(true))
            .SetUpdate(true);
    }

    // ── физика игрока ─────────────────────────────────────────────────────────

    private void UpdatePlayer(float dt)
    {
        if (!_onGround)
        {
            _velY += Gravity * dt;
            float newY = _playerRect.anchoredPosition.y + _velY * dt;

            if (newY <= GroundY)
            {
                newY  = GroundY;
                _velY = 0f;
                _onGround = true;

                if (_isSlamming)
                {
                    _isSlamming = false;
                    // Приседаем только если палец / кнопка ещё зажата
                    if (_fingerHeld)
                        StartCrouch();
                    SpawnSlamFX();
                }
            }

            _playerRect.anchoredPosition = new Vector2(PlayerX, newY);
        }

        // Squash/stretch когда не в приседании
        if (!_isCrouching && !_isSlamming)
        {
            float stretchY = _onGround ? 1f : Mathf.Clamp(1f + _velY / 3000f, 0.7f, 1.4f);
            float stretchX = _onGround ? 1f : 1f / Mathf.Max(stretchY, 0.5f);
            _playerRect.localScale = new Vector3(stretchX, stretchY, 1f);
        }
    }

    private void ScrollBackground(float dt)
    {
        float speed = CurrentSpeed();
        for (int i = 0; i < _bgLayers.Length; i++)
        {
            var rt  = _bgLayers[i];
            var pos = rt.anchoredPosition;
            pos.x -= speed * _bgSpeeds[i] * dt;
            if (pos.x < -_bgWidths[i] * 0.5f) pos.x += _bgWidths[i];
            rt.anchoredPosition = pos;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ПРЕПЯТСТВИЯ / МОНЕТЫ
    // ══════════════════════════════════════════════════════════════════════════

    private void SpawnObstacle()
    {
        _nextSpawnIn = Random.Range(SpawnMinGap, SpawnMaxGap);

        float r = Random.value;
        if      (r < 0.25f) SpawnCoin();
        else if (r < 0.50f) SpawnBeam();   // балка сверху — нужно приседать
        else                SpawnRock();
    }

    private void SpawnRock()
    {
        float h = Random.value < 0.35f ? 90f : 55f;
        float w = 42f;

        var go = new GameObject("Rock");
        go.transform.SetParent(_laneContainer, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(ObstacleStartX, GroundY + h * 0.5f);

        var img = go.AddComponent<Image>();
        img.sprite = (_cfg != null && _cfg.rockSprite != null) ? _cfg.rockSprite : _whiteSquare;
        img.color  = _cfg != null ? _cfg.rockColor : new Color(0.55f, 0.32f, 0.18f);

        AddStripe(go.transform, w, h);

        // Визуально дотягиваем камень до земли (на хитбокс не влияет)
        var footGo = new GameObject("Foot");
        footGo.transform.SetParent(go.transform, false);
        var foot = footGo.AddComponent<RectTransform>();
        foot.anchorMin = new Vector2(0, 0); foot.anchorMax = new Vector2(1, 0);
        foot.pivot = new Vector2(0.5f, 1f);
        foot.sizeDelta = new Vector2(0, PlayerH * 0.5f);
        foot.anchoredPosition = Vector2.zero;
        var footImg = footGo.AddComponent<Image>();
        footImg.sprite = img.sprite;
        footImg.color = img.color;
        footImg.raycastTarget = false;

        _lanes.Add(new Lane { rect = rt, isCoin = false, isCeiling = false, passed = false, wasHit = false });
    }

    private void SpawnBeam()
    {
        // Горизонтальная балка на высоте, которую можно миновать только присев.
        // Рисуем балку + две вертикальные опоры с краёв.
        var go = new GameObject("Beam");
        go.transform.SetParent(_laneContainer, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(BeamW, BeamH);
        rt.anchoredPosition = new Vector2(ObstacleStartX, GroundY + BeamCenterOffset);

        var img = go.AddComponent<Image>();
        img.sprite = (_cfg != null && _cfg.beamSprite != null) ? _cfg.beamSprite : _whiteSquare;
        img.color  = _cfg != null ? _cfg.beamColor : new Color(0.7f, 0.25f, 0.25f);

        // Опоры по бокам (декоративные)
        foreach (int side in new[] { -1, 1 })
        {
            var pole = new GameObject("Pole");
            pole.transform.SetParent(go.transform, false);
            var prt = pole.AddComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            float poleH = BeamCenterOffset; // от балки до земли
            prt.sizeDelta = new Vector2(8f, poleH);
            prt.anchoredPosition = new Vector2(side * (BeamW * 0.5f - 4f), -poleH * 0.5f - BeamH * 0.5f);
            var pimg = pole.AddComponent<Image>();
            pimg.color = new Color(0.55f, 0.18f, 0.18f);
            pimg.raycastTarget = false;
        }

        _lanes.Add(new Lane { rect = rt, isCoin = false, isCeiling = true, passed = false, wasHit = false });
    }

    private void SpawnCoin()
    {
        float coinY = GroundY + Random.Range(60f, 160f);

        var go = new GameObject("Coin");
        go.transform.SetParent(_laneContainer, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(34f, 34f);
        rt.anchoredPosition = new Vector2(ObstacleStartX, coinY);

        var img = go.AddComponent<Image>();
        img.sprite = (_cfg != null && _cfg.coinSprite != null) ? _cfg.coinSprite : _circleSprite;
        img.color  = _cfg != null ? _cfg.coinColor : new Color(1f, 0.85f, 0.1f);

        rt.DORotate(new Vector3(0, 0, -360f), 0.9f, RotateMode.FastBeyond360)
          .SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetUpdate(true);

        _lanes.Add(new Lane { rect = rt, isCoin = true, isCeiling = false, passed = false, wasHit = false });
    }

    private void AddStripe(Transform parent, float w, float h)
    {
        var s = new GameObject("Stripe");
        s.transform.SetParent(parent, false);
        var rt = s.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w * 0.35f, h * 0.6f);
        rt.anchoredPosition = new Vector2(-5f, 5f);
        var img = s.AddComponent<Image>();
        img.color = new Color(0.38f, 0.20f, 0.08f);
        img.raycastTarget = false;
    }

    private void UpdateObstacles(float dt)
    {
        float speed = CurrentSpeed();
        var toRemove = new List<int>();

        for (int i = 0; i < _lanes.Count; i++)
        {
            var lane = _lanes[i];
            if (lane.rect == null) { toRemove.Add(i); continue; }

            var pos = lane.rect.anchoredPosition;
            pos.x -= speed * dt;
            lane.rect.anchoredPosition = pos;

            // Препятствие миновано без столкновения
            if (!lane.passed && pos.x < PlayerX - 40f)
            {
                lane.passed = true;
                if (!lane.isCoin && !lane.wasHit)
                {
                    // Успешно обошли — плюс очки
                    AddScore(PointsClear, lane.rect.anchoredPosition + Vector2.up * 60f,
                        MiniGameTheme.Success);
                    _combo++;
                    OnComboStep(lane.rect.anchoredPosition + Vector2.up * 30f, MiniGameTheme.Success, 6, false);
                }
            }

            if (pos.x < ObstacleKillX)
            {
                Destroy(lane.rect.gameObject);
                toRemove.Add(i);
            }

            _lanes[i] = lane;
        }

        for (int i = toRemove.Count - 1; i >= 0; i--)
            _lanes.RemoveAt(toRemove[i]);
    }

    private void CheckCollisions()
    {
        if (_isDead) return;

        // Хитбокс игрока зависит от приседания
        float scaleY = _isCrouching ? CrouchScaleY : 1f;
        float scaleX = _isCrouching ? CrouchScaleX : 1f;
        float hw = PlayerW * scaleX * 0.42f;
        float hh = PlayerH * scaleY * 0.42f;

        Vector2 playerCenter = _playerRect.anchoredPosition;
        Rect playerRect = new Rect(playerCenter.x - hw, playerCenter.y - hh, hw * 2f, hh * 2f);

        for (int i = 0; i < _lanes.Count; i++)
        {
            var lane = _lanes[i];
            if (lane.rect == null || lane.passed || lane.wasHit) continue;

            Vector2 sz  = lane.rect.sizeDelta;
            Rect obsRect = new Rect(
                lane.rect.anchoredPosition.x - sz.x * 0.45f,
                lane.rect.anchoredPosition.y - sz.y * 0.45f,
                sz.x * 0.9f, sz.y * 0.9f);

            if (!playerRect.Overlaps(obsRect)) continue;

            if (lane.isCoin)
            {
                CollectCoin(i);
                return;
            }
            else
            {
                HitObstacle(i);
                return;
            }
        }
    }

    private void CollectCoin(int index)
    {
        var lane = _lanes[index];
        Vector2 pos = lane.rect.anchoredPosition;
        DOTween.Kill(lane.rect);
        Destroy(lane.rect.gameObject);
        var l2 = _lanes[index]; l2.passed = true; _lanes[index] = l2;
        AddScore(PointsPerCoin, pos + Vector2.up * 20f, MiniGameTheme.Warning);
        _combo++;
        _coinsCollected++;
        OnComboStep(pos, MiniGameTheme.Warning, 12, true);
    }

    // Частицы + звук за успешное действие; каждые 5 комбо — «PERFECT»-акцент
    private void OnComboStep(Vector2 pos, Color col, int particles, bool hitSound)
    {
        MiniGameJuice.Burst(_laneContainer, pos, col, particles, hitSound ? 80f : 55f, hitSound ? 16f : 11f, 0.45f);

        if (_combo > 0 && _combo % 5 == 0)
        {
            MiniGameJuice.Feedback(FeedbackType.Perfect);
            MiniGameJuice.Shake(_bgRect, 5f, 0.15f);
        }
        else if (hitSound)
        {
            MiniGameJuice.Feedback(FeedbackType.Hit);
        }
    }

    private void HitObstacle(int index)
    {
        // Помечаем препятствие как ударенное — оно не даст +40 при прохождении
        var lane = _lanes[index];
        lane.wasHit = true;
        _lanes[index] = lane;

        _isDead = true;
        _combo  = 0;

        // Штраф к счёту
        SubtractScore(PointsHitPenalty, _playerRect.anchoredPosition + Vector2.up * 40f);

        _playerImg.DOColor(new Color(1f, 0.2f, 0.2f), 0.08f).SetUpdate(true)
            .OnComplete(() => _playerImg.DOColor(new Color(0.35f, 0.75f, 1f), 0.3f).SetUpdate(true));
        _playerRect.DOShakeAnchorPos(0.25f, 18f, 25).SetUpdate(true);

        _obstacleHits++;
        MiniGameJuice.Feedback(FeedbackType.Miss);
        MiniGameJuice.Shake(_bgRect, 14f, 0.3f);
        MiniGameJuice.Flash(_rootRect, MiniGameTheme.Danger, 0.25f, 0.35f);

        if (_onGround)
        {
            _velY = 300f;
            _onGround = false;
        }

        DOVirtual.DelayedCall(0.5f, () => { _isDead = false; }, true);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // СЧЁТ / HUD
    // ══════════════════════════════════════════════════════════════════════════

    private void AddScore(int pts, Vector2 worldPos, Color col)
    {
        float multiplier = 1f + _combo * 0.1f;
        int gained = Mathf.RoundToInt(pts * multiplier);
        _score += gained;
        SpawnScorePopup($"+{gained}", worldPos, col);
    }

    private void SubtractScore(int pts, Vector2 worldPos)
    {
        _score -= pts;
        SpawnScorePopup($"-{pts}", worldPos, new Color(1f, 0.3f, 0.3f));
    }

    private void SpawnScorePopup(string text, Vector2 pos, Color col)
    {
        var go = new GameObject("Popup");
        go.transform.SetParent(_laneContainer, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(120f, 40f);
        rt.anchoredPosition = pos;

        var cg  = go.AddComponent<CanvasGroup>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = col;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        rt.DOAnchorPosY(pos.y + 70f, 0.7f).SetEase(Ease.OutCubic).SetUpdate(true);
        cg.DOFade(0f, 0.7f).SetDelay(0.2f).SetUpdate(true)
            .OnComplete(() => { if (go != null) Destroy(go); });
    }

    private void UpdateHUD()
    {
        _scoreTxt.text  = _score.ToString();
        _timerTxt.text  = Mathf.CeilToInt(_timeLeft).ToString();
        _timerTxt.color = _timeLeft <= 5f ? new Color(1f, 0.3f, 0.3f) : MiniGameTheme.TextPrimary;

        _maxCombo = Mathf.Max(_maxCombo, _combo);
        if (_combo >= 2)
        {
            _comboTxt.text = $"x{_combo} COMBO!";
            // punch только при изменении комбо (раньше punch запускался каждый кадр)
            if (_combo != _lastHudCombo)
                MiniGameJuice.Punch(_comboTxt.transform, 0.25f, 0.25f);
        }
        else
        {
            _comboTxt.text = "";
        }
        _lastHudCombo = _combo;

        // последние 5 секунд — тик таймера
        int sec = Mathf.CeilToInt(_timeLeft);
        if (_running && sec != _lastTimerSecond)
        {
            if (_lastTimerSecond > 0 && sec <= 5 && sec > 0)
            {
                MiniGameJuice.Punch(_timerTxt.transform, 0.3f, 0.25f);
                MiniGameJuice.Feedback(FeedbackType.Tap);
            }
            _lastTimerSecond = sec;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // FX
    // ══════════════════════════════════════════════════════════════════════════

    private void PunchPlayer()
    {
        DOTween.Kill(_playerRect, false);
        _playerRect.DOPunchScale(new Vector3(0.3f, -0.2f, 0f), 0.25f, 5).SetUpdate(true);
    }

    private void SpawnDoubleJumpFX()
    {
        var go = new GameObject("DJFx");
        go.transform.SetParent(_laneContainer, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(60f, 60f);
        rt.anchoredPosition = _playerRect.anchoredPosition;
        var img = go.AddComponent<Image>();
        img.sprite = _circleSprite;
        img.color  = new Color(0.4f, 0.85f, 1f, 0.7f);
        var cg = go.AddComponent<CanvasGroup>();

        rt.DOScale(2.2f, 0.35f).SetEase(Ease.OutCubic).SetUpdate(true);
        cg.DOFade(0f, 0.35f).SetUpdate(true)
            .OnComplete(() => { if (go != null) Destroy(go); });
    }

    private void SpawnSlamFX()
    {
        // Горизонтальная «ударная волна» при падении на землю
        var go = new GameObject("SlamFx");
        go.transform.SetParent(_laneContainer, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(20f, 14f);
        rt.anchoredPosition = new Vector2(PlayerX, GroundY - 4f);
        var img = go.AddComponent<Image>();
        img.sprite = _whiteSquare;
        img.color  = new Color(0.9f, 0.9f, 1f, 0.7f);
        var cg = go.AddComponent<CanvasGroup>();

        rt.DOSizeDelta(new Vector2(180f, 14f), 0.2f).SetEase(Ease.OutCubic).SetUpdate(true);

        MiniGameJuice.Shake(_bgRect, 6f, 0.15f);
        MiniGameJuice.Haptic(HapticType.Medium);
        cg.DOFade(0f, 0.25f).SetUpdate(true)
            .OnComplete(() => { if (go != null) Destroy(go); });
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private float CurrentSpeed() => BaseSpeed + Mathf.Floor(_elapsed / 5f) * SpeedRampPer5s;

    private RectTransform MakePanel(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        var img = go.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    private TextMeshProUGUI MakeText(RectTransform parent, string name, string text,
        int size, Vector2 pos, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400f, 60f);
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
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.sprite = _whiteSquare;
        img.color  = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cols = btn.colors;
        cols.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        cols.pressedColor     = Color.Lerp(color, Color.black, 0.15f);
        btn.colors = cols;

        var lgo = new GameObject("Label");
        lgo.transform.SetParent(go.transform, false);
        var lrt = lgo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var tmp = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }

    private GameObject MakeFullPanel(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;  rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<CanvasGroup>();
        return go;
    }

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
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            float cx = sz * 0.5f, r = cx;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float d = Mathf.Sqrt((x - cx + 0.5f) * (x - cx + 0.5f) + (y - cx + 0.5f) * (y - cx + 0.5f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((r - d) / 1.5f)));
                }
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f);
        }
    }

    private void OnDestroy()
    {
        _crouchTween?.Kill();
        DOTween.Kill(transform);
        foreach (var l in _lanes)
            if (l.rect != null) DOTween.Kill(l.rect);
    }
}
