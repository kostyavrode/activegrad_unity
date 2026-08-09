using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FriendsWindow : BaseWindow
{
    [SerializeField] private Button _backButton;
    [SerializeField] private Transform _content;
    [SerializeField] private UIGroupSelector _tabSelector;

    [Header("ScrollView Animation")]
    [SerializeField] private RectTransform _scrollViewRect;
    [SerializeField] private float _slideUpOffset = 80f;
    [SerializeField] private float _animationDuration = 0.3f;

    public Transform Content => _content;

    public event Action OnBackClicked;
    public event Action OnWindowOpened;
    public event Action OnFriendsTabClicked;
    public event Action OnPendingTabClicked;
    public event Action OnSentTabClicked;

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

        if (_tabSelector != null)
            _tabSelector.OnItemSelected += HandleTabSelected;

        OnWindowOpened?.Invoke();
    }

    protected override void OnHide()
    {
        _backButton.onClick.RemoveAllListeners();

        if (_tabSelector != null)
            _tabSelector.OnItemSelected -= HandleTabSelected;

        ResetScrollViewImmediate();
        UIListEntranceHelper.Kill(_content);
        UIListStatePresenter.HideFor(_content);
        ClearContent();
    }

    private void HandleTabSelected(int index)
    {
        switch (index)
        {
            case 0: OnFriendsTabClicked?.Invoke(); break;
            case 1: OnPendingTabClicked?.Invoke(); break;
            case 2: OnSentTabClicked?.Invoke(); break;
        }
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

    public void ClearContent()
    {
        if (_content == null) return;

        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);
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

