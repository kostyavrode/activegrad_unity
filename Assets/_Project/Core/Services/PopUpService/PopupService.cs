using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PopupService : IPopupService
{
    private const int MaxVisible = 2;
    private const float Spacing = 12f;
    private const float FallbackHeight = 120f;
    private const float QueuePumpDelay = 0.05f;

    private struct PendingPopup
    {
        public string Message;
        public Color Color;
        public FeedbackType Feedback;
    }

    private readonly PopupView.Factory _popupFactory;
    private readonly IFeedbackService _feedbackService;
    private readonly Queue<PendingPopup> _pending = new Queue<PendingPopup>();
    // Newest first.
    private readonly List<PopupView> _visible = new List<PopupView>();
    private Canvas _uiCanvas;
    private Tween _pumpTween;

    public PopupService(PopupView.Factory popupFactory, [InjectOptional] IFeedbackService feedbackService)
    {
        _popupFactory = popupFactory;
        _feedbackService = feedbackService;
        _uiCanvas = GameObject.FindObjectOfType<Canvas>();
    }

    public void ShowError(string message)
    {
        Enqueue(message, Color.red, FeedbackType.Error);
    }

    public void ShowInfo(string message)
    {
        Enqueue(message, Color.blue, FeedbackType.Tap);
    }

    public void ShowSuccess(string message)
    {
        Enqueue(message, Color.green, FeedbackType.Success);
    }

    private void Enqueue(string message, Color color, FeedbackType feedback)
    {
        _pending.Enqueue(new PendingPopup { Message = message, Color = color, Feedback = feedback });
        Pump();
    }

    private void Pump()
    {
        _visible.RemoveAll(p => p == null);

        while (_pending.Count > 0 && _visible.Count < MaxVisible)
        {
            var next = _pending.Dequeue();
            if (!CreatePopup(next))
            {
                _pending.Clear();
                return;
            }
        }
    }

    private bool CreatePopup(PendingPopup data)
    {
        if (_uiCanvas == null)
        {
            _uiCanvas = GameObject.FindObjectOfType<Canvas>();
        }

        if (_uiCanvas == null)
        {
            Debug.LogWarning("[PopupService] No Canvas found, popup dropped: " + data.Message);
            return false;
        }

        var popup = _popupFactory.Create();
        popup.transform.SetParent(_uiCanvas.transform, false);

        var rect = popup.transform as RectTransform;
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        popup.Closed += OnPopupClosed;
        _visible.Insert(0, popup);

        popup.Setup(data.Message, data.Color);
        Relayout(popup);

        _feedbackService?.Play(data.Feedback);
        return true;
    }

    private void Relayout(PopupView justCreated = null)
    {
        var offset = 0f;
        for (var i = 0; i < _visible.Count; i++)
        {
            var popup = _visible[i];
            if (popup == null)
                continue;

            // The newly created popup is always at index 0 (offset 0, already passed via Setup);
            // touching it here would override its slide-in start position.
            if (popup != justCreated)
                popup.SetStackOffset(offset);

            var height = popup.Height;
            if (height <= 1f)
                height = FallbackHeight;

            offset += height + Spacing;
        }
    }

    private void OnPopupClosed(PopupView popup)
    {
        if (popup != null)
            popup.Closed -= OnPopupClosed;

        _visible.Remove(popup);
        _visible.RemoveAll(p => p == null);
        Relayout();

        if (_pending.Count == 0)
            return;

        // Deferred so we never instantiate from inside OnDestroy (e.g. during scene unload).
        _pumpTween?.Kill();
        _pumpTween = DOVirtual.DelayedCall(QueuePumpDelay, () =>
        {
            _pumpTween = null;
            Pump();
        }).SetUpdate(true);
    }
}
