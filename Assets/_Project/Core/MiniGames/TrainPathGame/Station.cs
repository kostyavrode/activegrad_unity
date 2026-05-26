using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Station : MonoBehaviour
{
    private Vector2 _position;
    private float   _waitTime;
    private int     _index;
    private Image   _image;
    private TextMeshProUGUI _label;

    private bool _isStart;
    private bool _isEnd;
    private bool _isCargo;
    private bool _isCargoCollected;

    private Color _baseColor;
    private TrainPathConfig _config;

    private Tween _pulseTween;
    private Tween _cargoPulseTween;

    private static Sprite _cachedCircle;

    // ── Public ───────────────────────────────────────────────────────────────
    public Vector2 Position         => _position;
    public float   WaitTime         => _waitTime;
    public int     Index            => _index;
    public bool    IsCargo          => _isCargo;
    public bool    IsCargoCollected => _isCargoCollected;

    // ── Init ─────────────────────────────────────────────────────────────────
    public void Initialize(Vector2 position, float waitTime, int index, TrainPathConfig config)
    {
        _position = position;
        _waitTime = waitTime;
        _index    = index;
        _config   = config;

        _image = GetComponent<Image>();
        _label = GetComponentInChildren<TextMeshProUGUI>();

        // Always use circle sprite (fallback to procedural when no config)
        if (_image != null)
            _image.sprite = config?.StationSprite ?? GetOrCreateCircleSprite();

        _baseColor = config?.ColorDefault ?? new Color(0.30f, 0.42f, 0.75f);
        ApplyColor();

        // Clean label — no wait time shown
        if (_label != null) _label.text = "";
    }

    // ── Role setters ─────────────────────────────────────────────────────────
    public void SetAsStart()
    {
        _isStart   = true;
        _baseColor = _config?.ColorStart ?? new Color(0.15f, 0.85f, 0.35f);

        if (_config?.StartStationSprite != null && _image != null)
            _image.sprite = _config.StartStationSprite;

        ApplyColor();
        SetLabel("▶", 12);

        // Subtle outer ring pulse
        _pulseTween?.Kill();
        _pulseTween = transform.DOScale(1.08f, 1.0f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    public void SetAsEnd()
    {
        _isEnd     = true;
        _baseColor = _config?.ColorEnd ?? new Color(0.85f, 0.18f, 0.28f);

        if (_config?.EndStationSprite != null && _image != null)
            _image.sprite = _config.EndStationSprite;

        ApplyColor();
        SetLabel("■", 12);
    }

    public void SetAsCargo()
    {
        _isCargo   = true;
        _baseColor = _config?.ColorCargo ?? new Color(0.95f, 0.60f, 0.10f);

        if (_config?.CargoStationSprite != null && _image != null)
            _image.sprite = _config.CargoStationSprite;

        ApplyColor();
        SetLabel("📦", 10);

        // Cargo stations pulse until collected
        _cargoPulseTween?.Kill();
        _cargoPulseTween = transform
            .DOScale(1.18f, 0.65f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    public void CollectCargo()
    {
        _isCargoCollected = true;
        _cargoPulseTween?.Kill();
        _pulseTween?.Kill();

        Color collected = _config?.ColorCollected ?? new Color(0.28f, 0.32f, 0.40f);

        // Burst → shrink back
        var rt = GetComponent<RectTransform>();
        rt.DOScale(Vector3.one, 0f).SetUpdate(true); // reset scale from pulse
        DOTween.Sequence()
            .Append(rt.DOScale(1.65f, 0.12f).SetEase(Ease.OutBack))
            .Append(rt.DOScale(1f,    0.20f).SetEase(Ease.InBack))
            .SetUpdate(true);

        DOTween.To(() => _image.color, c => _image.color = c, collected, 0.15f)
            .SetDelay(0.05f).SetUpdate(true);

        _baseColor = collected;
        SetLabel("✓", 14);
    }

    public void SetHighlight(bool highlight)
    {
        if (_isStart || _isEnd || _isCargoCollected) return;
        if (_image == null) return;

        _pulseTween?.Kill();
        var rt = GetComponent<RectTransform>();

        Color hlColor = _config?.ColorHighlight ?? new Color(1f, 0.92f, 0.10f);

        if (highlight && !_isCargo)
        {
            _image.color = hlColor;
            rt.localScale = Vector3.one;
            _pulseTween = rt.DOScale(1.25f, 0.40f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }
        else
        {
            rt.DOScale(1f, 0.10f).SetUpdate(true);
            _image.color = _baseColor;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void SetLabel(string text, float size)
    {
        if (_label == null) return;
        _label.text     = text;
        _label.fontSize = size;
        _label.color    = Color.white;
    }

    private void ApplyColor()
    {
        if (_image != null) _image.color = _baseColor;
    }

    private void OnDestroy()
    {
        _pulseTween?.Kill();
        _cargoPulseTween?.Kill();
    }

    // ── Circle sprite (procedural) ────────────────────────────────────────────
    private static Sprite GetOrCreateCircleSprite()
    {
        if (_cachedCircle != null) return _cachedCircle;

        const int sz = 64;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        float cx = sz * 0.5f, r = cx - 1f;

        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float d = Mathf.Sqrt((x - cx + 0.5f) * (x - cx + 0.5f) +
                                     (y - cx + 0.5f) * (y - cx + 0.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((r - d) / 1.5f)));
            }

        tex.Apply();
        _cachedCircle = Sprite.Create(tex, new Rect(0, 0, sz, sz), Vector2.one * 0.5f);
        return _cachedCircle;
    }
}
