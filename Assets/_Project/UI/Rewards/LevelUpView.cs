using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полноэкранный "НОВЫЙ УРОВЕНЬ": число переворачивается со старого уровня на новый, конфетти, бейдж очков прокачки.
/// </summary>
public class LevelUpView : MonoBehaviour
{
    private const float FlipDelay = 0.7f;

    private IFeedbackService _feedback;
    private Action _onClosed;
    private bool _closedInvoked;
    private bool _closing;
    private bool _flipped;

    private RectTransform _root;
    private CanvasGroup _rootGroup;
    private RectTransform _card;
    private float _cardScale = 1f;

    public static LevelUpView Show(Transform canvasTransform, int oldLevel, int newLevel, int statPointsGained,
        IFeedbackService feedback, Action onClosed)
    {
        var root = RewardUiFactory.CreateRoot(canvasTransform, "LevelUp", true, out var group);
        var view = root.gameObject.AddComponent<LevelUpView>();
        view._root = root;
        view._rootGroup = group;
        view._feedback = feedback;
        view._onClosed = onClosed;
        if (oldLevel <= 0 || oldLevel >= newLevel) oldLevel = Mathf.Max(0, newLevel - 1);
        view.Build(oldLevel, newLevel, statPointsGained);
        return view;
    }

    private void Build(int oldLevel, int newLevel, int statPointsGained)
    {
        var go = gameObject;

        var backdrop = RewardUiFactory.MakeBackdrop(_root, new Color(0.01f, 0.07f, 0.03f, 0f));
        var backdropBtn = backdrop.gameObject.AddComponent<Button>();
        backdropBtn.transition = Selectable.Transition.None;
        backdropBtn.onClick.AddListener(Close);
        backdrop.DOFade(0.8f, 0.3f).U(go);

        _cardScale = Mathf.Min(1f, _root.rect.width * 0.92f / 880f);

        var content = RewardUiFactory.MakeRect(_root, "Content", Vector2.zero, new Vector2(880f, 1100f));
        _card = content;
        _card.localScale = Vector3.one * 0.6f * _cardScale;
        _card.DOScale(_cardScale, 0.5f).SetEase(Ease.OutBack).U(go);

        // Лучи + свечение за числом
        var rays = RewardUiFactory.MakeImage(content, "Rays", RewardUiFactory.Rays, new Color(0.75f, 1f, 0.4f, 0f),
            new Vector2(0f, 40f), Vector2.one * 1400f);
        rays.DOFade(0.6f, 0.5f).U(go);
        rays.rectTransform.DORotate(new Vector3(0f, 0f, 360f), 16f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart).U(go);
        var glow = RewardUiFactory.MakeImage(content, "Glow", RewardUiFactory.SoftCircle, new Color(0.7f, 1f, 0.45f, 0f),
            new Vector2(0f, 40f), Vector2.one * 640f);
        glow.DOFade(0.5f, 0.5f).U(go);

        var header = RewardUiFactory.MakeText(content, "Header", "НОВЫЙ УРОВЕНЬ", 84f, FontStyles.Bold,
            new Color(1f, 0.85f, 0.3f), new Vector2(0f, 400f), new Vector2(860f, 120f));
        header.enableAutoSizing = true;
        header.fontSizeMin = 40f;
        header.fontSizeMax = 84f;
        header.transform.localScale = Vector3.zero;
        header.transform.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetDelay(0.15f).U(go);

        // Круглый бейдж уровня
        var badge = RewardUiFactory.MakeRect(content, "LevelBadge", new Vector2(0f, 40f), Vector2.one * 420f);
        RewardUiFactory.MakeImage(badge, "Ring", RewardUiFactory.SoftCircle, new Color(1f, 0.8f, 0.3f), Vector2.zero, Vector2.one * 420f);
        RewardUiFactory.MakeImage(badge, "Fill", RewardUiFactory.SoftCircle, new Color(0.20f, 0.56f, 0.28f), Vector2.zero, Vector2.one * 370f);
        RewardUiFactory.MakeImage(badge, "Shine", RewardUiFactory.SoftCircle, new Color(1f, 1f, 1f, 0.18f), new Vector2(-60f, 70f), Vector2.one * 150f);

        var number = RewardUiFactory.MakeText(badge, "Number", oldLevel.ToString(), 210f, FontStyles.Bold, Color.white,
            Vector2.zero, new Vector2(400f, 300f));
        number.enableAutoSizing = true;
        number.fontSizeMin = 80f;
        number.fontSizeMax = 210f;
        var numberRt = number.rectTransform;

        // Бейдж очков прокачки
        RectTransform pointsRt = null;
        if (statPointsGained > 0)
        {
            var pill = RewardUiFactory.MakeImage(content, "PointsBadge", RewardUiFactory.Rounded, new Color(0.3f, 0.8f, 0.4f),
                new Vector2(0f, -250f), new Vector2(560f, 96f));
            RewardUiFactory.MakeText(pill.transform, "Label", $"+{statPointsGained} очков прокачки", 42f, FontStyles.Bold,
                Color.white, Vector2.zero, new Vector2(540f, 90f));
            pointsRt = pill.rectTransform;
            pointsRt.localScale = Vector3.zero;
        }

        var btn = RewardUiFactory.MakeButton(content, "OkButton", "Отлично!", new Color(1f, 0.72f, 0.2f), new Color(0.2f, 0.1f, 0.05f),
            new Vector2(0f, -420f), new Vector2(440f, 124f), 50f);
        btn.onClick.AddListener(Close);
        var btnRt = (RectTransform)btn.transform;
        btnRt.localScale = Vector3.zero;

        // Переворот числа старый → новый
        var flip = DOTween.Sequence();
        flip.AppendInterval(FlipDelay);
        flip.Append(numberRt.DOScaleY(0f, 0.16f).SetEase(Ease.InQuad));
        flip.AppendCallback(() =>
        {
            number.text = newLevel.ToString();
            _flipped = true;
            _feedback?.Play(FeedbackType.LevelUp);
            if (_root != null) RewardUiFactory.ConfettiBurst(_root, new Vector2(0f, 80f * _cardScale), 40, _cardScale);
        });
        flip.Append(numberRt.DOScaleY(1f, 0.22f).SetEase(Ease.OutBack));
        flip.Append(badge.DOPunchScale(Vector3.one * 0.25f, 0.45f, 7, 0.6f));
        if (pointsRt != null)
            flip.Join(pointsRt.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
        flip.Append(btnRt.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
        flip.SetUpdate(true).SetLink(go);

        btnRt.DOScale(1.06f, 0.6f).SetEase(Ease.InOutSine).SetDelay(FlipDelay + 1.4f)
            .SetLoops(-1, LoopType.Yoyo).U(go);
    }

    public void Close()
    {
        if (_closing || !_flipped || this == null) return;
        _closing = true;

        var go = gameObject;
        _rootGroup.blocksRaycasts = false;
        _rootGroup.DOFade(0f, 0.3f).U(go);

        DOVirtual.DelayedCall(0.32f, () =>
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
        InvokeClosed();
    }
}
