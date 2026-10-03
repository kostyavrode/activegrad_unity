using System;
using System.Collections;
using ActiveGrad.MiniGames;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Экран результата мини-игры, собираемый из кода: заголовок, счёт с count-up,
/// 1–3 звезды, баннер «Новый рекорд!», строки статистики, доп. область (например, бонусный ползунок)
/// и кнопки. Unscaled time.
/// </summary>
public class MiniGameResultPanel : MonoBehaviour
{
    public class Options
    {
        public string   GameId         = "game";          // ключ рекорда: MiniGameBest_{GameId}
        public string   Title          = "ИГРА ОКОНЧЕНА";
        public Color    TitleColor     = MiniGameTheme.Accent;
        public int      Score;
        public int      MaxScore       = 100;
        public float[]  StarThresholds = { 0.3f, 0.6f, 0.9f }; // доли MaxScore для 1/2/3 звёзд
        public string   ScoreCaption   = "очков из 100";
        public string[] StatLines;
        public float    ExtraHeight;                       // высота доп. области (ExtraArea)
        public bool     ShowInfoLine;                      // строка InfoText под доп. областью
        public string   PrimaryLabel   = "ЗАВЕРШИТЬ";
        public Action   OnPrimary;
        public bool     PrimaryVisibleImmediately = true;
        public string   SecondaryLabel;                    // null → без второй кнопки
        public Action   OnSecondary;
        public bool     CommitBestImmediately = true;      // false → вызовите UpdateScore/CommitBest позже
    }

    private const float CardWidth = 330f;

    // ── public refs ──────────────────────────────────────────────────────────
    public RectTransform   Card       { get; private set; }
    public RectTransform   ExtraArea  { get; private set; }
    public TextMeshProUGUI TitleText  { get; private set; }
    public TextMeshProUGUI InfoText   { get; private set; }
    public Button          PrimaryButton   { get; private set; }
    public Button          SecondaryButton { get; private set; }
    public int             DisplayedScore => _displayed;

    // ── internals ────────────────────────────────────────────────────────────
    private Options          _o;
    private CanvasGroup      _group;
    private TextMeshProUGUI  _scoreText;
    private TextMeshProUGUI  _bestText;
    private RectTransform    _starsRow;
    private Image[]          _starFills;
    private RectTransform    _banner;
    private int              _displayed;
    private int              _starsShown;
    private bool             _bestCommitted;
    private float            _fitScale = 1f;
    private Coroutine        _anim;

    // ══════════════════════════════════════════════════════════════════════════
    // FACTORY
    // ══════════════════════════════════════════════════════════════════════════

