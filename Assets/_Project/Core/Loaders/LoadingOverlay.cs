using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Экран загрузки поверх всего (DontDestroyOnLoad, строится кодом).
/// Использование: LoadingOverlay.LoadScene("SampleScene"), yield return LoadingOverlay.LoadSceneWithOverlay(...),
/// await LoadingOverlay.LoadSceneWithOverlayAsync(...), либо вручную Show/SetProgress/HideAfterLoad.
/// </summary>
public class LoadingOverlay : MonoBehaviour
{
    private const float MinDisplayTime = 0.6f;
    private const float FadeInTime = 0.2f;
    private const float FadeOutTime = 0.4f;
    private const float TipInterval = 3.2f;
    private const float TipFade = 0.35f;
    private const float ProgressFillSpeed = 1.6f;   // доля бара в секунду

    private static readonly string[] Tips =
    {
        "Гуляй больше — получай больше наград",
        "Отмечай достопримечательности рядом",
        "Выполняй ежедневные квесты",
        "Каждый шаг приближает к новому уровню",
        "Загляни в магазин партнёров поблизости",
    };

    // Цвета экранов входа/регистрации
    private static readonly Color TextDark = new Color32(47, 79, 79, 255);
    private static readonly Color TextSoft = new Color32(62, 95, 74, 255);

    private static LoadingOverlay _instance;

    private Canvas _canvas;
    private CanvasGroup _group;
    private RectTransform _fill;
    private RectTransform _titleRect;
    private Image _glow;
    private TextMeshProUGUI _tip;

    private float _targetProgress;
    private float _displayProgress;
    private float _tipTimer;
    private int _tipIndex;
    private bool _isLoading;
    private Coroutine _fadeRoutine;
    private event Action LoadFinished;

