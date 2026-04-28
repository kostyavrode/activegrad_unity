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
    public int SightID;
    
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

        CloseButton.onClick.AddListener(() => Destroy(gameObject));
        
        if (CheckInButton != null)
            CheckInButton.interactable = false;
        if (CaptureButton != null)
            CaptureButton.interactable = false;
    }

    private void OnDestroy()
    {
        if (CheckInButton != null)
            CheckInButton.onClick.RemoveAllListeners();
        if (CaptureButton != null)
            CaptureButton.onClick.RemoveAllListeners();
        CloseButton.onClick.RemoveAllListeners();
        OnClosed?.Invoke();
    }

    public void SetCheckInButtonState(bool state)
    {
        if (CheckInButton != null)
            CheckInButton.interactable = state;
    }

    public void SetCaptureButtonState(bool canCapture)
    {
        if (CaptureButton != null)
            CaptureButton.interactable = canCapture;
    }

    public void SetCaptureInfo(bool captured, string capturedByUsername, string capturedAt, string clanName,
        int? defenderShieldLevel = null, int? timeUntilMinutes = null, int? timeUntilSeconds = null, string blockReason = null)
    {
        if (CaptureNameText == null)
            return;

        var lines = new System.Collections.Generic.List<string>();

        if (captured)
        {
            string clanInfo = !string.IsNullOrEmpty(clanName) ? $" ({clanName})" : "";
            string date = !string.IsNullOrEmpty(capturedAt) ? ParseDate(capturedAt) : "";
            CaptureDateText.text = ParseDate(capturedAt);
            CaptureNameText.text = capturedByUsername;
            //lines.Add($"Захвачено: {capturedByUsername}{clanInfo}");
            lines.Add($"Дата: {date}");
        }
        else
        {
            lines.Add("Достопримечательность еще не захвачена");
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

        //CaptureNameText.text = string.Join("\n", lines);
    }

    private string ParseDate(string dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return "";

        try
        {
            // Формат: "2024-01-01T12:00:00Z"
            if (DateTime.TryParse(dateString, out DateTime date))
            {
                return date.ToString("dd.MM.yyyy HH:mm");
            }
        }
        catch { }

        return dateString;
    }
    
    public class Factory : PlaceholderFactory<SightDetailsView> { }
}