    public static MiniGameResultPanel Show(RectTransform parent, Options options)
    {
        if (parent == null) return null;
        options ??= new Options();

        var go = new GameObject("MiniGameResultPanel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        MiniGameTheme.Stretch(rt);
        go.transform.SetAsLastSibling();

        // затемнение (блокирует ввод под панелью)
        var dim = go.AddComponent<Image>();
        dim.color = new Color(0.184f, 0.310f, 0.310f, 0.55f);

        var panel = go.AddComponent<MiniGameResultPanel>();
        panel._o = options;
        panel._group = go.AddComponent<CanvasGroup>();
        panel.Build(rt, parent);
        panel.PlayIntro();
        return panel;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BUILD
    // ══════════════════════════════════════════════════════════════════════════

    private void Build(RectTransform root, RectTransform parent)
    {
        var o = _o;
        int statCount = o.StatLines?.Length ?? 0;
        bool hasSecondary = !string.IsNullOrEmpty(o.SecondaryLabel);

        // ── расчёт высоты ─────────────────────────────────────────────────────
        float h = 24f + 40f + 84f + 22f + 6f + 70f + 24f + 8f + statCount * 25f;
        if (o.ExtraHeight > 0f) h += 6f + o.ExtraHeight;
        if (o.ShowInfoLine) h += 30f;
        h += 14f + 56f;
        if (hasSecondary) h += 10f + 46f;
        h += 22f;

        Card = MiniGameTheme.CreateCard(root, "Card", new Vector2(CardWidth, h), out _, MiniGameTheme.Card, 26f);

        // ── вписываем в экран ────────────────────────────────────────────────
        var pr = parent.rect;
        if (pr.height > 10f && pr.width > 10f)
            _fitScale = Mathf.Clamp(Mathf.Min(pr.height * 0.9f / h, pr.width * 0.86f / CardWidth), 0.5f, 3f);

        float y = 24f;

        // title
        TitleText = MiniGameTheme.CreateText(Card, "Title", o.Title, 28f, o.TitleColor, FontStyles.Bold);
        Place(TitleText.rectTransform, y, 40f, CardWidth - 24f);
        y += 40f;

        // score
        _scoreText = MiniGameTheme.CreateText(Card, "Score", "0", 76f, MiniGameTheme.TextPrimary, FontStyles.Bold);
        Place(_scoreText.rectTransform, y, 84f, CardWidth - 24f);
        y += 84f;

        var cap = MiniGameTheme.CreateText(Card, "Caption", o.ScoreCaption ?? "", 16f, MiniGameTheme.TextSecondary);
        Place(cap.rectTransform, y, 22f, CardWidth - 24f);
        y += 22f + 6f;

        // stars
        var rowGo = new GameObject("Stars");
        rowGo.transform.SetParent(Card, false);
        _starsRow = rowGo.AddComponent<RectTransform>();
        Place(_starsRow, y, 70f, CardWidth - 40f);
        BuildStars();
        y += 70f;

        // best
        _bestText = MiniGameTheme.CreateText(Card, "Best", "", 15f, MiniGameTheme.TextSecondary);
        Place(_bestText.rectTransform, y, 24f, CardWidth - 24f);
        int prevBest = PlayerPrefs.GetInt(BestKey, 0);
        _bestText.text = prevBest > 0 ? $"Лучший результат: {prevBest}" : "";
        y += 24f + 8f;

        // stats
        for (int i = 0; i < statCount; i++)
        {
            var line = MiniGameTheme.CreateText(Card, $"Stat{i}", o.StatLines[i], 17f,
                MiniGameTheme.TextSecondary);
            Place(line.rectTransform, y, 25f, CardWidth - 30f);
            y += 25f;
        }

        // extra area
        if (o.ExtraHeight > 0f)
        {
            y += 6f;
            var ex = new GameObject("ExtraArea");
            ex.transform.SetParent(Card, false);
            ExtraArea = ex.AddComponent<RectTransform>();
            Place(ExtraArea, y, o.ExtraHeight, CardWidth - 20f);
            y += o.ExtraHeight;
        }

        // info
        if (o.ShowInfoLine)
        {
            InfoText = MiniGameTheme.CreateText(Card, "Info", "", 17f, MiniGameTheme.TextPrimary, FontStyles.Bold);
            Place(InfoText.rectTransform, y, 30f, CardWidth - 24f);
            y += 30f;
        }

        y += 14f;

        // buttons
        PrimaryButton = MiniGameTheme.CreateButton(Card, "PrimaryBtn", o.PrimaryLabel ?? "OK",
            new Vector2(240f, 56f), MiniGameTheme.Accent, MiniGameTheme.TextOnAccent, 22f, FeedbackType.Tap);
        Place((RectTransform)PrimaryButton.transform, y, 56f, 240f);
        PrimaryButton.onClick.AddListener(() => _o.OnPrimary?.Invoke());
        PrimaryButton.gameObject.SetActive(o.PrimaryVisibleImmediately);
        y += 56f;

        if (hasSecondary)
        {
            y += 10f;
            SecondaryButton = MiniGameTheme.CreateButton(Card, "SecondaryBtn", o.SecondaryLabel,
                new Vector2(240f, 46f), MiniGameTheme.CardLight, MiniGameTheme.TextPrimary, 19f, FeedbackType.Tap);
            Place((RectTransform)SecondaryButton.transform, y, 46f, 240f);
            SecondaryButton.onClick.AddListener(() => _o.OnSecondary?.Invoke());
        }

        // banner «Новый рекорд!» (скрыт)
        var bannerImg = MiniGameTheme.MakeImage(Card, "RecordBanner", MiniGameTheme.Warning);
        MiniGameTheme.ApplyRounded(bannerImg, 14f);
        bannerImg.raycastTarget = false;
        _banner = bannerImg.rectTransform;
        _banner.anchorMin = _banner.anchorMax = new Vector2(0.5f, 1f);
        _banner.pivot = new Vector2(0.5f, 0.5f);
        _banner.sizeDelta = new Vector2(200f, 38f);
        _banner.anchoredPosition = new Vector2(0f, 4f);
        _banner.localRotation = Quaternion.Euler(0f, 0f, 4f);
        var bannerTxt = MiniGameTheme.CreateText(_banner, "Text", "Новый рекорд!", 20f,
            MiniGameTheme.TextDark, FontStyles.Bold);
        MiniGameTheme.Stretch(bannerTxt.rectTransform);
        _banner.gameObject.SetActive(false);
    }

    private void BuildStars()
    {
        _starFills = new Image[3];
        float[] xs    = { -78f, 0f, 78f };
        float[] sizes = { 54f, 66f, 54f };
        float[] ys    = { -4f, 4f, -4f };

        for (int i = 0; i < 3; i++)
        {
            var slot = MiniGameTheme.MakeImage(_starsRow, $"Star{i}", new Color(0f, 0f, 0f, 0.10f));
            slot.sprite = MiniGameTheme.StarSprite;
            slot.raycastTarget = false;
            var srt = slot.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(sizes[i], sizes[i]);
            srt.anchoredPosition = new Vector2(xs[i], ys[i]);

            var fill = MiniGameTheme.MakeImage(srt, "Fill", MiniGameTheme.Warning);
            fill.sprite = MiniGameTheme.StarSprite;
            fill.raycastTarget = false;
            MiniGameTheme.Stretch(fill.rectTransform);
            fill.rectTransform.localScale = Vector3.zero;
            _starFills[i] = fill;
        }
    }

    private static void Place(RectTransform rt, float top, float height, float width)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(0f, -top);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════════════════

    private string BestKey => $"MiniGameBest_{_o.GameId}";

    /// <summary>Досчитать до нового значения (например, после бонусного ползунка), обновить звёзды.</summary>
    public void UpdateScore(int newScore, bool commitBest = true)
    {
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(AnimateScore(newScore, 0f, 0.55f, commitBest));
    }

    /// <summary>Сравнить с рекордом (однократно). Показывает баннер при новом рекорде.</summary>
    public void CommitBest(int score)
    {
        if (_bestCommitted) return;
        _bestCommitted = true;

        int best = PlayerPrefs.GetInt(BestKey, 0);
        if (score > best && score > 0)
        {
            PlayerPrefs.SetInt(BestKey, score);
            PlayerPrefs.Save();
            _bestText.text = $"Лучший результат: {score}";
            if (best > 0 || score > 0) ShowRecordBanner();
        }
    }

    /// <summary>Показать/скрыть основную кнопку (с анимацией появления).</summary>
    public void SetPrimaryVisible(bool visible)
    {
        if (PrimaryButton == null) return;
        var go = PrimaryButton.gameObject;
        if (!visible) { go.SetActive(false); return; }
        if (go.activeSelf) return;
        go.SetActive(true);
        var t = go.transform;
        t.localScale = Vector3.one * 0.5f;
        t.DOScale(1f, 0.35f).SetEase(Ease.OutBack, 2f).SetUpdate(true).SetLink(go);
    }

    /// <summary>
    /// Строит в ExtraArea бонусный ползунок (подсказка, подписи, трек с зонами, индикатор).
    /// Рекомендуемая ExtraHeight — 96.
    /// </summary>
    public BonusSliderComponent BuildBonusSlider(string leftLabel, string rightLabel, out TextMeshProUGUI hintText)
    {
        hintText = null;
        if (ExtraArea == null) return null;
        float w = ExtraArea.sizeDelta.x;

        hintText = MiniGameTheme.CreateText(ExtraArea, "Hint", "", 15f, MiniGameTheme.Success);
        Place(hintText.rectTransform, 0f, 24f, w);

        var left = MiniGameTheme.CreateText(ExtraArea, "SliderL", leftLabel, 14f, MiniGameTheme.TextSecondary,
            FontStyles.Normal, TextAlignmentOptions.Left);
        var right = MiniGameTheme.CreateText(ExtraArea, "SliderR", rightLabel, 14f, MiniGameTheme.TextSecondary,
            FontStyles.Normal, TextAlignmentOptions.Right);
        const float TW = 280f, TH = 26f;
        Place(left.rectTransform, 28f, 22f, TW);
        Place(right.rectTransform, 28f, 22f, TW);

        var trackImg = MiniGameTheme.MakeImage(ExtraArea, "SliderTrack", MiniGameTheme.CardLight);
        MiniGameTheme.ApplyRounded(trackImg, 8f);
        trackImg.raycastTarget = false;
        var track = trackImg.rectTransform;
        track.anchorMin = track.anchorMax = new Vector2(0.5f, 1f);
        track.pivot = new Vector2(0.5f, 0.5f);
        track.sizeDelta = new Vector2(TW, TH);
        track.anchoredPosition = new Vector2(0f, -(52f + TH * 0.5f + 4f));

        float hw = TW * 0.5f - 2f;
        (float from, float to, Color col)[] zones =
        {
            (-hw,         -hw * 0.55f, MiniGameTheme.Danger),
            (-hw * 0.55f, -hw * 0.22f, MiniGameTheme.Warning),
            (-hw * 0.22f,  hw * 0.22f, MiniGameTheme.Success),
            ( hw * 0.22f,  hw * 0.55f, MiniGameTheme.Warning),
            ( hw * 0.55f,  hw,         MiniGameTheme.Danger),
        };
        foreach (var (from, to, col) in zones)
        {
            var z = MiniGameTheme.MakeImage(track, "Zone", new Color(col.r, col.g, col.b, 0.85f));
            z.raycastTarget = false;
            var zrt = z.rectTransform;
            zrt.anchorMin = zrt.anchorMax = new Vector2(0.5f, 0.5f);
            zrt.sizeDelta = new Vector2(to - from - 1f, TH - 6f);
            zrt.anchoredPosition = new Vector2((from + to) * 0.5f, 0f);
        }

        var ind = MiniGameTheme.MakeImage(track, "Indicator", MiniGameTheme.TextPrimary);
        MiniGameTheme.ApplyRounded(ind, 3f);
        ind.raycastTarget = false;
        var indRt = ind.rectTransform;
        indRt.anchorMin = indRt.anchorMax = new Vector2(0.5f, 0.5f);
        indRt.sizeDelta = new Vector2(7f, TH + 12f);

        var host = new GameObject("SliderHost");
        host.transform.SetParent(ExtraArea, false);
        host.AddComponent<RectTransform>();
        var slider = host.AddComponent<BonusSliderComponent>();
        slider.Setup(track, indRt, left, right, null);
        // Setup перезаписывает подписи дефолтными — возвращаем нужные
        left.text = leftLabel;
        right.text = rightLabel;
        return slider;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ANIMATION
    // ══════════════════════════════════════════════════════════════════════════

    private void PlayIntro()
    {
        _group.alpha = 0f;
        _group.DOFade(1f, 0.25f).SetUpdate(true).SetLink(gameObject);

        Card.localScale = Vector3.one * (_fitScale * 0.82f);
        Card.DOScale(_fitScale, 0.4f).SetEase(Ease.OutBack, 1.6f).SetUpdate(true).SetLink(gameObject);

        MiniGameJuice.Feedback(FeedbackType.Success);
        _anim = StartCoroutine(AnimateScore(_o.Score, 0.35f, 0.9f, _o.CommitBestImmediately));
    }

    private IEnumerator AnimateScore(int target, float delay, float duration, bool commitBest)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        int from = _displayed;
        if (from != target)
        {
            float t = 0f, nextTick = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                k = 1f - Mathf.Pow(1f - k, 3f); // OutCubic
                int v = Mathf.RoundToInt(Mathf.Lerp(from, target, k));
                if (v != _displayed)
                {
                    _displayed = v;
                    _scoreText.text = v.ToString();
                    if (t >= nextTick)
                    {
                        MiniGameJuice.Feedback(FeedbackType.CoinTick);
                        nextTick = t + 0.08f;
                    }
                }
                yield return null;
            }
            _displayed = target;
            _scoreText.text = target.ToString();
            MiniGameJuice.Punch(_scoreText.transform, 0.18f, 0.3f);
        }

        // звёзды
        int stars = StarsFor(target);
        while (_starsShown < stars)
        {
            PopStar(_starsShown);
            _starsShown++;
            yield return new WaitForSecondsRealtime(0.24f);
        }
        while (_starsShown > stars)
        {
            _starsShown--;
            HideStar(_starsShown);
        }

        if (commitBest)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            CommitBest(target);
        }
        _anim = null;
    }

