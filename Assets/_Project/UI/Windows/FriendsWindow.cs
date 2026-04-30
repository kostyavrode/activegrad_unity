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

    protected override void OnShow()
    {
        if (_scrollViewRect != null)
            _scrollViewOriginalPos = _scrollViewRect.anchoredPosition;

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
        if (_scrollViewRect == null) return;

        _scrollTween?.Kill();

        // Мгновенно ставим чуть ниже цели, затем поднимаем вверх
        _scrollViewRect.anchoredPosition = _scrollViewOriginalPos + new Vector2(0, -_slideUpOffset);

        _scrollTween = _scrollViewRect
            .DOAnchorPos(_scrollViewOriginalPos, _animationDuration)
            .SetEase(Ease.OutQuad);
    }

    public void ClearContent()
    {
        if (_content == null) return;

        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);
    }

    private void ResetScrollViewImmediate()
    {
        if (_scrollViewRect == null) return;
        _scrollTween?.Kill();
        _scrollViewRect.anchoredPosition = _scrollViewOriginalPos;
    }
}

