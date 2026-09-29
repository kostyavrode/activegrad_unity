using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Zenject;

/// <summary>
/// Показывает экраны наград по очереди (один за раз), строит их из кода на канвасе с тегом "Canvas".
/// </summary>
public class RewardService : IRewardService
{
    private readonly IFeedbackService _feedback;
    private readonly Queue<Action<Action>> _queue = new Queue<Action<Action>>();
    private bool _busy;

    public RewardService([InjectOptional] IFeedbackService feedbackService = null)
    {
        _feedback = feedbackService;
    }

    public void ShowQuestComplete(string title, IReadOnlyList<RewardEntry> rewards)
    {
        var copy = Copy(rewards);
        Enqueue(done =>
        {
            var canvas = RewardUiFactory.FindCanvasTransform();
            if (canvas == null) { done(); return; }
            RewardCeremonyView.ShowQuest(canvas, title, copy, _feedback, done);
        });
    }

    public void ShowLevelUp(int oldLevel, int newLevel, int statPointsGained)
    {
        Enqueue(done =>
        {
            var canvas = RewardUiFactory.FindCanvasTransform();
            if (canvas == null) { done(); return; }
            LevelUpView.Show(canvas, oldLevel, newLevel, statPointsGained, _feedback, done);
        });
    }

    public void ShowResources(string header, IReadOnlyList<RewardEntry> rewards)
    {
        var copy = Copy(rewards);
        Enqueue(done =>
        {
            var canvas = RewardUiFactory.FindCanvasTransform();
            if (canvas == null) { done(); return; }
            RewardCeremonyView.ShowCompact(canvas, header, copy, _feedback, done);
        });
    }

    public void FlyCurrency(RewardEntry entry, Vector2 fromScreenPos, RectTransform target = null)
    {
        CurrencyFlyEffect.FlyFromScreen(entry, fromScreenPos, target, _feedback);
    }

    private static List<RewardEntry> Copy(IReadOnlyList<RewardEntry> rewards)
    {
        var list = new List<RewardEntry>();
        if (rewards != null)
            for (int i = 0; i < rewards.Count; i++) list.Add(rewards[i]);
        return list;
    }

    private void Enqueue(Action<Action> show)
    {
        _queue.Enqueue(show);
        TryShowNext();
    }

    private void TryShowNext()
    {
        if (_busy || _queue.Count == 0) return;
        _busy = true;

        var show = _queue.Dequeue();
        bool finished = false;
        Action done = () =>
        {
            if (finished) return;
            finished = true;
            _busy = false;
            // Следующий показ — с небольшой паузой и вне OnDestroy (при выгрузке сцены нельзя создавать объекты)
            DOVirtual.DelayedCall(0.15f, TryShowNext, true);
        };

        try
        {
            show(done);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RewardService] Failed to show reward view: {e}");
            done();
        }
    }
}
