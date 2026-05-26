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
    [SerializeField] private Button _collectRewardsButton;

    [Header("ScrollView Animation")]
    [SerializeField] private RectTransform _scrollViewRect;
    [SerializeField] private float _slideUpOffset = 80f;
    [SerializeField] private float _animationDuration = 0.3f;

    [Header("Performance")]
    [Tooltip("Ссылка на ScrollRect для отключения Elastic во время прокрутки (уменьшает нагрузку)")]
    [SerializeField] private ScrollRect _scrollRect;

    public Transform ContentParent => _contentParent;

    public event Action OnBackClicked;
    public event Action OnCollectRewardsClicked;
    public event Action OnWindowOpened;

    private Vector2 _scrollViewOriginalPos;
    private Tween _scrollTween;

    protected override void OnShow()
    {
        if (_scrollViewRect != null)
            _scrollViewOriginalPos = _scrollViewRect.anchoredPosition;

        _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
        _collectRewardsButton?.onClick.AddListener(() => OnCollectRewardsClicked?.Invoke());

        if (_scrollRect != null)
        {
            // Clamped дешевле Elastic — нет пересчёта позиции за границей
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // RectMask2D дешевле Mask: нет стенсил-буфера, clip на CPU
            var viewport = _scrollRect.viewport;
            if (viewport != null)
            {
                var oldMask = viewport.GetComponent<Mask>();
                if (oldMask != null && viewport.GetComponent<RectMask2D>() == null)
                {
                    // Прячем Image-заглушку маски (она прозрачная, нужна только стенсилу)
                    var maskImage = viewport.GetComponent<Image>();
                    if (maskImage != null && maskImage.color.a < 0.01f)
                        maskImage.enabled = false;

                    oldMask.enabled = false;
                    viewport.gameObject.AddComponent<RectMask2D>();
                }
            }
        }

        OnWindowOpened?.Invoke();
        _headerText.text = _contentParent.childCount.ToString();
    }

    protected override void OnHide()
    {
        _backButton.onClick.RemoveAllListeners();
        _collectRewardsButton?.onClick.RemoveAllListeners();
        ResetScrollViewImmediate();
        ClearSights();
    }

    // ── batch-заполнение списка ───────────────────────────────────────────────
    // Отключаем LayoutGroup на время добавления айтемов, чтобы не было
    // O(n²) пересчётов (один Rebuild на каждый SetParent/Destroy).
    // После EndBatchAdd выполняется единственный ForceRebuild.

    private LayoutGroup      _cachedLayoutGroup;
    private ContentSizeFitter _cachedSizeFitter;

    public void BeginBatchAdd()
    {
        if (_cachedLayoutGroup == null)
            _cachedLayoutGroup = _contentParent.GetComponent<LayoutGroup>();
        if (_cachedSizeFitter == null)
            _cachedSizeFitter = _contentParent.GetComponent<ContentSizeFitter>();

        if (_cachedLayoutGroup != null)  _cachedLayoutGroup.enabled  = false;
        if (_cachedSizeFitter  != null)  _cachedSizeFitter.enabled   = false;
    }

    public void EndBatchAdd()
    {
        if (_cachedSizeFitter  != null)  _cachedSizeFitter.enabled   = true;
        if (_cachedLayoutGroup != null)  _cachedLayoutGroup.enabled  = true;

        if (_contentParent is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
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
