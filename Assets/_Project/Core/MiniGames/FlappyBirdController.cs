using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ActiveGrad.MiniGames;

public class FlappyBirdController : MonoBehaviour
{
    // ── Физика ──────────────────────────────────────────────────────
    private const float Gravity        = -1500f;
    private const float JumpForce      =  500f;

    // ── Сложность ───────────────────────────────────────────────────
    private const float PipeSpeedStart =  230f;
    private const float PipeSpeedMax   =  460f;
    private const float PipeGapStart   =  235f;
    private const float PipeGapMin     =  148f;
    private const float PipeWidth      =   68f;
    private const float SpawnInterval  =  1.85f;
    private const float PipesPerLevel  =    5f; // труб до следующего уровня

    // ── Остальное ───────────────────────────────────────────────────
    private const float HitboxShrink   =   9f;
    private const float GroundHeight   =  56f;
    private const string BestScoreKey  = "FlappyBestScore";

    private enum State { Start, Countdown, Playing, Dying, Dead }

    // ── Зависимости ─────────────────────────────────────────────────
    private FlappyBirdGameEvent _gameEvent;
    private FlappyBirdUI        _ui;
    private UserDataService     _userDataService;

    // ── Состояние ───────────────────────────────────────────────────
    private State _state = State.Start;
    private float _areaH, _areaW;
    private float _birdVelocity;
    private int   _score, _finalScore, _bestScore;
    private float _pipeTimer;
    private float _curSpeed, _curGap;
    private int   _diffLevel;

    // ── Shake ───────────────────────────────────────────────────────
    private float _shakeAmp, _shakeDecay;

    // ── Земля ───────────────────────────────────────────────────────
    private RectTransform _ground1, _ground2;
    private float         _groundW;

    // ── Трубы ───────────────────────────────────────────────────────
    private class PipePair
    {
        public RectTransform top, bottom;
        public Image topImg, bottomImg;
        public float gapTop, gapBot;
        public bool scored;
    }
    private readonly List<PipePair> _pipes = new List<PipePair>();

    // ════════════════════════════════════════════════════════════════
    public void Initialize(FlappyBirdGameEvent gameEvent, FlappyBirdUI ui, UserDataService userDataService)
    {
        _gameEvent       = gameEvent;
        _ui              = ui;
        _userDataService = userDataService;
        _bestScore       = PlayerPrefs.GetInt(BestScoreKey, 0);

        _ui.StartButton?.onClick.AddListener(OnStartClicked);
        _ui.CloseButton?.onClick.AddListener(() => _gameEvent?.CloseGame());
        _ui.RestartButton?.onClick.AddListener(RestartGame);
        _ui.FinishButton?.onClick.AddListener(FinishGame);

        int agility = _userDataService?.Agility ?? 1;
        _ui.SetSkillInfo("Ловкость", agility);
        _ui.SetBestScore(_bestScore);
        _ui.ShowScreen(FlappyBirdScreen.Start);
    }

    // ── Старт ───────────────────────────────────────────────────────
    private void OnStartClicked()
    {
        var rect = _ui.PipesContainer.rect;
        _areaH = rect.height;
        _areaW = rect.width;

        ResetState();
        BuildGround();
        _ui.ShowScreen(FlappyBirdScreen.Game);
        StartCoroutine(CountdownRoutine());
    }

