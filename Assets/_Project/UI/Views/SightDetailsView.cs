using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

public class SightDetailsView : MonoBehaviour
{
    public Image Image;
    public TMP_Text Header;
    public TMP_Text Description;
    public Button CheckInButton;
    public Button CaptureButton;
    public Button CloseButton;
    [FormerlySerializedAs("CaptureInfoText")] public TMP_Text CaptureNameText;
    public TMP_Text CaptureDateText;
    public TMP_Text CaptureProbabilityText;
    public Image CaptureProbabilityBar;
    public int SightID;

    private UIModalAnimator _modalAnimator;
    private bool _isFirstProbabilitySet = true;

    public Action<int> OnCheckInClicked;
    public Action<int> OnCaptureClicked;
    public event Action OnClosed;

    public void Init(Sprite imageSprite, string header, string description, int sightID)
    {
        Image.sprite = imageSprite;
        Header.text = header;
        Description.text = description;
        SightID = sightID;
    }

    private void Awake()
    {
        _modalAnimator = GetComponent<UIModalAnimator>();
        if (_modalAnimator == null)
            _modalAnimator = gameObject.AddComponent<UIModalAnimator>();

        if (CheckInButton != null)
        {
            CheckInButton.onClick.AddListener(() =>
            {
                OnCheckInClicked?.Invoke(SightID);
                SetCheckInButtonState(false);
            });
        }

        if (CaptureButton != null)
        {
            CaptureButton.onClick.AddListener(() =>
            {
                OnCaptureClicked?.Invoke(SightID);
            });
        }

        CloseButton.onClick.AddListener(Close);

        if (CheckInButton != null)
            CheckInButton.interactable = false;
        if (CaptureButton != null)
            CaptureButton.interactable = false;

        if (CaptureProbabilityBar != null)
            UIProgressBarHelper.ResetFill(CaptureProbabilityBar);
    }

    public void Close()
    {
        if (_modalAnimator != null && _modalAnimator.IsClosing)
            return;

        CloseButton.interactable = false;

        if (_modalAnimator != null)
        {
            _modalAnimator.PlayHide(() => Destroy(gameObject));
            return;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (CheckInButton != null)
            CheckInButton.onClick.RemoveAllListeners();
        if (CaptureButton != null)
            CaptureButton.onClick.RemoveAllListeners();
        CloseButton.onClick.RemoveAllListeners();

        if (CaptureProbabilityBar != null)
            UIProgressBarHelper.Kill(CaptureProbabilityBar);

        if (CaptureButton != null)
            UIButtonGlowEffect.Stop(CaptureButton);

        OnClosed?.Invoke();
    }

    public void SetCheckInButtonState(bool state)
    {
        if (CheckInButton != null)
            CheckInButton.interactable = state;
    }

    public void SetCaptureButtonState(bool canCapture)
    {
        if (CaptureButton == null)
            return;

        CaptureButton.interactable = canCapture;

        if (canCapture)
            UIButtonGlowEffect.SetActive(CaptureButton, true);
        else
            UIButtonGlowEffect.Stop(CaptureButton);
    }

    public void SetCaptureInfo(bool captured, string capturedByUsername, string capturedAt, string clanName,
        int? defenderShieldLevel = null, int? timeUntilMinutes = null, int? timeUntilSeconds = null, string blockReason = null)
    {
        if (CaptureNameText == null)
            return;

        var lines = new System.Collections.Generic.List<string>();

        if (captured)
        {
            CaptureNameText.text = !string.IsNullOrEmpty(capturedByUsername) ? capturedByUsername : "—";
            if (CaptureDateText != null)
                CaptureDateText.text = !string.IsNullOrEmpty(capturedAt) ? ParseDate(capturedAt) : "—";
        }
        else
        {
            CaptureNameText.text = "—";
            if (CaptureDateText != null)
                CaptureDateText.text = "—";
        }

        if (defenderShieldLevel.HasValue)
            lines.Add($"Уровень щита: {defenderShieldLevel.Value}");

        if (timeUntilMinutes.HasValue || timeUntilSeconds.HasValue)
        {
            int min = timeUntilMinutes ?? 0;
            int sec = timeUntilSeconds ?? 0;
            lines.Add($"Захват возможен через: {min} мин {sec} сек");
        }

        if (!string.IsNullOrEmpty(blockReason))
        {
            string reasonText = blockReason switch
            {
                "invulnerability" => "неуязвимость",
                _ => blockReason
            };
            lines.Add($"Причина блокировки: {reasonText}");
        }
    }

    public void SetCaptureProbability(int percent)
    {
        if (CaptureProbabilityText != null)
            CaptureProbabilityText.text = $"{percent}%";

        if (CaptureProbabilityBar != null)
        {
            UIProgressBarHelper.SetFillAmount(
                CaptureProbabilityBar,
                percent / 100f,
                animateFromZero: _isFirstProbabilitySet);
            _isFirstProbabilitySet = false;
        }
    }

    private string ParseDate(string dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return "";

        try
        {
            if (DateTime.TryParse(dateString, out DateTime date))
                return date.ToString("dd.MM.yyyy HH:mm");
        }
        catch { }

        return dateString;
    }
    
    public class Factory : PlaceholderFactory<SightDetailsView> { }
}
