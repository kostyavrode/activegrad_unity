using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIModalAnimator : MonoBehaviour
{
    private const float ShowDuration = 0.3f;
    private const float HideDuration = 0.18f;
    private const float BackdropAlpha = 0.6f;
    private const float ShowStartScale = 0.9f;

    [Tooltip("Карточка, которая «прыгает». Пусто — подбирается автоматически, полноэкранные фоны только растворяются.")]
    [SerializeField] private RectTransform _contentPanel;
    [SerializeField] private CanvasGroup _backdrop;

    private CanvasGroup _rootGroup;
    private List<UIAnimTargets.Target> _targets;
    private Sequence _activeSequence;
    private bool _isClosing;
    private bool _isShown;

    public bool IsClosing => _isClosing;

    private void Start()
    {
        PlayShow();
    }

    private void OnDestroy()
    {
        KillSequence();
        DestroyBackdrop();
    }

    private void KillSequence()
    {
        if (_activeSequence != null)
        {
            _activeSequence.Kill();
            _activeSequence = null;
        }
    }

    public void PlayShow()
    {
        if (_isShown)
            return;

        _isShown = true;
        EnsureSetup();
        _isClosing = false;
        KillSequence();

        var sequence = DOTween.Sequence().SetUpdate(true);

        _rootGroup.alpha = 0f;
        sequence.Join(_rootGroup.DOFade(1f, ShowDuration * 0.7f).SetEase(Ease.OutQuad));

        foreach (var t in _targets)
        {
            if (t.Rect == null)
                continue;

            t.Rect.localScale = t.Scale * ShowStartScale;
            sequence.Join(t.Rect.DOScale(t.Scale, ShowDuration).SetEase(Ease.OutBack, 1.2f));
        }

        if (_backdrop != null)
        {
            _backdrop.alpha = 0f;
            _backdrop.gameObject.SetActive(true);
            sequence.Join(_backdrop.DOFade(1f, ShowDuration).SetEase(Ease.OutQuad));
        }

        _activeSequence = sequence;
    }

    public void PlayHide(Action onComplete)
    {
        EnsureSetup();

        if (_isClosing)
            return;

        _isClosing = true;
        KillSequence();

        var sequence = DOTween.Sequence().SetUpdate(true);
        // Закрытие — только растворение, без масштаба
        sequence.Join(_rootGroup.DOFade(0f, HideDuration).SetEase(Ease.InQuad));

        if (_backdrop != null)
            sequence.Join(_backdrop.DOFade(0f, HideDuration).SetEase(Ease.InQuad));

        sequence.OnComplete(() =>
        {
            _activeSequence = null;
            DestroyBackdrop();
            onComplete?.Invoke();
        });

        _activeSequence = sequence;
    }

    private void EnsureSetup()
    {
        var root = transform as RectTransform;

        if (_rootGroup == null)
        {
            _rootGroup = GetComponent<CanvasGroup>();
            if (_rootGroup == null)
                _rootGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (_targets == null)
        {
            if (_contentPanel != null)
            {
                _targets = new List<UIAnimTargets.Target>
                {
                    new UIAnimTargets.Target { Rect = _contentPanel, Position = _contentPanel.anchoredPosition, Scale = _contentPanel.localScale }
                };
            }
            else
            {
                _targets = UIAnimTargets.Collect(root);
            }
        }

        // У попапа уже есть своё затемнение — второй тёмный слой не нужен
        if (_backdrop != null || UIAnimTargets.HasOwnDim(root))
            return;

        var parent = transform.parent;
        if (parent == null)
            return;

        var backdropObject = new GameObject(
            "ModalBackdrop",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));

        backdropObject.transform.SetParent(parent, false);
        backdropObject.transform.SetSiblingIndex(transform.GetSiblingIndex());

        var backdropRect = backdropObject.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        var backdropImage = backdropObject.GetComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, BackdropAlpha);
        backdropImage.raycastTarget = true;

        _backdrop = backdropObject.GetComponent<CanvasGroup>();
        _backdrop.alpha = 0f;
        _backdrop.blocksRaycasts = true;
        _backdrop.interactable = false;
    }

    private void DestroyBackdrop()
    {
        if (_backdrop == null)
            return;

        var backdropObject = _backdrop.gameObject;
        _backdrop = null;

        // Не удаляем фон, который был назначен в префабе
        if (backdropObject != null && backdropObject.name == "ModalBackdrop")
            Destroy(backdropObject);
    }
}