    private void RestartGame()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        ClearPipes();
        OnStartClicked();
    }

    private void ResetState()
    {
        _birdVelocity = 0f;
        _score        = 0;
        _pipeTimer    = SpawnInterval;
        _diffLevel    = 0;
        _curSpeed     = PipeSpeedStart;
        _curGap       = PipeGapStart;
        _shakeAmp     = 0f;
        ClearPipes();

        if (_ui.Bird)
        {
            _ui.Bird.anchoredPosition = new Vector2(-_areaW * 0.25f, 0f);
            _ui.Bird.localRotation    = Quaternion.identity;
            _ui.Bird.localScale       = Vector3.one;
        }
        _ui.SetScore(0);
        _ui.SetCountdown("");
    }

    // ── Обратный отсчёт ─────────────────────────────────────────────
    private IEnumerator CountdownRoutine()
    {
        _state = State.Countdown;

        var steps = new[] { "3", "2", "1", "GO!" };
        var waits = new[] { 0.65f, 0.65f, 0.65f, 0.35f };

        for (int i = 0; i < steps.Length; i++)
        {
            _ui.SetCountdown(steps[i]);
            if (_ui.CountdownText)
                StartCoroutine(PulseScale(_ui.CountdownText.transform, 1.5f, 0.18f));
            yield return new WaitForSecondsRealtime(waits[i]);

            // Птица слегка подпрыгивает при каждом числе
            if (i < 3) _birdVelocity = 180f;
            yield return null;
        }

        _ui.SetCountdown("");
        _state = State.Playing;
        _pipeTimer = 1.0f; // первая труба чуть позже
    }

    // ── Update ──────────────────────────────────────────────────────
    private void Update()
    {
        switch (_state)
        {
            case State.Countdown:
                AnimateBirdIdle();
                break;

            case State.Playing:
                HandleInput();
                UpdateBird();
                UpdatePipes();
                UpdateGround();
                CheckCollisions();
                UpdateShake();
                break;

            case State.Dying:
                UpdateDyingBird();
                UpdateShake();
                break;
        }
    }

    // ── Ввод ────────────────────────────────────────────────────────
    private void HandleInput()
    {
        bool jumped = Input.GetMouseButtonDown(0);
        if (!jumped && Input.touchCount > 0)
            jumped = Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began;

        if (jumped) Jump();
    }

    private void Jump()
    {
        _birdVelocity = JumpForce;
        StartCoroutine(JumpSquash());
    }

    private IEnumerator JumpSquash()
    {
        // Мгновенный сквош → стретч при прыжке
        float t = 0f;
        while (t < 0.08f) { t += Time.deltaTime; _ui.Bird.localScale = Vector3.Lerp(new Vector3(1.25f, 0.75f, 1f), Vector3.one, t / 0.08f); yield return null; }
        t = 0f;
        while (t < 0.10f) { t += Time.deltaTime; _ui.Bird.localScale = Vector3.Lerp(new Vector3(0.85f, 1.25f, 1f), Vector3.one, t / 0.10f); yield return null; }
        _ui.Bird.localScale = Vector3.one;
    }

    // ── Птица в ожидании ────────────────────────────────────────────
    private void AnimateBirdIdle()
    {
        _birdVelocity += Gravity * Time.deltaTime * 0.4f;
        var pos  = _ui.Bird.anchoredPosition;
        pos.y   += _birdVelocity * Time.deltaTime;
        _ui.Bird.anchoredPosition = pos;

        // Мягкое ограничение: не даём улететь далеко
        if (pos.y > 40f)  _birdVelocity = Mathf.Min(_birdVelocity, -50f);
        if (pos.y < -40f) _birdVelocity = Mathf.Max(_birdVelocity,  50f);

        _ui.Bird.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(_birdVelocity * 0.04f, -15f, 15f));
    }

    // ── Физика птицы (геймплей) ──────────────────────────────────────
    private void UpdateBird()
    {
        _birdVelocity += Gravity * Time.deltaTime;

        var pos  = _ui.Bird.anchoredPosition;
        pos.y   += _birdVelocity * Time.deltaTime;
        _ui.Bird.anchoredPosition = pos;

        float angle = Mathf.Clamp(_birdVelocity * 0.05f, -35f, 28f);
        _ui.Bird.localRotation = Quaternion.Euler(0f, 0f, angle);

        // Squash/stretch по скорости (вертикальная)
        float vy     = _birdVelocity / JumpForce;
        float scaleY = 1f + vy * 0.18f;
        float scaleX = 1f / Mathf.Max(0.01f, scaleY); // объём сохраняется
        scaleY = Mathf.Clamp(scaleY, 0.82f, 1.25f);
        scaleX = Mathf.Clamp(scaleX, 0.82f, 1.12f);
        _ui.Bird.localScale = new Vector3(scaleX, scaleY, 1f);

        // Границы: потолок и земля
        float killY = -_areaH * 0.5f + GroundHeight;
        if (pos.y < killY || pos.y > _areaH * 0.5f)
            TriggerDeath();
    }

    // ── Трубы ───────────────────────────────────────────────────────
    private void UpdatePipes()
    {
        _pipeTimer -= Time.deltaTime;
        if (_pipeTimer <= 0f) { SpawnPipePair(); _pipeTimer = SpawnInterval; }

        float birdX  = _ui.Bird.anchoredPosition.x;
        float halfW  = _areaW * 0.5f;

        for (int i = _pipes.Count - 1; i >= 0; i--)
        {
            var pair = _pipes[i];
            float nx = pair.top.anchoredPosition.x - _curSpeed * Time.deltaTime;

            pair.top.anchoredPosition    = new Vector2(nx, pair.gapTop);
            pair.bottom.anchoredPosition = new Vector2(nx, pair.gapBot);

            if (!pair.scored && nx < birdX)
            {
                pair.scored = true;
                _score++;
                _ui.SetScore(_score);
                StartCoroutine(PulseScale(_ui.ScoreText.transform, 1.35f, 0.13f));
                SpawnScorePopup(nx);
                UpdateDifficulty();
            }

            if (nx < -halfW - PipeWidth)
            {
                Destroy(pair.top.gameObject);
                Destroy(pair.bottom.gameObject);
                _pipes.RemoveAt(i);
            }
        }
    }

    private void SpawnPipePair()
    {
        float halfH    = _areaH * 0.5f;
        float halfGap  = _curGap * 0.5f;
        float margin   = 65f + GroundHeight;
        float gapCY    = Random.Range(-halfH + halfGap + margin, halfH - halfGap - 40f);
        float spawnX   = _areaW * 0.5f + PipeWidth;
        Color c        = PipeColor();

        var pair = new PipePair
        {
            gapTop = gapCY + halfGap,
            gapBot = gapCY - halfGap,
            top    = MakePipeRect("TopPipe",    spawnX, gapCY + halfGap, true,  _areaH, c),
            bottom = MakePipeRect("BottomPipe", spawnX, gapCY - halfGap, false, _areaH, c),
        };
        pair.topImg    = pair.top.GetComponent<Image>();
        pair.bottomImg = pair.bottom.GetComponent<Image>();
        _pipes.Add(pair);
    }

    // Pivot .y=0 → anchoredPosition.y = нижний край (верх просвета)
    // Pivot .y=1 → anchoredPosition.y = верхний край (низ просвета)
    private RectTransform MakePipeRect(string n, float x, float y, bool bottomPivot, float h, Color c)
    {
        var go = new GameObject(n);
        go.transform.SetParent(_ui.PipesContainer, false);
        go.transform.SetAsFirstSibling();
        var img   = go.AddComponent<Image>();
        img.color = c;
        // Чёрная рамка сверху/снизу трубы
        // (можно добавить дочерний rect, но пока просто цвет)

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, bottomPivot ? 0f : 1f);
        rt.sizeDelta = new Vector2(PipeWidth, h);
        rt.anchoredPosition = new Vector2(x, y);
        return rt;
    }

    private void ClearPipes()
    {
        foreach (var p in _pipes)
        {
            if (p.top)    Destroy(p.top.gameObject);
            if (p.bottom) Destroy(p.bottom.gameObject);
        }
        _pipes.Clear();
    }

    // ── Сложность ───────────────────────────────────────────────────
    private void UpdateDifficulty()
    {
        int lvl = Mathf.FloorToInt(_score / PipesPerLevel);
        if (lvl <= _diffLevel) return;
        _diffLevel = lvl;
        _curSpeed  = Mathf.Min(PipeSpeedMax, PipeSpeedStart + _diffLevel * 32f);
        _curGap    = Mathf.Max(PipeGapMin,   PipeGapStart   - _diffLevel * 17f);
    }

    // Цвет труб: зелёный → жёлтый → оранжевый → красный
    private Color PipeColor()
    {
        float t = Mathf.Clamp01(_diffLevel / 4f);
        return Color.Lerp(new Color(0.22f, 0.72f, 0.28f), new Color(0.85f, 0.18f, 0.18f), t);
    }

    // ── Земля ───────────────────────────────────────────────────────
    private void BuildGround()
    {
        if (_ground1) Destroy(_ground1.gameObject);
        if (_ground2) Destroy(_ground2.gameObject);

        _groundW = _areaW + 6f;
        float y  = -_areaH * 0.5f + GroundHeight * 0.5f; // центр прямоугольника земли

        _ground1 = MakeGroundRect(0f,       y);
        _ground2 = MakeGroundRect(_groundW, y);

        // Земля поверх труб, но под птицей
        _ground1.SetSiblingIndex(_ui.Bird.GetSiblingIndex());
        _ground2.SetSiblingIndex(_ui.Bird.GetSiblingIndex());
        _ui.Bird.SetAsLastSibling();
    }

    private RectTransform MakeGroundRect(float x, float y)
    {
        var go = new GameObject("Ground");
        go.transform.SetParent(_ui.PipesContainer, false);
        var img   = go.AddComponent<Image>();
        img.color = new Color(0.36f, 0.26f, 0.12f);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(_groundW, GroundHeight);
        rt.anchoredPosition = new Vector2(x, y);
        return rt;
    }

    private void UpdateGround()
    {
        if (!_ground1) return;
        float dx = _curSpeed * Time.deltaTime;

        var p1 = _ground1.anchoredPosition; p1.x -= dx; _ground1.anchoredPosition = p1;
        var p2 = _ground2.anchoredPosition; p2.x -= dx; _ground2.anchoredPosition = p2;

        if (p1.x < -_groundW) { p1.x = p2.x + _groundW - 2f; _ground1.anchoredPosition = p1; }
        if (p2.x < -_groundW) { p2.x = p1.x + _groundW - 2f; _ground2.anchoredPosition = p2; }
    }

    // ── Коллизии ────────────────────────────────────────────────────
    private void CheckCollisions()
    {
        float s  = HitboxShrink;
        var   b  = _ui.Bird;
        float bL = b.anchoredPosition.x - b.sizeDelta.x * 0.5f + s;
        float bR = b.anchoredPosition.x + b.sizeDelta.x * 0.5f - s;
        float bT = b.anchoredPosition.y + b.sizeDelta.y * 0.5f - s;
        float bB = b.anchoredPosition.y - b.sizeDelta.y * 0.5f + s;

        foreach (var pair in _pipes)
        {
            float px = pair.top.anchoredPosition.x;
            if (bR < px - PipeWidth * 0.5f || bL > px + PipeWidth * 0.5f) continue;
            if (bT > pair.gapTop || bB < pair.gapBot) { TriggerDeath(); return; }
        }
    }

    // ── Смерть ──────────────────────────────────────────────────────
    private void TriggerDeath()
    {
        if (_state == State.Dying || _state == State.Dead) return;
        _state        = State.Dying;
        _birdVelocity = 220f; // подброс перед падением
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        StartCoroutine(Flash(new Color(1f, 0.15f, 0.15f, 0.55f)));
        _shakeAmp   = 22f;
        _shakeDecay = 70f;

        Time.timeScale = 0.18f;
        yield return new WaitForSecondsRealtime(0.55f);
        Time.timeScale = 1f;

        yield return new WaitForSecondsRealtime(0.25f);
        _state = State.Dead;
        ShowEndScreen();
    }

    private void UpdateDyingBird()
    {
        _birdVelocity        += Gravity * Time.deltaTime;
        var pos               = _ui.Bird.anchoredPosition;
        pos.y                += _birdVelocity * Time.deltaTime;
        _ui.Bird.anchoredPosition = pos;
        _ui.Bird.Rotate(0f, 0f, -720f * Time.deltaTime);
    }

    // ── Flash ───────────────────────────────────────────────────────
    private IEnumerator Flash(Color c)
    {
        if (!_ui.FlashOverlay) yield break;
        _ui.FlashOverlay.color = c;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            var col = c; col.a = Mathf.Lerp(c.a, 0f, t / 0.35f);
            _ui.FlashOverlay.color = col;
            yield return null;
        }
        _ui.FlashOverlay.color = Color.clear;
    }

    // ── Shake ───────────────────────────────────────────────────────
    private void UpdateShake()
    {
        if (_shakeAmp <= 0f) return;
        _shakeAmp -= _shakeDecay * Time.unscaledDeltaTime;
        if (_shakeAmp <= 0f) { _shakeAmp = 0f; _ui.PipesContainer.anchoredPosition = Vector2.zero; return; }
        _ui.PipesContainer.anchoredPosition = new Vector2(
            Random.Range(-_shakeAmp, _shakeAmp),
            Random.Range(-_shakeAmp, _shakeAmp));
    }

    // ── +1 попап ────────────────────────────────────────────────────
    private void SpawnScorePopup(float pipeX)
    {
        var go = new GameObject("ScorePopup");
        go.transform.SetParent(_ui.ScoreText.transform.parent, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text      = "+1";
        txt.fontSize  = 36;
        txt.fontStyle = FontStyles.Bold;
        txt.color     = Color.yellow;
        txt.alignment = TextAlignmentOptions.Center;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(70f, 50f);
        rt.anchoredPosition = _ui.ScoreText.rectTransform.anchoredPosition + new Vector2(52f, 0f);

        StartCoroutine(FloatFade(go, txt));
    }

    private IEnumerator FloatFade(GameObject go, TMP_Text txt)
    {
        float dur = 0.55f, t = 0f;
        var rt = go.GetComponent<RectTransform>();
        var start = rt.anchoredPosition;
        while (t < dur)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = start + Vector2.up * (70f * t / dur);
            var c = txt.color; c.a = 1f - t / dur; txt.color = c;
            yield return null;
        }
        Destroy(go);
    }

    private IEnumerator PulseScale(Transform t, float peak, float dur)
    {
        float half = dur * 0.5f;
        for (float e = 0; e < half; e += Time.deltaTime)
        { t.localScale = Vector3.one * Mathf.Lerp(1f, peak, e / half); yield return null; }
        for (float e = 0; e < half; e += Time.deltaTime)
        { t.localScale = Vector3.one * Mathf.Lerp(peak, 1f, e / half); yield return null; }
        t.localScale = Vector3.one;
    }

    // ── Финал ───────────────────────────────────────────────────────
    private void ShowEndScreen()
    {
        if (_score > _bestScore)
        {
            _bestScore = _score;
            PlayerPrefs.SetInt(BestScoreKey, _bestScore);
            PlayerPrefs.Save();
        }

        int baseScore = Mathf.Min(100, Mathf.RoundToInt(_score / 15f * 100f));
        _ui.SetResult(_score, baseScore);
        _ui.SetBestScore(_bestScore);
        _ui.SetMedal(Medal(_score));
        _ui.ShowScreen(FlappyBirdScreen.End);

        var slider = _ui.BonusSliderComponent;
        if (slider != null)
        {
            float lvl = Mathf.Clamp01((_userDataService != null ? _userDataService.Agility : 1) - 1) / 9f;
            slider.Run(() => lvl, baseScore, (fs, _) => { _finalScore = fs; _ui.SetResult(_score, fs); });
        }
        else
        {
            _finalScore = baseScore;
            if (_ui.EndScreenButtonsContainer) _ui.EndScreenButtonsContainer.SetActive(true);
        }
    }

    private static string Medal(int p) =>
        p >= 20 ? "🥇 Золото" : p >= 10 ? "🥈 Серебро" : p >= 5 ? "🥉 Бронза" : "—";

    private void FinishGame() => _gameEvent?.OnGameEnded(_finalScore);

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (_ui == null) return;
        _ui.StartButton?.onClick.RemoveAllListeners();
        _ui.CloseButton?.onClick.RemoveAllListeners();
        _ui.RestartButton?.onClick.RemoveAllListeners();
        _ui.FinishButton?.onClick.RemoveAllListeners();
    }
}
