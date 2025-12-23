using System;
using TMPro;
using UnityEngine;
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
    public TMP_Text CaptureInfoText;
    public int SightID;
    
    public Action<int> OnCheckInClicked;
    public Action<int> OnCaptureClicked;

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

    public void SetCaptureInfo(bool captured, string capturedByUsername, string capturedAt, string clanName)
    {
        if (CaptureInfoText == null)
            return;

        if (captured)
        {
            string clanInfo = !string.IsNullOrEmpty(clanName) ? $" ({clanName})" : "";
            string date = !string.IsNullOrEmpty(capturedAt) ? ParseDate(capturedAt) : "";
            CaptureInfoText.text = $"Захвачено: {capturedByUsername}{clanInfo}\nДата: {date}";
        }
        else
        {
            CaptureInfoText.text = "Достопримечательность еще не захвачена";
        }
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
