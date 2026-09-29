using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Экран награды. Два режима:
///  - Quest: полноэкранная церемония (лучи, карточка, конфетти, кнопка "Забрать");
///  - Compact: лёгкая плашка снизу, авто-закрытие через 2.2 с (или по тапу).
/// По закрытию иконки улетают в HUD (CurrencyFlyEffect).
/// </summary>
public class RewardCeremonyView : MonoBehaviour
{
    private const float CompactAutoCloseSec = 2.2f;
    private const float MinTimeBeforeClaim = 0.35f;

    private enum Mode { Quest, Compact }

    private Mode _mode;
    private IFeedbackService _feedback;
    private Action _onClosed;
    private bool _closedInvoked;
    private bool _claimed;
    private float _openTime;

    private RectTransform _root;
    private CanvasGroup _rootGroup;
    private Image _backdrop;
    private RectTransform _card;
    private CanvasGroup _cardGroup;
    private RectTransform _rays;
    private float _cardScale = 1f;
    private float _compactHiddenY;
    private readonly List<KeyValuePair<RectTransform, RewardEntry>> _icons = new List<KeyValuePair<RectTransform, RewardEntry>>();

    // ─────────────────────────── API ───────────────────────────

    public static RewardCeremonyView ShowQuest(Transform canvasTransform, string questTitle, IReadOnlyList<RewardEntry> rewards,
        IFeedbackService feedback, Action onClosed)
    {
        var view = Create(canvasTransform, "QuestComplete", Mode.Quest, feedback, onClosed);
        view.BuildQuest(questTitle, rewards);
        return view;
    }

    public static RewardCeremonyView ShowCompact(Transform canvasTransform, string header, IReadOnlyList<RewardEntry> rewards,
        IFeedbackService feedback, Action onClosed)
    {
        var view = Create(canvasTransform, "Resources", Mode.Compact, feedback, onClosed);
        view.BuildCompact(header, rewards);
        return view;
    }

    private static RewardCeremonyView Create(Transform canvasTransform, string name, Mode mode, IFeedbackService feedback, Action onClosed)
    {
        var root = RewardUiFactory.CreateRoot(canvasTransform, name, true, out var group);
        var view = root.gameObject.AddComponent<RewardCeremonyView>();
        view._root = root;
        view._rootGroup = group;
        view._mode = mode;
        view._feedback = feedback;
        view._onClosed = onClosed;
        view._openTime = Time.unscaledTime;
        return view;
    }

    // ─────────────────────────── Quest ───────────────────────────