    private static LoadingOverlay Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[LoadingOverlay]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<LoadingOverlay>();
                _instance.Build();
            }
            return _instance;
        }
    }

    public static bool IsLoading => _instance != null && _instance._isLoading;

    // ─────────────────────────── Public API ───────────────────────────

    public static void Show() => Instance.ShowInternal();

    public static void SetProgress(float progress)
    {
        var i = Instance;
        i._targetProgress = Mathf.Max(i._targetProgress, Mathf.Clamp01(progress));
    }

    /// <summary>Плавно скрыть (через кадр после активации сцены).</summary>
    public static void HideAfterLoad()
    {
        if (_instance == null)
            return;
        _instance.StartFade(_instance.HideRoutine());
    }

    /// <summary>Запустить загрузку сцены с оверлеем (корутина живёт на самом оверлее).</summary>
    public static void LoadScene(string sceneName, Action onActivated = null)
    {
        Instance.BeginLoad(sceneName, onActivated, null);
    }

    /// <summary>Корутина: завершается после активации сцены (продолжает работать, даже если вызывающий объект уничтожен).</summary>
    public static IEnumerator LoadSceneWithOverlay(string sceneName, Action onActivated = null)
    {
        var done = false;
        Instance.BeginLoad(sceneName, onActivated, () => done = true);
        while (!done)
            yield return null;
    }

    public static Task LoadSceneWithOverlayAsync(string sceneName, Action onActivated = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        Instance.BeginLoad(sceneName, onActivated, () => tcs.TrySetResult(true));
        return tcs.Task;
    }

    // ─────────────────────────── Loading ───────────────────────────

    private void BeginLoad(string sceneName, Action onActivated, Action onFinished)
    {
        if (onFinished != null)
            LoadFinished += onFinished;

        if (_isLoading)
        {
            Debug.LogWarning($"[LoadingOverlay] Load already in progress, ignoring request for '{sceneName}'");
            return;
        }

        StartCoroutine(LoadRoutine(sceneName, onActivated));
    }

    private IEnumerator LoadRoutine(string sceneName, Action onActivated)
    {
        _isLoading = true;
        _targetProgress = 0f;
        _displayProgress = 0f;
        UpdateFill();
        ShowInternal();

        // Даём оверлею отрисоваться до начала тяжёлой загрузки.
        yield return null;

        AsyncOperation op = null;
        try
        {
            op = SceneManager.LoadSceneAsync(sceneName);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        if (op == null)
        {
            Debug.LogError($"[LoadingOverlay] Failed to start loading scene '{sceneName}'");
            FinishLoad();
            StartFade(HideRoutine());
            yield break;
        }

        op.allowSceneActivation = false;
        var start = Time.unscaledTime;

        while (true)
        {
            _targetProgress = Mathf.Max(_targetProgress, Mathf.Clamp01(op.progress / 0.9f));
            if (op.progress >= 0.9f && _displayProgress >= 0.999f && Time.unscaledTime - start >= MinDisplayTime)
                break;
            yield return null;
        }

        _displayProgress = 1f;
        UpdateFill();
        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        try
        {
            onActivated?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        FinishLoad();
        yield return HideRoutine();
    }

    private void FinishLoad()
    {
        _isLoading = false;
        var callbacks = LoadFinished;
        LoadFinished = null;
        try
        {
            callbacks?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // ─────────────────────────── Show / Hide ───────────────────────────

    private void ShowInternal()
    {
        var wasHidden = !_canvas.enabled || _group.alpha < 0.01f;
        _canvas.enabled = true;
        _group.blocksRaycasts = true;

        if (wasHidden)
        {
            _tipIndex = UnityEngine.Random.Range(0, Tips.Length);
            _tip.text = Tips[_tipIndex];
            _tipTimer = 0f;
            if (!_isLoading)
            {
                _targetProgress = 0f;
                _displayProgress = 0f;
                UpdateFill();
            }
        }

        StartFade(FadeRoutine(1f, FadeInTime));
    }

    private IEnumerator HideRoutine()
    {
        yield return null; // +1 кадр после активации сцены
        yield return FadeRoutine(0f, FadeOutTime);
        if (!_isLoading)
        {
            _group.blocksRaycasts = false;
            _canvas.enabled = false;
        }
    }

    private void StartFade(IEnumerator routine)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(routine);
    }

    private IEnumerator FadeRoutine(float target, float duration)
    {
        var from = _group.alpha;
        var t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            var k = Mathf.Clamp01(t / duration);
            _group.alpha = Mathf.Lerp(from, target, k * k * (3f - 2f * k));
            yield return null;
        }
        _group.alpha = target;
    }

    // ─────────────────────────── Per-frame ───────────────────────────

    private void Update()
    {
        if (_canvas == null || !_canvas.enabled)
            return;

        var dt = Time.unscaledDeltaTime;
        var time = Time.unscaledTime;

        // Прогресс
        if (!Mathf.Approximately(_displayProgress, _targetProgress))
        {
            _displayProgress = Mathf.MoveTowards(_displayProgress, _targetProgress, ProgressFillSpeed * dt);
            UpdateFill();
        }

        // Пульсация заголовка и свечения
        var pulse = Mathf.Sin(time * 2.2f);
        var s = 1f + 0.025f * pulse;
        _titleRect.localScale = new Vector3(s, s, 1f);
        var glowColor = _glow.color;
        glowColor.a = 0.5f + 0.1f * pulse;
        _glow.color = glowColor;

        // Советы
        _tipTimer += dt;
        if (_tipTimer >= TipInterval)
        {
            _tipTimer = 0f;
            _tipIndex = (_tipIndex + 1) % Tips.Length;
            _tip.text = Tips[_tipIndex];
        }
        var fadeIn = Mathf.Clamp01(_tipTimer / TipFade);
        var fadeOut = Mathf.Clamp01((TipInterval - _tipTimer) / TipFade);
        _tip.alpha = 0.78f * Mathf.Min(fadeIn, fadeOut);
    }

    private void UpdateFill()
    {
        if (_fill == null)
            return;
        _fill.anchorMax = new Vector2(Mathf.Clamp01(_displayProgress), 1f);
    }

    // ─────────────────────────── Build UI ───────────────────────────

    private void Build()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _canvas.enabled = false;

        var root = (RectTransform)transform;

        // Фон (диагональный градиент, как на экранах входа)
        var bg = CreateImage("Background", root, CreateGradientSprite(), Color.white);
        Stretch(bg.rectTransform);
        bg.raycastTarget = true;

        // Мягкое свечение за заголовком
        _glow = CreateImage("Glow", root, CreateSoftCircleSprite(), new Color(1f, 1f, 1f, 0.5f));
        _glow.raycastTarget = false;
        SetCentered(_glow.rectTransform, new Vector2(0f, 160f), new Vector2(1000f, 1000f));

        // Заголовок
        var title = CreateText("Title", root, "АктивГрад", 118f, FontStyles.Bold, TextDark);
        _titleRect = title.rectTransform;
        SetCentered(_titleRect, new Vector2(0f, 160f), new Vector2(1000f, 220f));

        // Прогресс-бар
        var pill = CreateRoundedSprite();
        var barBg = CreateImage("ProgressBg", root, pill, new Color(1f, 1f, 1f, 0.75f));
        barBg.type = Image.Type.Sliced;
        barBg.pixelsPerUnitMultiplier = 64f / 28f;
        barBg.raycastTarget = false;
        SetCentered(barBg.rectTransform, new Vector2(0f, -140f), new Vector2(720f, 28f));

        var fill = CreateImage("ProgressFill", barBg.rectTransform, pill, TextDark);
        fill.type = Image.Type.Sliced;
        fill.pixelsPerUnitMultiplier = 64f / 28f;
        fill.raycastTarget = false;
        _fill = fill.rectTransform;
        _fill.anchorMin = Vector2.zero;
        _fill.anchorMax = new Vector2(0f, 1f);
        _fill.offsetMin = Vector2.zero;
        _fill.offsetMax = Vector2.zero;

        // Совет
        _tip = CreateText("Tip", root, Tips[0], 44f, FontStyles.Normal, TextSoft);
        var tipRect = _tip.rectTransform;
        tipRect.anchorMin = new Vector2(0.5f, 0f);
        tipRect.anchorMax = new Vector2(0.5f, 0f);
        tipRect.pivot = new Vector2(0.5f, 0.5f);
        tipRect.anchoredPosition = new Vector2(0f, 280f);
        tipRect.sizeDelta = new Vector2(920f, 160f);
    }

    private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, RectTransform parent, string text, float size, FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Texture2D NewTexture(int w, int h, string name)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
    }

    private static Sprite CreateGradientSprite()
    {
        const int size = 64;
        var tex = NewTexture(size, size, "LoadingGradient");
        // Мятный (левый верх) → светлый жёлто-зелёный (правый низ)
        var from = new Color32(160, 200, 196, 255);
        var mid = new Color32(205, 216, 196, 255);
        var to = new Color32(222, 221, 176, 255);
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var u = x / (float)(size - 1);
            var v = 1f - y / (float)(size - 1); // 0 = верх
            var t = (u + v) * 0.5f;
            pixels[y * size + x] = t < 0.5f ? Color.Lerp(from, mid, t * 2f) : Color.Lerp(mid, to, (t - 0.5f) * 2f);
        }
        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateSoftCircleSprite()
    {
        const int size = 128;
        var tex = NewTexture(size, size, "LoadingGlow");
        var pixels = new Color32[size * size];
        var half = (size - 1) * 0.5f;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var dx = (x - half) / half;
            var dy = (y - half) / half;
            var a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            a = a * a;
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateRoundedSprite()
    {
        const int size = 64;
        const float radius = 32f;
        var tex = NewTexture(size, size, "LoadingPill");
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var px = x + 0.5f;
            var py = y + 0.5f;
            var cx = Mathf.Clamp(px, radius, size - radius);
            var cy = Mathf.Clamp(py, radius, size - radius);
            var dx = px - cx;
            var dy = py - cy;
            var d = Mathf.Sqrt(dx * dx + dy * dy);
            var a = Mathf.Clamp01(radius - d + 0.5f);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(30f, 30f, 30f, 30f));
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
