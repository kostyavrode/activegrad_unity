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
    public Button CloseButton;
    public int SightID;
    
    public Action<int> OnCheckInClicked;

    public void Init(Sprite imageSprite, string header, string description, int sightID)
    {
        Image.sprite = imageSprite;
        Header.text = header;
        Description.text = description;
        SightID = sightID;
    }

    private void Awake()
    {
        CheckInButton.onClick.AddListener(() =>
        {
            OnCheckInClicked(SightID);
            SetCheckInButtonState(false);
        });
        CloseButton.onClick.AddListener(() => Destroy(gameObject));
        CheckInButton.interactable = false;
    }

    private void OnDestroy()
    {
        CheckInButton.onClick.RemoveAllListeners();
        CloseButton.onClick.RemoveAllListeners();
    }

    public void SetCheckInButtonState(bool state)
    {
        CheckInButton.interactable = state;
    }
    
    public class Factory : PlaceholderFactory<SightDetailsView> { }
}
