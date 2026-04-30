using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SightsWindow : BaseWindow
{
    [SerializeField] private Transform _contentParent;
    [SerializeField] private TMP_Text _headerText;
    [SerializeField] private Button _backButton;

    [Header("ScrollView Animation")]
    [SerializeField] private RectTransform _scrollViewRect;
    [SerializeField] private float _slideUpOffset = 80f;
    [SerializeField] private float _animationDuration = 0.3f;

    public Transform ContentParent => _contentParent;

    public event Action OnBackClicked;
    public event Action OnWindowOpened;

    private Vector2 _scrollViewOriginalPos;
    private Tween _scrollTween;

    protected override void OnShow()
    {
        if (_scrollViewRect != null)
            _scrollViewOriginalPos = _scrollViewRect.anchoredPosition;

        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        OnWindowOpened?.Invoke();
        _headerText.text = _contentParent.childCount.ToString();
    }

    protected override void OnHide()
    {
        _backButton.onClick.RemoveAllListeners();
        ResetScrollViewImmediate();
        ClearSights();
    }

    public void PlayTabAnimation()
    {
        if (_scrollViewRect == null) return;

        _scrollTween?.Kill();

        _scrollViewRect.anchoredPosition = _scrollViewOriginalPos + new Vector2(0, -_slideUpOffset);

        _scrollTween = _scrollViewRect
            .DOAnchorPos(_scrollViewOriginalPos, _animationDuration)
            .SetEase(Ease.OutQuad);
    }

    private void ClearSights()
    {
        SightItemView[] sights = _contentParent.GetComponentsInChildren<SightItemView>();
        foreach (var sight in sights)
            Destroy(sight.gameObject);
    }

    private void ResetScrollViewImmediate()
    {
        if (_scrollViewRect == null) return;
        _scrollTween?.Kill();
        _scrollViewRect.anchoredPosition = _scrollViewOriginalPos;
    }
}
