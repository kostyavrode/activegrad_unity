using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Процедурные спрайты и хелперы построения UI для экранов наград (без зависимостей от ассетов).
/// </summary>
public static class RewardUiFactory
{
    public const string RootPrefix = "[Reward]";

    private static Sprite _softCircle;
    private static Sprite _rays;
    private static Sprite _rounded;

    // ─────────────────────────── процедурные спрайты ───────────────────────────

    public static Sprite SoftCircle
    {
        get
        {
            if (_softCircle == null) _softCircle = CreateSoftCircle(128);
            return _softCircle;
        }
    }

    public static Sprite Rays
    {
        get
        {
            if (_rays == null) _rays = CreateRays(256, 14);
            return _rays;
        }
    }

    /// <summary>9-slice скруглённый прямоугольник для карточек и кнопок.</summary>
    public static Sprite Rounded
    {
        get
        {
            if (_rounded == null) _rounded = CreateRounded(96, 32);
            return _rounded;
        }
    }

    private static Texture2D NewTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            // Иначе Resources.UnloadUnusedAssets может выгрузить текстуру, на которую ссылается только статик
            hideFlags = HideFlags.HideAndDontSave
        };
        return tex;
    }

    private static Sprite ToSprite(Texture2D tex, Vector4 border = default)
    {
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, border);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Sprite CreateSoftCircle(int size)
    {
        var tex = NewTexture(size);
        var px = new Color32[size * size];
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x + 0.5f - r, dy = y + 0.5f - r;
            float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
            float a = 1f - Mathf.SmoothStep(0.86f, 1f, d);
            px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return ToSprite(tex);
    }

    private static Sprite CreateRays(int size, int rayCount)
    {
        var tex = NewTexture(size);
        var px = new Color32[size * size];
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x + 0.5f - r, dy = y + 0.5f - r;
            float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
            float ang = Mathf.Atan2(dy, dx);
            float ray = Mathf.SmoothStep(0.35f, 0.85f, (Mathf.Cos(ang * rayCount) + 1f) * 0.5f);
            float radial = Mathf.Pow(Mathf.Clamp01(1f - d), 1.3f);
            float core = Mathf.Clamp01(1f - d * 2.2f) * 0.6f;
            float a = Mathf.Clamp01(ray * radial + core);
            px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return ToSprite(tex);
    }

    private static Sprite CreateRounded(int size, int radius)
    {
        var tex = NewTexture(size);
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
            float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
            float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(radius - d + 0.5f);
            px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return ToSprite(tex, new Vector4(radius, radius, radius, radius));
    }

    // ─────────────────────────── канвас / координаты ───────────────────────────

    public static Transform FindCanvasTransform()
    {
        try
        {
            var go = GameObject.FindGameObjectWithTag("Canvas");
            if (go != null) return go.transform;
        }
        catch (UnityException)
        {
            // тег не определён — используем fallback
        }

        var canvas = Object.FindFirstObjectByType<Canvas>();
        return canvas != null ? canvas.rootCanvas.transform : null;
    }

    public static Camera GetCanvasCamera(Transform t)
    {
        if (t == null) return null;
        var canvas = t.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        canvas = canvas.rootCanvas;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    public static Vector2 ScreenToLocal(RectTransform rect, Vector2 screenPos, Camera cam)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, cam, out var local);
        return local;
    }

    /// <summary>Полноэкранный корень поверх всего (собственный Canvas с override sorting).</summary>
    public static RectTransform CreateRoot(Transform canvasTransform, string name, bool blocksRaycasts, out CanvasGroup group)
    {
        var go = new GameObject(RootPrefix + name, typeof(RectTransform));
        go.layer = canvasTransform.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvasTransform, false);
        Stretch(rt);
        rt.SetAsLastSibling();

        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        go.AddComponent<GraphicRaycaster>();

        group = go.AddComponent<CanvasGroup>();
        group.blocksRaycasts = blocksRaycasts;
        group.interactable = blocksRaycasts;
        return rt;
    }

    public static bool IsRewardObject(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
            if (p.name.StartsWith(RootPrefix)) return true;
        return false;
    }

    // ─────────────────────────── построение элементов ───────────────────────────

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static RectTransform MakeRect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    public static Image MakeImage(Transform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size,
        bool raycast = false)
    {
        var rt = MakeRect(parent, name, pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        if (sprite != null && sprite == _rounded)
            img.type = Image.Type.Sliced;
        return img;
    }

    public static Image MakeBackdrop(Transform parent, Color color)
    {
        var img = MakeImage(parent, "Backdrop", null, color, Vector2.zero, Vector2.zero, raycast: true);
        Stretch(img.rectTransform);
        return img;
    }

    public static TextMeshProUGUI MakeText(Transform parent, string name, string text, float fontSize, FontStyles style,
        Color color, Vector2 pos, Vector2 size)
    {
        var rt = MakeRect(parent, name, pos, size);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Button MakeButton(Transform parent, string name, string label, Color bg, Color textColor,
        Vector2 pos, Vector2 size, float fontSize = 40f)
    {
        var img = MakeImage(parent, name, Rounded, bg, pos, size, raycast: true);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var cols = btn.colors;
        cols.highlightedColor = Color.white;
        cols.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors = cols;

        var lbl = MakeText(img.transform, "Label", label, fontSize, FontStyles.Bold, textColor, Vector2.zero, size);
        Stretch(lbl.rectTransform);
        return btn;
    }

    /// <summary>Иконка награды: цветной круг (или спрайт), внутри "+N" если спрайта нет, подпись снизу.</summary>
    public static RectTransform MakeRewardIcon(Transform parent, RewardEntry entry, Vector2 pos, float size, bool withLabel = true)
    {
        var tint = entry.EffectiveTint;
        var root = MakeRect(parent, "Reward_" + entry.Id, pos, new Vector2(size, size));

        MakeImage(root, "Glow", SoftCircle, new Color(tint.r, tint.g, tint.b, 0.35f), Vector2.zero, Vector2.one * size * 1.45f);
        MakeImage(root, "Ring", SoftCircle, Color.Lerp(tint, Color.black, 0.35f), Vector2.zero, Vector2.one * size);
        MakeImage(root, "Fill", SoftCircle, tint, Vector2.zero, Vector2.one * size * 0.86f);
        MakeImage(root, "Shine", SoftCircle, new Color(1f, 1f, 1f, 0.35f), new Vector2(-size * 0.14f, size * 0.16f), Vector2.one * size * 0.34f);

        string amountText = entry.Amount > 0 ? "+" + entry.Amount : "★";
        if (entry.Icon != null)
        {
            var icon = MakeImage(root, "Icon", entry.Icon, Color.white, Vector2.zero, Vector2.one * size * 0.62f);
            icon.preserveAspect = true;
        }
        else
        {
            var t = MakeText(root, "Amount", amountText, size * 0.3f, FontStyles.Bold, Color.white, Vector2.zero,
                new Vector2(size * 0.9f, size * 0.6f));
            t.enableAutoSizing = true;
            t.fontSizeMin = 12f;
            t.fontSizeMax = size * 0.32f;
            t.outlineWidth = 0.18f;
            t.outlineColor = new Color32(0, 0, 0, 140);
        }

        if (withLabel)
        {
            string label = entry.Icon != null && entry.Amount > 0
                ? $"+{entry.Amount} {entry.Label}"
                : (entry.Label ?? "");
            MakeText(root, "Label", label, size * 0.2f, FontStyles.Bold, new Color(1f, 1f, 1f, 0.92f),
                new Vector2(0f, -size * 0.72f), new Vector2(size * 1.6f, size * 0.36f));
        }

        return root;
    }

    /// <summary>Маленькая иконка для полёта валюты (без текста).</summary>
    public static RectTransform MakeFlyIcon(Transform parent, RewardEntry entry, Vector2 pos, float size)
    {
        var tint = entry.EffectiveTint;
        var root = MakeRect(parent, "Fly_" + entry.Id, pos, new Vector2(size, size));
        MakeImage(root, "Glow", SoftCircle, new Color(tint.r, tint.g, tint.b, 0.4f), Vector2.zero, Vector2.one * size * 1.5f);
        if (entry.Icon != null)
        {
            var icon = MakeImage(root, "Icon", entry.Icon, Color.white, Vector2.zero, Vector2.one * size);
            icon.preserveAspect = true;
        }
        else
        {
            MakeImage(root, "Ring", SoftCircle, Color.Lerp(tint, Color.black, 0.35f), Vector2.zero, Vector2.one * size);
            MakeImage(root, "Fill", SoftCircle, tint, Vector2.zero, Vector2.one * size * 0.8f);
            MakeImage(root, "Shine", SoftCircle, new Color(1f, 1f, 1f, 0.45f), new Vector2(-size * 0.14f, size * 0.16f), Vector2.one * size * 0.32f);
        }
        return root;
    }

    // ─────────────────────────── эффекты ───────────────────────────

    private static readonly Color[] ConfettiColors =
    {
        new Color(1f, 0.82f, 0.2f), new Color(1f, 0.35f, 0.45f), new Color(0.35f, 0.85f, 1f),
        new Color(0.45f, 1f, 0.5f), new Color(0.75f, 0.5f, 1f), new Color(1f, 0.6f, 0.2f), Color.white
    };

    /// <summary>Взрыв конфетти из точки (локальные координаты parent, pivot по центру).</summary>
    public static void ConfettiBurst(RectTransform parent, Vector2 origin, int count = 40, float power = 1f)
    {
        for (int i = 0; i < count; i++)
        {
            var color = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
            var size = new Vector2(Random.Range(12f, 22f), Random.Range(20f, 34f));
            var img = MakeImage(parent, "Confetti", i % 3 == 0 ? SoftCircle : null, color, origin,
                i % 3 == 0 ? Vector2.one * size.x * 1.2f : size);
            var rt = img.rectTransform;
            var go = img.gameObject;
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Vector2 v = new Vector2(Random.Range(-750f, 750f), Random.Range(900f, 1800f)) * power;
            float g = -2600f * power;
            float dur = Random.Range(1.3f, 1.9f);
            float t = 0f;

            DOTween.To(() => t, x =>
                {
                    t = x;
                    rt.anchoredPosition = origin + v * x + Vector2.up * (0.5f * g * x * x);
                }, dur, dur)
                .SetEase(Ease.Linear)
                .OnComplete(() => { if (go != null) Object.Destroy(go); })
                .U(go);

            rt.DORotate(new Vector3(Random.Range(180f, 900f), 0f, Random.Range(-720f, 720f)), dur, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad).U(go);
            img.DOFade(0f, 0.45f).SetDelay(dur - 0.45f).U(go);
        }
    }

    /// <summary>SetUpdate(true) + SetLink(go): независимо от timeScale и авто-kill при уничтожении.</summary>
    public static T U<T>(this T tween, GameObject go) where T : Tween
    {
        tween.SetUpdate(true);
        if (go != null) tween.SetLink(go);
        return tween;
    }
}
