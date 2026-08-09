using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopWindow : BaseWindow
{
    [SerializeField] private Transform _contentParent;
    [SerializeField] private Button _backButton;
    [SerializeField] private TMP_Text _coinsText;

    [Header("ScrollView Animation")]
    [SerializeField] private RectTransform _scrollViewRect;
    [SerializeField] private float _slideUpOffset = 80f;
    [SerializeField] private float _animationDuration = 0.3f;

    public Transform ContentParent => _contentParent;

    public event Action OnBackClicked;
    public event Action OnWindowOpened;

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
            _contentParent);

        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        OnWindowOpened?.Invoke();
    }

    protected override void OnHide()
    {
        _backButton.onClick.RemoveAllListeners();
        ResetScrollViewImmediate();
        UIListEntranceHelper.Kill(_contentParent);
        UIListStatePresenter.HideFor(_contentParent);
    }

    public void PlayTabAnimation()
    {
        _scrollTween = UIScrollListAnimations.PlaySlideUpWithStagger(
            _scrollViewRect,
            _scrollViewOriginalPos,
            _slideUpOffset,
            _animationDuration,
            _contentParent,
            _scrollTween);
    }

    public void UpdateCoinsDisplay(int coins)
    {
        if (_coinsText != null)
            _coinsText.text = coins.ToString();
    }

    private void ResetScrollViewImmediate()
    {
        UIScrollListAnimations.PrepareForShow(
            _scrollViewRect,
            ref _scrollViewOriginalPos,
            ref _scrollOriginInitialized,
            ref _scrollTween,
            _contentParent);
    }
}