    private void BuildQuest(string questTitle, IReadOnlyList<RewardEntry> rewards)
    {
        var go = gameObject;

        _backdrop = RewardUiFactory.MakeBackdrop(_root, new Color(0f, 0f, 0f, 0f));
        AddClaimClick(_backdrop.gameObject);
        _backdrop.DOFade(0.75f, 0.3f).U(go);

        const float cardW = 880f, cardH = 700f;
        _cardScale = Mathf.Min(1f, _root.rect.width * 0.92f / cardW);

        // Лучи и свечение за карточкой
        _rays = RewardUiFactory.MakeImage(_root, "Rays", RewardUiFactory.Rays, new Color(1f, 0.85f, 0.35f, 0f),
            new Vector2(0f, 230f * _cardScale), Vector2.one * 1500f * _cardScale).rectTransform;
        var raysImg = _rays.GetComponent<Image>();
        _rays.localScale = Vector3.one * 0.3f;
        _rays.DOScale(1f, 0.6f).SetEase(Ease.OutBack).U(go);
        raysImg.DOFade(0.55f, 0.4f).U(go);
        _rays.DORotate(new Vector3(0f, 0f, -360f), 14f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).U(go);

        var glow = RewardUiFactory.MakeImage(_root, "Glow", RewardUiFactory.SoftCircle, new Color(1f, 0.9f, 0.5f, 0f),
            new Vector2(0f, 230f * _cardScale), Vector2.one * 700f * _cardScale);
        glow.DOFade(0.45f, 0.5f).U(go);

        // Карточка
        var cardImg = RewardUiFactory.MakeImage(_root, "Card", RewardUiFactory.Rounded, new Color(0.07f, 0.24f, 0.13f, 0.97f),
            Vector2.zero, new Vector2(cardW, cardH), raycast: true);
        _card = cardImg.rectTransform;
        _cardGroup = _card.gameObject.AddComponent<CanvasGroup>();
        _cardGroup.alpha = 0f;
        _card.localScale = Vector3.one * 0.6f * _cardScale;
        _card.DOScale(_cardScale, 0.5f).SetEase(Ease.OutBack).U(go);
        _cardGroup.DOFade(1f, 0.2f).U(go);

        RewardUiFactory.MakeImage(_card, "TopStripe", RewardUiFactory.Rounded, new Color(1f, 0.78f, 0.22f),
            new Vector2(0f, cardH * 0.5f - 12f), new Vector2(cardW - 60f, 10f));

        var header = RewardUiFactory.MakeText(_card, "Header", "Квест выполнен!", 64f, FontStyles.Bold,
            new Color(1f, 0.84f, 0.3f), new Vector2(0f, 250f), new Vector2(cardW - 60f, 90f));
        header.transform.localScale = Vector3.zero;
        header.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetDelay(0.2f).U(go);

        var titleText = RewardUiFactory.MakeText(_card, "QuestTitle", questTitle ?? "", 38f, FontStyles.Normal,
            new Color(1f, 1f, 1f, 0.9f), new Vector2(0f, 165f), new Vector2(cardW - 100f, 90f));
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 22f;
        titleText.fontSizeMax = 38f;

        BuildIconRow(_card, rewards, new Vector2(0f, 10f), 150f, 210f, cardW - 80f, 0.4f);

        var btn = RewardUiFactory.MakeButton(_card, "ClaimButton", "Забрать", new Color(0.3f, 0.8f, 0.4f), Color.white,
            new Vector2(0f, -250f), new Vector2(420f, 120f), 48f);
        btn.onClick.AddListener(Claim);
        var btnRt = (RectTransform)btn.transform;
        btnRt.localScale = Vector3.zero;
        float btnDelay = 0.5f + rewards.Count * 0.12f;
        btnRt.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(btnDelay).U(go);
        btnRt.DOScale(1.06f, 0.6f).SetEase(Ease.InOutSine).SetDelay(btnDelay + 0.4f)
            .SetLoops(-1, LoopType.Yoyo).U(go);

        // Конфетти чуть позже появления карточки
        DOVirtual.DelayedCall(0.25f, () =>
        {
            if (_root != null) RewardUiFactory.ConfettiBurst(_root, new Vector2(0f, 150f * _cardScale), 40, _cardScale);
        }, true).SetLink(go);

        _feedback?.Play(FeedbackType.Reward);
    }

    // ─────────────────────────── Compact ───────────────────────────

    private void BuildCompact(string headerText, IReadOnlyList<RewardEntry> rewards)
    {
        var go = gameObject;

        _backdrop = RewardUiFactory.MakeBackdrop(_root, new Color(0f, 0f, 0f, 0f));
        AddClaimClick(_backdrop.gameObject);
        _backdrop.DOFade(0.35f, 0.25f).U(go);

        const float cardW = 900f, cardH = 360f;
        _cardScale = Mathf.Min(1f, _root.rect.width * 0.94f / cardW);

        var cardImg = RewardUiFactory.MakeImage(_root, "Card", RewardUiFactory.Rounded, new Color(0.07f, 0.24f, 0.13f, 0.97f),
            Vector2.zero, new Vector2(cardW, cardH), raycast: true);
        AddClaimClick(cardImg.gameObject);
        _card = cardImg.rectTransform;
        _cardGroup = _card.gameObject.AddComponent<CanvasGroup>();
        _card.localScale = Vector3.one * _cardScale;

        float halfH = _root.rect.height * 0.5f;
        float shownY = -halfH + 180f + cardH * 0.5f * _cardScale;
        _compactHiddenY = -halfH - cardH * _cardScale;
        _card.anchoredPosition = new Vector2(0f, _compactHiddenY);
        _card.DOAnchorPos(new Vector2(0f, shownY), 0.45f).SetEase(Ease.OutBack).U(go);

        RewardUiFactory.MakeImage(_card, "TopStripe", RewardUiFactory.Rounded, new Color(0.3f, 0.8f, 0.4f),
            new Vector2(0f, cardH * 0.5f - 12f), new Vector2(cardW - 60f, 10f));

        RewardUiFactory.MakeText(_card, "Header", headerText ?? "", 48f, FontStyles.Bold,
            new Color(1f, 0.86f, 0.35f), new Vector2(0f, 120f), new Vector2(cardW - 60f, 70f));

        BuildIconRow(_card, rewards, new Vector2(0f, -10f), 120f, 190f, cardW - 60f, 0.25f);

        _feedback?.Play(FeedbackType.Success);

        DOVirtual.DelayedCall(CompactAutoCloseSec, Claim, true).SetLink(go);
    }