    private int StarsFor(int score)
    {
        var th = _o.StarThresholds;
        if (th == null || _o.MaxScore <= 0) return 0;
        float frac = score / (float)_o.MaxScore;
        int n = 0;
        for (int i = 0; i < th.Length && i < 3; i++)
            if (frac >= th[i]) n++;
        return n;
    }

    private void PopStar(int i)
    {
        if (_starFills == null || i < 0 || i >= _starFills.Length) return;
        var f = _starFills[i].rectTransform;
        f.DOKill();
        f.localScale = Vector3.zero;
        f.localRotation = Quaternion.Euler(0f, 0f, -35f);
        f.DOScale(1f, 0.38f).SetEase(Ease.OutBack, 3f).SetUpdate(true).SetLink(gameObject);
        f.DOLocalRotate(Vector3.zero, 0.38f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);

        var slot = (RectTransform)f.parent;
        MiniGameJuice.Burst(_starsRow, slot.anchoredPosition, MiniGameTheme.Warning, 10, 70f, 14f, 0.5f);
        MiniGameJuice.Feedback(FeedbackType.Perfect);
    }

    private void HideStar(int i)
    {
        if (_starFills == null || i < 0 || i >= _starFills.Length) return;
        var f = _starFills[i].rectTransform;
        f.DOKill();
        f.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true).SetLink(gameObject);
    }

    private void ShowRecordBanner()
    {
        if (_banner == null) return;
        _banner.gameObject.SetActive(true);
        _banner.localScale = Vector3.zero;
        _banner.DOScale(1f, 0.45f).SetEase(Ease.OutBack, 2.5f).SetUpdate(true).SetLink(gameObject);
        _banner.DOPunchRotation(new Vector3(0f, 0f, 8f), 0.6f, 6).SetDelay(0.3f).SetUpdate(true)
            .SetLink(gameObject);
        MiniGameJuice.Burst(Card, new Vector2(0f, Card.sizeDelta.y * 0.5f + 4f), MiniGameTheme.Warning, 16, 130f, 18f, 0.7f);
        MiniGameJuice.Feedback(FeedbackType.Reward);
    }

    private void OnDestroy()
    {
        if (_anim != null) StopCoroutine(_anim);
    }
}
