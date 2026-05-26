using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PartnerStoreDetailsView : MonoBehaviour
{
    public Image Image;
    public TMP_Text Header;
    public TMP_Text AddressText;
    public TMP_Text VisitStatusText;
    public Button VisitButton;
    public Button CloseButton;

    private int _storeId;

    public event Action<int> OnVisitClicked;
    public event Action OnClosed;

    private void Awake()
    {
        if (VisitButton != null)
        {
            VisitButton.onClick.AddListener(() =>
            {
                OnVisitClicked?.Invoke(_storeId);
                SetVisitButtonState(false);
            });
            VisitButton.interactable = false;
        }

        CloseButton.onClick.AddListener(() => Destroy(gameObject));
    }

    private void OnDestroy()
    {
        if (VisitButton != null)
            VisitButton.onClick.RemoveAllListeners();
        CloseButton.onClick.RemoveAllListeners();
        OnClosed?.Invoke();
    }

    public void Init(Sprite imageSprite, string header, string address, int storeId)
    {
        if (Image != null)
            Image.sprite = imageSprite;

        Header.text = !string.IsNullOrEmpty(header) ? header : "default";

        if (AddressText != null)
            AddressText.text = !string.IsNullOrEmpty(address) ? address : "default";

        _storeId = storeId;
    }

    public void SetVisitButtonState(bool canVisit)
    {
        if (VisitButton != null)
            VisitButton.interactable = canVisit;
    }

    public void SetVisitStatus(bool visited)
    {
        if (VisitStatusText == null) return;
        VisitStatusText.text = visited ? "Отмечено" : "Новый";
    }

    public class Factory : PlaceholderFactory<PartnerStoreDetailsView> { }
}