    // ─────────────────────────── общие части ───────────────────────────

    private void BuildIconRow(RectTransform parent, IReadOnlyList<RewardEntry> rewards, Vector2 center, float iconSize,
        float spacing, float maxWidth, float startDelay)
    {
        int n = rewards != null ? rewards.Count : 0;
        if (n == 0) return;

        if (spacing * (n - 1) + iconSize > maxWidth)
        {
            float k = maxWidth / (spacing * (n - 1) + iconSize);
            spacing *= k;
            iconSize *= k;
        }

        var go = gameObject;
        float startX = -spacing * (n - 1) * 0.5f;
        for (int i = 0; i < n; i++)
        {
            var entry = rewards[i];
            var icon = RewardUiFactory.MakeRewardIcon(parent, entry, center + new Vector2(startX + spacing * i, 0f), iconSize);
            icon.localScale = Vector3.zero;
            float delay = startDelay + i * 0.12f;
            icon.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetDelay(delay)
                .OnStart(() => _feedback?.Play(FeedbackType.CoinTick))
                .U(go);
            icon.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-25f, 25f));
            icon.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutBack).SetDelay(delay).U(go);
            _icons.Add(new KeyValuePair<RectTransform, RewardEntry>(icon, entry));
        }
    }

    private void AddClaimClick(GameObject target)
    {
        var btn = target.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(Claim);
    }

    public void Claim()
    {
        if (_claimed || this == null) return;
        if (Time.unscaledTime - _openTime < MinTimeBeforeClaim) return;
        _claimed = true;

        var go = gameObject;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.interactable = false;

        // Иконки → полёт в HUD
        float flyDuration = 0f;
        foreach (var kv in _icons)
        {
            var icon = kv.Key;
            if (icon == null) continue;
            Vector2 local = _root.InverseTransformPoint(icon.position);
            icon.DOKill();
            icon.DOScale(0f, 0.15f).SetEase(Ease.InQuad).U(icon.gameObject);
            if (kv.Value.Amount > 0)
                flyDuration = Mathf.Max(flyDuration, CurrencyFlyEffect.Fly(_root, kv.Value, local, null, _feedback, 0.05f));
        }

        // Закрытие (без затемнения всего корня — летящие иконки должны оставаться видимыми)
        if (_card != null)
        {
            // Закрытие — только растворение, без «подпрыга»
            _card.DOKill();
            if (_cardGroup != null)
                _cardGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad).U(go);
        }
        if (_rays != null)
        {
            var raysImage = _rays.GetComponent<Image>();
            if (raysImage != null)
                raysImage.DOFade(0f, 0.3f).U(go);
        }
        foreach (Transform child in _root)
        {
            if (child.name == "Glow")
            {
                var g = child.GetComponent<Image>();
                if (g != null) g.DOFade(0f, 0.25f).U(go);
            }
        }
        if (_backdrop != null)
            _backdrop.DOFade(0f, 0.35f).U(go);

        float closeAfter = Mathf.Max(0.4f, flyDuration + 0.05f);
        DOVirtual.DelayedCall(closeAfter, () =>
        {
            InvokeClosed();
            if (this != null) Destroy(gameObject);
        }, true).SetLink(go);
    }

    private void InvokeClosed()
    {
        if (_closedInvoked) return;
        _closedInvoked = true;
        _onClosed?.Invoke();
    }

    private void OnDestroy()
    {
        // Сцена выгружена/объект уничтожен снаружи — не блокируем очередь RewardService
        InvokeClosed();
    }
}
