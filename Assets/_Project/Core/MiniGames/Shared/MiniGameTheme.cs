using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Общая палитра и процедурные спрайты для мини-игр (UI, собранный из кода).
/// Никаких ассетов не требуется — все текстуры генерируются и кэшируются.
/// </summary>
public static class MiniGameTheme
{
    // ── palette ──────────────────────────────────────────────────────────────
    // Фирменная зелёно-салатовая палитра проекта
    public static readonly Color Background    = new Color(0.047f, 0.157f, 0.086f);
    public static readonly Color Card          = new Color(0.086f, 0.259f, 0.141f);
    public static readonly Color CardLight     = new Color(0.141f, 0.357f, 0.200f);
    public static readonly Color Accent        = new Color(0.60f, 0.90f, 0.25f);
    public static readonly Color Success       = new Color(0.22f, 0.88f, 0.47f);
    public static readonly Color Warning       = new Color(1.00f, 0.82f, 0.20f);
    public static readonly Color Danger        = new Color(0.95f, 0.28f, 0.30f);
    public static readonly Color TextPrimary   = Color.white;
    public static readonly Color TextSecondary = new Color(0.74f, 0.88f, 0.74f);
    public static readonly Color TextDark      = new Color(0.04f, 0.13f, 0.06f);
    public static readonly Color ShadowColor   = new Color(0f, 0f, 0f, 0.38f);

    public const float DefaultRadius = 18f;

    // ── sprite cache ─────────────────────────────────────────────────────────
    private const int RoundedTexSize  = 64;
    private const int RoundedRadiusPx = 24;

    private static Sprite _rounded;
    private static Sprite _softCircle;
    private static Sprite _circle;
    private static Sprite _star;

    /// <summary>9-slice скруглённый прямоугольник (радиус 24px на ppu 100).</summary>
    public static Sprite RoundedSprite
    {
        get { if (_rounded == null) _rounded = BuildRounded(); return _rounded; }
    }

    /// <summary>Мягкий круг с радиальным затуханием альфы (частицы, свечения).</summary>
    public static Sprite SoftCircleSprite
    {
        get { if (_softCircle == null) _softCircle = BuildCircle(64, true); return _softCircle; }
    }

    /// <summary>Чёткий круг со сглаженным краем.</summary>
    public static Sprite CircleSprite
    {
        get { if (_circle == null) _circle = BuildCircle(128, false); return _circle; }
    }

    /// <summary>Пятиконечная звезда.</summary>
    public static Sprite StarSprite
    {
        get { if (_star == null) _star = BuildStar(128); return _star; }
    }

    // ── styling helpers ──────────────────────────────────────────────────────

    /// <summary>Делает Image скруглённым (sliced). radius — в UI-единицах.</summary>
    public static void ApplyRounded(Image img, float radius = DefaultRadius)
    {
        if (img == null) return;
        img.sprite = RoundedSprite;
        img.type = Image.Type.Sliced;
        img.fillCenter = true;
        img.pixelsPerUnitMultiplier = RoundedRadiusPx / Mathf.Max(1f, radius);
    }

    /// <summary>
    /// Стилизует кнопку: скруглённый фон, цвет, жирная подпись, эффект нажатия и фидбек по клику.
    /// </summary>
    public static void StyleButton(Button btn, Color? color = null, Color? labelColor = null,
        float labelSize = 22f, float radius = 16f, FeedbackType? clickFeedback = FeedbackType.Tap)
    {
        if (btn == null) return;

        var img = btn.targetGraphic as Image;
        if (img == null) img = btn.GetComponent<Image>();
        if (img != null)
        {
            ApplyRounded(img, radius);
            img.color = color ?? Accent;
        }

        btn.transition = Selectable.Transition.ColorTint;
        var cols = btn.colors;
        cols.normalColor      = Color.white;
        cols.highlightedColor = new Color(1f, 1f, 1f, 1f);
        cols.selectedColor    = Color.white;
        cols.pressedColor     = new Color(0.82f, 0.82f, 0.82f, 1f);
        cols.disabledColor    = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        cols.fadeDuration     = 0.08f;
        btn.colors = cols;

        var lbl = btn.GetComponentInChildren<TMP_Text>(true);
        if (lbl != null)
        {
            lbl.fontSize  = labelSize;
            lbl.fontStyle = FontStyles.Bold;
            lbl.color     = labelColor ?? TextPrimary;
        }

        if (btn.GetComponent<MiniGamePress>() == null)
            btn.gameObject.AddComponent<MiniGamePress>();

        if (clickFeedback.HasValue)
        {
            var fb = clickFeedback.Value;
            btn.onClick.AddListener(() => MiniGameJuice.Feedback(fb));
        }
    }

