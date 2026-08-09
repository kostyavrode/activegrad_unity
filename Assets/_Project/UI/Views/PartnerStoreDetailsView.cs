using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PartnerStoreDetailsView : MonoBehaviour
{
    private static readonly Color StatusNewBackground = new Color(0.82f, 0.95f, 0.90f, 1f);
    private static readonly Color StatusNewText = new Color(0.12f, 0.42f, 0.36f, 1f);
    private static readonly Color StatusVisitedBackground = new Color(0.93f, 0.94f, 0.94f, 1f);
    private static readonly Color StatusVisitedText = new Color(0.45f, 0.48f, 0.48f, 1f);
    private static readonly Color BodyTextColor = new Color(0.35f, 0.45f, 0.45f, 1f);
    private static readonly Color DistanceTextColor = new Color(0.18f, 0.35f, 0.35f, 1f);

    public Image Image;
    public TMP_Text Header;
    public TMP_Text AddressText;
    public TMP_Text VisitStatusText;
    public TMP_Text DescriptionText;
    public TMP_Text DistanceText;
    public Image StatusBadgeImage;
    public Button VisitButton;
    public Button CloseButton;

    private int _storeId;
    private UIModalAnimator _modalAnimator;
    private Tween _imageFadeTween;
    private TMP_Text _visitButtonLabel;

    public event Action<int> OnVisitClicked;
    public event Action OnClosed;

    private void Awake()
    {
        ResolveOptionalReferences();
        SetupModalAnimator();
        SetupButtons();
        PolishLayout();
    }

    public void Init(
        Sprite imageSprite,
        string header,
        string address,
        int storeId,
        float distanceKm = -1f,
        string[] tags = null)
    {
        _storeId = storeId;

        if (Image != null)
        {
            Image.sprite = imageSprite;
            Image.preserveAspect = true;
            PlayImageReveal();
        }

        if (Header != null)
            Header.text = !string.IsNullOrEmpty(header) ? header : "Партнёрская точка";

        if (DistanceText != null)
        {
            var distanceLabel = FormatDistance(distanceKm);
            DistanceText.text = distanceLabel;
            DistanceText.gameObject.SetActive(!string.IsNullOrEmpty(distanceLabel));
        }

        if (AddressText != null)
            AddressText.text = !string.IsNullOrEmpty(address) ? address : "Адрес не указан";

        if (DescriptionText != null)
        {
            DescriptionText.text = BuildDescription(tags);
            DescriptionText.color = BodyTextColor;
            DescriptionText.enableWordWrapping = true;
        }
    }

    public void Close()
    {
        if (_modalAnimator != null && _modalAnimator.IsClosing)
            return;

        if (CloseButton != null)
            CloseButton.interactable = false;

        _imageFadeTween?.Kill();

        if (_modalAnimator != null)
        {
            _modalAnimator.PlayHide(() => Destroy(gameObject));
            return;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (VisitButton != null)
        {
            VisitButton.onClick.RemoveAllListeners();
            UIButtonGlowEffect.Stop(VisitButton);
        }

        if (CloseButton != null)
            CloseButton.onClick.RemoveAllListeners();

        _imageFadeTween?.Kill();
        OnClosed?.Invoke();
    }

    public void SetVisitButtonState(bool canVisit)
    {
        if (VisitButton == null)
            return;

        VisitButton.interactable = canVisit;

        if (_visitButtonLabel != null)
            _visitButtonLabel.text = canVisit ? "Отметиться" : "Посещено";

        if (canVisit)
            UIButtonGlowEffect.SetActive(VisitButton, true);
        else
            UIButtonGlowEffect.Stop(VisitButton);
    }

    public void SetVisitStatus(bool visited)
    {
        if (VisitStatusText != null)
        {
            VisitStatusText.text = visited ? "Посещено" : "Новое";
            VisitStatusText.color = visited ? StatusVisitedText : StatusNewText;
        }

        if (StatusBadgeImage != null)
            StatusBadgeImage.color = visited ? StatusVisitedBackground : StatusNewBackground;
    }

    private void ResolveOptionalReferences()
    {
        if (DescriptionText == null)
            DescriptionText = FindChildText("MainText");

        if (DistanceText == null)
            DistanceText = FindChildText("DistanceText");

        if (StatusBadgeImage == null)
        {
            var statusTransform = transform.Find("Status");
            if (statusTransform != null)
                StatusBadgeImage = statusTransform.GetComponent<Image>();
        }

        if (VisitButton != null)
            _visitButtonLabel = VisitButton.GetComponentInChildren<TMP_Text>(true);
    }

    private TMP_Text FindChildText(string objectName)
    {
        var texts = GetComponentsInChildren<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == objectName)
                return texts[i];
        }

        return null;
    }

    private void SetupModalAnimator()
    {
        _modalAnimator = GetComponent<UIModalAnimator>();
        if (_modalAnimator == null)
            _modalAnimator = gameObject.AddComponent<UIModalAnimator>();
    }

    private void SetupButtons()
    {
        if (VisitButton != null)
        {
            VisitButton.onClick.AddListener(() =>
            {
                OnVisitClicked?.Invoke(_storeId);
                SetVisitButtonState(false);
                SetVisitStatus(true);
            });
            VisitButton.interactable = false;
        }

        if (CloseButton != null)
            CloseButton.onClick.AddListener(Close);
    }

    private void PolishLayout()
    {
        var scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null)
        {
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var scrollBackground = scrollRect.GetComponent<Image>();
            if (scrollBackground != null)
                scrollBackground.color = new Color(1f, 1f, 1f, 0f);
        }

        if (DistanceText == null && AddressText != null)
            CreateDistanceLabel();

        if (DistanceText != null)
            DistanceText.color = DistanceTextColor;
    }

    private void CreateDistanceLabel()
    {
        var distanceObject = new GameObject(
            "DistanceText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        var addressRect = AddressText.transform as RectTransform;
        if (addressRect == null)
            return;

        distanceObject.transform.SetParent(addressRect.parent, false);
        distanceObject.transform.SetSiblingIndex(addressRect.GetSiblingIndex());

        var distanceRect = distanceObject.GetComponent<RectTransform>();
        distanceRect.anchorMin = addressRect.anchorMin;
        distanceRect.anchorMax = addressRect.anchorMax;
        distanceRect.pivot = addressRect.pivot;
        distanceRect.sizeDelta = new Vector2(addressRect.sizeDelta.x, 48f);
        distanceRect.anchoredPosition = addressRect.anchoredPosition + new Vector2(0f, 56f);

        DistanceText = distanceObject.GetComponent<TextMeshProUGUI>();
        DistanceText.font = AddressText.font;
        DistanceText.fontSize = 34f;
        DistanceText.fontStyle = FontStyles.Bold;
        DistanceText.alignment = TextAlignmentOptions.MidlineLeft;
        DistanceText.color = DistanceTextColor;
        DistanceText.text = string.Empty;
    }

    private void PlayImageReveal()
    {
        _imageFadeTween?.Kill();

        var color = Image.color;
        Image.color = new Color(color.r, color.g, color.b, 0f);

        _imageFadeTween = Image
            .DOFade(1f, 0.35f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private static string BuildDescription(string[] tags)
    {
        var parts = new List<string>();

        if (tags != null && tags.Length > 0)
            parts.Add(string.Join("  ·  ", tags));

        parts.Add("Партнёрская точка ActiveGrad. Отметьтесь при посещении, чтобы получить награду.");

        return string.Join("\n\n", parts);
    }

    private static string FormatDistance(float distanceKm)
    {
        if (distanceKm < 0f)
            return string.Empty;

        if (distanceKm < 1f)
            return $"{Mathf.Max(1, Mathf.RoundToInt(distanceKm * 1000f))} м от вас";

        return $"{distanceKm:F1} км от вас";
    }

    public class Factory : PlaceholderFactory<PartnerStoreDetailsView> { }
}
