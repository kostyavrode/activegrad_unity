using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PromoCodesWindow : BaseWindow
{
    [SerializeField] private Button _backButton;
    [SerializeField] private Transform _content;

    [Header("ScrollView Animation")]
    [SerializeField] private RectTransform _scrollViewRect;
    [SerializeField] private float _slideUpOffset = 80f;
    [SerializeField] private float _animationDuration = 0.3f;

    public Transform ResultContent => _content;

    public event Action OnBackClicked;
    public event Action OnWindowOpened;
    public event Action OnWindowClosed;

    private Vector2 _scrollViewOriginalPos;
    private Tween _scrollTween;
    private bool _scrollOriginInitialized;

    protected override void OnShow()
    {
        UIScrollListAnimations.PrepareForShow(
            _scrollViewRect,
            ref _scrollViewOriginalPos,
            ref _scrollOriginInitialized,
            ref _scrollTween,
            _content);

        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        OnWindowOpened?.Invoke();
    }

    protected override void OnHide()
    {
        OnWindowClosed?.Invoke();
        _backButton.onClick.RemoveAllListeners();
        ResetScrollViewImmediate();
        UIListEntranceHelper.Kill(_content);
    }

    public void PlayTabAnimation()
    {
        _scrollTween = UIScrollListAnimations.PlaySlideUpWithStagger(
            _scrollViewRect,
            _scrollViewOriginalPos,
            _slideUpOffset,
            _animationDuration,
            _content,
            _scrollTween);
    }

    private void ResetScrollViewImmediate()
    {
        UIScrollListAnimations.PrepareForShow(
            _scrollViewRect,
            ref _scrollViewOriginalPos,
            ref _scrollOriginInitialized,
            ref _scrollTween,
            _content);
    }
}