    /// <summary>
    /// Скругляет существующий Image-карточку и добавляет тень (соседний тёмный rounded-image со смещением).
    /// Тень — отдельный sibling, поэтому не анимируйте scale у такой карточки (используйте CreateCard).
    /// </summary>
    public static Image StyleCard(Image card, Color? color = null, float radius = 22f, float shadowOffset = 6f)
    {
        if (card == null) return null;
        ApplyRounded(card, radius);
        card.color = color ?? Card;

        var parent = card.transform.parent;
        if (parent == null) return null;

        var src = card.rectTransform;
        var go = new GameObject(card.name + "_Shadow");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = src.anchorMin;
        rt.anchorMax = src.anchorMax;
        rt.pivot     = src.pivot;
        rt.sizeDelta = src.sizeDelta;
        rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, -shadowOffset);
        var img = go.AddComponent<Image>();
        ApplyRounded(img, radius);
        img.color = ShadowColor;
        img.raycastTarget = false;
        go.transform.SetSiblingIndex(card.transform.GetSiblingIndex());
        return img;
    }

    /// <summary>
    /// Создаёт карточку: корень (без графики) → Shadow → Body. Контент добавляйте в корень —
    /// он рисуется поверх Body. Анимировать можно корень целиком.
    /// </summary>
    public static RectTransform CreateCard(Transform parent, string name, Vector2 size,
        out Image body, Color? color = null, float radius = 22f, float shadowOffset = 6f)
    {
        var rootGo = new GameObject(name);
        rootGo.transform.SetParent(parent, false);
        var root = rootGo.AddComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = size;

        var shadow = MakeImage(root, "Shadow", ShadowColor);
        Stretch(shadow.rectTransform);
        shadow.rectTransform.anchoredPosition = new Vector2(0f, -shadowOffset);
        ApplyRounded(shadow, radius);
        shadow.raycastTarget = false;

        body = MakeImage(root, "Body", color ?? Card);
        Stretch(body.rectTransform);
        ApplyRounded(body, radius);

        // тонкий светлый верхний блик
        var hl = MakeImage(root, "Highlight", new Color(1f, 1f, 1f, 0.05f));
        var hlr = hl.rectTransform;
        hlr.anchorMin = new Vector2(0f, 1f);
        hlr.anchorMax = new Vector2(1f, 1f);
        hlr.pivot     = new Vector2(0.5f, 1f);
        hlr.sizeDelta = new Vector2(-8f, Mathf.Min(40f, size.y * 0.3f));
        hlr.anchoredPosition = new Vector2(0f, -4f);
        ApplyRounded(hl, radius - 4f);
        hl.raycastTarget = false;

        return root;
    }

    /// <summary>Готовая стилизованная кнопка с подписью (якорь по центру parent).</summary>
    public static Button CreateButton(Transform parent, string name, string label, Vector2 size,
        Color? color = null, Color? labelColor = null, float labelSize = 22f,
        FeedbackType? clickFeedback = FeedbackType.Tap)
    {
        var img = MakeImage(parent, name, color ?? Accent);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        var lbl = CreateText(img.transform, "Label", label, labelSize, labelColor ?? TextPrimary, FontStyles.Bold);
        Stretch(lbl.rectTransform);

        StyleButton(btn, color ?? Accent, labelColor, labelSize, Mathf.Min(16f, size.y * 0.35f), clickFeedback);
        return btn;
    }

    /// <summary>TMP-текст с якорем по центру parent, без raycast.</summary>
    public static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size,
        Color color, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300f, size * 1.6f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Image MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ── texture builders ─────────────────────────────────────────────────────

    private static Texture2D NewTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
            hideFlags  = HideFlags.DontUnloadUnusedAsset
        };
        return tex;
    }

    private static Sprite BuildRounded()
    {
        int size = RoundedTexSize;
        float r = RoundedRadiusPx;
        var tex = NewTex(size);
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float cx = Mathf.Clamp(fx, r, size - r);
                float cy = Mathf.Clamp(fy, r, size - r);
                float d = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                float a = Mathf.Clamp01(r - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        s.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return s;
    }

    private static Sprite BuildCircle(int size, bool soft)
    {
        var tex = NewTex(size);
        var px = new Color32[size * size];
        float c = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a;
                if (soft)
                {
                    float t = Mathf.Clamp01(1f - d / c);
                    a = t * t * (3f - 2f * t);
                }
                else a = Mathf.Clamp01(c - 1f - d + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        s.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return s;
    }

    private static Sprite BuildStar(int size)
    {
        // 10-вершинный многоугольник, 4x4 суперсэмплинг для сглаживания
        var poly = new Vector2[10];
        float c = size * 0.5f;
        float outer = c - 2f, inner = outer * 0.45f;
        for (int i = 0; i < 10; i++)
        {
            float ang = Mathf.Deg2Rad * (90f + i * 36f);
            float rad = (i % 2 == 0) ? outer : inner;
            poly[i] = new Vector2(c + Mathf.Cos(ang) * rad, c + Mathf.Sin(ang) * rad - size * 0.03f);
        }

        var tex = NewTex(size);
        var px = new Color32[size * size];
        const int ss = 4;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                        if (InsidePoly(poly, x + (sx + 0.5f) / ss, y + (sy + 0.5f) / ss)) hits++;
                byte a = (byte)(hits * 255 / (ss * ss));
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        s.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return s;
    }

    private static bool InsidePoly(Vector2[] poly, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if (((poly[i].y > y) != (poly[j].y > y)) &&
                (x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                inside = !inside;
        }
        return inside;
    }
}
