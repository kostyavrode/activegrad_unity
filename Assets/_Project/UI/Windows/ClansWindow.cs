using System;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ClansWindow : BaseWindow
{
    [SerializeField] private Button _backButton;
    [SerializeField] private Transform _content;
    [SerializeField] private Button _topClansTabButton;
    [SerializeField] private Button _searchTabButton;
    [SerializeField] private Button _myClanTabButton;
    [SerializeField] private Button _createClanTabButton;
    [SerializeField] private GameObject _searchPanel;
    [SerializeField] private TMP_InputField _searchInputField;
    [SerializeField] private Button _searchButton;

    [Header("ScrollView Animation")]
    [SerializeField] private RectTransform _scrollViewRect;
    [SerializeField] private float _searchScrollOffset = 400f;
    [SerializeField] private float _slideUpOffset = 80f;
    [SerializeField] private float _animationDuration = 0.3f;

    public Transform Content => _content;
    public TMP_InputField SearchInputField => _searchInputField;

    public event Action OnBackClicked;
    public event Action OnWindowOpened;
    public event Action OnTopClansTabClicked;
    public event Action OnSearchTabClicked;
    public event Action OnMyClanTabClicked;
    public event Action OnCreateClanTabClicked;
    public event Action<string> OnSearchClicked;

    private Vector2 _scrollViewOriginalPos;
    private Tween _scrollTween;

    protected override void OnShow()
    {
        if (_scrollViewRect != null)
            _scrollViewOriginalPos = _scrollViewRect.anchoredPosition;

        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        _topClansTabButton.onClick.AddListener(() => OnTopClansTabClicked?.Invoke());
        _searchTabButton.onClick.AddListener(() => OnSearchTabClicked?.Invoke());
        _myClanTabButton.onClick.AddListener(() => OnMyClanTabClicked?.Invoke());
        _createClanTabButton.onClick.AddListener(() => OnCreateClanTabClicked?.Invoke());

        if (_searchButton != null)
            _searchButton.onClick.AddListener(() => OnSearchClicked?.Invoke(_searchInputField?.text ?? ""));

        OnWindowOpened?.Invoke();
    }

    protected override void OnHide()
    {
        _backButton.onClick.RemoveAllListeners();
        _topClansTabButton.onClick.RemoveAllListeners();
        _searchTabButton.onClick.RemoveAllListeners();
        _myClanTabButton.onClick.RemoveAllListeners();
        _createClanTabButton.onClick.RemoveAllListeners();

        if (_searchButton != null)
            _searchButton.onClick.RemoveAllListeners();

        ResetScrollViewImmediate();
        SetCreateClanButtonActive(true);
        ClearContent();
    }

    // ── Публичные методы для медиатора ───────────────────────────────────────

    public void SetCreateClanButtonActive(bool active)
    {
        if (_createClanTabButton != null)
            _createClanTabButton.gameObject.SetActive(active);
    }

    // isSearchTab: true — целевая позиция со смещением вниз на _searchScrollOffset
    public void PlayTabAnimation(bool isSearchTab)
    {
        if (_scrollViewRect == null) return;

        _scrollTween?.Kill();

        Vector2 target = isSearchTab
            ? _scrollViewOriginalPos + new Vector2(0, -_searchScrollOffset)
            : _scrollViewOriginalPos;

        // Мгновенно ставим чуть ниже цели, затем поднимаем вверх
        _scrollViewRect.anchoredPosition = target + new Vector2(0, -_slideUpOffset);

        _scrollTween = _scrollViewRect
            .DOAnchorPos(target, _animationDuration)
            .SetEase(Ease.OutQuad);
    }

    public void ShowSearchPanel(bool show)
    {
        if (_searchPanel != null)
            _searchPanel.SetActive(show);
    }

    public void ClearContent()
    {
        if (_content == null) return;

        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);
    }

    // ── Приватные ────────────────────────────────────────────────────────────

    private void ResetScrollViewImmediate()
    {
        if (_scrollViewRect == null) return;
        _scrollTween?.Kill();
        _scrollViewRect.anchoredPosition = _scrollViewOriginalPos;
    }
}
